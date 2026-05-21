using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using BotDofus.Divers.Jeu.Personnage.Spells;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Combats.IA;

/// <summary>
/// Configuration combat persistée par perso. Modèle SynFus/dyshay complet :
/// stratégie globale, positionnement, règles de sorts, consommable de soin.
/// Persistée en JSON dans peleas/&lt;perso&gt;.json.
/// </summary>
public sealed class ConfigCombat
{
    // === Onglet « General » SynFus ===

    /// <summary>
    /// Mode de combat = profil de POSITIONNEMENT pendant le combat (cf. ADR-001).
    /// 4 valeurs : Agressif (CAC) / Eloigne (max portée) / Fuyard (max dist + fuite)
    /// / Equilibre (tient DistancePreferee, défaut). Distinct de Strategie qui
    /// reste un profil style/sorts.
    /// </summary>
    public ModeCombat Mode { get; set; } = ModeCombat.Equilibre;

    /// <summary>Positionnement en début de combat (déplacement initial).</summary>
    public PositionnementCombat Positionnement { get; set; } = PositionnementCombat.PasDeDeplacement;

    /// <summary>Style de jeu (Tactique = priorité distance, Agressif = priorité dégâts).</summary>
    public StrategieCombat Strategie { get; set; } = StrategieCombat.Tactique;

    /// <summary>Distance préférée à maintenir avec l'ennemi (pour le mode Tactique).</summary>
    public int DistancePreferee { get; set; } = 5;

    /// <summary>« Bloquer le combat » : si true, empêche les ennemis de passer derrière nous.</summary>
    public bool BloquerLeCombat { get; set; } = false;

    /// <summary>Désactiver le mode spectateur (= dyshay desactivar_espectador).</summary>
    public bool DesactiverModeSpectateur { get; set; } = false;

    /// <summary>Utiliser la monture (dragodinde) en combat (= dyshay utilizar_dragopavo).</summary>
    public bool UtiliserMonture { get; set; } = false;

    // === Onglet « Sorts » SynFus ===

    /// <summary>
    /// Liste ORDONNÉE des règles de sorts (l'ordre = priorité d'essai, du haut
    /// vers le bas). En plus, chaque règle a son champ <see cref="RegleSort.Priorite"/>
    /// pour fine-tuning ; mais en pratique l'ordre suffit.
    /// </summary>
    public List<RegleSort> Regles { get; set; } = new();

    // === Onglet « Consommable de soin » SynFus ===

    /// <summary>ID du template d'objet à utiliser pour se soigner (0 = aucun).</summary>
    public int ConsommableSoinIdTemplate { get; set; } = 0;

    /// <summary>Nom affiché (pour l'UI), pas critique pour la logique.</summary>
    public string ConsommableSoinNom { get; set; } = string.Empty;

    /// <summary>Utiliser le consommable si PV ≤ X% (= dyshay iniciar_regeneracion).</summary>
    public int ConsommableUtiliserSiPvInfPct { get; set; } = 50;

    /// <summary>Jusqu'à Y% PV (= dyshay detener_regeneracion).</summary>
    public int ConsommableJusquaPvSupPct { get; set; } = 100;

    /// <summary>Délai min (ms) entre deux utilisations du consommable.</summary>
    public int ConsommableDelaiMinMs { get; set; } = 150;

    /// <summary>Délai max (ms) entre deux utilisations du consommable.</summary>
    public int ConsommableDelaiMaxMs { get; set; } = 400;

    // === Cellule de placement préférée (legacy) ===
    /// <summary>Cellule de placement préférée en début de combat (-1 = auto).</summary>
    public int CellulePlacementPrefere { get; set; } = -1;

    // === Fuite ===
    /// <summary>Si true, fuit dès que PV &lt; SeuilFuitePv% (mode FUGITIVA).</summary>
    public bool FuirSiPvBas { get; set; } = false;

    /// <summary>Seuil PV (0-100) pour déclencher la fuite.</summary>
    public int SeuilFuitePv { get; set; } = 20;

    /// <summary>Délai entre actions IA combat (ms) — humanise les casts.</summary>
    public int DelaiEntreActionsMs { get; set; } = 800;

    /// <summary>
    /// FLAG ROLLOUT — si <c>true</c>, en cas de <see cref="ResultatDeplacementCombat.TimeoutSilencieux"/>
    /// (aucun broadcast GA;0/1 reçu après envoi GA001) le bot continue quand
    /// même le cast en mode optimistic (= comportement pré-ADR-002).
    /// Si <c>false</c>, le bot passe son tour (Gt direct, plus safe).
    /// Par défaut <c>false</c> (changé 21/05 matin) : le serveur Hystoria
    /// rejetait silencieusement certains GA001 (cell d'arrivée invalide,
    /// tacle subi, etc.) et le mode secours faisait croire au bot qu'il
    /// avait bougé → cast hors portée au tour suivant (12 cases vs portée 8,
    /// bug user log 063241).
    /// </summary>
    public bool ModeDeplacementOptimisteSecours { get; set; } = false;

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
            Journaliseur.Info(
                $"[CFG-COMBAT] Chargé : style={cfg.Strategie}, placement={cfg.Positionnement}, "
                + $"distance={cfg.DistancePreferee}, {cfg.Regles.Count} règles, "
                + $"soin={(cfg.ConsommableSoinIdTemplate > 0 ? cfg.ConsommableSoinNom : "aucun")}");
            return cfg;
        }
        catch (System.Exception ex)
        {
            Journaliseur.Avertir($"[CFG-COMBAT] Erreur chargement {cheminFichier} : {ex.Message}, config par défaut");
            return new ConfigCombat();
        }
    }

    /// <summary>
    /// Génère une config par défaut depuis les sorts offensifs appris (scan SL).
    /// Pour chaque sort offensif, crée une règle "EnnemiLePlusProche" / méthode
    /// LesDeux / 1 cast par tour. L'utilisateur peut ensuite affiner via l'UI.
    /// </summary>
    public static ConfigCombat GenererParDefaut(IEnumerable<int> sortsApprisIds)
    {
        var cfg = new ConfigCombat { Strategie = StrategieCombat.Tactique };
        var offensifs = BaseSorts.Instance.SortsOffensifs(sortsApprisIds);
        int prio = 100;
        foreach (var s in offensifs)
        {
            cfg.Regles.Add(new RegleSort
            {
                IdSort = s.Identifiant,
                Nom = s.Nom,
                Focus = FocusSort.EnnemiLePlusProche,
                NombreParTour = 1,
                MethodeLancement = MethodeLancement.LesDeux,
                Priorite = prio,
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

/// <summary>
/// Positionnement en début de combat (= dyshay PosicionamientoInicioPelea).
/// </summary>
public enum PositionnementCombat
{
    /// <summary>Pas de déplacement initial (= dyshay INMOVIL).</summary>
    PasDeDeplacement,
    /// <summary>Se rapprocher des ennemis (= dyshay CERCA_DE_ENEMIGOS).</summary>
    PresDesEnnemis,
    /// <summary>S'éloigner des ennemis (= dyshay LEJOS_DE_ENEMIGOS).</summary>
    LoinDesEnnemis
}
