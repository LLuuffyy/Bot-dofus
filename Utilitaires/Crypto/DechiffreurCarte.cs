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
    public static string DechiffrerDonneesMap(string donneesMapChiffrees, string clefServeurHex)
    {
        if (string.IsNullOrEmpty(donneesMapChiffrees) || string.IsNullOrEmpty(clefServeurHex))
            return string.Empty;

        var clef = PreparerClef(clefServeurHex);
        if (string.IsNullOrEmpty(clef)) return string.Empty;

        var checksum = Checksum(clef) * 2;
        return DechiffrerHex(donneesMapChiffrees, clef, checksum);
    }

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

        return UnescapeFlash(sb.ToString());
    }

    private static string PreparerClef(string clefHex)
    {
        if ((clefHex.Length & 1) != 0) return string.Empty;

        var sb = new StringBuilder(clefHex.Length / 2);
        for (int i = 0; i < clefHex.Length; i += 2)
        {
            var hex = HexDeux(clefHex, i);
            if (hex < 0) return string.Empty;
            sb.Append((char)hex);
        }

        return UnescapeFlash(sb.ToString());
    }

    private static int Checksum(string s)
    {
        var somme = 0;
        for (int i = 0; i < s.Length; i++)
        {
            somme += s[i] % 16;
        }
        return somme % 16;
    }

    private static string DechiffrerHex(string donneesHex, string clef, int decalage)
    {
        if ((donneesHex.Length & 1) != 0 || clef.Length == 0) return string.Empty;

        var sb = new StringBuilder(donneesHex.Length / 2);
        int n = donneesHex.Length / 2;
        for (int i = 0; i < n; i++)
        {
            int hexVal = HexDeux(donneesHex, i * 2);
            if (hexVal < 0) return string.Empty;
            int keyVal = clef[(i + decalage) % clef.Length];
            sb.Append((char)(hexVal ^ keyVal));
        }

        return UnescapeFlash(sb.ToString());
    }

    /// <summary>
    /// Réimplémentation EXACTE de la fonction <c>unescape()</c> d'ActionScript/Flash
    /// (et NON <see cref="HttpUtility.UrlDecode"/>, qui n'est PAS équivalent) :
    ///
    ///   • <c>%XX</c> → 1 seul caractère = l'octet de valeur 0xXX (Latin-1, par octet).
    ///     .NET UrlDecode combine au contraire les <c>%XX</c> en UTF-8 multi-octets,
    ///     ce qui FUSIONNE 2-3 octets en 1 char et RACCOURCIT la chaîne (symptôme
    ///     observé : 479/560 cellules au lieu de 560, carte « rien à voir »).
    ///   • <c>%uXXXX</c> → 1 caractère Unicode (rare, pas dans les data carte mais géré).
    ///   • <c>+</c> est laissé TEL QUEL (UrlDecode le transforme en espace → corrompt
    ///     la clé binaire et les data déchiffrées).
    ///   • tout autre caractère est recopié verbatim.
    ///
    /// La clé (<see cref="PreparerClef"/>) et les data déchiffrées sont du binaire
    /// brut (octets 0-255) : seule la sémantique Flash par octet donne le bon
    /// résultat. Réf. Ankama Map.prepareKey / DecryptMapData.
    /// </summary>
    private static string UnescapeFlash(string texte)
    {
        if (string.IsNullOrEmpty(texte)) return texte;

        var sb = new StringBuilder(texte.Length);
        for (int i = 0; i < texte.Length; i++)
        {
            char c = texte[i];
            if (c != '%')
            {
                sb.Append(c);
                continue;
            }

            // %uXXXX (séquence Unicode Flash)
            if (i + 5 < texte.Length && (texte[i + 1] == 'u' || texte[i + 1] == 'U'))
            {
                int h3 = HexChar(texte[i + 2]), h2 = HexChar(texte[i + 3]);
                int h1 = HexChar(texte[i + 4]), h0 = HexChar(texte[i + 5]);
                if (h3 >= 0 && h2 >= 0 && h1 >= 0 && h0 >= 0)
                {
                    sb.Append((char)((h3 << 12) | (h2 << 8) | (h1 << 4) | h0));
                    i += 5;
                    continue;
                }
            }

            // %XX (octet brut Latin-1)
            if (i + 2 < texte.Length)
            {
                int hi = HexChar(texte[i + 1]);
                int lo = HexChar(texte[i + 2]);
                if (hi >= 0 && lo >= 0)
                {
                    sb.Append((char)((hi << 4) | lo));
                    i += 2;
                    continue;
                }
            }

            // '%' isolé ou séquence invalide : recopié verbatim (comportement Flash).
            sb.Append(c);
        }

        return sb.ToString();
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
