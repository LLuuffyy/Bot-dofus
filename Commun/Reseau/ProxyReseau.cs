using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Commun.Reseau;

/// <summary>
/// Proxy MITM TCP : écoute localement, accepte le client Dofus qui s'y connecte,
/// ouvre en parallèle une connexion sortante vers le serveur de jeu distant,
/// et orchestre les deux sens via un <see cref="SessionProxy"/>.
///
/// Le client Dofus doit être redirigé vers l'adresse locale du proxy
/// (via édition de config, fichier hosts ou argument de lancement).
/// </summary>
public sealed class ProxyReseau : IDisposable
{
    private const string PolicyResponse =
        "<?xml version=\"1.0\"?><cross-domain-policy><site-control permitted-cross-domain-policies=\"all\"/><allow-access-from domain=\"*\" to-ports=\"*\"/></cross-domain-policy>";

    private readonly ConfigReseau _config;
    private readonly TcpListener _ecouteur;
    private TcpListener? _serveurPolicy;
    private CancellationTokenSource? _annulation;
    private readonly ConcurrentBag<SessionProxy> _sessions = new();

    public event EventHandler<SessionProxy>? SessionDemarree;
    public event EventHandler<EvenementPaquetRecu>? PaquetRecu;

    public bool EnEcoute { get; private set; }

    public ProxyReseau(ConfigReseau config)
    {
        _config = config;
        _ecouteur = new TcpListener(IPAddress.Parse(_config.AdresseEcouteLocale), _config.PortEcouteLocal);
    }

    public Task DemarrerAsync(CancellationToken ct = default)
    {
        if (EnEcoute) return Task.CompletedTask;

        _annulation = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            _ecouteur.Start();
        }
        catch (Exception ex)
        {
            Journaliseur.Erreur(
                $"Impossible de démarrer le proxy sur {_config.AdresseEcouteLocale}:{_config.PortEcouteLocal} " +
                $"(port déjà utilisé ? droits insuffisants ?)",
                ex);
            throw;
        }

        EnEcoute = true;
        Journaliseur.Info($"Proxy MITM en écoute sur {_config.AdresseEcouteLocale}:{_config.PortEcouteLocal} → {_config.HoteDistant}:{_config.PortDistant}");

        DemarrerPolicyServerSiNecessaire(_annulation.Token);
        return Task.Run(() => BoucleAcceptationAsync(_annulation.Token), _annulation.Token);
    }

    private void DemarrerPolicyServerSiNecessaire(CancellationToken ct)
    {
        try
        {
            _serveurPolicy = new TcpListener(IPAddress.Any, 843);
            _serveurPolicy.Start();
            Journaliseur.Info("Policy server Flash en ecoute sur 0.0.0.0:843");
            _ = Task.Run(() => BouclePolicyAsync(_serveurPolicy, ct), ct);
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"Policy server 843 indisponible : {ex.Message}");
        }
    }

    private static async Task BouclePolicyAsync(TcpListener serveur, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient? client = null;
            try
            {
                client = await serveur.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                var reponse = Encoding.UTF8.GetBytes(PolicyResponse + '\0');
                await client.GetStream().WriteAsync(reponse, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
            }
            finally
            {
                try { client?.Close(); } catch { }
            }
        }
    }

    private async Task BoucleAcceptationAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var clientDofus = await _ecouteur.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                Journaliseur.Info($"Client Dofus connecté depuis {clientDofus.Client.RemoteEndPoint}");

                _ = Task.Run(() => GererNouveauClientAsync(clientDofus, ct), ct);
            }
        }
        catch (OperationCanceledException) { /* arrêt demandé */ }
        catch (Exception ex)
        {
            Journaliseur.Erreur("Boucle d'acceptation interrompue", ex);
        }
    }

    private async Task GererNouveauClientAsync(TcpClient clientDofus, CancellationToken ct)
    {
        TcpClient? connexionServeur = null;
        try
        {
            connexionServeur = new TcpClient();
            if (_config.PortSourceMarqueur > 0)
            {
                // Mode WinDivert : on lie la socket sortante à un port source fixe
                // pour que le redirecteur exclue NOTRE connexion du filtre (anti-boucle).
                try
                {
                    connexionServeur.Client.SetSocketOption(
                        System.Net.Sockets.SocketOptionLevel.Socket,
                        System.Net.Sockets.SocketOptionName.ReuseAddress, true);
                    connexionServeur.Client.Bind(
                        new System.Net.IPEndPoint(System.Net.IPAddress.Any, _config.PortSourceMarqueur));
                }
                catch (Exception exBind)
                {
                    Journaliseur.Avertir($"[WD] Bind port marqueur {_config.PortSourceMarqueur} échoué : {exBind.Message}");
                }
            }
            await connexionServeur.ConnectAsync(_config.HoteDistant, _config.PortDistant, ct).ConfigureAwait(false);
            Journaliseur.Info($"Connexion sortante établie vers {_config.HoteDistant}:{_config.PortDistant}"
                + (_config.PortSourceMarqueur > 0 ? $" (src marqueur :{_config.PortSourceMarqueur})" : ""));

            var session = new SessionProxy(clientDofus, connexionServeur, _config);
            session.PaquetRecu += (_, ev) => PaquetRecu?.Invoke(this, ev);
            _sessions.Add(session);

            SessionDemarree?.Invoke(this, session);
            session.Demarrer();
        }
        catch (Exception ex)
        {
            Journaliseur.Erreur("Impossible d'établir le pont MITM", ex);
            try { clientDofus.Close(); } catch { /* ignoré */ }
            try { connexionServeur?.Close(); } catch { /* ignoré */ }
        }
    }

    public void Arreter()
    {
        if (!EnEcoute) return;
        EnEcoute = false;

        try { _annulation?.Cancel(); } catch { /* ignoré */ }
        try { _ecouteur.Stop(); } catch { /* ignoré */ }

        try { _serveurPolicy?.Stop(); } catch { }

        foreach (var session in _sessions)
        {
            try { session.Arreter(); } catch { /* ignoré */ }
        }

        Journaliseur.Info("Proxy MITM arrêté");
    }

    public void Dispose()
    {
        Arreter();
        _annulation?.Dispose();
        foreach (var s in _sessions)
        {
            try { s.Dispose(); } catch { /* ignoré */ }
        }
    }
}
