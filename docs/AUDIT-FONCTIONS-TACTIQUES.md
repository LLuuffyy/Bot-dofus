# Audit des fonctions utilitaires tactiques

**Date** : 2026-05-22
**Contexte** : pré-implémentation moteur tactique ADR-008

## Fonctions auditées

### A. `Cellule.DistanceChebyshev` / `DistanceDofus`

**Statut** : ✅ OK

**Référence** : `max(|dx|, |dy|)` sur coordonnées (x,y) iso Dofus.
C'est la métrique CANONIQUE des portées de sorts Dofus 1.29 (cases
diagonales = distance 1).

**Pièges vérifiés** :
- Largeur carte Hystoria = 15 (pas 14) — fix forensic 2026-05-20.
- Distances toujours calculées via `Cellule.CalculerCoordonnees(id, mw)`,
  où `mw = carte.Largeur` (pas une constante hardcodée).

**Cas testés** (manuel sur logs) :
- Cell 200 vs 199 : dist=1 (adjacent)
- Cell 302 vs 224 : Chebyshev calculé en accord avec ce que le serveur attend
  pour la portée (cf. log 22:40 acceptation Ronce niv5 dist=8).

### B. `LigneVisuelle.EstObstruee`

**Statut** : ✅ OK

**Algo** : Bresenham iso aligné Dyshay (`Fight.get_Linea_Obstruida`).
- Pad précision ×100 pour gérer les sous-cellules
- Décalage +0.5 pour viser le centre cellule
- Diagonal pur géré séparément (`|dx| == |dy|`)
- X-major vs Y-major selon `|dx|` vs `|dy|`

**Limites V1** :
- Surcharge `EstObstruee(a, b, ISet<int>)` (sans carte) : ne teste PAS les
  obstacles → utilisée seulement quand la carte n'est pas dispo.
- Surcharge `EstObstruee(carte, a, b, ISet<int>)` : test combattants OK,
  mais NE TESTE PAS les bits LOS des murs MapData (à brancher quand le
  décodage bit-à-bit MapData sera implémenté, cf. BIBLE-CADERNIS-V2 §1).

**Cas testés** : 0 bugs détectés, comportement aligné dyshay.

### C. `Pathfinder.Trouver(carte, depart, arrivee, interdites, combat: bool)`

**Statut** : ✅ OK

**Algo** : A* heuristique Chebyshev sur grille iso.
- Mode `combat: true` → voisinage 4-dir orthogonal strict (cf. dyshay
  `PeleasPathfinder.get_Celdas_Adyecentes`)
- Mode `combat: false` → 8-dir avec diagonales pour overworld
- Filtre `interdites` : combattants vivants + obstacles map

**Limites** : pas de cap explicite sur le nombre de PM (le caller doit
checker `chemin.Count - 1 <= pmDispo`).

### D. `Pathfinder.EncoderChemin` / `PaquetDeplacement`

**Statut** : ✅ OK

**Format** : compression dyshay (`get_Pathfinding_Limpio`) — un segment
`<dir><cell2chars>` par changement de direction.

### E. `ScorePositionCombat.SommeDistances` / `DistanceMin`

**Statut** : ✅ OK (helpers pré-calculés, pas de bug)

### F. `ScorePositionCombat.ScoreCellule` (legacy)

**Statut** : ✅ OK pour la version multi-mobs basique. Conservée pour
fallback dans `IACombatHerosSimple.PreMouvementLegacyAsync` quand le sort
principal n'est pas identifiable.

## Nouvelles fonctions (ADR-008)

### G. `ScorePositionCombat.DistanceIdeale(ctx)`

**Statut** : ✅ Nouvelle, à tester unitairement.

**À tester** :
- Agressif + sort CAC (portée 1-1) → 1
- Agressif + sort distance (portée 1-8) → 1
- Eloigne + sort 1-8 + ennemi 3 PM → 11 (= 8+3)
- Eloigne + sort 1-12 + ennemi 0 PM → 15 (= 12+3 conservatif)
- Equilibre + sort 1-8 + distPref 6 → 6
- Equilibre + sort 1-8 + distPref 12 → 8 (clamp porteeMax)
- Equilibre + sort 1-8 + distPref 0 → 1 (clamp porteeMin)

### H. `ScorePositionCombat.ScoreCelluleAvance(...)`

**Statut** : ✅ Nouvelle, à tester unitairement.

**À tester** :
- Cellule pile à distIdeale + LOS OK → score minimal (≈ 0)
- Cellule à distIdeale-3 + LOS OK → score 30 (W1 × 3)
- Cellule à distIdeale + LOS bloquée + sort LOS → score +1000 (W2)
- Cellule hors [porteeMin, porteeMax] → score +500/case (W3)
- Cellules égales → tie-break sur sommeDist (W4)

### I. `MoteurTactique.CalculerMeilleureCellule(...)`

**Statut** : ✅ Nouvelle, à tester en intégration.

**À tester** :
- Cra dist 8 ennemi 3 PM → recul à 11
- Cra dist 4 ennemi 0 PM → recul à 12 (kite vers porteeMax)
- Iop dist 5 → avance à CAC (dist=1)
- Eni allié blessé adjacent → reste si distMin OK pour Mot Curatif
- Multi-mobs : 3 ennemis → Fuyard maximise Σdist
- Cellule cible LOS bloquée par allié → choisit autre cell

## Conclusion

**Aucun bug critique détecté** dans les fonctions existantes. Les nouvelles
fonctions ADR-008 sont conformes à l'architecture spécifiée et bâties sur
des primitives auditées (Chebyshev, A*, Bresenham).

Les tests unitaires/intégration ne sont **pas encore écrits** dans cette
session (limite scope). À ajouter dans `BotDofus.Tests` ultérieurement.
