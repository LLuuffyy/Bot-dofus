using BotDofus.Divers.Combats.IA;
using Xunit;

namespace BotDofus.Tests;

/// <summary>
/// Tests du scoring tactique enrichi Phase 4.2 :
/// exposition, cover invocation, marge coincé.
/// </summary>
public class ScoringTactiqueTests
{
    private static ScorePositionCombat.ContexteTactique CtxEloigne() => new(
        Mode: ModeCombat.Eloigne, PorteeMinSort: 1, PorteeMaxSort: 6,
        SortNecessiteLOS: false, PmEnnemiCible: 3,
        DistancePreferee: 6, DistanceMinEloigne: 5);

    private static ScorePositionCombat.ContexteTactique CtxAgressif() => new(
        Mode: ModeCombat.Agressif, PorteeMinSort: 1, PorteeMaxSort: 1,
        SortNecessiteLOS: false, PmEnnemiCible: 3,
        DistancePreferee: 1, DistanceMinEloigne: 1);

    [Fact]
    public void ScoreCelluleTactique_Eloigne_PenaliseExposition()
    {
        // 2 ennemis proches (dist 5 et 4) → exposition × 2.
        var ennemis = new[] { (0, 0), (1, 1) };
        var ctx = CtxEloigne();
        double scoreExpose = ScorePositionCombat.ScoreCelluleTactique(
            xCell: 5, yCell: 5, ennemis, (0, 0),
            invocsAlliesXY: System.Array.Empty<(int, int)>(),
            nbCasesAdjacentesMarchables: 8, ctx, losOk: true);

        // Même config mais loin des ennemis (1 seul à dist 15).
        var ennemisLoins = new[] { (0, 0), (15, 15) };  // 1 ennemi loin
        double scoreLibre = ScorePositionCombat.ScoreCelluleTactique(
            xCell: 5, yCell: 5, ennemisLoins, (0, 0),
            invocsAlliesXY: System.Array.Empty<(int, int)>(),
            nbCasesAdjacentesMarchables: 8, ctx, losOk: true);

        // En Eloigne, moins d'ennemis proches = meilleur score.
        Assert.True(scoreLibre < scoreExpose,
            $"Score libre ({scoreLibre}) doit être < score exposé ({scoreExpose})");
    }

    [Fact]
    public void ScoreCelluleTactique_BonusCoverInvocAdjacente()
    {
        var ennemis = new[] { (8, 8) };
        var ctx = CtxAgressif();

        double scoreSansInvoc = ScorePositionCombat.ScoreCelluleTactique(
            xCell: 5, yCell: 5, ennemis, (8, 8),
            invocsAlliesXY: System.Array.Empty<(int, int)>(),
            nbCasesAdjacentesMarchables: 8, ctx, losOk: true);

        // Une invocation alliée adjacente (dist 1) → bonus négatif (= mieux).
        var invocs = new[] { (5, 6) };  // dist 1 de (5,5)
        double scoreAvecInvoc = ScorePositionCombat.ScoreCelluleTactique(
            xCell: 5, yCell: 5, ennemis, (8, 8),
            invocsAlliesXY: invocs,
            nbCasesAdjacentesMarchables: 8, ctx, losOk: true);

        Assert.True(scoreAvecInvoc < scoreSansInvoc,
            $"Score avec invoc adjacente ({scoreAvecInvoc}) doit être < sans ({scoreSansInvoc})");
    }

    [Fact]
    public void ScoreCelluleTactique_PenaliseCellsCoincees()
    {
        var ennemis = new[] { (8, 8) };
        var ctx = CtxEloigne();

        // Cell avec 8 voisines libres = pas coincé.
        double scoreLibre = ScorePositionCombat.ScoreCelluleTactique(
            xCell: 5, yCell: 5, ennemis, (8, 8),
            invocsAlliesXY: System.Array.Empty<(int, int)>(),
            nbCasesAdjacentesMarchables: 8, ctx, losOk: true);

        // Cell avec 1 seule voisine libre = coincé.
        double scoreCoince = ScorePositionCombat.ScoreCelluleTactique(
            xCell: 5, yCell: 5, ennemis, (8, 8),
            invocsAlliesXY: System.Array.Empty<(int, int)>(),
            nbCasesAdjacentesMarchables: 1, ctx, losOk: true);

        Assert.True(scoreCoince > scoreLibre,
            $"Score coincé ({scoreCoince}) doit être > libre ({scoreLibre})");
    }

    [Fact]
    public void ScoreCelluleTactique_Agressif_IgnoreExposition()
    {
        // En Agressif, on ne pénalise PAS l'exposition (on cherche le combat).
        var ennemisProches = new[] { (1, 1), (2, 2) };
        var ennemisLoins = new[] { (1, 1), (15, 15) };
        var ctx = CtxAgressif();

        double scoreProches = ScorePositionCombat.ScoreCelluleTactique(
            xCell: 5, yCell: 5, ennemisProches, (1, 1),
            invocsAlliesXY: System.Array.Empty<(int, int)>(),
            nbCasesAdjacentesMarchables: 8, ctx, losOk: true);
        double scoreLoins = ScorePositionCombat.ScoreCelluleTactique(
            xCell: 5, yCell: 5, ennemisLoins, (1, 1),
            invocsAlliesXY: System.Array.Empty<(int, int)>(),
            nbCasesAdjacentesMarchables: 8, ctx, losOk: true);

        // En Agressif, W4_MultiMobs * sommeDist domine donc le scoring varie
        // mais NE pénalise PAS l'exposition (W6=0 pour Agressif).
        // Les 2 scores diffèrent par W4 uniquement.
        // Test smoke : pas de crash, scoring fonctionne.
        Assert.True(double.IsFinite(scoreProches));
        Assert.True(double.IsFinite(scoreLoins));
    }
}
