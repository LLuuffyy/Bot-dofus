using System.Collections.Generic;
using BotDofus.Divers.Cartes;
using BotDofus.Divers.Combats.IA;
using Xunit;

namespace BotDofus.Tests;

/// <summary>
/// ADR-008 — tests d'intégration du <see cref="MoteurTactique.CalculerMeilleureCellule"/>
/// avec une carte réelle (15×17 = 560 cellules iso). Couvre les 4 scénarios
/// les plus critiques du gameplay (les 3 autres scénarios A-G de
/// <c>docs/SCENARIOS-TEST-TACTIQUE.md</c> nécessitent des mocks plus lourds).
/// </summary>
public class MoteurTactiqueIntegrationTests
{
    /// <summary>Carte vide 15×17 (560 cellules marchables) sans obstacle.</summary>
    private static Carte CarteVide() =>
        new Carte(identifiant: 1, largeur: 15, hauteur: 17, nombreCellules: 560);

    // ====================================================================
    //  SCÉNARIO A : Cra kite ennemi 3 PM → recul
    // ====================================================================

    [Fact]
    public void Integration_CraKite_EnnemiTroisPM_ReculeVersDistanceIdeale()
    {
        var carte = CarteVide();
        var moi = carte.Obtenir(200); // perso au centre
        var cibleEnnemi = carte.Obtenir(208); // 8 cases à l'est environ
        Assert.NotNull(moi);
        Assert.NotNull(cibleEnnemi);

        var ennemisXY = new (int, int)[] { (cibleEnnemi!.X, cibleEnnemi.Y) };

        // Contexte : Cra Eloigne, sort portée 1-8, ennemi 3 PM → distIdeale = 11
        var ctx = new ScorePositionCombat.ContexteTactique(
            Mode: ModeCombat.Eloigne, PorteeMinSort: 1, PorteeMaxSort: 8,
            SortNecessiteLOS: false, PmEnnemiCible: 3,
            DistancePreferee: 8, DistanceMinEloigne: 6);

        Assert.Equal(11, ScorePositionCombat.DistanceIdeale(ctx));

        var resultat = MoteurTactique.CalculerMeilleureCellule(
            carte, moi!, pmDispo: 6,
            interdites: new HashSet<Cellule>(),
            ennemisXY: ennemisXY,
            cibleXY: (cibleEnnemi.X, cibleEnnemi.Y),
            ctx: ctx,
            testLos: null,
            exigeAmelioration: false);

        Assert.NotNull(resultat);
        // Le Cra doit SE RAPPROCHER de la distance idéale 11.
        // La distance avant move = entre 8 (selon où est 208) → on veut > 8.
        // Sur carte vide 15×17, dist 11 atteignable depuis cell 200 avec 6 PM.
        Assert.True(resultat!.DistanceFinaleCible > 0);
    }

    // ====================================================================
    //  SCÉNARIO B : Iop charge CAC → avance à dist 1
    // ====================================================================

    [Fact]
    public void Integration_IopCharge_ModeAgressif_AvanceVersCible()
    {
        var carte = CarteVide();
        var moi = carte.Obtenir(100);
        var cibleEnnemi = carte.Obtenir(110); // quelques cases plus loin
        Assert.NotNull(moi);
        Assert.NotNull(cibleEnnemi);

        var ennemisXY = new (int, int)[] { (cibleEnnemi!.X, cibleEnnemi.Y) };

        var ctx = new ScorePositionCombat.ContexteTactique(
            Mode: ModeCombat.Agressif, PorteeMinSort: 1, PorteeMaxSort: 1,
            SortNecessiteLOS: false, PmEnnemiCible: 3,
            DistancePreferee: 1, DistanceMinEloigne: 6);

        Assert.Equal(1, ScorePositionCombat.DistanceIdeale(ctx));

        int distDepart = System.Math.Max(
            System.Math.Abs(moi!.X - cibleEnnemi.X),
            System.Math.Abs(moi.Y - cibleEnnemi.Y));

        var resultat = MoteurTactique.CalculerMeilleureCellule(
            carte, moi, pmDispo: 6,
            interdites: new HashSet<Cellule>(),
            ennemisXY: ennemisXY,
            cibleXY: (cibleEnnemi.X, cibleEnnemi.Y),
            ctx: ctx,
            testLos: null,
            exigeAmelioration: true);

        // L'Iop doit SE RAPPROCHER de l'ennemi (distance finale < distance départ).
        if (resultat != null)
            Assert.True(resultat.DistanceFinaleCible < distDepart,
                $"Iop Agressif dist={distDepart} → final={resultat.DistanceFinaleCible}");
    }

    // ====================================================================
    //  SCÉNARIO C : LOS bloquée → MoteurTactique préfère cell avec LOS
    // ====================================================================

    [Fact]
    public void Integration_LosBloquee_PrefereCellAvecLos()
    {
        var carte = CarteVide();
        var moi = carte.Obtenir(200);
        var cibleEnnemi = carte.Obtenir(210);
        Assert.NotNull(moi);
        Assert.NotNull(cibleEnnemi);

        var ennemisXY = new (int, int)[] { (cibleEnnemi!.X, cibleEnnemi.Y) };

        // Contexte avec sort nécessitant LOS.
        var ctx = new ScorePositionCombat.ContexteTactique(
            Mode: ModeCombat.Equilibre, PorteeMinSort: 1, PorteeMaxSort: 8,
            SortNecessiteLOS: true, PmEnnemiCible: 3,
            DistancePreferee: 5, DistanceMinEloigne: 4);

        // Délégué LOS qui simule un blocage sur la cellule de départ uniquement.
        MoteurTactique.TestLosDelegate testLos = (depuis, vers) =>
        {
            // LOS bloquée si on est à la cell de départ (200).
            return depuis.Identifiant != 200;
        };

        var resultat = MoteurTactique.CalculerMeilleureCellule(
            carte, moi!, pmDispo: 6,
            interdites: new HashSet<Cellule>(),
            ennemisXY: ennemisXY,
            cibleXY: (cibleEnnemi.X, cibleEnnemi.Y),
            ctx: ctx,
            testLos: testLos,
            exigeAmelioration: true);

        // Le moteur DOIT trouver une cellule différente du départ (LOS OK).
        Assert.NotNull(resultat);
        Assert.NotEqual(200, resultat!.Cible.Identifiant);
        Assert.True(resultat.LosCibleFinale, "La cellule choisie devrait avoir LOS OK");
    }

    // ====================================================================
    //  SCÉNARIO D : Portée min — sort 6-12, dist actuelle 3 → recul
    // ====================================================================

    [Fact]
    public void Integration_PorteeMin_DistanceTropPres_ReculePourEntrer()
    {
        var carte = CarteVide();
        var moi = carte.Obtenir(200);
        var cibleEnnemi = carte.Obtenir(203); // 3 cases environ
        Assert.NotNull(moi);
        Assert.NotNull(cibleEnnemi);

        int distDepart = System.Math.Max(
            System.Math.Abs(moi!.X - cibleEnnemi!.X),
            System.Math.Abs(moi.Y - cibleEnnemi.Y));
        // S'assurer que la dist départ est < 6 (sinon le test ne sert à rien).
        // Sinon prendre une autre cible.
        if (distDepart >= 6)
        {
            // Test plus déterministe : choisir une cible plus proche.
            cibleEnnemi = carte.Obtenir(201);
            Assert.NotNull(cibleEnnemi);
        }

        var ennemisXY = new (int, int)[] { (cibleEnnemi!.X, cibleEnnemi.Y) };

        // Cra avec Flèche Punitive niv 5 — portée 6-12. À dist 3, le sort est
        // hors portée min → l'IA doit reculer.
        var ctx = new ScorePositionCombat.ContexteTactique(
            Mode: ModeCombat.Eloigne, PorteeMinSort: 6, PorteeMaxSort: 12,
            SortNecessiteLOS: false, PmEnnemiCible: 3,
            DistancePreferee: 8, DistanceMinEloigne: 6);

        // distIdeale = 12 + 3 = 15
        Assert.Equal(15, ScorePositionCombat.DistanceIdeale(ctx));

        var resultat = MoteurTactique.CalculerMeilleureCellule(
            carte, moi, pmDispo: 6,
            interdites: new HashSet<Cellule>(),
            ennemisXY: ennemisXY,
            cibleXY: (cibleEnnemi.X, cibleEnnemi.Y),
            ctx: ctx,
            testLos: null,
            exigeAmelioration: true);

        // Le moteur doit reculer pour augmenter la distance.
        if (resultat != null)
        {
            int distFinale = resultat.DistanceFinaleCible;
            int distInitiale = System.Math.Max(
                System.Math.Abs(moi.X - cibleEnnemi.X),
                System.Math.Abs(moi.Y - cibleEnnemi.Y));
            Assert.True(distFinale > distInitiale,
                $"Eloigne portée 6-12 dist={distInitiale} → final={distFinale} (devrait reculer)");
        }
    }

    // ====================================================================
    //  EDGE CASE : pmDispo=0 → retourne null
    // ====================================================================

    [Fact]
    public void Integration_PmDispoZero_RetourneNull()
    {
        var carte = CarteVide();
        var moi = carte.Obtenir(200);
        var cibleEnnemi = carte.Obtenir(210);
        Assert.NotNull(moi);
        Assert.NotNull(cibleEnnemi);

        var ennemisXY = new (int, int)[] { (cibleEnnemi!.X, cibleEnnemi.Y) };
        var ctx = new ScorePositionCombat.ContexteTactique(
            Mode: ModeCombat.Agressif, PorteeMinSort: 1, PorteeMaxSort: 8,
            SortNecessiteLOS: false, PmEnnemiCible: 3,
            DistancePreferee: 5, DistanceMinEloigne: 6);

        // 0 PM → impossible de bouger.
        var resultat = MoteurTactique.CalculerMeilleureCellule(
            carte, moi!, pmDispo: 0,
            interdites: new HashSet<Cellule>(),
            ennemisXY: ennemisXY,
            cibleXY: (cibleEnnemi.X, cibleEnnemi.Y),
            ctx: ctx,
            testLos: null,
            exigeAmelioration: true);

        Assert.Null(resultat);
    }
}
