using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Wpf;

public static class ClientDofusPrepare
{
    private const string IpOriginale = "162.19.127.156";
    private const string IpLocaleMemeLongueur = "127.000.000.01";

    public static string PreparerCopieLocale(string cheminClientOriginal)
    {
        var dossierOriginal = Path.GetDirectoryName(cheminClientOriginal)
            ?? throw new InvalidOperationException("Chemin client Dofus invalide.");

        var dossierCopie = Path.Combine(AppContext.BaseDirectory, "client-luffy", "retroclient");
        CopierDossierIncremental(dossierOriginal, dossierCopie);
        PatcherCoreSwf(Path.Combine(dossierCopie, "modules", "core.swf"));
        ForcerConfigProxy(Path.Combine(dossierCopie, "config.xml"));

        var cheminClientCopie = Path.Combine(dossierCopie, Path.GetFileName(cheminClientOriginal));
        if (!File.Exists(cheminClientCopie))
        {
            throw new FileNotFoundException("Client Dofus copie introuvable.", cheminClientCopie);
        }

        return cheminClientCopie;
    }

    private static void CopierDossierIncremental(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        var fichiers = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories).ToList();
        Journaliseur.Info($"Preparation client isole Luffy-bot : {fichiers.Count} fichiers a verifier");

        foreach (var fichierSource in fichiers)
        {
            var relatif = Path.GetRelativePath(source, fichierSource);
            var fichierDestination = Path.Combine(destination, relatif);
            Directory.CreateDirectory(Path.GetDirectoryName(fichierDestination)!);

            if (File.Exists(fichierDestination))
            {
                var sourceInfo = new FileInfo(fichierSource);
                var destinationInfo = new FileInfo(fichierDestination);
                if (sourceInfo.Length == destinationInfo.Length && sourceInfo.LastWriteTimeUtc <= destinationInfo.LastWriteTimeUtc)
                {
                    continue;
                }
            }

            File.Copy(fichierSource, fichierDestination, overwrite: true);
        }
    }

    private static void PatcherCoreSwf(string cheminCore)
    {
        if (!File.Exists(cheminCore))
        {
            Journaliseur.Avertir($"core.swf introuvable dans la copie : {cheminCore}");
            return;
        }

        var octets = File.ReadAllBytes(cheminCore);
        var resultat = octets.Length >= 3 && octets[0] == 'C' && octets[1] == 'W' && octets[2] == 'S'
            ? PatcherCws(octets)
            : PatcherBrut(octets);

        if (resultat.Patches == 0)
        {
            Journaliseur.Avertir($"IP {IpOriginale} introuvable dans la copie de core.swf");
            return;
        }

        File.WriteAllBytes(cheminCore, resultat.Octets);
        Journaliseur.Info($"core.swf copie patche : {resultat.Patches} occurrence(s), {IpOriginale} -> {IpLocaleMemeLongueur}");
    }

    private static (byte[] Octets, int Patches) PatcherCws(byte[] swf)
    {
        var entete = swf.Take(8).ToArray();
        var compresse = swf.Skip(8).ToArray();
        byte[] decompresse;

        using (var entree = new MemoryStream(compresse))
        using (var zlib = new ZLibStream(entree, CompressionMode.Decompress))
        using (var sortie = new MemoryStream())
        {
            zlib.CopyTo(sortie);
            decompresse = sortie.ToArray();
        }

        var patches = RemplacerToutesOccurrences(decompresse, Encoding.ASCII.GetBytes(IpOriginale), Encoding.ASCII.GetBytes(IpLocaleMemeLongueur));
        if (patches == 0)
        {
            return (swf, 0);
        }

        byte[] nouveauCompresse;
        using (var sortie = new MemoryStream())
        {
            using (var zlib = new ZLibStream(sortie, CompressionLevel.SmallestSize, leaveOpen: true))
            {
                zlib.Write(decompresse);
            }

            nouveauCompresse = sortie.ToArray();
        }

        var nouveauSwf = new byte[8 + nouveauCompresse.Length];
        Buffer.BlockCopy(entete, 0, nouveauSwf, 0, entete.Length);
        Buffer.BlockCopy(nouveauCompresse, 0, nouveauSwf, 8, nouveauCompresse.Length);
        return (nouveauSwf, patches);
    }

    private static (byte[] Octets, int Patches) PatcherBrut(byte[] swf)
        => (swf, RemplacerToutesOccurrences(swf, Encoding.ASCII.GetBytes(IpOriginale), Encoding.ASCII.GetBytes(IpLocaleMemeLongueur)));

    private static int RemplacerToutesOccurrences(byte[] donnees, byte[] cherche, byte[] remplace)
    {
        var total = 0;
        for (var i = 0; i <= donnees.Length - cherche.Length; i++)
        {
            var trouve = true;
            for (var j = 0; j < cherche.Length; j++)
            {
                if (donnees[i + j] != cherche[j])
                {
                    trouve = false;
                    break;
                }
            }

            if (!trouve)
            {
                continue;
            }

            Buffer.BlockCopy(remplace, 0, donnees, i, remplace.Length);
            total++;
            i += cherche.Length - 1;
        }

        return total;
    }

    private static void ForcerConfigProxy(string cheminConfig)
    {
        if (!File.Exists(cheminConfig))
        {
            return;
        }

        var document = XDocument.Load(cheminConfig, LoadOptions.PreserveWhitespace);
        var configuration = document.Root?.Elements("conf").FirstOrDefault();
        if (configuration == null)
        {
            return;
        }

        configuration.SetAttributeValue("name", "En ligne (Luffy-bot)");
        var connserver = configuration.Element("connserver") ?? new XElement("connserver");
        if (connserver.Parent == null)
        {
            configuration.AddFirst(connserver);
        }

        connserver.SetAttributeValue("name", "Luffy-bot");
        connserver.SetAttributeValue("ip", IpLocaleMemeLongueur);
        connserver.SetAttributeValue("port", "450");

        var servers = configuration.Element("servers") ?? new XElement("servers");
        if (servers.Parent == null)
        {
            configuration.Add(servers);
        }

        var server = servers.Elements("server").FirstOrDefault() ?? new XElement("server");
        if (server.Parent == null)
        {
            servers.Add(server);
        }

        server.SetAttributeValue("id", server.Attribute("id")?.Value ?? "601");
        server.SetAttributeValue("ip", "127.0.0.1");
        server.SetAttributeValue("port", "5556");
        document.Save(cheminConfig);
    }
}
