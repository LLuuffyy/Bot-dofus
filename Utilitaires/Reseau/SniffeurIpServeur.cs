using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Utilitaires.Reseau;

/// <summary>
/// Mode DISCOVERY : capture l'IP du serveur de jeu inconnu en sniffant les
/// connexions TCP sortantes du processus client (Dofus.exe / Rafal Retro).
///
/// Utilisation : à appeler quand <see cref="BotDofus.Commun.Reseau.ConfigReseau.EstIpServeurInconnue"/>
/// est true. Le user lance le client → on observe les connexions sortantes
/// vers le port cible (ex. 26118 pour Rafale) → la 1re IP non locale =
/// l'IP du serveur. On la log + on la persiste dans config-reseau.json.
///
/// Pourquoi pas WinDivert ? WinDivert exige le filtre IP à l'ouverture du
/// driver et nous voulons justement capturer l'IP. <see cref="IPGlobalProperties"/>
/// expose la table TCP active de Windows sans privilège élevé et sans driver.
/// On poll cette table toutes les 250 ms jusqu'à voir une connexion établie
/// vers <c>:&lt;PortCible&gt;</c>.
///
/// Limite : doit être lancé AVANT que le client se connecte (sinon on rate
/// le moment où la connexion est ESTABLISHED). On poll pendant <see cref="DureeMaxMs"/>.
/// </summary>
public sealed class SniffeurIpServeur
{
    /// <summary>Port distant à observer (ex. 26118 sur Rafale).</summary>
    public int PortCible { get; }

    /// <summary>Durée max d'écoute avant timeout (ms).</summary>
    public int DureeMaxMs { get; set; } = 60_000;

    /// <summary>Intervalle de poll de la table TCP (ms).</summary>
    public int IntervalleMs { get; set; } = 250;

    /// <summary>IP capturée (null tant que rien observé).</summary>
    public string? IpCapturee { get; private set; }

    /// <summary>Port distant capturé (peut différer de <see cref="PortCible"/>
    /// si on a observé une autre connexion utile).</summary>
    public int? PortCapture { get; private set; }

    public SniffeurIpServeur(int portCible)
    {
        PortCible = portCible;
    }

    /// <summary>
    /// Attend qu'une connexion sortante vers <see cref="PortCible"/> apparaisse
    /// dans la table TCP Windows et retourne l'IP distante observée.
    /// Retourne null en cas de timeout.
    /// </summary>
    public async Task<string?> AttendreIpAsync(CancellationToken ct = default)
    {
        Journaliseur.Info(
            $"[SNIFFER] Discovery mode actif — j'attends une connexion sortante vers :{PortCible} " +
            $"pendant {DureeMaxMs / 1000}s. Lance le client Rafale maintenant.");

        var observees = new ConcurrentDictionary<string, byte>();
        var debut = Environment.TickCount;

        while (Environment.TickCount - debut < DureeMaxMs)
        {
            if (ct.IsCancellationRequested)
            {
                Journaliseur.Avertir("[SNIFFER] Annulé par l'user.");
                return null;
            }

            try
            {
                var tcp = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
                foreach (var c in tcp)
                {
                    var dest = c.RemoteEndPoint;
                    if (dest.Port != PortCible) continue;
                    if (EstIpLocaleOuPrivee(dest.Address)) continue;

                    var key = $"{dest.Address}:{dest.Port}";
                    if (!observees.TryAdd(key, 0)) continue;

                    Journaliseur.Info(
                        $"[SNIFFER] ✅ IP serveur capturée : {dest.Address}:{dest.Port} " +
                        $"(état {c.State}).");

                    IpCapturee = dest.Address.ToString();
                    PortCapture = dest.Port;
                    return IpCapturee;
                }
            }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[SNIFFER] Lecture table TCP échouée : {ex.Message}");
            }

            await Task.Delay(IntervalleMs, ct).ConfigureAwait(false);
        }

        Journaliseur.Avertir(
            $"[SNIFFER] ⏱ Timeout après {DureeMaxMs / 1000}s — aucune connexion vers :{PortCible} observée. " +
            $"Vérifie que le client Rafale a bien été lancé.");
        return null;
    }

    /// <summary>Filtre les IP locales / réservées / loopback pour ne garder
    /// que les IP publiques (les vraies cibles serveur).</summary>
    private static bool EstIpLocaleOuPrivee(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true;
        if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return true;

        var bytes = ip.GetAddressBytes();
        // 10.0.0.0/8
        if (bytes[0] == 10) return true;
        // 172.16.0.0/12
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
        // 192.168.0.0/16
        if (bytes[0] == 192 && bytes[1] == 168) return true;
        // 169.254.0.0/16 (link-local)
        if (bytes[0] == 169 && bytes[1] == 254) return true;
        // 127.0.0.0/8 déjà filtré par IsLoopback
        return false;
    }
}
