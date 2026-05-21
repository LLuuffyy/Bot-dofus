namespace BotDofus.Divers.Combats.IA;

/// <summary>
/// Règle déclarative pour un sort en combat. Modèle aligné sur dyshay
/// (HechizoPelea) + extensions SynFus (priorité explicite, max par cible,
/// conditions situation/avancées). Persisté en JSON dans peleas/&lt;perso&gt;.json.
///
/// Mapping dyshay : id, nombre, focus, lanzamientos_x_turno, metodo_lanzamiento.
/// Mapping SynFus UI : ID, Name, Focus, « Nombre x par tours », « Lancement ».
/// </summary>
public sealed class RegleSort
{
    /// <summary>Identifiant du sort.</summary>
    public int IdSort { get; set; }

    /// <summary>Nom affiché (cache, retrouvable depuis BaseSorts).</summary>
    public string Nom { get; set; } = string.Empty;

    /// <summary>Sur QUI lance-t-on le sort ? (= dyshay HechizoFocus)</summary>
    public FocusSort Focus { get; set; } = FocusSort.EnnemiLePlusProche;

    /// <summary>Nombre maximum de lancers par tour (= dyshay lanzamientos_x_turno).</summary>
    public int NombreParTour { get; set; } = 1;

    /// <summary>Nombre maximum de lancers par CIBLE par tour (extension SynFus).</summary>
    public int NombreParCible { get; set; } = 0;

    /// <summary>Méthode de lancement = dyshay MetodoLanzamiento</summary>
    public MethodeLancement MethodeLancement { get; set; } = MethodeLancement.LesDeux;

    // -----------------------------------------------------------------
    // Compat ascendante (ancien modèle) — utilisés par DecideurCombat/ApiLua.
    // À terme, ces stats devraient être lues depuis BaseSorts.Instance.Trouver(IdSort).Stats(niv)
    // mais on les garde redondants pour faciliter la transition.
    // -----------------------------------------------------------------
    public int CoutPA { get; set; }
    public int PorteeMin { get; set; } = 1;
    public int PorteeMax { get; set; } = 6;

    /// <summary>Compat : alias direct de <see cref="Focus"/> sous l'ancien nom.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public CibleSort Cible
    {
        get => Focus switch
        {
            FocusSort.EnnemiLePlusProche => CibleSort.EnnemiPlusProche,
            FocusSort.EnnemiLePlusFaible => CibleSort.EnnemiPlusFaible,
            FocusSort.EnnemiLePlusFort   => CibleSort.EnnemiPlusFort,
            FocusSort.Moi                => CibleSort.Soi,
            FocusSort.AllieLePlusBlesse  => CibleSort.AlliePlusBlesse,
            _                            => CibleSort.EnnemiPlusProche
        };
        set => Focus = value switch
        {
            CibleSort.EnnemiPlusProche  => FocusSort.EnnemiLePlusProche,
            CibleSort.EnnemiPlusFaible  => FocusSort.EnnemiLePlusFaible,
            CibleSort.EnnemiPlusFort    => FocusSort.EnnemiLePlusFort,
            CibleSort.Soi               => FocusSort.Moi,
            CibleSort.AlliePlusBlesse   => FocusSort.AllieLePlusBlesse,
            _                           => FocusSort.EnnemiLePlusProche
        };
    }

    /// <summary>Compat : seuils PV anciens (synonymes des nouveaux *PvInfPourcent).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public int? SeuilPvAllie { get => CiblePvInfPourcent; set => CiblePvInfPourcent = value; }
    [System.Text.Json.Serialization.JsonIgnore]
    public int? SeuilPvSoi   { get => MesPvInfPourcent;   set => MesPvInfPourcent   = value; }

    /// <summary>Compat : recast si échec (ancien flag).</summary>
    public bool RecastSiEchec { get; set; } = true;

    /// <summary>
    /// Priorité décroissante (un nombre plus élevé = essayé en premier).
    /// L'ordre dans la liste reflète aussi la priorité dans l'UI SynFus.
    /// </summary>
    public int Priorite { get; set; } = 5;

    // -----------------------------------------------------------------
    // Conditions de distance (= bloc "Distance" SynFus)
    // -----------------------------------------------------------------
    public int? DistanceMin { get; set; }
    public int? DistanceMax { get; set; }
    public bool IgnorerCAC { get; set; }
    public bool SeulementCAC { get; set; }
    public int? EviterZone { get; set; }

    // -----------------------------------------------------------------
    // Conditions de cible (= bloc "Cible" SynFus)
    // -----------------------------------------------------------------
    public int? CiblePvInfPourcent { get; set; }
    public int? CiblePvSupPourcent { get; set; }
    public bool CiblePlusFaible { get; set; }
    public bool CiblePlusForte { get; set; }

    // -----------------------------------------------------------------
    // Conditions joueur (= bloc "Joueur" SynFus)
    // -----------------------------------------------------------------
    public int? MesPvInfPourcent { get; set; }
    public int? MesPvSupPourcent { get; set; }
    public bool PasSiTacle { get; set; }
    public bool SiInvocPresente { get; set; }

    // -----------------------------------------------------------------
    // Conditions situation (= bloc "Situation" SynFus)
    // -----------------------------------------------------------------
    public int? EnnemisMin { get; set; }
    public int? EnnemisMax { get; set; }
    public bool PremierTour { get; set; }
    public bool DernierTour { get; set; }

    // -----------------------------------------------------------------
    // Conditions avancées (= bloc "Avancé" SynFus)
    // -----------------------------------------------------------------
    public int? TousLesNTours { get; set; }
    public int? APartirDuTour { get; set; }
    public ElementSort ElementRequis { get; set; } = ElementSort.Aucun;
    public int? SeuilCritiqueInfPct { get; set; }
    public int? SeuilCritiqueSupPct { get; set; }

    /// <summary>
    /// Cooldown en TOURS entre 2 lancers de ce sort. 0 = pas de cooldown (= dyshay
    /// hechizos_intervalo). Ex. Sadida « Sacrifice Poupesque » cooldown 5 tours.
    /// Combiné à NombreParTour (1 = 1 cast/tour) et NombreParCible (jamais 2× sur
    /// la même cible dans le même tour).
    /// </summary>
    public int CooldownTours { get; set; } = 0;
}

/// <summary>Cible du sort (= dyshay HechizoFocus + extensions SynFus UI).</summary>
public enum FocusSort
{
    // === ENNEMIS ===
    EnnemiLePlusProche,
    EnnemiLePlusFaible,
    EnnemiLePlusFort,
    /// <summary>Ennemi le plus éloigné (utile sort à portée min, ex. Tir Lourd Cra).</summary>
    EnnemiLePlusLoin,

    // === SOI / ALLIÉS ===
    Moi,
    AllieLePlusBlesse,
    /// <summary>Allié le plus proche (heal CC ou buff ami).</summary>
    AllieLePlusProche,
    /// <summary>Allié avec le plus de PV manquants (heal optimal).</summary>
    AlliePlusGrosHeal,

    // === INVOCATIONS ALLIÉES ===
    /// <summary>Mes invocations les plus blessées (heal poupée Sadida).</summary>
    InvocationLaPlusBlessee,
    /// <summary>Mes invocations les plus proches (buff invocs adjacentes).</summary>
    InvocationLaPlusProche,

    // === CELLULES ===
    /// <summary>Cellule vide libre (invocation Sadida ex. La Folle). Legacy =
    /// première cellule vide adjacente trouvée, sans priorisation.</summary>
    CelluleVide,
    /// <summary>Cellule vide adjacente à l'ennemi le + proche (blocage avec La Bloqueuse).</summary>
    CelluleAdjacenteEnnemi,
    /// <summary>
    /// Cellule VIDE adjacente à MOI (Chebyshev=1), avec PRIORISATION :
    /// 1) cellule entre moi et l'ennemi le plus proche (bloque le chemin),
    /// 2) cellule du côté opposé à l'ennemi (protège l'invoc),
    /// 3) première cellule vide trouvée (fallback).
    /// Recommandé pour invocations Sadida (La Folle / Sacrifice Poupesque).
    /// </summary>
    CelluleAdjacenteMoi,
}

/// <summary>Méthode de lancement (= dyshay MetodoLanzamiento).</summary>
public enum MethodeLancement
{
    LesDeux,    // CAC + à distance
    CAC,        // adjacent à la cible uniquement
    Distance    // hors corps-à-corps uniquement
}

/// <summary>Élément du sort (filtre situation : si ennemi résiste à un élément).</summary>
public enum ElementSort
{
    Aucun,
    Force,
    Intelligence,
    Chance,
    Agilite,
    Neutre
}

// Compat ascendante — l'ancien CibleSort est mappé sur FocusSort.
[System.Obsolete("Utiliser FocusSort à la place")]
public enum CibleSort
{
    EnnemiPlusProche,
    EnnemiPlusFaible,
    EnnemiPlusFort,
    Soi,
    AlliePlusBlesse,
    PositionStrategique
}
