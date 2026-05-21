using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Banque;

/// <summary>
/// Configuration du dépôt banque automatique. Quand
/// <see cref="Jeu.Personnage.Personnage.PourcentagePoids"/> >= <see cref="SeuilPoidsPct"/>,
/// le bot interrompt le farm, se rend à <see cref="MapBanqueId"/>, ouvre la
/// banque (interactif <see cref="GfxNpcBanquier"/> ou skill associé), dépose
/// les items configurés, puis reprend.
///
/// Persisté par perso dans <c>banque/&lt;perso&gt;.json</c>.
/// </summary>
public sealed class ConfigBanque
{
    /// <summary>Active le dépôt auto (false = désactivé).</summary>
    public bool Active { get; set; } = false;

    /// <summary>Pourcentage de poids déclencheur (par défaut 90% = transition douce avant FULL).</summary>
    public int SeuilPoidsPct { get; set; } = 90;

    /// <summary>Pourcentage de poids cible APRÈS dépôt (relâcher au moins jusqu'à X% — 30% par défaut).</summary>
    public int CiblePoidsPct { get; set; } = 30;

    /// <summary>ID de la map où se trouve la banque (ex. Astrub bank = 10117).</summary>
    public int MapBanqueId { get; set; } = 10117;

    /// <summary>GFX du PNJ banquier (utile pour le repérer sur la map).</summary>
    public int GfxNpcBanquier { get; set; } = 65;

    /// <summary>
    /// IDs des templates d'items à déposer (vide = TOUS les items dépositables,
    /// c'est-à-dire tous sauf équipement courant, pains, potions, ressources
    /// listées dans <see cref="ItemsAGarder"/>).
    /// </summary>
    public List<int> ItemsADeposer { get; set; } = new();

    /// <summary>IDs des templates à GARDER dans l'inventaire même si le seuil est atteint.</summary>
    public List<int> ItemsAGarder { get; set; } = new()
    {
        // Pains/pichets (consommables de soin) — toujours garder.
        // Astrub bread = 312, par exemple. À étendre par l'user.
    };

    /// <summary>Tente une reconnexion vers la map de farm précédente après dépôt.</summary>
    public bool RetourFarmApresDepot { get; set; } = true;

    /// <summary>Delai min/max (ms) entre 2 actions de dépôt (humanisation).</summary>
    public int DelaiActionMinMs { get; set; } = 200;
    public int DelaiActionMaxMs { get; set; } = 500;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static ConfigBanque Charger(string cheminFichier)
    {
        if (!File.Exists(cheminFichier))
        {
            Journaliseur.Info($"[CFG-BANQUE] {cheminFichier} introuvable, config par défaut (inactive)");
            return new ConfigBanque();
        }
        try
        {
            var json = File.ReadAllText(cheminFichier);
            var cfg = JsonSerializer.Deserialize<ConfigBanque>(json, Options) ?? new ConfigBanque();
            Journaliseur.Info(
                $"[CFG-BANQUE] Chargé : actif={cfg.Active}, seuil={cfg.SeuilPoidsPct}%, "
                + $"cible={cfg.CiblePoidsPct}%, map={cfg.MapBanqueId}, "
                + $"items à déposer={cfg.ItemsADeposer.Count}, à garder={cfg.ItemsAGarder.Count}");
            return cfg;
        }
        catch (System.Exception ex)
        {
            Journaliseur.Avertir($"[CFG-BANQUE] Erreur chargement : {ex.Message}, config par défaut");
            return new ConfigBanque();
        }
    }

    public void Sauvegarder(string cheminFichier)
    {
        var dossier = Path.GetDirectoryName(cheminFichier);
        if (!string.IsNullOrEmpty(dossier)) Directory.CreateDirectory(dossier);
        var json = JsonSerializer.Serialize(this, Options);
        File.WriteAllText(cheminFichier, json);
        Journaliseur.Info($"[CFG-BANQUE] Sauvegardé : {cheminFichier}");
    }
}
