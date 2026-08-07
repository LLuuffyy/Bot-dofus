using System;

namespace BotDofus.Utilitaires.Crypto;

/// <summary>
/// Encodage/décodage des identifiants de cellule pour les paquets Dofus 1.29.
/// Chaque cellId (0..4095) est encodé en 2 chars base64 custom Dofus.
///
/// L'alphabet 64 chars est : a..z A..Z 0..9 - _
/// L'ORDRE EST CRITIQUE — il diffère de l'alphabet base64 standard ET de
/// celui utilisé par <see cref="ChiffrementDofus"/> pour les IPs (qui a - et _ en début).
/// Référence : dyshay/Bot-Dofus-Retro Utilities/Crypto/Hash.cs.
/// </summary>
public static class HashCarte
{
    public static readonly char[] Alphabet =
    {
        'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p',
        'q','r','s','t','u','v','w','x','y','z',
        'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P',
        'Q','R','S','T','U','V','W','X','Y','Z',
        '0','1','2','3','4','5','6','7','8','9','-','_'
    };

    public const int TailleAlphabet = 64;

    /// <summary>Encode un cellId en 2 chars (haut nibble + bas nibble base 64).</summary>
    public static string EncoderCellule(int cellId)
        => $"{Alphabet[cellId / 64]}{Alphabet[cellId % 64]}";

    /// <summary>Décode 2 chars vers un cellId.</summary>
    public static int DecoderCellule(string hash)
    {
        if (hash == null || hash.Length < 2) return 0;
        return IndexCar(hash[0]) * 64 + IndexCar(hash[1]);
    }

    /// <summary>Retourne l'index dans l'alphabet d'un caractère, ou -1 si introuvable.</summary>
    public static int IndexCar(char c)
    {
        for (int i = 0; i < Alphabet.Length; i++)
        {
            if (Alphabet[i] == c) return i;
        }
        return -1;
    }
}
