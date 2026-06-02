# FORENSIC V2 — log botdofus-20260601-211839.log

Période analysée : 01:15:57 → 01:16:17 (un seul cycle banque complet, 20 s).

## Q1 — V2 actif ?

**OUI**, V2 chargé et exécuté.

Tags V2 trouvés dans la fenêtre banque :
- `[BANQUE-VERIFY]` : **1** occurrence (ligne 278665 — `Fin workflow V2 — 30 envoyés, 27 confirmés (OR + PartielVide), 0 partiels retry, 3 timeouts, 0 refusés sécurité. Poids final 2,6%`)
- `[BANQUE-FAIL]` : **1** occurrence (ligne 278648 — UID 10303046122 timeout après 3 passes)
- `[BANQUE-PARTIEL]` : **0** occurrence
- `[BANQUE-OQ]` : **0** occurrence

Autres tags BANQUE pertinents (84 au total) :
- `[BANQUE-START]` : 7 (3 « Workflow complet démarré », 3 « Démarrage dépôt », 1 « ✅ ECK5 reçu »)
- `[BANQUE-DEPOT]` : 30
- `[BANQUE-CONFIRM]` : 27 + 3 lignes « Fin pass X/3 »
- `[BANQUE-TIMEOUT]` : 5 (2× ECK5 absent, 3× UID 10303046122 absent)
- `[BANQUE-RETRY]` : 2

Verdict V2 actif. Mais absence totale d'OQ (`VOCAB S→C OQ` : 2 occurrences sur 38 MB de log, dont aucun dans la fenêtre banque) → le code de gestion PartielServeur/PartielVide N'A PAS ÉTÉ EXERCÉ ce run.

## Q2 — Distribution verdicts par dépôt

Un seul dépôt complet (workflow auto déclenché à 01:15:57.249 + cycle supplémentaire + retries du Carreau).

| Dépôt # | Total envoyés | Confirmés OR | Partiels (OQ qte>0) | Timeouts | Refusés sécurité |
|---|---|---|---|---|---|
| Pass 1 | 28 | 27 | 0 | 1 (Carreau) | 0 |
| Pass 2 | 1 (retry Carreau) | 0 | 0 | 1 | 0 |
| Pass 3 | 1 (retry Carreau) | 0 | 0 | 1 (→ FAIL) | 0 |
| **Total VERIFY** | **30** | **27** | **0** | **3** | **0** |

Note : les 27 OR sont **tous** détectés via le pipeline TCS V2 (UID identifié), pas seulement via le 1er OR loggé en `VOCAB S→C OR401770|10299988597` (le seul affiché dans les logs — les 26 autres OR doivent transiter par le canal '-' déchiffré et sont absents du log VOCAB mais bien détectés par le pilote).

## Q3 — Passes utilisées

| Pass | Items | Résultat |
|---|---|---|
| 1 | 28 | 27 OR + 1 timeout Carreau d'Arbalète (UID 10303046122) |
| 2 | 1 | 1 timeout (même UID) |
| 3 | 1 | 1 timeout → BANQUE-FAIL (abandon) |

Plus 1 cycle supplémentaire WorkflowCompletAsync qui dépose 6 items (Osier, Makroute, Hache, Jeton, Baguette, Parchemin Ivoire) — tous OR — entre 01:16:07.203 et 01:16:08.542.

## Q4 — FAIL items

**UID 10303046122 — Carreau d'Arbalète — template 311 — qte 48.**

Pattern : un SEUL item failed sur le run. Le pilote l'a envoyé 3 fois (passes 1, 2, 3) :

| Pass | Envoi (REENC) | Timeout déclenché | Délai apparent | INV -1 observé |
|---|---|---|---|---|
| 1 | 01:16:07.052 idxProxy=11 | 01:16:07.084 (32 ms !) | menteur — log dit « 500 ms » | 01:16:07.168 (UID retiré du sac local) |
| 2 | 01:16:10.518 idxProxy=4 | 01:16:11.019 (501 ms — correct) | légitime | aucun |
| 3 | 01:16:13.005 idxProxy=5 | 01:16:13.505 (500 ms — correct) | légitime | aucun |

Implication : **l'item a effectivement été déposé côté serveur lors du pass 1** (l'inventaire local décrémente via un autre canal — probablement le handler `IQ` ou un OQ qte=0 non logué dans VOCAB). Les passes 2 et 3 envoient un EMO+ pour un objet qui n'existe plus dans le sac → le serveur droppe silencieusement → timeouts réels.

## Q5 — OQ partiels distribution

**Aucun OQ partiel observé dans la fenêtre banque.**

Recherche `VOCAB S→C OQ` dans tout le log : 2 occurrences seulement (hors fenêtre banque). Recherche `BANQUE-OQ` : 0. Recherche `OQ.*\|0` : 0.

→ Le pipeline V2 PartielServeur/PartielVide n'a pas été testé. Le serveur Hystoria a renvoyé OR pour les 27 succès, pas OQ.

## Q6 — Timing réel

Délai DEPOT → CONFIRM mesuré sur 27 paires (toutes pass 1, hors Carreau) :

- Médiane : ≈ 115 ms
- Min : 30 ms (UID 10303010790 — 05.881 → 05.919)
- Max : 198 ms (UID 10303002603 — 03.552 → 03.743)
- 95e centile : ~190 ms

**Le timeout 500 ms est largement suffisant** pour les dépôts normaux. Le « timeout 500 ms » loggé pour l'UID Carreau au pass 1 (en seulement 32 ms !) est un **bug de drain forcé** (cf. Q10), pas un vrai timeout réseau.

## Q7 — Cycle combat / banque

Pas de combat dans la fenêtre 01:15:57 → 01:16:17. Aucun `GTM|`, aucun `[COMBAT]`, aucun `[ACTION] +X` (loot OQ) entre les dépôts. Le perso est immobile sur map 5206 (8767 logs `[CARTE] #5206`).

Workflow déclenché **2 fois** :
1. À **01:15:57.249** (manuel ou Lua — poids 0,4% < seuil 60% → pas par `OnInventaireChange`).
2. À **01:16:01.574** par `OnInventaireChange` après le 1er OR (`📦 Poids 77,0% ≥ seuil 60% → déclenchement workflow banque`) — ce qui est **surprenant** car poids 0,4% à l'ouverture, puis 77% après retrait Sort Maîtrise. Sans doute désync poids (l'inventaire a perdu un item mais le calcul recalcule mal sur certains ELO ELE).

## Q8 — OQ inconnu

**Aucun warning `[INV] OQ inconnu` ni `OQ ignoré` dans le log.** Pas de désync inventaire observable côté handler local.

## Q9 — Symptôme final

À la fin du cycle banque (01:16:17.696) :
- `[BANQUE-END] === Workflow terminé (poids final 2,6%) ===`
- `0/34 item(s) RETENUS pour dépôt` (les 34 restants sont des équipements quête/inconnus non cochés)

Donc le **poids est tombé à 2,6%** (l'objectif est atteint). Le user dit « ça reste plein » mais les chiffres disent le contraire : 28+6+1 = **34 items déposés sur le run**, 1 seul item perdu (le Carreau qui est en réalité déposé silencieusement, juste mal détecté). L'illusion « ça reste plein » vient probablement de :
- la pléthore de logs `[BANQUE-DEPOT]/[BANQUE-CONFIRM]` qui donne l'impression de spam
- les **deux** "BANQUE-TIMEOUT ❌ Pas de ECK5 reçu 2s après ApS" (lignes 278440 et 278625) qui apparaissent au milieu et à la fin alors que tout marche
- le `[BANQUE-FAIL] UID 10303046122 timeout après 3 passes — abandon` qui donne l'impression d'un échec critique alors que l'item est déposé

## Q10 — Hypothèse V3 la plus probable

**Verdict : (d) Quelque chose désynchronise complètement l'inventaire/le workflow après le 1er OR — confiance 9/10.**

Preuves brutes :

**1. Double-trigger workflow** (preuve forte) — ligne 278342 :
```
[01:16:01.574] [Info] [BANQUE-START] === Workflow complet démarré (poids 65,0%) ===
```
Un 2e WorkflowCompletAsync démarre 4,3 s après le 1er, à la même seconde que le 1er OR de Pass 1. Probable cause : `OnInventaireChange` recalcule le poids → 77% → trigger banque. Le check `_banqueDeclenchee` dans `ContexteCompte` est censé bloquer ça, mais le poids calculé bondit étrangement de 0,4% à 77% sur ce premier OR — c'est suspect (pas de recolte récente, pas de combat).

**2. Timeout fantôme à 32 ms** (preuve mécanique) — lignes 278493-278559 :
```
[01:16:07.052] [BANQUE-DEPOT] → EMO+10303046122|48
[01:16:07.084] [BANQUE-TIMEOUT] UID 10303046122 aucune réponse en 500ms
[01:16:07.168] [INV] -1 objet (id 10303046122, total = 50)  ← item DÉPOSÉ
```
Le timeout loggé à 32 ms après l'envoi est techniquement impossible (`TimeSpan.FromMilliseconds(500)` dans WaitAsync). Explication probable : le **TCS du Carreau a été drainé par `AttenteResultat.Clear()` (ligne 261-262 PiloteBanque.cs)** déclenché par le 2e DeposerToutInterneAsync qui démarre exactement à ce moment-là (`[BANQUE-START] Démarrage dépôt — poids actuel 3,5%` à 07.084). Ce drain force `TrySetResult(Timeout)` sur les TCS « orphelins » des workflows précédents — mais le workflow 1 n'est pas mort, il est juste plus lent !

**3. Pile de WorkflowCompletAsync imbriqués** (preuve d'archi) — `[BANQUE-START] === Workflow complet démarré ===` à 57.249 ET 01.574 ; `[BANQUE-END]` à 10.592 ET 17.696. Deux workflows complets exécutés en série/chevauchés, chacun avec son ApS, son ECK5 (2 ECK5 attendus, 1 reçu, 1 timeout après 2s), son cycle supplémentaire.

**4. ECK5 timeout fictif** (preuve flow) — lignes 278440 et 278625 :
```
[01:16:05.084] [BANQUE-TIMEOUT] ❌ Pas de ECK5 reçu 2s après ApS — coffre PAS OUVERT côté serveur
[01:16:10.592] [BANQUE-TIMEOUT] ❌ Pas de ECK5 reçu 2s après ApS — coffre PAS OUVERT côté serveur
```
Le coffre EST ouvert (le ECK5 a été reçu une fois à 58.871). Les ApS suivants à 03.075 et 08.584 ne reçoivent pas de nouveau ECK5 car le serveur ne renvoie ECK5 qu'à la 1re ouverture. Le pilote du workflow 2 abort à tort (« Abort workflow, RIEN n'a été déposé ») même si les dépôts du workflow 1 continuent en parallèle.

→ **(d)** : ce n'est ni un drop serveur, ni un silence absolu, ni un mauvais stacking. C'est une **course critique** entre le workflow 1 et le workflow 2 qui partagent le champ STATIQUE `AttenteResultat` dans `PiloteBanque`.

## Recommandation V3

**Méthode 1 : SÉRIALISATION DU WORKFLOW (priorité 1)**

Ajouter un `SemaphoreSlim(1,1)` global dans `PiloteBanque` autour de `WorkflowCompletAsync` :
```csharp
private static readonly SemaphoreSlim _verrouWorkflow = new(1, 1);

public async Task<bool> WorkflowCompletAsync(int? carteFarmAvant, CancellationToken ct = default) {
    if (!await _verrouWorkflow.WaitAsync(0, ct).ConfigureAwait(false)) {
        Journaliseur.Avertir("[BANQUE] Workflow déjà en cours — skip (anti-double-trigger)");
        return false;
    }
    try { ... } finally { _verrouWorkflow.Release(); }
}
```

Bloque le 2e trigger automatique de `OnInventaireChange` (poids → 77% sur 1er OR) qui spawn un workflow concurrent. Le `_banqueDeclenchee` côté ContexteCompte est insuffisant car le 1er workflow a été lancé hors-`OnInventaireChange` (manuellement à 57.249).

**Méthode 2 : RE-SYNC INVENTAIRE POST-COMBAT (demande user)**

Le user demande un moyen de re-synchroniser l'inventaire après chaque combat. La méthode dyshay : envoyer un `Os` (= demande de Stats) ou un `ALr` (Allez-y, refresh) après GAF combat. Plus simplement : **envoyer `ApS` puis `EV` immédiatement** force le serveur à renvoyer ECK5 + OL (Object List complète) → reconstruction propre.

Préférable : un appel `bot.resync_inventaire()` exposé en API Lua qui ferme/rouvre la fenêtre échange (idempotent), à appeler en début de chaque dépôt OU au retour d'un combat. Cela évite que le poids local soit faussé (le 77% vs 0,4% du log).

**Méthode 3 : NE PAS DRAINER LES TCS DES WORKFLOWS PRÉCÉDENTS**

Au lieu de `AttenteResultat.Clear()` + `TrySetResult(Timeout)` dans `DeposerToutInterneAsync` ligne 261-263, scope `AttenteResultat` par instance de PiloteBanque (champ d'instance, pas statique). Comme ça les TCS du workflow 1 ne sont jamais touchés par le workflow 2.

**Méthode 4 : NE PAS RE-ENVOYER ApS SI BANQUE DÉJÀ OUVERTE**

Avant ApS, check `BanqueOuvertureObservee == true && !BanqueFermeeObservee`. Si déjà ouvert → skip ApS, skip ECK5 wait, skip le `BANQUE-TIMEOUT ❌ Pas de ECK5 reçu 2s`. Évite les 2 fausses alertes du log.

---

## Résumé exécutif

- V2 fonctionne **comme prévu** sur 27/28 dépôts du 1er run (96% de succès observable).
- Le seul échec apparent (Carreau d'Arbalète UID 10303046122) est en réalité **déposé côté serveur** mais mal détecté à cause d'un drain forcé du TCS par un 2e workflow concurrent.
- Le user a l'illusion que « ça reste plein » à cause du déluge de logs `[BANQUE-TIMEOUT]` et `[BANQUE-FAIL]` qui sont **techniquement faux** : poids final 2,6%, tous les items éligibles déposés.
- **Vrai bug** : `WorkflowCompletAsync` peut être appelé deux fois en concurrence (depuis manuel et depuis `OnInventaireChange`), et ces deux instances partagent l'état statique `AttenteResultat` → drain mutuel.
- **Fix V3** : SemaphoreSlim global + scope `AttenteResultat` par instance + skip ApS si déjà ouvert.
