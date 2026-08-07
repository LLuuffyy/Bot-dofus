namespace BotDofus.Divers.Cartes.Entites;

/// <summary>
/// Base commune des entités vivantes ou interactives présentes sur une carte.
/// </summary>
public abstract class Entite
{
    public int Identifiant { get; set; }
    public int CellulePosition { get; set; }
    public string Nom { get; set; } = string.Empty;
    public bool EstMort { get; set; }

    public override string ToString() => $"{GetType().Name} #{Identifiant} « {Nom} » @ {CellulePosition}";
}

/// <summary>Joueur présent sur la carte.</summary>
public sealed class EntiteJoueur : Entite
{
    public int Niveau { get; set; }
    public int IdClasse { get; set; }
    public int Sexe { get; set; }
    public bool EstEnGroupe { get; set; }
}

/// <summary>Monstre (mob) ou groupe de monstres.</summary>
public sealed class EntiteMonstre : Entite
{
    public int IdGabarit { get; set; }
    public int NiveauGroupe { get; set; }
    public bool EstAgressif { get; set; }
    public int TailleGroupe { get; set; } = 1;

    /// <summary>
    /// Vrai si c'est un vrai groupe de mobs attaquable. Sur Dofus 1.29 les
    /// groupes mobs ont des IDs négatifs (-300, -302…) ; les IDs positifs sont
    /// joueurs/héros/PNJs (jamais attaquables via GA907).
    /// </summary>
    public bool EstGroupeAttaquable => Identifiant < 0;
}

/// <summary>PNJ (personnage non joueur).</summary>
public sealed class EntitePNJ : Entite
{
    public int IdGabarit { get; set; }
}

/// <summary>Élément interactif (ressource, porte, zaap).</summary>
public sealed class EntiteInteractif : Entite
{
    public int IdInteractif { get; set; }
    public int EtatBrut { get; set; }
    public bool EstDisponible { get; set; } = true;
}
