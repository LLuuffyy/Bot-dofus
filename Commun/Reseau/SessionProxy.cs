using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Commun.Reseau;

/// <summary>
/// Représente une session MITM active : une paire de connexions TCP (client Dofus ↔ serveur distant).
/// Relaie les octets dans les deux sens, découpe les paquets sur le délimiteur 0x00 et publie
/// chaque paquet parsé via l'événement PaquetRecu.
///
/// Le protocole Dofus Retro 1.29 est textuel (UTF-8/ASCII), chaque message se termine par
/// un null byte 0x00. Quelques paquets serveur ont également un 0x0A (newline) juste avant
/// le 0x00 qu'il faut retirer.
/// </summary>
public sealed class SessionProxy : IDisposable
{
    private readonly TcpClient _cote_client;
    private readonly TcpClient _cote_serveur;
    private readonly ConfigReseau _config;
    private readonly CancellationTokenSource _annulation = new();

    private readonly StringBuilder _tamponVersClient = new();
    private readonly StringBuilder _tamponVersServeur = new();

    public event EventHandler<EvenementPaquetRecu>? PaquetRecu;
    public event EventHandler? SessionTerminee;

    public bool Active { get; private set; }

    public SessionProxy(TcpClient clientAccepte, TcpClient connexionVersServeur, ConfigReseau config)
    {
        _cote_client = clientAccepte;
        _cote_serveur = connexionVersServeur;
        _config = config;
    }

    /// <summary>Démarre les deux boucles de relais. Retourne immédiatement.</summary>
    public void Demarrer()
    {
        Active = true;
        Journaliseur.Info($"Session proxy démarrée {_cote_client.Client.RemoteEndPoint} ↔ {_cote_serveur.Client.RemoteEndPoint}");

        _ = Task.Run(() => BoucleRelaiAsync(
            source: _cote_client,
            destination: _cote_serveur,
            direction: DirectionPaquet.VersServeur,
            _annulation.Token));

        _ = Task.Run(() => BoucleRelaiAsync(
            source: _cote_serveur,
            destination: _cote_client,
            direction: DirectionPaquet.VersClient,
            _annulation.Token));
    }

    /// <summary>Injecte un message arbitraire vers le serveur (simule le client).</summary>
    public async Task EnvoyerAuServeurAsync(string message, CancellationToken ct = default)
    {
        var octets = EncoderPaquet(message);
        await _cote_serveur.GetStream().WriteAsync(octets, ct).ConfigureAwait(false);
        Journaliseur.Debogue($"[INJ →SRV] {message}");
    }

    /// <summary>Injecte un message arbitraire vers le client (simule le serveur).</summary>
    public async Task EnvoyerAuClientAsync(string message, CancellationToken ct = default)
    {
        var octets = EncoderPaquet(message);
        await _cote_client.GetStream().WriteAsync(octets, ct).ConfigureAwait(false);
        Journaliseur.Debogue($"[INJ →CLT] {message}");
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

        try
        {
            while (!ct.IsCancellationRequested)
            {
                int lus = await fluxSource.ReadAsync(tamponOctets, ct).ConfigureAwait(false);
                if (lus <= 0) break;

                await fluxDestination.WriteAsync(tamponOctets.AsMemory(0, lus), ct).ConfigureAwait(false);

                ExtrairePaquets(tamponOctets, lus, tamponTexte, direction);
            }
        }
        catch (OperationCanceledException) { /* fermeture normale */ }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"Boucle relai {direction} terminée : {ex.GetType().Name} - {ex.Message}");
        }
        finally
        {
            Arreter();
        }
    }

    private void ExtrairePaquets(byte[] octets, int longueur, StringBuilder tampon, DirectionPaquet direction)
    {
        int debutSegment = 0;
        for (int i = 0; i < longueur; i++)
        {
            if (octets[i] != 0x00) continue;

            if (i > debutSegment)
            {
                tampon.Append(Encoding.UTF8.GetString(octets, debutSegment, i - debutSegment));
            }

            var message = tampon.ToString().TrimEnd('\n', '\r');
            tampon.Clear();
            debutSegment = i + 1;

            if (message.Length == 0) continue;

            var paquet = new PaquetBrut(direction, message);
            Journaliseur.Trace(paquet.ToString());
            PaquetRecu?.Invoke(this, new EvenementPaquetRecu(paquet));
        }

        if (debutSegment < longueur)
        {
            tampon.Append(Encoding.UTF8.GetString(octets, debutSegment, longueur - debutSegment));
        }
    }

    public void Arreter()
    {
        if (!Active) return;
        Active = false;

        try { _annulation.Cancel(); } catch { /* ignoré */ }
        try { _cote_client.Close(); } catch { /* ignoré */ }
        try { _cote_serveur.Close(); } catch { /* ignoré */ }

        Journaliseur.Info("Session proxy terminée");
        SessionTerminee?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        Arreter();
        _annulation.Dispose();
        _cote_client.Dispose();
        _cote_serveur.Dispose();
    }
}
