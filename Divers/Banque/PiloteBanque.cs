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

/// <summary>V2 — Type de résultat d'un EMO+ retourné par le serveur Hystoria.
/// Cf. ADR <c>docs/PLAN-FIX-BANQUE-V2.md</c>.</summary>
public enum TypeResultatDepot
{
    /// <summary>OR&lt;uid&gt; reçu : pile entièrement déposée.</summary>
    Confirme,
    /// <summary>OQ&lt;uid&gt;|qte&gt;0 : pile partiellement déposée, qteRestante reste serveur → retry.</summary>
    PartielServeur,
    /// <summary>OQ&lt;uid&gt;|qte=0 : pile vidée (équivalent OR — Hystoria utilise parfois OQ pour signaler dépôt complet).</summary>
    PartielVide,
    /// <summary>Ni OR ni OQ après timeout : EMO+ probablement ignoré par le serveur.</summary>
    Timeout,
}

/// <summary>V2 — Résultat d'un EMO+ avec qte restante côté serveur.</summary>
public readonly record struct ResultatDepot(TypeResultatDepot Type, int QteRestante);

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
    /// Clé = UID inventaire, valeur = timestamp UTC d'envoi.
    /// V1 héritage : conservé pour compat. V2 utilise <see cref="AttenteResultat"/>.</summary>
    public static readonly System.Collections.Concurrent.ConcurrentDictionary<long, DateTime> EnvoyesEnAttenteOR = new();

    /// <summary>V2 — Map d'attente OR/OQ par UID. Clé = UID inventaire envoyé en EMO+.
    /// Valeur = TaskCompletionSource résolu par <see cref="SignalerOR"/> ou
    /// <see cref="SignalerOQ"/> quand le serveur répond. Le pilote crée le TCS
    /// AVANT d'envoyer l'EMO+ pour éviter la race « OR arrive avant que la map
    /// soit alimentée ». Cf. ADR <c>docs/PLAN-FIX-BANQUE-V2.md</c>.</summary>
    public static readonly System.Collections.Concurrent.ConcurrentDictionary<long, TaskCompletionSource<ResultatDepot>> AttenteResultat = new();

    /// <summary>V2 — Signale qu'un OR&lt;uid&gt; vient d'être reçu (TrameJeu).
    /// Retourne true si l'UID était attendu (dépôt confirmé côté pilote banque).</summary>
    public static bool SignalerOR(long uid)
    {
        if (AttenteResultat.TryRemove(uid, out var tcs))
        {
            tcs.TrySetResult(new ResultatDepot(TypeResultatDepot.Confirme, 0));
            return true;
        }
        return false;
    }

    /// <summary>V2 — Signale qu'un OQ&lt;uid&gt;|&lt;qte&gt; vient d'être reçu (TrameJeu).
    /// Retourne true si l'UID était attendu (dépôt partiel ou vide signalé via OQ).
    /// Si false : OQ de loot normal (chemin TrameJeu inchangé).</summary>
    public static bool SignalerOQ(long uid, int qteRestante)
    {
        if (AttenteResultat.TryRemove(uid, out var tcs))
        {
            var type = qteRestante > 0 ? TypeResultatDepot.PartielServeur : TypeResultatDepot.PartielVide;
            tcs.TrySetResult(new ResultatDepot(type, qteRestante));
            return true;
        }
        return false;
    }

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
        // de la map → log erroné au cycle suivant).
        EnvoyesEnAttenteOR.Clear();
        // V2 — drainer les TCS orphelins puis purger (sinon un OR tardif d'un
        // workflow précédent signalerait faussement un Confirme sur le nouveau).
        foreach (var kv in AttenteResultat)
            kv.Value.TrySetResult(new ResultatDepot(TypeResultatDepot.Timeout, 0));
        AttenteResultat.Clear();

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

        // === Étapes 2+3 : MULTI-PASS SYNCHRONE PAR UID (V2 — ADR PLAN-FIX-BANQUE-V2) ===
        // Cause racine V2 (AGENT 1 V2) : Hystoria répond OQ<uid>|<qteRestante>
        // au lieu de OR<uid> sur dépôt PARTIEL (pile vidée à ~92-94%). Le V1
        // burst fire-and-forget interprétait l'absence d'OR comme « perdu »
        // alors qu'il s'agit d'un dépôt partiel → suppression optimiste locale
        // = perte de vue des unités restantes côté serveur.
        // V2 : TCS par UID résolu par OR (Confirme) ou OQ (PartielServeur/Vide),
        // retry sur PartielServeur+Timeout uniquement (max 3 passes), 0
        // suppression optimiste — l'inventaire local est mis à jour par les
        // handlers TrameJeu sur ACK réel.
        int totalDeposes = 0, totalConfirmes = 0, totalPartiels = 0, totalTimeouts = 0;
        int totalRejetesSec = 0;
        const int MAX_PASSES = 3;
        const int TIMEOUT_PAR_ITEM_MS = 500;
        var rng = System.Random.Shared;

        // Construction initiale de la queue à déposer. Snapshot sous lock
        // (race UI thread + handlers TrameJeu).
        List<ObjetInventaire> aDeposerInit;
        lock (_perso.Inventaire)
        {
            aDeposerInit = _perso.Inventaire.ToList();
        }
        var queueDepot = CalculerItemsADeposer(aDeposerInit);
        Journaliseur.Info($"[BANQUE-FILTRE] {queueDepot.Count}/{aDeposerInit.Count} item(s) éligibles au dépôt initial.");

        for (int pass = 1; pass <= MAX_PASSES && queueDepot.Count > 0; pass++)
        {
            if (ct.IsCancellationRequested) break;
            if (BanqueFermeeObservee) break;

            Journaliseur.Info($"[BANQUE-FILTRE] === Pass {pass}/{MAX_PASSES} : {queueDepot.Count} item(s) à déposer ===");
            var queuePassSuivante = new List<ObjetInventaire>();
            int deposesPass = 0, confirmesPass = 0, partielsPass = 0, timeoutsPass = 0;

            foreach (var item in queueDepot)
            {
                if (ct.IsCancellationRequested) break;
                if (BanqueFermeeObservee)
                {
                    Journaliseur.Avertir(
                        $"[BANQUE] EV observé pendant dépôt — arrêt immédiat (pass {pass}, "
                        + $"{deposesPass} envoyés / {confirmesPass} confirmés).");
                    BloquerEvClient = false;
                    return true;
                }
                if (!EstAutoriseADeposer(item))
                {
                    Journaliseur.Debogue(
                        $"[BANQUE] item refusé en cours de dépôt #{item.Identifiant} template={item.IdTemplate} "
                        + "(config a probablement été éditée en live ou item décoché)");
                    totalRejetesSec++;
                    continue;
                }
                if (item.Quantite <= 0)
                {
                    Journaliseur.Debogue($"[BANQUE] skip item #{item.Identifiant} template={item.IdTemplate} (qte=0)");
                    continue;
                }

                // CRITIQUE — créer le TCS et l'insérer dans la map AVANT d'envoyer
                // l'EMO+. Sinon l'OR/OQ peut arriver entre EnvoyerAuServeurAsync et
                // AttenteResultat[uid]=tcs → SignalerOR/OQ retourne false → signal perdu.
                var tcs = new TaskCompletionSource<ResultatDepot>(TaskCreationOptions.RunContinuationsAsynchronously);
                AttenteResultat[item.Identifiant] = tcs;

                var paquet = $"EMO+{item.Identifiant}|{item.Quantite}";
                var infoItem = BaseDonnees.Instance.Item(item.IdTemplate);
                var nomItem = infoItem?.Nom ?? "?";
                Journaliseur.Info($"[BANQUE-DEPOT] → {paquet} « {nomItem} » (pass {pass}, qte {item.Quantite})");
                await _session.EnvoyerAuServeurAsync(paquet).ConfigureAwait(false);
                deposesPass++;

                // Attente résultat (OR Confirme / OQ PartielServeur / OQ PartielVide / Timeout).
                ResultatDepot res;
                try
                {
                    res = await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(TIMEOUT_PAR_ITEM_MS), ct).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    res = new ResultatDepot(TypeResultatDepot.Timeout, item.Quantite);
                    AttenteResultat.TryRemove(item.Identifiant, out _); // cleanup orphelin
                }

                switch (res.Type)
                {
                    case TypeResultatDepot.Confirme:
                        Journaliseur.Info($"[BANQUE-CONFIRM] UID {item.Identifiant} OR reçu (qte {item.Quantite} déposée).");
                        confirmesPass++;
                        break;

                    case TypeResultatDepot.PartielVide:
                        // OQ qte=0 — équivalent OR. OnObjetQuantite a déjà décrémenté/supprimé.
                        Journaliseur.Info($"[BANQUE-CONFIRM] UID {item.Identifiant} OQ qte=0 (équivalent OR, qte {item.Quantite} déposée).");
                        confirmesPass++;
                        break;

                    case TypeResultatDepot.PartielServeur:
                        // OQ qte>0 — dépôt partiel : ré-injecter avec la qte restante au pass suivant.
                        int deposeEffectif = item.Quantite - res.QteRestante;
                        Journaliseur.Avertir(
                            $"[BANQUE-PARTIEL] UID {item.Identifiant} dépôt partiel : "
                            + $"qte demandée {item.Quantite}, déposé {deposeEffectif}, restant {res.QteRestante} "
                            + $"→ retry pass {pass + 1}");
                        partielsPass++;
                        if (pass < MAX_PASSES)
                        {
                            queuePassSuivante.Add(new ObjetInventaire
                            {
                                Identifiant = item.Identifiant,
                                IdTemplate = item.IdTemplate,
                                Quantite = res.QteRestante,
                                Position = item.Position,
                            });
                            Journaliseur.Debogue($"[BANQUE-RETRY] UID {item.Identifiant} qte {res.QteRestante} pass {pass + 1}/{MAX_PASSES}");
                        }
                        else
                        {
                            Journaliseur.Avertir($"[BANQUE-FAIL] UID {item.Identifiant} dépôt partiel persistant après {MAX_PASSES} passes — abandon.");
                        }
                        break;

                    case TypeResultatDepot.Timeout:
                        Journaliseur.Avertir($"[BANQUE-TIMEOUT] UID {item.Identifiant} aucune réponse en {TIMEOUT_PAR_ITEM_MS}ms");
                        timeoutsPass++;
                        if (pass < MAX_PASSES)
                        {
                            queuePassSuivante.Add(item);
                            Journaliseur.Debogue($"[BANQUE-RETRY] UID {item.Identifiant} timeout → retry pass {pass + 1}/{MAX_PASSES}");
                        }
                        else
                        {
                            Journaliseur.Avertir($"[BANQUE-FAIL] UID {item.Identifiant} timeout après {MAX_PASSES} passes — abandon.");
                        }
                        break;
                }

                // Inter-item : 150ms ± 30% jitter (AGENT 3 V2 anti-bot scoring < 50ms variance).
                int delaiBase = 150;
                int jitter = (int)(delaiBase * 0.30);
                int delai = rng.Next(delaiBase - jitter, delaiBase + jitter + 1);
                await Task.Delay(delai, ct).ConfigureAwait(false);
            }

            Journaliseur.Info(
                $"[BANQUE-CONFIRM] Fin pass {pass}/{MAX_PASSES} — "
                + $"{confirmesPass} confirmés, {partielsPass} partiels, {timeoutsPass} timeouts, "
                + $"{queuePassSuivante.Count} à retenter. Poids actuel {_perso.PourcentagePoids:F1}%");

            totalDeposes += deposesPass;
            totalConfirmes += confirmesPass;
            totalPartiels += partielsPass;
            totalTimeouts += timeoutsPass;

            if (queuePassSuivante.Count == 0) break;

            // Délai inter-pass : laisser le serveur respirer.
            await Task.Delay(rng.Next(1500, 2500), ct).ConfigureAwait(false);
            queueDepot = queuePassSuivante;
        }

        // Refresh UI final : matérialise les suppressions cumulatives (OR + PartielVide).
        _perso.NotifierInventaireChange();

        // === Étape 4 : fermer la banque (EV, canal chiffré auto) ===
        BloquerEvClient = false;
        Journaliseur.Info("[BANQUE-END] → EV (fermeture coffre)");
        await _session.EnvoyerAuServeurAsync("EV").ConfigureAwait(false);
        await Delai(ct).ConfigureAwait(false);

        // Log final V2 : ratio confirmés / partiels / timeouts. Tout EMO+ a
        // maintenant une réponse déterministe (cf. ADR V2).
        Journaliseur.Info(
            $"[BANQUE-VERIFY] === Fin workflow V2 — {totalDeposes} envoyés, "
            + $"{totalConfirmes} confirmés (OR + PartielVide), {totalPartiels} partiels retry, "
            + $"{totalTimeouts} timeouts, {totalRejetesSec} refusés sécurité. "
            + $"Poids final {_perso.PourcentagePoids:F1}% ===");
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
