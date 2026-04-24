using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
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
    private readonly ConfigReseau _config;
    private readonly TcpListener _ecouteur;
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

        return Task.Run(() => BoucleAcceptationAsync(_annulation.Token), _annulation.Token);
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
            await connexionServeur.ConnectAsync(_config.HoteDistant, _config.PortDistant, ct).ConfigureAwait(false);
            Journaliseur.Info($"Connexion sortante établie vers {_config.HoteDistant}:{_config.PortDistant}");

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
