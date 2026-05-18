using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BotDofus.Utilitaires.Crypto;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Commun.Reseau;

/// <summary>
/// Client Dofus Retro 1.48 (Abrak) AUTONOME — architecture « SynFus » :
/// on ouvre NOTRE propre socket vers le serveur et on parle le protocole
/// nous-mêmes. On ne lance JAMAIS le client Electron officiel ⇒ Shield
/// (protection côté client) n'existe pas dans l'équation. La seule couche
/// crypto est le canal « - » du serveur, déjà cassé (<see cref="CanalAbrak"/>).
///
/// Flux d'auth (reconstitué des captures live + core.swf + bot réf 1.29) :
///   1303 : reçoit HC&lt;clé&gt; → envoie "1.48.0" → "&lt;login&gt;\n#1&lt;CryptPwd&gt;" → "Af"
///        → (Ad/AH/...) → "Ax" → reçoit états → "AX&lt;serverId&gt;|1"
///        → reçoit AXK&lt;gameTicket&gt;
///   1304 : envoie "AT&lt;gameTicket&gt;" → reçoit AK&lt;16 clés&gt; (cipher '-' prêt)
///        → "Ak0" → le jeu tourne. On gère NOUS la rotation de clé en envoi
///        (on est seul sur le socket ⇒ aucune désync possible).
/// </summary>
public sealed class ClientAutonomeAbrak : IDisposable
{
    private static readonly char[] CharsetMdp =
    {
        'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p',
        'q','r','s','t','u','v','w','x','y','z',
        'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P',
        'Q','R','S','T','U','V','W','X','Y','Z',
        '0','1','2','3','4','5','6','7','8','9','-','_'
    };

    private readonly string _hote;
    private readonly int _portAuth;
    private readonly int _portJeu;
    private readonly string _login;
    private readonly string _motDePasse;
    private readonly int _idServeur;

    private readonly CanalAbrak _canal = new();
    private readonly CancellationTokenSource _cts = new();
    private TcpClient? _socket;
    private NetworkStream? _flux;
    private readonly StringBuilder _tampon = new();

    private string _cleHc = string.Empty;
    private string _aksIdentity = string.Empty;
    private string _gameTicket = string.Empty;
    private bool _phaseJeu;
    private bool _avEnvoye;
    private bool _persoSelectionne;
    private bool _enJeu;
    private int _idxEnvoi = 1;          // rotation clé '-' en envoi (1..N-1)
    private bool _disposed;

    /// <summary>Paquet en clair reçu (déjà déchiffré si canal '-').</summary>
    public event Action<string>? PaquetClair;
    /// <summary>Trace d'état lisible (UI/log).</summary>
    public event Action<string>? Etat;
    /// <summary>Auth jeu réussie : AK reçues, cipher prêt.</summary>
    public event Action? PretJeu;

    public bool EstConnecte => !_disposed && _flux != null && _socket?.Connected == true;
    /// <summary>true seulement quand le perso est réellement entré en jeu
    /// (ASK reçu / GC1 envoyé) — gate pour le farm.</summary>
    public bool EstEnJeu => _enJeu && EstConnecte;
    public bool PhaseJeu => _phaseJeu;

    public ClientAutonomeAbrak(string hote, int portAuth, int portJeu,
        string login, string motDePasse, int idServeur)
    {
        _hote = hote;
        _portAuth = portAuth;
        _portJeu = portJeu;
        _login = login;
        _motDePasse = motDePasse;
        _idServeur = idServeur;

        // aks_identity capturé d'un login réel via le proxy (valeur stable
        // machine/compte). Indispensable : le serveur gate la version dessus.
        try
        {
            var f = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "aks_identity.txt");
            if (System.IO.File.Exists(f))
                _aksIdentity = System.IO.File.ReadAllText(f).Trim();
        }
        catch { /* pas grave : on tentera sans, le log le dira */ }
    }

    /// <summary>Crypt_Password classique Dofus : "#1" + 2 chars/octet via clé HC.</summary>
    public static string CrypterMotDePasse(string motDePasse, string cleHc)
    {
        var sb = new StringBuilder("#1");
        for (int i = 0; i < motDePasse.Length; i++)
        {
            char ch = motDePasse[i];
            char k = cleHc[i % cleHc.Length];
            int hi = (ch / 16 + k) % CharsetMdp.Length;
            int lo = (ch % 16 + k) % CharsetMdp.Length;
            sb.Append(CharsetMdp[hi]).Append(CharsetMdp[lo]);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Crée un socket dont le PORT SOURCE est le port-marqueur (50303 auth /
    /// 50304 jeu). Le filtre WinDivert exclut <c>tcp.SrcPort == marqueur</c>
    /// ⇒ notre connexion N'EST PAS redirigée vers notre propre proxy : on
    /// parle DIRECT au vrai serveur. Si le port est déjà pris (proxy actif
    /// via « Lancer jeu »), fallback sans bind (on passe alors par le proxy,
    /// ce qui marche aussi en relais transparent).
    /// </summary>
    private TcpClient CreerSocketMarque(int portMarqueur)
    {
        var s = new TcpClient();
        try
        {
            s.Client.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket,
                System.Net.Sockets.SocketOptionName.ReuseAddress, true);
            s.Client.Bind(new System.Net.IPEndPoint(
                System.Net.IPAddress.Any, portMarqueur));
            Etat?.Invoke($"[AUTO] Socket lié au port-marqueur {portMarqueur} (connexion DIRECTE, sans proxy).");
        }
        catch (Exception ex)
        {
            Etat?.Invoke($"[AUTO] Port-marqueur {portMarqueur} indispo ({ex.Message}) → via proxy (OK aussi).");
            s = new TcpClient();
        }
        return s;
    }

    /// <summary>Démarre l'auth puis la boucle de réception (tâche de fond).</summary>
    public async Task DemarrerAsync()
    {
        try
        {
            Etat?.Invoke($"[AUTO] Connexion auth {_hote}:{_portAuth}…");
            _socket = CreerSocketMarque(50303);
            await _socket.ConnectAsync(_hote, _portAuth).ConfigureAwait(false);
            _flux = _socket.GetStream();
            Etat?.Invoke("[AUTO] Socket auth établi — attente HC…");
            _ = Task.Run(BoucleReceptionAsync);
        }
        catch (Exception ex)
        {
            Etat?.Invoke($"[AUTO] Échec connexion auth : {ex.Message}");
        }
    }

    private async Task BoucleReceptionAsync()
    {
        var buf = new byte[16384];
        try
        {
            while (!_cts.IsCancellationRequested && _flux != null)
            {
                int n = await _flux.ReadAsync(buf, 0, buf.Length, _cts.Token).ConfigureAwait(false);
                if (n <= 0) { Etat?.Invoke("[AUTO] Flux fermé par le serveur."); break; }
                _tampon.Append(Encoding.UTF8.GetString(buf, 0, n));
                await DecouperEtTraiterAsync().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Etat?.Invoke($"[AUTO] Réception interrompue : {ex.Message}"); }
    }

    private async Task DecouperEtTraiterAsync()
    {
        var contenu = _tampon.ToString();
        int debut = 0;
        for (int i = 0; i < contenu.Length; i++)
        {
            if (contenu[i] != '\0') continue;
            var paquet = contenu.Substring(debut, i - debut);
            debut = i + 1;
            if (paquet.Length > 0) await TraiterPaquetAsync(paquet).ConfigureAwait(false);
        }
        _tampon.Clear();
        if (debut < contenu.Length) _tampon.Append(contenu, debut, contenu.Length - debut);
    }

    private async Task TraiterPaquetAsync(string p)
    {
        // === Canal chiffré '-' : déchiffre, remonte le clair ET réinjecte
        // dans la machine à états. Sinon les BN/ALK/ASK (chiffrés après AK
        // sur le serveur de jeu) ne déclenchent jamais AV/AS/GC1 → handshake
        // jeu bloqué (cause du stall après AT observée en test). ===
        if (p.Length > 2 && p[0] == '-' && _canal.PretAuDechiffrement)
        {
            string clair;
            try { clair = _canal.Dechiffrer(p); } catch { clair = p; }
            if (clair != p)
            {
                foreach (var sous in clair.Split('\n'))
                {
                    var q = sous.Trim('\r', '\0');
                    if (q.Length >= 2)
                    {
                        PaquetClair?.Invoke(q);
                        await TraiterClairAsync(q).ConfigureAwait(false);
                    }
                }
                return;
            }
        }

        PaquetClair?.Invoke(p);
        await TraiterClairAsync(p).ConfigureAwait(false);
    }

    /// <summary>
    /// Machine à états auth + handshake jeu. Reçoit du CLAIR : soit un paquet
    /// non chiffré, soit un sous-paquet déchiffré du canal '-'.
    /// </summary>
    private async Task TraiterClairAsync(string p)
    {
        // === Machine à états d'authentification ===
        if (!_phaseJeu)
        {
            if (p.StartsWith("HC", StringComparison.Ordinal))
            {
                _cleHc = p.Substring(2);
                // Séquence EXACTE du vrai client (capture wire, vérité sol) :
                // version EN PREMIER, puis login, puis Af. PAS d'Ai ici —
                // l'Ai vient bien plus tard (après AH, avec Ap1303, avant Ax).
                // Envoyer Ai avant la version = serveur lit "Ai…" comme version
                // → AlEv1.48.0 (BAD_VERSION). C'était LE bug.
                Etat?.Invoke("[AUTO] HC reçu → version + login (Ai plus tard, comme le vrai client).");
                await EnvoyerClairAsync("1.48.0").ConfigureAwait(false);
                await EnvoyerClairAsync(_login + "\n" + CrypterMotDePasse(_motDePasse, _cleHc)).ConfigureAwait(false);
                await EnvoyerClairAsync("Af").ConfigureAwait(false);
                return;
            }
            if (p.StartsWith("AlE", StringComparison.Ordinal))
            {
                Etat?.Invoke($"[AUTO] Auth REFUSÉE : {p} "
                    + "(Ev=version, Ea/Ec=déjà connecté, Eb=ban, Ef=identifiants).");
                return;
            }
            if (p.StartsWith("AH", StringComparison.Ordinal))
            {
                // Vrai client : après AH → Ap1303, Ai<identity>, Ax.
                Etat?.Invoke("[AUTO] Liste serveurs → Ap1303 + Ai (identity rejouée) + Ax.");
                await EnvoyerClairAsync("Ap1303").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(_aksIdentity))
                    await EnvoyerClairAsync("Ai" + _aksIdentity).ConfigureAwait(false);
                else
                    Etat?.Invoke("[AUTO] ⚠ pas d'aks_identity (fais 1 « Lancer jeu » d'abord).");
                await EnvoyerClairAsync("Ax").ConfigureAwait(false);
                return;
            }
            if (p.StartsWith("Ax", StringComparison.Ordinal) && p.Length > 2)
            {
                Etat?.Invoke($"[AUTO] États serveurs → sélection serveur #{_idServeur}.");
                await EnvoyerClairAsync($"AX{_idServeur}|1").ConfigureAwait(false);
                return;
            }
            if (p.StartsWith("AX", StringComparison.Ordinal) && p.Length > 3)
            {
                // Format : AXK + "33599914auy" (header CONSTANT observé dans
                // toutes les sessions) + <vraiTicket>. Le vrai client strippe
                // "AXK33599914auy" et envoie "AT"+ticket. On gardait le header
                // → AT malformé (+11 c.) → serveur de jeu muet (pas d'AK).
                var t = p.StartsWith("AXK", StringComparison.Ordinal) ? p.Substring(3) : p.Substring(2);
                const string headerConst = "33599914auy";
                if (t.StartsWith(headerConst, StringComparison.Ordinal))
                    t = t.Substring(headerConst.Length);
                _gameTicket = t;
                Etat?.Invoke($"[AUTO] Ticket de jeu reçu ({_gameTicket.Length} c., header strippé) → bascule serveur jeu.");
                await BasculerServeurJeuAsync().ConfigureAwait(false);
                return;
            }
            return;
        }

        // === Phase jeu : handshake serveur de jeu + sélection perso ===
        // Séquence exacte reconstituée des captures live (1304) :
        //   AT → S:AK,ATK,BN → C:AV → S:AV0 → C:Agfr,1.48.0,AL,Af
        //   → S:ALK0|1|<id>;<nom>;… → C:AS<id> → S:ASK|<id>|… → C:GC1 → monde
        if (p.StartsWith("AK", StringComparison.Ordinal) && p.Length > 6 && p.IndexOf('|') > 0)
        {
            try
            {
                _canal.EnregistrerDepuisAK(p);
                _idxEnvoi = 1;
                Etat?.Invoke($"[AUTO] AK reçues : {_canal.NombreCles} clés — cipher '-' PRÊT.");
            }
            catch (Exception ex) { Etat?.Invoke($"[AUTO] AK erreur : {ex.Message}"); }
            return;
        }
        // (le vrai client n'envoie PAS de Ak0 sur 1304 — supprimé)
        if (!_avEnvoye && p == "BN")
        {
            _avEnvoye = true;
            await EnvoyerClairAsync("AV").ConfigureAwait(false);
            return;
        }
        if (p.StartsWith("AV0", StringComparison.Ordinal))
        {
            // Réplique exacte du client officiel sur le serveur de jeu :
            // Agfr → Ai(identity rejouée) → 1.48.0 → AL → Af.
            await EnvoyerClairAsync("Agfr").ConfigureAwait(false);
            if (!string.IsNullOrEmpty(_aksIdentity))
                await EnvoyerClairAsync("Ai" + _aksIdentity).ConfigureAwait(false);
            await EnvoyerClairAsync("1.48.0").ConfigureAwait(false);
            await EnvoyerClairAsync("AL").ConfigureAwait(false);
            await EnvoyerClairAsync("Af").ConfigureAwait(false);
            return;
        }
        // Liste perso : "ALK0|1|401770;Beiloddurul;…" → on sélectionne le 1er.
        if (!_persoSelectionne && p.StartsWith("AL", StringComparison.Ordinal) && p.IndexOf(';') > 0)
        {
            var parts = p.Split('|');
            var entree = parts.Length > 0 ? parts[^1] : string.Empty;
            var idTxt = entree.Split(';')[0];
            if (int.TryParse(idTxt, out var idPerso) && idPerso > 0)
            {
                _persoSelectionne = true;
                Etat?.Invoke($"[AUTO] Perso #{idPerso} → sélection (AS).");
                await EnvoyerClairAsync("AS" + idPerso).ConfigureAwait(false);
            }
            return;
        }
        if (p.StartsWith("ASK", StringComparison.Ordinal) && !_enJeu)
        {
            _enJeu = true;
            await EnvoyerClairAsync("GC1").ConfigureAwait(false);
            Etat?.Invoke("[AUTO] ✅ EN JEU sans client officiel (sans Shield) — GC1 envoyé, monde en cours de chargement.");
            PretJeu?.Invoke();
            return;
        }
    }

    private async Task BasculerServeurJeuAsync()
    {
        try
        {
            _flux?.Dispose();
            _socket?.Close();
            _tampon.Clear();
            _phaseJeu = true;

            Etat?.Invoke($"[AUTO] Connexion jeu {_hote}:{_portJeu}…");
            _socket = CreerSocketMarque(50304);
            await _socket.ConnectAsync(_hote, _portJeu).ConfigureAwait(false);
            _flux = _socket.GetStream();
            await EnvoyerClairAsync("AT" + _gameTicket).ConfigureAwait(false);
            Etat?.Invoke("[AUTO] AT envoyé — attente AK (clés cipher).");
            _ = Task.Run(BoucleReceptionAsync);
        }
        catch (Exception ex) { Etat?.Invoke($"[AUTO] Échec bascule jeu : {ex.Message}"); }
    }

    /// <summary>
    /// Whitelist de chiffrement EXACTE de core.swf <c>Aks.prepareSendPacket</c>
    /// (décompilée) : true = le paquet DOIT partir chiffré '-', false = clair.
    /// C'est ce qui distingue GR1/GT (clair) de GA/déplacement (chiffré).
    /// </summary>
    public static bool DoitEtreChiffre(string p)
    {
        if (string.IsNullOrEmpty(p)) return false;
        char c0 = p[0], c1 = p.Length > 1 ? p[1] : '\0';
        switch (c0)
        {
            case 'A': return c1 is 'A' or 'D' or 'Z';
            case 'N': return c1 is 'A' or 'R';
            case 'W' or 'e' or 'O' or 'D' or 'F' or 'K' or 'z' or 'w' or 'S' or 'B':
                return true;
            case 'G': return c1 is 'A' or 'K' or 'M';
            case 'E':
                switch (c1)
                {
                    case 'V' or 'R' or 's' or 'A' or 'K' or 'P' or 'S' or 'B' or 'Q' or 'q' or 'w':
                        return true;
                    case 'M':
                        if (p.Length > 2 && p[2] == 'O')
                            return p.Substring(3).Split('|').Length <= 3;
                        return true;
                    case 'H':
                        return p.Length > 2 && p[2] == 'B';
                    default: return false;
                }
            default: return false;
        }
    }

    /// <summary>
    /// Point d'envoi unique côté API : route automatiquement en clair ou via
    /// le canal '-' chiffré selon la whitelist. Aucun Shield (pas de client
    /// officiel) → un paquet '-' qu'on forge est accepté tel quel.
    /// </summary>
    public async Task EnvoyerAuServeurAsync(string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        if (DoitEtreChiffre(message) && _canal.PretAuDechiffrement)
            await EnvoyerChiffreAsync(message).ConfigureAwait(false);
        else
            await EnvoyerClairAsync(message).ConfigureAwait(false);
    }

    /// <summary>Envoi en clair (paquets de contrôle : GR1, GT, Ak0, auth…).</summary>
    public async Task EnvoyerClairAsync(string message)
    {
        if (_disposed || _flux == null || !EstConnecte) return;
        try
        {
            // '\n' terminateur Dofus + '\0' frame (sans le '\n' → AlEv).
            var octets = Encoding.UTF8.GetBytes(message + "\n\0");
            await _flux.WriteAsync(octets, 0, octets.Length, _cts.Token).ConfigureAwait(false);
            Journaliseur.Debogue($"[AUTO →SRV clair] {message}");
        }
        catch (ObjectDisposedException) { }
        catch (System.IO.IOException) { }
        catch (Exception ex) { Journaliseur.Debogue($"[AUTO →SRV clair] drop ({ex.GetType().Name})"); }
    }

    /// <summary>
    /// Envoi CHIFFRÉ via le canal '-' (déplacement GA001, sorts, dialogue…).
    /// Rotation de clé gérée localement : on est le seul client sur ce socket,
    /// donc aucune désync — contrairement au MITM où le vrai client tournait
    /// sa propre clé en parallèle.
    /// </summary>
    public async Task<bool> EnvoyerChiffreAsync(string clair)
    {
        if (_disposed || _flux == null || !EstConnecte || !_canal.PretAuDechiffrement) return false;
        int n = Math.Max(2, _canal.NombreCles);
        _idxEnvoi++;
        if (_idxEnvoi > n - 1) _idxEnvoi = 1;
        var chiffre = _canal.Chiffrer(clair, _idxEnvoi);
        if (chiffre == null)
        {
            Journaliseur.Avertir($"[AUTO →SRV '-'] échec chiffrement '{clair}' (idx {_idxEnvoi})");
            return false;
        }
        try
        {
            // '\n' terminateur Dofus (les '-' C→S finissent par \n) + '\0'.
            var octets = Encoding.UTF8.GetBytes(chiffre + "\n\0");
            await _flux.WriteAsync(octets, 0, octets.Length, _cts.Token).ConfigureAwait(false);
            Journaliseur.Debogue($"[AUTO →SRV '-'] idx={_idxEnvoi} clair='{clair}'");
            return true;
        }
        catch (ObjectDisposedException) { return false; }
        catch (System.IO.IOException) { return false; }
        catch (Exception ex) { Journaliseur.Debogue($"[AUTO →SRV '-'] drop ({ex.GetType().Name})"); return false; }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _cts.Cancel(); } catch { }
        try { _flux?.Dispose(); } catch { }
        try { _socket?.Close(); } catch { }
        _cts.Dispose();
    }
}
