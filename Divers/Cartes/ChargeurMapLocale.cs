using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using BotDofus.Utilitaires.Hystoria;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Cartes;

public static class ChargeurMapLocale
{
    public static string? ChargerMapData(int mapId, string dateVersion)
    {
        var racineDofus = Path.GetDirectoryName(LanceurDofus.Localiser() ?? LanceurDofus.CheminDefaut);
        if (string.IsNullOrWhiteSpace(racineDofus)) return null;

        var dossierMaps = Path.Combine(racineDofus, "data", "maps");
        var candidats = Directory.Exists(dossierMaps)
            ? Directory.GetFiles(dossierMaps, $"{mapId}_{dateVersion}*.swf")
            : Array.Empty<string>();

        var fichier = candidats.FirstOrDefault();
        if (fichier == null)
        {
            Journaliseur.Avertir($"[MAP] Fichier map introuvable : {mapId}_{dateVersion}*.swf");
            return null;
        }

        try
        {
            var bytes = File.ReadAllBytes(fichier);
            var swf = DecompresserSwf(bytes);
            var mapData = ExtraireChaineApresClef(swf, "mapData");
            if (!string.IsNullOrWhiteSpace(mapData))
            {
                Journaliseur.Info($"[MAP] SWF local charge : {Path.GetFileName(fichier)} ({mapData.Length} chars mapData)");
                return mapData;
            }

            Journaliseur.Avertir($"[MAP] mapData absent dans {Path.GetFileName(fichier)}");
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[MAP] Lecture SWF impossible ({Path.GetFileName(fichier)}) : {ex.Message}");
        }

        return null;
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
