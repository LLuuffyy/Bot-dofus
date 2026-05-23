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
/// IA de combat des héros liés en mode héros Abrak. Pipeline unifié au master :
/// utilise <see cref="MoteurReglesCombat"/> SynFus (Focus, conditions PV%,
/// LOS Bresenham, IgnorerCAC/SeulementCAC, MethodeLancement, NombreParTour /
/// NombreParCible, CooldownTours, etc.) + pré-mouvement selon
/// <see cref="ModeCombat"/> + déplacement A* 4-dir pour atteindre une cell
/// de cast + repositionnement fin de tour style dyshay <c>get_Fin_Turno</c>.
///
/// Pipeline (refonte 2026-05-22, audit logs cell 253 + spams sans rush) :
/// <list type="number">
///   <item><b>Pré-mouvement</b> selon Mode (Agressif rush CAC, Fuyard recule,
///         Eloigne kite à porteeMax, Equilibre vise DistancePreferee).</item>
///   <item><b>Boucle multi-cast SynFus</b> : <see cref="MoteurReglesCombat.Evaluer"/>
///         avec caster explicite = ce héros, exécute la règle, recommence
///         tant qu'une règle reste utilisable.</item>
///   <item><b>Fallback déplacement+cast</b> si aucune règle SynFus n'est en
///         portée mais le héros a des PM : cherche cell de cast atteignable
///         pour la 1re règle utilisable, bouge, cast.</item>
///   <item><b>get_Fin_Turno</b> : repositionnement final selon Mode
///         (Agressif sans CAC → avance, Fuyard <8 → recule, Fuyard >12 → avance).</item>
///   <item><c>Gt</c> + délai humanisé.</item>
/// </list>
///
/// Différences vs master : pas de pipeline événementiel (Combat.MouvementBotConfirme
/// = master only, le héros utilise <c>Task.Delay(330ms × nbPas)</c>),
/// pas de consommable de soin (Phase ultérieure).
/// </summary>
public static class IACombatHerosSimple
{
    private const int DelaiAvantFinTourMs = 700;
    /// <summary>Garde-fou anti-boucle multi-cast.</summary>
    private const int MaxCastsParTour = 8;

    public static async Task JouerTourAsync(
        MembreHeros membre,
        Combat combat,
        Carte? carte,
        SessionProxy session)
    {
        if (membre is null || combat is null || session is null) return;

        string tag = $"IA-HEROS:{membre.Nom}";

        if (carte is null)
        {
            Journaliseur.Avertir($"[{tag}] Pas de carte → Gt direct");
            await EnvoyerFinTourAsync(session);
            return;
        }
        if (membre.SortsAppris.Count == 0)
        {
            Journaliseur.Info($"[{tag}] Aucun sort connu → Gt direct");
            await EnvoyerFinTourAsync(session);
            return;
        }
        var cfg = membre.ConfigCombat;
        if (cfg is null || cfg.Regles.Count == 0)
        {
            Journaliseur.Info($"[{tag}] ConfigCombat vide → Gt direct");
            await EnvoyerFinTourAsync(session);
            return;
        }

        // Trouver le Combattant qui représente CE héros dans Combat.Allies.
        var moi = combat.Allies.FirstOrDefault(a => a.Identifiant == membre.IdJeu);
        if (moi is null)
        {
            Journaliseur.Avertir($"[{tag}] Combattant id={membre.IdJeu} absent de Combat.Allies → Gt");
            await EnvoyerFinTourAsync(session);
            return;
        }
        // Sync cell du Combattant sur la valeur de MembreHeros (au cas où le
        // GTM n'a pas suivi un précédent déplacement de l'IA).
        if (membre.Cellule > 0) moi.CellulePosition = membre.Cellule;

        // Aligne le flag turbo sur la config héros — chaque héros peut avoir
        // sa propre préférence ; en pratique on suit la config du master qui
        // pilote déjà le flag global (cf. TrameJeu.JouerTourCombatAsync).
        TimingsCombat.AppliquerConfig(cfg);

        var ennemisVivants = combat.Ennemis.Where(e => !e.EstMort && e.PV > 0 && e.PVMax > 0).ToList();
        if (ennemisVivants.Count == 0)
        {
            Journaliseur.Info($"[{tag}] Plus d'ennemis vivants → Gt");
            await EnvoyerFinTourAsync(session);
            return;
        }

        int delaiReaction = TimingsCombat.Delai(1100, 1700);
        Journaliseur.Info(
            $"[{tag}] Tour — cell {moi.CellulePosition}, PA={moi.PA}, PM={moi.PM}, "
            + $"alliés={combat.Allies.Count}, ennemis={ennemisVivants.Count}, "
            + $"mode={cfg.Mode}, règles={cfg.Regles.Count}, délai réaction={delaiReaction}ms");
        await Task.Delay(delaiReaction).ConfigureAwait(false);

        // === ORDRE INVERSÉ (refonte 2026-05-22) ===
        // CAST D'ABORD depuis la position actuelle (préserve le tacle CAC),
        // déplacement uniquement si aucun sort en portée (fallback),
        // repositionnement final à la fin du tour.
        // Avant : PRE-MOVE → cast → FIN-TOUR. Problème : si le perso bougeait
        // depuis un CAC (où il tacle un ennemi), il perdait son tacle pour
        // se rapprocher d'un autre mob → tour gaspillé.

        // === (1) BOUCLE MULTI-CAST SynFus depuis position actuelle ===
        int castsEffectues = 0;
        while (castsEffectues < MaxCastsParTour)
        {
            // Resync cell allié sur position autoritative du membre.
            if (membre.Cellule > 0) moi.CellulePosition = membre.Cellule;

            var resultat = MoteurReglesCombat.Evaluer(combat, cfg, membre.SortsAppris, carte, moi);
            if (resultat == null) break;

            bool castOk = await EnvoyerCastSynFusAsync(tag, moi, combat, carte, resultat, session)
                .ConfigureAwait(false);
            if (!castOk) break;
            castsEffectues++;
        }

        // === (2) FALLBACK déplacement + cast si AUCUN sort en portée ===
        // Cas typique : hors portée tous sorts → faut bouger pour atteindre.
        if (castsEffectues == 0 && moi.PM > 0)
        {
            bool castFallbackOk = await TenterDeplacementPuisCastAsync(
                tag, moi, membre, combat, carte, cfg, ennemisVivants, session).ConfigureAwait(false);
            if (castFallbackOk) castsEffectues = 1;
        }

        // === (3) REPOSITIONNEMENT FIN DE TOUR — anciennement « PRE-MOVE »  ===
        // Après les casts, on positionne pour le tour suivant. C'est ICI que
        // le smart-positioning du MoteurTactique opère, pas en début de tour
        // (sinon perte du tacle CAC). Skip si déjà au CAC en Agressif.
        if (moi.PM > 0)
        {
            await PreMouvementSelonModeAsync(tag, moi, membre, combat, carte, cfg, ennemisVivants, session)
                .ConfigureAwait(false);
        }

        // === (4) GET_FIN_TURNO style dyshay : repositionnement complémentaire ===
        // RepositionnerFinTourAsync gère uniquement les cas extrêmes
        // (Fuyard <8 / >12, etc.) sur les PM résiduels.
        if (moi.PM > 0)
        {
            await RepositionnerFinTourAsync(tag, moi, membre, combat, carte, cfg, session)
                .ConfigureAwait(false);
        }

        Journaliseur.Info(
            $"[{tag}] Fin tour ({castsEffectues} cast(s), PA restants {moi.PA}, PM restants {moi.PM})");

        // SÉCURITÉ Gt : on envoie un GKK0 SYSTÉMATIQUEMENT avant le Gt.
        // Pourquoi : le PreMouvementSelonModeAsync / RepositionnerFinTourAsync
        // post-cast envoient un GA001 final qui doit être fermé par un GKK0
        // sinon le serveur Hystoria ignore le Gt suivant (forensic
        // 2026-05-22 18:39:51 — Ukdeshan, 8 casts puis déplacement
        // post-cast, Gt bot ignoré pendant 13s jusqu'à intervention user).
        // Les GKK0 redondants sont ignorés par le serveur, donc safe.
        try
        {
            await session.EnvoyerAuServeurAsync("GKK0").ConfigureAwait(false);
            await Task.Delay(TimingsCombat.DelaiApresDeplacement(150, 300)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[{tag}] échec GKK0 fin-action : {ex.Message}");
        }

        await Task.Delay(TimingsCombat.DelaiPasserTour(DelaiAvantFinTourMs, DelaiAvantFinTourMs)).ConfigureAwait(false);
        await EnvoyerFinTourAsync(session);
    }

    // =============================================================
    // (1) PRÉ-MOUVEMENT selon Mode — copie adaptée master TrameJeu
    // =============================================================

    private static async Task PreMouvementSelonModeAsync(
        string tag, Combats.Combattants.Combattant moi, MembreHeros membre,
        Combat combat, Carte carte, ConfigCombat cfg,
        List<Combats.Combattants.Combattant> ennemisVivants, SessionProxy session)
    {
        if (moi.PM <= 0) return;
        int maCellId = moi.CellulePosition;
        var depart = carte.Obtenir(maCellId);
        if (depart == null) return;

        // Mode effectif : bascule en Fuyard si bas PV (FuirSiPvBas + SeuilFuitePv).
        var mode = cfg.ModeEffectif(moi.PV, moi.PVMax);
        int distPref = cfg.DistancePreferee;
        int distMinEloigne = cfg.DistanceMinEloigne;
        int mw = carte.Largeur > 0 ? carte.Largeur : Carte.LargeurParDefaut;

        // Smart positioning multi-mobs : score = SOMME distances Chebyshev
        // vers TOUS les ennemis vivants (cf. ScorePositionCombat).
        var ennemisXY = ScorePositionCombat.CoordsEnnemis(ennemisVivants, mw);

        // === MOTEUR TACTIQUE AVANCÉ — identifie sort principal + cible + LOS ===
        // Sort principal = 1re règle de la rotation (priorité décroissante)
        // qui a un sort appris et offensif. Utilisé pour calculer la distance
        // d'arrêt idéale (kite intelligent : porteeMax + PM_ennemi en Eloigne).
        var (sortPrincipal, ciblePrincipale) = IdentifierSortEtCibleAsync(membre, combat, cfg, ennemisVivants, mw);

        if (sortPrincipal == null || ciblePrincipale == null)
        {
            // Pas de sort offensif identifiable → fallback comportement legacy
            // (heuristique distance min + somme dist multi-mobs).
            await PreMouvementLegacyAsync(tag, moi, membre, combat, carte, cfg, ennemisVivants, session).ConfigureAwait(false);
            return;
        }

        var statsSort = sortPrincipal.Sort.Stats(sortPrincipal.NiveauAppris);
        int porteeMinSort = statsSort?.PorteeMin ?? 0;
        int porteeMaxSort = statsSort?.PorteeMax ?? 6;
        bool sortLOS = statsSort?.NecessiteLOS ?? false;

        var ctx = new ScorePositionCombat.ContexteTactique(
            Mode: mode,
            PorteeMinSort: porteeMinSort,
            PorteeMaxSort: porteeMaxSort,
            SortNecessiteLOS: sortLOS,
            PmEnnemiCible: ciblePrincipale.PM,
            DistancePreferee: distPref,
            DistanceMinEloigne: distMinEloigne);

        int distIdeale = ScorePositionCombat.DistanceIdeale(ctx);
        var (xMoi, yMoi) = Cellule.CalculerCoordonnees(maCellId, mw);
        var (xCible, yCible) = Cellule.CalculerCoordonnees(ciblePrincipale.CellulePosition, mw);
        int distActuelle = System.Math.Max(System.Math.Abs(xMoi - xCible), System.Math.Abs(yMoi - yCible));

        Journaliseur.Info(
            $"[{tag}] TACTIC Mode={mode}, sort=#{sortPrincipal.Sort.Identifiant} portée [{porteeMinSort}-{porteeMaxSort}] LOS={sortLOS} "
            + $"| cible #{ciblePrincipale.Identifiant} cell {ciblePrincipale.CellulePosition} dist={distActuelle} pmEnnemi={ciblePrincipale.PM} "
            + $"→ distIdéale={distIdeale}");

        // Skip si déjà à la distance idéale (et LOS OK si requise).
        bool dejaIdeal = distActuelle == distIdeale
            && (!sortLOS || TesterLos(carte, depart, carte.Obtenir(ciblePrincipale.CellulePosition), combat, moi.Identifiant));
        if (dejaIdeal)
        {
            Journaliseur.Info($"[{tag}] TACTIC déjà à distance idéale {distIdeale}, skip pré-move.");
            return;
        }

        int pmMax = moi.PM;
        var interdites = ConstruireInterdites(carte, combat, moi.Identifiant);
        var interdites_int = new HashSet<int>(System.Linq.Enumerable.Select(interdites, c => c.Identifiant));

        // LOS delegate qui réutilise la carte + occupations.
        MoteurTactique.TestLosDelegate testLos = (depuis, vers) =>
            !LigneVisuelle.EstObstruee(carte, depuis, vers, interdites_int);

        var resultat = MoteurTactique.CalculerMeilleureCellule(
            carte, depart, pmMax, interdites,
            ennemisXY, (xCible, yCible),
            ctx, testLos,
            exigeAmelioration: true);

        if (resultat == null)
        {
            Journaliseur.Info($"[{tag}] TACTIC aucune amélioration possible (distActuelle={distActuelle}, distIdéale={distIdeale}, PM={pmMax})");
            return;
        }

        Journaliseur.Info(
            $"[{tag}] TACTIC PRE-MOVE Mode={mode} | cell {maCellId} → {resultat.Cible.Identifiant} "
            + $"| {resultat.PmConsommes} pas | dist {distActuelle}→{resultat.DistanceFinaleCible} (idéale={distIdeale}) "
            + $"| LOS={resultat.LosCibleFinale} | Σdist={resultat.SommeDistEnnemis} | score={resultat.Score:F1}");

        var meilleurChemin = new List<Cellule>(resultat.Chemin);
        var meilleureCible = resultat.Cible;
        int meilleurNbPasTie = resultat.PmConsommes;
        int meilleureDistAfter = resultat.DistanceFinaleCible;

        int cellReelle = await DeplacerAsync(tag, combat, moi.Identifiant, session, meilleurChemin)
            .ConfigureAwait(false);
        if (cellReelle > 0)
        {
            moi.CellulePosition = cellReelle;
            membre.Cellule = cellReelle;
            // Décrément PM optimiste (le GTS suivant resync).
            moi.PM = System.Math.Max(0, moi.PM - meilleurNbPasTie);
        }
    }

    // =============================================================
    // (1bis) HELPERS pour MoteurTactique
    // =============================================================

    /// <summary>
    /// Identifie le sort principal + cible prioritaire selon la 1re règle de
    /// la rotation qui matche (priorité décroissante). Sert au moteur tactique
    /// pour calculer la distance d'arrêt idéale.
    ///
    /// Retourne <c>(null, null)</c> si aucune règle ne donne un sort offensif
    /// avec un ennemi vivant cible → le moteur tactique tombe en fallback.
    /// </summary>
    private static (MoteurReglesCombat.ResultatRegle?, Combats.Combattants.Combattant?) IdentifierSortEtCibleAsync(
        MembreHeros membre, Combat combat, ConfigCombat cfg,
        List<Combats.Combattants.Combattant> ennemisVivants, int mapWidth)
    {
        // On utilise le moteur règles tel quel — il choisit la 1re règle valide
        // selon focus + conditions configurées par l'user.
        var moi = combat.Allies.FirstOrDefault(a => a.Identifiant == membre.IdJeu);
        if (moi == null) return (null, null);

        // Pour le pré-mouvement, on veut le sort EN PRIORITÉ, peu importe la
        // portée actuelle (puisqu'on va se déplacer pour atteindre la portée).
        // Donc on évalue sans tenir compte de la position actuelle — on cherche
        // le sort que le perso CAST quand il est en portée.
        // Heuristique : on prend la 1re règle de la rotation avec :
        //   - sort connu de BaseSorts
        //   - sort appris (niveau > 0) par le membre
        //   - PorteeMax > 0 (sort offensif distance ou CAC)
        //   - cible existe selon Focus
        foreach (var regle in cfg.Regles.OrderByDescending(r => r.Priorite))
        {
            if (regle.IdSort <= 0) continue;
            if (!membre.SortsAppris.TryGetValue(regle.IdSort, out var niveau) || niveau <= 0) continue;
            var sort = Divers.Jeu.Personnage.Spells.BaseSorts.Instance.Trouver(regle.IdSort);
            if (sort == null) continue;
            var stats = sort.Stats(niveau);
            int porteeMax = stats?.PorteeMax ?? sort.PorteeMax;
            if (porteeMax <= 0) continue;
            int porteeMin = stats?.PorteeMin ?? sort.PorteeMin;
            int coutPA = stats?.CoutPA ?? sort.CoutPA;

            // Cible selon Focus — on choisit dans les ennemis vivants pour les
            // règles offensives. Pour les règles soin/buff, on saute (pas un
            // candidat pour pré-move offensif).
            Combats.Combattants.Combattant? cible = regle.Focus switch
            {
                FocusSort.EnnemiLePlusProche => ennemisVivants
                    .OrderBy(e => DistanceChebyshev(moi.CellulePosition, e.CellulePosition, mapWidth))
                    .FirstOrDefault(),
                FocusSort.EnnemiLePlusFaible => ennemisVivants
                    .Where(e => !e.EstInvocation).DefaultIfEmpty(ennemisVivants.FirstOrDefault())
                    .OrderBy(e => e?.PV ?? int.MaxValue).FirstOrDefault(),
                FocusSort.EnnemiLePlusFort => ennemisVivants
                    .Where(e => !e.EstInvocation).DefaultIfEmpty(ennemisVivants.FirstOrDefault())
                    .OrderByDescending(e => e?.PV ?? -1).FirstOrDefault(),
                FocusSort.EnnemiLePlusLoin => ennemisVivants
                    .OrderByDescending(e => DistanceChebyshev(moi.CellulePosition, e.CellulePosition, mapWidth))
                    .FirstOrDefault(),
                _ => null,  // sorts non-offensifs (Moi, Allié, Cellule…) — skip pour pré-move
            };
            if (cible == null) continue;

            // Distance actuelle (pour info, pas pour filtrage — on bouge pour atteindre).
            int dist = DistanceChebyshev(moi.CellulePosition, cible.CellulePosition, mapWidth);
            var resultat = new MoteurReglesCombat.ResultatRegle(
                regle, sort, cible, dist, coutPA, porteeMin, porteeMax, niveau);
            return (resultat, cible);
        }
        return (null, null);
    }

    /// <summary>
    /// Test LOS depuis <paramref name="depuis"/> vers <paramref name="vers"/>
    /// en considérant les combattants vivants (sauf moi et la cible) comme
    /// obstacles. Retourne <c>true</c> si LOS dégagée.
    /// </summary>
    private static bool TesterLos(Carte carte, Cellule? depuis, Cellule? vers, Combat combat, int idMoi)
    {
        if (depuis == null || vers == null) return true;
        var occupees = new HashSet<int>();
        foreach (var a in combat.Allies)
            if (!a.EstMort && a.CellulePosition > 0 && a.Identifiant != idMoi) occupees.Add(a.CellulePosition);
        foreach (var e in combat.Ennemis)
            if (!e.EstMort && e.CellulePosition > 0 && e.CellulePosition != vers.Identifiant)
                occupees.Add(e.CellulePosition);
        return !LigneVisuelle.EstObstruee(carte, depuis, vers, occupees);
    }

    /// <summary>
    /// Pré-mouvement legacy — utilisé en fallback quand aucun sort principal
    /// n'est identifiable (config combat sans sorts offensifs, perso sans
    /// sort appris, etc.). Logique multi-mobs simple (Σ distance ennemis).
    /// </summary>
    private static async Task PreMouvementLegacyAsync(
        string tag, Combats.Combattants.Combattant moi, MembreHeros membre,
        Combat combat, Carte carte, ConfigCombat cfg,
        List<Combats.Combattants.Combattant> ennemisVivants, SessionProxy session)
    {
        int maCellId = moi.CellulePosition;
        var depart = carte.Obtenir(maCellId);
        if (depart == null) return;
        var mode = cfg.ModeEffectif(moi.PV, moi.PVMax);
        int mw = carte.Largeur > 0 ? carte.Largeur : Carte.LargeurParDefaut;
        var ennemisXY = ScorePositionCombat.CoordsEnnemis(ennemisVivants, mw);
        var (xMoi, yMoi) = Cellule.CalculerCoordonnees(maCellId, mw);
        int distActuelle = ScorePositionCombat.DistanceMin(xMoi, yMoi, ennemisXY);
        double scoreActuel = ScorePositionCombat.ScoreCellule(xMoi, yMoi, ennemisXY, mode, cfg.DistancePreferee, cfg.DistanceMinEloigne);

        switch (mode)
        {
            case ModeCombat.Agressif when distActuelle <= 1: return;
            case ModeCombat.Equilibre when System.Math.Abs(distActuelle - cfg.DistancePreferee) <= 1: return;
        }

        int pmMax = moi.PM;
        var interdites = ConstruireInterdites(carte, combat, moi.Identifiant);

        Cellule? meilleureCible = null;
        List<Cellule>? meilleurChemin = null;
        double meilleurScore = scoreActuel;
        int meilleurNbPasTie = int.MaxValue;

        foreach (var c in carte.Cellules)
        {
            if (c == null || !c.EstMarchable || c.IdInteractif >= 0 || interdites.Contains(c)) continue;
            int dEst = System.Math.Abs(c.X - depart.X) + System.Math.Abs(c.Y - depart.Y);
            if (dEst == 0 || dEst > pmMax) continue;
            double score = ScorePositionCombat.ScoreCellule(c.X, c.Y, ennemisXY, mode, cfg.DistancePreferee, cfg.DistanceMinEloigne);
            var chemin = Pathfinder.Trouver(carte, depart, c, interdites, combat: true);
            if (chemin == null) continue;
            int nbPas = chemin.Count - 1;
            if (nbPas <= 0 || nbPas > pmMax) continue;
            if (score < meilleurScore || (score == meilleurScore && nbPas < meilleurNbPasTie))
            {
                meilleurScore = score;
                meilleurNbPasTie = nbPas;
                meilleureCible = c;
                meilleurChemin = chemin;
            }
        }
        if (meilleureCible == null || meilleurChemin == null || meilleurScore >= scoreActuel) return;
        Journaliseur.Info($"[{tag}] TACTIC fallback legacy : cell {maCellId}→{meilleureCible.Identifiant} ({meilleurNbPasTie} pas)");
        int cellReelle = await DeplacerAsync(tag, combat, moi.Identifiant, session, meilleurChemin).ConfigureAwait(false);
        if (cellReelle > 0)
        {
            moi.CellulePosition = cellReelle;
            membre.Cellule = cellReelle;
            moi.PM = System.Math.Max(0, moi.PM - meilleurNbPasTie);
        }
    }

    // =============================================================
    // (2) ENVOI CAST SynFus (analogue TrameJeu.EnvoyerCastAsync)
    // =============================================================

    private static async Task<bool> EnvoyerCastSynFusAsync(
        string tag, Combats.Combattants.Combattant moi, Combat combat, Carte carte,
        MoteurReglesCombat.ResultatRegle r, SessionProxy session)
    {
        Journaliseur.Info(
            $"[{tag}] DECIDEUR : « {r.Sort.Nom} » (#{r.Sort.Identifiant} niv{r.NiveauAppris}) "
            + $"focus={r.Regle.Focus}, cible « {r.Cible.Nom} » cell {r.Cible.CellulePosition} "
            + $"(dist={r.Distance}, {r.CoutPA} PA, portée {r.PorteeMin}-{r.PorteeMax})");

        int mw = carte.Largeur > 0 ? carte.Largeur : Carte.LargeurParDefaut;

        // GARDE-FOU ANTI-BAN : recalcule distance réelle avec position actuelle.
        int distReelle = DistanceChebyshev(moi.CellulePosition, r.Cible.CellulePosition, mw);
        if (distReelle < r.PorteeMin || (r.PorteeMax > 0 && distReelle > r.PorteeMax))
        {
            Journaliseur.Avertir(
                $"[{tag}] ANTI-BAN refuse « {r.Sort.Nom} » : dist réelle {distReelle} "
                + $"hors portée [{r.PorteeMin}-{r.PorteeMax}] (ma cell {moi.CellulePosition}, "
                + $"cible {r.Cible.CellulePosition}).");
            // Bloque cette règle pour le reste du tour (clé cloisonnée par caster).
            var cleAR = (moi.Identifiant, r.Sort.Identifiant);
            combat.CompteursRegleParTour[cleAR] =
                (combat.CompteursRegleParTour.TryGetValue(cleAR, out var cnt) ? cnt : 0)
                + System.Math.Max(1, r.Regle.NombreParTour);
            return false;
        }

        // LOS check si requis.
        var statsR = r.Sort.Stats(r.NiveauAppris);
        bool besoinLOS = statsR?.NecessiteLOS ?? false;
        if (besoinLOS && distReelle > 1)
        {
            var celluleMoi = carte.Obtenir(moi.CellulePosition);
            var celluleCible = carte.Obtenir(r.Cible.CellulePosition);
            if (celluleMoi != null && celluleCible != null)
            {
                var occupees = new HashSet<int>(
                    combat.Allies.Where(a => !a.EstMort).Select(a => a.CellulePosition)
                        .Concat(combat.Ennemis.Where(e => !e.EstMort).Select(e => e.CellulePosition)));
                if (LigneVisuelle.EstObstruee(carte, celluleMoi, celluleCible, occupees))
                {
                    Journaliseur.Avertir(
                        $"[{tag}] ANTI-BAN refuse « {r.Sort.Nom} » : LOS obstruée "
                        + $"entre cell {moi.CellulePosition} et {r.Cible.CellulePosition}.");
                    var cleALos = (moi.Identifiant, r.Sort.Identifiant);
                    combat.CompteursRegleParTour[cleALos] =
                        (combat.CompteursRegleParTour.TryGetValue(cleALos, out var cntLos) ? cntLos : 0)
                        + System.Math.Max(1, r.Regle.NombreParTour);
                    return false;
                }
            }
        }

        // Tracking invocations attendues (pour l'héritage GTM ennemi→allié).
        if (r.Regle.Focus == FocusSort.CelluleVide
            || r.Regle.Focus == FocusSort.CelluleAdjacenteEnnemi)
        {
            combat.CellsInvocationsAttendues.Add(r.Cible.CellulePosition);
        }

        Journaliseur.Info(
            $"[{tag}] CAST « {r.Sort.Nom} » niv{r.NiveauAppris} sur cell {r.Cible.CellulePosition} "
            + $"({r.CoutPA} PA, portée {r.PorteeMin}-{r.PorteeMax}, dist={distReelle})");
        combat.DeclencherCast(r.Sort.Identifiant, r.Sort.Nom, r.Cible.CellulePosition);

        try
        {
            await session.EnvoyerAuServeurAsync($"GA300{r.Sort.Identifiant};{r.Cible.CellulePosition}")
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[{tag}] échec cast #{r.Sort.Identifiant} : {ex.Message}");
            return false;
        }

        // Compteurs (pareil que TrameJeu.EnvoyerCastAsync) — cloisonnés par caster.
        var cleT = (moi.Identifiant, r.Sort.Identifiant);
        combat.CompteursRegleParTour[cleT] =
            (combat.CompteursRegleParTour.TryGetValue(cleT, out var ct) ? ct : 0) + 1;
        var cleC = (moi.Identifiant, r.Sort.Identifiant, r.Cible.Identifiant);
        combat.CompteursRegleParCible[cleC] =
            (combat.CompteursRegleParCible.TryGetValue(cleC, out var ctc) ? ctc : 0) + 1;
        combat.DernierTourLanceParSort[r.Sort.Identifiant] = combat.NumeroTour;

        // Décrément PA optimiste (la prochaine itération du moteur voit le bon budget).
        if (moi.PA >= r.CoutPA) moi.PA -= r.CoutPA;

        await Task.Delay(TimingsCombat.DelaiLancerSort(300, 500)).ConfigureAwait(false);
        try { await session.EnvoyerAuServeurAsync("GKK0").ConfigureAwait(false); } catch { /* ack best-effort */ }
        await Task.Delay(TimingsCombat.DelaiEntreDeuxSorts(500, 900)).ConfigureAwait(false);
        return true;
    }

    // =============================================================
    // (3) FALLBACK : déplacement + cast (sort hors portée + PM dispos)
    // =============================================================

    private static async Task<bool> TenterDeplacementPuisCastAsync(
        string tag, Combats.Combattants.Combattant moi, MembreHeros membre,
        Combat combat, Carte carte, ConfigCombat cfg,
        List<Combats.Combattants.Combattant> ennemisVivants, SessionProxy session)
    {
        var depart = carte.Obtenir(moi.CellulePosition);
        if (depart == null) return false;
        int mw = carte.Largeur > 0 ? carte.Largeur : Carte.LargeurParDefaut;

        // Choisir la cible : ennemi le plus proche par défaut.
        var cible = ennemisVivants
            .OrderBy(e => DistanceChebyshev(moi.CellulePosition, e.CellulePosition, mw))
            .First();
        var cibleCell = carte.Obtenir(cible.CellulePosition);
        if (cibleCell == null) return false;

        // Itère les règles par priorité décroissante, garde la 1re qui matche
        // (sort appris + PA OK) — la portée est gérée par TrouverCellulesCast.
        foreach (var regle in cfg.Regles.OrderByDescending(r => r.Priorite))
        {
            if (regle.IdSort <= 0) continue;
            if (!membre.SortsAppris.TryGetValue(regle.IdSort, out var niveau) || niveau <= 0) continue;
            var sort = BaseSorts.Instance.Trouver(regle.IdSort);
            if (sort == null) continue;
            var stats = sort.Stats(niveau);
            if (stats == null || stats.CoutPA <= 0 || stats.CoutPA > moi.PA) continue;

            // NombreParTour check (cloisonné par caster).
            var cleFb = (moi.Identifiant, regle.IdSort);
            if (regle.NombreParTour > 0
                && combat.CompteursRegleParTour.TryGetValue(cleFb, out var dejaT)
                && dejaT >= regle.NombreParTour)
                continue;

            var interdits = ConstruireInterdites(carte, combat, moi.Identifiant);
            // Cherche cell de cast atteignable (dist ∈ [portéeMin, portéeMax],
            // LOS OK si requis, PM atteignable).
            var candidats = TrouverCellulesCast(
                carte, cibleCell, stats, interdits, depart, moi.PM, mw);
            if (candidats.Count == 0) continue;

            // Trie selon ModeCombat (Agressif : minDist, Eloigne/Fuyard : maxDist, Equilibre : DistPref).
            var ordre = TrierCellsSelonMode(candidats, depart, cibleCell, cfg, stats, mw);
            List<Cellule>? cheminChoisi = null;
            Cellule? cellArrivee = null;
            int pmRequis = 0;
            foreach (var cand in ordre)
            {
                var chemin = Pathfinder.Trouver(carte, depart, cand, interdits, combat: true);
                if (chemin == null || chemin.Count < 2) continue;
                int pmCh = chemin.Count - 1;
                if (pmCh > moi.PM) continue;
                cheminChoisi = chemin;
                cellArrivee = cand;
                pmRequis = pmCh;
                break;
            }
            if (cheminChoisi == null || cellArrivee == null) continue;

            Journaliseur.Info(
                $"[{tag}] FALLBACK : déplacement cell {moi.CellulePosition}→{cellArrivee.Identifiant} "
                + $"({pmRequis} PM) puis cast « {sort.Nom } » niv{niveau} sur cell {cibleCell.Identifiant}");

            int cellReelleFb = await DeplacerAsync(tag, combat, moi.Identifiant, session, cheminChoisi)
                .ConfigureAwait(false);
            if (cellReelleFb < 0) return false;
            moi.CellulePosition = cellReelleFb;
            membre.Cellule = cellReelleFb;
            moi.PM = System.Math.Max(0, moi.PM - pmRequis);

            // Construire le ResultatRegle équivalent et l'exécuter via EnvoyerCastSynFusAsync.
            // (On contourne MoteurReglesCombat.Evaluer car il filtrerait sur la cell de départ
            // qui n'est plus la même — on a déjà fait le travail de vérif portée/LOS).
            var resultat = new MoteurReglesCombat.ResultatRegle(
                regle, sort, cible,
                Distance: DistanceChebyshev(cellArrivee.Identifiant, cible.CellulePosition, mw),
                CoutPA: stats.CoutPA,
                PorteeMin: stats.PorteeMin,
                PorteeMax: stats.PorteeMax,
                NiveauAppris: niveau);

            await Task.Delay(TimingsCombat.Delai(300, 500)).ConfigureAwait(false);
            return await EnvoyerCastSynFusAsync(tag, moi, combat, carte, resultat, session).ConfigureAwait(false);
        }
        return false;
    }

    // =============================================================
    // (4) GET_FIN_TURNO — repositionnement fin de tour (dyshay)
    // =============================================================

    /// <summary>
    /// Repositionnement style dyshay <c>get_Fin_Turno</c> :
    /// <list type="bullet">
    ///   <item>Agressif sans CAC → avance (minimise sum dist tous ennemis).</item>
    ///   <item>Fuyard en CAC OU ennemi proche &lt; 8 → recule (max sum dist).</item>
    ///   <item>Fuyard ennemi loin &gt; 12 → avance (reste en portée).</item>
    ///   <item>Equilibre / Tactique → rien (préserve PM).</item>
    /// </list>
    /// Appelée APRÈS la boucle multi-cast pour préparer le tour suivant.
    /// </summary>
    private static async Task RepositionnerFinTourAsync(
        string tag, Combats.Combattants.Combattant moi, MembreHeros membre,
        Combat combat, Carte carte, ConfigCombat cfg, SessionProxy session)
    {
        if (moi.PM <= 0) return;
        var ennemisVivants = combat.Ennemis.Where(e => !e.EstMort && e.PV > 0).ToList();
        if (ennemisVivants.Count == 0) return;
        int mw = carte.Largeur > 0 ? carte.Largeur : Carte.LargeurParDefaut;

        int distMin = ennemisVivants.Min(e => DistanceChebyshev(moi.CellulePosition, e.CellulePosition, mw));

        bool avancer = false, reculer = false;
        switch (cfg.Mode)
        {
            case ModeCombat.Agressif when distMin > 1:
                avancer = true;
                break;
            case ModeCombat.Fuyard when distMin <= 1:
            case ModeCombat.Fuyard when distMin < 8:
                reculer = true;
                break;
            case ModeCombat.Fuyard when distMin > 12:
                avancer = true;
                break;
            case ModeCombat.Eloigne when distMin < cfg.DistanceMinEloigne:
                reculer = true;
                break;
        }
        if (!avancer && !reculer) return;

        Journaliseur.Info(
            $"[{tag}] FIN-TOUR Mode={cfg.Mode}, distMin={distMin} → {(avancer ? "AVANCE" : "RECULE")}");

        await DeplacerVersAsync(tag, moi, membre, combat, carte, ennemisVivants, !reculer, session)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Déplacement multi-ennemis : énumère cells atteignables, score =
    /// somme distances Chebyshev à TOUS les ennemis (style dyshay
    /// <c>Get_Total_Distancia_Enemigo</c>). <c>cercano=true</c> minimise (avance),
    /// <c>cercano=false</c> maximise (recule).
    /// </summary>
    private static async Task DeplacerVersAsync(
        string tag, Combats.Combattants.Combattant moi, MembreHeros membre,
        Combat combat, Carte carte, List<Combats.Combattants.Combattant> ennemisVivants,
        bool cercano, SessionProxy session)
    {
        var depart = carte.Obtenir(moi.CellulePosition);
        if (depart == null) return;
        int mw = carte.Largeur > 0 ? carte.Largeur : Carte.LargeurParDefaut;
        int pmMax = moi.PM;

        var interdites = ConstruireInterdites(carte, combat, moi.Identifiant);

        // Coords ennemis (pré-calculées).
        var ennemisXY = ennemisVivants
            .Select(e => Cellule.CalculerCoordonnees(e.CellulePosition, mw))
            .ToArray();

        int distDepart = SommeDistancesEnnemis(depart.X, depart.Y, ennemisXY);

        Cellule? meilleure = null;
        List<Cellule>? meilleurChemin = null;
        int meilleurScore = distDepart;
        int meilleurPmConsomme = cercano ? int.MaxValue : -1;

        foreach (var c in carte.Cellules)
        {
            if (c == null || !c.EstMarchable || c.IdInteractif >= 0 || interdites.Contains(c)) continue;
            int dEst = System.Math.Abs(c.X - depart.X) + System.Math.Abs(c.Y - depart.Y);
            if (dEst == 0 || dEst > pmMax) continue;

            int score = SommeDistancesEnnemis(c.X, c.Y, ennemisXY);
            bool ameliore = cercano ? score <= meilleurScore : score >= meilleurScore;
            if (!ameliore) continue;

            var chemin = Pathfinder.Trouver(carte, depart, c, interdites, combat: true);
            if (chemin == null) continue;
            int nbPas = chemin.Count - 1;
            if (nbPas <= 0 || nbPas > pmMax) continue;

            // Tie-break : pour reculer, MAXIMISE pmConsommé (= s'éloigne au max).
            // Pour avancer, minimise pmConsommé (économise PM).
            bool premier = meilleure == null;
            bool meilleurPm = cercano ? nbPas < meilleurPmConsomme : nbPas > meilleurPmConsomme;
            if (premier || score != meilleurScore || meilleurPm)
            {
                meilleure = c;
                meilleurChemin = chemin;
                meilleurScore = score;
                meilleurPmConsomme = nbPas;
            }
        }

        if (meilleure == null || meilleurChemin == null) return;
        if (meilleureCellEqualsDepart(meilleure, depart)) return;
        // Skip si pas d'amélioration réelle.
        if (cercano && meilleurScore >= distDepart) return;
        if (!cercano && meilleurScore <= distDepart) return;

        Journaliseur.Info(
            $"[{tag}] FIN-TOUR move cell {moi.CellulePosition}→{meilleure.Identifiant} "
            + $"({meilleurPmConsomme} PM, ΣdistEnnemis {distDepart}→{meilleurScore})");

        int cellReelleFt = await DeplacerAsync(tag, combat, moi.Identifiant, session, meilleurChemin)
            .ConfigureAwait(false);
        if (cellReelleFt > 0)
        {
            moi.CellulePosition = cellReelleFt;
            membre.Cellule = cellReelleFt;
            moi.PM = System.Math.Max(0, moi.PM - meilleurPmConsomme);
        }
    }

    private static bool meilleureCellEqualsDepart(Cellule a, Cellule b)
        => a.Identifiant == b.Identifiant;

    private static int SommeDistancesEnnemis(int x, int y, (int x, int y)[] ennemisXY)
    {
        int total = 0;
        foreach (var (ex, ey) in ennemisXY)
            total += System.Math.Max(System.Math.Abs(x - ex), System.Math.Abs(y - ey));
        return total;
    }

    // =============================================================
    // Helpers cells / pathfinder / réseau
    // =============================================================

    private static List<Cellule> TrouverCellulesCast(
        Carte carte, Cellule cible, StatsNiveau stats, ICollection<Cellule> interdites,
        Cellule depart, int pmDispo, int mw)
    {
        var cellsValides = new List<Cellule>(64);
        int rmin = System.Math.Max(0, stats.PorteeMin);
        int rmax = System.Math.Max(rmin, stats.PorteeMax);
        if (rmax <= 0) return cellsValides;

        var (xC, yC) = (cible.X, cible.Y);
        var occupeesInts = new HashSet<int>(interdites.Select(c => c.Identifiant));

        for (int dx = -rmax; dx <= rmax; dx++)
        {
            for (int dy = -rmax; dy <= rmax; dy++)
            {
                int cheby = System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy));
                if (cheby < rmin || cheby > rmax) continue;
                var cell = carte.ObtenirParCoords(xC + dx, yC + dy);
                if (cell == null) continue;
                if (cell.Identifiant == cible.Identifiant) continue;
                if (occupeesInts.Contains(cell.Identifiant)) continue;
                if (!cell.EstMarchable && cell.Identifiant != depart.Identifiant) continue;
                // Pré-filtre PM (estim Manhattan).
                int dEstim = System.Math.Abs(cell.X - depart.X) + System.Math.Abs(cell.Y - depart.Y);
                if (dEstim > pmDispo + 1) continue;
                // LOS check.
                if (stats.NecessiteLOS && LigneVisuelle.EstObstruee(carte, cell, cible, occupeesInts)) continue;
                cellsValides.Add(cell);
            }
        }
        return cellsValides;
    }

    private static IEnumerable<Cellule> TrierCellsSelonMode(
        List<Cellule> candidats, Cellule depart, Cellule cible, ConfigCombat cfg, StatsNiveau stats, int mw)
    {
        int distancePref = System.Math.Clamp(cfg.DistancePreferee, stats.PorteeMin, stats.PorteeMax);
        return cfg.Mode switch
        {
            ModeCombat.Agressif => candidats
                .OrderBy(c => System.Math.Max(System.Math.Abs(c.X - cible.X), System.Math.Abs(c.Y - cible.Y)))
                .ThenBy(c => System.Math.Abs(c.X - depart.X) + System.Math.Abs(c.Y - depart.Y)),
            ModeCombat.Eloigne or ModeCombat.Fuyard => candidats
                .OrderByDescending(c => System.Math.Max(System.Math.Abs(c.X - cible.X), System.Math.Abs(c.Y - cible.Y)))
                .ThenBy(c => System.Math.Abs(c.X - depart.X) + System.Math.Abs(c.Y - depart.Y)),
            _ => candidats
                .OrderBy(c => System.Math.Abs(System.Math.Max(System.Math.Abs(c.X - cible.X), System.Math.Abs(c.Y - cible.Y)) - distancePref))
                .ThenBy(c => System.Math.Abs(c.X - depart.X) + System.Math.Abs(c.Y - depart.Y)),
        };
    }

    private static HashSet<Cellule> ConstruireInterdites(Carte carte, Combat combat, int idMoi)
    {
        var interdites = new HashSet<Cellule>();
        foreach (var c in combat.Allies.Concat(combat.Ennemis))
        {
            if (c.EstMort || c.CellulePosition <= 0) continue;
            if (c.Identifiant == idMoi) continue;
            var cell = carte.Obtenir(c.CellulePosition);
            if (cell != null) interdites.Add(cell);
        }
        return interdites;
    }

    private static int DistanceChebyshev(int idA, int idB, int mw)
    {
        var (xA, yA) = Cellule.CalculerCoordonnees(idA, mw);
        var (xB, yB) = Cellule.CalculerCoordonnees(idB, mw);
        return System.Math.Max(System.Math.Abs(xA - xB), System.Math.Abs(yA - yB));
    }

    /// <summary>
    /// Envoie <c>GA001</c> puis attend la confirmation serveur via
    /// <see cref="PipelineDeplacementCombat.AttendreMouvementOuTimeoutAsync"/>
    /// (broadcast <c>GA;0/1;&lt;idMoi&gt;</c>). Bien plus rapide et fiable que
    /// le <c>Task.Delay</c> aveugle car on poursuit dès que le serveur a
    /// validé le déplacement (souvent ~100-200 ms après l'envoi en LAN).
    /// </summary>
    /// <returns>
    /// La cellule d'arrivée réelle confirmée par le serveur (cf. cas
    /// <see cref="ResultatDeplacementCombat.ConfirmePartiel"/> = troncature
    /// chemin) ; ou <c>-1</c> si timeout / erreur réseau.
    /// </returns>
    private static async Task<int> DeplacerAsync(
        string tag, Combat combat, int idMoi, SessionProxy session,
        IReadOnlyList<Cellule> chemin)
    {
        try
        {
            var encodage = Pathfinder.EncoderChemin(chemin);
            if (string.IsNullOrEmpty(encodage)) return -1;
            int cellAttendue = chemin[chemin.Count - 1].Identifiant;
            int nbPas = chemin.Count - 1;

            // Timeout généreux : 1.5s + 500ms par case (couvre lag réseau).
            // En turbo on garde la même borne haute car c'est juste un timeout
            // de sécurité, pas un délai actif (l'event broadcast peut arriver
            // beaucoup plus tôt).
            int timeoutMs = System.Math.Max(1500, nbPas * 500 + 1500);

            await session.EnvoyerAuServeurAsync($"GA001{encodage}").ConfigureAwait(false);

            var resultat = await PipelineDeplacementCombat
                .AttendreMouvementOuTimeoutAsync(combat, idMoi, cellAttendue, timeoutMs, default)
                .ConfigureAwait(false);

            switch (resultat)
            {
                case ResultatDeplacementCombat.Confirme:
                    return cellAttendue;
                case ResultatDeplacementCombat.ConfirmePartiel:
                    // Combat.Allies a déjà été mis à jour par OnActionJeu →
                    // récupère la cell réelle depuis le Combattant.
                    var moiC = combat.Allies.FirstOrDefault(a => a.Identifiant == idMoi);
                    int cellAtteinte = moiC?.CellulePosition ?? cellAttendue;
                    Journaliseur.Avertir(
                        $"[{tag}] déplacement TRONQUÉ par le serveur : visé {cellAttendue}, atteint {cellAtteinte}");
                    return cellAtteinte;
                case ResultatDeplacementCombat.TimeoutSilencieux:
                default:
                    // Fallback : on suppose que le déplacement a marché (mode
                    // optimistic legacy) — c'est cohérent avec le master quand
                    // ModeDeplacementOptimisteSecours=true. Le broadcast a peut-être
                    // été émis avant qu'on s'abonne (race), ou le serveur a
                    // simplement avalé le paquet sans broadcaster.
                    Journaliseur.Avertir(
                        $"[{tag}] timeout déplacement ({timeoutMs}ms) — fallback optimistic");
                    return cellAttendue;
            }
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[{tag}] échec déplacement : {ex.Message}");
            return -1;
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
}
