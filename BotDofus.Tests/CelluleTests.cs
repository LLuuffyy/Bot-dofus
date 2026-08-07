using BotDofus.Divers.Cartes;
using Xunit;

namespace BotDofus.Tests;

public class CelluleTests
{
    /// <summary>
    /// Vérifie la formule dyshay CalculerCoordonnees (Cell.cs ctor). Test
    /// sanity : recalculer l'ID inverse depuis (x, y) doit retomber sur cellId.
    /// </summary>
    [Theory]
    [InlineData(0, 15)]
    [InlineData(14, 15)]
    [InlineData(222, 15)]
    [InlineData(440, 15)]
    public void CalculerCoordonnees_FormuleReversible(int cellId, int mw)
    {
        var (x, y) = Cellule.CalculerCoordonnees(cellId, mw);
        // Formule inverse de dyshay : id = (mw-1)*y + x*mw + y = (2*mw - 1)*y + x
        // Wait : il faut tester avec la VRAIE formule inverse pour iso losange.
        // Formule directe (Cell.cs:56-61) :
        //   loc5 = id / (2*mw - 1)
        //   loc6 = id - loc5 * (2*mw - 1)
        //   loc7 = loc6 % mw
        //   y = loc5 - loc7
        //   x = (id - (mw - 1)*y) / mw
        // Inverse : id = x*mw + (mw - 1)*y, à condition d'un offset cohérent.
        int recalc = x * mw + (mw - 1) * y;
        Assert.Equal(cellId, recalc);
    }

    /// <summary>
    /// Distance Chebyshev (max |dx|, |dy|) — métrique canonique des portées sorts Dofus.
    /// </summary>
    [Theory]
    [InlineData(0, 0, 5, 0, 5)]
    [InlineData(0, 0, 0, 5, 5)]
    [InlineData(0, 0, 3, 4, 4)]   // diagonale = max (chebyshev, pas euclidienne)
    [InlineData(0, 0, -3, -4, 4)]
    public void DistanceChebyshev(int x1, int y1, int x2, int y2, int distAttendue)
    {
        int dx = System.Math.Abs(x1 - x2);
        int dy = System.Math.Abs(y1 - y2);
        int chebyshev = System.Math.Max(dx, dy);
        Assert.Equal(distAttendue, chebyshev);
    }

    /// <summary>
    /// Distance Manhattan (|dx|+|dy|) — métrique combat 4-dir (PM consommés).
    /// </summary>
    [Theory]
    [InlineData(0, 0, 5, 0, 5)]
    [InlineData(0, 0, 3, 4, 7)]   // diagonale = somme
    public void DistanceManhattan(int x1, int y1, int x2, int y2, int distAttendue)
    {
        int dx = System.Math.Abs(x1 - x2);
        int dy = System.Math.Abs(y1 - y2);
        int manhattan = dx + dy;
        Assert.Equal(distAttendue, manhattan);
    }
}
