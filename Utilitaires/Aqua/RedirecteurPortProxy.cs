using System;
using System.Diagnostics;
using System.Linq;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Utilitaires.Aqua;

/// <summary>
/// Backup au PatcheurConfigXml : utilise <c>netsh interface portproxy</c> pour rediriger
/// au niveau noyau Windows un couple IP:port distant vers 127.0.0.1:port local.
///
/// C'est l'équivalent système de la DLL hook <c>aqua_hook.dll</c> de Synfus, mais
/// sans code natif : on délègue à <c>netsh</c> (commande Windows standard).
///
/// Usage typique :
///   - À utiliser si <see cref="PatcheurConfigXml"/> ne suffit pas (le client retombe sur l'API
///     remote ankama_acc et ignore notre <connserver> du config.xml)
///   - Requiert d'être lancé en ADMINISTRATEUR (sinon "L'accès est refusé.")
///
/// Idempotent : ajout/suppression vérifient l'existence avant action.
/// </summary>
public sealed class RedirecteurPortProxy : IDisposable
{
    public string IpDistante { get; }
    public int PortDistant { get; }
    public int PortLocal { get; }

    private bool _ajoutee;

    public RedirecteurPortProxy(string ipDistante, int portDistant, int portLocal = 0)
    {
        IpDistante = ipDistante;
        PortDistant = portDistant;
        PortLocal = portLocal == 0 ? portDistant : portLocal;
    }

    /// <summary>
    /// Crée la règle : tout trafic local sortant vers <see cref="IpDistante"/>:<see cref="PortDistant"/>
    /// est redirigé vers <c>127.0.0.1:<see cref="PortLocal"/></c>.
    /// </summary>
    public void Ajouter()
    {
        var args = $"interface portproxy add v4tov4 " +
                   $"listenaddress={IpDistante} listenport={PortDistant} " +
                   $"connectaddress=127.0.0.1 connectport={PortLocal}";
        var (ok, sortie) = ExecuterNetsh(args);
        if (!ok)
        {
            throw new InvalidOperationException(
                $"netsh portproxy add a échoué (admin requis ?). Sortie :\n{sortie}");
        }
        _ajoutee = true;
        Journaliseur.Info($"[NETSH] portproxy {IpDistante}:{PortDistant} → 127.0.0.1:{PortLocal}");
    }

    /// <summary>Retire la règle si elle existe. Sans effet si déjà absente.</summary>
    public void Retirer()
    {
        var args = $"interface portproxy delete v4tov4 " +
                   $"listenaddress={IpDistante} listenport={PortDistant}";
        var (ok, sortie) = ExecuterNetsh(args);
        if (ok)
        {
            Journaliseur.Info($"[NETSH] portproxy retiré : {IpDistante}:{PortDistant}");
        }
        else
        {
            Journaliseur.Avertir($"[NETSH] retrait portproxy : {sortie}");
        }
        _ajoutee = false;
    }

    /// <summary>Liste les règles actuellement actives (diagnostic).</summary>
    public static string Lister()
    {
        var (_, sortie) = ExecuterNetsh("interface portproxy show all");
        return sortie;
    }

    /// <summary>Diagnostic lecture-seule : true si une règle pour cet IP:port est active.</summary>
    public bool EstActive()
    {
        var sortie = Lister();
        var clef = $"{IpDistante}";
        return sortie.Split('\n').Any(l => l.Contains(clef) && l.Contains(PortDistant.ToString()));
    }

    public void Dispose()
    {
        if (_ajoutee)
        {
            try { Retirer(); } catch { }
        }
    }

    private static (bool ok, string sortie) ExecuterNetsh(string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo("netsh", arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var proc = Process.Start(psi);
            if (proc == null) return (false, "Process.Start a renvoyé null");
            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit(5000);
            return (proc.ExitCode == 0, stdout + stderr);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
