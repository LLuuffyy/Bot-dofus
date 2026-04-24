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
    }

    public Cellule? Obtenir(int identifiantCellule)
        => identifiantCellule >= 0 && identifiantCellule < Cellules.Length
            ? Cellules[identifiantCellule]
            : null;

    /// <summary>Retourne les voisins à 1 case (4-connexité Dofus : haut/bas/gauche/droite).</summary>
    public IEnumerable<Cellule> Voisins(Cellule centre)
    {
        foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
        {
            var nx = centre.X + dx;
            var ny = centre.Y + dy;
            var id = Cellule.CoordonneesVersId(nx, ny);
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
}
