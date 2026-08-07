# Plan de refonte IA Combat — Luffy-bot (nuit 2026-05-21)

> Doc n°4 / 4. **L'ADR de synthèse** : les 3 docs précédents ont décrit l'état
> de l'art (SynFus/dyshay) et l'état actuel de notre IA. Ce doc liste les
> 10 phases d'implémentation à effectuer, leurs dépendances mutuelles, les
> fichiers concernés (chemins absolus), et l'estimation d'effort.
>
> **Aucun code à toucher avant validation** du plan par l'utilisateur.
> Tout est numéroté en `Phase N`, parallélisable où mentionné, sinon
> séquentiel (dépendances explicites).

---

## A. Synthèse des 3 docs précédents

### A.1 Constat SynFus (doc 1)
- Boucle tour event-driven : `TurnStarted → ProcessRule → callback → recurse`.
- 3 compteurs distincts : `lanzamientos_x_turno`, `lanzamientos_por_objetivo`,
  `hechizos_intervalo` (cooldown multi-tours).
- 12 codes `FallosLanzandoHechizo` explicites.
- `get_Fin_Turno` repositionnement avant `Gt`.
- Form UI : 22 conditions par sort + 4 modes.

### A.2 Constat dyshay (doc 2)
- Source authoritative (`Alvaro Prendes 2019`, identique SynFus).
- Pathfinder combat strict 4-dir (`PeleasPathfinder`).
- Encoding GA001 segments `<dir><cell2char>`.
- Sum distance multi-ennemis (`Get_Total_Distancia_Enemigo`).
- Heuristique cible : low-HP non-invocation dans range, fallback any.

### A.3 Constat notre code (doc 3)
- IA actuelle : mono-cast + déplacement si hors portée + mode passif global.
- Stable sur cipher MITM, pipeline event-based déplacement (ADR-002).
- 22 conditions UI bindées + parsées (mais `NombreParCible` ignoré).
- Manque : multi-cast, repositionnement post-cast, cooldown multi-tours,
  soin auto, invocations, heuristique cible smart, codes échec explicites.

---

## B. Les 10 phases d'implémentation

### Phase 1 — Refactor en machine d'état event-driven

**But** : extraire la boucle linéaire `JouerTourCombatAsync` (260 l.) en une
machine d'état pilotée par les events serveur (`hechizo_lanzado`, `movimiento`,
`turno_iniciado`, `turno_acabado`), à la manière de dyshay
`FightExtensions.get_Procesar_hechizo`.

**Fichiers concernés** (créer / modifier) :
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\MachineEtatCombat.cs` **(nouveau, ~250 l.)**
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\ProcesseurTour.cs` **(nouveau, ~150 l.)**
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Commun\Frames\TrameJeu.cs` (extraction IA combat vers ProcesseurTour)
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\Combat.cs` (events `SortLance`, `DeplacementTermine`, `TourTermine`)

**Dépendances** : aucune. Fondation pour Phases 4 et 6.

**Effort** : ~4-5h. Critique : ne PAS casser le pipeline `PipelineDeplacementCombat`.

---

### Phase 2 — Enum codes échec cast explicites

**But** : porter `FallosLanzandoHechizo` (12 codes) en notre `EchecLancement`
pour remplacer les `continue` silent du moteur règles. Permet log précis +
décision "rejette ce sort, essaie suivant" vs "bouge puis re-essaye".

**Fichiers concernés** :
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\EchecLancement.cs` **(nouveau, ~30 l.)**
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\MoteurReglesCombat.cs` (refactor `Evaluer` retourne aussi raison rejet)

**Enum** :
```csharp
public enum EchecLancement
{
    Aucun, SortInconnu, NonAppris, PaInsuffisants,
    TropDeLancers, TropDeLancersParCible, EnCooldown,
    TropDInvocations, NonEnRange, PasEnLigne, NecessiteCelluleVide,
    LosObstruee, ConditionFiltree
}
```

**Dépendances** : aucune. Indépendant. Peut tourner en parallèle Phase 1.

**Effort** : ~1-2h. Simple, mécanique.

---

### Phase 3 — Cooldown multi-tours `IntervalleSort`

**But** : ajouter `Combat.IntervallesSorts: Dictionary<int, int>` (sortId →
tours restants) qui se décrémente à chaque `NouveauTour` (= dyshay `Fight.hechizos_intervalo`).

**Fichiers concernés** :
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\Combat.cs` (ajout Dict + reset/dec dans `NouveauTour`)
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\MoteurReglesCombat.cs` (check `IntervallesSorts.ContainsKey(idSort) → EnCooldown`)
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Jeu\Personnage\Spells\InfoSort.cs` (déjà a `Stats.Intervalle`, à exposer)

**Dépendances** : Phase 2 (utilise `EchecLancement.EnCooldown`).

**Effort** : ~1h.

---

### Phase 4 — Multi-cast par tour

**But** : après cast réussi (event `hechizo_lanzado` succès), re-rentrer dans
le décideur tant que :
- PA restants ≥ coût d'un sort utilisable.
- Pas dépassé `RegleSort.NombreParTour` (déjà géré par `CompteursRegleParTour`).
- Pas dépassé `RegleSort.NombreParCible` (à câbler).

**Fichiers concernés** :
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\ProcesseurTour.cs` (boucle `WhileCanCast`)
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\Combat.cs` (ajout `Dictionary<(int sortId, int cellId), int> CompteursParCible`)
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\MoteurReglesCombat.cs` (check `CompteursParCible[(sortId, cible.Cell)] >= NombreParCible`)

**Dépendances** : Phase 1 (machine état), Phase 3 (cooldown).

**Effort** : ~2-3h. Attention timing GKK0 entre deux casts (envoyer GKK0
après chaque GA300, pas seulement après le dernier).

---

### Phase 5 — Heuristique cible low-HP non-invoc

**But** : porter `Fight.get_Obtener_Enemigo_Mas_Cercano(range)` :
- 1er passage : focus low-HP non-invocation dans range portée sort.
- 2e passage : si rien trouvé, accepte invocations.
- Fallback : ennemi le plus proche.

**Fichiers concernés** :
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\SelectionCible.cs` **(nouveau, ~80 l.)**
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\MoteurReglesCombat.cs` (`ChoisirCible` délègue à `SelectionCible.MeilleureCibleEnemmi(...)`)

**Dépendances** : aucune. Parallélisable.

**Effort** : ~1-2h.

---

### Phase 6 — Repositionnement post-cast (`EndTurnRepositioning`)

**But** : porter `FightExtensions.get_Fin_Turno` qui repositionne avant `Gt` :
- AGRESSIF + pas en CAC → avance vers ennemi le plus proche.
- FUYARD + en CAC ou ennemi < 8 cases → recule (maximise dist multi-ennemis).
- FUYARD + ennemi > 12 cases → avance pour rester en portée.

Utilise `Get_Total_Distancia_Enemigo = sum(dist(cell, ennemi) - 1)`.

**Fichiers concernés** :
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\Repositionnement.cs` **(nouveau, ~120 l.)**
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\ProcesseurTour.cs` (appel `Repositionnement.AvantFinTour(...)` avant `Gt`)
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\ConfigCombat.cs` (déjà a `BloquerLeCombat`, ajouter `RepositionnerEnFinTour: bool = true`)

**Dépendances** : Phase 1 (machine état appelle le repositionneur entre dernier
cast et `Gt`), Phase 4 (multi-cast doit être terminé avant repositionnement).

**Effort** : ~3h. Test : valider sur Sadida vs Bouftou que le bot recule
après avoir cast Ronce niv 5 (portée max 8).

---

### Phase 7 — Refactor `MoteurReglesCombat` ↔ `ProcesseurTour` ↔ `SpellsManager`

**But** : aligner notre architecture sur dyshay. Le `MoteurReglesCombat` (évalue
règles) reste séparé du `ProcesseurTour` (orchestre cast/move/end-turn) qui
appelle un nouveau `SpellsManager` (= équivalent dyshay) responsable du
cast / mouvement avant cast.

**Fichiers concernés** :
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\GestionnaireSorts.cs` **(nouveau, ~150 l., port dyshay `SpellsManager`)**
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\ProcesseurTour.cs` (utilise `GestionnaireSorts.Lancer(regle)`)
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\DecideurCombat.cs` (**SUPPRIMER** ; classe legacy redondante avec `MoteurReglesCombat`)
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\ActionCombat.cs` (peut rester si on garde l'abstraction)
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Commun\Frames\TrameJeu.cs` (supprime `JouerTourCombatAsync` + `TrouverApprocheCombat` + `ExecuterRegleAsync` → délègue à `ProcesseurTour`)

**Dépendances** : Phases 1, 4, 6 (toutes complétées avant ce gros nettoyage).

**Effort** : ~4-5h. Le plus risqué : touche le cœur. **Validation** : doit
fonctionner sur un combat live BEFORE merge (test avec Beiloddurul vs un
groupe de 2 Bouftou).

---

### Phase 8 — Use `NombreParCible` dans moteur règles

**But** : ajouter le compteur `Combat.CompteursParCible: Dictionary<(int sortId, int cellId), int>`
et le check dans `MoteurReglesCombat.Evaluer` :

```csharp
if (regle.NombreParCible > 0)
{
    int dejaSurCible = combat.CompteursParCible.TryGetValue((regle.IdSort, cible.CellulePosition), out var c) ? c : 0;
    if (dejaSurCible >= regle.NombreParCible) continue; // ou EchecLancement.TropDeLancersParCible
}
```

**Fichiers concernés** :
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\Combat.cs` (ajout Dict tuple)
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\MoteurReglesCombat.cs` (check)
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\GestionnaireSorts.cs` (incrément après cast réussi)

**Dépendances** : Phase 2 (code échec), Phase 7 (GestionnaireSorts existe).

**Effort** : ~1h.

---

### Phase 9 — Soin auto consommable

**But** : avant chaque tour, si PV % ≤ `cfg.ConsommableUtiliserSiPvInfPct`,
chercher l'objet `cfg.ConsommableSoinIdTemplate` dans l'inventaire et envoyer
`GOFLO<idObjet>` (paquet 1.29 utilisation objet) tant que PV % <
`cfg.ConsommableJusquaPvSupPct`, avec délai `cfg.ConsommableDelaiMin/Max`.

**Fichiers concernés** :
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\Soigneur.cs` **(nouveau, ~100 l.)**
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\ProcesseurTour.cs` (appel `Soigneur.UtiliserSiBesoinAsync()` en début de tour)
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Commun\Frames\TrameJeu.cs` (déjà a accès à `Personnage.Inventaire`)
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Jeu\Personnage\Personnage.cs` (event `InventaireMaj` pour rafraîchir compte)
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\BotDofus.Wpf\Vues\VueCombat.xaml(.cs)` (UI section consommable déjà présente, brancher save → ConfigCombat)

**Dépendances** : Phase 1 (machine état appelle Soigneur en début tour).

**Effort** : ~2-3h. **Bloquant** : trouver le bon paquet 1.29 pour
"utiliser objet en combat" — cf. cadernis #1771 « Bonne IA en combat »
(phylonia) ou capture user avec pain.

---

### Phase 10 — Invocations Sadida + capture âme (optionnel)

**But** : sorts spéciaux Sadida :
- 182 La Folle (invoc → cell vide en début combat).
- 193 La Bloqueuse (invoc → cell vide).
- 198 Sacrifice Poupesque (mute allié → PV transferts).

Et capture âme (`HechizoFocus.CELDA_VACIA` déjà supporté en config, à câbler
dans le moteur).

**Fichiers concernés** :
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\Invocations.cs` **(nouveau, ~80 l.)**
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\MoteurReglesCombat.cs` (`ChoisirCelluleVide` : scanne cells adjacentes libres)
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\Combat.cs` (compteur `Invocations.Count` filtré par `idInvocateur == perso.Id`)

**Dépendances** : Phases 1-8.

**Effort** : ~3h. **Optionnel pour la session nuit** (le projet d'examen
fonctionne sans).

---

## C. Dépendances entre phases (graphe)

```
Phase 1 ──┬──> Phase 4 ──┬──> Phase 6 ──┬──> Phase 7 ──┬──> Phase 8
          │              │              │              │
          │              │              │              ├──> Phase 9
          │              │              │              │
          │              │              │              └──> Phase 10
          │              │
Phase 2 ──┴──> Phase 3 ──┘              
          │              
          └──> Phase 4 (codes échec utilisés par compteurs)
          
Phase 5 (indépendant, parallélisable n'importe où)
```

**Chemin critique** : 1 → 4 → 6 → 7 → 8.
**Parallélisable** : 2, 3, 5 peuvent tourner en parallèle dès le début.

---

## D. Ordre d'exécution recommandé (séquentiel pour nuit autonome)

| # | Phase | Justification |
|---|-------|---------------|
| 1 | **Phase 2** — Enum codes échec | Mécanique simple, débloque Phases 3-4 |
| 2 | **Phase 3** — Cooldown multi-tours | Petit ajout, démontre la logique de compteurs |
| 3 | **Phase 5** — Heuristique cible low-HP | Indépendant, gain qualité immédiat |
| 4 | **Phase 1** — Refactor machine état | Le plus gros, fondation pour la suite |
| 5 | **Phase 6** — Repositionnement fin tour | Gain visible "bot vivant" (kite Sadida) |
| 6 | **Phase 4** — Multi-cast par tour | Gain énorme de DPS |
| 7 | **Phase 8** — `NombreParCible` | Petit ajout après Phase 4 |
| 8 | **Phase 7** — Refactor final (supprime legacy) | Cleanup une fois 1+4+6 stables |
| 9 | **Phase 9** — Soin auto consommable | Quality of life pour grind |
| 10 | **Phase 10** — Invocations Sadida | Optionnel, projet d'examen marche sans |

---

## E. Estimation effort total

| Phase | Effort | Cumul |
|-------|--------|-------|
| 2 | 1-2h | 2h |
| 3 | 1h | 3h |
| 5 | 1-2h | 5h |
| 1 | 4-5h | 10h |
| 6 | 3h | 13h |
| 4 | 2-3h | 16h |
| 8 | 1h | 17h |
| 7 | 4-5h | 22h |
| 9 | 2-3h | 25h |
| 10 | 3h | 28h |

**Total** : ~22h sans Phase 9-10, ~28h tout compris.

Session nuit autonome (~6-8h max effective) → réaliste d'attaquer Phases 2, 3,
5, 1, et entamer 6. Le reste sur session suivante.

---

## F. Critères de succès (validation E2E)

À chaque phase complétée, doit passer ces 3 tests live :

### Test 1 — Combat solo Beiloddurul vs Bouftou cell 175 (Astrub)
- Bot place près de l'ennemi (mode Agressif).
- Tour 1 : cast Ronce niv 5 (portée 1-8, 4 PA), il reste 2 PA → cast Larme
  (3 PA portée 1-3, donc skip si dist > 3) sinon Gt.
- Tour 2-3 : multi-cast si PA suffisants.
- Repositionnement post-cast : si mode Fuyard et ennemi à 2 cases → recule
  de 3 PM jusqu'à dist 5.

### Test 2 — Combat duo Beiloddurul + Aerawiol vs 2 Bouftou
- Bot leader engage en CAC (Sacrifice Poupesque sur Aerawiol si pris en target).
- Filtre cible : ignore mob invoc, vise low-HP non-invoc.
- Soin auto : si PV Aerawiol < 50% → utilise consommable de soin.

### Test 3 — Combat groupe 3+ ennemis
- Sum-distance multi-ennemis : repositionnement = optimum global pas 1-ennemi.
- Cooldown sort : si Larme a `intervalle=2`, après cast tour N, indispo
  tour N+1, redispo tour N+2.
- Multi-cast respecte `NombreParTour` (ne lance pas Ronce 4× si max=2).

---

## G. Notes finales

- **Aucune modif de code à effectuer cette nuit.** Le user a explicitement
  demandé "PAS de modif de code". Ce plan + 3 docs préparatoires = livrable.
- Les fichiers `Synfus.exe` étant obfusqué (.NET self-contained), toute
  l'analyse SynFus passe par **dyshay** (auteur identique, code clair).
- Le worktree `vigorous-murdock` contient une copie complète de dyshay-source
  utilisable comme référence pendant l'implémentation.
- Documenter chaque phase implémentée par un **commit séparé** avec message
  FR style `feat(combat): phase N — <titre>` + Co-Authored-By Claude.

---

## H. Liens vers les 3 docs frères

1. [`docs/ANALYSE-SYNFUS-COMBAT.md`](./ANALYSE-SYNFUS-COMBAT.md) — extraction
   logique SynFus depuis dyshay (auteur identique). 6 sections : boucle tour,
   décision déplacement, choix cellule, PA/PM/multi-cast, UI form, synthèse.

2. [`docs/ANALYSE-DYSHAY-COMBAT.md`](./ANALYSE-DYSHAY-COMBAT.md) — extraits
   verbatim + traduction française des 5 fichiers clés dyshay : SpellsManager,
   FightExtensions, Fight, Movimiento, PathFinderUtil. 8 sections, mapping
   complet dyshay → notre code.

3. [`docs/IA-COMBAT-EXPLICATION.md`](./IA-COMBAT-EXPLICATION.md) — doc
   central : architecture WPF, cycle de tour pas-à-pas (7 étapes), règle de
   décision, déplacement event-based, 10 bugs historiques + fix, comparaison
   dyshay vs notre code, mapping ligne-à-ligne.

4. **Ce doc** — synthèse + 10 phases d'implémentation + dépendances + effort.
