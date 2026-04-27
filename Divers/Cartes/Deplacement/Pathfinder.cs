using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BotDofus.Utilitaires.Crypto;

namespace BotDofus.Divers.Cartes.Deplacement;

/// <summary>
/// Algorithme A* pour les cartes Dofus 1.29.
///
/// Trouve le chemin optimal entre deux cellules d'une <see cref="Carte"/> en évitant les
/// obstacles, autres entités et cellules de téléport (sauf si la destination EST une teleport).
///
/// Voisinage : 8 directions (4 ortho + 4 diagonales) — Dofus permet le déplacement diagonal.
///
/// Référence : portage de dyshay/Bot-Dofus-Retro Pathfinder.cs.
/// </summary>
public static class Pathfinder
{
    /// <summary>
    /// Calcule un chemin de <paramref name="depart"/> vers <paramref name="arrivee"/>.
    /// </summary>
    /// <param name="carte">Carte courante (avec topologie).</param>
    /// <param name="depart">Cellule de départ.</param>
    /// <param name="arrivee">Cellule d'arrivée.</param>
    /// <param name="cellulesInterdites">Cellules à éviter (autres joueurs, mobs, ressources occupées).</param>
    /// <param name="arreterDevant">Si true, s'arrête à <paramref name="distanceArret"/> cases avant si la cible est non marchable.</param>
    /// <param name="distanceArret">Distance d'arrêt si arreterDevant=true.</param>
    /// <returns>Liste de cellules du chemin (depart inclus, arrivee inclus), ou null si impossible.</returns>
    public static List<Cellule>? Trouver(
        Carte carte,
        Cellule depart,
        Cellule arrivee,
        ICollection<Cellule>? cellulesInterdites = null,
        bool arreterDevant = false,
        int distanceArret = 1)
    {
        if (depart == null || arrivee == null) return null;

        cellulesInterdites ??= new List<Cellule>();

        // Reset A* state pour toutes les cellules touchées (lazy : on reset au fur et à mesure)
        depart.ResetA();

        var ouvertes = new List<Cellule> { depart };
        var fermees = new HashSet<Cellule>(cellulesInterdites);
        // Si la destination est dans interdites, on l'enlève (cas d'une ressource ciblée)
        fermees.Remove(arrivee);

        while (ouvertes.Count > 0)
        {
            // Sélectionne la cellule avec le plus petit f, tie-break sur g (le plus grand g d'abord)
            int idx = 0;
            for (int i = 1; i < ouvertes.Count; i++)
            {
                if (ouvertes[i].CouF < ouvertes[idx].CouF
                    || (ouvertes[i].CouF == ouvertes[idx].CouF && ouvertes[i].CouG > ouvertes[idx].CouG))
                {
                    idx = i;
                }
            }
            var courante = ouvertes[idx];

            // Test arrêt anticipé devant la cible
            if (arreterDevant
                && Distance(courante, arrivee) <= distanceArret
                && !arrivee.EstMarchable)
            {
                return Reconstruire(depart, courante);
            }

            if (courante == arrivee)
            {
                return Reconstruire(depart, arrivee);
            }

            ouvertes.RemoveAt(idx);
            fermees.Add(courante);

            foreach (var voisin in VoisinsAdjacents(carte, courante))
            {
                if (fermees.Contains(voisin)) continue;
                if (!voisin.EstMarchable) continue;
                if (voisin.EstCelluleTeleport() && voisin != arrivee) continue;

                int gTemporaire = courante.CouG + Distance(voisin, courante);

                bool dejaOuvert = ouvertes.Contains(voisin);
                if (!dejaOuvert)
                {
                    voisin.ResetA();
                    ouvertes.Add(voisin);
                }
                else if (gTemporaire >= voisin.CouG)
                {
                    continue;
                }

                voisin.CouG = gTemporaire;
                voisin.CouH = Distance(voisin, arrivee);
                voisin.CouF = voisin.CouG + voisin.CouH;
                voisin.ParentNoeud = courante;
            }
        }

        return null;
    }

    /// <summary>
    /// Encode un chemin pour l'envoi serveur (préfixe "GA001" + ce résultat).
    ///
    /// Format Dofus : pour chaque CHANGEMENT de direction, on émet
    ///   &lt;direction&gt;&lt;cellule_destination_2_chars&gt;
    /// Cas optimisé si chemin de 2 cellules (pas direct) : un seul bloc dir+cellule.
    ///
    /// Référence : dyshay PathFinderUtil.get_Pathfinding_Limpio.
    /// </summary>
    public static string EncoderChemin(IReadOnlyList<Cellule> chemin)
    {
        if (chemin.Count <= 1) return string.Empty;

        var destination = chemin[chemin.Count - 1];

        if (chemin.Count == 2)
        {
            char dir = chemin[0].DirectionVers(chemin[1]);
            return dir + HashCarte.EncoderCellule(destination.Identifiant);
        }

        var sb = new StringBuilder();
        char directionPrecedente = chemin[0].DirectionVers(chemin[1]);

        for (int i = 2; i < chemin.Count; i++)
        {
            char direction = chemin[i - 1].DirectionVers(chemin[i]);
            if (direction != directionPrecedente)
            {
                sb.Append(directionPrecedente);
                sb.Append(HashCarte.EncoderCellule(chemin[i - 1].Identifiant));
                directionPrecedente = direction;
            }
        }

        sb.Append(directionPrecedente);
        sb.Append(HashCarte.EncoderCellule(destination.Identifiant));
        return sb.ToString();
    }

    /// <summary>
    /// Calcule le packet GA001 complet à envoyer au serveur pour faire bouger le perso.
    /// </summary>
    public static string PaquetDeplacement(IReadOnlyList<Cellule> chemin)
        => "GA001" + EncoderChemin(chemin);

    // -------------------------------------------------------------
    // Helpers internes
    // -------------------------------------------------------------

    private static int Distance(Cellule a, Cellule b)
        => (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);

    private static List<Cellule> Reconstruire(Cellule depart, Cellule fin)
    {
        var chemin = new List<Cellule>();
        var n = fin;
        while (n != depart)
        {
            chemin.Add(n);
            if (n.ParentNoeud == null) break;
            n = n.ParentNoeud;
        }
        chemin.Add(depart);
        chemin.Reverse();
        return chemin;
    }

    /// <summary>
    /// Voisins 8-directions (4 orthogonaux + 4 diagonaux) dans la carte.
    /// On utilise les coordonnées (x, y) calculées par <see cref="Cellule"/>.
    /// </summary>
    private static IEnumerable<Cellule> VoisinsAdjacents(Carte carte, Cellule centre)
    {
        var deltas = new (int dx, int dy)[]
        {
            ( 1,  0), (-1,  0), ( 0,  1), ( 0, -1),
            ( 1,  1), ( 1, -1), (-1,  1), (-1, -1),
        };

        foreach (var (dx, dy) in deltas)
        {
            int x = centre.X + dx;
            int y = centre.Y + dy;
            var c = carte.Cellules.FirstOrDefault(cel => cel != null && cel.X == x && cel.Y == y);
            if (c != null) yield return c;
        }
    }
}
