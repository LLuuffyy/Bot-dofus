using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Banque;

/// <summary>
/// Configuration du dépôt banque automatique. Quand
/// <see cref="Jeu.Personnage.Personnage.PourcentagePoids"/> >= <see cref="SeuilPoidsPct"/>,
/// le bot interrompt le farm, se rend à <see cref="MapBanqueId"/>, ouvre la
/// banque via le coffre interactif (paquet <c>ApS</c> clair), dépose
/// les items selon les filtres (paquets <c>EMO+&lt;uid&gt;|&lt;qte&gt;</c> chiffré '-'),
/// puis ferme (<c>EV</c> chiffré '-') et reprend.
///
/// Persisté par perso dans <c>banque/&lt;perso&gt;.json</c>.
///
/// ⚠ PROTOCOLE confirmé par capture user 2026-05-21 (cf. docs/ANALYSE-FLOW-BANQUE.md).
/// </summary>
public sealed class ConfigBanque
{
    /// <summary>Active le dépôt auto (false = désactivé).</summary>
    public bool Active { get; set; } = false;

    /// <summary>Pourcentage de poids déclencheur (par défaut 80% — laisse marge avant FULL).</summary>
    public int SeuilPoidsPct { get; set; } = 80;

    /// <summary>Pourcentage de poids cible APRÈS dépôt (relâcher au moins jusqu'à X% — 30% par défaut).</summary>
    public int CiblePoidsPct { get; set; } = 30;

    /// <summary>ID de la map où se trouve la banque (ex. Astrub bank = 10303 confirmé Hystoria).</summary>
    public int MapBanqueId { get; set; } = 10303;

    /// <summary>GFX du PNJ banquier OU du coffre interactif (utile pour le repérer sur la map).</summary>
    public int GfxNpcBanquier { get; set; } = 65;

    // === FILTRES PAR CATÉGORIE (modèle dyshay/SynFus) ===
    /// <summary>Déposer les équipements (armes, armures, anneaux, capes…) — défaut FAUX (on garde).</summary>
    public bool DeposerEquipements { get; set; } = false;
    /// <summary>Déposer les ressources (blé, bois, plantes, minerais…) — défaut VRAI (récolte = à déposer).</summary>
    public bool DeposerRessources { get; set; } = true;
    /// <summary>Déposer les consommables (potions, pains, parchemins…) — défaut FAUX (utile en combat).</summary>
    public bool DeposerConsommables { get; set; } = false;
    /// <summary>Déposer les items de quête — défaut FAUX (toujours garder, sauf si user le veut).</summary>
    public bool DeposerQuetes { get; set; } = false;
    /// <summary>Déposer les items de catégorie inconnue — défaut FAUX (prudence).</summary>
    public bool DeposerInconnus { get; set; } = false;

    // === FILTRES PAR ITEM ===
    /// <summary>IDs template forcés à GARDER même si la catégorie est cochée (liste noire).</summary>
    public HashSet<int> IdsAGarder { get; set; } = new();

    /// <summary>IDs template forcés à DÉPOSER même si la catégorie est décochée (liste blanche).</summary>
    public HashSet<int> IdsADeposerForce { get; set; } = new();

    /// <summary>Nombre min à garder en inventaire par template (ex. pain id 312 => garder 50).</summary>
    public Dictionary<int, int> SeuilParTemplate { get; set; } = new();

    /// <summary>Tente une reconnexion vers la map de farm précédente après dépôt.</summary>
    public bool RetourFarmApresDepot { get; set; } = true;

    /// <summary>Delai min/max (ms) entre 2 actions de dépôt (humanisation).
    /// Capture user 2026-05-21 : 1.3–3.9s entre dépôts manuels.</summary>
    public int DelaiActionMinMs { get; set; } = 1500;
    public int DelaiActionMaxMs { get; set; } = 3000;

    // === COMPAT ASCENDANTE ===
    /// <summary>Compat : ancien champ — utiliser <see cref="IdsADeposerForce"/>.</summary>
    [JsonIgnore]
    public List<int> ItemsADeposer
    {
        get => IdsADeposerForce.ToList();
        set { IdsADeposerForce.Clear(); foreach (var i in value) IdsADeposerForce.Add(i); }
    }
    /// <summary>Compat : ancien champ — utiliser <see cref="IdsAGarder"/>.</summary>
    [JsonIgnore]
    public List<int> ItemsAGarder
    {
        get => IdsAGarder.ToList();
        set { IdsAGarder.Clear(); foreach (var i in value) IdsAGarder.Add(i); }
    }

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
                + $"catégories=[éq:{cfg.DeposerEquipements},rs:{cfg.DeposerRessources},"
                + $"cs:{cfg.DeposerConsommables},qu:{cfg.DeposerQuetes},in:{cfg.DeposerInconnus}], "
                + $"garde={cfg.IdsAGarder.Count}, dépose={cfg.IdsADeposerForce.Count}, "
                + $"seuils={cfg.SeuilParTemplate.Count}");
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

/// <summary>Catégorie d'un item Dofus Retro 1.29 (modèle dyshay/SynFus).</summary>
public enum CategorieObjet
{
    /// <summary>Armes, amulettes, anneaux, ceintures, bottes, capes, coiffes…</summary>
    Equipement,
    /// <summary>Matériaux récoltés (blé, bois, lin, peaux, poissons, minerais, plantes…).</summary>
    Ressource,
    /// <summary>Potions, pains, parchemins, boissons (= "MISCELLANEOUS" chez dyshay).</summary>
    Consommable,
    /// <summary>Items de quête (généralement à NE PAS déposer).</summary>
    Quete,
    /// <summary>Type non reconnu — par défaut on garde par prudence.</summary>
    Inconnu,
}

/// <summary>
/// Catégoriseur d'items basé sur le champ <c>"t"</c> (byte type) de
/// <c>items_merged.json</c>, mapping importé de
/// <c>dyshay-source/Otros/Game/Character/Inventory/InventoryUtilities.cs:76-150</c>.
/// </summary>
public static class CategoriseurObjet
{
    public static CategorieObjet Categoriser(int typeByte) => typeByte switch
    {
        // Équipements (armes/anneaux/amulettes/capes/coiffes/ceintures/bottes…).
        1 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9 or 10
            or 11 or 16 or 17 or 18 or 19 or 20 or 21 or 22 or 83
                                                  => CategorieObjet.Equipement,
        // Consommables (potions, parchemins, divers).
        12 or 13 or 33 or 85 or 86               => CategorieObjet.Consommable,
        // Ressources (matériaux récoltés).
        15 or 34 or 35 or 36 or 38 or 41 or 46 or 47 or 48 or 50
            or 51 or 53 or 54 or 55 or 56 or 57 or 58 or 59 or 60
            or 63 or 65 or 68 or 84 or 96 or 98
            or 100 or 103 or 104 or 105 or 106
            or 107 or 108 or 109 or 111          => CategorieObjet.Ressource,
        // Items de quête.
        24                                       => CategorieObjet.Quete,
        // Tout le reste → inconnu (par prudence on garde).
        _                                        => CategorieObjet.Inconnu,
    };
}
