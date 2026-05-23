using System;
using System.Collections.Generic;
using System.Linq;
using BotDofus.Divers.Cartes;
using BotDofus.Divers.Combats.Combattants;

namespace BotDofus.Divers.Combats.IA;

/// <summary>
/// Heuristiques de scoring de cellule pour le positionnement combat — version
/// multi-mobs alignée dyshay (<c>Get_Total_Distancia_Enemigo</c>, somme des
/// distances Chebyshev à TOUS les ennemis vivants au lieu du seul plus proche).
///
/// <para>
/// Pourquoi le multi-mobs : à 1 ennemi, min-dist et sum-dist sont identiques.
/// À 2+ ennemis, la différence est énorme : avec sum-dist, le bot se met au
/// MILIEU du groupe (Agressif) ou s'extrait au MAX du groupe (Fuyard/Eloigne).
/// Le min-dist faisait yo-yo entre 2 mobs symétriques.
/// </para>
///
/// <para>
/// API stateless — tout est calculé à partir d'un snapshot d'ennemis vivants.
/// </para>
/// </summary>
public static class ScorePositionCombat
{
    /// <summary>
    /// Pré-calcule les coordonnées (x,y) des ennemis vivants. À appeler une
    /// fois par tour ; le retour est consommé par <see cref="ScoreCellule"/>
    /// pour chaque candidate sans recalculer.
    /// </summary>
    public static (int x, int y)[] CoordsEnnemis(IEnumerable<Combattant> ennemisVivants, int mapWidth)
        => ennemisVivants
            .Select(e => Cellule.CalculerCoordonnees(e.CellulePosition, mapWidth))
            .ToArray();

    /// <summary>
    /// Somme des distances Chebyshev d'une cellule (xCell, yCell) vers chaque
    /// ennemi. Métrique principale dyshay (<c>Get_Total_Distancia_Enemigo</c>).
    /// </summary>
    public static int SommeDistances(int xCell, int yCell, (int x, int y)[] ennemis)
    {
        int total = 0;
        foreach (var (ex, ey) in ennemis)
            total += Math.Max(Math.Abs(xCell - ex), Math.Abs(yCell - ey));
        return total;
    }

    /// <summary>
    /// Distance Chebyshev minimale d'une cellule vers l'ennemi le plus proche.
    /// Sert au gating « suis-je au CAC ? » (dist=1) et aux paliers Fuyard 8/12.
    /// </summary>
    public static int DistanceMin(int xCell, int yCell, (int x, int y)[] ennemis)
    {
        if (ennemis.Length == 0) return int.MaxValue;
        int min = int.MaxValue;
        foreach (var (ex, ey) in ennemis)
        {
            int d = Math.Max(Math.Abs(xCell - ex), Math.Abs(yCell - ey));
            if (d < min) min = d;
        }
        return min;
    }

    /// <summary>
    /// Score d'une cellule selon <see cref="ModeCombat"/>, en utilisant
    /// la SOMME des distances aux ennemis (multi-mobs).
    /// Plus le score est <b>BAS</b>, meilleure est la cellule.
    /// </summary>
    /// <param name="xCell">Coord X de la cellule candidate.</param>
    /// <param name="yCell">Coord Y de la cellule candidate.</param>
    /// <param name="ennemisXY">Coords des ennemis (cf. <see cref="CoordsEnnemis"/>).</param>
    /// <param name="mode">Mode de combat.</param>
    /// <param name="distancePreferee">Pour <see cref="ModeCombat.Equilibre"/>.</param>
    /// <param name="distanceMinEloigne">Plancher Eloigne/Fuyard (pénalité brutale si non respecté).</param>
    public static double ScoreCellule(
        int xCell, int yCell,
        (int x, int y)[] ennemisXY,
        ModeCombat mode,
        int distancePreferee,
        int distanceMinEloigne)
    {
        if (ennemisXY.Length == 0) return 0;
        int sommeDist = SommeDistances(xCell, yCell, ennemisXY);
        int distMin = DistanceMin(xCell, yCell, ennemisXY);
        int n = ennemisXY.Length;

        return mode switch
        {
            // Agressif : on veut MINIMISER la sum-dist (proche du groupe).
            // Bonus supplémentaire si on est au CAC (distMin=1) pour préférer
            // une cell adjacente à un mob (tacle), même si elle augmente
            // légèrement la sum vs une cell au barycentre.
            ModeCombat.Agressif => sommeDist + (distMin == 1 ? -2 : 0),

            // Eloigne / Fuyard : MAXIMISER la sum-dist + respect dur du plancher.
            // Si distMin < distanceMinEloigne → pénalité 1000 × (manque) :
            // le bot ne termine JAMAIS son tour plus proche que le seuil
            // demandé tant qu'il a des PM pour s'écarter.
            ModeCombat.Eloigne or ModeCombat.Fuyard
                => distMin >= distanceMinEloigne
                    ? -sommeDist
                    : 1000.0 * (distanceMinEloigne - distMin) - sommeDist,

            // Equilibre / Tactique : on vise distancePreferee comme distance
            // MOYENNE au groupe (= sumDist / N ≈ distancePref). Pénalité =
            // écart absolu × N pour rester comparable aux autres scores.
            ModeCombat.Equilibre => Math.Abs(sommeDist - distancePreferee * n),

            _ => 0
        };
    }

    // ====================================================================
    //  MOTEUR TACTIQUE AVANCÉ — distance idéale + kite + LOS + portée
    // ====================================================================

    /// <summary>
    /// Contexte tactique pour <see cref="ScoreCelluleAvance"/>. Regroupe les
    /// paramètres du sort principal et de l'ennemi prioritaire pour calculer
    /// la distance idéale d'arrêt (kite intelligent, portée min/max respectées).
    /// </summary>
    public readonly record struct ContexteTactique(
        ModeCombat Mode,
        /// <summary>Portée min du sort principal (0 si pas de min).</summary>
        int PorteeMinSort,
        /// <summary>Portée max du sort principal (>0).</summary>
        int PorteeMaxSort,
        /// <summary>Sort nécessite LOS (= cellule de cast doit voir la cible).</summary>
        bool SortNecessiteLOS,
        /// <summary>PM restants de l'ennemi prioritaire. Sert au kite intelligent :
        /// si on recule, on s'arrête à PorteeMax + PmEnnemi pour qu'au tour suivant
        /// il arrive juste à PorteeMax. Si on connaît pas (=0), on suppose conservateur.</summary>
        int PmEnnemiCible,
        /// <summary>Mode Equilibre uniquement : distance préférée user.</summary>
        int DistancePreferee,
        /// <summary>Mode Eloigne/Fuyard : plancher minimal demandé par l'user.</summary>
        int DistanceMinEloigne);

    /// <summary>
    /// Calcule la DISTANCE D'ARRÊT IDÉALE vers l'ennemi prioritaire selon
    /// le mode et le sort principal. Sortie utilisée par <see cref="ScoreCelluleAvance"/>
    /// pour scorer chaque cellule candidate selon son écart à cette distance.
    ///
    /// <para>Algorithmes par mode :</para>
    /// <list type="bullet">
    ///   <item><b>Agressif</b> : si sort CAC (porteeMax=1) → 1, sinon
    ///   <c>max(porteeMin, 1)</c> (le plus collé possible en restant lançable).</item>
    ///   <item><b>Eloigne / Fuyard</b> : KITE INTELLIGENT —
    ///   <c>distIdeale = min(porteeMax + pmEnnemi, max_recul_realiste)</c>.
    ///   Si l'ennemi a 3 PM, on s'arrête à porteeMax+3 → au tour suivant il
    ///   arrive juste à porteeMax → on peut RE-CAST sans se tacler.</item>
    ///   <item><b>Equilibre</b> : <c>clamp(distancePref, porteeMin, porteeMax)</c>.</item>
    /// </list>
    /// </summary>
    public static int DistanceIdeale(ContexteTactique ctx)
    {
        int pMin = Math.Max(0, ctx.PorteeMinSort);
        int pMax = Math.Max(pMin, ctx.PorteeMaxSort);
        if (pMax <= 0) pMax = pMin;
        return ctx.Mode switch
        {
            // Agressif : CAC si possible, sinon dist minimale lançable.
            ModeCombat.Agressif when pMax <= 1 => 1,
            ModeCombat.Agressif => Math.Max(1, pMin),
            // Eloigne / Fuyard : kite parfait.
            // distIdeale = porteeMax + pmEnnemi (l'ennemi peut nous rattraper à porteeMax).
            // Si pmEnnemi inconnu (=0), fallback adaptatif selon portée du sort :
            // - Sort courte portée (pMax <= 3) : suppose 3 PM (mob faible)
            // - Sort moyenne portée (4-7) : suppose 4 PM (mob standard)
            // - Sort longue portée (8+) : suppose 5 PM (mob endgame)
            // Mieux que le 3 hardcoded — préserve la marge de sécurité kite.
            ModeCombat.Eloigne or ModeCombat.Fuyard
                => pMax + (ctx.PmEnnemiCible > 0
                    ? ctx.PmEnnemiCible
                    : pMax <= 3 ? 3 : pMax <= 7 ? 4 : 5),
            // Equilibre : clamp dist préférée dans la portée.
            ModeCombat.Equilibre => Math.Clamp(ctx.DistancePreferee, Math.Max(1, pMin), Math.Max(1, pMax)),
            _ => Math.Max(1, pMin),
        };
    }

    /// <summary>
    /// Score AVANCÉ d'une cellule selon le contexte tactique complet (mode +
    /// sort + ennemi). Plus le score est <b>BAS</b>, meilleure est la cellule.
    ///
    /// <para>Composantes (poids configurables via <paramref name="poids"/>) :</para>
    /// <list type="bullet">
    ///   <item><b>w1</b> = écart à <see cref="DistanceIdeale"/> (objectif tactique principal)</item>
    ///   <item><b>w2</b> = pénalité LOS bloquée (=1000 si sort nécessite LOS et obstacle)</item>
    ///   <item><b>w3</b> = pénalité hors portée [PorteeMin, PorteeMax] (=500 par case d'écart)</item>
    ///   <item><b>w4</b> = bonus Σ-distance multi-mobs (sécurité Fuyard, mobilité Agressif)</item>
    /// </list>
    /// Une cellule HORS PORTÉE du sort principal est très pénalisée → l'IA
    /// préfère se placer dans la zone de cast valide même si distance idéale
    /// n'est pas atteinte exactement.
    /// </summary>
    /// <param name="cibleXY">Coords (x,y) de l'ennemi PRIORITAIRE (focus principal).</param>
    /// <param name="losOk">Résultat pré-calculé du test LOS depuis (xCell,yCell) vers cibleXY.
    /// Pertinent UNIQUEMENT si <c>ctx.SortNecessiteLOS = true</c>.</param>
    public static double ScoreCelluleAvance(
        int xCell, int yCell,
        (int x, int y)[] ennemisXY,
        (int x, int y) cibleXY,
        ContexteTactique ctx,
        bool losOk,
        PoidsScoreCellule? poids = null)
    {
        var w = poids ?? PoidsScoreCellule.Defaut;

        int distCible = Math.Max(Math.Abs(xCell - cibleXY.x), Math.Abs(yCell - cibleXY.y));
        int distIdeale = DistanceIdeale(ctx);
        int sommeDist = SommeDistances(xCell, yCell, ennemisXY);

        double score = 0;

        // (1) Composante principale : écart à la distance idéale tactique.
        score += w.W1_DistanceIdeale * Math.Abs(distCible - distIdeale);

        // (2) Pénalité LOS bloquée (cellule où on ne pourra PAS cast à cause d'un obstacle).
        if (ctx.SortNecessiteLOS && !losOk)
            score += w.W2_LosBloquee;

        // (3) Pénalité forte si hors de la portée du sort principal — la cellule
        // ne permet PAS de cast direct. C'est encore acceptable en pré-move (on
        // peut bouger encore après), mais on préfère une cell dans la portée.
        int pMin = Math.Max(0, ctx.PorteeMinSort);
        int pMax = Math.Max(pMin, ctx.PorteeMaxSort);
        if (pMax > 0)
        {
            if (distCible < pMin) score += w.W3_HorsPortee * (pMin - distCible);
            else if (distCible > pMax) score += w.W3_HorsPortee * (distCible - pMax);
        }

        // (4) Composante secondaire multi-mobs (sécurité Fuyard, mobilité Agressif).
        score += ctx.Mode switch
        {
            ModeCombat.Agressif => w.W4_MultiMobs * sommeDist,           // minimise → bonus si on est central
            ModeCombat.Eloigne or ModeCombat.Fuyard
                => -w.W4_MultiMobs * sommeDist,                          // maximise → bonus si on s'éloigne du groupe
            _ => 0
        };

        // (5) Tie-break : préfère les cells avec LOS OK même quand le sort ne le requiert pas
        // (utile pour les autres sorts de la rotation qui pourraient le nécessiter).
        if (!ctx.SortNecessiteLOS && !losOk)
            score += w.W5_LosBonus;

        return score;
    }

    /// <summary>Poids configurables du scoring avancé. Valeurs par défaut tunées
    /// pour 1-3 ennemis lvl 1-50. Ajuster en cas de besoin (UI sliders viendront).</summary>
    public sealed record PoidsScoreCellule(
        double W1_DistanceIdeale = 10.0,
        double W2_LosBloquee = 1000.0,
        double W3_HorsPortee = 500.0,
        double W4_MultiMobs = 1.0,
        double W5_LosBonus = 2.0)
    {
        public static PoidsScoreCellule Defaut { get; } = new();
    }
}
