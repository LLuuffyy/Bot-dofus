using System;
using System.Text;
using System.Web;

namespace BotDofus.Utilitaires.Crypto;

/// <summary>
/// Déchiffrement des données carte (paquet GDM Dofus 1.29 / Hystoria).
///
/// Algorithme historique Ankama (réf. dyshay/Bot-Dofus-Retro Map.DecryptMapData) :
///   1. La <c>key</c> (la <c>dateVersion</c> du GDM) est URL-encodée
///   2. checksum = (Σ key[i]) mod 16, en restant dans la version URL-encodée
///   3. Pour chaque paire de 2 chars hex dans les données chiffrées :
///        decoded = HexParse(data[i..i+2]) XOR key[(i/2 + checksum) mod key.length]
///   4. La chaîne reconstituée est ensuite URL-décodée pour produire le texte final
///      (qui est lui-même la concaténation des cellules encodées 10 chars chacune)
///
/// Le résultat est consommable directement par <see cref="BotDofus.Divers.Cartes.DecompresseurMapData"/>.
/// </summary>
public static class DechiffreurCarte
{
    public static string Dechiffrer(string donneesChiffrees, string dateVersion)
    {
        if (string.IsNullOrEmpty(donneesChiffrees) || string.IsNullOrEmpty(dateVersion))
            return string.Empty;
        if ((donneesChiffrees.Length & 1) != 0)
            return string.Empty;

        var clefPreparee = HttpUtility.UrlEncode(dateVersion) ?? dateVersion;
        if (string.IsNullOrEmpty(clefPreparee)) return string.Empty;

        int checksum = 0;
        for (int i = 0; i < clefPreparee.Length; i++)
            checksum += clefPreparee[i] % 16;
        checksum %= 16;

        var sb = new StringBuilder(donneesChiffrees.Length / 2);
        int n = donneesChiffrees.Length / 2;
        for (int i = 0; i < n; i++)
        {
            int hexVal = HexDeux(donneesChiffrees, i * 2);
            if (hexVal < 0) return string.Empty;
            int keyVal = clefPreparee[(i + checksum) % clefPreparee.Length];
            sb.Append((char)(hexVal ^ keyVal));
        }

        try
        {
            return HttpUtility.UrlDecode(sb.ToString()) ?? sb.ToString();
        }
        catch
        {
            return sb.ToString();
        }
    }

    private static int HexDeux(string s, int offset)
    {
        int hi = HexChar(s[offset]);
        int lo = HexChar(s[offset + 1]);
        if (hi < 0 || lo < 0) return -1;
        return (hi << 4) | lo;
    }

    private static int HexChar(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1,
    };
}
