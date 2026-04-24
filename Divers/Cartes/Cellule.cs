using System;

namespace BotDofus.Divers.Cartes;

/// <summary>
/// Représente une cellule de la grille de combat/carte Dofus Retro.
/// Les cartes standards font 14 × 17 en pattern losange, soit 560 cellules.
///
/// Le système de coordonnées Dofus est particulier : l'index linéaire <see cref="Identifiant"/>
/// (0..559) se convertit en coordonnées (x, y) suivant la formule officielle :
///   x = (id % 14) + (id / 28) * 14  // colonne isométrique
///   y = (id / 14) - (id / 28) * 14  // ligne isométrique
/// </summary>
public sealed class Cellule
{
    public Cellule(int identifiant, TypesCellule type)
    {
        Identifiant = identifiant;
        Type = type;
        (X, Y) = CalculerCoordonnees(identifiant);
    }

    public int Identifiant { get; }
    public TypesCellule Type { get; set; }
    public int X { get; }
    public int Y { get; }

    public bool EstMarchable => Type == TypesCellule.Marchable || Type == TypesCellule.Interactif;
    public bool EstInteractif => Type == TypesCellule.Interactif
                               || Type == TypesCellule.Zaap
                               || Type == TypesCellule.Zaapi
                               || Type == TypesCellule.Transition;

    /// <summary>Distance de Chebyshev (déplacement en diagonale libre) entre deux cellules.</summary>
    public int DistanceChebyshev(Cellule autre)
        => Math.Max(Math.Abs(X - autre.X), Math.Abs(Y - autre.Y));

    /// <summary>Distance de Manhattan (Dofus : pas de diagonale pour marcher).</summary>
    public int DistanceManhattan(Cellule autre)
        => Math.Abs(X - autre.X) + Math.Abs(Y - autre.Y);

    public static (int x, int y) CalculerCoordonnees(int identifiant)
    {
        // Formule Dofus Retro (grid isométrique 14 × 20 en losange) :
        // Chaque "band" de 28 cellules alterne 14 pairs puis 14 impairs.
        int x = (identifiant % 14) + (identifiant / 28) * 14;
        int y = (identifiant / 14) - (identifiant / 28) * 14;
        return (x, y);
    }

    public static int CoordonneesVersId(int x, int y)
    {
        // Inverse de la formule ci-dessus.
        int bande = (x - y) / 14;
        int pos = (y % 14) + ((x - bande * 14) % 14);
        return bande * 28 + (y % 14) * 14 + pos;
    }

    public override string ToString() => $"Cellule #{Identifiant} ({X},{Y}) {Type}";
}
