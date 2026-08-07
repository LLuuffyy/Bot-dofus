using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Utilitaires.Hystoria;

/// <summary>
/// Gère l'ajout/suppression d'une entrée dans le fichier hosts Windows
/// (C:\Windows\System32\drivers\etc\hosts) pour faire pointer un hostname vers 127.0.0.1.
///
/// Nécessite que le bot tourne en ADMINISTRATEUR (le hosts file n'est modifiable
/// qu'avec les droits SYSTEM/Administrator).
///
/// Toutes les entrées ajoutées sont marquées avec un commentaire signature pour
/// permettre une suppression ciblée sans toucher aux autres entrées du user.
/// </summary>
public sealed class GestionnaireHosts
{
    public const string CheminHosts = @"C:\Windows\System32\drivers\etc\hosts";

    /// <summary>Tag de signature ajouté à chaque ligne pour identifier nos entrées.</summary>
    private const string MarqueurSignature = "# BotDofus MITM Proxy";

    public string IpCible { get; }
    public string Hostname { get; }

    public GestionnaireHosts(string hostname, string ipCible = "127.0.0.1")
    {
        Hostname = hostname ?? throw new ArgumentNullException(nameof(hostname));
        IpCible = ipCible ?? throw new ArgumentNullException(nameof(ipCible));
    }

    /// <summary>
    /// Ajoute (ou met à jour) l'entrée mappant <see cref="Hostname"/> vers <see cref="IpCible"/>.
    /// Idempotent : peut être appelé plusieurs fois sans accumuler de doublons.
    /// Vide aussi le cache DNS Windows pour que la nouvelle résolution soit prise en compte immédiatement.
    /// </summary>
    public void Ajouter()
    {
        if (!File.Exists(CheminHosts))
        {
            throw new FileNotFoundException($"Fichier hosts introuvable : {CheminHosts}");
        }

        string[] lignesActuelles = File.ReadAllLines(CheminHosts);
        var lignesFiltrees = lignesActuelles
            .Where(l => !ConcerneNotreEntree(l))
            .ToList();

        string nouvelleLigne = $"{IpCible} {Hostname}\t{MarqueurSignature}";
        lignesFiltrees.Add(nouvelleLigne);

        try
        {
            File.WriteAllLines(CheminHosts, lignesFiltrees);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException(
                "Le bot n'a pas les droits pour écrire dans hosts. Lancer Luffy-bot.exe en tant qu'administrateur.",
                ex);
        }

        Journaliseur.Info($"[HOSTS] Ajouté : {IpCible} {Hostname}");
        ViderCacheDns();
    }

    /// <summary>
    /// Retire toutes les entrées correspondant à <see cref="Hostname"/> ajoutées par ce bot
    /// (identifiées via <see cref="MarqueurSignature"/>).
    /// </summary>
    public void Retirer()
    {
        if (!File.Exists(CheminHosts)) return;

        string[] lignesActuelles = File.ReadAllLines(CheminHosts);
        var lignesFiltrees = lignesActuelles
            .Where(l => !ConcerneNotreEntree(l))
            .ToArray();

        if (lignesFiltrees.Length == lignesActuelles.Length)
        {
            Journaliseur.Info("[HOSTS] Aucune entrée du bot à retirer");
            return;
        }

        try
        {
            File.WriteAllLines(CheminHosts, lignesFiltrees);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException(
                "Le bot n'a pas les droits pour écrire dans hosts. Lancer en admin pour faire le cleanup.",
                ex);
        }

        Journaliseur.Info($"[HOSTS] Entrée {Hostname} retirée");
        ViderCacheDns();
    }

    private bool ConcerneNotreEntree(string ligne)
    {
        if (string.IsNullOrWhiteSpace(ligne)) return false;
        if (ligne.Contains(MarqueurSignature, StringComparison.Ordinal)) return true;

        // Cas où une ligne mapperait Hostname sans notre signature (ex: SynFus)
        // → on ne touche pas, par prudence.
        return false;
    }

    /// <summary>
    /// Diagnostic : retourne true si une entrée signée par le bot existe pour ce hostname.
    /// Lecture seule, ne nécessite pas les droits admin.
    /// </summary>
    public bool EstInstalle()
    {
        if (!File.Exists(CheminHosts)) return false;
        try
        {
            return File.ReadAllLines(CheminHosts).Any(ConcerneNotreEntree);
        }
        catch
        {
            return false;
        }
    }

    private static void ViderCacheDns()
    {
        try
        {
            var psi = new ProcessStartInfo("ipconfig", "/flushdns")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(3000);
            Journaliseur.Info("[HOSTS] Cache DNS vidé");
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[HOSTS] Impossible de vider le cache DNS : {ex.Message}");
        }
    }
}
