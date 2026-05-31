# STATE-MACHINE-BANQUE

Audit lecture-seule du pilote banque pour comprendre la divergence entre cycle #1 (OK) et cycles suivants (dépôts incomplets).

Fichiers analysés :
- `Divers/Banque/PiloteBanque.cs`
- `Divers/Banque/ConfigBanque.cs`
- `Commun/Frames/TrameJeu.cs` (lignes 99-128, 1134-1226)
- `Divers/ContexteCompte.cs` (lignes 321-485)

---

## Diagramme états (ASCII)

État implicite (pas de variable d'état explicite, le pilote est en pure flow async). Représentation logique de ce que la machine traverse :

```
                            ContexteCompte.OnPersonnageMisAJour
                                       │
              (perso.PourcentagePoids >= seuilEffectif && !_banqueDeclenchee
               && !grace && !ModePassif && Combat.Etat == Inactif
               && ConfigBanque.Active && session != null)
                                       │
                                       v
                       [Trigger]  _banqueDeclenchee=true
                                  Compte.BanqueEnCours=true
                                       │
                                       v
              ┌────────────────────────────────────────────┐
              │              INACTIF                       │
              └────────────────────────────────────────────┘
                                       │  WorkflowCompletAsync(carteAvant)
                                       v
              ┌────────────────────────────────────────────┐
              │  ZAAP_VERS_BANQUE  (si !OuvertureDirecte) │
              │  side-effects: UtiliserZaapAsync           │
              └────────────────────────────────────────────┘
                                       │ DeposerToutAsync()
                                       v
              ┌────────────────────────────────────────────┐
              │  OUVERTURE_DEMANDEE                        │
              │  side-effects:                             │
              │    BanqueFermeeObservee=false              │
              │    BloquerEvClient=true                    │
              │    BanqueOuvertureObservee=false           │
              │  delai 1.5s puis envoi "ApS"               │
              └────────────────────────────────────────────┘
                                       │ loop poll 100ms
                                       │ (BanqueOuvertureObservee → ECK5 reçu)
                                       │
              ┌─── timeout 2s : abort ────► [FERMEE_SANS_DEPOT] return false
              v
              ┌────────────────────────────────────────────┐
              │  OUVERTE                                   │
              │  side-effects:                             │
              │    BanqueFermeeObservee=false (re-reset)   │
              │    Task.Delay 2.5s "stabilisation OQ"      │
              └────────────────────────────────────────────┘
                                       │  pass=1..MAX_PASSES=3
                                       v
              ┌────────────────────────────────────────────┐
              │  DEPOT_BURST  (pass N)                     │
              │  - snapshot = _perso.Inventaire.ToList()   │
              │  - aDeposer = CalculerItemsADeposer(snap)  │
              │  - if aDeposer == 0 → break loop           │
              │  - compteurOrAuDebutPass = CompteurOR      │
              │  - foreach item: EMO+<uid>|<qte>           │
              │                  + Task.Delay 300          │
              └────────────────────────────────────────────┘
                                       │  burst envoyé
                                       v
              ┌────────────────────────────────────────────┐
              │  ATTENTE_OR (8s max)                       │
              │  while CompteurOR < compteurOrAttendu      │
              │    Task.Delay 200                          │
              │    if BanqueFermeeObservee → break         │
              └────────────────────────────────────────────┘
                                       │
                          confirmes = min(deposes, orRecus)
                                       │
              ┌─── deposes==0 OR confirmes==deposes → break pass loop
              │
              ├─── sinon : Task.Delay 3000 → pass suivant (jusqu'à 3)
              v
              ┌────────────────────────────────────────────┐
              │  FERMETURE                                 │
              │  side-effects:                             │
              │    BloquerEvClient=false                   │
              │    envoi "EV"                              │
              │    Delai(1.5-3s humanisé)                  │
              └────────────────────────────────────────────┘
                                       │  return totalConfirmes > 0
                                       v
              ┌────────────────────────────────────────────┐
              │  RETOUR (à WorkflowCompletAsync)           │
              │  Pour cycleSupplementaire=1..3 :           │
              │    Task.Delay 2000                         │
              │    snapshot = _perso.Inventaire.ToList()   │
              │    reste = CalculerItemsADeposer(snap)     │
              │    if reste==0 → break                     │
              │    else → DeposerToutAsync() (RE-CYCLE)    │
              └────────────────────────────────────────────┘
                                       │
                          finally: BloquerEvClient = false
                                       v
              ┌────────────────────────────────────────────┐
              │  FIN WORKFLOW                              │
              │  finally (ContexteCompte) :                │
              │    _banqueDeclenchee = false               │
              │    Task.Delay 3000 (grâce)                 │
              │    Compte.BanqueEnCours = false            │
              └────────────────────────────────────────────┘
```

---

## Variables clés

| Nom | Type | Définie | Lue par | Écrite par | Reset au démarrage cycle ? |
|---|---|---|---|---|---|
| `CompteurObjectRemove` | `static int` | `PiloteBanque.cs:37` | `PiloteBanque.cs:281,338,344,715` (poll OR) | `TrameJeu.cs:1192` (Interlocked.Increment) | **NON** — jamais reset. Snapshot au début de chaque pass via `compteurOrAuDebutPass` (L281) |
| `BanqueFermeeObservee` | `static volatile bool` | `PiloteBanque.cs:42` | `PiloteBanque.cs:260,287,342` | `TrameJeu.cs:106` (EV reçu) | OUI : reset à false à L193 puis re-reset à L239 (après ECK5) |
| `BanqueOuvertureObservee` | `static volatile bool` | `PiloteBanque.cs:49` | `PiloteBanque.cs:220,226` | `TrameJeu.cs:119` (ECK kind=5) | OUI : reset à false à L202 (avant ApS) |
| `BloquerEvClient` | `static volatile bool` | `PiloteBanque.cs:58` | proxy SessionProxy (filtre EV) | `PiloteBanque.cs:201,293,375,183` | OUI : true à L201, false dans `finally` L183 + à L375 (avant EV) + L293 (sur arrêt) |
| `_banqueDeclenchee` | `private bool` (instance) | `ContexteCompte.cs:324` | `ContexteCompte.cs:400,412` | `ContexteCompte.cs:418,463,478` | OUI : reset à false dans `finally` L463 |
| `Compte.BanqueEnCours` | `bool` (Compte) | externe | ApiBot (bloque combat) + MoteurLua (suspend) | `ContexteCompte.cs:419,470,479` | OUI : reset à false dans `finally` L470 (après grâce 3s) |
| `_perso.Inventaire` | `List<ObjetInventaire>` | Personnage.cs | `PiloteBanque.cs:266,130` | `TrameJeu.cs` OnObjetAjout/Retrait/Quantite L1134/1177/1195 | NON (état persistant) |
| `_perso.NbLootsRecus` | `int` | Personnage.cs | (récolte boucle) | `TrameJeu.cs:1208` (OQ delta>0) | NON |
| `aDeposer` (locale) | `List<ObjetInventaire>` | L267 | foreach L282 | calculé L267 (CalculerItemsADeposer) | OUI : recalculé à chaque pass |
| `compteurOrAuDebutPass` | `int` (locale) | L281 | L335,344 | snapshot CompteurOR | OUI : recalculé à chaque pass |
| `deposes`, `confirmes`, `rejetesSec` | `int` (locales) | L276-278 | log L290-294,352 | L327,345,305 | OUI : recalculés à chaque pass |
| `_dernierChangementCarteUtc` | `DateTime` | ContexteCompte.cs:329 | L390 (grace 2s) | L136 (CarteChangee event) | NON |
| `_etat.Combat.Etat` | `EtatCombat` | EtatJeu | `ContexteCompte.cs:402,416` | TrameJeu | bascule autonome |

---

## Cycle banque #1 (premier passage)

État global avant trigger :
- `CompteurObjectRemove` = 0 (jamais incrémenté car aucun OR avant)
- `BanqueFermeeObservee` = false (jamais set)
- `BanqueOuvertureObservee` = false (jamais set)
- `_perso.Inventaire` = snapshot stable, alimenté par ASK initial (TrameJeu.cs:599-619) + OAK/OQ farm
- `Compte.BanqueEnCours` = false

Étapes :
1. **Trigger** (ContexteCompte:410) — perso plein, conditions ok, `_banqueDeclenchee=true`, `BanqueEnCours=true`, capture `carteAvant`.
2. **WorkflowCompletAsync** (PB:72) — log poids, branche `OuvertureDirecte=true` par défaut (ConfigBanque:46) → skip zaap.
3. **DeposerToutAsync → DeposerToutInterneAsync** (PB:172,187) :
   - L193 `BanqueFermeeObservee=false` (déjà false, no-op)
   - L201 `BloquerEvClient=true`
   - L202 `BanqueOuvertureObservee=false` (déjà false, no-op)
   - L210 `Task.Delay 1500`
   - L213 envoi `ApS`
4. **Attente ECK5** (PB:219-225) — TrameJeu reçoit ECK kind=5 → `BanqueOuvertureObservee=true` (TrameJeu:119). Sortie de boucle.
5. **L239** `BanqueFermeeObservee=false` (redondant)
6. **L246** `Task.Delay 2500` stabilisation OQ.
7. **Pass 1** (L257-372) :
   - L266 snapshot inventaire complet (sac : items farmés)
   - L267 `aDeposer = CalculerItemsADeposer(snapshot)` — disons N items
   - L281 `compteurOrAuDebutPass = CompteurOR = 0` (premier cycle, jamais incrémenté)
   - boucle foreach : envoie N × `EMO+<uid>|<qte>` espacés de 300ms
   - `deposes = N`
   - L336 `compteurOrAttendu = 0 + N`
   - Poll 8s. À chaque `OR` reçu, `TrameJeu.OnObjetRetrait` (L1177) → `inv.RemoveAll(...)` + `Interlocked.Increment(CompteurObjectRemove)`. Donc l'inventaire local diminue ET le compteur monte en synchro.
   - L344 `orRecus = N − 0 = N` (cas optimal). `confirmes = N`.
   - L361 `deposes==confirmes` → **break pass loop**
8. **Fermeture** (L375-378) : `BloquerEvClient=false`, envoi `EV`, délai.
9. **Retour à WorkflowCompletAsync** (L127) : boucle cycleSupplementaire 1..3 :
   - L129 `Task.Delay 2000`
   - L130 nouveau snapshot. Inventaire local = vide (tous OR reçus → tous retirés).
   - L131 `resteAdeposer = 0` → break à L132.
10. **Fin** : log final, return true. Reset `_banqueDeclenchee=false`, grâce 3s, `BanqueEnCours=false`.

**Bilan cycle 1** : compteurs résiduels après ce cycle =
- `CompteurObjectRemove = N` (≠ 0 !)
- `BanqueFermeeObservee = true` (depuis l'EV qu'on a envoyé soi-même, TrameJeu:106 met le flag à `true` à la réception)
- `BanqueOuvertureObservee = true` (depuis l'ECK5 reçu)
- `BloquerEvClient = false`

---

## Cycle banque #2 (post-combats)

Entre-temps : combats ont eu lieu. À chaque combat, en sortie (TrameJeu:147-168) :
- `Combat.Reinitialiser()`, `Personnage.NotifierInventaireChange()`.
- Les loots de combat arrivent en `OAK` (OnObjetAjout) puis `OQ` (OnObjetQuantite) → `_perso.Inventaire` se remplit à nouveau.
- **`CompteurObjectRemove` n'est PAS reset** ; il continue à monter si un objet est retiré (consommation pain, etc.).

Trigger #2 (perso à nouveau plein) :
- `_banqueDeclenchee` était reset à false en finally de cycle 1 → ok.
- Trigger refait pareil : `_banqueDeclenchee=true`, `BanqueEnCours=true`, `carteAvant=…`

1. **WorkflowCompletAsync** L74 log, skip zaap.
2. **DeposerToutAsync → DeposerToutInterneAsync** L172,187 :
   - L193 `BanqueFermeeObservee = false` ← **reset OK** (alors qu'il était true depuis le précédent EV)
   - L201 `BloquerEvClient = true`
   - L202 `BanqueOuvertureObservee = false` ← **reset OK** (alors qu'il était true)
   - L210 `Task.Delay 1500`
   - L213 envoi `ApS`
3. **Attente ECK5** (L219). Serveur Hystoria renvoie ECK5 → flag remis à true → sortie boucle ok.
4. **L239** `BanqueFermeeObservee=false` (no-op).
5. **L246** `Task.Delay 2500`.
6. **Pass 1** :
   - L266 snapshot inventaire.
   - L267 `aDeposer = CalculerItemsADeposer(snapshot)` — disons M items.
   - **L281 `compteurOrAuDebutPass = CompteurOR`** — ⚠️ **MAIS** CompteurOR vaut maintenant N (cycle précédent) + tous les OR éventuels entre-temps (consommables, etc.). Ce snapshot est local au début de cette pass — c'est correct mathématiquement.
   - foreach : envoie M × `EMO+`. `deposes = M`.
   - L336 `compteurOrAttendu = compteurOrAuDebutPass + M`.
   - Poll 8s. À chaque OR reçu, `CompteurObjectRemove++`. Quand on atteint `compteurOrAttendu`, sortie.
   - L344 `orRecus = nouveau − ancien = M (idéalement)`.

**ICI le mécanisme du compteur est OK** parce qu'il fait une différence (ligne 344 : `actuel − compteurOrAvantBurst`). Le compteur statique n'est pas un bug **direct**. **Mais** plusieurs autres points créent une divergence.

---

## DELTA entre cycle 1 et 2

| Élément | Cycle #1 | Cycle #2 | Risque |
|---|---|---|---|
| `CompteurObjectRemove` au début pass 1 | 0 | N (résiduel + consommations entre-temps) | **OK localement** (différentiel), MAIS si un OR arrive *entre* L281 (snapshot) et le 1er `EMO+`, il sera compté par erreur dans le burst, faisant croire au pilote qu'1 OR est déjà reçu alors que l'`EMO+` n'a même pas été envoyé |
| `BanqueFermeeObservee` à l'entrée | false | true (résidu cycle 1, EV propre) — reset à L193 → **ok** | OK |
| `BanqueOuvertureObservee` à l'entrée | false | true (résidu ECK5 cycle 1) — reset à L202 → **ok** | OK |
| `_perso.Inventaire` au snapshot pass 1 | calé (récolte propre, position 63 stable) | **partiellement instable** : OAK/OQ encore en cours de réception après combat. `Personnage.NotifierInventaireChange()` est appelé en sortie combat (TrameJeu:160) MAIS les OQ correctifs (qte finale d'un drop) arrivent souvent après l'OAK initial. La pause 2.5s L246 essaie de couvrir ça mais n'est pas suffisante. | **ÉLEVÉ** |
| `_dernierChangementCarteUtc` | distant | **proche** (peut tomber dans la grace 2s si on vient de zaap ou de changer de map post-combat) | trigger retardé, pas direct |
| `aDeposer` (foreach) | items entiers et propres | items dont la `Quantite` lue est l'**ancienne valeur** d'avant OQ correctif → EMO+ envoyé avec mauvaise qté → serveur rejette silencieusement → pas d'OR | **ÉLEVÉ** (et c'est exactement ce qu'évoque le commentaire du code L240-245 !) |
| Items « fantômes » (template hors [1-30000]) | aucun | possible (OQ corrompu post-OR cycle 1 qui ré-utilise un UID) | bloqué par filtre L434, OK |
| `aDeposer` capturé en boucle | snapshot stable | **certains items disparaissent en cours de foreach** : pendant la pass, OR du début arrivent → `RemoveAll` modifie `_perso.Inventaire` **sous le lock** (TrameJeu:1183) MAIS la copie `snapshot` n'est plus à jour. L'item référencé par `item.Identifiant` peut avoir été retiré dans l'inventaire local mais l'envoi `EMO+<uid>|<qte>` est déjà parti — donc côté serveur le serveur le reçoit, retire l'objet, renvoie OR… ok. **Mais** si l'utilisateur enchaîne très vite, l'UID peut être ré-utilisé pour un nouveau loot, et là l'`EstAutoriseADeposer(item)` (L300) ne re-vérifie pas le snapshot frais → décision figée. | MOYEN |

**La différence critique : entre cycle 1 et cycle 2, l'état de `_perso.Inventaire` au moment de `CalculerItemsADeposer` n'est plus stable.** En cycle 1, l'inventaire vient de l'ASK initial + récolte propre. En cycle 2+, il vient de loots combat (OAK initial avec qte=1, puis OQ correctifs qte=18). Si le pilote snapshot pendant la fenêtre OAK→OQ, il envoie `EMO+|1` alors que le serveur attend `EMO+|18`. Le serveur ignore (ou plafonne) → pas d'OR → timeout 8s → la pass suivante recalcule mais l'item est toujours là → re-envoi.

---

## Sorties prématurées identifiées

| Endroit | Condition | Impact |
|---|---|---|
| `DeposerToutInterneAsync` L222 | `ct.IsCancellationRequested` dans boucle attente ECK | break silencieux, ECK pas confirmé |
| L226 | `!BanqueOuvertureObservee` après 2s | **abort complet du workflow, return false sans EV** ; côté serveur la banque n'est pas ouverte donc ok, mais aucun retry interne |
| L259 | `ct.IsCancellationRequested` en début de pass | break pass loop |
| L260 | `BanqueFermeeObservee` en début de pass | break pass loop ; **ATTENTION** : si un `EV` résiduel arrive entre la fin du burst pass N et le début pass N+1 (lecture L260), pass N+1 ne se fait jamais |
| L268 | `aDeposer.Count == 0` | break pass loop (normal) |
| L284 | `ct.IsCancellationRequested` dans foreach | break burst |
| L287 | `BanqueFermeeObservee` dans foreach | **arrêt avec `return true`** ; les items restants ne seront pas tentés ; remet `BloquerEvClient=false` mais **PAS** d'envoi EV depuis le pilote (commenté « déjà fait » mais c'est le cas seulement si EV vient du client/serveur) |
| L300 | `!EstAutoriseADeposer(item)` | skip item silencieux (logique normale) |
| L310 | `item.Quantite <= 0` | skip item (transitoire OQ) — **PIÈGE en cycle 2** : si OAK initial qte=1 a été déjà retiré par un OQ qte=0 entre snapshot et envoi |
| L342 | `BanqueFermeeObservee` dans attente OR | break attente |
| L361 | `deposes==0 \|\| confirmes==deposes` | break pass loop → **PROBLÈME si `deposes==0` à cause de skips L300/L310** : on sort des passes alors que `aDeposer` était non vide. Aucun re-cycle, et l'item « invisible » à cause d'OAK/OQ désync reste en sac |

**Exceptions silencieusement avalées** :
- Aucun `try/catch` interne dans `DeposerToutInterneAsync` qui swallow. Les exceptions bubblent au `finally` de `DeposerToutAsync` qui remet juste `BloquerEvClient=false`.
- L'appel `WorkflowCompletAsync` est protégé par `try/catch` côté `ContexteCompte` L435/448 qui log avec `Journaliseur.Erreur` mais **ne re-déclenche pas** un nouveau workflow.

---

## Points de désynchro possibles

1. **`_perso.Inventaire` muté concurremment pendant le snapshot** — `PiloteBanque.cs:266` : `var snapshot = _perso.Inventaire.ToList();` **n'est pas dans un lock**. `TrameJeu.cs:1183` (OR handler) modifie la même liste sous `lock(inv)`. `ToList()` itère sans le lock → **`InvalidOperationException` possible** ou snapshot incohérent. Logs montreraient potentiellement un avertissement. Idem L130.

2. **`CalculerItemsADeposer` itère `inventaire` qui vient du snapshot** — ok pour ça, mais **`totalParTemplate` calcule sur snapshot** alors que `_perso.Inventaire` continue d'évoluer. Quand l'envoi `EMO+` part avec une `qte` figée du snapshot, le serveur peut ne plus matcher si entre-temps un OQ a modifié la quantité côté serveur. Cas typique cycle 2 : OAK reçu qte=1 → snapshot=1 → on envoie `EMO+|1` → pendant le sleep 300ms l'OQ qte=18 arrive → le serveur considère l'objet à qte 18, l'EMO+|1 retire 1 et il en reste 17 → l'OR n'est PAS émis (puisque l'objet existe encore avec qte 17). **Conséquence** : `confirmes < deposes` à la pass 1 → pause 3s → pass 2 voit qte=17 → envoie `EMO+|17` → OK. **Sauf que** entre pass 1 et pass 2 (3s), encore d'autres OQ peuvent arriver. Et la boucle `cycleSupplementaire` 1..3 de `WorkflowCompletAsync` est elle aussi bornée à 3.

3. **`BanqueFermeeObservee` est un singleton statique partagé** — si l'utilisateur a un échange (PNJ marchand `EV` envoyé) entre deux cycles banque, le flag est mis à true. Heureusement reset à L193, mais **uniquement en début de `DeposerToutInterneAsync`**, pas dans la boucle `cycleSupplementaire` côté `WorkflowCompletAsync`. Si un `EV` survient pendant le `Task.Delay 2000` L129 d'un cycle supplémentaire, le `DeposerToutAsync` suivant le reset → ok. **MAIS** entre cycles supplémentaires multiples, aucun reset de `CompteurObjectRemove` n'est fait → le delta reste sain (snapshot/diff), c'est correct.

4. **`OnObjetQuantite` peut avaler des OR pour des items disparus** — `TrameJeu.cs:1212-1224` : un OQ pour UID inconnu (post-OR cycle 1 puis ré-utilisation UID côté serveur) **ne fait rien**, juste un warning. L'item reste invisible côté bot → `aDeposer` ne le contient pas → pas de dépôt. **Conséquence : items partiellement déposés en cycle 1 + nouveaux loots avec UID recyclé deviennent invisibles**.

5. **Capture `carteAvant`** (`ContexteCompte.cs:429`) — au cycle 2, la carte courante peut être déjà la map banque (si `OuvertureDirecte` + perso ne bouge pas) → `carteAvant == MapBanqueId` → L149 `dejaSurMapBanque ? skip zaap : OuvertureDirecte ? skip : zaap`. Tester `RetourFarmApresDepot && carteFarmAvant.HasValue && carteFarmAvant.Value != _cfg.MapBanqueId` (L149 du workflow) — si on est en banque mobile + carteFarmAvant = map farm (différente de MapBanqueId mais le perso n'a JAMAIS bougé puisque OuvertureDirecte), on tenterait quand même un zaap retour vers la map de farm. Mais `OuvertureDirecte=true` court-circuite tout retour zaap (L145), donc ok ici.

6. **Race ECK5 / inventaire** : `BanqueOuvertureObservee` peut être déjà true à l'entrée de cycle 2 si un ECK5 résiduel est arrivé hors workflow (peu probable). Reset à L202 → ok.

7. **`_perso.NbLootsRecus++` côté OQ** (TrameJeu:1208) — décorrélé de la banque, pas un risque direct.

---

## Cause racine la plus probable depuis la SM

**Désynchronisation `_perso.Inventaire` ↔ état serveur au cycle ≥ 2 due aux OAK/OQ post-combat encore en vol.** Trois mécanismes superposés :

1. `CalculerItemsADeposer` snapshot la liste à un instant où plusieurs items ont leur `Quantite` provisoire (OAK avec qte=1 reçu, OQ correctif qte=N encore à venir). Les `EMO+|<uid>|1` envoyés sont silencieusement absorbés par le serveur (qui voit qte>1) → pas d'OR → pas de retrait local → l'item reste dans le sac. La pause 2.5s L246 atténue mais ne garantit pas, et au cycle ≥ 2 la fréquence des OAK/OQ post-combat est plus haute qu'au cycle 1.

2. La sortie de boucle multi-pass à L361 sur `confirmes == deposes` est correcte, mais si `deposes` chute à 0 parce que tous les items éligibles ont été skip à L300/L310 (race avec OR concurrents qui retirent les items pendant l'itération), on sort des passes **sans avoir tenté les items réellement en stock** — l'inventaire local au moment du snapshot suivant restera incomplet.

3. La boucle `cycleSupplementaire` 1..3 de `WorkflowCompletAsync` réutilise `DeposerToutAsync` qui réouvre un `ApS` → reset complet (`BanqueFermeeObservee`, `BanqueOuvertureObservee`, `BloquerEvClient`) → ok côté flags, mais **réutilise `_perso.Inventaire` qui est toujours désync** : OR du dépôt précédent peut arriver pendant le `Task.Delay 2000` L129, mais OQ correctif d'un nouveau loot aussi. La 2.5s de stabilisation L246 du nouveau cycle n'est toujours pas suffisante si des OQ retardés continuent d'arriver. Au final, après 3 passes × 3 cycles = jusqu'à 9 itérations, certains items « instables » restent invisibles ou désynchronisés et **ne sont jamais déposés**.

Secondairement : l'absence de `lock(_perso.Inventaire)` autour de `_perso.Inventaire.ToList()` (L130, L266) peut générer un `InvalidOperationException` qui bubble au `try/catch` ContexteCompte:448 et termine le workflow sans relancer → cycle 2 incomplet.

---
