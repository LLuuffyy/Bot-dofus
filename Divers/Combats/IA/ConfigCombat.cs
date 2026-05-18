using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using BotDofus.Divers.Jeu.Personnage.Spells;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Combats.IA;

/// <summary>
/// Configuration combat persistée par compte/personnage. Stocke la stratégie
/// globale et la liste ordonnée de règles de sorts à appliquer.
///
/// Sérialisée en JSON dans <c>peleas/&lt;perso&gt;.json</c>.
/// </summary>
public sealed class ConfigCombat
{
    /// <summary>Stratégie globale de l'IA.</summary>
    public StrategieCombat Strategie { get; set; } = StrategieCombat.Agressif;

    /// <summary>
    /// Liste ordonnée des règles de sorts. L'ordre N'EST PAS la priorité
    /// (on utilise <see cref="RegleSort.Priorite"/>) mais c'est l'ordre
    /// d'affichage dans l'UI.
    /// </summary>
    public List<RegleSort> Regles { get; set; } = new();

    /// <summary>Cellule de placement préférée en début de combat (-1 = première dispo).</summary>
    public int CellulePlacementPrefere { get; set; } = -1;

    /// <summary>Si true, fuit dès que PV &lt; SeuilFuitePv%.</summary>
    public bool FuirSiPvBas { get; set; } = false;

    /// <summary>Seuil PV (0-100) pour déclencher la fuite.</summary>
    public int SeuilFuitePv { get; set; } = 20;

    /// <summary>Délai entre actions (ms) — pour humaniser les casts.</summary>
    public int DelaiEntreActionsMs { get; set; } = 800;

    // ---------------------------------------------------------------
    // Sérialisation JSON
    // ---------------------------------------------------------------

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static ConfigCombat Charger(string cheminFichier)
    {
        if (!File.Exists(cheminFichier))
        {
            Journaliseur.Info($"[CFG-COMBAT] Fichier {cheminFichier} introuvable, config par défaut");
            return new ConfigCombat();
        }
        try
        {
            var json = File.ReadAllText(cheminFichier);
            var cfg = JsonSerializer.Deserialize<ConfigCombat>(json, Options) ?? new ConfigCombat();
            Journaliseur.Info($"[CFG-COMBAT] Chargé : strategie={cfg.Strategie}, {cfg.Regles.Count} règles");
            return cfg;
        }
        catch (System.Exception ex)
        {
            Journaliseur.Avertir($"[CFG-COMBAT] Erreur chargement {cheminFichier} : {ex.Message}, config par défaut");
            return new ConfigCombat();
        }
    }

    /// <summary>
    /// Génère une config de combat par défaut à partir des sorts OFFENSIFS
    /// réellement appris par le perso (scan SL). Règle par sort : cible =
    /// ennemi le plus proche, priorité = portée (les sorts longue portée
    /// d'abord), portées/PA repris de la base. À utiliser plus tard pour
    /// proposer une config auto dans l'UI — NON appliqué automatiquement.
    /// </summary>
    public static ConfigCombat GenererParDefaut(IEnumerable<int> sortsApprisIds)
    {
        var cfg = new ConfigCombat { Strategie = StrategieCombat.Agressif };
        var offensifs = BaseSorts.Instance.SortsOffensifs(sortsApprisIds);
        int prio = 100;
        foreach (var s in offensifs)
        {
            cfg.Regles.Add(new RegleSort
            {
                IdSort = s.Identifiant,
                Priorite = prio,
                CoutPA = s.CoutPA,
                PorteeMin = s.PorteeMin,
                PorteeMax = s.PorteeMax <= 0 ? 1 : s.PorteeMax,
                Cible = CibleSort.EnnemiPlusProche,
            });
            prio -= 5;
        }
        Journaliseur.Info(
            $"[CFG-COMBAT] Auto-générée : {cfg.Regles.Count} règle(s) offensive(s) "
            + $"depuis {sortsApprisIds.Count()} sort(s) appris.");
        return cfg;
    }

    public void Sauvegarder(string cheminFichier)
    {
        var dossier = Path.GetDirectoryName(cheminFichier);
        if (!string.IsNullOrEmpty(dossier)) Directory.CreateDirectory(dossier);
        var json = JsonSerializer.Serialize(this, Options);
        File.WriteAllText(cheminFichier, json);
        Journaliseur.Info($"[CFG-COMBAT] Sauvegardé : {cheminFichier}");
    }
}
