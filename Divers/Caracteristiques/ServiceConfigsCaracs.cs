using System;
using System.IO;
using System.Text.Json;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Caracteristiques;

/// <summary>
/// Charge / sauvegarde les configs de répartition caracs dans le dossier
/// <c>caracs/&lt;identifiantCompte&gt;.json</c>. Format identique au pattern
/// <c>peleas/&lt;perso&gt;.json</c> du combat.
/// </summary>
public static class ServiceConfigsCaracs
{
    private const string DossierCaracs = "caracs";

    public static string CheminFichier(string identifiantCompte)
        => Path.Combine(DossierCaracs, $"{identifiantCompte}.json");

    /// <summary>
    /// Charge la config caracs depuis <c>caracs/&lt;perso&gt;.json</c>.
    /// Retourne null si le fichier n'existe pas ou si le parsing échoue
    /// (dans ce cas l'auto-distribution est désactivée → mode Manuel implicite).
    /// </summary>
    public static ConfigRepartitionCaracs? Charger(string identifiantCompte)
    {
        try
        {
            var chemin = CheminFichier(identifiantCompte);
            if (!File.Exists(chemin)) return null;
            var json = File.ReadAllText(chemin);
            var cfg = JsonSerializer.Deserialize<ConfigRepartitionCaracs>(json, OptionsJson);
            if (cfg == null) return null;
            Journaliseur.Info($"[CARACS] Config chargée pour {identifiantCompte} : Mode={cfg.Mode}, total={cfg.Total}%");
            return cfg;
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[CARACS] Échec chargement {identifiantCompte} : {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Sauvegarde la config caracs dans <c>caracs/&lt;perso&gt;.json</c>.
    /// Crée le dossier si nécessaire.
    /// </summary>
    public static void Sauvegarder(string identifiantCompte, ConfigRepartitionCaracs cfg)
    {
        if (cfg == null) return;
        try
        {
            Directory.CreateDirectory(DossierCaracs);
            var chemin = CheminFichier(identifiantCompte);
            var json = JsonSerializer.Serialize(cfg, OptionsJson);
            File.WriteAllText(chemin, json);
            Journaliseur.Info($"[CARACS] Config sauvegardée pour {identifiantCompte}.");
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[CARACS] Échec sauvegarde {identifiantCompte} : {ex.Message}");
        }
    }

    private static readonly JsonSerializerOptions OptionsJson = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
