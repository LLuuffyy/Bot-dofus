using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Utilitaires.Hystoria;

/// <summary>
/// Patche le fichier core.swf du client Hystoria pour rediriger sa connexion sortante
/// du vrai serveur (162.19.127.156) vers un hostname résolu localement (via le hosts file).
///
/// L'IP est codée EN CLAIR (bytes ASCII) dans le bytecode AS2 de core.swf. Approche :
///  1. Lire core.swf, détecter signature CWS (zlib compressed) ou FWS (uncompressed)
///  2. Si CWS : décompresser le body
///  3. Trouver et remplacer la chaîne ASCII de l'IP par le hostname (MÊME LONGUEUR obligatoire pour
///     ne pas décaler les offsets bytecode)
///  4. Écrire le SWF en FWS (uncompressed) — plus simple et robuste, pas de problème de taille
///
/// Pattern observé dans le bot SynFus de référence :
///   IP originale : "162.19.127.156" (14 chars)
///   Remplacement : "synfusprxy.xxx" (14 chars)
///
/// Notre choix : "dofusproxy.bot" (14 chars exactement). On évite ".local" (mDNS) et ".xxx"
/// (TLD pornographique parfois filtré par les antivirus).
/// </summary>
public sealed class PatcheurCoreSwf
{
    /// <summary>IP originale du serveur d'auth Hystoria, en dur dans core.swf.</summary>
    public const string IpOriginale = "162.19.127.156";

    /// <summary>
    /// Hostname de remplacement (14 chars EXACTEMENT, même longueur que l'IP).
    /// .xxx car c'est le TLD utilisé par SynFus (preuve que ça marche sur Hystoria).
    /// Ne pas utiliser .local (mDNS bypass le hosts file). .bot semble silencieusement
    /// rejeté par le serveur Hystoria — symptôme : login forwardé mais aucune réponse Af.
    /// </summary>
    public const string HostnameProxy = "dofusproxy.xxx";

    static PatcheurCoreSwf()
    {
        if (IpOriginale.Length != HostnameProxy.Length)
        {
            throw new InvalidOperationException(
                $"HostnameProxy '{HostnameProxy}' doit faire EXACTEMENT {IpOriginale.Length} chars (taille de l'IP), "
                + $"sinon le bytecode AS2 sera décalé.");
        }
    }

    /// <summary>
    /// Patche le core.swf. Crée un backup `core_original.swf` à côté s'il n'existe pas
    /// (ou utilise celui qu'un autre bot comme SynFus aurait déjà créé).
    /// </summary>
    /// <param name="cheminCoreSwf">Chemin absolu vers core.swf (ex: AppData\Local\Hystoria\Dofus\resources\app\retroclient\modules\core.swf)</param>
    /// <returns>Nombre d'occurrences patchées (devrait être 1 si tout va bien, 0 si déjà patché ou IP introuvable)</returns>
    public int Patcher(string cheminCoreSwf)
    {
        if (!File.Exists(cheminCoreSwf))
        {
            throw new FileNotFoundException($"core.swf introuvable : {cheminCoreSwf}");
        }

        string cheminBackup = Path.Combine(
            Path.GetDirectoryName(cheminCoreSwf)!,
            "core_original.swf");

        // 1. Backup : si déjà existant on le restaure (au cas où une session précédente
        //    aurait patché et qu'on re-patche), sinon on le crée.
        if (File.Exists(cheminBackup))
        {
            File.Copy(cheminBackup, cheminCoreSwf, overwrite: true);
            Journaliseur.Info("[PATCH] Backup existant restauré avant re-patch.");
        }
        else
        {
            File.Copy(cheminCoreSwf, cheminBackup, overwrite: false);
            Journaliseur.Info($"[PATCH] Backup créé : {cheminBackup}");
        }

        byte[] swfBrut = File.ReadAllBytes(cheminCoreSwf);
        Journaliseur.Info($"[PATCH] core.swf lu : {swfBrut.Length} octets | IP serveur : {IpOriginale} ({IpOriginale.Length} chars)");

        // 2. Détecte CWS / FWS / ZWS
        if (swfBrut.Length < 8)
        {
            throw new InvalidDataException("core.swf trop petit pour être un SWF valide");
        }

        string signature = Encoding.ASCII.GetString(swfBrut, 0, 3);
        byte version = swfBrut[3];
        // bytes 4-7 = file length (little endian, taille décompressée du body après header)

        byte[] bodyDecompresse;
        switch (signature)
        {
            case "CWS":
                Journaliseur.Info($"[SWF] Signature CWS v{version} → décompression zlib");
                bodyDecompresse = DecompresserZlib(swfBrut, offsetBody: 8);
                Journaliseur.Info($"[SWF] Décompressé : {bodyDecompresse.Length} octets");
                break;
            case "FWS":
                Journaliseur.Info($"[SWF] Signature FWS v{version} → déjà décompressé");
                bodyDecompresse = new byte[swfBrut.Length - 8];
                Buffer.BlockCopy(swfBrut, 8, bodyDecompresse, 0, bodyDecompresse.Length);
                break;
            case "ZWS":
                throw new NotSupportedException("Signature ZWS (LZMA) non supportée — décompresser manuellement avec un outil tiers");
            default:
                throw new InvalidDataException($"Signature SWF inconnue : '{signature}'");
        }

        // 3. Trouver et remplacer l'IP
        byte[] motifIp = Encoding.ASCII.GetBytes(IpOriginale);
        byte[] remplacement = Encoding.ASCII.GetBytes(HostnameProxy);

        int occurrences = 0;
        for (int i = 0; i <= bodyDecompresse.Length - motifIp.Length; i++)
        {
            if (CompareBytes(bodyDecompresse, i, motifIp))
            {
                Buffer.BlockCopy(remplacement, 0, bodyDecompresse, i, remplacement.Length);
                occurrences++;
                Journaliseur.Info($"[SWF] Patch à l'offset 0x{i:X} : {IpOriginale} → {HostnameProxy}");
            }
        }

        if (occurrences == 0)
        {
            // Pas grave : peut être un re-patch sur le même fichier après restauration ratée.
            // On vérifie si le hostname est déjà présent.
            byte[] motifHost = Encoding.ASCII.GetBytes(HostnameProxy);
            for (int i = 0; i <= bodyDecompresse.Length - motifHost.Length; i++)
            {
                if (CompareBytes(bodyDecompresse, i, motifHost))
                {
                    Journaliseur.Info($"[SWF] Hostname {HostnameProxy} déjà présent à l'offset 0x{i:X} (déjà patché)");
                    return 0;
                }
            }
            Journaliseur.Avertir($"[SWF] AUCUNE occurrence de {IpOriginale} trouvée dans core.swf — Hystoria a peut-être changé l'IP, vérifier la spec");
            return 0;
        }

        // 4. Réécrire en FWS (uncompressed) : plus simple, pas de problème de taille
        byte[] swfFws = new byte[8 + bodyDecompresse.Length];
        swfFws[0] = (byte)'F';
        swfFws[1] = (byte)'W';
        swfFws[2] = (byte)'S';
        swfFws[3] = version;
        // file length = total uncompressed size (header + body)
        int fileLength = swfFws.Length;
        swfFws[4] = (byte)(fileLength & 0xFF);
        swfFws[5] = (byte)((fileLength >> 8) & 0xFF);
        swfFws[6] = (byte)((fileLength >> 16) & 0xFF);
        swfFws[7] = (byte)((fileLength >> 24) & 0xFF);
        Buffer.BlockCopy(bodyDecompresse, 0, swfFws, 8, bodyDecompresse.Length);

        File.WriteAllBytes(cheminCoreSwf, swfFws);
        Journaliseur.Info($"[PATCH] {occurrences} IP patchée(s) (FWS écrit, {swfFws.Length} octets) : {IpOriginale} → {HostnameProxy}");

        return occurrences;
    }

    /// <summary>Restaure le core_original.swf en core.swf (annule le patch).</summary>
    public bool Restaurer(string cheminCoreSwf)
    {
        string cheminBackup = Path.Combine(
            Path.GetDirectoryName(cheminCoreSwf)!,
            "core_original.swf");

        if (!File.Exists(cheminBackup))
        {
            Journaliseur.Avertir($"[PATCH] Aucun backup à restaurer : {cheminBackup}");
            return false;
        }

        File.Copy(cheminBackup, cheminCoreSwf, overwrite: true);
        Journaliseur.Info($"[PATCH] core.swf restauré depuis le backup");
        return true;
    }

    /// <summary>
    /// Décompresse les octets ZLIB du body d'un SWF CWS.
    /// Le body commence à l'offset 8 (après "CWS" + version + 4 bytes file length).
    /// Le SWF utilise du zlib (RFC 1950) avec un header 2 bytes.
    /// </summary>
    private static byte[] DecompresserZlib(byte[] swfCws, int offsetBody)
    {
        // Skip les 2 bytes du header zlib (0x78 0x9C ou similaire) pour utiliser DeflateStream
        // qui ne sait pas lire le header zlib.
        using var ms = new MemoryStream(swfCws, offsetBody + 2, swfCws.Length - offsetBody - 2);
        using var deflate = new DeflateStream(ms, CompressionMode.Decompress);
        using var sortie = new MemoryStream();
        deflate.CopyTo(sortie);
        return sortie.ToArray();
    }

    private static bool CompareBytes(byte[] tableau, int offset, byte[] motif)
    {
        for (int i = 0; i < motif.Length; i++)
        {
            if (tableau[offset + i] != motif[i]) return false;
        }
        return true;
    }
}
