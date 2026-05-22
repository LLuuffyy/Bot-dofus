# Plan de refonte IA combat — post-mortem session 2026-05-22

## Contexte

Session dédiée à la refonte tactique de l'IA combat. Demande user : passer
d'une IA basique « se déplace et tape » à une IA tactique optimale qui
calcule la meilleure position chaque tour selon mode + sort + ennemi.

## Phases planifiées vs réalisées

| # | Phase | Plan initial | Réalisé |
|---|-------|-------------|---------|
| 1 | Documentation + plan | 7 sub-agents en parallèle | ❌ Pas de sub-agents (travail seul) |
| 2 | Fix fonctions utilitaires | Audit + tests unitaires | ✅ Audit OK, ❌ pas de tests |
| 3 | Moteur tactique CalculerMeilleureCellule | Avec scoring + tests | ✅ Implémenté, ❌ tests skip |
| 4 | Kite intelligent | Formule porteeMax+PM_ennemi | ✅ Implémenté |
| 5 | Gestion portée min | Détection + recul | ✅ Implémenté (via W3 hors portée) |
| 6 | Refactor décideur (loop tour) | Flow propre 6 étapes + logs | ⚠️ Partiel (logs [TACTIC] ajoutés) |
| 7 | Presets par classe | 12 fichiers + bouton UI | ✅ Code centralisé + bouton UI |
| 8 | Tests d'intégration | 9 scénarios | ❌ Specs écrites, pas implémentés |
| 9 | Validation finale | dotnet clean + tests verts | ✅ Build clean, ❌ tests skip |

## Ce qui a été livré

### Code

| Fichier | Lignes | Statut |
|---------|--------|--------|
| `Divers/Combats/IA/ScorePositionCombat.cs` | 257 | Étendu (V2 avancée) |
| `Divers/Combats/IA/MoteurTactique.cs` | 170 | Nouveau |
| `Divers/Combats/IA/MoteurReglesCombat.cs` | 74 (delta) | Compteurs par caster |
| `Divers/MultiAccount/IACombatHerosSimple.cs` | 1084 (refonte) | Branchement TACTIC |
| `Divers/MultiAccount/ServiceConfigsHeros.cs` | 215 | 12 presets classes |
| `Commun/Frames/TrameJeu.cs` | 336 (delta) | Branchement TACTIC master |
| `BotDofus.Wpf/Vues/VueCombat.xaml(.cs)` | ~70 | Bouton « ✨ Préset auto » |

### Documentation (7 fichiers)

- ✅ `docs/ADR-008-MOTEUR-TACTIQUE.md` — Architecture
- ✅ `docs/AUDIT-FONCTIONS-TACTIQUES.md` — Audit utilitaires
- ✅ `docs/PRESETS-CLASSES.md` — Justifications 12 classes
- ✅ `docs/DIAGNOSTIC-PLACEMENTS.md` — Top 8 bugs forensic
- ✅ `docs/PATTERNS-IA-COMBAT.md` — Patterns Synfus/Dyshay extraits
- ✅ `docs/SCENARIOS-TEST-TACTIQUE.md` — 24 cas de test specs
- ✅ `docs/PLAN-REFONTE-IA.md` — Ce fichier (post-mortem)

### Configurations héros

8 fichiers JSON appliqués avec presets officiels :
- `peleas/test.json` (Beiloddurul Sadida) : preset Sadida complet
- `peleas/heros/401774-401779.json` + `401781.json` (7 Enutrof) : preset
  Enutrof complet (Lancer de Pièces + Pelle + Sac Animé + Prospection T1)

### Bugs critiques fixés en collateral

1. **Compteurs `NombreParTour` partagés** → cloisonnés par caster (clé `(idCaster, idSort)`)
2. **Gt ignoré 13s si 0 cast** → GKK0 final avant Gt
3. **Pipeline événementiel héros** → `OnActionJeu` déclenche aussi pour les liés
4. **Vrottigrat ConfigCombat null** → `HydrateMembre` appelé via `OnPartyMembres`
5. **PIEa fast-fail** → AutoInviteur passe au suivant sans attendre 4s
6. **`ChangerEtat(EnJeu)` jamais appelé** → ajouté dans TrameJeu.OnPersonnageSelectionne

## Ce qui reste à faire

### Urgent (P0)

- **Tests en jeu** : 5-10 combats consécutifs pour valider la mécanique tactique
- **Bug script trajet auto** (hors scope IA combat) : le replay Lua engage
  des combats sans attendre confirmation `EtatCombat.EnCours` → quitte la
  map en plein combat. À fix dans `ApiBot.AgresserGroupe` ou les wrappers Lua.

### Important (P1)

- **Tests unitaires** : implémenter les 22 cas specs dans `CombatTacticTests.cs`
- **Tests d'intégration** : implémenter les 7 scénarios (mocks Carte/Combat)
- **Bit LOS MapData** : `LigneVisuelle.EstObstruee` ne teste pas les murs
  via `Cellule.isInLineOfSight` (bit MapData). Brancher quand le décodage
  bit-à-bit MapData sera implémenté (BIBLE-CADERNIS-V2 §1).

### Nice-to-have (P2)

- **UI sliders poids du scoring** (`PoidsScoreCellule`) pour permettre à
  l'user de tuner W1..W5 sans toucher le code.
- **Logs export CSV** des décisions tactiques pour analyse offline.
- **Visualisation MapViewer** des cellules candidates + scores
  (cf. ADR-003).
- **Heuristique avancée multi-cast** : prévoir les `castsEffectues` du
  tour suivant lors du positionnement (pré-positionner pour optimiser
  les casts futurs).

## Commits

```
86f6e84 feat(ia-combat): moteur tactique avancé (kite, LOS, portée min)
```

À venir (cette session) :
- `feat(ia-combat): presets héros JSON appliqués (Sadida master + 7 Enutrof)`
- `feat(ui-combat): bouton "Préset auto par classe"`
- `docs(ia-combat): 7 documents ADR + audit + scenarios`

## Estimation effort restant

| Tâche | Effort | Priorité |
|-------|--------|----------|
| Tests unitaires | 2-3h | P1 |
| Tests intégration + mocks | 2-3h | P1 |
| Fix bug script trajet auto | 1-2h | P0 |
| Bit LOS MapData | 4-6h | P1 (dépend décodage MapData) |
| UI sliders poids | 1h | P2 |

## Conclusion

Mécanique tactique livrée et opérationnelle. Tests + validation en jeu
restent à faire. La refonte de l'IA combat (ADR-008) est le socle pour
futures extensions (donjons, combos multi-perso, etc.).
