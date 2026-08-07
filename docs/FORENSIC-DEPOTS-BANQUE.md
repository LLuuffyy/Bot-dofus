# FORENSIC-DEPOTS-BANQUE

Analyse forensic du bug "le 1er dépôt vide tout, les suivants déposent qu'une petite partie".

## Sessions analysées

| Log | Taille | `[BANQUE]` | `Démarrage dépôt` | `cycle supplémentaire` | Dépôts multiples? |
|-----|--------|-----------|-------------------|------------------------|-------------------|
| botdofus-20260531-114624.log | 60 MB | 59502 | 32 | 21 | OUI (massif) |
| botdofus-20260530-055050.log | 144 MB | 10449 | (env. 10) | 0 | non (code legacy ?) |
| botdofus-20260529-222425.log | 56 MB | 25557 | (multi) | 39 | OUI |
| botdofus-20260529-080512.log | 29 MB | 2000 | — | — | (peu d'activité banque) |

→ Le bug est massivement présent dans les 2 logs récents (29 mai soir + 31 mai après-midi). Le log du 30 mai 5h50 a une version code antérieure (sans "cycle supplémentaire", la boucle "anti-désync" a été ajoutée entre temps).

## Pattern récurrent (la signature précise du bug)

Workflow typique observé 31/05 17:26 → 17:31 (≈5 min de boucle stérile) :

```
17:26:40  Workflow complet démarré (poids 90.5%)
17:26:44  → 65/405 RETENUS         (poids 90.5% → cible : vider)
17:27:12  Fin pass 1/3 — 8/65 OR reçus en 8000ms  (poids 87.2%)   ← 8 confirmés
17:27:15  → 57/397 RETENUS                                          ← reste 57
17:27:41  Fin pass 2/3 — 30/57 OR reçus en 8000ms (poids 66.2%)   ← 30 confirmés
17:27:44  → 27/367 RETENUS                                          ← reste 27 ⬅ bloqué
17:28:00  Fin pass 3/3 — 0/27 OR reçus en 8000ms  (poids 66.2%)   ← 0/27 !!
17:28:06  ✅ Dépôt terminé — 38 OR confirmés sur 149 EMO+ envoyés. Poids final 66.2%

17:28:08  Il reste 27 items éligibles → cycle supplémentaire 1/3 (anti-désync)
17:28:29  Fin pass 1/3 — 0/27 OR reçus en 8000ms                  ← 0/27 again
17:28:48  Fin pass 2/3 — 0/27 OR reçus en 8000ms                  ← 0/27
17:29:08  Fin pass 3/3 — 0/27 OR reçus en 8000ms                  ← 0/27
17:29:13  ✅ Dépôt terminé — 0 OR confirmés sur 81 EMO+ envoyés. Poids final 66.2%

17:29:15  cycle supplémentaire 2/3 → idem 0/27 × 3 passes
17:30:23  cycle supplémentaire 3/3 → idem 0/27 × 3 passes
17:31:28  Workflow terminé (poids final 66.2%)  ← 5 min perdues, 0 dépôt sur 243 EMO+
```

**Signature ULTRA fiable du bug** :
1. Premier `Démarrage dépôt` du workflow → poids baisse partiellement (env. 25 pts).
2. Démarrage dépôt suivants (cycles supplémentaires) → poids **strictement constant** (66.2% → 66.2% → 66.2%).
3. **Toujours exactement les MÊMES UIDs renvoyés** dans chaque pass (ex. `EMO+8134945108|295 « Slip en Cuir Moulant du Vampire »` vu **9 fois consécutives** à 17:26:44, 17:27:15, 17:27:44, 17:28:12, 17:28:32, 17:28:51, 17:29:19, 17:29:39, 17:29:58…).
4. **`0/N OR reçus en 8000ms`** — le serveur ne répond JAMAIS.

## Sessions avec dépôts multiples (cycles supplémentaires)

### Session 31/05 17:26→17:31 (workflow #1 hostile)

| Étape | Timing | Items à déposer | EMO+ envoyés | OR reçus | Poids avant→après |
|-------|--------|-----------------|--------------|----------|-------------------|
| Workflow start (dépôt #1, pass 1) | 17:26:44 | 65/405 | 65 | **8** (12 %) | 90.5 → 87.2 |
| Dépôt #1, pass 2 | 17:27:15 | 57/397 | 57 | **30** (53 %) | 87.2 → 66.2 |
| Dépôt #1, pass 3 | 17:27:44 | 27/367 | 27 | **0** | 66.2 → 66.2 |
| Cycle suppl. 1/3 pass 1 | 17:28:12 | 27/367 | 27 | **0** | 66.2 → 66.2 |
| Cycle suppl. 1/3 pass 2 | 17:28:32 | 27/367 | 27 | **0** | 66.2 → 66.2 |
| Cycle suppl. 1/3 pass 3 | 17:28:51 | 27/367 | 27 | **0** | 66.2 → 66.2 |
| Cycle suppl. 2/3 pass 1 | 17:29:19 | 27/367 | 27 | **0** | 66.2 → 66.2 |
| Cycle suppl. 2/3 pass 2 | 17:29:39 | 27/367 | 27 | **0** | 66.2 → 66.2 |
| Cycle suppl. 2/3 pass 3 | 17:29:58 | 27/367 | 27 | **0** | 66.2 → 66.2 |
| Cycle suppl. 3/3 pass 1-3 | 17:30:27-17:31:23 | 27 × 3 | 81 | **0** | 66.2 → 66.2 |

**Bilan** : 243 EMO+ envoyés pour 0 OR. Le perso est resté bloqué bras-cassés en map combat pendant 5 minutes.

### Session 31/05 17:46→17:51 (workflow #2 identique)
- Dépôt #1 pass 1: 2/46 ; pass 2: 17/44 ; pass 3: 0/27 → poids 90.0 → 88.2.
- 9 passes suivants : tous 0/27. → 81 EMO+ supplémentaires inutiles.

### Session 31/05 17:52→17:57, 17:57→18:02, 18:02→18:07, 18:07→18:12, etc.
- Pattern strictement identique : workflow démarre poids 91-93 %, drop initial 25 pts, puis 9 passes 0/27 → workflow se termine inchangé.
- Au moins **6 workflows consécutifs sur le 31/05** affichent ce profil.

### Session 29/05 23:10→23:15 (preuve indépendante)
```
23:10:56 → 35/76 RETENUS
23:11:14 Fin pass 1/3 — 5/35 OR reçus en 8000ms (poids 7.3%)
23:11:35 Fin pass 2/3 — 8/30 OR reçus en 8000ms (poids 5.1%)
23:11:53 Fin pass 3/3 — 0/22 OR reçus en 8000ms (poids 5.1%)
23:11:58 ✅ Dépôt terminé — 13/87 EMO+
23:12:00 cycle suppl. 1/3 → 0/22 × 3 passes
23:13:01 cycle suppl. 2/3 → 0/22 × 3 passes
23:14:03 cycle suppl. 3/3 → 0/22 × 3 passes
```
→ Le bug n'est PAS lié au poids résiduel : même à 5.1 % de poids, le bot essaie obstinément de déposer 22 items qui sont déjà partis serveur-side.

### Sessions où ça marche (controle positif)

Quand le 1er dépôt s'auto-termine avec `confirmes == deposes`, la boucle `cycle supplémentaire` n'a jamais besoin de tourner. Exemple `botdofus-20260530-055050.log` 01:04:34 :
```
→ 27/56 RETENUS
Fin pass 1/3 — 27/27 OR reçus en 0ms  ← tout passe d'un coup
✅ 27 OR confirmés, 0 refusés, poids 2.8%
→ 0/29 RETENUS  ← rien à reboucler, workflow se finit proprement
```

## Hypothèses pré-validées (depuis les logs)

### H1 — Cipher desync (idxProxy / canal '-') ❌ ÉCARTÉ
EMO+ est envoyé via `[INJ ->SRV '-' réenc]` à chaque fois, le proxy ne signale aucun reset cipher pendant ces séquences. Les 8 / 30 OR du début de workflow passent donc le cipher est OK. Si c'était le cipher, **rien** ne passerait, pas 8 OR sur 65 puis 30 OR sur 57.

### H2 — Filtrage côté bot rejette les items ❌ ÉCARTÉ
La log `Item RETENU` montre que les bons items (Ressources : Slip Vampire, Os Chafer, Tissu Sanguin, etc.) sont sélectionnés correctement. Le filtrage par catégorie marche : équipement non-coché filtré, ressources cochées retenues. Si c'était le filtrage, on n'enverrait pas 27 EMO+ pertinents.

### H3 — Le serveur Hystoria limite à N items/seconde ❌ ÉCARTÉ
Le délai inter-EMO+ est 300 ms = 3.3 items/sec. Sur le pass 1 du 1er dépôt, 8 OR / 65 EMO+ en 8s → serveur en répond < 1/s. Ce n'est pas la cadence d'envoi (qui est lente).

### H4 — Tous les 27 items rebouclés sont des items FANTÔMES ❌ ÉCARTÉ
Seuls 1-2 PHANTOM sont loggués par cycle. Les 27 « bloqués » sont des items réels (Slip Vampire qty 295, Os Chafer qty 2389, Tissu Sombre qty 60, etc.). Templates valides (756, 310, 2285…).

### H5 — Désync poids → workflow ne se déclenche pas ❌ ÉCARTÉ
Le workflow démarre bien (poids 90.5 % détecté). Le problème est dans l'exécution, pas le déclenchement.

### H6 — Le serveur a déjà supprimé les items mais ne renvoie pas OR de "no-op" ✅ HAUTE CONFIANCE
**C'est la cause racine la plus probable**. Preuves :
- Les MÊMES UIDs sont envoyés 9 fois dans le même workflow (vérifié pour UID `8134945108`, `8138168669`, `8138168668` — voir log lignes 374756 → 379558).
- Avant le 1er pass 1 (qui réussit partiellement), les OR `OnObjetRetrait` du serveur retirent ~38 items du sac local.
- Mais sur les 27 items restants, le serveur ne répond PAS DU TOUT — pas d'OR, pas de "vous avez déjà déposé cet objet", rien.
- Sur 8s × 9 = 72 secondes d'attente, ZÉRO paquet `OR<charId>|<uid>` pour ces 27 UIDs.
- Le code C# (`PiloteBanque.cs` ligne 282-330) renvoie les MÊMES `EMO+<uid>|<qte>` car `_perso.Inventaire` n'a pas été décrémenté (pas d'OR → pas de `OnObjetRetrait`).

**Hypothèse précise** : durant le burst pass 1 du dépôt #1, **certains EMO+ sont reçus mais "consommés" par le serveur sans qu'il envoie l'OR retour** (ex: rate limit anti-spam, file pleine, double-soumission par le timing). L'item est PHYSIQUEMENT déposé en banque, mais le bot n'en a aucune trace. Il rebatte sur sa liste interne `_perso.Inventaire` qui contient encore le fantôme.

### H7 — Bug dans `OnObjetRetrait` qui ne décrémente pas réellement ❌ ÉCARTÉ
Code vérifié `TrameJeu.cs:1177-1192` :
```csharp
n = inv.RemoveAll(x => x.Identifiant == msg.IdentifiantObjet);
```
Marche, et `CompteurObjectRemove` est incrémenté. Les passes 1/2 progressent bien quand OR arrive (8 → 30 OR effectifs).

### H8 — Le proxy droppe le OR retour ❌ NON CONFIRMÉ MAIS PEU LIKELY
Le log montre les OR aller au client réel ; or si le proxy droppait `OR`, on aurait `[INV] OQ inconnu` (warning ajouté en `TrameJeu.cs:1221`), or **zéro occurrence dans le log**.

## Indices forts (citations brutes)

### Preuve A — Même UID renvoyé 9 fois
```
374756: 17:26:44.577 [BANQUE] → EMO+8134945108|295 « Slip en Cuir Moulant du Vampire »
375826: 17:27:15.631 [BANQUE] → EMO+8134945108|295 « Slip en Cuir Moulant du Vampire »
376801: 17:27:44.259 [BANQUE] → EMO+8134945108|295 « Slip en Cuir Moulant du Vampire »
378077: 17:28:12.565 [BANQUE] → EMO+8134945108|295 « Slip en Cuir Moulant du Vampire »
378807: 17:28:32.112 [BANQUE] → EMO+8134945108|295 « Slip en Cuir Moulant du Vampire »
379557: 17:28:51.613 [BANQUE] → EMO+8134945108|295 « Slip en Cuir Moulant du Vampire »
380810: 17:29:19.538 [BANQUE] → EMO+8134945108|295 « Slip en Cuir Moulant du Vampire »
381566: 17:29:39.152 [BANQUE] → EMO+8134945108|295 « Slip en Cuir Moulant du Vampire »
...
```
Toujours `|295` (même quantité côté bot) — donc le bot n'a JAMAIS vu de décrément OQ pour cet UID après le 1er envoi.

### Preuve B — `0/27 OR reçus en 8000ms` répété
```
377110: 17:28:00 Fin pass 3/3 — 0/27 OR reçus en 8000ms (0 rejets sécurité). Poids actuel 66.2%
378369: 17:28:29 Fin pass 1/3 — 0/27 OR reçus en 8000ms (0 rejets sécurité). Poids actuel 66.2%
379117: 17:28:48 Fin pass 2/3 — 0/27 OR reçus en 8000ms (0 rejets sécurité). Poids actuel 66.2%
379846: 17:29:08 Fin pass 3/3 — 0/27 OR reçus en 8000ms (0 rejets sécurité). Poids actuel 66.2%
381125: 17:29:36 Fin pass 1/3 — 0/27 OR reçus en 8000ms (0 rejets sécurité). Poids actuel 66.2%
381871: 17:29:55 Fin pass 2/3 — 0/27 OR reçus en 8000ms (0 rejets sécurité). Poids actuel 66.2%
382617: 17:30:15 Fin pass 3/3 — 0/27 OR reçus en 8000ms (0 rejets sécurité). Poids actuel 66.2%
...
```
9 passes consécutifs avec **strictement 0 OR reçu**. Le serveur a totalement ignoré 9 × 27 = 243 EMO+ identiques.

### Preuve C — Taux de réussite dégradé pass par pass
Pour le 1er burst (workflow 17:26) :
- Pass 1 : **8/65 = 12 %** OR reçus
- Pass 2 : **30/57 = 53 %** OR reçus (les items pass 1 partiellement déposés ont libéré du débit)
- Pass 3 : **0/27 = 0 %** OR reçus

Pattern reproductible sur tous les workflows ultérieurs du log. Les 27 items "résiduels" sont systématiquement bloqués.

### Preuve D — Aucun `[INV] OQ inconnu`
0 occurrences dans le log = le proxy ne droppe pas les OQ/OR. C'est bien le serveur Hystoria qui ne répond rien.

### Preuve E — Le log 30/05 (version code antérieure) montre que sans "cycle supplémentaire", le bug est moins visible
```
botdofus-20260530-055050.log : 0 occurrence de "cycle supplémentaire"
botdofus-20260531-114624.log : 21 occurrences
```
Mais les `Fin pass 3/3 — 0/N OR reçus` apparaissent quand même dans le log 30/05 (ex. 16:36:46 → 2/24 OR reçus, 16:36:55 → 22/22). Donc la "désync serveur" existait avant ; la boucle `cycle supplémentaire` (commit récent) ne fait que prolonger le scénario d'échec en boucle stérile.

## Cause racine probable

**H6 — Désynchronisation inventaire serveur ↔ bot après burst EMO+ partiellement consommé**.

Mécanisme :
1. Le bot envoie 65 `EMO+<uid>|<qte>` en 20s (300 ms d'intervalle).
2. Le serveur Hystoria/Abrak traite **certains** EMO+ et émet l'OR retour (8 sur 65 dans le 1er pass).
3. Le serveur consomme physiquement plus d'EMO+ que le nombre d'OR émis (probablement à cause d'un buffer interne ou de la déchiffre rate-limited côté serveur).
4. Le bot, ne voyant pas d'OR pour 57 items, croit qu'ils sont encore en sac.
5. Pass 2 : il rebatte sur la même liste, certains items "vraiment encore en sac" → 30 OR. Mais 27 items sont fantômes côté bot.
6. Pass 3+ : ces 27 fantômes sont renvoyés ad nauseam. Le serveur les ignore (item déjà déposé/inexistant côté serveur) **sans émettre d'erreur**.

## Suggestion de fix (pour info, hors scope agent forensic)

3 pistes complémentaires :

1. **Optimistic remove** : après chaque EMO+ envoyé, décrémenter localement `_perso.Inventaire` (qte -= envoyée) au lieu d'attendre l'OR. Si l'OR arrive, ne rien faire de plus. Si non, l'item est de facto considéré comme déposé. ↑ vire complètement le scénario boucle stérile.

2. **Détection "0 OR consécutif" → exit early** : si une pass complète retourne `0/N OR reçus`, ne pas relancer une pass suivante avec la même liste — soit fermer la banque et ré-ouvrir (refresh inventaire), soit terminer le workflow.

3. **Ré-ouverture forcée du coffre** entre cycles : envoyer EV + ApS pour forcer un refresh complet de l'état serveur côté bot (EL renvoyé contient l'inventaire banque réel, par déduction on sait quels items sont passés).

---

**Status** : analyse terminée. Preuves brutes recueillies. Confiance H6 = 9/10.
