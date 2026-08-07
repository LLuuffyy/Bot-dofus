using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using BotDofus.Utilitaires.Hystoria;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Cartes;

/// <summary>
/// Données extraites d'un SWF de carte Dofus : la <see cref="MapData"/> chiffrée,
/// et surtout les dimensions <see cref="Largeur"/>/<see cref="Hauteur"/>.
///
/// CRITIQUE : la conversion id → (x,y) dépend de la LARGEUR de la carte. Une
/// carte Dofus 1.29 standard fait 15×17 (= 15·17 + 14·16 = 479 cellules), PAS
/// 14×20. Si on suppose une largeur fixe fausse, toutes les coordonnées sont
/// décalées et la carte rendue n'a « rien à voir » avec le client.
/// </summary>
public sealed record DonneesMapSwf(string? MapData, int Largeur, int Hauteur);

public static class ChargeurMapLocale
{
    // Carte Dofus 1.29 standard : 15 large × 17 haut = 479 cellules.
    public const int LargeurStandard = 15;
    public const int HauteurStandard = 17;

    public static string? ChargerMapData(int mapId, string dateVersion)
        => ChargerInfos(mapId, dateVersion).MapData;

    public static DonneesMapSwf ChargerInfos(int mapId, string dateVersion)
    {
        var racineDofus = Path.GetDirectoryName(LanceurDofus.Localiser() ?? LanceurDofus.CheminDefaut);
        if (string.IsNullOrWhiteSpace(racineDofus))
            return new DonneesMapSwf(null, LargeurStandard, HauteurStandard);

        var dossierMaps = Path.Combine(racineDofus, "data", "maps");
        var candidats = Directory.Exists(dossierMaps)
            ? Directory.GetFiles(dossierMaps, $"{mapId}_{dateVersion}*.swf")
            : Array.Empty<string>();

        var fichier = candidats.FirstOrDefault();
        if (fichier == null)
        {
            Journaliseur.Avertir($"[MAP] Fichier map introuvable : {mapId}_{dateVersion}*.swf");
            return new DonneesMapSwf(null, LargeurStandard, HauteurStandard);
        }

        try
        {
            var bytes = File.ReadAllBytes(fichier);
            var swf = DecompresserSwf(bytes);

            var mapData = ExtraireChaineApresClef(swf, "mapData");
            int largeur = ExtraireEntierApresClef(swf, "width", LargeurStandard);
            int hauteur = ExtraireEntierApresClef(swf, "height", HauteurStandard);

            if (!string.IsNullOrWhiteSpace(mapData))
            {
                Journaliseur.Info(
                    $"[MAP] SWF local charge : {Path.GetFileName(fichier)} " +
                    $"({mapData.Length} chars mapData, {largeur}×{hauteur})");
                return new DonneesMapSwf(mapData, largeur, hauteur);
            }

            Journaliseur.Avertir($"[MAP] mapData absent dans {Path.GetFileName(fichier)}");
            return new DonneesMapSwf(null, largeur, hauteur);
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[MAP] Lecture SWF impossible ({Path.GetFileName(fichier)}) : {ex.Message}");
        }

        return new DonneesMapSwf(null, LargeurStandard, HauteurStandard);
    }

    /// <summary>
    /// Extrait une variable numérique (ex. <c>width</c>, <c>height</c>) stockée
    /// dans le pool de constantes du SWF juste après <c>clef\0</c>. Retourne
    /// <paramref name="defaut"/> si introuvable ou non plausible (1..40).
    /// </summary>
    private static int ExtraireEntierApresClef(byte[] bytes, string clef, int defaut)
    {
        var motif = Encoding.Latin1.GetBytes(clef);
        for (int i = 0; i <= bytes.Length - motif.Length - 1; i++)
        {
            var ok = true;
            for (int j = 0; j < motif.Length; j++)
            {
                if (bytes[i + j] != motif[j]) { ok = false; break; }
            }

            if (!ok || bytes[i + motif.Length] != 0) continue;

            int debut = i + motif.Length + 1;
            int fin = debut;
            while (fin < bytes.Length && bytes[fin] != 0 && (fin - debut) < 8) fin++;
            if (fin <= debut) continue;

            var brut = Encoding.Latin1.GetString(bytes, debut, fin - debut);
            if (int.TryParse(brut, out var v) && v is >= 1 and <= 40)
                return v;
        }

        return defaut;
    }

    private static byte[] DecompresserSwf(byte[] bytes)
    {
        if (bytes.Length < 8) return bytes;
        if (bytes[0] == (byte)'F' && bytes[1] == (byte)'W' && bytes[2] == (byte)'S') return bytes;
        if (bytes[0] != (byte)'C' || bytes[1] != (byte)'W' || bytes[2] != (byte)'S') return bytes;

        using var input = new MemoryStream(bytes, 8, bytes.Length - 8);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        output.Write(bytes, 0, 8);
        zlib.CopyTo(output);
        return output.ToArray();
    }

    private static string? ExtraireChaineApresClef(byte[] bytes, string clef)
    {
        var motif = Encoding.Latin1.GetBytes(clef);
        for (int i = 0; i <= bytes.Length - motif.Length - 1; i++)
        {
            var ok = true;
            for (int j = 0; j < motif.Length; j++)
            {
                if (bytes[i + j] != motif[j])
                {
                    ok = false;
                    break;
                }
            }

            if (!ok || bytes[i + motif.Length] != 0) continue;

            var debut = i + motif.Length + 1;
            var fin = debut;
            while (fin < bytes.Length && bytes[fin] != 0) fin++;
            if (fin <= debut) continue;

            var valeur = Encoding.Latin1.GetString(bytes, debut, fin - debut);
            if (valeur.Length > 100 && valeur.All(EstHexOuPourcent))
                return valeur;
        }

        return null;
    }

    private static bool EstHexOuPourcent(char c)
        => c == '%' || c is >= '0' and <= '9' || c is >= 'a' and <= 'f' || c is >= 'A' and <= 'F';
}
