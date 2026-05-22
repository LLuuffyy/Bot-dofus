using System.Collections.Generic;
using BotDofus.Divers.Combats.IA;
using BotDofus.Divers.Combats.Combattants;
using Xunit;

namespace BotDofus.Tests;

/// <summary>
/// ADR-008 — tests unitaires du moteur tactique de positionnement IA combat.
/// Couvre <see cref="ScorePositionCombat.DistanceIdeale"/> (kite intelligent),
/// <see cref="ScorePositionCombat.ScoreCelluleAvance"/> (multi-critères) et
/// les heuristiques multi-mobs.
///
/// Note : les tests d'intégration avec <see cref="MoteurTactique.CalculerMeilleureCellule"/>
/// nécessitent un mock complet de Carte (479 cellules iso) — voir
/// <c>docs/SCENARIOS-TEST-TACTIQUE.md</c> §A-G pour les specs.
/// </summary>
public class MoteurTactiqueTests
{
    // ====================================================================
    //  DistanceIdeale — kite intelligent (cœur ADR-008)
    // ====================================================================

    [Fact]
    public void DistanceIdeale_AgressifCAC_RetourneUn()
    {
        var ctx = new ScorePositionCombat.ContexteTactique(
            Mode: ModeCombat.Agressif, PorteeMinSort: 1, PorteeMaxSort: 1,
            SortNecessiteLOS: false, PmEnnemiCible: 3,
            DistancePreferee: 5, DistanceMinEloigne: 6);
        Assert.Equal(1, ScorePositionCombat.DistanceIdeale(ctx));
    }

    [Fact]
    public void DistanceIdeale_AgressifSortDistance_RetourneUn()
    {
        // Mode Agressif veut le CAC même si le sort tire loin.
        var ctx = new ScorePositionCombat.ContexteTactique(
            Mode: ModeCombat.Agressif, PorteeMinSort: 1, PorteeMaxSort: 8,
            SortNecessiteLOS: false, PmEnnemiCible: 3,
            DistancePreferee: 5, DistanceMinEloigne: 6);
        Assert.Equal(1, ScorePositionCombat.DistanceIdeale(ctx));
    }

    [Fact]
    public void DistanceIdeale_EloignePMEnnemi3_RetournePorteeMaxPlus3()
    {
        // CŒUR DU KITE INTELLIGENT : distance = porteeMax + PM_ennemi
        var ctx = new ScorePositionCombat.ContexteTactique(
            Mode: ModeCombat.Eloigne, PorteeMinSort: 1, PorteeMaxSort: 8,
            SortNecessiteLOS: false, PmEnnemiCible: 3,
            DistancePreferee: 5, DistanceMinEloigne: 6);
        Assert.Equal(11, ScorePositionCombat.DistanceIdeale(ctx));
    }

    [Fact]
    public void DistanceIdeale_EloignePMEnnemi0_FallbackConservatif3()
    {
        // Si PM ennemi inconnu (= 0), on suppose 3 PM conservateur
        // (mob lvl ~10 standard).
        var ctx = new ScorePositionCombat.ContexteTactique(
            Mode: ModeCombat.Eloigne, PorteeMinSort: 1, PorteeMaxSort: 8,
            SortNecessiteLOS: false, PmEnnemiCible: 0,
            DistancePreferee: 5, DistanceMinEloigne: 6);
        Assert.Equal(11, ScorePositionCombat.DistanceIdeale(ctx));
    }

    [Fact]
    public void DistanceIdeale_EloigneCraPortee12_PmEnnemi5_Retourne17()
    {
        // Cra avec Flèche Punitive niv 5 (portée 6-12), ennemi très mobile.
        var ctx = new ScorePositionCombat.ContexteTactique(
            Mode: ModeCombat.Eloigne, PorteeMinSort: 6, PorteeMaxSort: 12,
            SortNecessiteLOS: false, PmEnnemiCible: 5,
            DistancePreferee: 10, DistanceMinEloigne: 8);
        Assert.Equal(17, ScorePositionCombat.DistanceIdeale(ctx));
    }

    [Fact]
    public void DistanceIdeale_Fuyard_MemeFormuleQueEloigne()
    {
        var ctx = new ScorePositionCombat.ContexteTactique(
            Mode: ModeCombat.Fuyard, PorteeMinSort: 1, PorteeMaxSort: 6,
            SortNecessiteLOS: false, PmEnnemiCible: 4,
            DistancePreferee: 4, DistanceMinEloigne: 6);
        Assert.Equal(10, ScorePositionCombat.DistanceIdeale(ctx));
    }

    [Fact]
    public void DistanceIdeale_EquilibreDistPrefDansPortee_RetourneDistPref()
    {
        var ctx = new ScorePositionCombat.ContexteTactique(
            Mode: ModeCombat.Equilibre, PorteeMinSort: 1, PorteeMaxSort: 8,
            SortNecessiteLOS: false, PmEnnemiCible: 3,
            DistancePreferee: 6, DistanceMinEloigne: 4);
        Assert.Equal(6, ScorePositionCombat.DistanceIdeale(ctx));
    }

    [Fact]
    public void DistanceIdeale_EquilibreDistPrefHorsPortee_ClampPorteeMax()
    {
        // distPref=12 mais portée 1-8 → clamp à 8.
        var ctx = new ScorePositionCombat.ContexteTactique(
            Mode: ModeCombat.Equilibre, PorteeMinSort: 1, PorteeMaxSort: 8,
            SortNecessiteLOS: false, PmEnnemiCible: 3,
            DistancePreferee: 12, DistanceMinEloigne: 4);
        Assert.Equal(8, ScorePositionCombat.DistanceIdeale(ctx));
    }

    [Fact]
    public void DistanceIdeale_EquilibreDistPref0_ClampPorteeMin()
    {
        // distPref=0 mais portéeMin=1 → clamp à 1.
        var ctx = new ScorePositionCombat.ContexteTactique(
            Mode: ModeCombat.Equilibre, PorteeMinSort: 1, PorteeMaxSort: 8,
            SortNecessiteLOS: false, PmEnnemiCible: 3,
            DistancePreferee: 0, DistanceMinEloigne: 4);
        Assert.Equal(1, ScorePositionCombat.DistanceIdeale(ctx));
    }

    // ====================================================================
    //  ScoreCelluleAvance — scoring multi-critères
    // ====================================================================

    private static ScorePositionCombat.ContexteTactique CtxCra() => new(
        Mode: ModeCombat.Eloigne, PorteeMinSort: 1, PorteeMaxSort: 8,
        SortNecessiteLOS: true, PmEnnemiCible: 3,
        DistancePreferee: 5, DistanceMinEloigne: 6);

    [Fact]
    public void ScoreCelluleAvance_CellulePileADistanceIdeale_ScoreFaible()
    {
        // Cra ennemi cell (0,0), cellule candidate (0,11) → dist=11=distIdeale
        var ennemisXY = new (int, int)[] { (0, 0) };
        var ctx = CtxCra();

        double score = ScorePositionCombat.ScoreCelluleAvance(
            xCell: 0, yCell: 11, ennemisXY, cibleXY: (0, 0), ctx, losOk: true);

        // Distance idéale = 11, distCible = 11 → écart=0
        // Pas de pénalité LOS (true)
        // dist=11 > porteeMax=8 → pénalité hors portée 500×3 = 1500
        // Σdist=11, Eloigne → bonus -11
        Assert.InRange(score, 1488, 1492); // 1500 - 11 + epsilon
    }

    [Fact]
    public void ScoreCelluleAvance_LosBloqueeSortLOS_PenaliteForte()
    {
        var ennemisXY = new (int, int)[] { (0, 0) };
        var ctx = CtxCra();
        double scoreOk = ScorePositionCombat.ScoreCelluleAvance(
            xCell: 0, yCell: 8, ennemisXY, cibleXY: (0, 0), ctx, losOk: true);
        double scoreKo = ScorePositionCombat.ScoreCelluleAvance(
            xCell: 0, yCell: 8, ennemisXY, cibleXY: (0, 0), ctx, losOk: false);
        Assert.Equal(1000, scoreKo - scoreOk);
    }

    [Fact]
    public void ScoreCelluleAvance_HorsPorteeMax_PenaliteParCase()
    {
        var ennemisXY = new (int, int)[] { (0, 0) };
        var ctx = new ScorePositionCombat.ContexteTactique(
            Mode: ModeCombat.Equilibre, PorteeMinSort: 1, PorteeMaxSort: 8,
            SortNecessiteLOS: false, PmEnnemiCible: 0,
            DistancePreferee: 5, DistanceMinEloigne: 4);
        double sc8 = ScorePositionCombat.ScoreCelluleAvance(
            xCell: 0, yCell: 8, ennemisXY, cibleXY: (0, 0), ctx, losOk: true);
        double sc10 = ScorePositionCombat.ScoreCelluleAvance(
            xCell: 0, yCell: 10, ennemisXY, cibleXY: (0, 0), ctx, losOk: true);
        // 10 - 8 = 2 cases hors portée → +500 × 2 = +1000
        // De plus, distCible passe de 8 (=distIdeale 5 clampé à 8... wait 5 dans la portée donc distIdeale=5)
        // Recalcul : distIdeale = clamp(5, 1, 8) = 5
        // sc8 : |8-5|×10 = 30, hors portée 0
        // sc10 : |10-5|×10 = 50, hors portée 2×500 = 1000
        Assert.True(sc10 > sc8 + 900); // gap d'au moins 900 (la pénalité hors portée)
    }

    // ====================================================================
    //  Helpers SommeDistances / DistanceMin
    // ====================================================================

    [Fact]
    public void SommeDistances_TroisEnnemis_CalculCorrect()
    {
        var ennemis = new (int, int)[] { (0, 0), (5, 0), (0, 5) };
        // Depuis (0,0) : dist=0, 5, 5 → somme=10
        int s = ScorePositionCombat.SommeDistances(0, 0, ennemis);
        Assert.Equal(10, s);
    }

    [Fact]
    public void DistanceMin_TroisEnnemis_RetourneLePlusProche()
    {
        var ennemis = new (int, int)[] { (10, 0), (3, 0), (5, 5) };
        // Depuis (0,0) : dist=10, 3, 5 → min=3
        int m = ScorePositionCombat.DistanceMin(0, 0, ennemis);
        Assert.Equal(3, m);
    }

    [Fact]
    public void DistanceMin_AucunEnnemi_RetourneMaxValue()
    {
        var ennemis = new (int, int)[] { };
        Assert.Equal(int.MaxValue, ScorePositionCombat.DistanceMin(0, 0, ennemis));
    }

    // ====================================================================
    //  CoordsEnnemis — extraction depuis combattants
    // ====================================================================

    [Fact]
    public void CoordsEnnemis_DeuxCombattants_TailleCorrecte()
    {
        // mapWidth=15 (Hystoria). On vérifie juste que la conversion produit
        // 2 entrées (la formule iso CalculerCoordonnees est testée ailleurs
        // dans CelluleTests).
        var ennemis = new List<Combattant>
        {
            new CombattantMonstre { Identifiant = -1, CellulePosition = 100 },
            new CombattantMonstre { Identifiant = -2, CellulePosition = 200 },
        };
        var xy = ScorePositionCombat.CoordsEnnemis(ennemis, 15);
        Assert.Equal(2, xy.Length);
    }
}
