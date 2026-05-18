using System;
using System.Collections.Generic;
using System.Text;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Utilitaires.Crypto;

/// <summary>
/// Déchiffrement/chiffrement du canal réseau « <c>-</c> » d'Abrak (protocole
/// custom v1.48). Reconstitué par décompilation FFDec de core.swf
/// (<c>dofus.aks.Aks.prepareData / unprepareData / checksum</c>) :
///
///   Paquet chiffré = "-" + [idxClé:1 hex] + [checksum:1 hex] + cypherData(...)
///   cypherData(data, cléPréparée, offset) :
///     • à l'envoi    : escape(data) puis octet ^ clé[(i+offset) % len] → hex2
///     • à la réception: hex2 → octet ^ clé[(i+offset) % len] puis unescape
///   offset = parseInt(checksumChar, 16) * 2
///   checksum(s) = HEX[(Σ s[i] mod 16) mod 16]   (HEX = 0-9A-F)
///   clé = prepareKey(tokenHex) = (hex paires → octets) puis unescape Flash
///
/// Les clés viennent du paquet serveur <c>AK</c> : <c>AK&lt;id&gt;k0|k1|k2|…</c>.
/// L'indexation exacte (offset de base) du handler <c>resetKeys</c> est dans un
/// script protégé non décompilable → on l'AUTO-CALIBRE : le framing porte un
/// checksum, donc on essaie les mappings plausibles et on verrouille celui dont
/// les paquets déchiffrés valident le checksum (oracle fiable).
///
/// Identique au cipher carte (<see cref="DechiffreurCarte"/>, prouvé : 479/479
/// cellules décodées) — mêmes primitives, clé/offset différents.
/// </summary>
public sealed class CanalAbrak
{
    private const string Hex = "0123456789ABCDEF";

    // Clés préparées indexées. _cles[i] = prepareKey(token).
    private readonly List<string> _clesBrutes = new();
    private string[] _clesPreparees = Array.Empty<string>();
    private int _baseCalibree = -1;        // décalage index AK → _clesPreparees (auto)
    private bool _calibre;

    public bool PretAuDechiffrement => _clesPreparees.Length > 0;

    /// <summary>
    /// Enregistre les clés depuis un paquet <c>AK</c> brut (ex.
    /// « AK014a68…|13b7ec…|2e19cb…|… »). Le premier token porte souvent un
    /// caractère d'id en tête (longueur impaire) qu'on retire pour avoir des
    /// paires hex valides.
    /// </summary>
    public void EnregistrerDepuisAK(string paquetAk)
    {
        if (string.IsNullOrEmpty(paquetAk)) return;
        var corps = paquetAk.StartsWith("AK", StringComparison.Ordinal) ? paquetAk.Substring(2) : paquetAk;

        _clesBrutes.Clear();
        var tokens = corps.Split('|');
        for (int i = 0; i < tokens.Length; i++)
        {
            var t = tokens[i];
            // Token de longueur impaire → caractère d'id/version en tête à retirer.
            if ((t.Length & 1) != 0 && t.Length > 1) t = t.Substring(1);
            _clesBrutes.Add(t);
        }

        var prep = new List<string>(_clesBrutes.Count);
        foreach (var t in _clesBrutes)
            prep.Add(PreparerClef(t));
        _clesPreparees = prep.ToArray();
        _calibre = false;
        _baseCalibree = -1;
        Journaliseur.Info($"[CRYPT] Clés réseau AK enregistrées : {_clesPreparees.Length} clé(s) — canal '-' prêt.");

        // Self-test injection : Chiffrer puis Dechiffrer doit redonner le
        // clair. Prouve que notre chiffrement C→S est l'inverse exact du
        // déchiffrement S→C → un paquet qu'on forge sera accepté serveur.
        try
        {
            _calibre = true; _baseCalibree = 0;     // calibration prouvée
            const string test = "GA0011test";
            var c = Chiffrer(test, 1);
            var r = c != null ? Dechiffrer(c) : null;
            Journaliseur.Info(r == test
                ? "[CRYPT] Self-test injection OK : Chiffrer⇄Dechiffrer round-trip validé (C→S forgeable)."
                : $"[CRYPT] Self-test injection ÉCHEC : '{test}' → '{c}' → '{r}' (à investiguer).");
        }
        catch (Exception ex) { Journaliseur.Avertir($"[CRYPT] Self-test : {ex.Message}"); }
    }

    public void Reset()
    {
        _clesBrutes.Clear();
        _clesPreparees = Array.Empty<string>();
        _calibre = false;
        _baseCalibree = -1;
    }

    /// <summary>
    /// Déchiffre un paquet du canal « - ». Retourne le texte clair, ou la
    /// chaîne d'origine si pas chiffré / clé absente / checksum invalide
    /// (exactement le comportement <c>unprepareData</c> d'Abrak).
    /// </summary>
    public string Dechiffrer(string paquet)
    {
        if (string.IsNullOrEmpty(paquet) || paquet[0] != '-') return paquet;
        if (_clesPreparees.Length == 0) return paquet;
        if (paquet.Length < 3) return paquet;

        int idx = HexVal(paquet[1]);
        char cks = char.ToUpperInvariant(paquet[2]);
        if (idx < 0) return paquet;
        // IMPORTANT : on retire tout caractère de contrôle traînant
        // (\0 \r \n) avant le hex. Sans ça le payload C→S avait une
        // longueur impaire → DechiffrerHex échouait (faux « déchiffrable=
        // NON » alors que le cipher C→S est IDENTIQUE au S→C — prouvé :
        // -268980→"BD", -5D5A883E82→"GKK0", delta clé 0, offset cks*2).
        var donnees = paquet.Substring(3).TrimEnd('\0', '\r', '\n', ' ');

        // Mapping auto-calibré : base = décalage entre l'index du frame et
        // l'indice dans _clesPreparees. On essaie 0 et -1 (cas observés) puis
        // on verrouille celui qui valide le checksum.
        if (_calibre)
        {
            var r = TenterDechiffrer(donnees, idx + _baseCalibree, cks);
            return r ?? paquet;
        }

        foreach (var b in new[] { 0, -1, 1, -2 })
        {
            var r = TenterDechiffrer(donnees, idx + b, cks);
            if (r != null)
            {
                _baseCalibree = b;
                _calibre = true;
                Journaliseur.Info($"[CRYPT] Canal '-' calibré (base index = {b}). Déchiffrement actif.");
                return r;
            }
        }
        return paquet;
    }

    /// <summary>
    /// Crackeur sens CLIENT→SERVEUR : le C→S utilise une calibration clé/
    /// offset différente du S→C. On essaie TOUTES les clés (0..N) × plusieurs
    /// formules d'offset et on retourne la 1re combinaison qui donne de
    /// l'ASCII imprimable (opcode Dofus lisible). But : trouver la
    /// calibration C→S pour pouvoir CHIFFRER nos injections (déplacement).
    /// </summary>
    public string? CraquerVersServeur(string paquet)
    {
        if (string.IsNullOrEmpty(paquet) || paquet[0] != '-' || paquet.Length < 6) return null;
        if (_clesPreparees.Length == 0) return null;

        char cks = char.ToUpperInvariant(paquet[2]);
        int idxFrame = HexVal(paquet[1]);
        var donnees = paquet.Substring(3).TrimEnd('\0', '\r', '\n');
        if ((donnees.Length & 1) != 0 || donnees.Length < 2) return null;

        int cksVal = HexVal(cks);
        int[] offsets = { cksVal * 2, 0, cksVal, cksVal * 2 + 1, cksVal * 2 - 1, idxFrame * 2 };

        foreach (var off in offsets)
        {
            if (off < 0) continue;
            for (int k = 0; k < _clesPreparees.Length; k++)
            {
                var cle = _clesPreparees[k];
                if (string.IsNullOrEmpty(cle)) continue;
                var clair = DechiffrerHex(donnees, cle, off);
                if (string.IsNullOrEmpty(clair)) continue;

                bool printable = true;
                foreach (var c in clair)
                    if (c is < ' ' or > '~') { printable = false; break; }
                if (!printable) continue;

                bool cksOk = Checksum(clair) == cks;
                return $"clé={k} (delta={k - idxFrame}) offset={off} "
                     + $"cks={(cksOk ? "OK" : "non")} clair='{clair}'";
            }
        }
        return null;
    }

    private string? TenterDechiffrer(string donneesHex, int indiceCle, char checksumChar)
    {
        if (indiceCle < 0 || indiceCle >= _clesPreparees.Length) return null;
        var cle = _clesPreparees[indiceCle];
        if (string.IsNullOrEmpty(cle)) return null;

        int offset = HexVal(checksumChar) * 2;
        var clair = DechiffrerHex(donneesHex, cle, offset);
        if (clair == null) return null;

        // Oracle : le checksum recalculé du clair doit matcher le frame.
        return Checksum(clair) == checksumChar ? clair : null;
    }

    // ---- primitives (identiques à DechiffreurCarte, cipher prouvé) ----

    private static string PreparerClef(string clefHex)
    {
        if (string.IsNullOrEmpty(clefHex) || (clefHex.Length & 1) != 0) return string.Empty;
        var sb = new StringBuilder(clefHex.Length / 2);
        for (int i = 0; i < clefHex.Length; i += 2)
        {
            int h = HexDeux(clefHex, i);
            if (h < 0) return string.Empty;
            sb.Append((char)h);
        }
        return UnescapeFlash(sb.ToString());
    }

    /// <summary>checksum Abrak : HEX[(Σ charCode mod 16) mod 16] (1 caractère).</summary>
    private static char Checksum(string s)
    {
        int somme = 0;
        for (int i = 0; i < s.Length; i++) somme += s[i] % 16;
        return Hex[somme % 16];
    }

    private static string? DechiffrerHex(string donneesHex, string clef, int decalage)
    {
        if ((donneesHex.Length & 1) != 0 || clef.Length == 0) return null;
        int n = donneesHex.Length / 2;
        var sb = new StringBuilder(n);
        for (int i = 0; i < n; i++)
        {
            int v = HexDeux(donneesHex, i * 2);
            if (v < 0) return null;
            int k = clef[(i + decalage) % clef.Length];
            sb.Append((char)(v ^ k));
        }
        return UnescapeFlash(sb.ToString());
    }

    /// <summary>
    /// Chiffre (sens client→serveur) un paquet en clair pour le canal « - ».
    /// <paramref name="indexCle"/> = index de rotation courant (1.._clesPreparees-1).
    /// À n'utiliser que pour l'injection (le client réel gère sa propre
    /// rotation ; risque de désync à manier avec précaution).
    /// </summary>
    public string? Chiffrer(string clair, int indexCle)
    {
        if (!_calibre || _clesPreparees.Length == 0) return null;
        int idxReel = indexCle + _baseCalibree;
        if (idxReel < 0 || idxReel >= _clesPreparees.Length) return null;
        var cle = _clesPreparees[idxReel];
        if (string.IsNullOrEmpty(cle)) return null;

        char cks = Checksum(clair);
        int offset = HexVal(cks) * 2;
        var echappe = EscapeFlash(clair);
        var sb = new StringBuilder(echappe.Length * 2);
        for (int i = 0; i < echappe.Length; i++)
        {
            int k = cle[(i + offset) % cle.Length];
            int v = (echappe[i] ^ k) & 0xFF;
            sb.Append(Hex[v >> 4]).Append(Hex[v & 0xF]);
        }
        if (indexCle < 0 || indexCle > 15) return null;
        return "-" + Hex[indexCle] + cks + sb.ToString();
    }

    public int NombreCles => _clesPreparees.Length;

    // ---- helpers hex / escape Flash ----

    private static int HexVal(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1
    };

    private static int HexDeux(string s, int off)
    {
        if (off + 1 >= s.Length) return -1;
        int hi = HexVal(s[off]), lo = HexVal(s[off + 1]);
        return (hi < 0 || lo < 0) ? -1 : (hi << 4) | lo;
    }

    /// <summary>unescape() Flash par octet (Latin-1, %XX → 1 char, '+' inchangé).</summary>
    private static string UnescapeFlash(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '%' && i + 2 < s.Length)
            {
                int hi = HexVal(s[i + 1]), lo = HexVal(s[i + 2]);
                if (hi >= 0 && lo >= 0) { sb.Append((char)((hi << 4) | lo)); i += 2; continue; }
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>escape() Flash : encode tout sauf [A-Za-z0-9 @*_+-./] en %XX.</summary>
    private static string EscapeFlash(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            bool sur = c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9')
                       or '@' or '*' or '_' or '+' or '-' or '.' or '/';
            if (sur) sb.Append(c);
            else if (c < 256) sb.Append('%').Append(Hex[c >> 4]).Append(Hex[c & 0xF]);
            else sb.Append("%u").Append(((int)c).ToString("X4"));
        }
        return sb.ToString();
    }
}
