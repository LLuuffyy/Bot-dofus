using BotDofus.Divers.Cartes;
using BotDofus.Divers.Cartes.Deplacement;
using Xunit;

namespace BotDofus.Tests;

public class PathfinderTests
{
    /// <summary>
    /// Crée une carte vide 15×17 (560 cells marchables) sans obstacle pour les tests.
    /// </summary>
    private static Carte CarteVide()
    {
        // Constructeur Carte(id, largeur, hauteur, nbCellules) crée 560 cellules
        // Marchables par défaut — parfait pour des tests sans obstacles.
        return new Carte(identifiant: 1, largeur: 15, hauteur: 17, nombreCellules: 560);
    }

    /// <summary>
    /// Sanity check : pathfinder retourne le départ si arrivée == départ.
    /// </summary>
    [Fact]
    public void Trouver_DepartEgalArrivee_RetourneCheminMonopas()
    {
        var carte = CarteVide();
        var depart = carte.Obtenir(100);
        var arrivee = carte.Obtenir(100);
        Assert.NotNull(depart);
        Assert.NotNull(arrivee);

        var chemin = Pathfinder.Trouver(carte!, depart!, arrivee!);

        Assert.NotNull(chemin);
        // Le chemin contient au moins le départ.
        Assert.True(chemin!.Count >= 1);
    }

    /// <summary>
    /// Combat (4-dir) : le chemin ne contient JAMAIS de mouvement diagonal.
    /// Test régression du commit G.4 (dyshay PeleasPathfinder no diagonales).
    /// </summary>
    [Fact]
    public void Trouver_ModeCombat_PasDeDiagonale()
    {
        var carte = CarteVide();
        var depart = carte.Obtenir(100);
        var arrivee = carte.Obtenir(150);
        Assert.NotNull(depart);
        Assert.NotNull(arrivee);

        var chemin = Pathfinder.Trouver(carte!, depart!, arrivee!, combat: true);

        Assert.NotNull(chemin);
        // Pour 2 cells consécutives du chemin, vérifier que (dx, dy) est ortho
        // (un des 2 = 0). En 4-dir strict, jamais (1,1) ou (-1,-1).
        for (int i = 1; i < chemin!.Count; i++)
        {
            int dx = System.Math.Abs(chemin[i].X - chemin[i - 1].X);
            int dy = System.Math.Abs(chemin[i].Y - chemin[i - 1].Y);
            Assert.True(dx == 0 || dy == 0,
                $"Chemin combat contient diagonale entre cell {chemin[i - 1].Identifiant} et {chemin[i].Identifiant} (dx={dx}, dy={dy})");
        }
    }

    /// <summary>
    /// Cellule de départ null → retourne null sans crash.
    /// </summary>
    [Fact]
    public void Trouver_DepartNull_RetourneNull()
    {
        var carte = CarteVide();
        var arrivee = carte.Obtenir(100);
        Assert.NotNull(arrivee);

        var chemin = Pathfinder.Trouver(carte!, null!, arrivee!);

        Assert.Null(chemin);
    }
}
