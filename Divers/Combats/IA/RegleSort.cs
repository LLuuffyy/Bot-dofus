namespace BotDofus.Divers.Combats.IA;

/// <summary>
/// Règle déclarative décrivant l'usage souhaité d'un sort dans une rotation IA.
/// Chargée depuis la config par personnage (ex. peleas/&lt;perso&gt;.config) ou
/// déclarée en Lua via SHOW_FIGHT_COUNTER / configurations futures.
/// </summary>
public sealed class RegleSort
{
    /// <summary>Identifiant du sort dans <c>spells_hystoria.json</c>.</summary>
    public int IdSort { get; set; }

    /// <summary>Priorité décroissante (un nombre plus élevé = essayé en premier).</summary>
    public int Priorite { get; set; } = 5;

    /// <summary>Coût en PA du sort (utilisé pour déterminer combien de fois le caster).</summary>
    public int CoutPA { get; set; }

    /// <summary>Portée minimale du sort (en cases).</summary>
    public int PorteeMin { get; set; } = 1;

    /// <summary>Portée maximale (0 = pas de limite, lancée même au CaC).</summary>
    public int PorteeMax { get; set; } = 6;

    /// <summary>Si vrai, ne lance ce sort que si la cible n'a pas déjà subi un échec ce tour.</summary>
    public bool RecastSiEchec { get; set; } = true;

    /// <summary>Cible privilégiée : ennemi le plus proche, le plus faible, soi-même, etc.</summary>
    public CibleSort Cible { get; set; } = CibleSort.EnnemiPlusProche;

    /// <summary>Seuil de PV en pourcentage pour déclencher (utile pour soins / fuite).</summary>
    public int? SeuilPvAllie { get; set; }
    public int? SeuilPvSoi { get; set; }
}

/// <summary>Stratégies de ciblage pour <see cref="RegleSort"/>.</summary>
public enum CibleSort
{
    EnnemiPlusProche,
    EnnemiPlusFaible,
    EnnemiPlusFort,
    Soi,
    AlliePlusBlesse,
    PositionStrategique
}
