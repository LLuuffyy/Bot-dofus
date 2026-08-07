# ADR V2 — Fix dépôts banque (dépôts partiels OQ)

## Statut
Proposé : 2026-05-31 — AGENT 4 V2 ARCHITECT-V2
Remplace : ADR-BANQUE V1 (suppression locale optimiste post-EMO+, MAX_PASSES=1)

## Contexte

### Symptôme observé en V1
Après le fix V1 (suppression optimiste + single-pass dyshay-style), les logs montrent toujours un ratio `OR reçus / EMO+ envoyés` faible :
- Test 1 : 24 « LOST » sur 27 EMO+ → seulement 3 OR
- Test 3 : 8 « LOST » sur 10 EMO+ → seulement 2 OR
- Poids final inchangé ou faiblement réduit, items « gardés » côté serveur sans confirmation

Hypothèse V1 (« le serveur a juste perdu le OR ») = **fausse**.

### Résumé des 3 forensic V2

**AGENT 1 V2 — `docs/PATTERN-PAQUETS-PERDUS.md`**
Pour 100 % des UIDs marqués `LOST` (24/24 sur Test 1, 8/8 sur Test 3), un paquet `OQ<uid>|<qteRestante>` arrive du serveur dans les 200-450 ms suivant l'EMO+. **Aucun paquet OR n'est perdu** : le serveur a simplement répondu OQ au lieu d'OR pour signaler un dépôt **partiel** (la pile a été partiellement vidée, il reste `qteRestante` unités côté inventaire serveur). Recommandation : timeout d'attente OR-OU-OQ de 500 ms, re-soumission de l'EMO+ avec la nouvelle qte sur max 5 passes, inter-EMO+ 150 ms.

**AGENT 2 V2 — `docs/DYSHAY-RELOOK.md`**
dyshay (`StoreAllObjectsAction.cs`) fait bien burst fire-and-forget 300 ms suivi de suppression optimiste : le V1 est donc conforme. MAIS dyshay vise Dofus Retro Ankama officiel ; **Hystoria a un comportement spécifique sur OQ partiel** non documenté chez dyshay. Le pattern fire-and-forget ne suffit donc pas ici — il faut câbler une logique synchrone par UID.

**AGENT 3 V2 — `docs/HYSTORIA-RATE-LIMITS.md`**
Pas de rate-limit EMO+ documenté côté serveur. PiloteMarchand utilise déjà EMO+ à 80-200 ms sans perte. La cadence safe = 400-600 ms avec jitter ±30 %. Le V1 envoie à cadence métronomique 300 ms sans jitter (signature anti-bot probable). Recommandations : jitter ±30 %, stabilisation post-ECK5 5 s, EV/ApS si pass 1 < 50 % OR.

### Cause racine V2 consolidée

Quand le bot envoie `EMO+<uid>|<qte>` avec `qte > 1` et qu'Hystoria ne peut pas tout déposer (raison serveur non encore comprise — anti-flood interne, slot banque saturé, dépôt fractionné), le serveur :
1. Dépose une partie de la pile en banque (ex. 16 sur 18)
2. Laisse le reste dans l'inventaire serveur (2 unités)
3. Envoie `OQ<persoId>|<uid>,<qteRestante>` (et **PAS** d'OR)

Le V1 :
1. Suppression optimiste locale de l'UID immédiatement après EMO+ (toute la pile est retirée localement)
2. Aucune logique OQ pour distinguer « OQ loot normal » de « OQ dépôt partiel » → l'OQ partiel tombe dans la branche `Avertir(« OQ inconnu »)` (puisque l'UID a déjà été supprimé localement)
3. **Les 2 unités restantes côté serveur sont perdues de vue** côté bot jusqu'à un OAK / changement de map / ré-ouverture banque

L'ADR V2 corrige cela avec un pattern synchrone par UID utilisant `TaskCompletionSource<ResultatDepot>` et un traitement explicite de l'OQ partiel.

## Décision

Remplacer le pattern **burst fire-and-forget + suppression optimiste + compteur global OR** (V1) par un pattern **synchrone par UID avec `TaskCompletionSource<ResultatDepot>` + traitement explicite OR/OQ partiel + multi-pass intelligent (max 3 passes, ne ré-envoie QUE les UIDs partiels/timeout)**.

Conserver les éléments V1 qui marchent :
- Reset des flags + map d'attente au début du workflow (`PiloteBanque.cs:204-211`)
- Snapshot d'inventaire SOUS LOCK (race UI thread) (`PiloteBanque.cs:285-289`)
- Logs `[BANQUE-START]`, `[BANQUE-CONFIRM]`, `[BANQUE-END]`, `[BANQUE-FILTRE]`
- Filtre catégorie (`CalculerItemsADeposer`), filtre PHANTOM, `EstAutoriseADeposer`
- Délai de stabilisation 2.5 s post-ECK5
- `BloquerEvClient = true` pendant le burst, reset garanti via `finally`
- Bypass IA combat (zaap → ApS → dépôt)

Ajouter :
- Délais avec jitter ±30 % (recommandation AGENT 3 V2)
- Map `ConcurrentDictionary<long, TaskCompletionSource<ResultatDepot>>`
- Hooks `SignalerOR(uid)` / `SignalerOQ(uid, qteRestante)` consommés par les handlers `OnObjetRetrait` / `OnObjetQuantite`
- Boucle multi-pass (max 3) sur les seuls `PartielServeur` / `Timeout`

## Conséquences positives

- **0 item perdu de vue** : tout EMO+ a maintenant une réponse déterministe (Confirme / PartielVide / PartielServeur / Timeout)
- **Multi-pass borné et efficace** : pass 2 ne re-soumet que les `PartielServeur` (avec la `qteRestante` réelle) — fini les boucles 9× stériles
- **Logs lisibles** : un dépôt partiel est tracé `[BANQUE-PARTIEL]` au lieu d'apparaître comme `LOST` (le « LOST » V1 était trompeur — il ne s'agissait pas d'une perte)
- **Anti-bot friendly** : jitter ±30 % cassera la signature métronomique 300 ms
- **Robustesse** : un timeout serveur réel (vraie perte) reste détectable via `Timeout` après 3 passes (`[BANQUE-FAIL]`)

## Conséquences négatives / risques

- **Légèrement plus lent en cas de pass unique** : on attend explicitement OR ou OQ par item (jusqu'à 500 ms) au lieu de fire-and-forget — mais c'est exactement ce qu'il fallait. Estimation Test 1 (27 items, ~250 ms moyen) : 6.7 s en V2 vs ~8 s + 8 s burst-OR V1 ≈ aucun overhead net.
- **Code un peu plus complexe** : `ConcurrentDictionary<long, TaskCompletionSource>` + race entre handler async et timeout `WaitAsync`. Mitigation : tout passe par `TryRemove` + `TrySetResult` (idempotents).
- **Potentiel oubli d'un TCS non résolu** : si un EMO+ ne reçoit ni OR ni OQ, le TCS reste dans la map jusqu'au prochain Clear (début workflow). Acceptable car le `Clear()` au début du nouveau workflow purge tout.
- **Risque LOST silencieux résiduel** : si un OR arrive AVANT que le TCS soit inséré dans la map (race extrême), il est perdu. Mitigation : insérer TCS **avant** d'envoyer EMO+ (cf. étape 6).

## Causes racines confirmées V2 (mise à jour H1-H8 + nouvelle hypothèse)

| Hyp | Description | Statut V1 | Statut V2 |
|---|---|---|---|
| H1 | Suppression optimiste agressive | confirmé (utile contre boucle 9×) | toujours utile mais **paramétrée** par résultat |
| H2 | Compteur OR global imprécis | suspecté | **confirmé** : impossible de distinguer OR de tel ou tel UID — V2 utilise map par UID |
| H3 | Race UI thread sur snapshot | confirmé | conservé (lock(inv) au snapshot) |
| H4 | EV client spontané fermait coffre | confirmé (fix `BloquerEvClient`) | conservé |
| H5 | ApS sans ECK5 préalable | confirmé (fix attente ECK5 2s) | conservé, **éventuellement durci à 5 s** post-ECK5 si pass 1 < 50% |
| H6 | Items PHANTOM template hors plage | confirmé (fix 1-30000) | conservé |
| H7 | Cadence métronomique 300 ms = anti-bot | suspecté | **partiellement validé** AGENT 3 V2 → jitter ±30 % obligatoire |
| H8 | OR perdu par le proxy MITM | hypothèse V1 | **infirmé** AGENT 1 V2 (0 OR perdu, OQ arrive) |
| **H_PARTIEL (nouveau)** | **Hystoria répond OQ<uid>\|<qteRestante> au lieu d'OR sur dépôt fractionné quand qte > 1** | non identifié | **CAUSE RACINE V2 — confirmé AGENT 1 V2 (24/24 + 8/8)** |

## Plan d'implémentation détaillé

### Étape 1 — Nouveau modèle `ResultatDepotBanque`
**Fichier** : `Divers/Banque/PiloteBanque.cs` — ajouter en tête de fichier (avant `public sealed class PiloteBanque`).

**AVANT** : pas d'enum, pas de type résultat.

**APRÈS** :
```csharp
/// <summary>Type de résultat d'un EMO+ — pattern V2 (cf. ADR PLAN-FIX-BANQUE-V2).</summary>
public enum TypeResultatDepot
{
    Confirme,         // OR<uid> reçu : pile entièrement déposée
    PartielServeur,   // OQ<uid>|qte>0 : pile partiellement déposée, qte_restante reste serveur → retry
    PartielVide,      // OQ<uid>|qte=0 : pile vidée (équivalent OR — pas d'OR direct sur Hystoria pour certains items)
    Timeout,          // ni OR ni OQ après timeout : VRAI problème (probable rate-limit, EMO+ ignoré)
}

public readonly record struct ResultatDepot(TypeResultatDepot Type, int QteRestante);
```

**Justification** : type immutable, `record struct` zéro-alloc, lisible côté logs (`switch` exhaustif).

### Étape 2 — `AttenteResultat` dans `PiloteBanque`
**Fichier** : `Divers/Banque/PiloteBanque.cs:45`

**AVANT** (`PiloteBanque.cs:39-45`) :
```csharp
public static readonly System.Collections.Concurrent.ConcurrentDictionary<long, DateTime> EnvoyesEnAttenteOR = new();
```

**APRÈS** : ajouter en dessous (on garde `EnvoyesEnAttenteOR` un cycle pour compat — supprimé à l'étape 8).
```csharp
/// <summary>V2 — Map d'attente OR/OQ par UID. Clé = UID inventaire envoyé en EMO+.
/// Valeur = TCS résolu par <see cref="SignalerOR"/> ou <see cref="SignalerOQ"/>
/// quand le serveur répond. Le pilote crée le TCS AVANT d'envoyer l'EMO+ pour
/// éviter la race « OR arrive avant que la map soit alimentée ».</summary>
public static readonly System.Collections.Concurrent.ConcurrentDictionary<long, TaskCompletionSource<ResultatDepot>> AttenteResultat = new();
```

**Justification** : `ConcurrentDictionary` + `TaskCompletionSource` est la primitive standard pour ce pattern en .NET 8 (utilisée dans BCL `Channel<T>` etc.). `TrySetResult` est idempotent → safe contre double-signal.

### Étape 3 — `SignalerOR` / `SignalerOQ`
**Fichier** : `Divers/Banque/PiloteBanque.cs` — ajouter après `BloquerEvClient` (vers ligne 67).

**APRÈS** :
```csharp
/// <summary>V2 — Signale qu'un OR&lt;uid&gt; vient d'être reçu côté TrameJeu.
/// Retourne true si l'UID était attendu par le pilote banque (dépôt confirmé).</summary>
public static bool SignalerOR(long uid)
{
    if (AttenteResultat.TryRemove(uid, out var tcs))
    {
        tcs.TrySetResult(new ResultatDepot(TypeResultatDepot.Confirme, 0));
        return true;
    }
    return false;
}

/// <summary>V2 — Signale qu'un OQ&lt;uid&gt;|&lt;qte&gt; vient d'être reçu côté TrameJeu.
/// Retourne true si l'UID était attendu par le pilote banque (dépôt partiel ou vide).
/// Si false : c'est un OQ de loot normal (cas existant TrameJeu).</summary>
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
```

**Justification** : API statique simple, callable depuis TrameJeu sans injection. `TryRemove` + `TrySetResult` = idempotent. Retour booléen pour permettre à `OnObjetQuantite` de savoir s'il doit traiter l'OQ comme un dépôt banque ou comme un loot normal (incrémenter `NbLootsRecus` ou non).

### Étape 4 — Modif `OnObjetRetrait`
**Fichier** : `Commun/Frames/TrameJeu.cs:1177-1204`

**AVANT** :
```csharp
private void OnObjetRetrait(MessageObjetRetrait msg)
{
    var inv = _etat.Personnage.Inventaire;
    int n;
    lock (inv) { n = inv.RemoveAll(x => x.Identifiant == msg.IdentifiantObjet); }
    if (n > 0)
    {
        Journaliseur.Debogue($"[INV] -1 objet (id {msg.IdentifiantObjet}, total = {inv.Count})");
        _etat.Personnage.NotifierInventaireChange();
    }
    else
    {
        Journaliseur.Debogue($"[INV] OR pour UID {msg.IdentifiantObjet} mais déjà absent localement (suppression optimiste banque) — no-op.");
    }
    System.Threading.Interlocked.Increment(ref BotDofus.Divers.Banque.PiloteBanque.CompteurObjectRemove);
    BotDofus.Divers.Banque.PiloteBanque.EnvoyesEnAttenteOR.TryRemove(msg.IdentifiantObjet, out _);
}
```

**APRÈS** :
```csharp
private void OnObjetRetrait(MessageObjetRetrait msg)
{
    var inv = _etat.Personnage.Inventaire;
    int n;
    lock (inv) { n = inv.RemoveAll(x => x.Identifiant == msg.IdentifiantObjet); }
    if (n > 0)
    {
        Journaliseur.Debogue($"[INV] -1 objet (id {msg.IdentifiantObjet}, total = {inv.Count})");
        _etat.Personnage.NotifierInventaireChange();
    }
    else
    {
        Journaliseur.Debogue($"[INV] OR pour UID {msg.IdentifiantObjet} mais déjà absent localement — no-op (V2 le pilote ne fait plus de suppression optimiste, donc c'est rare : OAK déjà manqué ou OR doublon).");
    }
    // V2 — Signaler au PiloteBanque si l'UID est attendu (réveille le TCS).
    // Conservé pour compat : compteur global + map V1 (sera supprimé étape 8).
    System.Threading.Interlocked.Increment(ref BotDofus.Divers.Banque.PiloteBanque.CompteurObjectRemove);
    BotDofus.Divers.Banque.PiloteBanque.EnvoyesEnAttenteOR.TryRemove(msg.IdentifiantObjet, out _);
    BotDofus.Divers.Banque.PiloteBanque.SignalerOR(msg.IdentifiantObjet);
}
```

**Justification** : un seul appel supplémentaire (`SignalerOR`), pas de break-change.

### Étape 5 — Modif `OnObjetQuantite`
**Fichier** : `Commun/Frames/TrameJeu.cs:1206-1237`

**AVANT** :
```csharp
private void OnObjetQuantite(MessageObjetQuantite msg)
{
    var existant = _etat.Personnage.Inventaire.FirstOrDefault(x => x.Identifiant == msg.IdentifiantObjet);
    if (existant != null)
    {
        int delta = msg.NouvelleQuantite - existant.Quantite;
        existant.Quantite = msg.NouvelleQuantite;
        Journaliseur.Debogue($"[INV] objet {msg.IdentifiantObjet} → qty {msg.NouvelleQuantite}");
        if (delta > 0)
        {
            var nom = ...;
            Journaliseur.Info($"[ACTION] +{delta} {nom} (total {msg.NouvelleQuantite})");
            _etat.Personnage.NbLootsRecus++;
        }
        _etat.Personnage.NotifierInventaireChange();
    }
    else
    {
        Journaliseur.Avertir($"[INV] OQ inconnu : UID {msg.IdentifiantObjet} qte {msg.NouvelleQuantite} → ...");
    }
}
```

**APRÈS** :
```csharp
private void OnObjetQuantite(MessageObjetQuantite msg)
{
    // V2 — Avant toute autre logique, vérifier si l'UID est attendu par le PiloteBanque.
    // Si oui, c'est une réponse à EMO+ (dépôt partiel) — PAS un loot. On signale et
    // on évite d'incrémenter NbLootsRecus (qui sert à la détection de récolte).
    bool consommeBanque = BotDofus.Divers.Banque.PiloteBanque.SignalerOQ(
        msg.IdentifiantObjet, msg.NouvelleQuantite);
    if (consommeBanque)
    {
        // Mettre à jour la qte locale au passage (le pilote ré-injectera l'item
        // avec la nouvelle qte au pass suivant ; on garde l'inventaire cohérent).
        var inv = _etat.Personnage.Inventaire;
        lock (inv)
        {
            var existant = inv.FirstOrDefault(x => x.Identifiant == msg.IdentifiantObjet);
            if (existant != null)
            {
                existant.Quantite = msg.NouvelleQuantite;
                if (msg.NouvelleQuantite <= 0)
                    inv.RemoveAll(x => x.Identifiant == msg.IdentifiantObjet);
            }
        }
        Journaliseur.Debogue(
            $"[BANQUE-OQ] OQ partiel pour UID {msg.IdentifiantObjet} qte={msg.NouvelleQuantite} "
            + "→ consommé par PiloteBanque (pas un loot).");
        // Note : pas de NotifierInventaireChange ici, le pilote le fera en fin de pass.
        return;
    }

    // Cas loot normal (path V1 inchangé).
    var existantLoot = _etat.Personnage.Inventaire.FirstOrDefault(x => x.Identifiant == msg.IdentifiantObjet);
    if (existantLoot != null)
    {
        int delta = msg.NouvelleQuantite - existantLoot.Quantite;
        existantLoot.Quantite = msg.NouvelleQuantite;
        Journaliseur.Debogue($"[INV] objet {msg.IdentifiantObjet} → qty {msg.NouvelleQuantite}");
        if (delta > 0)
        {
            var nom = Divers.Donnees.BaseDonnees.Instance.Item(existantLoot.IdTemplate)?.Nom
                      ?? $"Item #{existantLoot.IdTemplate}";
            Journaliseur.Info($"[ACTION] +{delta} {nom} (total {msg.NouvelleQuantite})");
            _etat.Personnage.NbLootsRecus++;
        }
        _etat.Personnage.NotifierInventaireChange();
    }
    else
    {
        Journaliseur.Avertir(
            $"[INV] OQ inconnu : UID {msg.IdentifiantObjet} qte {msg.NouvelleQuantite} "
            + "→ item invisible localement (template manquant). "
            + "Désync OAK perdu (NB : V2 n'utilise plus la suppression optimiste, donc ce cas ne devrait plus apparaître après dépôt banque).");
    }
}
```

**Justification** : on intercepte AVANT le traitement loot. Si l'UID est dans la map, on évite l'incrément `NbLootsRecus` (qui fausserait la détection de récolte) ET on synchronise la qte locale pour le pass suivant. Si la qte = 0, on supprime localement (équivalent OR manuel pour le cas `PartielVide`).

### Étape 6 — Refactor `DeposerToutInterneAsync` (pattern synchrone)
**Fichier** : `Divers/Banque/PiloteBanque.cs:200-452`

**AVANT** : burst fire-and-forget de tous les items, suppression optimiste, attente compteur global OR jusqu'à 8 s, détection `[BANQUE-LOST]` à 10 s. MAX_PASSES = 1.

**APRÈS** : la phase préambule (ApS, attente ECK5, stabilisation 2.5 s) reste IDENTIQUE. À partir du `for (int pass = 1 ...)`, refonte complète :

```csharp
// === Étapes 2+3 : MULTI-PASS SYNCHRONE PAR UID (V2 — ADR PLAN-FIX-BANQUE-V2) ===
int totalDeposes = 0, totalConfirmes = 0, totalPartiels = 0, totalTimeouts = 0;
int totalRejetesSec = 0;
const int MAX_PASSES = 3;
const int TIMEOUT_PAR_ITEM_MS = 500;
var rng = System.Random.Shared;

// Construction initiale de la liste à déposer (single snapshot, MAJ par les
// résultats partiels — évite ré-évaluer la BDD entre chaque pass).
List<ObjetInventaire> aDeposerInit;
lock (_perso.Inventaire) { aDeposerInit = _perso.Inventaire.ToList(); }
var queueDepot = CalculerItemsADeposer(aDeposerInit);
Journaliseur.Info($"[BANQUE-FILTRE] {queueDepot.Count}/{aDeposerInit.Count} item(s) éligibles au dépôt initial.");

for (int pass = 1; pass <= MAX_PASSES && queueDepot.Count > 0; pass++)
{
    if (ct.IsCancellationRequested || BanqueFermeeObservee) break;

    Journaliseur.Info($"[BANQUE-FILTRE] === Pass {pass}/{MAX_PASSES} : {queueDepot.Count} item(s) à déposer ===");
    var queuePassSuivante = new List<ObjetInventaire>();
    int deposesPass = 0, confirmesPass = 0, partielsPass = 0, timeoutsPass = 0;

    foreach (var item in queueDepot)
    {
        if (ct.IsCancellationRequested) break;
        if (BanqueFermeeObservee)
        {
            Journaliseur.Avertir($"[BANQUE] EV observé pendant dépôt — arrêt immédiat (pass {pass}, "
                + $"{deposesPass} envoyés / {confirmesPass} confirmés).");
            BloquerEvClient = false;
            return true;
        }
        if (!EstAutoriseADeposer(item)) { totalRejetesSec++; continue; }
        if (item.Quantite <= 0) continue;

        // CRITIQUE : créer le TCS et l'insérer AVANT l'envoi, sinon l'OR/OQ peut
        // arriver entre l'envoi et l'insertion → SignalerOR retourne false → perdu.
        var tcs = new TaskCompletionSource<ResultatDepot>(TaskCreationOptions.RunContinuationsAsynchronously);
        AttenteResultat[item.Identifiant] = tcs;

        var paquet = $"EMO+{item.Identifiant}|{item.Quantite}";
        var infoItem = BaseDonnees.Instance.Item(item.IdTemplate);
        var nomItem = infoItem?.Nom ?? "?";
        Journaliseur.Info($"[BANQUE-DEPOT] → {paquet} « {nomItem} » (pass {pass}, qte {item.Quantite})");
        await _session.EnvoyerAuServeurAsync(paquet).ConfigureAwait(false);
        deposesPass++;

        // Attente résultat (OR / OQ) avec timeout — pattern V2.
        ResultatDepot res;
        try
        {
            res = await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(TIMEOUT_PAR_ITEM_MS), ct).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            res = new ResultatDepot(TypeResultatDepot.Timeout, item.Quantite);
            AttenteResultat.TryRemove(item.Identifiant, out _); // cleanup orphan
        }

        switch (res.Type)
        {
            case TypeResultatDepot.Confirme:
                // OR reçu — OnObjetRetrait a déjà retiré l'item localement.
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
                int deposeeEffectif = item.Quantite - res.QteRestante;
                Journaliseur.Avertir(
                    $"[BANQUE-PARTIEL] UID {item.Identifiant} dépôt partiel : "
                    + $"qte demandée {item.Quantite}, déposé {deposeeEffectif}, restant {res.QteRestante} "
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

        // Inter-item : 150 ms ± 30 % jitter (AGENT 3 V2 recommandation).
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

    // Délai inter-pass : laisser le serveur respirer (vrais retries souvent suffisants après 1.5-2s).
    await Task.Delay(rng.Next(1500, 2500), ct).ConfigureAwait(false);
    queueDepot = queuePassSuivante;
}

// Refresh UI final.
_perso.NotifierInventaireChange();

// === Étape 4 : fermer la banque (EV, canal chiffré auto) ===
BloquerEvClient = false;
Journaliseur.Info("[BANQUE-END] → EV (fermeture coffre)");
await _session.EnvoyerAuServeurAsync("EV").ConfigureAwait(false);
await Delai(ct).ConfigureAwait(false);

// Log final structuré.
Journaliseur.Info(
    $"[BANQUE-VERIFY] === Fin workflow V2 — {totalDeposes} envoyés, "
    + $"{totalConfirmes} confirmés (OR + PartielVide), {totalPartiels} partiels retry, "
    + $"{totalTimeouts} timeouts, {totalRejetesSec} refusés sécurité. "
    + $"Poids final {_perso.PourcentagePoids:F1}% ===");
return totalConfirmes > 0;
```

**Justification** : la queue de retry est construite **incrémentalement** (uniquement les `PartielServeur` / `Timeout`). Plus de re-snapshot inventaire entre passes → plus de risque de re-soumettre un UID déjà confirmé. Le TCS est inséré **avant** l'EMO+, garantissant qu'aucun OR/OQ ne peut être perdu (race-free). `TaskCreationOptions.RunContinuationsAsynchronously` évite que les handlers TrameJeu (thread réseau) exécutent la suite du pilote (qui ferait des appels réseau) sur leur propre thread → évite les deadlocks.

### Étape 7 — Multi-pass intelligent
Inclus dans étape 6 (boucle `for pass`, `queuePassSuivante` ne contient QUE les `PartielServeur` + `Timeout`). MAX_PASSES = 3 (au-delà → `[BANQUE-FAIL]`).

### Étape 8 — Retirer la suppression optimiste V1
**Fichier** : `Divers/Banque/PiloteBanque.cs:353-363`

**AVANT** : appel à `_perso.SupprimerObjetOptimiste(item.Identifiant)` + ajout dans `EnvoyesEnAttenteOR`.

**APRÈS** : supprimer ces 4 lignes (V2 n'a plus besoin de supprimer localement — l'OR réel le fait via `OnObjetRetrait`, l'OQ partiel met à jour la qte via `OnObjetQuantite`, le `PartielVide` (qte=0) supprime localement via la branche `inv.RemoveAll` dans `OnObjetQuantite`).

**Note** : on **conserve** la méthode `Personnage.SupprimerObjetOptimiste` (utile pour le cas `PartielVide` dans `OnObjetQuantite` si jamais on veut une suppression explicite — actuellement intégrée inline étape 5). On peut aussi conserver `EnvoyesEnAttenteOR` un cycle pour ne pas break la compat avec un éventuel autre consommateur (recommandation : supprimer dans un commit séparé).

**Justification** : pattern V1 (suppression locale optimiste) cause le bug `[INV] OQ inconnu` que l'AGENT 1 V2 observe sur les 24/24 LOST → c'est en réalité un OQ partiel qui tombe dans la branche « inconnu » car l'UID a été supprimé localement.

### Étape 9 — Logs structurés V2
Inclus dans étape 6. Récap :

| Tag | Sens | Étape |
|---|---|---|
| `[BANQUE-START]` | Workflow démarre | conservé V1 |
| `[BANQUE-FILTRE]` | Snapshot et items éligibles | conservé V1 + ajout queue init |
| `[BANQUE-DEPOT]` | EMO+ envoyé | conservé V1 |
| `[BANQUE-CONFIRM]` | OR reçu OU OQ qte=0 | étape 6 |
| `[BANQUE-PARTIEL]` | OQ qte>0 reçu — partiel | NOUVEAU étape 6 |
| `[BANQUE-RETRY]` | Re-soumission pass+1 | NOUVEAU étape 6 |
| `[BANQUE-TIMEOUT]` | Ni OR ni OQ après 500 ms | étape 6 |
| `[BANQUE-FAIL]` | Abandon après 3 passes | NOUVEAU étape 6 |
| `[BANQUE-OQ]` | OQ partiel consommé par pilote (debug TrameJeu) | NOUVEAU étape 5 |
| `[BANQUE-VERIFY]` | Récap fin workflow | NOUVEAU étape 6 |
| `[BANQUE-END]` | EV envoyé | conservé V1 |
| `[BANQUE-LOST]` | (V1) supprimé — remplacé par PARTIEL/TIMEOUT/FAIL |

### Étape 10 — Reset map d'attente au début du workflow
**Fichier** : `Divers/Banque/PiloteBanque.cs:211`

**AVANT** :
```csharp
EnvoyesEnAttenteOR.Clear();
```

**APRÈS** :
```csharp
EnvoyesEnAttenteOR.Clear();
// V2 — Reset map TCS : un workflow précédent qui a abort entre EMO+ et résolution
// laisserait des TCS orphelins → faux Confirme sur l'UID si le serveur ré-utilise l'UID.
foreach (var kv in AttenteResultat)
    kv.Value.TrySetResult(new ResultatDepot(TypeResultatDepot.Timeout, 0));
AttenteResultat.Clear();
```

**Justification** : drainer les TCS orphelins (les setter en Timeout silencieusement, personne n'écoute) avant de purger la map. Évite qu'un OR tardif d'un workflow précédent ne signale faussement un Confirme sur le nouveau workflow.

## Tests

### Tests unitaires (à ajouter — `BotDofus.Tests/Banque/PiloteBanqueV2Tests.cs`)
1. `SignalerOR_UID_attendu_resout_TCS_Confirme`
2. `SignalerOQ_UID_attendu_qte_zero_resout_PartielVide`
3. `SignalerOQ_UID_attendu_qte_positive_resout_PartielServeur`
4. `SignalerOR_UID_non_attendu_retourne_false_no_op`
5. `SignalerOQ_UID_non_attendu_retourne_false_no_op` (cas loot normal)
6. `ResetWorkflow_drain_TCS_orphelins_en_Timeout`
7. `Race_OR_avant_TCS_insere_perdu` — confirmer que c'est bien le cas (cf. risque), justifier insertion AVANT EMO+

### Tests manuels Johann
Cf. « Procédure de validation » ci-dessous.

## Procédure de validation

1. **Avant rebuild** : copier `logs/botdofus-*.log` courant en `logs/avant-fix-v2.log` (référence).
2. **Rebuild** : `dotnet build BotDofus.Wpf/BotDofus.Wpf.csproj -c Debug --nologo -v minimal` — fermer Luffy-bot.exe avant.
3. **Lancer Luffy-bot.exe**, connecter Beiloddurul, attendre map d'arrivée.
4. **Run banque mobile** (option « banque mobile » activée, perso à proximité d'un coffre ApS).
5. **Observer les logs en temps réel** sur la VueDashboard (onglet Console) :
   - `[BANQUE-START]` doit apparaître
   - `[BANQUE-FILTRE]` : nombre d'items éligibles
   - `[BANQUE-DEPOT]` par EMO+ envoyé
   - **Chaque EMO+ doit être suivi de** `[BANQUE-CONFIRM]` OU `[BANQUE-PARTIEL]` OU `[BANQUE-TIMEOUT]` (jamais aucun de ces 3)
   - `[BANQUE-PARTIEL]` → un `[BANQUE-RETRY]` au pass suivant
   - `[BANQUE-VERIFY]` final avec récap
6. **Vérifier le poids** : doit baisser proportionnellement aux `Confirme + PartielVide`. Pas d'écart > 5% entre attendu et observé.
7. **Vérifier l'inventaire UI** : doit refléter exactement l'inventaire serveur (pas d'items « fantômes » qui réapparaissent au prochain changement de map).
8. **Cas dégradé** : forcer un dépôt avec inventaire chargé (>50 piles). Confirmer que les 3 passes suffisent à vider (ou que `[BANQUE-FAIL]` documente clairement les rares failures).
9. **Test de robustesse** : pendant le dépôt V2, faire un Ctrl-clic banque côté client réel (envoie EV spontané) → confirmer abort propre (`[BANQUE] EV observé pendant dépôt — arrêt immédiat`) et pas de TCS orphelin.

## Décisions parallèles (choix techniques)

### TIMEOUT_PAR_ITEM_MS = 500
AGENT 1 V2 observe OR/OQ en 200-450 ms. Marge 50-100 ms suffisante. Si > 500 ms = vrai problème serveur.

### MAX_PASSES = 3
- Pass 1 = bulk normal
- Pass 2 = absorbe la majorité des PartielServeur (les `qteRestante` sont déjà 1-3 unités)
- Pass 3 = filet
- Au-delà : aucune valeur ajoutée observée (le serveur ne donnera pas plus)

### Inter-item 150 ms ± 30 %
Recommandation AGENT 3 V2 (anti-bot scoring sur variance < 50 ms). Total burst 27 items : ~4 s envoi + ~7 s attente OR/OQ = 11 s total. Comparable à V1 (~15 s avec les 8 s d'attente OR finale).

### Inter-pass 1500-2500 ms (jitter)
Laisse le temps au serveur de purger sa queue de réponses pendantes avant de re-bombarder.

### Pourquoi PAS de stabilisation ECK5 à 5 s
La pause 2.5 s post-ECK5 V1 suffit (les OQ de loots arrivés pendant ouverture sont absorbés). Recommandation AGENT 3 V2 « 5 s » jugée over-conservative — gardable en config si dégradation observée.

### Pourquoi PAS d'EV/ApS si pass 1 < 50% (AGENT 3 V2)
Avec le pattern V2 (résolution par UID, retry intelligent), un pass 1 « partiel » n'est PAS un échec — c'est le mode nominal. Pas besoin de fermer/rouvrir.

### Pourquoi PAS d'OR direct côté Hystoria (PartielVide ?)
Hypothèse à valider en run réel : Hystoria envoie OQ|0 au lieu d'OR pour certains items (peut-être ceux qui étaient en `stack > 1` initialement, le serveur préfère un événement « quantité » uniforme). Cas géré comme équivalent OR par V2 (`Confirme` + suppression locale via OnObjetQuantite). À approfondir si run montre que tous les Hystoria items sont `PartielVide` jamais `Confirme`.

## Fichiers modifiés (récap)

| Fichier | Lignes touchées | Nature |
|---|---|---|
| `Divers/Banque/PiloteBanque.cs` | 1 (header types), 45 (map TCS), 67-90 (Signaler*), 200-452 (refactor DeposerToutInterneAsync), 211 (reset TCS), 353-363 (retirer suppression optimiste) | refactor majeur + ajouts |
| `Commun/Frames/TrameJeu.cs` | 1177-1204 (`OnObjetRetrait`: +SignalerOR), 1206-1237 (`OnObjetQuantite`: branche consommeBanque) | ajouts ciblés |
| `BotDofus.Tests/Banque/PiloteBanqueV2Tests.cs` (nouveau) | tout | tests unitaires |

**Hors-scope V2 (ne pas toucher)** :
- `Personnage.SupprimerObjetOptimiste` — gardé pour cas `PartielVide` éventuel
- `MessageObjetQuantite` / `MessageObjetRetrait` — parseurs OK
- `ConfigBanque` — pas de changement config
- `CalculerItemsADeposer` / `EstAutoriseADeposer` — inchangés

## Confiance globale ADR V2 : **8.5/10**

**Justifications confiance haute** :
- Cause racine V2 **directement observée** dans les logs (24/24 + 8/8 par AGENT 1 V2) — pas une hypothèse
- Pattern `TaskCompletionSource` + `ConcurrentDictionary` est standard .NET, faible risque d'implémentation
- Modifications **chirurgicales et localisées** : 2 fichiers, ~150 LOC nettes
- Backward-compat : `EnvoyesEnAttenteOR` et `CompteurObjectRemove` conservés un cycle → pas de break d'éventuel autre consommateur
- Tests unitaires faciles à écrire (API statique testable)

**Points qui empêchent un 10/10** :
- Race théorique OR/OQ arrivant **avant** insertion TCS dans la map — mitigée par insertion AVANT envoi EMO+, mais reste une fenêtre de quelques µs entre `AttenteResultat[uid] = tcs` et `EnvoyerAuServeurAsync`. Acceptable en pratique.
- Cas `PartielVide` (OQ qte=0 sans OR) **pas encore observé en run réel** — extrapolation depuis le cas `PartielServeur`. À valider sur run.
- 0 perte assurée seulement si TIMEOUT_PAR_ITEM_MS >= temps max réel observé. Si Hystoria ralentit (charge serveur, load test), des `Timeout` faux-positifs pourraient apparaître → mitigé par 3 passes.
- Pas de monitoring long-terme du ratio `PartielServeur` / `Confirme`. Recommandé : exposer ces compteurs en stat Dashboard pour détecter dérive.

**Risque résiduel principal** : un comportement Hystoria spécifique non encore vu (ex: OQ envoyé 2× pour un même UID — un partiel puis un OR). Mitigé par idempotence de `TrySetResult` (le 2e signal serait silencieusement ignoré).
