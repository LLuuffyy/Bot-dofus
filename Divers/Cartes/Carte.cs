using System;
using System.Collections.Generic;
using BotDofus.Divers.Cartes.Entites;

namespace BotDofus.Divers.Cartes;

/// <summary>
/// Carte Dofus Retro : 560 cellules (14 × 20 en losange standard, ou plus
/// pour certaines maps spéciales). Stocke la topologie, les entités présentes
/// (joueurs, monstres, PNJ) et les objets interactifs.
/// </summary>
public sealed class Carte
{
    public const int LargeurParDefaut = 14;
    public const int HauteurParDefaut = 20;
    public const int NombreCellulesParDefaut = 560;

    public int Identifiant { get; }
    public int Largeur { get; set; } = LargeurParDefaut;
    public int Hauteur { get; set; } = HauteurParDefaut;
    public Cellule[] Cellules { get; }
    public Dictionary<int, Entite> Entites { get; } = new();

    public event EventHandler? Rechargee;

    public Carte(int identifiant, int nombreCellules = NombreCellulesParDefaut)
    {
        Identifiant = identifiant;
        Cellules = new Cellule[nombreCellules];
        for (int i = 0; i < nombreCellules; i++)
        {
            Cellules[i] = new Cellule(i, TypesCellule.Marchable);
        }

        MarquerBordsCommeTransitions();
    }

    public Cellule? Obtenir(int identifiantCellule)
        => identifiantCellule >= 0 && identifiantCellule < Cellules.Length
            ? Cellules[identifiantCellule]
            : null;

    /// <summary>
    /// Retourne les voisins directs d'une cellule. En Dofus Retro les 4 directions
    /// visuelles (haut, bas, gauche, droite) correspondent aux décalages d'id
    /// ±1 et ±14 sur une grille linéaire simplifiée. Les 4 diagonales sont ±13
    /// et ±15. Pour le pathfinding principal on se limite aux 4 orthogonaux.
    /// </summary>
    public IEnumerable<Cellule> Voisins(Cellule centre)
    {
        foreach (var decalage in new[] { 1, -1, Largeur, -Largeur })
        {
            var id = centre.Identifiant + decalage;
            // Évite que id+1 déborde sur la ligne suivante.
            if (Math.Abs(decalage) == 1
                && (centre.Identifiant / Largeur) != (id / Largeur)) continue;
            var c = Obtenir(id);
            if (c != null) yield return c;
        }
    }

    /// <summary>Appliquer les types de cellules depuis la chaîne de mouvement décodée.</summary>
    public void AppliquerMouvements(ReadOnlySpan<int> codesMouvement)
    {
        int n = Math.Min(codesMouvement.Length, Cellules.Length);
        for (int i = 0; i < n; i++)
        {
            Cellules[i].Type = codesMouvement[i] switch
            {
                0 => TypesCellule.Obstacle,
                1 => TypesCellule.Marchable,
                2 => TypesCellule.LignDeVueSeule,
                4 => TypesCellule.Interactif,
                _ => TypesCellule.Marchable
            };
        }
        Rechargee?.Invoke(this, EventArgs.Empty);
    }

    private void MarquerBordsCommeTransitions()
    {
        if (Cellules.Length == 0) return;

        var minX = Cellules.Min(c => c.X);
        var maxX = Cellules.Max(c => c.X);
        var minY = Cellules.Min(c => c.Y);
        var maxY = Cellules.Max(c => c.Y);

        foreach (var cellule in Cellules)
        {
            if (cellule.X == minX || cellule.X == maxX || cellule.Y == minY || cellule.Y == maxY)
            {
                cellule.Type = TypesCellule.Transition;
            }
        }
    }
}
