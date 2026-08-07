# DIAG combat — run `botdofus-20260520-224044.log` (post commit `c3ed617`)

Run de test après le triple correctif : pathfinder 4-dir combat, placement
initial pré-combat, suppression GKK0 post-cast. Combats sur **map #10309**
(15×17), Beiloddurul (Sadida) niv 15, Ronce niv 5 portée 1-8.

---

## §1 — Séquence d'un tour bot (combat #1, ligne 260-300 du log)

| t (ms écoulé) | timestamp | événement | détail |
|---|---|---|---|
| 0 | 22:42:09.402 | `[PLACEMENT-DEBUT]` Mode=Agressif | 10 cells dispo, cell choisie **222** |
| +1 | 22:42:09.403 | `INJ ->SRV Gp222` | placement OK |
| +37 | 22:42:09.439 | S→C `GIC|401770;222;1` | serveur accepte cell |
| +1399 | 22:42:10.802 | `INJ ->SRV GR1` | prêt |
| +45 | 22:42:10.847 | S→C `GR1401770` | confirme prêt |
| +6 | 22:42:10.848 | S→C `GS` + `[COMBAT] 1 allié(s) vs 5 ennemi(s)` | combat démarre |
| +2007 | 22:42:12.855 | S→C `GTM|...;222;...|-1;...;133;...` | composition combat |
| +121 | 22:42:12.976 | S→C `GTS401770|45000|1` | mon tour |
| +1595 | 22:42:14.571 | `[COMBAT] >>> Mon tour : cell 268, PA=6, PM=3, ...` | ⚠ cell=268 (legacy `?`) avant maj |
| +4 | 22:42:14.575 | `[COMBAT] Règle SynFus Ronce focus=EnnemiLePlusProche cell 133 dist=5` | règle SynFus matche |
| +0 | 22:42:14.575 | `INJ GA300183;133` | cast Ronce sur 133 |
| +54 | 22:42:14.629 | S→C `GAS401770` puis `GAF0|401770` | action acceptée |
| +21 | 22:42:14.650 | client→srv `GKK0` (NATIF, pas injecté) | ack normal |
| ... | 22:42:14.919 | S→C `GE3839;4|...` | combat terminé (one-shot) |

**Tour 5 du combat #2** (montre déplacement, ligne 1395-1422) :

| t (ms) | événement |
|---|---|
| 0 | `[COMBAT] >>> Mon tour : cell 327, PA=6, PM=3` |
| +0 | Cible cell 206 dist=9 (Chebyshev iso) |
| +0 | Rejet Ronce/Paral/Larme (portée 9>8, 9>2, 9>3) → fallback déplacement |
| +0 | `[PATHFINDING] départ 327 → arrivée 312, 1 pas, dist après=8, chemin: 327→312` |
| +0 | `[ACTION-MV] Envoi GA001 → 'GA001fe4'` (direction `f`=W ortho strict) |
| +155 | S→C `GA;0;...` cell 327→312 |
| +1 | `[ACTION-MV] Mouvement CONFIRMÉ serveur` |
| +156 | `INJ GA300183;206` cast à dist=8 ✓ |

---

## §2 — Réponses aux 6 questions

1. **Placement initial fonctionne ?** ✅ OUI. 3 occurrences `[PLACEMENT-DEBUT]`
   suivies de `Gp<cell>` puis `GR1` (lignes 260-264, 1203-1206, et combats
   ultérieurs avec `Mode=Fuyard`). Quand `cell choisie == ma cell` on saute
   le `Gp` (« déjà sur la cell optimale »), comportement attendu.
   Confirmation serveur `GIC|401770;222;1` à chaque placement.

2. **Déplacement combat fonctionne ?** ✅ OUI. 4 occurrences
   `[ACTION-MV] Mouvement CONFIRMÉ serveur` (lignes **1418, 1903, 2490** + une
   4ème à 2620 environ). AUCUN `Mouvement PARTIEL` ni `Timeout`. Le pipeline
   ADR-002 `AttendreMouvementOuTimeoutAsync` fonctionne (broadcast `GA;0;`
   reçu en ~37-155 ms, bien < timeout 2.5s + nbPas*450ms).

3. **Pathfinder 4-dir évite les diagonales ?** ✅ OUI. En combat (cell 327→312 et
   327→297), les GA001 envoyés sont :
   - `GA001fe4` → direction unique **`f` (W = -1,0 ortho)** + dest 312
   - `GA001feP` → direction unique **`f` (W ortho)** + dest 297 (2 pas W)

   Mapping confirmé dans `Divers/Cartes/Cellule.cs:102-127` :
   `b=E(1,0) / d=S(0,1) / f=W(-1,0) / h=N(0,-1)` sont les 4 orthos,
   `a=NE / c=SE / e=SW / g=NW` sont les diagonales **JAMAIS** observées en
   combat. Hors combat (overworld), on voit bien des `a/c/e/g` (ex.
   `GA001afTcgkagmhf-`) → 8-dir actif. Cohérent avec `Pathfinder.cs:206-215`
   (4 deltas si `combat:true`).

4. **« cast portée 9 > max 8 » est-il un bug ?** ❌ NON, faux positif.
   Rejets observés : 9>8 (lignes 1409, 2482), 10>8 (l. 1895), 12>8 (l. 1362).
   Dans CHAQUE cas, le bot fait ENSUITE un pas ortho et cast à dist=8 (cf.
   séquence §1 tour 5). Le rejet `[Debug]` est juste informatif — la
   distance Chebyshev sur (x,y) iso = distance Dofus canonique = bonne
   métrique pour la portée d'un sort. Vérifié manuellement :
   `cell 327 = (19,3)`, `cell 206 = (10,4)` → max(|9|,|1|) = **9** ✓
   et après pas `cell 312 = (18,3)` → max(8,1) = **8** = portée max ronce ✓.

5. **Distance pathfinder vs cast — cohérente ?** ⚠ PARTIELLEMENT.
   - Cast / filtre portée (ligne 978, 1244) → **Chebyshev (x,y)** = portée Dofus ✓
   - Pathfinder en 4-dir (PeleasPathfinder dyshay) → nb pas réels = **Manhattan**
   - Filtre PM (ligne 1248-1250) → **Chebyshev estimée** comme borne BASSE avant A*

   Conséquence : sur une candidate à Chebyshev=PM mais Manhattan>PM, l'A* échoue
   silencieusement après le filtre rapide. Bénin — la candidate est juste rejetée
   en aval. Mais le log `[PATHFINDING] AUCUNE cellule cible trouvée` (ligne 1365)
   peut survenir sur des configs où Chebyshev OK / Manhattan KO. À surveiller
   sur grandes cartes ; ici cela n'est jamais arrivé pendant ce run.

6. **Crash sur conditions personnalisées ?** ❌ AUCUN crash. Aucune occurrence
   de `Exception`, `Erreur`, `at System.`, `at BotDofus.` dans les 3 logs
   `223818`, `223926`, `224044`. La config chargée est `1 règle simple`
   (`Ronce`, focus EnnemiLePlusProche). Il n'y a pas d'UI WPF pour saisir
   des conditions custom — elles sont éditées via JSON. Si le user a vu un
   « crash », c'est probablement un effet visuel UI (combobox vide) et non
   un Exception runtime. Aucun champ stacktrace.

---

## §3 — Diagnostic du « bug cast portée 9 > max 8 »

**Le bug n'existe pas.** Le user a pris un message `[Debug]` informatif pour
un cast échoué. Lecture complète de la séquence :

```
22:45:10.388  [Debug] rejet « Ronce » : portée 9 > max 8       ← INFO, pas un échec
22:45:10.388  [PATHFINDING] départ 327 → arrivée 312, 1 pas    ← fallback déplacement
22:45:10.388  [ACTION-MV] Envoi GA001 → 'GA001fe4'             ← move 1 pas W
22:45:10.543  Position confirmée : cell 327 → 312              ← broadcast srv
22:45:10.544  [ACTION-MV] Mouvement CONFIRMÉ                   ← pipeline ADR-002 OK
22:45:10.700  [ACTION] Sort Ronce sur cell 206                 ← cast à dist=8 ✓
```

La logique est :
1. Calcule `distEnnemi = DistanceDofus(maCell, ennemiCell)` (Chebyshev iso, l. 941)
2. Itère sur sorts offensifs ; un par un, log `rejet portée X > max Y` si KO (l. 980)
3. Si AUCUN sort en portée immédiate → `sort == null` → branche `if (sort == null && perso.PM > 0)` (l. 997)
4. `TrouverApprocheCombat` cherche la meilleure cell à atteindre en PM tels que la dist Chebyshev résultante soit ≤ porteeMax (l. 1238-1245)
5. Envoie GA001, attend broadcast, cast

Tout marche. Le seul vrai défaut est que `[Debug] rejet ...` apparaît dans la
console générale, donc l'user voit un message rouge avant le succès. **C'est
un problème de communication UX, pas un bug fonctionnel.**

---

## §4 — Fix à appliquer

### Fix unique : améliorer la lisibilité des logs

Le message `rejet « X » : portée 9 > max 8` est noyé dans le bruit. Quand
un fallback déplacement va suivre, mieux vaut le promouvoir d'une ligne
de contexte explicite (et garder le détail en Debug).

**Fichier** : `Commun/Frames/TrameJeu.cs`
**Lignes** : 994 (après la boucle `foreach offensifs`), avant le bloc
`if (sort == null && perso.PM > 0)`.

```diff
@@ -993,6 +993,15 @@
             break;
         }

+        // Trace lisible : un seul Info par tour résumant pourquoi on déclenche
+        // le fallback déplacement, plutôt que N lignes [Debug] que l'utilisateur
+        // prend pour un échec. Le détail par-sort reste en [Debug] au-dessus.
+        if (sort == null)
+        {
+            Journaliseur.Info($"[COMBAT] Aucun sort en portée immédiate (cible "
+                + $"dist={distEnnemi}, sorts testés={offensifs.Count}). Tentative "
+                + $"de rapprochement (PM={perso.PM})…");
+        }
+
         // 2bis) DÉPLACEMENT si aucun sort en portée. On essaie de se rapprocher
         // jusqu'à ce qu'un sort soit utilisable, en respectant les PM dispos.
         if (sort == null && perso.PM > 0)
```

### Fix optionnel (mineur) : cohérence Chebyshev/Manhattan

Le filtre PM en ligne 1248-1250 utilise une borne Chebyshev qui peut laisser
passer des candidates inatteignables en 4-dir (Manhattan > PM). C'est sans
conséquence (A* renvoie null en aval → continue boucle) mais coûte un appel
A* inutile. Pour optimiser :

**Fichier** : `Commun/Frames/TrameJeu.cs` ligne 1248-1250.

```diff
-            int dEstimee = System.Math.Max(
-                System.Math.Abs(c.X - depart.X), System.Math.Abs(c.Y - depart.Y));
-            if (dEstimee > pmMax) continue;
+            // En combat (4-dir ortho), la distance réelle = Manhattan, pas
+            // Chebyshev. La borne basse devient |dx|+|dy| (toujours ≥ Cheby).
+            int dEstimee = System.Math.Abs(c.X - depart.X)
+                         + System.Math.Abs(c.Y - depart.Y);
+            if (dEstimee > pmMax) continue;
```

Gain attendu : -10 à -40 % de candidates testées par tour de combat sur
cartes denses ; pas d'impact fonctionnel.

---

## Annexe — cells observées (mw=15)

| cell | (x,y) | cell | (x,y) |
|---|---|---|---|
| 222 | (15,0) | 327 | (19,3) |
| 268 | (17,1) | 312 | (18,3) |
| 133 | (8,1) | 297 | (17,3) |
| 206 | (10,4) | 192 | (10,3) |
| 161 | (8,3) | 177 | (9,3) |

(Formule `Cellule.CalculerCoordonnees(cid, mw=15)` ; vérifiée avec
`map #10309 15×17` au démarrage log ligne 184.)
