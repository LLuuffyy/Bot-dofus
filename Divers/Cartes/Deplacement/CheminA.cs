using System.Collections.Generic;

namespace BotDofus.Divers.Cartes.Deplacement;

/// <summary>
/// Algorithme A* pour trouver un chemin marchable entre deux cellules sur une
/// <see cref="Carte"/>. Utilise la 4-connexité (comme Dofus Retro, pas de diagonale
/// en déplacement) et la distance de Manhattan comme heuristique.
/// </summary>
public static class CheminA
{
    /// <summary>
    /// Retourne la liste des cellules du chemin, départ inclus et arrivée incluse.
    /// Retourne une liste vide si aucun chemin n'existe.
    /// </summary>
    public static IReadOnlyList<Cellule> Trouver(Carte carte, Cellule depart, Cellule arrivee)
    {
        if (depart.Identifiant == arrivee.Identifiant) return new[] { depart };

        var aVisiter = new PriorityQueue<Cellule, int>();
        var vientDe = new Dictionary<int, int>();
        var coutReel = new Dictionary<int, int> { [depart.Identifiant] = 0 };

        aVisiter.Enqueue(depart, depart.DistanceManhattan(arrivee));

        while (aVisiter.TryDequeue(out var courant, out _))
        {
            if (courant.Identifiant == arrivee.Identifiant)
            {
                return Reconstituer(vientDe, carte, depart, arrivee);
            }

            foreach (var voisin in carte.Voisins(courant))
            {
                if (!voisin.EstMarchable) continue;

                var coutCandidat = coutReel[courant.Identifiant] + 1;
                if (coutReel.TryGetValue(voisin.Identifiant, out var coutExistant)
                    && coutCandidat >= coutExistant)
                {
                    continue;
                }

                vientDe[voisin.Identifiant] = courant.Identifiant;
                coutReel[voisin.Identifiant] = coutCandidat;
                var priorite = coutCandidat + voisin.DistanceManhattan(arrivee);
                aVisiter.Enqueue(voisin, priorite);
            }
        }

        return System.Array.Empty<Cellule>();
    }

    private static IReadOnlyList<Cellule> Reconstituer(Dictionary<int, int> vientDe, Carte carte, Cellule depart, Cellule arrivee)
    {
        var chemin = new List<Cellule> { arrivee };
        var courant = arrivee.Identifiant;
        while (vientDe.TryGetValue(courant, out var precedent))
        {
            var cell = carte.Obtenir(precedent);
            if (cell == null) break;
            chemin.Add(cell);
            courant = precedent;
            if (courant == depart.Identifiant) break;
        }
        chemin.Reverse();
        return chemin;
    }
}
