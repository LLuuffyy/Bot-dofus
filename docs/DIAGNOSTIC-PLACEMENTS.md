# Diagnostic des placements IA combat — pré-refonte ADR-008

**Date forensic** : 2026-05-22 (logs 14:38 → 16:21)
**Méthode** : grep + lecture détaillée des logs de 10 combats consécutifs.

## Top 3 problèmes identifiés

### #1 — Compteurs `NombreParTour` partagés entre persos (CRITIQUE)

**Symptôme** : Athabiel et Aelardast finissent leurs tours avec 0 cast.
**Log forensic** (14:38:28) :
```
Dranariel cast Lancer de Pièces 3× → CompteursRegleParTour[51]=3
Aerawiol FALLBACK + 1 cast
Athabiel : pré-move OK → Fin tour (0 cast)  ← compteur saturé !
Aelardast : pareil
```

**Cause** : `Combat.CompteursRegleParTour` était indexé par `idSort` seul.
Le 1er caster à dépenser ses casts saturait le compteur partagé.

**Fix appliqué** : clé cloisonnée `(idCaster, idSort)`. Idem pour
`CompteursRegleParCible` (clé `(idCaster, idSort, idCible)`).

### #2 — Persos coincés sans cast → Gt ignoré 13 secondes

**Symptôme** : héros qui fait juste un pré-move (pas de cast) envoie `Gt`,
mais le serveur l'ignore. Il faut intervention manuelle.

**Log forensic** (14:38:28 Athabiel) :
```
14:38:28.598 [Athabiel] Fin tour (0 cast(s))
14:38:28.598 [INJ ->SRV] Gt              ← bot envoie Gt
... 13 SECONDES de silence serveur ...
14:38:41.382 [VOCAB C→S] Gt              ← USER appuie « Passer » manuellement
14:38:41.420 [PKT S→C] GTF401777         ← serveur accepte enfin
```

**Cause** : Serveur Hystoria attend un `GKK0` final pour fermer l'action
de déplacement avant d'accepter le `Gt`. Sans cast après le pré-move, pas
de GKK0 → tour bloqué.

**Fix appliqué** (dans `IACombatHerosSimple.JouerTourAsync` et fallback
master) : envoyer `GKK0` final SI `castsEffectues == 0` avant le `Gt`.

### #3 — Pas de prise en compte du PM ennemi (kite naïf)

**Symptôme** : Cra/Sadida en Eloigne s'arrête à distance arbitraire (max
PM possible), pas à la distance optimale qui dépend du PM ennemi.

**Cas concret** : Cra portée 1-8 + ennemi 3 PM.
- Mauvais : recul à 11 cases → ennemi avance 3 → arrive à 8 → cast OK MAIS
  tour suivant ennemi avance 3 → arrive à 5 → recul à 11 = max PM 3 →
  KITE INFINI mais sans damage progressif.
- Optimal : recul à `porteeMax + PM_ennemi = 8 + 3 = 11` → ennemi arrive
  pile à 8 (porteeMax) → cast immédiat à damage MAX.

**Fix appliqué** : `ScorePositionCombat.DistanceIdeale` pour Eloigne/Fuyard :
```
distIdeale = porteeMax + (pmEnnemi > 0 ? pmEnnemi : 3)
```

## Top 5 problèmes mineurs

### #4 — LOS pas vérifiée pré-déplacement

Le bot pouvait se placer derrière un allié → sort nécessite LOS → cast
rejeté serveur → tour perdu.

**Fix** : `MoteurTactique` pénalise (W2=1000) les cellules sans LOS si
le sort principal nécessite LOS.

### #5 — Portée min ignorée

Un Cra portée 6-12 à dist 2 ne savait pas reculer pour rentrer dans la
fourchette.

**Fix** : `ScoreCelluleAvance` pénalise (W3=500/case) hors `[porteeMin, porteeMax]`.

### #6 — Cible « fantôme » après déplacement ennemi

Les héros tiraient sur cell 253 alors que l'ennemi #-2 y était mort ou en
transit. `combat.Ennemis` gardait l'ancienne position.

**Fix** : `MoteurReglesCombat.Evaluer` filtre `!EstMort && PV > 0 && PVMax > 0`.

### #7 — Timeout déplacement héros 2500ms (`PipelineDeplacementCombat`)

L'event `Combat.MouvementBotConfirme` n'était déclenché que pour le master.
Les broadcasts `GA;0;<idHeros>` étaient ignorés → 2500ms perdus à chaque
pré-move héros.

**Fix** : `TrameJeu.OnActionJeu` étendu pour traiter aussi les héros liés
(toute la branche `else` quand `acteurId != _etat.Personnage.Identifiant`).

### #8 — `ConfigCombat` jamais hydratée pour héros invités via PM+

Vrottigrat ajouté via PM+ (pas via `GTSX` combat) n'avait pas de config
chargée → IA disait `ConfigCombat vide → Gt direct`.

**Fix** : `DetecteurModeHeros.OnPartyMembres` appelle maintenant
`ServiceConfigsHeros.HydrateMembre` pour les Suiveurs.

## Conclusion

8 bugs majeurs identifiés, tous corrigés dans la refonte ADR-008. Le moteur
tactique central + les compteurs cloisonnés résolvent les problèmes de
gameplay observés (héros muets, mauvais placement, kite naïf).

Reste à valider en jeu sur 5-10 combats consécutifs.
