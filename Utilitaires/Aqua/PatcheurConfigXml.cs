using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Utilitaires.Aqua;

/// <summary>
/// Patche le <c>config.xml</c> du client Aqua (Bubble/play-astra) pour rediriger sa
/// connexion serveur vers notre proxy MITM local.
///
/// Le client Aqua (loader.swf) lit, dans son XML <c>&lt;conf&gt;</c> sélectionné :
/// <code>
///   &lt;connexionServers&gt;
///     &lt;connserver url="..." ip="127.0.0.1" port="7781"/&gt;
///   &lt;/connexionServers&gt;
/// </code>
/// Si présent, le SWF utilise cette IP/port à la place du serveur officiel.
///
/// Stratégie : on injecte le bloc <c>connexionServers</c> dans le <c>&lt;conf&gt;</c>
/// existant, on lance <c>Dofus.exe</c>, on attend que Flash ait fini de lire (par défaut 10s),
/// puis on restaure le fichier d'origine. C'est exactement ce que fait <c>Synfus Bot</c>
/// (cf. son log <c>[PATCH] config.xml modifié : connexionServer → 127.0.0.1:7781</c>).
///
/// Avantages vs DLL injection (méthode AquaHook) :
///   - 100% C# managé, aucun code natif
///   - Pas de modif binaire (le XML est restauré automatiquement)
///   - Reproductible et lisible pour la démo de projet
/// </summary>
public sealed class PatcheurConfigXml
{
    /// <summary>Chemin par défaut du <c>config.xml</c> Abrak (launcher Electron, Roaming).</summary>
    public static readonly string CheminConfigDefaut = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Abrak Launcher", "Abrak", "Retro", "resources", "app", "retroclient", "config.xml");

    /// <summary>Chemin par défaut de l'exécutable Abrak (retroclient).</summary>
    public static readonly string CheminExecutableDefaut = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Abrak Launcher", "Abrak", "Retro", "resources", "app", "retroclient", "Abrak.exe");

    public string CheminConfig { get; }
    public string IpLocale { get; }
    public int PortLocal { get; }

    /// <summary>Délai d'attente avant restauration du fichier (le client doit avoir lu).</summary>
    // Le launcher Abrak (Electron + Chromium + Flash) est lent à booter : il peut
    // mettre 30-50s avant de lire config.xml. Si on restaure trop tôt, il lit la
    // version d'origine et se connecte au vrai serveur. 60s = marge confortable.
    public TimeSpan DelaiRestoreApresLancement { get; set; } = TimeSpan.FromSeconds(60);

    public PatcheurConfigXml(string ipLocale = "127.0.0.1", int portLocal = 1303, string? cheminConfig = null)
    {
        IpLocale = ipLocale;
        PortLocal = portLocal;
        CheminConfig = cheminConfig ?? CheminConfigDefaut;
    }

    /// <summary>
    /// Patche le config.xml en injectant le <c>&lt;connexionServers&gt;</c> qui force le client
    /// à se connecter à <see cref="IpLocale"/>:<see cref="PortLocal"/>.
    /// Sauvegarde le contenu original pour restauration ultérieure.
    /// Idempotent : si le bloc existe déjà, il est mis à jour à la valeur courante.
    /// </summary>
    /// <returns>Le contenu XML original (à passer à <see cref="Restaurer"/>).</returns>
    public string Patcher()
    {
        if (!File.Exists(CheminConfig))
        {
            throw new FileNotFoundException(
                $"config.xml Aqua introuvable : {CheminConfig}\n" +
                $"Vérifie que le launcher Bubble/Aqua est installé.");
        }

        var original = File.ReadAllText(CheminConfig);
        var doc = XDocument.Parse(original);

        var conf = doc.Root?.Element("conf")
                   ?? throw new InvalidDataException("config.xml : élément <conf> introuvable.");

        // Supprime tout ancien bloc connexionServers (idempotence).
        conf.Elements("connexionServers").Remove();

        // Injecte le bloc avec une seule entrée pointant vers notre proxy.
        var bloc = new XElement("connexionServers",
            new XElement("connserver",
                new XAttribute("url", "BotDofus MITM Proxy"),
                new XAttribute("ip", IpLocale),
                new XAttribute("port", PortLocal.ToString())));
        conf.AddFirst(bloc);

        File.WriteAllText(CheminConfig, doc.ToString());

        Journaliseur.Info($"[AQUA-PATCH] config.xml patché : connexionServer → {IpLocale}:{PortLocal}");
        return original;
    }

    /// <summary>Restaure le contenu original passé en paramètre.</summary>
    public void Restaurer(string contenuOriginal)
    {
        try
        {
            File.WriteAllText(CheminConfig, contenuOriginal);
            Journaliseur.Info("[AQUA-PATCH] config.xml restauré.");
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[AQUA-PATCH] Restauration échouée : {ex.Message}");
        }
    }

    /// <summary>Contenu config.xml d'origine, conservé pour restauration différée.</summary>
    public string? ContenuOriginal { get; private set; }

    /// <summary>
    /// Workflow : patche le XML, lance le client. NE RESTAURE PAS sur timer
    /// (le launcher Electron Abrak est lent et peut lire config.xml tardivement —
    /// un restore prématuré le ferait connecter au vrai serveur).
    /// La restauration se fait via <see cref="RestaurerDiffere"/> à la déconnexion
    /// ou fermeture du bot. <see cref="ContenuOriginal"/> garde la sauvegarde.
    /// </summary>
    public Task LancerClientAsync(string? cheminExe = null, CancellationToken ct = default)
    {
        cheminExe ??= CheminExecutableDefaut;
        if (!File.Exists(cheminExe))
        {
            throw new FileNotFoundException($"Client Abrak introuvable : {cheminExe}");
        }

        ContenuOriginal = Patcher();
        var psi = new System.Diagnostics.ProcessStartInfo(cheminExe)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(cheminExe)!,
        };
        var proc = System.Diagnostics.Process.Start(psi);
        Journaliseur.Info($"[AQUA-PATCH] Client lancé (PID={proc?.Id ?? -1}) — config.xml RESTE patché jusqu'à déconnexion");
        return Task.CompletedTask;
    }

    /// <summary>Restaure le config.xml d'origine s'il a été patché. Idempotent.</summary>
    public void RestaurerDiffere()
    {
        if (ContenuOriginal != null)
        {
            Restaurer(ContenuOriginal);
            ContenuOriginal = null;
        }
    }

    /// <summary>Diagnostic lecture-seule : true si un bloc connexionServers est présent.</summary>
    public bool EstActuellementPatche()
    {
        try
        {
            if (!File.Exists(CheminConfig)) return false;
            var doc = XDocument.Load(CheminConfig);
            return doc.Root?.Element("conf")?.Element("connexionServers") != null;
        }
        catch
        {
            return false;
        }
    }
}
