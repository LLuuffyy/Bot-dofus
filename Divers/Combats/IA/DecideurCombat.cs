using System.Collections.Generic;
using System.Linq;
using BotDofus.Divers.Combats.Combattants;

namespace BotDofus.Divers.Combats.IA;

/// <summary>
/// Décide de l'action à exécuter pendant le tour du bot, à partir de l'état
/// du combat et d'une liste de règles de sorts. Algorithme par défaut :
///
///  1. Trier les règles par priorité décroissante.
///  2. Pour chaque règle : trouver une cible valide (selon CibleSort + portée
///     + seuils PV), vérifier que le bot a assez de PA, retourner l'action.
///  3. S'il reste des PM mais pas de sort utilisable : se rapprocher d'un
///     ennemi pour préparer le tour suivant.
///  4. Sinon : passer le tour.
///
/// Les calculs de portée actuels supposent une distance de Manhattan entre
/// id de cellules (~ pathfinding 4-connexité). Ils seront affinés quand
/// le décodage de carte exposera les vraies coordonnées.
/// </summary>
public sealed class DecideurCombat
{
    private readonly StrategieCombat _strategie;
    private readonly List<RegleSort> _regles;

    public DecideurCombat(StrategieCombat strategie, IEnumerable<RegleSort> regles)
    {
        _strategie = strategie;
        _regles = regles.OrderByDescending(r => r.Priorite).ToList();
    }

    public ActionCombat Decider(Combat combat)
    {
        if (_strategie == StrategieCombat.Passif) return new ActionCombat.PasserTour();

        var moi = combat.Allies.FirstOrDefault(c => c.Identifiant == combat.IdentifiantAllie);
        if (moi == null) return new ActionCombat.PasserTour();

        // Stratégie défensive : si PV bas, soin/objet/fuite avant tout.
        if (_strategie == StrategieCombat.Defensif
            && moi.PVMax > 0
            && (100 * moi.PV / moi.PVMax) < 35)
        {
            var soin = TrouverSoin(combat, moi);
            if (soin != null) return soin;
        }

        // Tentative d'application des règles de sorts dans l'ordre de priorité.
        foreach (var regle in _regles)
        {
            if (regle.CoutPA > moi.PA) continue;

            var cible = ChoisirCible(regle, combat, moi);
            if (cible == null) continue;

            var distance = DistanceApprox(moi.CellulePosition, cible.CellulePosition);
            if (distance < regle.PorteeMin || (regle.PorteeMax > 0 && distance > regle.PorteeMax)) continue;

            return new ActionCombat.LancerSort(regle.IdSort, cible.CellulePosition);
        }

        // Aucun sort utilisable : se rapprocher d'un ennemi.
        if (moi.PM > 0)
        {
            var cibleApproche = combat.Ennemis
                .Where(e => !e.EstMort)
                .OrderBy(e => DistanceApprox(moi.CellulePosition, e.CellulePosition))
                .FirstOrDefault();

            if (cibleApproche != null)
            {
                return new ActionCombat.SeDeplacer(cibleApproche.CellulePosition);
            }
        }

        return new ActionCombat.PasserTour();
    }

    private static Combattant? ChoisirCible(RegleSort regle, Combat combat, Combattant moi)
    {
        return regle.Cible switch
        {
            CibleSort.Soi => moi,
            CibleSort.EnnemiPlusProche => combat.Ennemis.Where(e => !e.EstMort)
                .OrderBy(e => DistanceApprox(moi.CellulePosition, e.CellulePosition))
                .FirstOrDefault(),
            CibleSort.EnnemiPlusFaible => combat.Ennemis.Where(e => !e.EstMort)
                .OrderBy(e => e.PV)
                .FirstOrDefault(),
            CibleSort.EnnemiPlusFort => combat.Ennemis.Where(e => !e.EstMort)
                .OrderByDescending(e => e.PV)
                .FirstOrDefault(),
            CibleSort.AlliePlusBlesse => combat.Allies.Where(a => !a.EstMort && a.PVMax > 0)
                .OrderBy(a => 100 * a.PV / a.PVMax)
                .FirstOrDefault(),
            CibleSort.PositionStrategique => moi,
            _ => null
        };
    }

    private static ActionCombat? TrouverSoin(Combat combat, Combattant moi)
    {
        // À enrichir : choisir la potion/pain dans Inventaire selon priorité.
        _ = combat;
        _ = moi;
        return null;
    }

    private static int DistanceApprox(int idA, int idB)
    {
        // Approximation : distance de Chebyshev sur une grille 14×N.
        const int largeur = 14;
        int xA = idA % largeur, yA = idA / largeur;
        int xB = idB % largeur, yB = idB / largeur;
        int dx = System.Math.Abs(xA - xB);
        int dy = System.Math.Abs(yA - yB);
        return dx + dy;
    }
}
