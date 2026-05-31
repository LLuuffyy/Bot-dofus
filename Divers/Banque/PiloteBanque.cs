using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BotDofus.Commun.Reseau;
using BotDofus.Divers.Donnees;
using BotDofus.Divers.Jeu.Personnage;
using BotDofus.Divers.Scripts.Api;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Banque;

/// <summary>
/// Pilote async du dépôt banque : zaap vers map banque → ouverture (<c>ApS</c>
/// CLAIR) → dépôt items (<c>EMO+&lt;uidInv&gt;|&lt;qte&gt;</c> chiffré '-')
/// → fermeture (<c>EV</c> chiffré '-') → zaap retour optionnel.
///
/// ✅ PROTOCOLE confirmé par capture user 2026-05-21 sur Hystoria :
/// <list type="bullet">
///   <item><c>ApS</c> ouvre le coffre interactif (pas de dialogue NPC, gratuit).</item>
///   <item><c>EMO+&lt;uid&gt;|&lt;qte&gt;</c> dépose (canal chiffré auto via DoitEtreChiffre).</item>
///   <item><c>EV</c> ferme (canal chiffré).</item>
///   <item>Attendre <c>OR&lt;persoId&gt;|&lt;uid&gt;</c> entre dépôts pour confirmation.</item>
/// </list>
/// </summary>
public sealed class PiloteBanque
{
    private readonly ApiBot _api;
    private readonly SessionProxy _session;
    private readonly Personnage _perso;
    private readonly ConfigBanque _cfg;

    /// <summary>Compteur monotone d'<c>OR</c> reçus (Object Remove de l'inventaire).
    /// Incrémenté par <see cref="TrameJeu"/> à chaque OR observé. Utilisé pour
    /// attendre la confirmation serveur entre 2 dépôts.</summary>
    public static int CompteurObjectRemove;

    /// <summary>UIDs envoyés en <c>EMO+</c> dont on attend l'OR du serveur.
    /// Clé = UID inventaire, valeur = timestamp UTC d'envoi. Lu par
    /// <see cref="TrameJeu"/>.<c>OnObjetRetrait</c> qui retire l'UID dès que
    /// l'OR arrive. Le pilote log <c>[BANQUE-LOST]</c> pour les UIDs restants
    /// après timeout 10s (item considéré déposé serveur-side mais sans ACK).
    /// Cf. ADR-BANQUE (suppression locale optimiste dyshay-style).</summary>
    public static readonly System.Collections.Concurrent.ConcurrentDictionary<long, DateTime> EnvoyesEnAttenteOR = new();

    /// <summary>Flag set à true quand un paquet <c>EV</c> est observé (S→C ou C→S)
    /// — la banque est fermée. Le pilote en cours doit arrêter ses dépôts.
    /// Reset à false par le pilote au début du workflow.</summary>
    public static volatile bool BanqueFermeeObservee;

    /// <summary>Flag set à true quand un paquet <c>ECK</c> est observé S→C
    /// (Échange Créé — serveur confirme l'ouverture du coffre). Le pilote
    /// l'attend pendant 2s après l'ApS : si toujours false → bank n'est PAS
    /// ouverte (perso pas près d'un coffre, map sans banque mobile…) →
    /// abort, pas la peine d'envoyer EMO+ dans le vide.</summary>
    public static volatile bool BanqueOuvertureObservee;

    /// <summary>Flag set à true par le pilote au début du dépôt — indique au
    /// proxy MITM de DROP toute trame <c>EV</c> émise par le vrai client
    /// Dofus (qui n'a pas conscience que la banque est ouverte côté serveur
    /// via ApS injecté, et envoie EV spontanément après quelques secondes,
    /// fermant le coffre avant que les EMO+ soient finis — forensic
    /// 2026-05-24 03:26+). Reset à false JUSTE avant l'envoi de notre EV
    /// de clôture (sinon notre propre EV serait droppé aussi).</summary>
    public static volatile bool BloquerEvClient;

    public PiloteBanque(ApiBot api, SessionProxy session, Personnage perso, ConfigBanque cfg)
    {
        _api = api;
        _session = session;
        _perso = perso;
        _cfg = cfg;
    }

    /// <summary>
    /// Workflow COMPLET : zaap vers banque → dépôt → zaap retour si configuré.
    /// Retourne true si le pourcentage poids est passé sous CiblePoidsPct.
    /// </summary>
    public async Task<bool> WorkflowCompletAsync(int? carteFarmAvant, CancellationToken ct = default)
    {
        Journaliseur.Info($"[BANQUE-START] === Workflow complet démarré (poids {_perso.PourcentagePoids:F1}%) ===");

        // 0) RESYNC INVENTAIRE — DÉSACTIVÉ : forensic 2026-05-28 20:00
        // les tentatives de changement de map via ChangerMapDirectionAsync
        // échouent toutes (les bords « Transition » marqués par
        // MarquerBordsCommeTransitions ne sont pas de VRAIES transitions
        // serveur, le serveur refuse de changer la map). 20s perdues pour
        // rien. Re-activable seulement si on identifie une cellule de
        // transition GARANTIE (zaap, RoadCreator, etc.).
        // if (_cfg.OuvertureDirecte)
        //     await ResyncInventaireParChangementCarteAsync(ct).ConfigureAwait(false);

        // 1) Si on est DÉJÀ sur la map banque OU si OuvertureDirecte est activé,
        //    sauter l'étape zaap et envoyer ApS directement depuis la position.
        int? mapActuelle = _perso.CarteCourante;
        bool dejaSurMapBanque = mapActuelle.HasValue && mapActuelle.Value == _cfg.MapBanqueId;
        if (dejaSurMapBanque)
        {
            Journaliseur.Info($"[BANQUE] Étape 1/4 : déjà sur map banque {_cfg.MapBanqueId} — skip zaap");
        }
        else if (_cfg.OuvertureDirecte)
        {
            Journaliseur.Info(
                $"[BANQUE] Étape 1/4 : OuvertureDirecte=true — skip zaap, ApS envoyé depuis map {mapActuelle}. "
                + "Le perso DOIT être à proximité d'un coffre banque ou ApS sera ignoré par le serveur.");
        }
        else
        {
            Journaliseur.Info($"[BANQUE] Étape 1/4 : zaap vers map banque {_cfg.MapBanqueId} (depuis map {mapActuelle})");
            bool zaapOk = await _api.UtiliserZaapAsync(_cfg.MapBanqueId, ct).ConfigureAwait(false);
            if (!zaapOk)
            {
                Journaliseur.Avertir(
                    $"[BANQUE] Zaap vers {_cfg.MapBanqueId} échoué — abandon workflow. "
                    + "Cause probable : pas de cellule zaap (gfx 7000) sur la map actuelle, "
                    + "ou map cible non débloquée. Mets-toi sur un zaap connu ou directement "
                    + "sur la map banque avant de lancer 'Tester maintenant'.");
                return false;
            }
            await Task.Delay(System.Random.Shared.Next(1500, 2500), ct).ConfigureAwait(false);
        }

        // 2) Dépôt items (tentative 1).
        Journaliseur.Info("[BANQUE] Étape 2/4 : dépôt items");
        await DeposerToutAsync(ct).ConfigureAwait(false);

        // 2b) Un SEUL cycle supplémentaire pour absorber les OQ correctifs
        // tardifs (loots arrivés pendant le 1er dépôt). Avec la suppression
        // locale optimiste post-EMO+ (ADR-BANQUE), le 1er dépôt vide déjà
        // 95-100 % des items éligibles. Au-delà de 1 cycle supplémentaire,
        // on entrait dans la boucle stérile 9-passes-0-OR observée
        // (forensic AGENT 1 « Pattern récurrent »).
        // Snapshot sous lock(_perso.Inventaire) (race UI thread + handlers
        // TrameJeu).
        for (int cycleSupplementaire = 1; cycleSupplementaire <= 1; cycleSupplementaire++)
        {
            await Task.Delay(2000, ct).ConfigureAwait(false);
            List<ObjetInventaire> snapshotApres;
            lock (_perso.Inventaire)
            {
                snapshotApres = _perso.Inventaire.ToList();
            }
            var resteAdeposer = CalculerItemsADeposer(snapshotApres);
            if (resteAdeposer.Count == 0)
            {
                Journaliseur.Info(
                    $"[BANQUE-END] Aucun item éligible restant après {cycleSupplementaire - 1} cycle(s) supplémentaire(s) — fin du workflow.");
                break;
            }
            Journaliseur.Info(
                $"[BANQUE-START] Il reste {resteAdeposer.Count} items éligibles après 1er dépôt — "
                + $"cycle supplémentaire {cycleSupplementaire}/1 (anti-désync inventaire).");
            await DeposerToutAsync(ct).ConfigureAwait(false);
        }

        // 3) Retour vers la map de farm (zaap aller-retour — skip si banque mobile).
        if (_cfg.OuvertureDirecte)
        {
            Journaliseur.Info("[BANQUE] Étape 3/4 : OuvertureDirecte=true — pas de retour zaap, perso reste sur place");
        }
        else if (_cfg.RetourFarmApresDepot && carteFarmAvant.HasValue && carteFarmAvant.Value != _cfg.MapBanqueId)
        {
            Journaliseur.Info($"[BANQUE] Étape 3/4 : zaap retour vers map {carteFarmAvant.Value}");
            bool retourOk = await _api.UtiliserZaapAsync(carteFarmAvant.Value, ct).ConfigureAwait(false);
            if (!retourOk)
            {
                Journaliseur.Avertir($"[BANQUE] Zaap retour échoué — perso reste à la banque");
            }
            await Task.Delay(System.Random.Shared.Next(1500, 2500), ct).ConfigureAwait(false);
        }
        else
        {
            Journaliseur.Info("[BANQUE] Étape 3/4 : pas de retour configuré, perso reste à la banque");
        }

        Journaliseur.Info($"[BANQUE-END] === Workflow terminé (poids final {_perso.PourcentagePoids:F1}%) ===");
        return true;
    }

    /// <summary>
    /// Workflow ouvrir-déposer-fermer (sans zaap). Retourne true si le
    /// pourcentage poids est passé sous CiblePoidsPct.
    /// </summary>
    public async Task<bool> DeposerToutAsync(CancellationToken ct = default)
    {
        try
        {
            return await DeposerToutInterneAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            // GARANTIE : on libère TOUJOURS le filtre EV proxy en sortie,
            // même sur exception/cancellation — sinon le proxy continuerait
            // de bloquer les EV client après le workflow.
            BloquerEvClient = false;
        }
    }

    private async Task<bool> DeposerToutInterneAsync(CancellationToken ct)
    {
        Journaliseur.Info($"[BANQUE-START] Démarrage dépôt — poids actuel {_perso.PourcentagePoids:F1}% (seuil={_cfg.SeuilPoidsPct}%)");

        // Reset des flags observateurs avant ouverture (sinon un EV résiduel
        // d'un workflow précédent ferait croire que la banque est déjà fermée).
        BanqueFermeeObservee = false;

        // Reset map d'attente OR : pourrait contenir des résidus d'un workflow
        // précédent qui aurait abort sur ECK5 timeout (UIDs jamais retirés
        // de la map → log [BANQUE-LOST] erroné au cycle suivant).
        EnvoyesEnAttenteOR.Clear();

        // === Étape 1 : ouvrir le coffre banque (ApS, CLAIR) ===
        // ⚠ PROTOCOLE Hystoria : pas de dialogue NPC, le coffre interactif s'ouvre directement.
        // Le serveur répond ECK5 (Échange Créé kind=5) puis EL (liste vide ou contenu).
        // FIX 03:26 : on bloque les EV émis par le vrai client Dofus durant
        // tout le dépôt — sinon il ferme le coffre 2-4s après ApS (le vrai
        // client n'a pas conscience que le bot a ouvert la banque).
        BloquerEvClient = true;
        BanqueOuvertureObservee = false;  // reset avant ApS, set par TrameJeu sur ECK

        // FIX 04:08 : délai de 1.5s avant ApS pour laisser le serveur sortir
        // de l'état combat (GC1 client + GCK + GDM map data prennent ~500ms-
        // 1s). Sans ce délai, le serveur ignore l'ApS — forensic 04:03 :
        // 4 tentatives consécutives RIEN n'a été déposé alors que ECK5 ne
        // vient jamais. Note user : « banque mobile uniquement, jamais coffre
        // physique » → on ne peut PAS contourner par zaap, l'ApS doit marcher.
        await Task.Delay(1500, ct).ConfigureAwait(false);

        Journaliseur.Info("[BANQUE] → ApS (ouverture coffre)");
        await _session.EnvoyerAuServeurAsync("ApS").ConfigureAwait(false);

        // === Attente ECK5 (confirmation serveur) — 2s max ===
        // Si pas d'ECK reçu : ApS a été ignoré (perso pas près d'un coffre
        // banque, ou map sans banque mobile, ou état combat résiduel).
        // Inutile d'envoyer des EMO+ dans le vide → abort propre.
        int attenduMs = 0;
        while (attenduMs < 2000 && !BanqueOuvertureObservee)
        {
            if (ct.IsCancellationRequested) break;
            await Task.Delay(100, ct).ConfigureAwait(false);
            attenduMs += 100;
        }
        if (!BanqueOuvertureObservee)
        {
            Journaliseur.Avertir(
                "[BANQUE-TIMEOUT] ❌ Pas de ECK5 reçu 2s après ApS — coffre PAS OUVERT côté serveur "
                + "(perso pas près d'un coffre banque, ou map sans banque mobile, ou état "
                + "combat résiduel). Abort workflow, RIEN n'a été déposé.");
            return false;
        }
        Journaliseur.Info("[BANQUE-START] ✅ ECK5 reçu — coffre ouvert, démarrage des dépôts");
        // RESET EV flag : le serveur peut émettre EV (fermer ancienne session)
        // JUSTE AVANT ECK5 (ex: ApS d'un workflow précédent abort sans EV →
        // 2ème ApS → serveur ferme ancienne + ouvre nouvelle → EV puis ECK).
        // Cet EV est PRÉ-coffre-ouvert, à ignorer pour le pilote courant.
        BanqueFermeeObservee = false;
        // ATTENDRE STABILISATION INVENTAIRE 2.5s : les OQ pour les items
        // récemment lootés peuvent arriver après ECK5 (les drop combat
        // génèrent OQ avec qte initiale 1 puis OQ correctifs avec qte stack).
        // Forensic 06:51 : pass 1 envoyait EMO+|1 pour 7 items à qte réelle
        // 18/17/11 → tous timeouts car serveur incohérent → pass 2 corrigeait
        // avec les vraies qtés. Cette pause supprime ce double effort.
        await Task.Delay(2500, ct).ConfigureAwait(false);

        // === Étapes 2+3 : SINGLE-PASS de dépôt (alignement dyshay) ===
        // ADR-BANQUE : avec la suppression locale optimiste après chaque
        // EMO+, la pass 1 voit déjà l'inventaire après décrément local de
        // chaque envoi. Les items dont l'OR n'arrive pas sont considérés
        // déposés (pattern dyshay StoreAllObjectsAction.cs : pas de retry,
        // resync via OAK du prochain combat). Évite la boucle stérile
        // 9-passes-0-OR observée (AGENT 1 « Pattern récurrent »).
        int totalDeposes = 0;
        int totalConfirmes = 0;
        int totalRejetesSec = 0;
        const int MAX_PASSES = 1;
        for (int pass = 1; pass <= MAX_PASSES; pass++)
        {
            if (ct.IsCancellationRequested) break;
            if (BanqueFermeeObservee) break;

            // Snapshot SOUS LOCK : évite InvalidOperationException si
            // OnObjetAjout/OnObjetQuantite mutent la liste pendant ToList()
            // (race UI thread, forensic AGENT 5 §1).
            List<ObjetInventaire> snapshot;
            lock (_perso.Inventaire)
            {
                snapshot = _perso.Inventaire.ToList();
            }
            Journaliseur.Info($"[BANQUE-FILTRE] Pass {pass} snapshot : {snapshot.Count} items dans l'inventaire local.");
            var aDeposer = CalculerItemsADeposer(snapshot);
            if (aDeposer.Count == 0)
            {
                Journaliseur.Info($"[BANQUE-FILTRE] Pass {pass}/{MAX_PASSES} : 0 item à déposer, arrêt boucle.");
                break;
            }
            Journaliseur.Info(
                $"[BANQUE-FILTRE] === Pass {pass}/{MAX_PASSES} : {aDeposer.Count}/{snapshot.Count} item(s) sélectionné(s) ===");

            int deposes = 0;
            int confirmes = 0;
            int rejetesSec = 0;
            // Snapshot du compteur global d'OR au début de la pass pour
            // calculer combien on en a reçu à la fin du burst.
            int compteurOrAuDebutPass = System.Threading.Interlocked.CompareExchange(ref CompteurObjectRemove, 0, 0);
            foreach (var item in aDeposer)
            {
                if (ct.IsCancellationRequested) break;
                // L'user (ou un autre process) a fermé la banque → on stoppe net,
                // sans envoyer la fermeture EV nous-mêmes (déjà fait).
                if (BanqueFermeeObservee)
                {
                    Journaliseur.Avertir(
                        $"[BANQUE] EV observé pendant dépôt — arrêt immédiat (pass {pass}, "
                        + $"{deposes} envoyés / {confirmes} confirmés OR, "
                        + $"{aDeposer.Count - deposes} restants annulés).");
                    BloquerEvClient = false;
                    return true;
                }

                // Sécurité ultime (relue à CHAQUE item car l'user peut éditer la config
                // pendant le workflow → IdsAGarder peut grandir, catégorie peut être
                // décochée). Cause normale du refus à mi-workflow ; pas un bug.
                if (!EstAutoriseADeposer(item))
                {
                    Journaliseur.Debogue(
                        $"[BANQUE] item refusé en cours de dépôt #{item.Identifiant} template={item.IdTemplate} "
                        + "(config a probablement été éditée en live ou item décoché)");
                    rejetesSec++;
                    continue;
                }

                // Skip items qte=0 (transitoire entre OQ/OR snapshot).
                if (item.Quantite <= 0)
                {
                    Journaliseur.Debogue($"[BANQUE] skip item #{item.Identifiant} template={item.IdTemplate} (qte=0)");
                    continue;
                }

                // Pattern dyshay (réf. StoreAllObjectsAction.cs) : envoyer
                // l'EMO+ et juste attendre 300ms avant le suivant. PAS d'attente
                // OR par item (cause des timeouts en cascade + cadence lente).
                // On comptabilise les OR globalement à la fin du burst.
                var paquet = $"EMO+{item.Identifiant}|{item.Quantite}";
                var infoItem = BaseDonnees.Instance.Item(item.IdTemplate);
                var nomItem = infoItem?.Nom ?? "?";
                var typeItem = infoItem?.IdType ?? -1;
                Journaliseur.Info(
                    $"[BANQUE-DEPOT] → {paquet} « {nomItem} » (template {item.IdTemplate}, type={typeItem}, qte {item.Quantite})");
                await _session.EnvoyerAuServeurAsync(paquet).ConfigureAwait(false);
                deposes++;

                // SUPPRESSION LOCALE OPTIMISTE (pattern dyshay
                // StoreAllObjectsAction.cs:35). Sans ça, si l'OR ne revient
                // pas (timeout, désync serveur, mismatch qty), la pass
                // suivante re-snapshot l'inventaire et re-soumet le MÊME UID
                // → boucle stérile 9× observée (AGENT 1). Race-safe :
                // OnObjetRetrait sous lock(inv) ; si l'OR arrive après
                // notre suppression, RemoveAll retourne 0 → no-op silencieux.
                bool supprimeLocal = _perso.SupprimerObjetOptimiste(item.Identifiant);
                EnvoyesEnAttenteOR[item.Identifiant] = DateTime.UtcNow;
                if (supprimeLocal)
                    Journaliseur.Debogue($"[BANQUE-DEPOT] Suppression optimiste OK : UID {item.Identifiant} retiré localement.");

                // Délai fixe 300ms (= dyshay) entre chaque EMO+.
                await Task.Delay(300, ct).ConfigureAwait(false);
            }

            // BURST TERMINÉ : attendre jusqu'à 8s que tous les OR arrivent du
            // serveur (sous charge Hystoria, le burst d'OR peut prendre 2-5s).
            int compteurOrAvantBurst = compteurOrAuDebutPass;
            int compteurOrAttendu = compteurOrAvantBurst + deposes;
            int attente = 0;
            while (attente < 8000 && System.Threading.Interlocked.CompareExchange(ref CompteurObjectRemove, 0, 0) < compteurOrAttendu)
            {
                await Task.Delay(200, ct).ConfigureAwait(false);
                attente += 200;
                if (BanqueFermeeObservee) break;
            }
            int orRecus = System.Threading.Interlocked.CompareExchange(ref CompteurObjectRemove, 0, 0) - compteurOrAvantBurst;
            confirmes = Math.Min(deposes, orRecus);

            // Détection des items en attente OR > 10s : potentiellement perdus
            // côté serveur (refus silencieux ou OR dropé par le proxy).
            // Loggués mais PAS réinjectés localement (acceptation du risque
            // mineur — l'item est soit déposé serveur-side sans ACK, soit
            // récupérable au prochain OAK / changement de map). Cf. ADR-BANQUE
            // §Risques.
            var seuilPerdu = DateTime.UtcNow.AddSeconds(-10);
            var perdus = EnvoyesEnAttenteOR
                .Where(kv => kv.Value < seuilPerdu)
                .Select(kv => kv.Key)
                .ToList();
            foreach (var uidPerdu in perdus)
            {
                Journaliseur.Avertir(
                    $"[BANQUE-LOST] UID {uidPerdu} envoyé sans OR retour depuis >10s — "
                    + "item considéré déposé côté serveur, local OK (suppression optimiste).");
                EnvoyesEnAttenteOR.TryRemove(uidPerdu, out _);
            }

            Journaliseur.Info(
                $"[BANQUE-CONFIRM] Fin pass {pass}/{MAX_PASSES} — {confirmes}/{deposes} OR reçus en {attente}ms "
                + $"({rejetesSec} rejets sécurité, {perdus.Count} perdus). Poids actuel {_perso.PourcentagePoids:F1}%");

            totalDeposes += deposes;
            totalConfirmes += confirmes;
            totalRejetesSec += rejetesSec;

            // Si tout a été confirmé OU rien n'a été déposé → inutile de reboucler.
            if (deposes == 0 || confirmes == deposes)
            {
                Journaliseur.Info($"[BANQUE-CONFIRM] Pass {pass} complète — arrêt boucle.");
                break;
            }

            // Pause 3s avant le pass suivant : laisse arriver les OR/OQ
            // différés du serveur Hystoria (peut être 2-3s sous charge), qui
            // décrémentent l'inventaire et permettent au pass suivant de
            // skip les items déjà partiellement déposés.
            // Note : avec MAX_PASSES=1, cette pause n'est jamais atteinte —
            // gardée par sécurité au cas où MAX_PASSES serait remonté.
            await Task.Delay(3000, ct).ConfigureAwait(false);
        }

        // Refresh UI après burst : un seul NotifierInventaireChange pour
        // matérialiser toutes les suppressions optimistes (évite le storm UI
        // pendant le burst, cf. Personnage.SupprimerObjetOptimiste).
        _perso.NotifierInventaireChange();

        // === Étape 4 : fermer la banque (EV, canal chiffré auto) ===
        BloquerEvClient = false;
        Journaliseur.Info("[BANQUE-END] → EV (fermeture coffre)");
        await _session.EnvoyerAuServeurAsync("EV").ConfigureAwait(false);
        await Delai(ct).ConfigureAwait(false);

        // Log final : on regarde le poids inventaire pour juger du succès,
        // pas le ratio EMO+/OR (en pass unique sans ré-envois, les deux sont
        // alignés sauf cas d'OR perdu — alors [BANQUE-LOST] aura tracé l'écart).
        if (totalConfirmes == totalDeposes)
        {
            Journaliseur.Info($"[BANQUE-END] ✅ Dépôt terminé — {totalConfirmes}/{totalDeposes} OR confirmés, "
                + $"{totalRejetesSec} refusés (sécurité), poids final {_perso.PourcentagePoids:F1}%");
        }
        else
        {
            Journaliseur.Info($"[BANQUE-END] ✅ Dépôt terminé — {totalConfirmes}/{totalDeposes} OR confirmés "
                + $"({totalDeposes - totalConfirmes} envois sans ACK serveur, items déposés serveur-side "
                + $"selon pattern dyshay). Poids final {_perso.PourcentagePoids:F1}%");
        }
        return totalConfirmes > 0;
    }

    /// <summary>
    /// Calcule la liste effective d'items à déposer selon les filtres :
    /// catégorie (Équipement/Ressource/Consommable/Quête/Inconnu), liste
    /// blanche/noire d'IDs template, seuils par template.
    /// </summary>
    internal List<ObjetInventaire> CalculerItemsADeposer(IEnumerable<ObjetInventaire> inventaire)
    {
        var bdd = BaseDonnees.Instance;
        var resultat = new List<ObjetInventaire>();

        // FILTRE SAC UNIQUEMENT : Position==63 sur Dofus 1.29 = sac.
        var itemsSac = new List<ObjetInventaire>();
        foreach (var o in inventaire)
            if (o.Position == 63 && o.Quantite > 0) itemsSac.Add(o);

        // 1) Compter le total par template (pour les seuils) — sur les items du sac seulement.
        var totalParTemplate = new Dictionary<int, int>();
        foreach (var o in itemsSac)
            totalParTemplate[o.IdTemplate] = totalParTemplate.GetValueOrDefault(o.IdTemplate) + o.Quantite;

        // STATS DE DIAGNOSTIC : compte par catégorie pour aider à comprendre
        // pourquoi N items sont rejetés (forensic 06:05 : user pensait que
        // « presque rien » était déposé). Sortie : breakdown du sac.
        int statEquip = 0, statRes = 0, statConso = 0, statQuete = 0, statInco = 0;
        int statGarde = 0, statForce = 0, statSeuil = 0;
        var rejetsParRaison = new Dictionary<string, int>();

        int statPhantom = 0;
        foreach (var item in itemsSac)
        {
            // Skip items « fantômes » : template hors plage Dofus 1.29 standard.
            // Forensic 01:58 : template=41148 « ? » type=-1 qte=112 — UID
            // 7962919339 généré probablement par un OQ corrompu. Le serveur
            // ne possède PAS cet item → tous les EMO+ timeout, plombent le
            // workflow. Templates Dofus valides : 1-30000.
            if (item.IdTemplate < 1 || item.IdTemplate > 30000)
            {
                Journaliseur.Debogue(
                    $"[BANQUE] Item PHANTOM ignoré : #{item.Identifiant} template={item.IdTemplate} "
                    + $"qte={item.Quantite} (hors plage 1-30000 = item fantôme côté serveur)");
                statPhantom++;
                continue;
            }

            // Liste noire : on garde TOUJOURS.
            if (_cfg.IdsAGarder.Contains(item.IdTemplate)) { statGarde++; continue; }

            // Liste blanche : on dépose TOUJOURS.
            if (_cfg.IdsADeposerForce.Contains(item.IdTemplate))
            {
                statForce++;
                int avant = resultat.Count;
                AjouterAvecSeuil(item, totalParTemplate, resultat);
                if (resultat.Count == avant) statSeuil++;
                continue;
            }

            // Catégorie via le type de l'item dans la BDD.
            var info = bdd.Item(item.IdTemplate);
            int typeBrut = info?.IdType ?? -1;
            var cat = info != null
                ? CategoriseurObjet.Categoriser(info.IdType)
                : CategorieObjet.Inconnu;

            // FALLBACK NOM : sur Hystoria, beaucoup d'items DROP COMBAT ont
            // IdType=0 dans items_merged.json (entrée BDD incomplète) →
            // tombaient en Inconnu → déposés à tort. On reclasse en
            // Équipement si le nom commence par un mot-clé arme/équipement.
            // Forensic 06:34 : « Dague de Iritim » template=3480 type=0
            // était envoyé en banque alors que c'est une dague.
            if (cat == CategorieObjet.Inconnu && info != null && !string.IsNullOrEmpty(info.Nom))
            {
                var n = info.Nom;
                if (EstNomEquipement(n))
                {
                    cat = CategorieObjet.Equipement;
                    Journaliseur.Debogue(
                        $"[BANQUE] Reclassif par nom : « {n} » → Équipement (typeBrut={typeBrut} était Inconnu)");
                }
            }

            switch (cat)
            {
                case CategorieObjet.Equipement: statEquip++; break;
                case CategorieObjet.Ressource: statRes++; break;
                case CategorieObjet.Consommable: statConso++; break;
                case CategorieObjet.Quete: statQuete++; break;
                case CategorieObjet.Inconnu: statInco++; break;
            }

            bool autorise = cat switch
            {
                CategorieObjet.Equipement => _cfg.DeposerEquipements,
                CategorieObjet.Ressource => _cfg.DeposerRessources,
                CategorieObjet.Consommable => _cfg.DeposerConsommables,
                CategorieObjet.Quete => _cfg.DeposerQuetes,
                CategorieObjet.Inconnu => _cfg.DeposerInconnus,
                _ => false,
            };
            if (!autorise)
            {
                string raison = $"{cat} non coché (typeBrut={typeBrut})";
                rejetsParRaison[raison] = rejetsParRaison.GetValueOrDefault(raison) + 1;
                string nom = info?.Nom ?? "?";
                Journaliseur.Debogue(
                    $"[BANQUE] Item REJETÉ : #{item.Identifiant} template={item.IdTemplate} « {nom} » "
                    + $"qte={item.Quantite} cat={cat} typeBrut={typeBrut} (catégorie non cochée)");
                continue;
            }

            int avant2 = resultat.Count;
            AjouterAvecSeuil(item, totalParTemplate, resultat);
            if (resultat.Count == avant2) { statSeuil++; }
        }

        // Log breakdown final.
        Journaliseur.Info(
            $"[BANQUE] === Breakdown sac (Position=63) : {itemsSac.Count} items ===");
        Journaliseur.Info(
            $"[BANQUE] Par catégorie : Équip={statEquip} (dépose={_cfg.DeposerEquipements}) | "
            + $"Ress={statRes} (dépose={_cfg.DeposerRessources}) | Conso={statConso} (dépose={_cfg.DeposerConsommables}) | "
            + $"Quête={statQuete} (dépose={_cfg.DeposerQuetes}) | Inconnu={statInco} (dépose={_cfg.DeposerInconnus})");
        if (statGarde > 0 || statForce > 0)
            Journaliseur.Info($"[BANQUE] Listes : {statGarde} item(s) en IdsAGarder, {statForce} en IdsADeposerForce");
        if (statPhantom > 0)
            Journaliseur.Info($"[BANQUE] {statPhantom} item(s) FANTÔMES skippés (template hors 1-30000)");
        if (statSeuil > 0)
            Journaliseur.Info($"[BANQUE] {statSeuil} item(s) skippés par SeuilParTemplate (quantité < seuil de garde)");
        if (rejetsParRaison.Count > 0)
        {
            foreach (var kv in rejetsParRaison)
                Journaliseur.Info($"[BANQUE] Rejets : {kv.Value} × « {kv.Key} »");
        }
        Journaliseur.Info($"[BANQUE] → {resultat.Count}/{itemsSac.Count} item(s) RETENUS pour dépôt");

        return resultat;
    }

    /// <summary>
    /// Ajoute l'item à la liste de dépôt en respectant le <c>SeuilParTemplate</c> :
    /// si l'user veut garder N exemplaires d'un template (ex. 50 pains), on
    /// ajuste la quantité déposable pour conserver ce seuil en inventaire.
    /// </summary>
    private void AjouterAvecSeuil(ObjetInventaire item, Dictionary<int, int> totalParTemplate, List<ObjetInventaire> resultat)
    {
        if (!_cfg.SeuilParTemplate.TryGetValue(item.IdTemplate, out int seuil) || seuil <= 0)
        {
            resultat.Add(item);
            return;
        }
        // Combien de cet item dans l'inventaire ?
        int total = totalParTemplate.GetValueOrDefault(item.IdTemplate);
        // Combien on peut déposer en gardant `seuil` ?
        int dispo = total - seuil;
        if (dispo <= 0) return;

        int qteADep = System.Math.Min(item.Quantite, dispo);
        if (qteADep <= 0) return;

        // On dépose en ajustant la quantité (sans muter l'original).
        resultat.Add(new ObjetInventaire
        {
            Identifiant = item.Identifiant,
            IdTemplate = item.IdTemplate,
            Quantite = qteADep,
            Position = item.Position,
        });
        // Mettre à jour le total restant en inventaire pour les prochains items du même template.
        totalParTemplate[item.IdTemplate] = total - qteADep;
    }

    /// <summary>
    /// Force le serveur à renvoyer l'inventaire complet en effectuant un
    /// aller-retour entre 2 maps adjacentes. Sur Dofus 1.29, le serveur
    /// push les nouveaux OAK incrémentalement après combat — si un OAK est
    /// manqué (race condition, proxy buffer), l'item devient invisible
    /// côté bot jusqu'à la prochaine resync. Le changement de map déclenche
    /// un GDM qui force le serveur à re-pousher l'inventaire perso.
    /// On essaie est→ouest, puis nord→sud si la 1ère échoue. ~5s total.
    /// </summary>
    private async Task ResyncInventaireParChangementCarteAsync(CancellationToken ct)
    {
        var mapDepart = _perso.CarteCourante;
        if (!mapDepart.HasValue)
        {
            Journaliseur.Debogue("[BANQUE] Resync skip : pas de map courante.");
            return;
        }

        // Paires de directions opposées à essayer.
        var paires = new[] { ("est", "ouest"), ("ouest", "est"), ("nord", "sud"), ("sud", "nord") };

        foreach (var (allee, retour) in paires)
        {
            if (ct.IsCancellationRequested) return;
            Journaliseur.Info($"[BANQUE] Étape 0/4 : resync inventaire — sortie {allee}…");
            _ = _api.ChangerMapDirectionAsync(allee, ct); // fire-and-forget
            // On NE se base PAS sur le retour de ChangerMapDirectionAsync
            // (timeout court interne) → on regarde directement si la map
            // a effectivement changé en attendant jusqu'à 5s.
            int attente = 0;
            while (attente < 5000 && _perso.CarteCourante == mapDepart)
            {
                if (ct.IsCancellationRequested) return;
                await Task.Delay(200, ct).ConfigureAwait(false);
                attente += 200;
            }
            if (_perso.CarteCourante == mapDepart)
            {
                Journaliseur.Debogue($"[BANQUE] Resync {allee} : pas de changement de map après {attente}ms — essai suivant.");
                continue;
            }

            var mapIntermediaire = _perso.CarteCourante;
            Journaliseur.Info(
                $"[BANQUE] Resync : sortie {allee} OK ({mapDepart}→{mapIntermediaire}), attente 1s avant retour…");
            await Task.Delay(1000, ct).ConfigureAwait(false);

            // Retour pour rester sur la map de farm initiale.
            Journaliseur.Info($"[BANQUE] Resync : retour {retour}…");
            _ = _api.ChangerMapDirectionAsync(retour, ct);
            attente = 0;
            while (attente < 5000 && _perso.CarteCourante == mapIntermediaire)
            {
                if (ct.IsCancellationRequested) return;
                await Task.Delay(200, ct).ConfigureAwait(false);
                attente += 200;
            }
            if (_perso.CarteCourante == mapDepart)
            {
                await Task.Delay(1000, ct).ConfigureAwait(false);
                Journaliseur.Info(
                    $"[BANQUE] ✅ Resync OK — map d'origine {mapDepart} retrouvée, inventaire re-synchronisé.");
            }
            else
            {
                Journaliseur.Avertir(
                    $"[BANQUE] Resync : retour {retour} a abouti sur map {_perso.CarteCourante} "
                    + $"(au lieu de {mapDepart}). Workflow continue depuis cette map.");
            }
            return;
        }

        Journaliseur.Avertir(
            "[BANQUE] Resync inventaire : aucune des 4 directions n'a changé la carte — workflow continue sans resync.");
    }

    /// <summary>
    /// Détecte par PRÉFIXE DU NOM si l'item est un équipement (arme,
    /// anneau, etc.) — utilisé en fallback quand <c>IdType=0</c> dans la
    /// BDD Hystoria (forensic 06:34 : 64/117 items déposés à tort dont
    /// « Dague », « Arc », « Hache » qui avaient type=0 dans la BDD).
    /// </summary>
    private static bool EstNomEquipement(string nom)
    {
        // Préfixes d'armes / équipements Dofus 1.29.
        string[] prefixesEquipement = {
            "Dague", "Arc", "Bâton", "Baton", "Marteau", "Pelle", "Hache",
            "Épée", "Epée", "Baguette", "Faux",
            "Anneau", "Ceinture", "Coiffe", "Casque", "Bottes", "Cape",
            "Amulette", "Bouclier", "Robe", "Chapeau",
            // Familiers Dofus drop : Blop* (panoplie blop), Amublop, Blopanneau, Blopture, Bloptes
            "Blop", "Amublop", "Bouftou",
            // Craqueleur panoplie
            "Craquel",
        };
        foreach (var p in prefixesEquipement)
            if (nom.StartsWith(p, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>
    /// Vérifie une dernière fois (défensif) qu'on a le droit de déposer cet item :
    /// JAMAIS un item dans <see cref="ConfigBanque.IdsAGarder"/>, JAMAIS un item
    /// de catégorie Quête si <see cref="ConfigBanque.DeposerQuetes"/> est false.
    /// </summary>
    internal bool EstAutoriseADeposer(ObjetInventaire item)
    {
        if (_cfg.IdsAGarder.Contains(item.IdTemplate)) return false;
        if (_cfg.IdsADeposerForce.Contains(item.IdTemplate)) return true;

        var info = BaseDonnees.Instance.Item(item.IdTemplate);
        var cat = info != null ? CategoriseurObjet.Categoriser(info.IdType) : CategorieObjet.Inconnu;

        // Même fallback nom-based qu'en CalculerItemsADeposer (cohérence).
        if (cat == CategorieObjet.Inconnu && info != null && !string.IsNullOrEmpty(info.Nom)
            && EstNomEquipement(info.Nom))
        {
            cat = CategorieObjet.Equipement;
        }

        return cat switch
        {
            CategorieObjet.Equipement => _cfg.DeposerEquipements,
            CategorieObjet.Ressource => _cfg.DeposerRessources,
            CategorieObjet.Consommable => _cfg.DeposerConsommables,
            CategorieObjet.Quete => _cfg.DeposerQuetes,
            CategorieObjet.Inconnu => _cfg.DeposerInconnus,
            _ => false,
        };
    }

    /// <summary>
    /// Attend que <see cref="CompteurObjectRemove"/> avance (= un OR&lt;persoId&gt;|&lt;uid&gt;
    /// reçu, donc le dépôt est confirmé côté serveur). Sinon timeout — on
    /// continue quand même pour éviter le blocage si le compteur n'est pas câblé.
    /// </summary>
    /// <summary>Attend que le compteur OR augmente (= serveur a confirmé un
    /// objet retiré inventaire). Retourne true si confirmé, false si timeout
    /// (le serveur a probablement refusé silencieusement le dépôt).</summary>
    private static async Task<bool> AttendreObjectRemoveAsync(int compteurAvant, int timeoutMs, CancellationToken ct)
    {
        int waitMs = 0;
        while (waitMs < timeoutMs)
        {
            if (ct.IsCancellationRequested) return false;
            int actuel = System.Threading.Interlocked.CompareExchange(ref CompteurObjectRemove, 0, 0);
            if (actuel > compteurAvant) return true;
            await Task.Delay(100, ct).ConfigureAwait(false);
            waitMs += 100;
        }
        return false;
    }

    private Task Delai(CancellationToken ct = default)
        => Task.Delay(System.Random.Shared.Next(
            System.Math.Max(50, _cfg.DelaiActionMinMs),
            System.Math.Max(100, _cfg.DelaiActionMaxMs)), ct);
}
