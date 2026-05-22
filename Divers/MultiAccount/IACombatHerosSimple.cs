using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BotDofus.Commun.Reseau;
using BotDofus.Divers.Cartes;
using BotDofus.Divers.Cartes.Deplacement;
using BotDofus.Divers.Combats;
using BotDofus.Divers.Combats.IA;
using BotDofus.Divers.Jeu.Personnage.Spells;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.MultiAccount;

/// <summary>
/// IA de combat des héros liés en mode héros Abrak. Pipeline complet
/// avec déplacement A*, ligne de vue (Bresenham iso) et choix de cellule
/// selon <see cref="ModeCombat"/> :
///
/// <list type="bullet">
///   <item><b>Agressif</b> : minimise la distance à l'ennemi (cellule de
///         cast la plus proche), bourrine au CAC quand possible.</item>
///   <item><b>Eloigne</b> : maximise la distance dans la portée du sort,
///         kite si possible.</item>
///   <item><b>Fuyard</b> : maximise la distance, skip cast si PV bas.</item>
///   <item><b>Equilibre</b> : vise <c>DistancePreferee</c> en priorité.</item>
/// </list>
///
/// Règle d'or (demandée par l'user 2026-05-22) : <b>prioriser le cast</b>
/// par rapport à la position idéale. Mieux vaut taper à 5 cases (en portée)
/// que rester à la distance préférée 7 sans rien lancer.
///
/// Pipeline par tour :
/// <list type="number">
///   <item>Sélectionne l'ennemi le plus pertinent (le plus proche).</item>
///   <item>Pour chaque règle (priorité décroissante) :
///         <list type="bullet">
///           <item>Cast direct si position + portée + LOS OK.</item>
///           <item>Sinon, cherche cellule de cast atteignable (PM dispo).</item>
///           <item>Bouge + cast.</item>
///         </list></item>
///   <item>Si aucune action : approche selon mode (consomme PM).</item>
///   <item><c>Gt</c> en fin de tour.</item>
/// </list>
/// </summary>
public sealed class IACombatHerosSimple
{
    private const int DelaiAvantAckMs = 350;
    private const int DelaiEntreActionsMs = 1100;
    private const int DelaiAvantFinTourMs = 700;
    /// <summary>Délai par case de déplacement (cf. cadence GKK0 standard, log dyshay).</summary>
    private const int DelaiParCasePmMs = 330;

    public static async Task JouerTourAsync(
        MembreHeros membre,
        Combat combat,
        Carte? carte,
        SessionProxy session)
    {
        if (membre is null || combat is null || session is null) return;

        if (carte is null)
        {
            Journaliseur.Avertir($"[IA-HEROS:{membre.Nom}] Pas de carte → Gt direct");
            await EnvoyerFinTourAsync(session);
            return;
        }
        if (membre.SortsAppris.Count == 0)
        {
            Journaliseur.Info($"[IA-HEROS:{membre.Nom}] Aucun sort connu → Gt direct");
            await EnvoyerFinTourAsync(session);
            return;
        }
        var cfg = membre.ConfigCombat;
        if (cfg is null || cfg.Regles.Count == 0)
        {
            Journaliseur.Info($"[IA-HEROS:{membre.Nom}] ConfigCombat vide → Gt direct");
            await EnvoyerFinTourAsync(session);
            return;
        }

        var membreCell = carte.Obtenir(membre.Cellule);
        if (membreCell is null)
        {
            Journaliseur.Avertir($"[IA-HEROS:{membre.Nom}] Cellule {membre.Cellule} introuvable → Gt");
            await EnvoyerFinTourAsync(session);
            return;
        }

        var ennemis = combat.Ennemis.Where(e => !e.EstMort && e.CellulePosition > 0).ToList();
        if (ennemis.Count == 0)
        {
            Journaliseur.Info($"[IA-HEROS:{membre.Nom}] Plus d'ennemis → Gt");
            await EnvoyerFinTourAsync(session);
            return;
        }

        // Cible : l'ennemi le plus proche (Chebyshev). On pourrait raffiner via
        // Focus dans la règle, mais V1 = simple.
        var cibleCell = ChoisirCibleProche(carte, membreCell, ennemis);
        if (cibleCell is null)
        {
            Journaliseur.Avertir($"[IA-HEROS:{membre.Nom}] Aucune cible accessible → Gt");
            await EnvoyerFinTourAsync(session);
            return;
        }

        Journaliseur.Info(
            $"[IA-HEROS:{membre.Nom}] Tour — cell {membreCell.Identifiant}, "
            + $"PA={membre.Pa}, PM={membre.Pm}, cible cell {cibleCell.Identifiant} "
            + $"(dist {membreCell.DistanceChebyshev(cibleCell)})");

        // Cellules occupées par les autres combattants (pour LOS + pathfinder).
        var cellsOccupees = SnapshotCellsOccupees(combat, membre.IdJeu);

        int paRestant = membre.Pa;
        int pmRestant = membre.Pm;
        int casts = 0;

        var reglesTriees = cfg.Regles
            .Where(r => r != null && r.IdSort > 0)
            .OrderByDescending(r => r.Priorite)
            .ToList();
        var compteursParSort = new Dictionary<int, int>();

        // Boucle multi-cast.
        bool actionTrouvee;
        do
        {
            actionTrouvee = false;
            foreach (var regle in reglesTriees)
            {
                int idSort = regle.IdSort;
                if (!membre.SortsAppris.TryGetValue(idSort, out var niveau) || niveau <= 0) continue;
                if (compteursParSort.TryGetValue(idSort, out var deja) && deja >= regle.NombreParTour) continue;

                var infoSort = BaseSorts.Instance.Trouver(idSort);
                if (infoSort is null) continue;
                var stats = infoSort.Stats(niveau);
                if (stats is null || stats.CoutPA <= 0 || stats.CoutPA > paRestant) continue;

                // (A) Cast direct depuis position actuelle ?
                int distActuelle = membreCell.DistanceChebyshev(cibleCell);
                if (distActuelle >= stats.PorteeMin && distActuelle <= stats.PorteeMax
                    && LosOk(stats, membreCell, cibleCell, cellsOccupees))
                {
                    Journaliseur.Info(
                        $"[IA-HEROS:{membre.Nom}] cast #{idSort} niv{niveau} sur cell {cibleCell.Identifiant} "
                        + $"(direct, dist {distActuelle}, coût {stats.CoutPA} PA)");
                    if (!await CastAsync(session, idSort, cibleCell.Identifiant))
                    {
                        goto fin;
                    }
                    paRestant -= stats.CoutPA;
                    casts++;
                    compteursParSort[idSort] = deja + 1;
                    actionTrouvee = true;
                    break;
                }

                // (B) Sinon, chercher cellule en portée+LoS atteignable.
                if (pmRestant <= 0) continue;
                var candidats = TrouverCellulesCast(
                    carte, cibleCell, stats, cellsOccupees, membreCell, pmRestant);
                if (candidats.Count == 0) continue;

                var ordre = TrierCellsSelonMode(candidats, membreCell, cibleCell, cfg, stats);
                CheminChoisi? choisi = null;
                foreach (var cand in ordre)
                {
                    var interdits = ConstruireInterdits(carte, combat, membre.IdJeu);
                    var chemin = Pathfinder.Trouver(carte, membreCell, cand, interdits, combat: true);
                    if (chemin is null || chemin.Count < 2) continue;
                    int pmRequis = chemin.Count - 1;
                    if (pmRequis > pmRestant) continue;
                    choisi = new CheminChoisi(chemin, pmRequis, cand);
                    break;
                }
                if (choisi is null) continue;

                Journaliseur.Info(
                    $"[IA-HEROS:{membre.Nom}] déplacement {membreCell.Identifiant}→{choisi.Cellule.Identifiant} "
                    + $"({choisi.PmRequis} PM) puis cast #{idSort} niv{niveau} sur cell {cibleCell.Identifiant}");
                if (!await DeplacerAsync(session, choisi.Chemin))
                {
                    goto fin;
                }
                membreCell = choisi.Cellule;
                membre.Cellule = membreCell.Identifiant;
                pmRestant -= choisi.PmRequis;
                cellsOccupees = SnapshotCellsOccupees(combat, membre.IdJeu);

                await Task.Delay(DelaiEntreActionsMs).ConfigureAwait(false);

                if (!await CastAsync(session, idSort, cibleCell.Identifiant))
                {
                    goto fin;
                }
                paRestant -= stats.CoutPA;
                casts++;
                compteursParSort[idSort] = deja + 1;
                actionTrouvee = true;
                break;
            }
        } while (actionTrouvee && paRestant > 0);

    fin:
        // (C) Aucun cast trouvé ce tour : on consomme quand même les PM pour
        // se positionner selon le mode (préparer le tour suivant). Sauf en
        // Equilibre/Tactique qui restent sur place (préservent les PM).
        if (casts == 0 && pmRestant > 0
            && cfg.Mode != ModeCombat.Equilibre)
        {
            await ApprocherAsync(membre, membreCell, cibleCell, carte, combat, cfg, pmRestant, session);
        }

        Journaliseur.Info(
            $"[IA-HEROS:{membre.Nom}] Fin tour ({casts} cast(s), PA restants {paRestant}, PM restants {pmRestant})");
        await Task.Delay(DelaiAvantFinTourMs).ConfigureAwait(false);
        await EnvoyerFinTourAsync(session);
    }

    // ----- Sélection cible / cellules / mode -----

    private static Cellule? ChoisirCibleProche(
        Carte carte,
        Cellule moi,
        IEnumerable<Combats.Combattants.Combattant> ennemis)
    {
        Cellule? best = null;
        int meilleureDist = int.MaxValue;
        foreach (var e in ennemis)
        {
            var ec = carte.Obtenir(e.CellulePosition);
            if (ec is null) continue;
            int d = moi.DistanceChebyshev(ec);
            if (d < meilleureDist)
            {
                meilleureDist = d;
                best = ec;
            }
        }
        return best;
    }

    /// <summary>
    /// Toutes les cellules d'où le sort <paramref name="stats"/> peut être lancé
    /// sur <paramref name="cible"/> : distance ∈ [min, max], LOS OK (si requis),
    /// cell libre. Cap distance maximale au pathfinder via budget PM.
    /// </summary>
    private static List<Cellule> TrouverCellulesCast(
        Carte carte,
        Cellule cible,
        StatsNiveau stats,
        ISet<int> cellsOccupees,
        Cellule depart,
        int pmDispo)
    {
        var cellsValides = new List<Cellule>(64);
        // Balayage par déplacement Chebyshev = on regarde les cells autour de la cible.
        int rmin = Math.Max(1, stats.PorteeMin);
        int rmax = Math.Max(rmin, stats.PorteeMax);
        for (int dx = -rmax; dx <= rmax; dx++)
        {
            for (int dy = -rmax; dy <= rmax; dy++)
            {
                int chebyCible = Math.Max(Math.Abs(dx), Math.Abs(dy));
                if (chebyCible < rmin || chebyCible > rmax) continue;
                var cell = carte.ObtenirParCoords(cible.X + dx, cible.Y + dy);
                if (cell is null) continue;
                if (cell.Identifiant == cible.Identifiant) continue;
                if (cellsOccupees.Contains(cell.Identifiant)) continue;
                if (!cell.EstMarchable && cell.Identifiant != depart.Identifiant) continue;
                // Pré-filtre distance à départ pour pathfinder (gain perf).
                int chebyDepart = depart.DistanceChebyshev(cell);
                if (chebyDepart > pmDispo + 1) continue;
                if (stats.NecessiteLOS && LigneVisuelle.EstObstruee(cell, cible, cellsOccupees)) continue;
                cellsValides.Add(cell);
            }
        }
        return cellsValides;
    }

    /// <summary>Trie les candidats selon le <see cref="ModeCombat"/>.</summary>
    private static IEnumerable<Cellule> TrierCellsSelonMode(
        List<Cellule> candidats, Cellule depart, Cellule cible, ConfigCombat cfg, StatsNiveau stats)
    {
        // Critère commun : moins de PM = mieux (préserve mobilité), sauf pour
        // les modes qui veulent vraiment éloigner.
        int distancePref = Math.Clamp(cfg.DistancePreferee, stats.PorteeMin, stats.PorteeMax);
        int distanceMinEloigne = Math.Clamp(cfg.DistanceMinEloigne, stats.PorteeMin, stats.PorteeMax);
        return cfg.Mode switch
        {
            // Agressif : se colle au mob (distance minimale à la cible).
            ModeCombat.Agressif => candidats
                .OrderBy(c => c.DistanceChebyshev(cible))
                .ThenBy(c => depart.DistanceChebyshev(c)),
            // Eloigne / Fuyard : max distance à la cible mais en portée.
            ModeCombat.Eloigne => candidats
                .OrderByDescending(c => c.DistanceChebyshev(cible))
                .ThenBy(c => depart.DistanceChebyshev(c)),
            ModeCombat.Fuyard => candidats
                .OrderByDescending(c => c.DistanceChebyshev(cible))
                .ThenBy(c => depart.DistanceChebyshev(c)),
            // Equilibre : vise distancePref + minimise les PM (cast prioritaire).
            _ => candidats
                .OrderBy(c => Math.Abs(c.DistanceChebyshev(cible) - distancePref))
                .ThenBy(c => depart.DistanceChebyshev(c)),
        };
    }

    private static HashSet<int> SnapshotCellsOccupees(Combat combat, int idMembreActif)
    {
        var set = new HashSet<int>();
        foreach (var a in combat.Allies)
            if (!a.EstMort && a.CellulePosition > 0 && a.Identifiant != idMembreActif)
                set.Add(a.CellulePosition);
        foreach (var e in combat.Ennemis)
            if (!e.EstMort && e.CellulePosition > 0)
                set.Add(e.CellulePosition);
        return set;
    }

    private static List<Cellule> ConstruireInterdits(Carte carte, Combat combat, int idMembreActif)
    {
        var interdits = new List<Cellule>();
        foreach (var c in combat.Allies.Concat(combat.Ennemis))
        {
            if (c.EstMort || c.CellulePosition <= 0) continue;
            if (c.Identifiant == idMembreActif) continue;
            var cell = carte.Obtenir(c.CellulePosition);
            if (cell != null) interdits.Add(cell);
        }
        return interdits;
    }

    private static bool LosOk(StatsNiveau stats, Cellule a, Cellule b, ISet<int> cellsOccupees)
        => !stats.NecessiteLOS || !LigneVisuelle.EstObstruee(a, b, cellsOccupees);

    // ----- Mouvement de repli (rien à caster) -----

    private static async Task ApprocherAsync(
        MembreHeros membre, Cellule depart, Cellule cible,
        Carte carte, Combat combat, ConfigCombat cfg, int pmDispo,
        SessionProxy session)
    {
        // On veut aller le plus près possible de la cible (Agressif) ou s'éloigner (Eloigne/Fuyard).
        Cellule? destination = null;
        if (cfg.Mode == ModeCombat.Agressif)
        {
            // Cell adjacente à la cible la plus accessible.
            destination = ChoisirCellAdjacente(carte, cible, combat, membre.IdJeu);
        }
        else if (cfg.Mode == ModeCombat.Eloigne || cfg.Mode == ModeCombat.Fuyard)
        {
            // Cell la plus éloignée à portée pmDispo.
            destination = ChoisirCellFuite(carte, depart, cible, combat, membre.IdJeu, pmDispo);
        }
        if (destination is null || destination.Identifiant == depart.Identifiant) return;

        var interdits = ConstruireInterdits(carte, combat, membre.IdJeu);
        var chemin = Pathfinder.Trouver(carte, depart, destination, interdits, combat: true);
        if (chemin is null || chemin.Count < 2) return;

        // Tronque au budget PM si nécessaire.
        if (chemin.Count - 1 > pmDispo)
        {
            chemin = chemin.Take(pmDispo + 1).ToList();
        }
        Journaliseur.Info(
            $"[IA-HEROS:{membre.Nom}] approche {depart.Identifiant}→{chemin[^1].Identifiant} ({chemin.Count - 1} PM)");
        await DeplacerAsync(session, chemin);
        membre.Cellule = chemin[^1].Identifiant;
    }

    private static Cellule? ChoisirCellAdjacente(Carte carte, Cellule cible, Combat combat, int idMembre)
    {
        var occ = SnapshotCellsOccupees(combat, idMembre);
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var c = carte.ObtenirParCoords(cible.X + dx, cible.Y + dy);
                if (c is null) continue;
                if (occ.Contains(c.Identifiant)) continue;
                if (!c.EstMarchable) continue;
                return c;
            }
        return null;
    }

    private static Cellule? ChoisirCellFuite(
        Carte carte, Cellule depart, Cellule cible, Combat combat, int idMembre, int pmDispo)
    {
        var occ = SnapshotCellsOccupees(combat, idMembre);
        Cellule? best = null;
        int meilleureDist = -1;
        for (int dx = -pmDispo; dx <= pmDispo; dx++)
            for (int dy = -pmDispo; dy <= pmDispo; dy++)
            {
                if (Math.Max(Math.Abs(dx), Math.Abs(dy)) > pmDispo) continue;
                var c = carte.ObtenirParCoords(depart.X + dx, depart.Y + dy);
                if (c is null) continue;
                if (occ.Contains(c.Identifiant)) continue;
                if (!c.EstMarchable) continue;
                int d = c.DistanceChebyshev(cible);
                if (d > meilleureDist)
                {
                    meilleureDist = d;
                    best = c;
                }
            }
        return best;
    }

    // ----- Réseau -----

    private static async Task<bool> CastAsync(SessionProxy session, int idSort, int cellCible)
    {
        try
        {
            await session.EnvoyerAuServeurAsync($"GA300{idSort};{cellCible}").ConfigureAwait(false);
            await Task.Delay(DelaiAvantAckMs).ConfigureAwait(false);
            await session.EnvoyerAuServeurAsync("GKK0").ConfigureAwait(false);
            await Task.Delay(DelaiEntreActionsMs).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[IA-HEROS] échec cast #{idSort}@{cellCible} : {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> DeplacerAsync(SessionProxy session, IReadOnlyList<Cellule> chemin)
    {
        try
        {
            var encodage = Pathfinder.EncoderChemin(chemin);
            if (string.IsNullOrEmpty(encodage)) return false;
            await session.EnvoyerAuServeurAsync($"GA001{encodage}").ConfigureAwait(false);
            // Attente proportionnelle au nombre de pas.
            int delaiAttente = Math.Max(150, DelaiParCasePmMs * (chemin.Count - 1));
            await Task.Delay(delaiAttente).ConfigureAwait(false);
            await session.EnvoyerAuServeurAsync("GKK0").ConfigureAwait(false);
            await Task.Delay(150).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[IA-HEROS] échec déplacement : {ex.Message}");
            return false;
        }
    }

    private static async Task EnvoyerFinTourAsync(SessionProxy session)
    {
        try
        {
            await session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[IA-HEROS] échec Gt : {ex.Message}");
        }
    }

    private sealed record CheminChoisi(IReadOnlyList<Cellule> Chemin, int PmRequis, Cellule Cellule);
}
