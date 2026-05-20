# DIAG-COMBAT — log botdofus-20260520-220343

**Build testé** : commits jusqu'à `d9ea637` (inclut F.7.1+F.7.2+F.7.3 = pipeline event-based déplacement combat).
**Log** : `BotDofus.Wpf/bin/Debug/net8.0-windows/logs/botdofus-20260520-220343.log` (663 lignes).
**Période** : 2 combats consécutifs sur map #10309 (perso Beiloddurul cell 268).

---

## §1 Séquence temporelle d'un tour bot (combat 2, le seul où ça pathfind)

| Timestamp | Source | Paquet / Évent | Résultat |
|---|---|---|---|
| 22:07:58.509 | S→C | `GTM\|401770;0;140;6;3;268;;140\|-1;0;15;6;3;161;;15` | MOI cell 268 PA=6 PM=3, ennemi cell 161 |
| 22:07:58.509 | S→C | `GTS401770\|45000\|1` | Mon tour démarre |
| 22:08:00.217 | IA | `[COMBAT] >>> Mon tour : cell 268, PA=6, PM=3, ennemis=1` | délai humanisé ~1.7s |
| 22:08:00.217 | IA | `Cible : « » #-1 cell 161 (PV=15/15, dist=9)` | dist 9 = hors portée Ronce 8 |
| 22:08:00.217 | IA | rejet 3 sorts (portée < 9) | aucun cast direct |
| 22:08:00.219 | IA | `[PATHFINDING] départ 268 → arrivée 181 \| 3 pas \| dist après=6 \| chemin: 268→239→210→181` | A* OK |
| 22:08:00.219 | IA→S | `[ACTION-MV] Envoi GA001 → 'GA001gc1' (cells 268→181)` | injection cipher OK |
| 22:08:00.219 | proxy | `[REENC C→S] #21 idxProxy=7 INJECTÉ clair='GA001gc1'` | paquet rentré dans le canal '-' |
| **22:08:02.721** | IA | **`[ACTION-MV] Timeout 2500ms (pas de broadcast GA;0/1) → mode SECOURS optimiste activé`** | **pipeline event-based déclenche fallback** |
| 22:08:02.721 | IA→S | `[ACTION-MV] Envoi GKK0 (ack déplacement)` | cipher OK |
| 22:08:03.236 | IA→S | `[ACTION] Sort « Ronce » sur cell 161` puis `GA300183;161` injecté | cast aveugle |
| 22:08:03.236 | IA→S | `GKK0` | ack cast |
| 22:08:05.133 | IA→S | `Gt` (pass turn) | turn end demandé |
| 22:08:05.222 | S→C | `GTF401770` puis `GTR401770` | turn validé serveur |
| 22:08:05.508 | S→C | `GTM\|401770;0;140;6;3;**268**;;140\|-1;0;15;6;3;161;;15` | **MOI TOUJOURS CELL 268 côté serveur** |
| 22:08:08.264 | S→C | `GTM\|401770;0;139;6;3;**268**;;140\|-1;0;15;6;3;148;;15` | toujours 268, ennemi bougé à 148 |
| 22:08:10.325 | IA | `>>> Mon tour : **cell 181**, PA=6, PM=3, ennemis=1 \| Cible cell 148 dist=5` | IA croit cell 181 (optimistic update local), distance correcte par chance car cible bouge plus loin |
| 22:08:10.325 | IA→S | `GA300183;148` | cast Ronce dist 5 (ok portée 8) |
| 22:08:10.785 | IA→S | `GKK0` | ack |
| 22:08:10.887 | S→C | `GE14119;2\|401770\|0\|2;401770;Beiloddurul…` | **GE = mort ennemi, combat terminé** |

Le bot gagne le combat **sans avoir bougé d'un mm côté serveur**. Le cast aveugle aurait été possible dès le tour 1 (ennemi bouge à 148, dist Chebyshev 268↔148 ≈ 5 ≤ portée 8). L'IA aurait DÛ tuer au tour 1 sans déplacement nécessaire — sauf que au tour 1 elle voit ennemi cell 161 dist 9, hors portée, donc essaie de bouger.

---

## §2 Réponses précises aux 8 questions

1. **`[PATHFINDING]` et `[ACTION-MV]`** : OUI présents.
   - `[PATHFINDING]` : 2 occurrences (lignes 341 « AUCUNE cellule cible trouvée », 533 « départ 268 → arrivée 181 »).
   - `[ACTION-MV]` : 4 occurrences au tour 1 du combat 2 uniquement (envoi GA001 / Timeout / Envoi GKK0).
   - Combat 1 : pas de pathfinding (ligne 341 = échec « ennemi inaccessible / PM insuffisants / cells bloquées »).
   - Combat 2 tour 2 : pas de pathfinding (cast direct dist 5 portée 8 = pass through).

2. **`Mouvement CONFIRMÉ serveur`** : **JAMAIS** (0 occurrence).
   - `Mouvement PARTIEL` : **JAMAIS** (0 occurrence).
   - `Timeout 2500ms` : **1 occurrence** ligne 538.
   - Recherche grep globale sur tous les logs `botdofus-20260520-*.log` : **0 confirmation jamais**.

3. **Paquet GA001 envoyé en combat** : `GA001gc1` (1 seule fois ligne 535).
   - Decode : préfixe `GA001` + dir `g` (= 6 = NW iso) + cell `c1` (= 2*64+53 = 181). Correct pour chemin 268→181 en iso-NW.
   - Hors combat, le client réel envoie `GA001fc1` (ligne 488, dir `f`=5=W ortho, cell 181) → confirmé serveur en `chemin='adefde'` dest 196 (troncature).

4. **Broadcast `[GA0] acteur #401770 (MOI)` post-GA001 combat** : **NON, JAMAIS reçu**.
   - 5 broadcast `[GA0]` au total dans le log : 2× #401770 (lignes 279, 491 — HORS combat), 2× #-1 ennemis (lignes 369, 565 — leurs déplacements EN combat), 1× #402102 (autre joueur). **Aucun #401770 en combat.**

5. **GAS/GAF pour casts bot** : **NON, JAMAIS**.
   - Lignes 367, 370 : `GAS-1` + `GAF0|-1` (= début/fin action ENNEMI au tour 1).
   - GA300183;192 du tour 2 combat 1 (ligne 405) → réponse serveur = `As` (stats update PV) + `GE8536;192` (mort) + `OQ401770` (loot). **Pas de GAS401770**.
   - GA300183;161 (combat 2 tour 1, ligne 543) → uniquement `Info 8~11` + `As` au tour suivant.
   - GA300183;148 (combat 2 tour 2, ligne 603) → `As` + `GE` (mort).
   - **Le serveur n'émet ni GAS401770 ni GAF*|401770 pour les actions bot, mais le cast aboutit (As+GE).** Confirme ADR-002 §3.1 : pas de GAS/GAF retour pour acteur local.

6. **Cellules dans GTM** : MOI #401770 cellule **figée à 268** sur les 7 GTM (321, 355, 388, 518, 560, 584). Le bot n'a JAMAIS bougé côté serveur sur tout le log.
   - Ennemi #-1 : 102 → 192 (combat 1), 161 → 148 (combat 2). Bouge normalement.

7. **`≠proxy(décalé)`** : **17 occurrences**. Pas massivement (sur 34 REENC C→S au total, soit 50%). C'est NORMAL : chaque injection bot (GA001/GA300/GKK0/Gt) avance idxProxy sans avancer idxClient, donc tous les paquets client suivants sont décalés. **Ce n'est pas une cause de bug** — la cipher fonctionne (preuve : GA300 → GE = mort ennemi).

8. **Sorts appris** : **7 sorts** (ligne 152).
   - `[SORTS] 7 sort(s) scanné(s) : #192 niv1, #193 niv1, #195 niv1, #198 niv1, #182 niv1, #183 niv5, #200 niv1`
   - Ronce #183 niv5 portée 1-8 (correct pour `Stats(5)` du XML dyshay).
   - 3 sorts offensifs détectés (Ronce / Poison Paralysant / Larme) — cohérent.

---

## §3 Diagnostic — pourquoi le bot ne bouge PAS post-fix

**Le pipeline event-based F.7 est BIEN invoqué.** TrameJeu.cs ligne 1041-1043 appelle `PipelineDeplacementCombat.AttendreMouvementOuTimeoutAsync`. L'event `Combat.MouvementBotConfirme` est correctement câblé dans `OnActionJeu` (TrameJeu.cs ligne 538) — il se déclenche dès qu'un paquet `GA0;1;401770;...` arrive.

**Le problème n'est PAS dans le code C#.** Il est dans la chaîne réseau :

1. Bot injecte `GA001gc1` (idxProxy=7) → bytes chiffrés '-' envoyés au serveur, ré-encodés correctement (preuve : le serveur accepte les autres injections cipher du tour, ex. GA300 ligne 543 → cast aboutit avec GE/As/OQ).
2. **Le serveur Hystoria ne renvoie AUCUN broadcast GA0** confirmant le déplacement bot. Ni au bot, ni à un autre client observateur (le client réel reçoit pourtant GA0 quand l'ennemi bouge → confirme que le canal de broadcast fonctionne).
3. Pipeline timeout à 2.5s → mode SECOURS active `Personnage.CellulePosition = 181` côté C# uniquement → désync **silencieuse** entre état IA et état serveur. Cast GA300 suivant fonctionne PAR HASARD si la dist Chebyshev depuis la VRAIE position (268) reste dans la portée du sort.

**Conclusion** : le pipeline event-based diagnostique correctement l'échec (TimeoutSilencieux), mais le mode SECOURS par défaut masque le problème en mettant à jour la position locale alors que le serveur a refusé. F.7 fait son travail ; le bug est en amont — **le GA001 lui-même est silentement rejeté par le serveur Hystoria en combat**.

---

## §4 Hypothèses survivantes (avec preuves log)

| # | Hypothèse | Preuve / contre-preuve |
|---|---|---|
| H1 | **Encodage GA001 en combat ≠ hors combat (format ou alphabet)** | Hors combat le CLIENT envoie `GA001fc1` (ligne 488) et obtient un GA0 retour. En combat le BOT envoie `GA001gc1` et n'obtient rien. Mais c'est la même structure `GA001<dir><cell2>`. **Probablement OK**, sauf si la dir `g` (NW iso) est mal mappée pour ce contexte. Capture client réel en combat manquante. |
| H2 | **Chemin compressé trop court : besoin de waypoints intermédiaires** | Le chemin réel passe par 239 et 210 entre 268 et 181, mais le paquet n'encode QUE la destination finale (pas les intermédiaires) car la direction reste constante (`g`). En combat, le serveur peut exiger un encodage explicite des waypoints intermédiaires (anti-tricheur). dyshay confirme « emit par CHANGEMENT de direction » uniquement — mais SynFus_Hystoria pourrait differ. |
| H3 | **Cellule 181 hors zone combat / inaccessible** | Le GP placement (ligne 496) liste cells alliées 222→327 et ennemies 102→207. Cell 181 dans la fourchette ennemie (102-207) — peut-être **réservée camp ennemi**, donc le serveur refuse qu'un allié s'y rende. C'EST L'HYPOTHÈSE LA PLUS PROBABLE. |
| H4 | **Action préalable manquante (GAS, signature handshake)** | Pas de GAS envoyé par client avant GA001 en exploration (le client envoie directement GA001), donc en théorie non requis. |
| H5 | **Terminateur `\n\0` manquant** | Faux : commit d4ec747 force `\n\0` sur canal '-' (ligne 247 SessionProxy). Et les GA300/GKK0/Gt fonctionnent avec le même chemin réseau → terminateur OK. |
| H6 | **idxProxy collision : injection dans une fenêtre où le client envoie aussi** | À 22:08:00 le client n'envoie rien (REENC précédent #20 à 22:07:58.645). Pas de collision. Idx 7 propre. |
| H7 | **Sort Ronce niv5 PA=4, agent envoie GA001 mais a déjà 0 PA après cast précédent** | Faux : tour 1, PA=6 plein. |

**Hypothèse maître = H3** (cell 181 = cell de placement ennemi) confirmée par le GP : `bMb1cecfctcHcIcXdadp` = ennemis sur 102, 117, 132, 133, 147, 161, 162, 177, 192, 207. **181 PAS dedans mais entourée par 177 et 192 ennemis** → probablement zone interdite alliés en grille iso combat 1.29.

Élément renforçant H3 : le pathfinder filtre par `combattantsId` (cells occupées), mais ne sait pas que les cells de placement camp opposé sont interdites de traversée tant que les ennemis sont placés. C'est une RÈGLE COMBAT 1.29 héritée du Dofus officiel.

---

## §5 Fix le plus probable

**Ajouter dans `TrouverApprocheCombat` un filtre « cells de placement camp opposé »** : récupérer la liste des cells du GP placement initial (équipe ennemie) et les marquer comme `interdites` au pathfinder en plus des cells `combattantsId`. La cell 181 — entre 177 et 192 placement ennemi — sera alors rejetée, et l'A* trouvera un détour valide (ex. 268→239→210→195 ou similaire en restant sur le côté allié). En complément, garder le mode SECOURS désactivé par défaut (ou abaisser le timeout à 1500ms et logger en `[Erreur]` + abandonner le tour proprement avec `Gt`) pour éviter la désync silencieuse position locale ≠ position serveur. Capturer **un déplacement combat réel du client manuel** sur la même map (mode passif, user joue à la main, bouge dans le placement allié) pour valider que le format `GA001<dir><cell2>` est bien identique à celui injecté par le bot — ce qui confirmerait définitivement H3 vs H1/H2.
