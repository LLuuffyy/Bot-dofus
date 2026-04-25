namespace BotDofus.Divers.Combats.IA;

/// <summary>
/// Action décidée par l'IA pour le tour courant. Sera traduite en messages
/// GA (déplacement / sort) par <see cref="DecideurCombat"/>.
/// </summary>
public abstract record ActionCombat
{
    /// <summary>Aucune action : on passe le tour.</summary>
    public sealed record PasserTour : ActionCombat;

    /// <summary>Se déplacer vers une cellule cible (consomme des PM).</summary>
    public sealed record SeDeplacer(int CelluleCible) : ActionCombat;

    /// <summary>Lancer un sort sur une cible (case ou combattant).</summary>
    public sealed record LancerSort(int IdSort, int CelluleCible) : ActionCombat;

    /// <summary>Utiliser un objet (potion, pain) sur soi.</summary>
    public sealed record UtiliserObjet(int IdObjet) : ActionCombat;
}
