namespace BotDofus.Divers.Cartes;

/// <summary>
/// Types d'une cellule de carte Dofus Retro. Le champ 'mouv' de chaque cellule
/// dans les données cartes encode ces états : marchable, obstacle, interactive,
/// zaap, porte, etc.
/// </summary>
public enum TypesCellule
{
    /// <summary>Cellule marchable classique (herbe, route, parquet).</summary>
    Marchable = 0,

    /// <summary>Obstacle infranchissable (mur, rocher, bâtiment).</summary>
    Obstacle = 1,

    /// <summary>Élément interactif (ressource, porte, levier, zaap).</summary>
    Interactif = 2,

    /// <summary>Zone de changement de carte (bord écran, téléport vers carte voisine).</summary>
    Transition = 3,

    /// <summary>Zaap (transport rapide).</summary>
    Zaap = 4,

    /// <summary>Zaapi (transport interne à une zone).</summary>
    Zaapi = 5,

    /// <summary>Ligne de vue seulement (archer peut tirer, mais pas marcher).</summary>
    LignDeVueSeule = 6
}
