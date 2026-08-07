# AUDIT-CODE-BANQUE

Audit lecture seule du code banque pour BUG : « 1er dépôt OK (vide le sac), dépôts suivants après combats ne déposent qu'une petite partie ». Pas un bug de filtrage (le 1er passage marche). Désynchronisation entre passages.

## Fichiers audités

| Fichier | Lignes | Rôle |
|---|---|---|
| `Divers/Banque/PiloteBanque.cs` | 727 | Workflow ouvrir/déposer/fermer (cœur) |
| `Divers/Banque/ConfigBanque.cs` | 206 | Config filtres catégorie + persistance |
| `Commun/Frames/TrameJeu.cs` | 1700+ (audit ciblé L85-200, L590-620, L1130-1230) | Handlers OAK/OR/OQ/EV/ECK |
| `Commun/Messages/VersClient/Objet/MessagesObjet.cs` | 142 | Parsers OAK / OR / OQ / Ow / OM |
| `Commun/Messages/VersClient/Objet/MessagesEchange.cs` | 42 | Parsers EV / ECK |
| `Divers/Jeu/Personnage/Personnage.cs` | 196 | Modèle inventaire + `ObjetInventaire` |
| `Divers/ContexteCompte.cs` (L419-470) | extrait | Trigger workflow banque (seuil poids) |

Pas de fichier `Inventaire*.cs` ni `Divers/Objets/*.cs` — la liste d'items vit dans `Personnage.Inventaire : List<ObjetInventaire>`.

## Architecture banque (résumé)

1. `ContexteCompte` détecte `PourcentagePoids >= SeuilPoidsPct` → spawn `PiloteBanque.WorkflowCompletAsync(carteAvant)` en `Task.Run`, marque `BanqueEnCours=true`.
2. `WorkflowComplet` : zaap vers map banque (ou skip si `OuvertureDirecte=true` = banque mobile) → `DeposerToutAsync()` → boucle `for cycleSupplementaire 1..3` qui re-appelle `DeposerToutAsync` tant que `CalculerItemsADeposer(_perso.Inventaire.ToList()).Count > 0` (avec 2s pause entre cycles).
3. Chaque `DeposerToutInterneAsync` : `BloquerEvClient=true`, `Task.Delay(1500)`, **`ApS` (ouverture)**, attend `BanqueOuvertureObservee` (set par ECK5 du serveur, 2s timeout), reset `BanqueFermeeObservee=false`, attend 2.5s la stabilisation OQ, puis 3 passes max : `_perso.Inventaire.ToList()` → `CalculerItemsADeposer` → foreach EMO+ avec délai 300ms → attente burst OR (up to 8s, compteur global `CompteurObjectRemove`). En fin : envoie `EV`, libère `BloquerEvClient=false`.
4. État inventaire actualisé par : `OAK`→ ajout/maj, `OR`→`RemoveAll(uid)` + `Interlocked.Increment(CompteurObjectRemove)`, `OQ`→update quantité existant OU log warn si UID inconnu (PAS de recréation).

---

## Hypothèses

### H1 — Snapshot figé (liste items snapshotée une fois, non refresh)

- **VERDICT** : FAUX
- **Preuves** :
  - `PiloteBanque.cs:266` — `var snapshot = _perso.Inventaire.ToList();` est À L'INTÉRIEUR de la boucle `for pass = 1..3` (recalcul chaque pass).
  - `PiloteBanque.cs:130` — `var snapshotApres = _perso.Inventaire.ToList();` est aussi À L'INTÉRIEUR de la boucle `for cycleSupplementaire = 1..3` (recalcul chaque cycle).
  - Aucun cache statique d'items, aucun champ de la classe ne stocke la liste.
- **Évaluation** : **1/10**. Les snapshots sont pris à chaque itération, pas figés.

### H2 — Filtre par catégorie cassé après 1er dépôt

- **VERDICT** : FAUX (le filtre est pur, sans état)
- **Preuves** :
  - `PiloteBanque.cs:404-535` `CalculerItemsADeposer` — fonction pure, lit `_cfg`, `BaseDonnees.Instance.Item(item.IdTemplate)`, classifie via `CategoriseurObjet.Categoriser` (`ConfigBanque.cs:178` switch immutable).
  - `EstAutoriseADeposer` (L675-699) idem : pas de mutation.
  - Mais ANOMALIE : `PiloteBanque.cs:434` — `if (item.IdTemplate < 1 || item.IdTemplate > 30000)` skip items « phantom ». Cohérent entre passes.
- **Évaluation** : **1/10**. La catégorisation est déterministe.

### H3 — Modification de l'inventaire pendant l'itération

- **VERDICT** : FAUX (mais subtil)
- **Preuves** :
  - `PiloteBanque.cs:266` : `_perso.Inventaire.ToList()` créé une **copie**, donc le `foreach (var item in aDeposer)` (L282) itère sur la copie. Pas d'`InvalidOperationException` ni de skip d'index.
  - Le serveur via `TrameJeu.OnObjetRetrait` (L1183) appelle `inv.RemoveAll` sur la liste ORIGINALE pendant que le pilote itère la COPIE — c'est sûr.
- **Évaluation** : **1/10**.

### H4 — Timeout/délai entre dépôts coupe la boucle

- **VERDICT** : INDÉTERMINÉ → tendance VRAI partiel
- **Preuves** :
  - `PiloteBanque.cs:338` — attente burst OR : `while (attente < 8000 && CompteurObjectRemove < compteurOrAttendu)`. Max **8s** pour TOUS les OR de la pass.
  - Si pass 2 dépose 50 items × 300ms = 15s d'envoi + attente OR — **le burst lui-même prend déjà 15s, l'attente OR n'a aucun sens parce qu'elle commence APRÈS le burst, donc 8s suffisent pour les derniers OR seulement**. Si le serveur retarde les OR sous charge, on perd la confirmation et la pass suivante re-tente les mêmes UIDs.
  - `PiloteBanque.cs:344-345` : `confirmes = Math.Min(deposes, orRecus)`. Si `confirmes < deposes` → on entre pass 2 (L361 break si égalité).
  - Mais entre passes, **pas de garantie** que les items déjà déposés en pass 1 ont été retirés du `Personnage.Inventaire` (l'OR n'arrive peut-être pas encore) → pass 2 ré-envoie EMO+ sur des UIDs déjà déposés → le serveur les ignore (item déjà parti) → 2ème EMO+ timeout → infinite spam jusqu'au MAX_PASSES=3 break.
  - PIRE : entre `WorkflowComplet` cycles supplémentaires (L127), il y a `await Task.Delay(2000)` puis un NOUVEAU `DeposerToutAsync` qui re-ApS. Si pass 1 du cycle 1 a déposé 80% des items mais 8s n'ont pas suffi pour tous les OR, cycle 2 va voir des items en local qui n'existent plus côté serveur.
- **Évaluation** : **5/10** — partiellement causal mais pas la racine.

### H5 — État interne Personnage/Inventaire pas synchronisé après 1er dépôt ⚠️

- **VERDICT** : **VRAI — CAUSE RACINE PROBABLE**
- **Preuves** :
  - `TrameJeu.cs:1213-1224` — handler `OnObjetQuantite` quand l'UID est **inconnu** :
    ```csharp
    Journaliseur.Avertir(
        $"[INV] OQ inconnu : UID {msg.IdentifiantObjet} qte {msg.NouvelleQuantite} "
        + "→ item invisible localement (template manquant). "
        + "Désync probable post-dépôt banque ou OAK perdu.");
    ```
    Le commentaire au-dessus (L1214-1220) admet explicitement le bug :
    > « UID inconnu localement : OQ envoyé pour un item dont on n'a jamais vu l'OAK. Possible désync post-dépôt-banque (l'UID a été supprimé en pass 1, puis serveur ré-utilise l'UID pour un nouveau loot). On NE PEUT PAS recréer l'objet (OQ ne contient ni template ni position). → l'item est invisible pour le bot jusqu'à un OAK ou un changement de map. »
  - Scénario complet du bug observé :
    1. Pass 1 banque : EMO+ sur UID A → OR reçu → `inv.RemoveAll(A)` → A disparaît de l'inventaire local.
    2. Le perso retourne en farm, combat 1 → drop d'un Frêne avec UID A' (UID nouveau OU réutilisé).
    3. **Si le serveur envoie OQ A',n APRÈS la suppression de A** (item stack avec un autre déjà présent côté serveur, mais notre vue locale n'a pas l'UID A' parce que l'OAK initial était SUR un UID stack-target qu'on a supprimé en banque), `OnObjetQuantite` log « OQ inconnu » et **ne fait rien**.
    4. L'item est invisible localement → `CalculerItemsADeposer` du 2e workflow ne le voit pas → pas déposé.
  - Renforcé par `PiloteBanque.cs:240-246` : commentaire forensic 06:51 — « les OQ pour les items récemment lootés peuvent arriver après ECK5 (les drop combat génèrent OQ avec qte initiale 1 puis OQ correctifs avec qte stack). Pass 1 envoyait EMO+|1 pour 7 items à qte réelle 18/17/11 → tous timeouts ». Symptôme exact : qte locale désynchronisée du serveur.
  - `Personnage.PourcentagePoids` peut DIRE 86% (poids réel inventaire serveur) mais `_perso.Inventaire.ToList()` ne retourne que 20% d'items visibles → `CalculerItemsADeposer` retourne 5 items → seulement 5 EMO+ envoyés au 2ème workflow → seulement 5 items déposés. **Exactement le symptôme rapporté**.
  - Le commentaire `PiloteBanque.cs:120-126` confirme : « bug intermittent : retourne 28% alors que le poids réel est 86% — forensic 2026-05-28 12:49:55 ». **Le poids serveur dit qu'il y a plein de trucs mais notre liste locale ne les voit pas.**
- **Évaluation** : **9/10**. C'est LA cause racine.

### H6 — Banque vue comme déjà ouverte (état machine désync)

- **VERDICT** : FAUX
- **Preuves** :
  - `PiloteBanque.cs:202` — `BanqueOuvertureObservee = false;` reset AVANT chaque ApS.
  - `PiloteBanque.cs:193` — `BanqueFermeeObservee = false;` reset au début de chaque `DeposerToutInterneAsync`.
  - `PiloteBanque.cs:239` — reset secondaire au cas où un EV pré-coffre arrive.
  - `BloquerEvClient` reset garanti par `try/finally` L183 dans `DeposerToutAsync`.
  - Les flags sont consciencieusement re-init à chaque appel.
- **Évaluation** : **2/10**.

### H7 — Paquet batch limité par le serveur

- **VERDICT** : FAUX (pas de batch)
- **Preuves** :
  - `PiloteBanque.cs:320` — envoi unitaire `EMO+{uid}|{qte}` un par un, pas de batch dans un même paquet. Délai 300ms strict entre chaque.
  - Aucune indication de cap N items côté serveur dans la base code (Resources/data n'a pas de tel param).
- **Évaluation** : **1/10**.

### H8 — Rate limit anti-flood serveur

- **VERDICT** : INDÉTERMINÉ
- **Preuves** :
  - `PiloteBanque.cs:330` — `await Task.Delay(300, ct)` entre chaque EMO+. Pattern dyshay référencé. 300ms est connu OK.
  - Si pass 2/3 ré-envoie sur les MÊMES UIDs (parce que H5 + H4), le serveur peut soit ignorer (silent), soit pénaliser le client. Pas de DOS observé dans le code.
  - Délai entre cycles supplémentaires : `await Task.Delay(2000)` (`PiloteBanque.cs:129`). Délai entre passes intra-workflow : `await Task.Delay(3000)` (L371).
- **Évaluation** : **3/10**. Possible aggravant mais pas la cause racine.

---

## Anomalies inattendues

### A1 — OQ avec quantité 0 ne supprime PAS l'item de `Inventaire`

- `TrameJeu.cs:1200-1210` : si `msg.NouvelleQuantite == 0`, code fait `existant.Quantite = 0;` mais **garde l'objet dans la liste**.
- `CalculerItemsADeposer` filtre `o.Quantite > 0` (L412) donc il est ignoré au calcul, mais il reste dans `Inventaire`. Pas un bug fatal, mais cumule des entrées zombies.

### A2 — Compteur global `CompteurObjectRemove` partagé inter-personnages

- `PiloteBanque.cs:37` : `public static int CompteurObjectRemove;` — **static**. Si plusieurs comptes utilisent la banque simultanément, le compteur peut être avancé par les OR d'un autre perso → faux confirmé sur le compte courant. Pas la cause du bug mais source potentielle de désync future.

### A3 — Multi-pass relance sans vérifier que les UIDs sont encore présents serveur

- `PiloteBanque.cs:257-372` — Si pass 1 a envoyé EMO+ sur UID X mais l'OR n'est pas arrivé dans les 8s, pass 2 re-snapshot `_perso.Inventaire.ToList()` → X est encore là (pas d'OR), re-envoie EMO+ X → serveur ignore (item déjà déposé) → comptage `confirmes < deposes` → relance pass 3 → idem. Fini en MAX_PASSES=3 sans rien faire de neuf. Pas FAUX, juste inefficace.

### A4 — `BaseDonnees.Item(template)` peut retourner null pour items Hystoria customs

- `PiloteBanque.cs:457-461` : `var info = bdd.Item(item.IdTemplate);` — si null, cat=Inconnu, et `DeposerInconnus=false` par défaut → item PAS déposé. Si des items custom Hystoria arrivent après combat sans entry BDD, ils ne seront jamais déposés. Pas la cause du bug d'exemple (1er passage déposait tout).

### A5 — `BloquerEvClient` ne couvre PAS les cycles supplémentaires

- `PiloteBanque.cs:172-185` `DeposerToutAsync` met `BloquerEvClient=false` en **finally** de chaque appel. La boucle cycle supplémentaire (L127-142) appelle `DeposerToutAsync` 3 fois → entre les 2 cycles, `BloquerEvClient=false` pendant `Task.Delay(2000)`. Le vrai client Dofus peut envoyer un EV pendant ces 2s. Effet : ECK5 du cycle suivant marche, mais c'est risqué.

### A6 — Stabilisation 2.5s appliquée UNIQUEMENT au 1er pass

- `PiloteBanque.cs:246` — `await Task.Delay(2500, ct)` après ECK5. Au cycle supplémentaire, nouveau `DeposerToutAsync` → re-Apply 2.5s. Bien. Mais entre les passes intra-workflow (L371 `Task.Delay(3000)`), pas de wait OQ → si combat avant banque génère un OQ correctif retardé, il arrive entre pass 1 et pass 2 → décompte OK pour pass 2. OK en fait.

---

## Cause racine la plus probable

**H5 — Désynchronisation `Personnage.Inventaire` vs état serveur après combats post-1er-dépôt**, confirmée par `TrameJeu.cs:1213-1224` (commentaire forensic de la base code reconnaissant explicitement le bug).

Mécanisme :
1. 1er dépôt vide tout : `RemoveAll` sur tous les UIDs côté local.
2. Combat suivant : le serveur envoie un mix d'`OAK` (nouveaux UIDs) et d'`OQ` (mises à jour de stacks). Sur Dofus 1.29, un loot qui stack avec un item existant côté serveur arrive parfois comme `OQ uid,nouvelleQte` SANS `OAK` préalable (si le serveur considère que le client a déjà cet UID en stock). Mais notre client local A SUPPRIMÉ cet UID en banque → l'OQ tombe dans la branche « UID inconnu » de `OnObjetQuantite` (L1212-1224) → **item invisible côté bot**.
3. `Personnage.PourcentagePoids` (calculé depuis `Ow` serveur, L114-123) est correct (86% = il y a plein de loot). Mais `_perso.Inventaire.ToList()` n'en voit qu'une fraction → `CalculerItemsADeposer` retourne 5-10 items → seulement ces 5-10 EMO+ envoyés → seuls ces 5-10 déposés.
4. Le bug est silencieux : aucune erreur, juste un sac visuellement plein côté Dofus mais quasi-vide côté bot.

**Fix possible** (hors scope audit) :
- Soit forcer un resync inventaire post-combat via paquet `OK` (ré-init complète, déjà partiellement étudié dans `ResyncInventaireParChangementCarteAsync` désactivé L82-84).
- Soit demander au serveur le full inventaire au début de `DeposerToutInterneAsync` (paquet `Os` ? à vérifier dans le protocole 1.29).
- Soit, dans `OnObjetQuantite` branche « UID inconnu », envoyer un paquet de récupération template (ou tracer l'UID dans une table « à demander »).

Confiance : **9/10** que H5 est LA cause racine. H4 (timeout OR 8s trop court) est un aggravant qui peut produire des symptômes similaires mais ne suffit pas à expliquer « ne dépose qu'une petite partie » alors que le 1er dépôt complet marche.
