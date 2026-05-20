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
}

/// <summary>Cible du sort (= dyshay HechizoFocus + extensions SynFus UI).</summary>
public enum FocusSort
{
    EnnemiLePlusProche,
    EnnemiLePlusFaible,
    EnnemiLePlusFort,
    Moi,
    AllieLePlusBlesse,
    CelluleVide
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
