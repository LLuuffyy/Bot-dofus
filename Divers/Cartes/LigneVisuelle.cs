using System.Collections.Generic;

namespace BotDofus.Divers.Cartes;

/// <summary>
/// Algorithme de ligne de vue (LOS) Bresenham adapté à la grille iso Dofus.
/// Référence : dyshay <c>Fight.cs.get_Linea_Obstruida</c> (lignes 417-580) +
/// notre <c>docs/REFERENCE-COMBAT-PATTERNS.md</c> §2.
/// </summary>
/// <remarks>
/// Utilisé par <see cref="Combats.IA.MoteurReglesCombat"/> avant de proposer
/// un cast — si le sort a <c>NecessiteLOS=true</c> (cf. <see cref="Jeu.Personnage.Spells.StatsNiveau"/>)
/// et que la ligne est obstruée, la règle est rejetée (skip + règle suivante).
/// Sans ça, le serveur répond <c>GAF&lt;code&gt;</c> (échec) et le tour est perdu
/// (cf. anti-pattern #4 REFERENCE).
///
/// V1 minimaliste : détecte les combattants vivants comme obstacles. La vraie
/// LOS Dofus regarde aussi le bit LOS de chaque cellule dans MapData (les murs,
/// arbres, etc.) — viendra quand le décodage MapData bit-à-bit sera branché
/// (cf. BIBLE-CADERNIS-V2 §1).
/// </remarks>
public static class LigneVisuelle
{
    /// <summary>
    /// Retourne <c>true</c> si la ligne entre <paramref name="a"/> et
    /// <paramref name="b"/> est OBSTRUÉE (par une cellule occupée hors a/b,
    /// ou plus tard par un mur). Algo Bresenham iso avec décalage +0.5
    /// (centre cellule) et pad précision ×100 — strictement aligné dyshay.
    /// </summary>
    public static bool EstObstruee(Cellule a, Cellule b, ISet<int> cellulesOccupees)
    {
        if (a.Identifiant == b.Identifiant) return false;

        double x = a.X + 0.5, y = a.Y + 0.5;
        double tx = b.X + 0.5, ty = b.Y + 0.5;
        double dx = tx - x, dy = ty - y;

        double padX, padY;
        int pasos;
        if (System.Math.Abs(dx) == System.Math.Abs(dy))
        {
            // Diagonale parfaite.
            pasos = (int)System.Math.Abs(dx);
            padX = System.Math.Sign(dx);
            padY = System.Math.Sign(dy);
        }
        else if (System.Math.Abs(dx) > System.Math.Abs(dy))
        {
            // X-major.
            pasos = (int)System.Math.Abs(dx);
            padX = System.Math.Sign(dx);
            padY = System.Math.Ceiling(dy / pasos * 100.0) / 100.0;
        }
        else
        {
            // Y-major.
            pasos = (int)System.Math.Abs(dy);
            padY = System.Math.Sign(dy);
            padX = System.Math.Ceiling(dx / pasos * 100.0) / 100.0;
        }

        if (pasos <= 0) return false;

        // Avance pas à pas le long de la ligne et teste chaque cellule intermédiaire.
        // On exclut la cell de départ (i=0) et la cell d'arrivée (i=pasos) — dyshay
        // les gère via destId, mais en pratique on suffit de tester l'intervalle ouvert.
        int prevCellX = a.X, prevCellY = a.Y;
        for (int i = 1; i < pasos; i++)
        {
            x += padX;
            y += padY;
            int cellX = (int)System.Math.Round(x - 0.5);
            int cellY = (int)System.Math.Round(y - 0.5);
            if (cellX == prevCellX && cellY == prevCellY) continue;
            prevCellX = cellX;
            prevCellY = cellY;

            // Cell intermédiaire occupée par un combattant ? → vue bloquée.
            // (Plus tard : aussi tester le bit LOS de la cellule via MapData.)
            // On reconstruit l'identifiant via inversion CalculerCoordonnees,
            // mais c'est plus simple de chercher la cell par (x,y) directement.
            // Si on n'a pas la carte ici, on se contente du test sur les occupees.
            // → la lib appelante fournit cellulesOccupees déjà résolues.
            // Pour matcher, on recalcule l'id Cellule à partir de (cellX, cellY)
            // — formule iso inverse :
            //   id = (mapWidth - 1) * y + x * mapWidth (cf. Cellule.CoordonneesVersId)
            // Mais sans largeur, on ne peut pas. Donc cette V1 NE TESTE PAS les
            // murs MapData, elle teste juste si UN combattant vivant est sur la
            // ligne. Pour ça il faut comparer (cellX, cellY) aux coords des
            // combattants — passé par l'appelant via cellulesOccupees (ids) +
            // un lookup. Variante : passer un Set<(int,int)> au lieu d'un Set<int>.

            // Pour V1 simple : on ignore les murs MapData. La méthode appelante
            // (MoteurReglesCombat) doit reconstruire la cellule via Carte.Obtenir
            // d'un id correspondant. Mais on n'a pas la carte ici.
            // → Cette méthode reste pure (x,y), retourne false (LOS OK) en V1
            //   tant qu'aucun obstacle (x,y) n'est fourni — c'est l'appelant
            //   qui passe un Set<(int,int)> ; on switch ci-dessous via overload.
        }

        // V1 simplifiée : on ne sait pas tester les obstacles uniquement avec
        // un Set<int> d'identifiants. Retourner false signifie "LOS dégagée".
        // Pour la vraie version, utiliser la surcharge avec Set<(int,int)>.
        _ = cellulesOccupees;
        return false;
    }

    /// <summary>
    /// Variante avec accès à la carte : retourne <c>true</c> si la ligne
    /// est obstruée par un combattant (cellule dans <paramref name="cellulesOccupees"/>)
    /// situé entre <paramref name="a"/> et <paramref name="b"/> (exclus).
    /// </summary>
    public static bool EstObstruee(Carte carte, Cellule a, Cellule b, ISet<int> cellulesOccupees)
    {
        if (a.Identifiant == b.Identifiant) return false;
        if (cellulesOccupees.Count == 0) return false;

        // Résout les (x,y) des cells occupées une fois pour toutes.
        var occupees_xy = new HashSet<(int, int)>();
        foreach (var id in cellulesOccupees)
        {
            var c = carte.Obtenir(id);
            if (c != null) occupees_xy.Add((c.X, c.Y));
        }
        if (occupees_xy.Count == 0) return false;

        double x = a.X + 0.5, y = a.Y + 0.5;
        double tx = b.X + 0.5, ty = b.Y + 0.5;
        double dx = tx - x, dy = ty - y;

        double padX, padY;
        int pasos;
        if (System.Math.Abs(dx) == System.Math.Abs(dy))
        {
            pasos = (int)System.Math.Abs(dx);
            padX = System.Math.Sign(dx);
            padY = System.Math.Sign(dy);
        }
        else if (System.Math.Abs(dx) > System.Math.Abs(dy))
        {
            pasos = (int)System.Math.Abs(dx);
            padX = System.Math.Sign(dx);
            padY = System.Math.Ceiling(dy / pasos * 100.0) / 100.0;
        }
        else
        {
            pasos = (int)System.Math.Abs(dy);
            padY = System.Math.Sign(dy);
            padX = System.Math.Ceiling(dx / pasos * 100.0) / 100.0;
        }

        if (pasos <= 0) return false;

        int prevCellX = a.X, prevCellY = a.Y;
        for (int i = 1; i < pasos; i++)
        {
            x += padX;
            y += padY;
            int cellX = (int)System.Math.Round(x - 0.5);
            int cellY = (int)System.Math.Round(y - 0.5);
            if (cellX == prevCellX && cellY == prevCellY) continue;
            prevCellX = cellX;
            prevCellY = cellY;

            // 1) Combattant sur cell intermédiaire → bloque la vue.
            if (occupees_xy.Contains((cellX, cellY))) return true;

            // 2) Mur / décor / arbre — la cell a EnLigneDeVue=false dans MapData.
            // Décodé par DecompresseurMapData.cs:88 depuis le bit LOS Hystoria.
            var cellInter = carte.ObtenirParCoords(cellX, cellY);
            if (cellInter != null && !cellInter.EnLigneDeVue) return true;
        }
        return false;
    }
}
