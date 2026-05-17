using System;
using System.Net;
using System.Threading;
using BotDofus.Utilitaires.Journaux;
using WinDivertSharp;

namespace BotDofus.Utilitaires.Reseau;

/// <summary>
/// Redirection réseau au niveau driver (WinDivert) — la SEULE méthode fiable quand
/// le client se connecte directement à une IP serveur qu'on ne peut pas influencer
/// (cas Abrak : le launcher Electron récupère l'IP dynamiquement et l'ignore dans
/// config.xml). Équivalent de la DLL hook connect() de Synfus, en packet-level.
///
/// Principe :
///   Client → 51.89.153.20:1303   (paquet sortant intercepté)
///     → on réécrit src+dst en 127.0.0.1  (livraison loopback à notre proxy)
///   Proxy 127.0.0.1:1303 → 51.89.153.20:1303  (vraie connexion, src = port marqueur,
///     EXCLUE du filtre → pas de boucle)
///   Réponse 127.0.0.1:1303 → client  (paquet entrant intercepté)
///     → on restaure src=51.89.153.20, dst=IP réelle du client
///   Le client croit parler au vrai serveur ; on voit tout en clair via le proxy.
///
/// Nécessite : admin (chargement driver WinDivert64.sys), .dll/.sys présents
/// à côté de l'exe (copiés par le package WinDivertSharp).
/// </summary>
public sealed class RedirecteurWinDivert : IDisposable
{
    private readonly string _ipServeur;
    private readonly int _port1;
    private readonly int _port2;
    private readonly int _portMarqueur;
    private readonly IPAddress _ipLocale = IPAddress.Parse("127.0.0.1");
    private readonly IPAddress _ipServeur1;

    private IntPtr _handle = IntPtr.Zero;
    private Thread? _boucle;
    private volatile bool _actif;
    private IPAddress? _ipClient;

    public bool Actif => _actif;
    public long PaquetsRediriges { get; private set; }

    public RedirecteurWinDivert(string ipServeur, int portAuth, int portJeu, int portMarqueur)
    {
        _ipServeur = ipServeur;
        _ipServeur1 = IPAddress.Parse(ipServeur);
        _port1 = portAuth;
        _port2 = portJeu;
        _portMarqueur = portMarqueur;
    }

    /// <summary>
    /// Ouvre le handle WinDivert et démarre la boucle d'interception en arrière-plan.
    /// Lève une exception si WinDivert ne peut pas s'ouvrir (driver absent / pas admin).
    /// </summary>
    public void Demarrer()
    {
        if (_actif) return;

        // Filtre : sortant client→serveur (hors notre port marqueur) + entrant proxy→client.
        var filtre =
            $"tcp and (" +
            $"(outbound and ip.DstAddr == {_ipServeur} and " +
            $"(tcp.DstPort == {_port1} or tcp.DstPort == {_port2}) and tcp.SrcPort != {_portMarqueur}) " +
            $"or " +
            $"(inbound and ip.SrcAddr == 127.0.0.1 and " +
            $"(tcp.SrcPort == {_port1} or tcp.SrcPort == {_port2}))" +
            $")";

        _handle = WinDivert.WinDivertOpen(filtre, WinDivertLayer.Network, 0, WinDivertOpenFlags.None);
        if (_handle == IntPtr.Zero || _handle == new IntPtr(-1))
        {
            var err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"WinDivertOpen a échoué (code {err}). Causes : pas admin, WinDivert64.sys absent, " +
                $"ou un autre WinDivert tourne déjà.");
        }

        _actif = true;
        _boucle = new Thread(BoucleInterception) { IsBackground = true, Name = "WinDivert-Redir" };
        _boucle.Start();
        Journaliseur.Info($"[WD] Redirecteur actif : {_ipServeur}:{_port1}/{_port2} → 127.0.0.1 " +
                          $"(port marqueur exclu : {_portMarqueur})");
    }

    private unsafe void BoucleInterception()
    {
        var buffer = new WinDivertBuffer();
        var addr = new WinDivertAddress();

        while (_actif)
        {
            uint readLen = 0;
            if (!WinDivert.WinDivertRecv(_handle, buffer, ref addr, ref readLen))
            {
                if (!_actif) break;
                continue;
            }

            try
            {
                var parse = WinDivert.WinDivertHelperParsePacket(buffer, readLen);
                if (parse.IPv4Header == null || parse.TcpHeader == null)
                {
                    WinDivert.WinDivertSend(_handle, buffer, readLen, ref addr);
                    continue;
                }

                var ip = parse.IPv4Header;

                if (addr.Direction == WinDivertDirection.Outbound)
                {
                    // Client → serveur réel (51.89.153.20). On mémorise l'IP réelle
                    // du client puis on bascule le paquet en loopback pur (127↔127)
                    // ET on l'injecte comme ENTRANT : sinon Windows le renvoie vers
                    // la carte réseau et il n'atteint jamais notre proxy local
                    // ("serveur introuvable"). C'est LE point critique du redirect.
                    _ipClient ??= ip->SrcAddr;
                    ip->SrcAddr = _ipLocale;
                    ip->DstAddr = _ipLocale;
                    addr.Direction = WinDivertDirection.Inbound;
                    addr.Loopback = true;
                }
                else
                {
                    // Réponse du proxy (127.0.0.1:1303/1304) → client. Le client a
                    // appelé connect(51.89.153.20:1303) : sa pile TCP n'accepte la
                    // réponse que si elle vient de 51.89.153.20. On restaure donc
                    // src=serveur, dst=IP réelle client, et on injecte ENTRANT
                    // (paquet "venant du réseau" pour la socket cliente).
                    ip->SrcAddr = _ipServeur1;
                    ip->DstAddr = _ipClient ?? ip->DstAddr;
                    addr.Direction = WinDivertDirection.Inbound;
                    addr.Loopback = false;
                }

                addr.Impostor = false;
                WinDivert.WinDivertHelperCalcChecksums(buffer, readLen, ref addr,
                    WinDivertChecksumHelperParam.All);
                WinDivert.WinDivertSend(_handle, buffer, readLen, ref addr);
                PaquetsRediriges++;
            }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[WD] Paquet ignoré : {ex.Message}");
                try { WinDivert.WinDivertSend(_handle, buffer, readLen, ref addr); } catch { }
            }
        }
    }

    public void Arreter()
    {
        if (!_actif) return;
        _actif = false;
        try
        {
            if (_handle != IntPtr.Zero && _handle != new IntPtr(-1))
            {
                WinDivert.WinDivertClose(_handle);
            }
        }
        catch { }
        _handle = IntPtr.Zero;
        Journaliseur.Info($"[WD] Redirecteur arrêté ({PaquetsRediriges} paquets redirigés).");
    }

    public void Dispose() => Arreter();
}
