using System;
using System.IO;
using System.Text.Json;
using BotDofus.Divers.Banque;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Marchand;

/// <summary>
/// Config de vente automatique au PNJ marchand. Mêmes catégories que la
/// <see cref="ConfigBanque"/> mais cibles différentes : on vend au PNJ ce
/// qu'on ne veut pas mettre en banque (typiquement équipements drop combat).
///
/// <para>Workflow type : à <see cref="SeuilPoidsPct"/> de pods, le bot suit le
/// trajet enregistré jusqu'à la taverne, dialogue avec le PNJ
/// <see cref="IdPnjMarchand"/>, vend tous les items des catégories cochées,
/// puis reprend le farm.</para>
/// </summary>
public sealed class ConfigMarchand
{
    /// <summary>Si <c>false</c>, le bot ne déclenche jamais la vente PNJ auto.</summary>
    public bool Active { get; set; } = false;

    /// <summary>Pourcentage de poids qui déclenche le trajet vers le marchand.</summary>
    public int SeuilPoidsPct { get; set; } = 80;

    /// <summary>ID de la map du PNJ marchand (taverne par défaut, à customiser).</summary>
    public int MapMarchandId { get; set; } = 7573;

    /// <summary>
    /// ID du PNJ marchand sur la map (gabarit positif). Sera résolu vers l'id
    /// contextuel via <see cref="EntitePNJ.IdGabarit"/> au moment du dialogue.
    /// </summary>
    public int IdPnjMarchand { get; set; } = 464;

    /// <summary>
    /// Vendre les équipements (armes / armures / anneaux / capes / coiffes…).
    /// C'est le cas d'usage principal — c'est ces items qui font du revenu.
    /// </summary>
    public bool VendreEquipements { get; set; } = true;

    /// <summary>Vendre les ressources (déconseillé — préférer la banque).</summary>
    public bool VendreRessources { get; set; } = false;

    /// <summary>Vendre les consommables (potions/parchemins/pains).</summary>
    public bool VendreConsommables { get; set; } = false;

    /// <summary>Vendre les items inconnus (catégorie non répertoriée par le catalogueur).</summary>
    public bool VendreInconnus { get; set; } = false;

    /// <summary>Liste noire : items à garder même si la catégorie est cochée.</summary>
    public System.Collections.Generic.HashSet<int> IdsAGarder { get; set; } = new();

    /// <summary>Délai humanisé entre 2 actions de vente (ms, min-max).</summary>
    public int DelaiActionMinMs { get; set; } = 200;
    public int DelaiActionMaxMs { get; set; } = 600;

    /// <summary>Charge la config depuis <c>marchand/&lt;perso&gt;.json</c>. Retourne défauts si absente.</summary>
    public static ConfigMarchand Charger(string cheminFichier)
    {
        try
        {
            if (!File.Exists(cheminFichier)) return new ConfigMarchand();
            var json = File.ReadAllText(cheminFichier);
            var cfg = JsonSerializer.Deserialize<ConfigMarchand>(json, OptionsJson) ?? new ConfigMarchand();
            cfg.Valider();
            Journaliseur.Info($"[MARCHAND] Config chargée : actif={cfg.Active}, seuil={cfg.SeuilPoidsPct}%, "
                + $"map={cfg.MapMarchandId}, pnj={cfg.IdPnjMarchand}, "
                + $"équipements={cfg.VendreEquipements}, ressources={cfg.VendreRessources}");
            return cfg;
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[MARCHAND] Échec chargement {cheminFichier} : {ex.Message} — défauts utilisés.");
            return new ConfigMarchand();
        }
    }

    public void Sauvegarder(string cheminFichier)
    {
        try
        {
            var dir = Path.GetDirectoryName(cheminFichier);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(this, OptionsJson);
            File.WriteAllText(cheminFichier, json);
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[MARCHAND] Échec sauvegarde {cheminFichier} : {ex.Message}");
        }
    }

    public void Valider()
    {
        if (SeuilPoidsPct < 20) SeuilPoidsPct = 20;
        if (SeuilPoidsPct > 99) SeuilPoidsPct = 80;
        if (DelaiActionMaxMs < DelaiActionMinMs)
        {
            int tmp = DelaiActionMinMs; DelaiActionMinMs = DelaiActionMaxMs; DelaiActionMaxMs = tmp;
        }
    }

    private static readonly JsonSerializerOptions OptionsJson = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
