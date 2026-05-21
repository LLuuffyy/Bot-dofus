using System.Collections.Generic;
using System.Linq;
using BotDofus.Divers.Cartes;
using BotDofus.Divers.Combats.Combattants;
using BotDofus.Divers.Jeu.Personnage.Spells;

namespace BotDofus.Divers.Combats.IA;

/// <summary>
/// Décideur IA combat fondé sur la liste ORDONNÉE de <see cref="RegleSort"/>
/// (modèle SynFus/dyshay). Itère les règles par priorité décroissante et
/// retourne la première dont les conditions sont satisfaites.
///
/// Stateless : reçoit l'état combat + config, ne stocke rien. Les compteurs
/// par tour (NombreParTour, TousLesNTours) seront ajoutés en Phase 2/3.
/// </summary>
public static class MoteurReglesCombat
{
    /// <summary>
    /// Résultat d'évaluation : la règle choisie + la cible + les stats du sort
    /// au niveau APPRIS du perso (pas niv 1). Renvoyé à TrameJeu qui construit
    /// le paquet <c>GA300&lt;id&gt;;&lt;cell&gt;</c> et gère le déplacement
    /// préalable si la cible est hors portée.
    /// </summary>
    public sealed record ResultatRegle(
        RegleSort Regle,
        InfoSort Sort,
        Combattant Cible,
        int Distance,
        int CoutPA,
        int PorteeMin,
        int PorteeMax,
        int NiveauAppris);

    /// <summary>
    /// Évalue les règles dans l'ordre de priorité décroissante et retourne la
    /// première utilisable (sort appris, PA OK, portée OK, cible valide,
    /// conditions distance/méthode satisfaites, compteur NombreParTour OK).
    /// Retourne null si aucune règle ne convient → TrameJeu fait le fallback
    /// (déplacement vers ennemi le plus proche ou Gt).
    /// </summary>
    /// <param name="carte">
    /// Carte courante (<see cref="Cartes.Carte"/>). Donne la largeur réelle pour
    /// le calcul distance (Hystoria=15, pas 14 — bug fixé 20/05/2026) ET sert
    /// au test LOS Bresenham (cf. <see cref="Cartes.LigneVisuelle"/>) quand
    /// le sort a <c>NecessiteLOS=true</c>.
    /// </param>
    public static ResultatRegle? Evaluer(Combat combat, ConfigCombat cfg, IReadOnlyDictionary<int, int> sortsAppris, Carte carte)
    {
        int mapWidth = carte.Largeur > 0 ? carte.Largeur : Carte.LargeurParDefaut;
        if (cfg.Regles.Count == 0) return null;

        var moi = combat.Allies.FirstOrDefault(c => c.Identifiant == combat.IdentifiantAllie);
        if (moi == null) return null;

        // Tri par priorité décroissante (l'ordre dans la liste = ordre UI SynFus,
        // la Priorite explicite finalise le tri quand l'ordre liste est ambigu).
        foreach (var regle in cfg.Regles.OrderByDescending(r => r.Priorite))
        {
            string Diag(string raison)
            {
                BotDofus.Utilitaires.Journaux.Journaliseur.Debogue(
                    $"[DECIDEUR] rejet règle #{regle.IdSort} '{regle.Nom}' : {raison}");
                return string.Empty;
            }

            // Sort connu de la base XML dyshay ?
            var sort = BaseSorts.Instance.Trouver(regle.IdSort);
            if (sort == null) { Diag("sort introuvable dans BaseSorts XML"); continue; }

            // Sort réellement appris par le perso ? (paquet SL)
            if (!sortsAppris.TryGetValue(regle.IdSort, out int niveau) || niveau <= 0)
            { Diag($"sort non appris (sortsAppris.Count={sortsAppris.Count})"); continue; }

            // Compteur NombreParTour : la règle a-t-elle déjà été lancée
            // le nombre max de fois autorisé ce tour ? (limite SynFus)
            if (regle.NombreParTour > 0)
            {
                int dejaLance = combat.CompteursRegleParTour.TryGetValue(regle.IdSort, out var cnt) ? cnt : 0;
                if (dejaLance >= regle.NombreParTour) continue;
            }

            // === Conditions Joueur (bloc « Joueur » SynFus) ===
            // Mes PV en % : seuils utilisés ex. pour sorts de soin / sorts panique.
            if (moi.PVMax > 0)
            {
                int mesPvPct = 100 * moi.PV / moi.PVMax;
                if (regle.MesPvInfPourcent.HasValue && mesPvPct >= regle.MesPvInfPourcent.Value) continue;
                if (regle.MesPvSupPourcent.HasValue && mesPvPct <= regle.MesPvSupPourcent.Value) continue;
            }
            // Présence d'une invocation alliée vivante (ex. Sadida Surpuissante/Folle).
            if (regle.SiInvocPresente && !combat.Allies.Any(a => a.EstInvocation && !a.EstMort)) continue;
            // PasSiTacle : nécessite détection « tacle subi » (effets GAS/GA), TODO.

            // === Conditions Situation (bloc « Situation » SynFus) ===
            int nbEnnemisVivants = combat.Ennemis.Count(e => !e.EstMort && e.PV > 0 && e.PVMax > 0);
            if (regle.EnnemisMin.HasValue && nbEnnemisVivants < regle.EnnemisMin.Value) continue;
            if (regle.EnnemisMax.HasValue && nbEnnemisVivants > regle.EnnemisMax.Value) continue;
            if (regle.PremierTour && combat.NumeroTour != 1) continue;
            if (regle.APartirDuTour.HasValue && combat.NumeroTour < regle.APartirDuTour.Value) continue;
            if (regle.TousLesNTours.HasValue && regle.TousLesNTours.Value > 0
                && combat.NumeroTour % regle.TousLesNTours.Value != 0) continue;
            // DernierTour : impossible à connaître a priori (pas d'info serveur).

            // Stats au niveau APPRIS (pas niv 1).
            var stats = sort.Stats(niveau);
            int coutPA = stats?.CoutPA ?? sort.CoutPA;
            int porteeMin = stats?.PorteeMin ?? sort.PorteeMin;
            int porteeMax = stats?.PorteeMax ?? sort.PorteeMax;

            // PA disponibles ?
            if (coutPA > 0 && moi.PA > 0 && coutPA > moi.PA)
            { Diag($"PA insuffisants ({moi.PA}<{coutPA})"); continue; }

            // Cible selon Focus, avec overrides CiblePlusFaible / CiblePlusForte
            // (= forcer la cible vivante ayant le min/max PV, peu importe le Focus).
            Combattant? cible;
            if (regle.CiblePlusFaible)
                cible = combat.Ennemis.Where(e => !e.EstMort && e.PV > 0 && e.PVMax > 0)
                    .OrderBy(e => e.PV).FirstOrDefault();
            else if (regle.CiblePlusForte)
                cible = combat.Ennemis.Where(e => !e.EstMort && e.PV > 0 && e.PVMax > 0)
                    .OrderByDescending(e => e.PV).FirstOrDefault();
            else
                cible = ChoisirCible(regle.Focus, combat, moi, mapWidth, carte);
            if (cible == null) { Diag($"focus={regle.Focus} : aucune cible valide"); continue; }

            // === Conditions Cible (bloc « Cible » SynFus) ===
            if (cible.PVMax > 0)
            {
                int ciblePvPct = 100 * cible.PV / cible.PVMax;
                if (regle.CiblePvInfPourcent.HasValue && ciblePvPct >= regle.CiblePvInfPourcent.Value) continue;
                if (regle.CiblePvSupPourcent.HasValue && ciblePvPct <= regle.CiblePvSupPourcent.Value) continue;
            }

            // Distance Chebyshev (= métrique Dofus pour portées). CAC = dist 1.
            int dist = DistanceDofus(moi.CellulePosition, cible.CellulePosition, mapWidth);
            if (dist < porteeMin) { Diag($"dist {dist} < porteeMin {porteeMin}"); continue; }
            if (porteeMax > 0 && dist > porteeMax) { Diag($"dist {dist} > porteeMax {porteeMax}"); continue; }

            // Conditions de distance SynFus (bloc « Distance »).
            if (regle.DistanceMin.HasValue && dist < regle.DistanceMin.Value)
            { Diag($"dist {dist} < DistanceMin {regle.DistanceMin}"); continue; }
            if (regle.DistanceMax.HasValue && dist > regle.DistanceMax.Value)
            { Diag($"dist {dist} > DistanceMax {regle.DistanceMax}"); continue; }
            if (regle.IgnorerCAC && dist <= 1) { Diag("IgnorerCAC + en CAC"); continue; }
            if (regle.SeulementCAC && dist > 1) { Diag("SeulementCAC + à distance"); continue; }

            // Méthode de lancement SynFus : CAC = adjacent / Distance = pas
            // adjacent / LesDeux = pas de filtre. Aligné dyshay MetodoLanzamiento.
            if (regle.MethodeLancement == MethodeLancement.CAC && dist > 1)
            { Diag($"MethodeLancement=CAC mais dist {dist}>1"); continue; }
            if (regle.MethodeLancement == MethodeLancement.Distance && dist <= 1)
            { Diag($"MethodeLancement=Distance mais dist {dist}<=1"); continue; }

            // LOS Bresenham — si le sort nécessite une ligne de vue, vérifier
            // qu'aucun combattant n'est sur la trajectoire (cf. anti-pattern #4
            // REFERENCE : sans test, le serveur répond GAF échec et la même
            // règle est retentée indéfiniment).
            if (stats?.NecessiteLOS == true && dist > 1)
            {
                var celluleMoi = carte.Obtenir(moi.CellulePosition);
                var celluleCible = carte.Obtenir(cible.CellulePosition);
                if (celluleMoi != null && celluleCible != null)
                {
                    var occupees = new HashSet<int>();
                    foreach (var a in combat.Allies)
                        if (!a.EstMort && a.Identifiant != moi.Identifiant) occupees.Add(a.CellulePosition);
                    foreach (var e in combat.Ennemis)
                        if (!e.EstMort && e.Identifiant != cible.Identifiant) occupees.Add(e.CellulePosition);

                    if (LigneVisuelle.EstObstruee(carte, celluleMoi, celluleCible, occupees))
                        continue;
                }
            }

            return new ResultatRegle(regle, sort, cible, dist, coutPA, porteeMin, porteeMax, niveau);
        }
        return null;
    }

    /// <summary>
    /// Choisit une cible parmi alliés/ennemis vivants selon le Focus de la règle.
    /// Filtre robuste : on jette les combattants à PV=0/PVMax=0 (non initialisés
    /// par un GTM complet — cf. fix log 18:27:09 du bot ciblant un cadavre).
    /// </summary>
    private static Combattant? ChoisirCible(FocusSort focus, Combat combat, Combattant moi, int mapWidth, Carte carte)
    {
        var ennemisVivants = combat.Ennemis.Where(e => !e.EstMort && e.PV > 0 && e.PVMax > 0).ToList();
        var alliesVivants = combat.Allies.Where(a => !a.EstMort && a.PVMax > 0).ToList();
        var allies_humains = alliesVivants.Where(a => !a.EstInvocation && a.Identifiant != moi.Identifiant).ToList();
        var mes_invocations = alliesVivants.Where(a => a.EstInvocation).ToList();

        // Phase 5 — éviter de cibler les invocations adverses pour PlusFaible/PlusFort
        // (les invocations ennemies pop souvent à 10-30 PV → biais qui ferait que
        // « Plus Faible » cible l'invoc au lieu du boss. Priorité au mob principal.)
        var ennemisPrincipaux = ennemisVivants.Where(e => !e.EstInvocation).ToList();
        if (ennemisPrincipaux.Count == 0) ennemisPrincipaux = ennemisVivants;  // fallback si que des invoc

        return focus switch
        {
            // === ENNEMIS ===
            FocusSort.EnnemiLePlusProche => ennemisVivants
                .OrderBy(e => DistanceDofus(moi.CellulePosition, e.CellulePosition, mapWidth))
                .FirstOrDefault(),
            FocusSort.EnnemiLePlusFaible => ennemisPrincipaux
                .OrderBy(e => e.PV)
                .FirstOrDefault(),
            FocusSort.EnnemiLePlusFort => ennemisPrincipaux
                .OrderByDescending(e => e.PV)
                .FirstOrDefault(),
            FocusSort.EnnemiLePlusLoin => ennemisVivants
                .OrderByDescending(e => DistanceDofus(moi.CellulePosition, e.CellulePosition, mapWidth))
                .FirstOrDefault(),

            // === SOI / ALLIÉS ===
            FocusSort.Moi => moi,
            FocusSort.AllieLePlusBlesse => alliesVivants
                .Where(a => a.PV < a.PVMax)
                .OrderBy(a => a.PVMax > 0 ? 100 * a.PV / a.PVMax : 100)
                .FirstOrDefault(),
            FocusSort.AllieLePlusProche => allies_humains
                .OrderBy(a => DistanceDofus(moi.CellulePosition, a.CellulePosition, mapWidth))
                .FirstOrDefault(),
            FocusSort.AlliePlusGrosHeal => alliesVivants
                .Where(a => a.PV < a.PVMax)
                .OrderByDescending(a => a.PVMax - a.PV)  // max points à régénérer
                .FirstOrDefault(),

            // === INVOCATIONS ALLIÉES (mes invoc, ex. Sadida poupées) ===
            FocusSort.InvocationLaPlusBlessee => mes_invocations
                .Where(i => i.PV < i.PVMax)
                .OrderBy(i => i.PVMax > 0 ? 100 * i.PV / i.PVMax : 100)
                .FirstOrDefault(),
            FocusSort.InvocationLaPlusProche => mes_invocations
                .OrderBy(i => DistanceDofus(moi.CellulePosition, i.CellulePosition, mapWidth))
                .FirstOrDefault(),

            // === CELLULES (sorts d'invocation type La Folle / La Bloqueuse) ===
            // Retourne un Combattant FICTIF dont CellulePosition = la cell choisie.
            // Le cast GA300 utilisera donc cette cell comme cible.
            FocusSort.CelluleVide => TrouverCelluleVide(carte, combat, moi, mapWidth),
            FocusSort.CelluleAdjacenteEnnemi => TrouverCelluleAdjacenteEnnemi(carte, combat, moi, mapWidth),
            _ => null
        };
    }

    /// <summary>
    /// Cherche une cellule vide marchable adjacente (Chebyshev=1) à MOI.
    /// Utilisée pour invocations type La Folle (Sadida) qui pop adjacent au caster.
    /// </summary>
    private static Combattant? TrouverCelluleVide(Carte carte, Combat combat, Combattant moi, int mapWidth)
    {
        var occupees = new HashSet<int>(
            combat.Allies.Where(a => !a.EstMort).Select(a => a.CellulePosition)
                .Concat(combat.Ennemis.Where(e => !e.EstMort).Select(e => e.CellulePosition)));
        var (xMoi, yMoi) = Cellule.CalculerCoordonnees(moi.CellulePosition, mapWidth);

        foreach (var c in carte.Cellules)
        {
            if (c == null || !c.EstMarchable || c.IdInteractif >= 0) continue;
            if (occupees.Contains(c.Identifiant)) continue;
            int dx = System.Math.Abs(c.X - xMoi);
            int dy = System.Math.Abs(c.Y - yMoi);
            int dist = System.Math.Max(dx, dy);
            if (dist != 1) continue;  // strictement adjacent
            return new CombattantMonstre { Identifiant = -9000, CellulePosition = c.Identifiant, Nom = "(cell vide)" };
        }
        return null;
    }

    /// <summary>
    /// Cherche une cellule vide marchable adjacente à l'ennemi le plus proche.
    /// Utilisée pour invocations de blocage (La Bloqueuse Sadida) qui pop entre
    /// nous et l'ennemi pour stopper son rush.
    /// </summary>
    private static Combattant? TrouverCelluleAdjacenteEnnemi(Carte carte, Combat combat, Combattant moi, int mapWidth)
    {
        var ennemi = combat.Ennemis
            .Where(e => !e.EstMort && e.PV > 0 && e.PVMax > 0)
            .OrderBy(e => DistanceDofus(moi.CellulePosition, e.CellulePosition, mapWidth))
            .FirstOrDefault();
        if (ennemi == null) return null;
        var occupees = new HashSet<int>(
            combat.Allies.Where(a => !a.EstMort).Select(a => a.CellulePosition)
                .Concat(combat.Ennemis.Where(e => !e.EstMort).Select(e => e.CellulePosition)));
        var (xEnn, yEnn) = Cellule.CalculerCoordonnees(ennemi.CellulePosition, mapWidth);

        foreach (var c in carte.Cellules)
        {
            if (c == null || !c.EstMarchable || c.IdInteractif >= 0) continue;
            if (occupees.Contains(c.Identifiant)) continue;
            int dx = System.Math.Abs(c.X - xEnn);
            int dy = System.Math.Abs(c.Y - yEnn);
            int dist = System.Math.Max(dx, dy);
            if (dist != 1) continue;  // strictement adjacent à l'ennemi
            return new CombattantMonstre { Identifiant = -9001, CellulePosition = c.Identifiant, Nom = "(adj. ennemi)" };
        }
        return null;
    }

    /// <summary>
    /// Distance Chebyshev `max(|dx|, |dy|)` — métrique canonique pour la PORTÉE
    /// des sorts Dofus 1.29 (cases diagonales = distance 1). Vérifié log 22:40.
    /// Le déplacement combat utilise Manhattan (pathfinder 4-dir), mais la
    /// portée de cast reste Chebyshev.
    /// </summary>
    private static int DistanceDofus(int idA, int idB, int mapWidth)
    {
        var (xA, yA) = Cellule.CalculerCoordonnees(idA, mapWidth);
        var (xB, yB) = Cellule.CalculerCoordonnees(idB, mapWidth);
        return System.Math.Max(System.Math.Abs(xA - xB), System.Math.Abs(yA - yB));
    }
}
