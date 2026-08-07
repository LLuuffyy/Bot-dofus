# ADR-008 — Moteur tactique de positionnement IA combat

**Date** : 2026-05-22
**Statut** : Implémenté
**Auteurs** : Toukiki (specs) + Claude Opus 4.7 (impl)

## Contexte

Le moteur tactique précédent (`ScorePositionCombat.ScoreCellule`) ne prenait pas
en compte le **sort principal** ni le **PM de l'ennemi**. Résultat : kite naïf,
LOS pas vérifiée pré-déplacement, portée min ignorée.

## Décision

Introduction d'un **moteur tactique central** `MoteurTactique.CalculerMeilleureCellule`
qui :
1. Reçoit un `ContexteTactique` (mode + sort + ennemi)
2. Calcule la `DistanceIdeale` selon le mode (kite intelligent pour Eloigne)
3. Score chaque cellule candidate via `ScoreCelluleAvance` multi-critères
4. Retourne (Cellule, Chemin, PmConsommes, Score)

### Distance idéale par mode

| Mode | Formule |
|------|---------|
| Agressif | `1` si sort CAC, sinon `max(porteeMin, 1)` |
| Eloigne / Fuyard | **`porteeMax + PmEnnemiCible`** (kite parfait) |
| Equilibre | `clamp(distPref, porteeMin, porteeMax)` |

**Pourquoi `porteeMax + PmEnnemi`** : si je m'arrête à porteeMax + N, l'ennemi
qui a N PM arrive juste à porteeMax au tour suivant → je peux re-cast SANS être
tacle. C'est le kite optimal pour Cra/Xelor.

### Scoring multi-critères (`ScoreCelluleAvance`)

Plus le score est BAS, meilleure est la cellule.

```
score = W1 * |distCible - distIdeale|         // objectif tactique principal
      + W2 * (LOS_bloquée && requise ? 1 : 0) // 1000 = blocage critique
      + W3 * écart_hors_portée_sort           // 500 par case d'écart
      + W4 * sommeDist_multi_mobs             // ±1 selon Agressif/Eloigne
      + W5 * (LOS_OK ? 0 : 2)                 // tie-break léger
```

Poids par défaut : W1=10, W2=1000, W3=500, W4=1, W5=2.

### Pathfinder

A* combat 4-dir strict (`Pathfinder.Trouver(combat: true)`). Évite les
combattants vivants (alliés + ennemis). Pré-filtre Manhattan pour éviter
le coût A* sur les cellules manifestement trop loin.

## Conséquences

### Avantages

- Kite optimal en Eloigne (Cra reste à porteeMax + PM_ennemi exact)
- Plus de placement derrière obstacle/allié si sort nécessite LOS
- Portée min respectée (un Cra à dist 2 RECULE pour rentrer dans [6-12])
- Multi-mobs : score Σdist prend en compte tous les ennemis
- Réutilisable : master + héros liés via la même classe

### Inconvénients

- Coût CPU : O(N × pathfinder_cost) où N = nombre cells candidates.
  Mitigation : pré-filtre Manhattan + pré-score AVANT A*.
- Dépendance au PM ennemi : si `Combattant.PM = 0` (info pas à jour),
  fallback conservateur sur PM=3.

## Composants

| Fichier | Rôle |
|---------|------|
| `Divers/Combats/IA/ScorePositionCombat.cs` | `ContexteTactique`, `DistanceIdeale`, `ScoreCelluleAvance` |
| `Divers/Combats/IA/MoteurTactique.cs` | `CalculerMeilleureCellule(carte, depart, pm, ...)` |
| `Commun/Frames/TrameJeu.cs:TenterMoteurTactiqueMaster` | Branchement master |
| `Divers/MultiAccount/IACombatHerosSimple.cs:PreMouvementSelonModeAsync` | Branchement héros |

## Tests

À écrire (Phase 8 du plan initial) :
- `DistanceIdeale` : 4 modes × 3 sorts (CAC/dist/burst) × variations PM ennemi
- `ScoreCelluleAvance` : cas LOS OK/KO, hors portée, score-rank
- Intégration : scénario "Cra ennemi 3 PM → recul à porteeMax+3"

## Liens

- Forensic problèmes initiaux : `docs/DIAGNOSTIC-PLACEMENTS.md`
- Patterns Synfus/Dyshay : `docs/ANALYSE-DYSHAY-COMBAT.md`
- Audit fonctions utilitaires : `docs/AUDIT-FONCTIONS-TACTIQUES.md`
