using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BotDofus.Utilitaires.Crypto;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Commun.Reseau;

/// <summary>
/// Active MITM session between a Dofus client and a remote server.
/// It relays packets, exposes clear packets to the dispatcher, and handles
/// Hystoria CRYPTS encryption after the game-server handshake.
/// </summary>
public sealed class SessionProxy : IDisposable
{
    private enum EtatChiffrement
    {
        Inactif,
        HandshakeEnCours,
        Actif
    }

    private const string PolicyResponse =
        "<?xml version=\"1.0\"?><cross-domain-policy><site-control permitted-cross-domain-policies=\"all\"/><allow-access-from domain=\"*\" to-ports=\"*\"/></cross-domain-policy>";

    private readonly TcpClient _coteClient;
    private readonly TcpClient _coteServeur;
    private readonly ConfigReseau _config;
    private readonly CancellationTokenSource _annulation = new();

    private readonly StringBuilder _tamponVersClient = new();
    private readonly StringBuilder _tamponVersServeur = new();
    private readonly HystoriaCipher _cipherVersServeur = new();
    private readonly HystoriaCipher _cipherVersClient = new();
    // Canal chiffré Abrak v1.48 (paquets « - ») — déchiffrement lecture seule
    // pour que les parsers voient enfin entités/positions overworld.
    private readonly BotDofus.Utilitaires.Crypto.CanalAbrak _canalAbrak = new();
    private readonly object _verrouChiffrement = new();
    private EtatChiffrement _etatChiffrement = EtatChiffrement.Inactif;

    // Tâche #6 : observation des paquets « - » CLIENT→SERVEUR. Décisif pour
    // savoir si l'injection chiffrée est faisable : le client envoie-t-il du
    // « - » ? quel index de clé (rotation) ? y a-t-il une signature Shield
    // (octet 0xF9 = 'ù') autour du payload ? On loggue les N premiers.
    private int _obsClientMinus;
    private const int ObsClientMinusMax = 12;
    private int _obsBrutCs;
    private const int ObsBrutCsMax = 30;
    private bool _aiCapture;

    // === Proxy RE-CHIFFRANT C→S (modèle SynFus) ===
    // Le proxy devient SEUL maître de la rotation « - » côté serveur : il
    // déchiffre chaque paquet « - » du client puis le RE-CHIFFRE avec son
    // propre compteur avant relai, injection comprise. Le serveur ne voit
    // donc qu'UNE séquence continue → plus de désync → on peut injecter
    // (clic map / farm) avec le Dofus.exe ouvert en parallèle, exactement
    // comme SynFus. _idxCs<0 = pas encore amorcé (on s'aligne sur l'index
    // du 1er paquet client pour rester identique tant qu'on n'injecte pas).
    private readonly object _verrouCs = new();
    private int _idxCs = -1;
    private int _dernierIdxClient = -1;   // dernier index d'en-tête client vu (détection reset réel)
    private bool _akRenegocie;            // un AK S→C est réapparu APRÈS amorce = vraie réinit rotation serveur (preuve positive, lève l'ambiguïté wrap 15→1)
    private int _obsReenc;
    private const int ObsReencMax = 40;

    public event EventHandler<EvenementPaquetRecu>? PaquetRecu;
    public event EventHandler? SessionTerminee;

    /// <summary>
    /// Optional hook to rewrite packets in transit. The packet passed to this
    /// delegate is clear text. Return null to keep it unchanged, string.Empty
    /// to drop it, or another string to forward a replacement.
    /// </summary>
    public Func<string, DirectionPaquet, string?>? ModificateurPaquet { get; set; }

    public bool Active { get; private set; }

    public bool ChiffrementActif
    {
        get
        {
            lock (_verrouChiffrement)
            {
                return _etatChiffrement == EtatChiffrement.Actif;
            }
        }
    }

    public SessionProxy(TcpClient clientAccepte, TcpClient connexionVersServeur, ConfigReseau config)
    {
        _coteClient = clientAccepte;
        _coteServeur = connexionVersServeur;
        _config = config;
    }

    public void Demarrer()
    {
        Active = true;
        Journaliseur.Info($"Session proxy demarree {_coteClient.Client.RemoteEndPoint} <-> {_coteServeur.Client.RemoteEndPoint}");

        _ = Task.Run(() => BoucleRelaiAsync(
            source: _coteClient,
            destination: _coteServeur,
            direction: DirectionPaquet.VersServeur,
            _annulation.Token));

        _ = Task.Run(() => BoucleRelaiAsync(
            source: _coteServeur,
            destination: _coteClient,
            direction: DirectionPaquet.VersClient,
            _annulation.Token));
    }

    /// <summary>
    /// Envoie un paquet C→S. Retourne <c>false</c> si l'écriture a échoué
    /// (socket fermée / session morte) : l'appelant (ApiBot) doit alors
    /// stopper ses boucles auto au lieu de spammer dans un socket disposé.
    /// </summary>
    public async Task<bool> EnvoyerAuServeurAsync(string message, CancellationToken ct = default)
    {
        // Modèle SynFus : le proxy est SEUL maître de la rotation « - »
        // côté serveur (il re-chiffre tout le flux C→S, cf. relai plus bas).
        // L'injection prend donc simplement le PROCHAIN index du compteur
        // proxy → aucune désync, même avec le Dofus.exe ouvert en parallèle.
        if (BotDofus.Commun.Reseau.ClientAutonomeAbrak.DoitEtreChiffre(message)
            && _canalAbrak.PretAuDechiffrement)
        {
            bool ok = EnvoyerCsVersServeur(message, null, injecte: true);
            Journaliseur.Debogue(ok
                ? $"[INJ ->SRV '-' réenc] {message}"
                : $"[INJ ->SRV '-' réenc] ÉCHEC '{message}'");
            return ok;
        }

        // Injection CLAIRE (GR1/GT/Gt combat, GI…) : DOIT passer par le même
        // verrou que le re-chiffrant, sinon écriture concurrente avec le relai
        // sur le socket serveur → trame entrelacée → kick (cause de la déco
        // constatée en combat quand l'auto-combat injecte GR1/GT). Écriture
        // synchrone sous _verrouCs = atomique et sérialisée avec tout le C→S.
        try
        {
            var octets = EncoderPaquet(ChiffrerSiNecessaire(message, DirectionPaquet.VersServeur));
            lock (_verrouCs)
            {
                _coteServeur.GetStream().Write(octets, 0, octets.Length);
            }
            Journaliseur.Debogue($"[INJ ->SRV] {message}");
            return true;
        }
        catch (Exception ex) when (ex is ObjectDisposedException
            or System.IO.IOException or System.Net.Sockets.SocketException
            or InvalidOperationException)
        {
            Journaliseur.Debogue($"[INJ ->SRV] ÉCHEC '{message}' : {ex.GetType().Name}");
            return false;
        }
    }

    /// <summary>
    /// Émetteur UNIQUE et SÉRIALISÉ du canal « - » vers le serveur (modèle
    /// SynFus). Tous les paquets « - » C→S — relayés (re-chiffrés depuis le
    /// client) comme injectés — passent ici sous verrou : le compteur de
    /// rotation reste strictement monotone et les écritures wire sont
    /// atomiques → le serveur ne voit qu'une séquence cohérente.
    /// <paramref name="idxClientObserve"/> = index lu dans l'en-tête du
    /// paquet client (sert UNIQUEMENT à amorcer le compteur sur la 1re
    /// trame, pour rester bit-compatible tant qu'on n'a pas injecté).
    /// </summary>
    private bool EnvoyerCsVersServeur(string clair, int? idxClientObserve, bool injecte)
    {
        lock (_verrouCs)
        {
            int n = Math.Max(2, _canalAbrak.NombreCles);
            int idxClient = idxClientObserve ?? -1;

            // RESET RÉEL du compteur client (vraie réinit serveur) : son
            // index saute en arrière vers le bas (≤2) depuis une valeur
            // moyenne, SANS être le wrap naturel 15→1. Dans ce cas le
            // serveur a aussi réinitialisé → on ré-amorce sur l'index
            // client (on abandonne le décalage d'injection, légitime ici).
            // Sinon (index continu, même après GC1) on garde le compteur
            // monotone — droper le décalage casserait la séquence serveur.
            // Saut arrière de l'index client (valeur moyenne → ≤2) = candidat
            // « vraie réinit serveur ». Deux cas :
            //  • franc (dernier idx ≠ 15) : impossible que ce soit le wrap
            //    naturel 15→1 → vraie réinit. INCHANGÉ (zéro régression).
            //  • ambigu (dernier idx = 15) : 15→1 peut être le wrap naturel OU
            //    une réinit tombée pile sur 15. On ne tranche QUE sur preuve
            //    positive (_akRenegocie : un AK S→C a été rejoué) — sinon on
            //    considère wrap naturel et on garde le compteur monotone.
            bool sautArriere = !injecte
                && idxClient is >= 1 and <= 2
                && _dernierIdxClient >= 4;
            bool resetReel =
                sautArriere && (_dernierIdxClient != n - 1 || _akRenegocie);

            if (_idxCs < 0 || resetReel)
            {
                _idxCs = (idxClient is >= 1 and <= 15) ? idxClient : 1;
                if (resetReel)
                    Journaliseur.Info($"[REENC C→S] reset rotation détecté "
                        + $"(idxClient {_dernierIdxClient}→{idxClient}, "
                        + $"akRenégocié={_akRenegocie}) → ré-amorce idx={_idxCs}");
            }
            else
            {
                // Compteur monotone : AVANCE pour TOUT paquet (relayé OU
                // injecté) hors amorce/reset. C'est ce qui garde la séquence
                // serveur cohérente — ne JAMAIS retirer cet incrément.
                _idxCs++;
                if (_idxCs > n - 1) _idxCs = 1;
            }
            // Le signal AK n'est consommé que par un paquet CLIENT relayé
            // (qu'il ait ou non déclenché le reset) : un paquet injecté ne
            // doit pas l'éteindre, sinon il disparaît avant le vrai paquet
            // client post-réinit → faux négatif réintroduit. Une fois vu par
            // un paquet relayé, on le désarme pour éviter un reset fantôme.
            if (!injecte) _akRenegocie = false;
            if (!injecte && idxClient >= 0) _dernierIdxClient = idxClient;

            var chiffre = _canalAbrak.Chiffrer(clair, _idxCs);
            if (chiffre is null)
            {
                Journaliseur.Avertir($"[REENC C→S] échec chiffrement idx={_idxCs} "
                    + $"clair='{clair[..Math.Min(clair.Length, 24)]}'");
                return false;
            }

            // Trace décisive (N premières) : séquence EXACTE émise vers le
            // serveur. Si kick → ce log montre à quel idx/clair, et si
            // idxProxy s'écarte de idxClient (désync wrap / injection).
            if (_obsReenc < ObsReencMax)
            {
                _obsReenc++;
                string dec = injecte
                    ? "INJECTÉ"
                    : $"relayé idxClient={idxClient}"
                      + (idxClient >= 0 && idxClient != _idxCs ? " ≠proxy(décalé)" : "");
                Journaliseur.Info($"[REENC C→S] #{_obsReenc} idxProxy={_idxCs} {dec} "
                    + $"clair='{clair[..Math.Min(clair.Length, 32)]}'");
            }

            try
            {
                var octets = Encoding.UTF8.GetBytes(chiffre + "\n\0");
                _coteServeur.GetStream().Write(octets, 0, octets.Length);
                return true;
            }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[REENC C→S] écriture KO : {ex.GetType().Name}");
                return false;
            }
        }
    }

    public async Task EnvoyerAuClientAsync(string message, CancellationToken ct = default)
    {
        var octets = EncoderPaquet(ChiffrerSiNecessaire(message, DirectionPaquet.VersClient));
        await _coteClient.GetStream().WriteAsync(octets, ct).ConfigureAwait(false);
        Journaliseur.Debogue($"[INJ ->CLT] {message}");
    }

    /// <summary>Valeur hex d'un caractère (0-9 a-f A-F), ou -1.</summary>
    private static int HexValCar(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1
    };

    /// <summary>
    /// Encode un paquet Dofus 1.29 pour le wire.
    /// Terminateur par défaut : <c>"\n\0"</c> — c'est ce que le client réel
    /// et dyshay/SynFus envoient (cf. dyshay/Comun/Network/TCPClient.cs
    /// ligne 124 : <c>packet += "\n\x00"</c>). Sans le <c>\n</c>, le serveur
    /// Hystoria IGNORE silencieusement les paquets en clair injectés par le
    /// bot (Gt, GR1, GT) — symptôme observé : tour combat finit par timeout
    /// 45 s au lieu de partir immédiatement après <c>Gt</c>.
    /// Le canal cipher '-' ajoutait déjà <c>\n\0</c> (cf. EnvoyerCsVersServeur),
    /// d'où l'asymétrie : cipher OK, clair muet.
    /// <paramref name="ajouterSautLigne"/>=false uniquement pour la réponse
    /// Flash policy-file-request (qui attend strictement <c>\0</c>).
    /// </summary>
    private static byte[] EncoderPaquet(string message, bool ajouterSautLigne = true)
    {
        var brut = Encoding.UTF8.GetBytes(message);
        int extra = ajouterSautLigne ? 2 : 1;
        var avecTerminaison = new byte[brut.Length + extra];
        Buffer.BlockCopy(brut, 0, avecTerminaison, 0, brut.Length);
        int p = brut.Length;
        if (ajouterSautLigne) avecTerminaison[p++] = (byte)'\n';
        avecTerminaison[p] = 0x00;
        return avecTerminaison;
    }

    private async Task BoucleRelaiAsync(TcpClient source, TcpClient destination, DirectionPaquet direction, CancellationToken ct)
    {
        var tamponOctets = new byte[_config.TailleTamponOctets];
        var fluxSource = source.GetStream();
        var fluxDestination = destination.GetStream();
        var tamponTexte = direction == DirectionPaquet.VersClient ? _tamponVersClient : _tamponVersServeur;
        var sortie = new StringBuilder();

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var lus = await fluxSource.ReadAsync(tamponOctets, ct).ConfigureAwait(false);
                if (lus <= 0)
                {
                    break;
                }

                tamponTexte.Append(Encoding.UTF8.GetString(tamponOctets, 0, lus));
                sortie.Clear();
                ExtraireEtTraiterPaquets(tamponTexte, direction, sortie);

                if (sortie.Length > 0)
                {
                    var octetsSortie = Encoding.UTF8.GetBytes(sortie.ToString());
                    if (direction == DirectionPaquet.VersServeur)
                    {
                        // Sérialisé avec le re-chiffrant et l'injection : toute
                        // écriture vers le socket serveur passe par _verrouCs,
                        // sinon entrelacement de trames → kick.
                        lock (_verrouCs)
                        {
                            fluxDestination.Write(octetsSortie, 0, octetsSortie.Length);
                        }
                    }
                    else
                    {
                        await fluxDestination.WriteAsync(octetsSortie, ct).ConfigureAwait(false);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"Boucle relai {direction} terminee : {ex.GetType().Name} - {ex.Message}");
        }
        finally
        {
            Arreter();
        }
    }

    private void ExtraireEtTraiterPaquets(StringBuilder tampon, DirectionPaquet direction, StringBuilder sortie)
    {
        var contenu = tampon.ToString();
        var debut = 0;

        for (var i = 0; i < contenu.Length; i++)
        {
            if (contenu[i] != '\0')
            {
                continue;
            }

            // IMPORTANT : on NE STRIPPE PAS les \r\n trailing. Le protocole Dofus utilise \n
            // comme SÉPARATEUR INTERNE dans le paquet d'auth ("login\n#hashedpwd[\n]"). Un
            // TrimEnd('\n') casserait silencieusement le mot de passe : le serveur reçoit
            // un paquet incomplet et drop l'auth sans erreur (symptôme : keep-alives Af et
            // jamais de Af0|0|0|1|-1 en réponse). Cf. session debug 2026-04-27 + SynFus.
            var brut = contenu.Substring(debut, i - debut);
            debut = i + 1;

            if (brut.Length == 0)
            {
                continue;
            }

            var aForwarder = TraiterPaquet(brut, direction);
            if (aForwarder is null)
            {
                continue;
            }

            sortie.Append(aForwarder);
            sortie.Append('\0');
        }

        tampon.Clear();
        if (debut < contenu.Length)
        {
            tampon.Append(contenu, debut, contenu.Length - debut);
        }
    }

    private string? TraiterPaquet(string brut, DirectionPaquet direction)
    {
        // NB : le resync rotation « - » N'EST PLUS déclenché par GC1. Le
        // client ne réinitialise PAS toujours son compteur après combat
        // (constaté 13:05 : idxClient continu 2→12). Re-seeder sur GC1
        // droppait le décalage d'injection (+N paquets injectés) → le
        // serveur attendait idx+N, on envoyait idx → désync → kick. Le
        // resync se fait désormais UNIQUEMENT sur saut arrière RÉEL de
        // l'index client (vraie réinit), géré dans EnvoyerCsVersServeur.

        // === Capture aks_identity (Ai) du vrai client → persistée pour le
        // client autonome. La valeur est STABLE (fingerprint machine/compte,
        // identique à chaque session) donc rejouable hors client officiel. ===
        if (direction == DirectionPaquet.VersServeur
            && brut.StartsWith("Ai", StringComparison.Ordinal)
            && brut.Length > 40 && !_aiCapture)
        {
            _aiCapture = true;
            try
            {
                var chemin = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "aks_identity.txt");
                System.IO.File.WriteAllText(chemin, brut.Substring(2));
                Journaliseur.Info(
                    $"[AUTO] aks_identity capturé ({brut.Length - 2} c.) → {chemin}. "
                    + "Le client autonome pourra l'utiliser pour s'auth sans Shield.");
            }
            catch (Exception ex) { Journaliseur.Avertir($"[AUTO] Sauvegarde Ai : {ex.Message}"); }
        }

        // === Tâche #6 : trace BRUTE de TOUT paquet C→S (30 premiers) ===
        // Décisif : que voit-on quand le client bouge ? un '-' ? du binaire
        // Shield (0xF9) ? rien (paquet hors-proxy) ? Ça tranche la faisabilité
        // de l'injection de déplacement.
        if (direction == DirectionPaquet.VersServeur
            && _obsBrutCs < ObsBrutCsMax
            && !string.IsNullOrEmpty(brut))
        {
            _obsBrutCs++;
            int b0 = brut[0];
            bool f9 = brut.IndexOf('ù') >= 0;
            bool ascii = true;
            foreach (var c in brut) if (c < 32 || c > 126) { ascii = false; break; }
            string ap = brut.Length <= 24 ? brut : brut.Substring(0, 24);
            // échappe le non-imprimable pour la lisibilité console
            var sb = new StringBuilder();
            foreach (var c in ap) sb.Append(c is >= ' ' and <= '~' ? c : '·');
            Journaliseur.Info(
                $"[OBS-BRUT C→S] #{_obsBrutCs} len={brut.Length} b0=0x{b0:X2}'{(b0 is >= 32 and <= 126 ? (char)b0 : '·')}' "
                + $"ascii={(ascii ? "OUI" : "non")} Shield0xF9={(f9 ? "OUI" : "non")} aperçu='{sb}'");
        }

        if (direction == DirectionPaquet.VersServeur
            && brut.StartsWith("<policy-file-request", StringComparison.Ordinal))
        {
            Journaliseur.Info("[POLICY] Requete Flash policy-file-request recue, reponse locale.");
            _ = _coteClient.GetStream().WriteAsync(EncoderPaquet(PolicyResponse, ajouterSautLigne: false), _annulation.Token).AsTask();
            return null;
        }

        // === Abrak v1.48 : clés réseau (AK) + canal chiffré « - » ===
        // On capture les clés AK, et on déchiffre les paquets « - » UNIQUEMENT
        // pour l'observation/parsing : on relaie TOUJOURS l'original chiffré
        // intact au destinataire (intégrité MITM). Déverrouille les entités/
        // positions overworld qui transitent chiffrées.
        if (direction == DirectionPaquet.VersClient
            && brut.StartsWith("AK", StringComparison.Ordinal) && brut.Length > 6
            && brut.IndexOf('|') > 0)
        {
            try { _canalAbrak.EnregistrerDepuisAK(brut); } catch (Exception ex)
            { Journaliseur.Avertir($"[CRYPT] AK : {ex.Message}"); }
            // Un AK qui réapparaît APRÈS amorce (_idxCs déjà ≥ 0) = le serveur
            // a renégocié/réinitialisé la rotation '-' (vraie réinit). C'est la
            // preuve POSITIVE qui lève l'ambiguïté du wrap 15→1 dans resetReel
            // (sinon faux négatif : réinit ratée quand dernier idx client = 15
            // → proxy reste +1 / serveur remis à zéro → décrypt corrompu →
            // GA001 tronqué à 1 case → boucle « saut sans progrès »).
            lock (_verrouCs)
            {
                if (_idxCs >= 0)
                {
                    _akRenegocie = true;
                    Journaliseur.Info("[REENC C→S] AK renégocié S→C après amorce "
                        + "→ vraie réinit rotation serveur armée (resetReel au prochain paquet client).");
                }
            }
            // continue le traitement normal (AK est relayé/loggué via la suite)
        }

        // === Tâche #6 : observation « - » CLIENT→SERVEUR (diagnostic décisif) ===
        if (direction == DirectionPaquet.VersServeur
            && brut.Length > 2 && brut[0] == '-'
            && _obsClientMinus < ObsClientMinusMax)
        {
            _obsClientMinus++;
            bool shield = brut.IndexOf('ù') >= 0;   // 0xF9 = signature Shield
            int idx = HexValCar(brut[1]);
            char cks = brut[2];
            string tete = brut.Substring(0, Math.Min(10, brut.Length));
            string queue = brut.Length > 10 ? brut.Substring(brut.Length - 10) : "";
            string clairTest;
            try { clairTest = _canalAbrak.Dechiffrer(brut); } catch { clairTest = brut; }
            bool dechiffrable = clairTest != brut && clairTest.Length > 0;
            Journaliseur.Info(
                $"[OBS C→S '-'] #{_obsClientMinus} len={brut.Length} idxClé={idx} cks='{cks}' "
                + $"Shield(0xF9)={(shield ? "OUI" : "non")} déchiffrable={(dechiffrable ? "OUI" : "NON")} "
                + $"tête='{tete}' queue='{queue}'"
                + (dechiffrable ? $" → clair='{clairTest.Substring(0, Math.Min(40, clairTest.Length))}'" : ""));

            // Crackeur calibration C→S : essaie toutes les clés × offsets.
            if (!dechiffrable)
            {
                string? crack = null;
                try { crack = _canalAbrak.CraquerVersServeur(brut); } catch { }
                Journaliseur.Info(crack != null
                    ? $"[CRACK C→S] #{_obsClientMinus} TROUVÉ → {crack}"
                    : $"[CRACK C→S] #{_obsClientMinus} aucune clé/offset ne donne d'ASCII (chiffre C→S ≠ S→C ?)");
            }
        }

        if (brut.Length > 2 && brut[0] == '-' && _canalAbrak.PretAuDechiffrement)
        {
            string clairAbrak;
            try { clairAbrak = _canalAbrak.Dechiffrer(brut); }
            catch { clairAbrak = brut; }

            if (clairAbrak != brut && clairAbrak.Length > 0)
            {
                // Le clair peut contenir plusieurs messages (séparateur \n).
                foreach (var sous in clairAbrak.Split('\n'))
                {
                    var p = sous.Trim('\r', '\0');
                    if (p.Length >= 2) EmettrePaquet(p, direction);
                }

                // === Modèle SynFus : RE-CHIFFRAGE C→S ===
                // On ne relaie SURTOUT PAS l'original du client : le proxy
                // re-chiffre le clair avec SON compteur (émetteur unique).
                // 1re trame → on amorce le compteur sur l'index du client
                // (séquence bit-identique tant qu'on n'injecte pas) ; ensuite
                // chaque trame (client re-chiffrée OU injectée) avance le
                // compteur du proxy → le serveur ne voit qu'une séquence
                // continue, donc l'injection ne désync plus (Dofus.exe ouvert).
                if (direction == DirectionPaquet.VersServeur)
                {
                    int idxClient = HexValCar(brut[1]);
                    EnvoyerCsVersServeur(clairAbrak, idxClient, injecte: false);
                    return null; // déjà envoyé, re-chiffré, par l'émetteur unique
                }
            }
            else if (direction == DirectionPaquet.VersServeur)
            {
                Journaliseur.Avertir("[REENC C→S] déchiffrement KO, relais brut "
                    + $"(risque désync) : {brut[..Math.Min(brut.Length, 16)]}");
            }
            // S→C (« - » serveur→client) ou échec déchiffrement : relais inchangé.
            return brut;
        }

        if (direction == DirectionPaquet.VersServeur
            && brut.Length <= 8
            && brut.StartsWith("CRYPTS", StringComparison.Ordinal))
        {
            lock (_verrouChiffrement)
            {
                _etatChiffrement = EtatChiffrement.HandshakeEnCours;
                _cipherVersServeur.Reset();
                _cipherVersClient.Reset();
            }

            Journaliseur.Info("[CIPHER] Handshake CRYPTS detecte.");
            EmettrePaquet(brut, direction);
            return brut;
        }

        var etaitChiffre = false;
        var clair = brut;

        if (HystoriaCipher.IsCryptsPacket(brut) && ChiffrementActif)
        {
            var cipher = direction == DirectionPaquet.VersClient ? _cipherVersClient : _cipherVersServeur;
            var dechiffre = cipher.Decrypt(brut);
            if (dechiffre is null)
            {
                Journaliseur.Avertir($"[CIPHER] Dechiffrement impossible ({direction}) : {brut[..Math.Min(brut.Length, 30)]}...");
                EmettrePaquet(brut, direction);
                return brut;
            }

            etaitChiffre = true;
            clair = dechiffre;
        }

        MettreAJourEtatChiffrement(clair, direction);
        EmettrePaquet(clair, direction);

        var aForwarder = clair;
        if (ModificateurPaquet is not null)
        {
            var modifie = ModificateurPaquet(clair, direction);
            if (modifie == string.Empty)
            {
                Journaliseur.Debogue($"[MOD] Paquet supprime : {clair[..Math.Min(clair.Length, 40)]}");
                return null;
            }

            if (modifie is not null)
            {
                aForwarder = modifie;
            }
        }

        if (!etaitChiffre)
        {
            return aForwarder;
        }

        if (aForwarder == clair)
        {
            return brut;
        }

        var cipherModification = direction == DirectionPaquet.VersClient ? _cipherVersClient : _cipherVersServeur;
        return cipherModification.Encrypt(aForwarder);
    }

    private void MettreAJourEtatChiffrement(string paquetClair, DirectionPaquet direction)
    {
        if (direction != DirectionPaquet.VersClient)
        {
            return;
        }

        if (paquetClair.StartsWith("HG", StringComparison.Ordinal))
        {
            lock (_verrouChiffrement)
            {
                if (_etatChiffrement == EtatChiffrement.HandshakeEnCours)
                {
                    _etatChiffrement = EtatChiffrement.Actif;
                    Journaliseur.Info("[CIPHER] HG recu, chiffrement Hystoria actif.");
                }
            }
        }
        else if (paquetClair.StartsWith("CRYPTOK", StringComparison.Ordinal))
        {
            lock (_verrouChiffrement)
            {
                _etatChiffrement = EtatChiffrement.Actif;
                _cipherVersServeur.Reset();
                _cipherVersClient.Reset();
            }

            Journaliseur.Info("[CIPHER] CRYPTOK recu, chiffrement Hystoria actif.");
        }
        else if (paquetClair.StartsWith("CRYPTFAIL", StringComparison.Ordinal))
        {
            lock (_verrouChiffrement)
            {
                _etatChiffrement = EtatChiffrement.Inactif;
            }

            Journaliseur.Avertir("[CIPHER] CRYPTFAIL recu, chiffrement Hystoria desactive.");
        }
    }

    private static readonly string[] PrefixesDiagnostic =
    {
        "HC", "Af", "AH", "AT", "AYK", "AL", "Ad", "Ax", "As", "ASK", "AA", "AB", "AG",
        "GS", "GE", "GDM", "GJ", "GP", "GT", "GC", "GM"
    };

    // Diagnostic Abrak : on logge UNE SEULE FOIS chaque type de paquet vu
    // (préfixe), dans chaque sens. Révèle tout le vocabulaire réseau réel
    // d'Abrak (dont LE paquet qui porte les entités/acteurs map) sans spam.
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _vocabVu = new();

    private void EmettrePaquet(string contenu, DirectionPaquet direction)
    {
        var paquet = new PaquetBrut(direction, contenu);
        Journaliseur.Trace(paquet.ToString());

        if (contenu.Length >= 2)
        {
            // Clé = sens + préfixe 3 chars (ou 2 si plus court).
            var pfx = contenu.Length >= 3 ? contenu[..3] : contenu[..2];
            var cle = (direction == DirectionPaquet.VersClient ? "S>" : "C>") + pfx;
            // Les paquets de DIALOGUE PNJ (préfixe 'D' : DC/DCK/DQ/DR/DRK/DV…)
            // sont loggués À CHAQUE FOIS (pas de dédup) : il faut voir TOUTE la
            // séquence question/réponses pour reverse le protocole de dialogue.
            bool estDialogue = contenu.Length >= 2 && contenu[0] == 'D'
                && char.IsUpper(contenu[1]);
            if (estDialogue || _vocabVu.TryAdd(cle, 1))
            {
                var fleche = direction == DirectionPaquet.VersClient ? "S→C" : "C→S";
                var apercu = contenu.Length > 240 ? contenu[..240] + "…" : contenu;
                Journaliseur.Info($"[VOCAB {fleche}] {apercu}");
            }
        }

        // Log Info pour les paquets de bootstrap de session (HC challenge, file d'attente,
        // liste serveurs, redirect AYK, sélection perso, etc.) — c'est ce qu'on veut voir
        // immédiatement dans la console quand on diagnostique une connexion qui marche/pas.
        // Les paquets de gameplay (GM, BS, Im...) ne sont PAS loggés ici (Vue Sniffer s'en charge).
        if (contenu.Length >= 2)
        {
            foreach (var prefixe in PrefixesDiagnostic)
            {
                if (contenu.StartsWith(prefixe, StringComparison.Ordinal))
                {
                    var flèche = direction == DirectionPaquet.VersClient ? "S→C" : "C→S";
                    var charge = contenu.Length > 60 ? contenu[..60] + "…" : contenu;
                    Journaliseur.Info($"[PKT {flèche}] {charge}");
                    break;
                }
            }
        }

        // Défensif : un handler (parser, vue UI, script Lua) qui throw ne doit JAMAIS
        // tuer la boucle relai — sinon la session Dofus est coupée pour un simple bug
        // d'affichage. On isole chaque invocation.
        try
        {
            PaquetRecu?.Invoke(this, new EvenementPaquetRecu(paquet));
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[RELAI] Handler PaquetRecu a levé {ex.GetType().Name} (ignoré) : {ex.Message}");
        }
    }

    private string ChiffrerSiNecessaire(string message, DirectionPaquet direction)
    {
        if (!ChiffrementActif || message.StartsWith("CRYPT", StringComparison.Ordinal))
        {
            return message;
        }

        var cipher = direction == DirectionPaquet.VersServeur ? _cipherVersServeur : _cipherVersClient;
        return cipher.Encrypt(message);
    }

    public void Arreter()
    {
        if (!Active)
        {
            return;
        }

        Active = false;

        try { _annulation.Cancel(); } catch { }
        try { _coteClient.Close(); } catch { }
        try { _coteServeur.Close(); } catch { }

        Journaliseur.Info("Session proxy terminee");
        SessionTerminee?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        Arreter();
        _annulation.Dispose();
        _coteClient.Dispose();
        _coteServeur.Dispose();
    }
}
