# DIAG combat — run `botdofus-20260521-014203.log`

Test nuit (01:44 → 01:47), Beiloddurul (Sadida) niv 16-17, map #10309, **1 seule règle SynFus configurée (Ronce niv5 portée 1-8, focus EnnemiLePlusProche, x1/tour, x1/cible, méthode = vide)**. Comparaison vs run précédent `224044`.

---

## §1 — Résumé exécutif

5 combats joués, **5 victoires** (GE serveur → joueur 401770 toujours du bon côté, XP > 0). 1 crash zéro, 0 stacktrace, 0 timeout. Le bot cast Ronce à chaque tour, bouge 1 seule fois sur les ~25 tours. **Régression visible** vs 224044 : la règle SynFus configurée ne matche plus dès le tour 2 (« Aucune règle SynFus en portée → fallback legacy »), alors qu'en 224044 elle matchait quasi-systématiquement. Le mode `MODE DE COMBAT` est passé à Agressif comme attendu (placement cell 222), et n'a PAS été testé en Fuyard cette nuit. **Aucun multi-cast** : 1 sort + Gt à chaque tour malgré 6 PA dispo (Ronce niv5 = 4 PA → reste 2 PA dormants).

---

## §2 — Tableau combats / tours / actions

Tag colonne **Action** : `S`=sort cast, `M`=mouvement confirmé serveur, `P`=Gt pass turn.

| # | Heure | Issue | Ennemis init | Tours bot | Pattern actions | Règle SynFus utilisée ? |
|---|-------|-------|--------------|-----------|-----------------|-------------------------|
| 1 | 01:44:25 → 01:44:38 (13s) | **Gagné** (GE `;200` kamas) | 2 | 2 | T1 `S+P` / T2 `S` (combat terminé pendant cast) → `P` | T1 OUI, T2 OUI (avant fin) |
| 2 | 01:45:01 → 01:45:28 (27s) | **Gagné** (GE `;0` kamas — XP solo : `+5644`) | 2 | 4 | T1 `S+P`, T2 `S+P`, T3 `S+P`, **T4 `M+S+P`** | T1-4 **NON** (fallback legacy) |
| 3 | 01:45:42 → 01:45:48 (6s) | **Gagné** (GE `;12`) | 1 (1 mob vivant à init) | 1 | T1 `S` (combat terminé pendant cast) → `P` | NON (fallback legacy) |
| 4 | 01:45:55 → 01:46:57 (62s) | **Gagné** (GE `;0` — XP solo `+5644`) | 3 | **10** | T1-10 tous `S+P`, jamais bougé | T1-10 **NON** (fallback legacy) |
| 5 | 01:47:33 → 01:47:46 (13s) | **Gagné** (GE `;12`) | 2 | 2 | T1 `S+P`, T2 `S` (terminé pendant cast) → `P` | T1-2 **NON** (fallback legacy) |

**Totaux** : 19 tours bot · 19 casts Ronce · 1 mouvement (combat #2 T4 cell 222→177 sur GA001 `fcX`) · 17 Gt explicites · 0 crash · 0 timeout · 0 cible fantôme

**1 seul cas où SynFus a matché** : combat #1 T1+T2 (avec config sauvegardée pré-combat).
**18/19 tours sont en fallback legacy** — cf. §3.

---

## §3 — Bugs / régressions identifiés

### B1 — Règle SynFus ignorée dès combat #2 (régression critique)
- Symptôme : `[COMBAT] Aucune règle SynFus en portée (1 règle(s) configurée(s)) → fallback legacy` sur 18/19 tours.
- Conditions remplies pourtant : distance(cible) ∈ [3-8] = dans portée 1-8 du sort, focus EnnemiLePlusProche, PA(6) ≥ coût(4).
- Cause probable (à confirmer en code) : la `MethodeLancement` de la règle est restée **vide** dans la sauvegarde de config (le screenshot 014622 montre le combobox « METHODE DE LANCEMENT » non sélectionné). Côté décideur, méthode vide ⇒ aucune méthode autorisée ⇒ règle filtrée. En combat #1 le décideur tournait sur état avant éventuelle écriture/reload de la config.
- Impact : la branche fallback légacy fonctionne (cast OK) — mais on perd toute la valeur des conditions personnalisées que tu viens d'ajouter.

### B2 — Aucun multi-cast (limite hard-codée 1 sort/tour)
- Tous les tours : PA=6 au début, cast Ronce (4 PA), reste 2 PA → `Gt` direct sans tenter un 2e sort moins cher (ex. Larme niv1 = 2 PA portée 1-3).
- Le décideur ne boucle pas tant que `PA ≥ minCoût`. Roadmap CLAUDE.md ligne 174 : « Multi-cast par tour (drain PA, respecte maxParTour) » toujours TODO.

### B3 — Mode Fuyard non testé cette nuit
- Le log 224044 contenait `Mode=Fuyard` à partir de la ligne 1203, ce qui faisait que le bot choisissait cell 327 (loin) et bougeait plusieurs fois.
- Ce log 014203 reste en `Mode=Agressif` du début à la fin (cell 222 verrouillée). Pas de regression du mode lui-même — juste pas de couverture.

### B4 — `[UI-COMBAT] ToggleNullable` ABSENT du log
- Aucune occurrence dans le log. Soit (a) le user n'a pas cliqué sur ⓘ + coché une condition (cas le plus probable, le screenshot 014635 montre toutes les checkboxes décochées), soit (b) le nouveau log try/catch n'est pas branché. Pas d'évidence de crash mais pas d'évidence de fonctionnement non plus → **à re-tester explicitement** en cochant Min/Max/PV ENN/etc. depuis l'UI.

### B5 — Combat #4 dure 62s pour 1 ennemi à 0 PV
- À T8-T10 (lignes 1392-1525), l'ennemi #-3 ciblé est à `PV=1/46` puis `PV=1/46` puis `PV=1/46` — le bot relance Ronce 3 tours d'affilée pour finir 1 PV. Suggestion de log : afficher quel sort a réellement touché vs miss/résistance, car le mob ne meurt pas immédiatement (probable miss/résistance terre — Ronce = élément terre, Bouftou résiste). Pas un bug du bot mais un signal pour roadmap : prévoir un sort de secours quand un sort ne « débite » plus.

---

## §4 — Top 5 priorités pour fix

1. **(B1) Auto-défaut `MethodeLancement = LesDeux`** quand l'UI sauvegarde une règle avec méthode vide, OU faire matcher le décideur sur règle « méthode vide = wildcard ». Sinon la chaîne SynFus est inutilisable tant que l'user ne re-clique pas la combo. Toucher : `ConfigCombat.cs` (load JSON) + `DecideurCombat` (filtre).
2. **(B4) Re-tester explicitement** la chaîne UI conditions (clic ⓘ + cocher Min/Max/PV/Ennemis ≥/etc., puis Sauvegarder, puis combat) pour valider le log `[UI-COMBAT] ToggleNullable` et fermer la crainte de crash silencieux.
3. **(B2) Implémenter multi-cast par tour** (boucle `tant que PA ≥ minCoût ET nbCastSortX < maxParTour`) — ROI énorme : Beiloddurul peut Ronce(4) + Larme(2) = 100% du PA utilisé, ~+30% dégât/tour estimé. Toucher : `TrameJeu.JouerTourCombatAsync` + `DecideurCombat.ChoisirProchaineAction`.
4. **(B3) Couverture Fuyard manquante** : reproduire un combat en mode Fuyard pour valider que le placement cell 327 + le pathfinder 4-dir tiennent toujours après les patches récents (ConfigCombat/RegleSort/StrategieCombat modifiés cf. git status).
5. **(B5) Détecter sort « inefficace »** sur N tours et basculer cible/sort. Heuristique : si `Cible.PV` ne baisse pas après 2 casts du même sort, retirer cette cible du focus pendant 2 tours. Toucher : `DecideurCombat` + état combat (compteur dégâts/tour).

---

## Pièces visuelles vérifiées

- 014407 / 014635 / 014713 : onglet Combat WPF — règle « Ronce — Ennemi + proche — Les deux — x1 » bien listée, **mais** le combobox « METHODE DE LANCEMENT » sous AJOUTER UN SORT est vide (suggère que la dernière édition a laissé la méthode non choisie sur la règle persistée).
- 014622 : UI conditions (Distance/Cible/Joueur/Situation/Avancé) toutes décochées — confirme que `[UI-COMBAT] ToggleNullable` n'a effectivement pas été stressé.
- 014735 : MapViewer rendu, transitions oranges OK, placement combat dessiné (croix rouge/bleu) — UI map non régressée.
