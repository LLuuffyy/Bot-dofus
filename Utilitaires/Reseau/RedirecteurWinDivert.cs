using System;
using System.Net;
using System.Threading;
using BotDofus.Utilitaires.Journaux;
using WindivertDotnet;

namespace BotDofus.Utilitaires.Reseau;

/// <summary>
/// Redirection réseau au niveau driver (WinDivert 2.2 via WindivertDotnet) — la
/// SEULE méthode fiable quand le client se connecte directement à une IP serveur
/// qu'on ne peut pas influencer (Abrak : launcher Electron, IP dynamique, ignore
/// config.xml). Équivalent packet-level du hook connect() de Synfus.
///
/// WinDivert 1.4 ne gère pas le loopback (127.0.0.1) → on est passé en 2.2 qui
/// le supporte, avec <see cref="WinDivertRouter"/> qui recalcule automatiquement
/// IfIdx + flags Loopback/Outbound pour la nouvelle destination (le point qui
/// faisait échouer la redirection "serveur introuvable").
///
/// Flux :
///   Client → 51.89.153.20:1303   (sortant intercepté)
///     → src+dst = 127.0.0.1, router recalcule l'adresse → livré au proxy local
///   Proxy 127.0.0.1:1303 → 51.89.153.20:1303  (vraie connexion, src = port
///     marqueur 50303, EXCLUE du filtre → pas de boucle)
///   Réponse 127.0.0.1 → client  (entrant intercepté)
///     → src = 51.89.153.20, dst = IP réelle client → la pile TCP cliente accepte
///
/// Nécessite admin (driver WinDivert64.sys) + WinDivert.dll natif près de l'exe.
/// </summary>
public sealed class RedirecteurWinDivert : IDisposable
{
    private readonly string _ipServeur;
    private readonly IPAddress _ipServeurAddr;
    private readonly int _port1;
    private readonly int _port2;
    private readonly int _portMarqueur;
    private static readonly IPAddress Loopback = IPAddress.Loopback;

    private WinDivert? _divert;
    private Thread? _boucle;
    private volatile bool _actif;
    private IPAddress? _ipClient;

    public bool Actif => _actif;
    public long PaquetsRediriges { get; private set; }

    public RedirecteurWinDivert(string ipServeur, int portAuth, int portJeu, int portMarqueur)
    {
        _ipServeur = ipServeur;
        _ipServeurAddr = IPAddress.Parse(ipServeur);
        _port1 = portAuth;
        _port2 = portJeu;
        _portMarqueur = portMarqueur;
    }

    public void Demarrer()
    {
        if (_actif) return;

        var filtre =
            $"tcp and (" +
            $"(outbound and ip.DstAddr == {_ipServeur} and " +
            $"(tcp.DstPort == {_port1} or tcp.DstPort == {_port2}) and tcp.SrcPort != {_portMarqueur}) " +
            $"or " +
            $"(ip.SrcAddr == 127.0.0.1 and (tcp.SrcPort == {_port1} or tcp.SrcPort == {_port2}))" +
            $")";

        try
        {
            _divert = new WinDivert(filtre, WinDivertLayer.Network);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"WinDivert n'a pas pu s'ouvrir : {ex.Message}. " +
                $"Causes : pas admin, WinDivert.dll/WinDivert64.sys absents près de l'exe.", ex);
        }

        _actif = true;
        _boucle = new Thread(BoucleInterception) { IsBackground = true, Name = "WinDivert-Redir" };
        _boucle.Start();
        Journaliseur.Info($"[WD] Redirecteur actif (WinDivert 2.2) : {_ipServeur}:{_port1}/{_port2} " +
                          $"→ 127.0.0.1 (port marqueur exclu : {_portMarqueur})");
    }

    private unsafe void BoucleInterception()
    {
        using var packet = new WinDivertPacket();
        using var addr = new WinDivertAddress();

        int diag = 0;          // nb de paquets loggués en détail
        int errRecv = 0;       // nb d'erreurs Recv loggées
        long recus = 0;

        while (_actif && _divert != null)
        {
            int len;
            try
            {
                len = _divert.Recv(packet, addr);
            }
            catch (Exception exr)
            {
                if (!_actif) break;
                if (errRecv++ < 3)
                    Journaliseur.Avertir($"[WD] Recv erreur ({exr.GetType().Name}): {exr.Message}");
                Thread.Sleep(5);
                continue;
            }

            recus++;
            try
            {
                var res = packet.GetParseResult();
                if (res.IPV4Header == null || res.TcpHeader == null)
                {
                    if (diag < 20) Journaliseur.Info($"[WD] #{recus} non-IPv4/TCP, relayé tel quel");
                    _divert.Send(packet, addr);
                    continue;
                }

                var ip = res.IPV4Header;
                var tcp = res.TcpHeader;
                bool sortant = addr.Flags.HasFlag(WinDivertAddressFlag.Outbound);
                var avant = $"{ip->SrcAddr}:{tcp->SrcPort} → {ip->DstAddr}:{tcp->DstPort}";

                IPAddress nouvelleDst;
                if (sortant)
                {
                    _ipClient ??= ip->SrcAddr;
                    ip->SrcAddr = Loopback;
                    ip->DstAddr = Loopback;
                    nouvelleDst = Loopback;
                }
                else
                {
                    ip->SrcAddr = _ipServeurAddr;
                    ip->DstAddr = _ipClient ?? ip->DstAddr;
                    nouvelleDst = _ipClient ?? ip->DstAddr;
                }

                bool routerOk = true;
                try { new WinDivertRouter(nouvelleDst).ApplyToAddress(addr); }
                catch (Exception exrt) { routerOk = false; if (diag < 20) Journaliseur.Avertir($"[WD] Router KO ({exrt.Message})"); }

                packet.CalcChecksums(addr, ChecksumsFlag.All);
                int envoye = _divert.Send(packet, addr);

                if (diag++ < 20)
                {
                    Journaliseur.Info($"[WD] #{recus} {(sortant ? "OUT" : "IN ")} {avant} " +
                        $"→ {ip->SrcAddr}:{tcp->SrcPort}→{ip->DstAddr}:{tcp->DstPort} " +
                        $"routeur={(routerOk ? "ok" : "KO")} envoyé={envoye}o");
                }
                PaquetsRediriges++;
            }
            catch (Exception ex)
            {
                if (diag < 20) Journaliseur.Avertir($"[WD] Paquet #{recus} ignoré : {ex.GetType().Name} {ex.Message}");
                try { _divert.Send(packet, addr); } catch { }
            }
        }

        Journaliseur.Info($"[WD] Boucle terminée ({recus} reçus, {PaquetsRediriges} redirigés).");
    }

    public void Arreter()
    {
        if (!_actif) return;
        _actif = false;
        try { _divert?.Dispose(); } catch { }
        _divert = null;
        Journaliseur.Info($"[WD] Redirecteur arrêté ({PaquetsRediriges} paquets redirigés).");
    }

    public void Dispose() => Arreter();
}
