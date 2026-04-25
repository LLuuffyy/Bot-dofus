using System;
using System.Text;

namespace BotDofus.Utilitaires.Crypto;

/// <summary>
/// Routines de chiffrement et de codage du protocole Dofus Retro 1.29 :
///  - chiffrement du mot de passe avec la clé reçue via <c>HC</c> (préfixe "#1")
///  - décodage de l'IP (<c>cryptedIp</c>) et du port (<c>cryptedPort</c>) renvoyés par <c>AYK</c>
///
/// Ces algorithmes sont documentés dans plusieurs implémentations open-source
/// (Guinness-Bot Kotlin, Romain-P, AstrubTools/dofus-protocol).
/// Le protocole étant textuel, ces routines manipulent uniquement de l'ASCII imprimable.
/// </summary>
public static class ChiffrementDofus
{
    /// <summary>Alphabet hex Dofus (16 caractères) utilisé par <see cref="ChiffrerMotDePasse"/>.</summary>
    private const string AlphabetHex = "0123456789abcdef";

    /// <summary>Alphabet base-64-like utilisé par cryptedIp/cryptedPort.</summary>
    private const string AlphabetBase64Dofus = "-_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>
    /// Chiffre un mot de passe pour la phase d'authentification (paquet AA).
    /// Algorithme Ankama :
    ///   - on préfixe le résultat par "#1"
    ///   - pour chaque caractère du mdp, on additionne ses deux nibbles à la
    ///     valeur ASCII modulée du caractère correspondant de la clé,
    ///     modulo 16 ; chaque nibble produit un caractère hex.
    /// </summary>
    public static string ChiffrerMotDePasse(string motDePasse, string clePublique)
    {
        if (string.IsNullOrEmpty(motDePasse) || string.IsNullOrEmpty(clePublique))
        {
            return "#1";
        }

        var sb = new StringBuilder(motDePasse.Length * 2 + 2);
        sb.Append("#1");

        for (int i = 0; i < motDePasse.Length; i++)
        {
            int p = motDePasse[i];
            int k = clePublique[i % clePublique.Length];

            int hautMdp = (p >> 4) & 0x0F;
            int basMdp = p & 0x0F;
            int hautCle = (k >> 4) & 0x0F;
            int basCle = k & 0x0F;

            int hautChiffre = (hautMdp + hautCle) & 0x0F;
            int basChiffre = (basMdp + basCle) & 0x0F;

            sb.Append(AlphabetHex[hautChiffre]);
            sb.Append(AlphabetHex[basChiffre]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Décode un champ <c>cryptedIp</c> (8 caractères Dofus) en notation a.b.c.d.
    /// Chaque octet est encodé sur 2 caractères de l'alphabet base-64-like.
    /// </summary>
    public static string DecoderIpChiffree(string ipChiffree)
    {
        if (string.IsNullOrEmpty(ipChiffree) || ipChiffree.Length < 8)
        {
            return "0.0.0.0";
        }

        var octets = new int[4];
        for (int i = 0; i < 4; i++)
        {
            int haut = AlphabetBase64Dofus.IndexOf(ipChiffree[i * 2]);
            int bas = AlphabetBase64Dofus.IndexOf(ipChiffree[i * 2 + 1]);
            if (haut < 0) haut = 0;
            if (bas < 0) bas = 0;
            octets[i] = (haut * AlphabetBase64Dofus.Length + bas) & 0xFF;
        }
        return string.Join('.', octets);
    }

    /// <summary>
    /// Décode un champ <c>cryptedPort</c> (3 caractères) en entier 16 bits.
    /// </summary>
    public static int DecoderPortChiffre(string portChiffre)
    {
        if (string.IsNullOrEmpty(portChiffre) || portChiffre.Length < 3) return 0;
        int r = 0;
        foreach (var c in portChiffre)
        {
            int idx = AlphabetBase64Dofus.IndexOf(c);
            if (idx < 0) idx = 0;
            r = r * AlphabetBase64Dofus.Length + idx;
        }
        return r & 0xFFFF;
    }
}
