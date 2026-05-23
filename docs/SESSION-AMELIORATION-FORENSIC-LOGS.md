# Forensic — Bug « lancement combat / monstre » (sessions 20260522)

## 1. Comptage des `[FARM] Timeout` par log

| Log (newest → oldest) | Timeouts | `cible #[neg]` | `cible #[pos] « Monstre #0 »` | Statut fix 70255d2 |
|---|---:|---:|---:|---|
| **205230** (POST-fix) | **10** | 28 | **0** | Fix EFFICACE sur "Monstre #0" |
| 193449 (PRE-fix) | **389** | 20 | 10+ (#401770) | Bug perso-master présent |
| 184503 (PRE-fix) | 27 | 43 | 14 (#401770) | Bug présent |
| 183135 (PRE-fix) | 24 | (n/a) | 0 | Autre cause |
| 181943, 175229, 160651, 155814, 155352, 155158 | 0 | – | – | – |

→ Le fix `70255d2 (perso master ciblé comme monstre)` a **éliminé** les engagements sur ID positif `#401770 « Monstre #0 » (perso 56, dist 0)` qui spammaient `GA90756;401770` en boucle. **Aucune occurrence positive dans 205230**. Confirmation OK.

## 2. NOUVEAU bug surfacé (205230, POST-fix) — loop sur groupe lointain

Pattern observé : `[FARM] cible #-490 « Boufton Blanc » cell 266 (perso 74, dist 10)` engagé **10 fois consécutives** entre `21:00:49` et `21:02:29`, chaque tentative timeout 4 s, mêmes paquets identiques :

```
21:00:49.622  [FARM] cible #-490 « Boufton Blanc » cell 266 (perso 74, dist 10) → engage direct
21:00:49.622  [INJ ->SRV '-' réenc] GA001bbzabAhbmabnbcNcdedek      ← chemin 17 cases
21:00:53.130  [INJ ->SRV '-' réenc] GA907266;-490                    ← agression
21:00:53.258  [INJ ->SRV '-' réenc] GKK0
21:00:57.271  [Avertissement] [FARM] Timeout 4000ms : combat pas démarré
21:00:59.786  [FARM] cible #-490 « Boufton Blanc » cell 266 (perso 74, dist 10) → engage direct  ← MÊME cible, perso TOUJOURS cell 74
```

Le **perso ne bouge jamais** (reste cell 74) entre les 10 tentatives malgré injection GA001 identique. Aucun paquet S→C `GA001…` confirmation (mouvement accepté), aucun GTM mettant à jour la position du héros.

### Hypothèse principale : GA001 silencieusement rejeté par le serveur

- 17 cases dans un seul chemin = trop long ? (les autres engagements négatifs réussis dans le même log faisaient 1-11 cases)
- Caractères majuscules dans l'encodage (`bbzabAhbmabnbcNcdedek` — `A` et `N`) → suspect. Le serveur Hystoria n'aime pas certains encodages dyshay.
- Le combat suivant juste après (cible #-488 cell 68 dist 3) avait marché → cell 266 = cas extrême.

### Cause secondaire candidate : timing GA907 trop tôt
Délai GA001→GA907 = ~3.5 s sur cell 266 (17 cases × ~0.33 s = 5.6 s nécessaires). **Le GA907 part 2 s trop tôt** = serveur ignore (perso pas encore arrivé).

## 3. Classement des causes racines

| Cause | Fréquence | Sévérité | Évidence |
|---|---:|---:|---|
| **A. Perso-master engagé comme monstre** (`#401770 « Monstre #0 »`) | 14+ × 193449, 14 × 184503 | CRITIQUE | Fix 70255d2 appliqué OK |
| **B. GA907 envoyé avant fin de déplacement long** | 10 × 205230 | HAUTE | GA907 part avant GA001 fini (cell 266 dist 10) |
| **C. Groupe déjà engagé / cell hors-portée** | Sporadique | MOYENNE | Pas d'évidence directe S→C |
| **D. `timeout déplacement` IA-HEROS** (fallback optimistic) | 70 × 205230, 325 × 193449 | BASSE | Bot continue quand même |

Aucun `INJ ->SRV '-' réenc] ÉCHEC`, aucune exception .NET, aucune déconnexion dans 205230. Pré-fix 193449 a `ObjectDisposedException [REENC C→S]` + `IOException` (lignes 36231/36658) = client Dofus relancé pendant la session.

## 4. Anomalies hors-bug-combat

- **Pathfinder hyper-long** : chemin `bbzabAhbmabnbcNcdedek` (17 cases) prend ~5.6 s. Le timeout déplacement est fixé à 2000-3000 ms IA-HEROS → fallback optimistic, mais pour le farm c'est 4000 ms et insuffisant pour distances > 6 cases.
- **Spam `[CARTE] #7804 : 0 cellule(s) interactive(s)`** : la même ligne est loggée ~50 ×/s pendant tout le farm (~95 % du log). À filtrer (level Debug, hors console).
- **Banque interleaved avec combat** : `EMO+...` (dépôt banque) part en pleine séquence GA001→GA907 (lignes 11370, 11424, 11463…) → potentiel de désync ou de pollution canal '-'. À investiguer.
- **`Info #0 :` vide** émis systématiquement après chaque GA001 — probablement Im0 truncated → indicateur que serveur reçoit, mais rejette silencieusement la trajectoire.

## 5. Top 3 bugs à fixer (frequency × severity)

### 1. **GA907 timing pour déplacements longs** (NOUVEAU, POST-fix)
- Symptôme : cell distante (≥10 cases), GA907 part avant fin de GA001, serveur ignore → loop infini.
- Fix : calculer dynamiquement `delayGA907 = nbCases * ~350 ms + 300 ms safety`, OU attendre confirmation S→C GA001 avant GA907.
- Évidence : log 205230 lignes 11357–11897, target #-490 cell 266 perso 74 spam 10×.

### 2. **Pathfinder retourne chemin que serveur rejette silencieusement**
- Symptôme : `GA001bbzabAhbmabnbcNcdedek` (17 cases, caractères majuscules) jamais ack par serveur ; perso reste cell 74.
- Fix : valider l'encodage GA001 vs format dyshay (uniquement minuscules a-h pour directions ?), capper longueur à 8-10 cases max, splitter si plus long.
- Évidence : aucune confirmation S→C ni mise à jour position après les 10 GA001 identiques.

### 3. **Re-targeting du même groupe inaccessible**
- Symptôme : après timeout, le même groupe #-490 est re-sélectionné instantanément (10× consécutifs).
- Fix : blacklist temporaire (30-60 s) du groupe après N timeouts consécutifs (déjà partiellement traité par GA907 timing dans 70255d2 ? À revérifier).
- Évidence : log 205230 lignes 11402, 11447, 11490, 11523, 11558, …
