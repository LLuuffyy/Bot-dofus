using System;
using System.Collections.Generic;
using BotDofus.Divers.Cartes;
using BotDofus.Divers.Cartes.Deplacement;

namespace BotDofus.Divers.Combats.IA;

/// <summary>
/// Moteur tactique central : calcule la <b>meilleure cellule cible</b> pour
/// le perso actif chaque tour selon :
/// <list type="bullet">
///   <item>Mode de combat (Agressif / Eloigne / Fuyard / Equilibre)</item>
///   <item>Sort principal (portée min/max, LOS requis)</item>
///   <item>Distance d'arrêt optimale via <see cref="ScorePositionCombat.DistanceIdeale"/>
///         (kite intelligent : porteeMax + PM_ennemi pour Eloigne)</item>
///   <item>Pénalité LOS bloquée par allié / obstacle / mur</item>
///   <item>Pénalité hors portée du sort principal</item>
///   <item>Score multi-mobs (Σ-distance ennemis)</item>
/// </list>
///
/// <para>
/// Stateless, thread-safe. Réutilisable par le master (<c>TrameJeu.PreMouvementSelonModeAsync</c>)
/// et par les héros liés (<c>IACombatHerosSimple.PreMouvementSelonModeAsync</c>).
/// </para>
///
/// <para>
/// Référence design : ADR-008 (refonte IA tactique 2026-05-22). Patterns
/// inspirés de Synfus/Dyshay (cf. <c>docs/ANALYSE-DYSHAY-COMBAT.md</c> §2.2
/// <c>FightExtensions.get_Mover</c>).
/// </para>
/// </summary>
public static class MoteurTactique
{
    /// <summary>Résultat du calcul : cellule de destination + chemin + diag.</summary>
    public sealed record Resultat(
        Cellule Cible,
        IReadOnlyList<Cellule> Chemin,
        int PmConsommes,
        int DistanceFinaleCible,
        int SommeDistEnnemis,
        bool LosCibleFinale,
        double Score);

    /// <summary>Délégué de test LOS — implémentation injectée par l'appelant
    /// pour découpler MoteurTactique des dépendances spécifiques (Carte,
    /// occupations live, etc.).</summary>
    public delegate bool TestLosDelegate(Cellule depuis, Cellule vers);

    /// <summary>
    /// Cherche la meilleure cellule où se rendre pour optimiser la position
    /// tactique selon le contexte. Retourne <c>null</c> si aucune amélioration
    /// par rapport à la position de départ (cf. <paramref name="exigeAmelioration"/>).
    /// </summary>
    /// <param name="carte">Carte courante (largeur réelle Hystoria).</param>
    /// <param name="depart">Cellule de départ du perso.</param>
    /// <param name="pmDispo">PM disponibles (budget de déplacement A*).</param>
    /// <param name="interdites">Cellules à éviter (combattants vivants sauf moi).</param>
    /// <param name="ennemisXY">Coords pré-calculées (<see cref="ScorePositionCombat.CoordsEnnemis"/>).</param>
    /// <param name="cibleXY">Coords (x,y) de l'ennemi PRIORITAIRE (focus principal du sort).</param>
    /// <param name="ctx">Contexte tactique (mode + portée sort + PM ennemi).</param>
    /// <param name="testLos">Délégué qui vérifie si la LOS est dégagée depuis
    /// une cellule donnée vers la cible. Appelé seulement si le sort
    /// <see cref="ContexteTactique.SortNecessiteLOS"/> = true.</param>
    /// <param name="exigeAmelioration">Si <c>true</c>, retourne <c>null</c> quand
    /// aucune cellule candidate ne fait mieux que le score de départ. Mettre
    /// <c>false</c> pour forcer un repositionnement même petit.</param>
    /// <param name="poids">Poids du scoring (null → <see cref="ScorePositionCombat.PoidsScoreCellule.Defaut"/>).</param>
    public static Resultat? CalculerMeilleureCellule(
        Carte carte,
        Cellule depart,
        int pmDispo,
        ICollection<Cellule> interdites,
        (int x, int y)[] ennemisXY,
        (int x, int y) cibleXY,
        ScorePositionCombat.ContexteTactique ctx,
        TestLosDelegate? testLos = null,
        bool exigeAmelioration = true,
        ScorePositionCombat.PoidsScoreCellule? poids = null)
    {
        if (carte == null || depart == null) return null;
        if (pmDispo <= 0) return null;
        if (ennemisXY.Length == 0) return null;

        // Score de départ (référence pour exigeAmelioration).
        bool losDepart = testLos == null ? true : !ctx.SortNecessiteLOS || EvaluerLos(testLos, depart, cibleXY, carte);
        double scoreDepart = ScorePositionCombat.ScoreCelluleAvance(
            depart.X, depart.Y, ennemisXY, cibleXY, ctx, losDepart, poids);

        // Si LOS bloquée au départ, accepter une cellule équivalente en distance
        // qui dégage la LOS (sinon le perso reste planté avec 0 cast par tour).
        if (ctx.SortNecessiteLOS && !losDepart) exigeAmelioration = false;

        Cellule? meilleureCible = null;
        IReadOnlyList<Cellule>? meilleurChemin = null;
        double meilleurScore = exigeAmelioration ? scoreDepart : double.MaxValue;
        int meilleurNbPas = int.MaxValue;
        int meilleureDistFinale = 0;
        int meilleureSommeDist = 0;
        bool meilleureLosFinale = false;

        // Énumère toutes les cellules de la carte. On filtre rapidement par
        // estimation Manhattan AVANT de payer le coût du pathfinder A*.
        foreach (var c in carte.Cellules)
        {
            if (c == null || !c.EstMarchable || c.IdInteractif >= 0) continue;
            if (interdites.Contains(c)) continue;

            // Pré-filtre PM (estimation Manhattan = borne basse). Le pathfinder
            // A* peut nécessiter +1-2 pas si la route fait des détours.
            int dEst = Math.Abs(c.X - depart.X) + Math.Abs(c.Y - depart.Y);
            if (dEst == 0 || dEst > pmDispo) continue;

            // Score AVANT pathfinder (gain perf : si déjà pire que le meilleur,
            // pas la peine de chercher le chemin).
            // Pour ça il faut le LOS pré-calculé. Coût modéré (Bresenham), mais
            // ça reste plus rapide qu'A*.
            (int xC, int yC) cibleP = cibleXY;
            int distC = Math.Max(Math.Abs(c.X - cibleP.xC), Math.Abs(c.Y - cibleP.yC));
            bool losCandidate = testLos == null
                ? true
                : !ctx.SortNecessiteLOS || EvaluerLos(testLos, c, cibleXY, carte);

            double score = ScorePositionCombat.ScoreCelluleAvance(
                c.X, c.Y, ennemisXY, cibleXY, ctx, losCandidate, poids);

            // Skip si pas mieux que le meilleur actuel.
            if (score > meilleurScore) continue;

            // Si exigeAmelioration : on doit STRICTEMENT améliorer.
            if (exigeAmelioration && score >= scoreDepart) continue;

            // Pathfinder A* (coût réel — exécuté seulement pour les vraies candidates).
            var chemin = Pathfinder.Trouver(carte, depart, c, interdites, combat: true);
            if (chemin == null || chemin.Count < 2) continue;
            int nbPas = chemin.Count - 1;
            if (nbPas > pmDispo) continue;

            // Sélection : score min, tie-break sur nb pas min (économise PM).
            if (score < meilleurScore || (score == meilleurScore && nbPas < meilleurNbPas))
            {
                meilleurScore = score;
                meilleurNbPas = nbPas;
                meilleureCible = c;
                meilleurChemin = chemin;
                meilleureDistFinale = distC;
                meilleureSommeDist = ScorePositionCombat.SommeDistances(c.X, c.Y, ennemisXY);
                meilleureLosFinale = losCandidate;
            }
        }

        if (meilleureCible == null || meilleurChemin == null) return null;
        return new Resultat(
            Cible: meilleureCible,
            Chemin: meilleurChemin,
            PmConsommes: meilleurNbPas,
            DistanceFinaleCible: meilleureDistFinale,
            SommeDistEnnemis: meilleureSommeDist,
            LosCibleFinale: meilleureLosFinale,
            Score: meilleurScore);
    }

    /// <summary>
    /// Wrapper qui retrouve la <see cref="Cellule"/> par coords pour appeler
    /// le délégué <see cref="TestLosDelegate"/>. Si la cible n'est pas une
    /// cellule connue de la carte (cas exotique), retourne <c>true</c> (LOS OK
    /// par défaut, ne pas pénaliser à tort).
    /// </summary>
    private static bool EvaluerLos(TestLosDelegate testLos, Cellule depuis, (int x, int y) cibleXY, Carte carte)
    {
        var cellCible = carte.ObtenirParCoords(cibleXY.x, cibleXY.y);
        if (cellCible == null) return true;
        return testLos(depuis, cellCible);
    }
}
