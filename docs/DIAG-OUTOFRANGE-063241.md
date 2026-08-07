# DIAG — Cast hors portée (log 063241)

**Log** : `BotDofus.Wpf/bin/Debug/net8.0-windows/logs/botdofus-20260521-063241.log`
**Symptôme** : Dofus affiche `« Impossible de lancer ce sort : vous avez une portée de 1 à 8 et vous visez à 12 »` à partir de 06:35:35.
**Combats joués** : **3** (terminés 06:34:51, 06:36:20, 06:36:36). Tous probablement gagnés (`[COMBAT] Combat terminé` sans erreur), aucun `GAF` avec code ≠ 0 (l'unique `GAF0|401770` ligne 301 = code **0** = succès serveur côté bot ; le « 0 » est le code de réussite).

---

## §1 — Séquence du tour qui rate (tour 5 du combat #2)

| Timestamp     | Log                                                                                                            | État perso (bot ↔ serveur)         |
|---------------|----------------------------------------------------------------------------------------------------------------|-------------------------------------|
| 06:35:32.289  | `[COMBAT] Tour de #401770 (tour 5) ← MOI`                                                                       | bot=222 / srv=222                   |
| 06:35:32.289  | `[TOUR-START] Tour #35 — cell 222, PA=6, PM=3, ennemis=1/3`                                                     | bot=222 / srv=222                   |
| 06:35:33.769  | `[DECIDEUR] rejet règle #183 'Ronce' : dist 10 > porteeMax 8` (×2)                                              | MoteurReglesCombat → null           |
| 06:35:33.769  | `[COMBAT] Aucune règle SynFus en portée (2 règle(s) configurée(s)) → fallback legacy`                            | bascule fallback legacy             |
| 06:35:33.770  | `[COMBAT] Cible : «  » #-2 cell 44 (PV=19/19, dist=10)`                                                          | fallback OK                         |
| 06:35:33.772  | `[PATHFINDING] départ 222 → arrivée 177 \| 3 pas \| dist après=7 \| chemin: 222→207→192→177`                     | TrouverApprocheCombat OK            |
| 06:35:33.772  | `[ACTION-MV] Envoi GA001 → 'GA001fcX' (cells 222→177)`                                                           | GA001 envoyé                        |
| 06:35:36.278  | `[ACTION-MV] Timeout 2500ms (pas de broadcast GA;0/1) → mode SECOURS optimiste activé`                          | **bot=177 / srv=222** (désync!)     |
| 06:35:36.496  | `[ACTION] Sort « Ronce » sur cell 44 (dist supposée 7)` → `GA300183;44`                                          | serveur voit dist 222↔44 = hors 1-8 |
| 06:35:36.5xx  | (Dofus client affiche `« portée 1-8 vous visez à 12 »`)                                                          | cast REJETÉ silencieusement         |
| 06:36:19.400  | `[ACTION-MV] Position confirmée par serveur : cell 177 → 177` (broadcast tardif au tour 12)                      | resync tardif                       |

→ **Tours 6 à 11 (06:35:40 → 06:36:14)** : bot persiste à se croire en 177, recast vers `cell 88` (dist *supposée* 8, dist *réelle* 8 depuis 222 mais cell 44 dist réelle 10) → **6 tours perdus** consécutifs.

---

## §2 — Code path précis (`Commun/Frames/TrameJeu.cs`)

`JouerTourCombatAsync` (lignes 893-1160) :

| Ligne(s)  | Branche                                                                                                 |
|-----------|----------------------------------------------------------------------------------------------------------|
| 930-952   | Moteur SynFus (`MoteurReglesCombat.Evaluer`). Retourne `null` si toutes règles hors portée (cas réel).  |
| 951       | Log `Aucune règle SynFus en portée → fallback legacy` ✅ atteint                                         |
| 954-959   | Calcule `ennemi` le plus proche (Chebyshev) + log `[COMBAT] Cible :`                                    |
| 962-1009  | Cherche sort lançable SANS bouger → tous rejetés `portée X > max Y` (lignes 845-847 du log)             |
| 1013-1127 | **Bloc déplacement legacy** (sort==null && PM>0) → appelle bien `TrouverApprocheCombat` (1040)          |
| 1041-1062 | Si chemin trouvé → `GA001` envoyé puis `AttendreMouvementOuTimeoutAsync(timeoutMs=2500+nbPas*450+1000)` |
| 1084-1102 | **Branche TimeoutSilencieux** : si `ModeDeplacementOptimisteSecours==true` (DÉFAUT) → `maCell=cellArrivee` + `_etat.Personnage.CellulePosition = cellArrivee` + `deplacementValide=true` |
| 1115-1119 | Suite après pseudo-déplacement : `sort=sortVise`, `distEnnemi=distApresMove` (calculée sur position FANTÔME) |
| 1143-1159 | Envoi `GA300<id>;<cellEnnemi>` → cast aveugle hors portée serveur                                       |

→ **Le fallback legacy appelle bien `TrouverApprocheCombat` (ligne 1040). Le pathfinding fonctionne. Le mouvement réseau (`GA001`) part bien.**

---

## §3 — Maillon défaillant

**Le défaut est dans la branche `ResultatDeplacementCombat.TimeoutSilencieux` (TrameJeu.cs L1084-1102)** combinée à la valeur par défaut `ModeDeplacementOptimisteSecours = true`.

Quand le serveur ne broadcast pas `GA;0/1;<idMoi>` dans les 2500-4150ms (timeout calculé) :

1. Le code **présume** que le serveur a accepté le mouvement (ligne 1091 « cast aveugle, perso supposé cell 177 »).
2. Il **réécrit** `_etat.Personnage.CellulePosition = cellArrivee` (L1092) — état perso CORROMPU côté bot.
3. **Mais le serveur n'a pas bougé le perso** (preuve : broadcast `GA;0` arrive 45 secondes plus tard à 06:36:19.400, indiquant que le mouvement N'A JAMAIS abouti côté serveur dans la fenêtre du tour 5).
4. À partir du tour 6, `MoteurReglesCombat` calcule la distance depuis la position fantôme `177` (cible `cell 88` → dist supposée=8 ≤ portée 8 ✅) → règle ACCEPTÉE → `EnvoyerCastAsync` → cast invalide côté serveur (dist réelle 222↔88 = 8 mais 222↔44 = 10/12).
5. **Aucune resynchronisation** sur la position réelle entre les tours (l'IA ne réinterroge pas la position serveur en début de tour, elle se fie à `_etat.Personnage.CellulePosition` qu'elle a elle-même corrompue).

**Cause racine** : `ModeDeplacementOptimisteSecours = true` par défaut + corruption de l'état perso sans rollback ultérieur.

Secondaire : aucun mécanisme dans `TrameJeu.OnTourCombatRecu` / `TOUR-START` pour resync `CellulePosition` depuis le dernier `GTM` ou broadcast position du serveur.

---

## §4 — Fix précis

### Fix #1 (immédiat, 1 ligne) — désactiver le secours optimiste par défaut

**Fichier** : `Divers/Combats/IA/ConfigCombat.cs` (ou class équivalente définissant `ModeDeplacementOptimisteSecours`)

```diff
-public bool ModeDeplacementOptimisteSecours { get; set; } = true;
+public bool ModeDeplacementOptimisteSecours { get; set; } = false;   // ADR-002: cast aveugle = 6 tours perdus si GA001 silencieux (log 063241)
```

Effet immédiat : branche L1098-1100 prend le contrôle → `[ACTION-MV] Mouvement REFUSÉ silencieux` + `Gt` propre, perd 1 tour au lieu de 6.

### Fix #2 (robuste) — resync position en début de chaque tour

**Fichier** : `Commun/Frames/TrameJeu.cs` `JouerTourCombatAsync` après la ligne 906 (juste après `[COMBAT] >>> Mon tour`)

```diff
 Journaliseur.Info($"[COMBAT] >>> Mon tour : cell {maCell}, PA={perso.PA}, PM={perso.PM}, ...");
+
+// Resync position perso depuis état combat (GTM/GA broadcasts). Sans ça, un
+// SECOURS optimiste du tour N-1 laisse une position fantôme qui fait caster
+// hors portée pendant 6 tours (cf. docs/DIAG-OUTOFRANGE-063241.md).
+var moiCombat = combat.Allies.FirstOrDefault(a => a.Identifiant == _etat.Personnage.Identifiant);
+if (moiCombat != null && moiCombat.CellulePosition != maCell)
+{
+    Journaliseur.Avertir($"[TOUR-START] Resync position : bot croyait cell {maCell}, serveur dit cell {moiCombat.CellulePosition}");
+    _etat.Personnage.CellulePosition = moiCombat.CellulePosition;
+    maCell = moiCombat.CellulePosition;
+}
```

### Fix #3 (optionnel) — augmenter le timeout de mouvement combat

**Fichier** : `Commun/Frames/TrameJeu.cs` ligne 1057

Le 2500ms est trop court sur Hystoria (broadcast arrivé 45 s plus tard dans le log). Passer à 4000ms minimum + monitorer la cause (probable `GKK0` manquant en amont ou cipher index décalé sur ce segment).

---

## Conclusion

L'hypothèse user est partiellement correcte : **le fallback legacy appelle BIEN `TrouverApprocheCombat` et envoie BIEN GA001**. Le bug n'est PAS dans le pipeline pathfinder, il est dans la **gestion du timeout du broadcast de confirmation** : le mode SECOURS optimiste corrompt `CellulePosition` et aucune resync ultérieure ne corrige l'erreur → cast hors portée pendant 6+ tours.

Fix recommandé : **Fix #1 + Fix #2 combinés** (5 lignes au total).
