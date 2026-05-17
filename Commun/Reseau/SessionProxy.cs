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
    private readonly object _verrouChiffrement = new();
    private EtatChiffrement _etatChiffrement = EtatChiffrement.Inactif;

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

    public async Task EnvoyerAuServeurAsync(string message, CancellationToken ct = default)
    {
        var octets = EncoderPaquet(ChiffrerSiNecessaire(message, DirectionPaquet.VersServeur));
        await _coteServeur.GetStream().WriteAsync(octets, ct).ConfigureAwait(false);
        Journaliseur.Debogue($"[INJ ->SRV] {message}");
    }

    public async Task EnvoyerAuClientAsync(string message, CancellationToken ct = default)
    {
        var octets = EncoderPaquet(ChiffrerSiNecessaire(message, DirectionPaquet.VersClient));
        await _coteClient.GetStream().WriteAsync(octets, ct).ConfigureAwait(false);
        Journaliseur.Debogue($"[INJ ->CLT] {message}");
    }

    private static byte[] EncoderPaquet(string message)
    {
        var brut = Encoding.UTF8.GetBytes(message);
        var avecTerminaison = new byte[brut.Length + 1];
        Buffer.BlockCopy(brut, 0, avecTerminaison, 0, brut.Length);
        avecTerminaison[^1] = 0x00;
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
                    await fluxDestination.WriteAsync(octetsSortie, ct).ConfigureAwait(false);
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
        if (direction == DirectionPaquet.VersServeur
            && brut.StartsWith("<policy-file-request", StringComparison.Ordinal))
        {
            Journaliseur.Info("[POLICY] Requete Flash policy-file-request recue, reponse locale.");
            _ = _coteClient.GetStream().WriteAsync(EncoderPaquet(PolicyResponse), _annulation.Token).AsTask();
            return null;
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
        "GS", "GE", "GDM", "GJ", "GP", "GT", "GC"
    };

    private void EmettrePaquet(string contenu, DirectionPaquet direction)
    {
        var paquet = new PaquetBrut(direction, contenu);
        Journaliseur.Trace(paquet.ToString());

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
