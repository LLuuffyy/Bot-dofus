using System;
using System.Text;

namespace BotDofus.Utilitaires.Crypto;

/// <summary>
/// Routines de chiffrement propres au protocole Dofus Retro 1.29 :
///  - chiffrement du mot de passe avec la clé reçue via <c>HC</c>
///    (XOR en alphabet imprimable puis mise en hexadécimal)
///  - décryptage de l'IP et du port fournis via <c>AYK</c> (cryptedIp/cryptedPort)
///
/// Placeholder tant que les constantes exactes d'Hystoria ne sont pas vérifiées ;
/// ces implémentations correspondent à l'algorithme documenté dans les
/// références open-source (Guinness-Bot Kotlin, Dofus-1.29 Amakna).
/// </summary>
public static class ChiffrementDofus
{
    private const string AlphabetHex = "ABCDEF";
    private const string AlphabetEtendu = "-_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>
    /// Chiffre un mot de passe à l'aide de la clé publique reçue dans le paquet HC.
    /// L'algorithme Dofus Retro repose sur un XOR caractère par caractère entre
    /// la clé et le mot de passe, chaque octet étant ensuite transformé en deux
    /// caractères hex préfixés d'un marqueur.
    /// </summary>
    public static string ChiffrerMotDePasse(string motDePasse, string clePublique)
    {
        if (string.IsNullOrEmpty(motDePasse) || string.IsNullOrEmpty(clePublique))
            return motDePasse ?? string.Empty;

        // Implémentation placeholder : XOR caractère par caractère, encodé en hex.
        // TODO : remplacer par l'algorithme exact Ankama (voir Guinness-Bot Kotlin
        //        et D1ElectronLauncher deobfusqué pour la variante Hystoria).
        var sb = new StringBuilder(motDePasse.Length * 2 + 1);
        sb.Append('#');

        for (int i = 0; i < motDePasse.Length; i++)
        {
            int aV = motDePasse[i];
            int bV = clePublique[i % clePublique.Length];
            int xor = aV ^ (bV & 0x7F);

            int indexHaut = (xor >> 4) & 0x0F;
            int indexBas = xor & 0x0F;
            sb.Append(AlphabetHex[indexHaut % AlphabetHex.Length]);
            sb.Append(AlphabetHex[indexBas % AlphabetHex.Length]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Décode une IP chiffrée Dofus (champ cryptedIp) en retournant la notation a.b.c.d.
    /// Chaque caractère de la chaîne encode un octet via <see cref="AlphabetEtendu"/>.
    /// </summary>
    public static string DecoderIpChiffree(string ipChiffree)
    {
        if (string.IsNullOrEmpty(ipChiffree) || ipChiffree.Length < 4)
            return "0.0.0.0";

        var octets = new int[4];
        for (int i = 0; i < 4; i++)
        {
            octets[i] = AlphabetEtendu.IndexOf(ipChiffree[i]);
            if (octets[i] < 0) octets[i] = 0;
        }
        return string.Join('.', octets);
    }

    /// <summary>Décode un port chiffré Dofus (champ cryptedPort) en retournant l'entier.</summary>
    public static int DecoderPortChiffre(string portChiffre)
    {
        if (string.IsNullOrEmpty(portChiffre) || portChiffre.Length < 3) return 0;
        int r = 0;
        for (int i = 0; i < portChiffre.Length; i++)
        {
            var pos = AlphabetEtendu.IndexOf(portChiffre[i]);
            if (pos < 0) pos = 0;
            r = (r * AlphabetEtendu.Length) + pos;
        }
        return r;
    }
}
