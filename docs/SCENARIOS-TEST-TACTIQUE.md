# Scénarios de test tactique — IA combat

**Date** : 2026-05-22
**Statut** : Specs écrites, **tests non implémentés** dans cette session
(limite scope). À ajouter dans `BotDofus.Tests/CombatTacticTests.cs`.

## Tests unitaires (fonctions critiques)

### `ScorePositionCombat.DistanceIdeale`

| # | Mode | Sort (pmin-pmax) | PM ennemi | Attendu |
|---|------|------------------|-----------|---------|
| 1 | Agressif | 1-1 (CAC) | 3 | 1 |
| 2 | Agressif | 1-8 (dist) | 3 | 1 |
| 3 | Eloigne | 1-8 | 0 | 11 (= 8+3 conservatif) |
| 4 | Eloigne | 1-8 | 3 | 11 |
| 5 | Eloigne | 1-12 | 5 | 17 |
| 6 | Fuyard | 1-6 | 4 | 10 |
| 7 | Equilibre | 1-8, distPref=6 | _ | 6 |
| 8 | Equilibre | 1-8, distPref=12 | _ | 8 (clamp porteeMax) |
| 9 | Equilibre | 1-8, distPref=0 | _ | 1 (clamp porteeMin) |

### `ScorePositionCombat.ScoreCelluleAvance`

| # | Setup | Attendu |
|---|-------|---------|
| 10 | Cell pile à distIdeale + LOS OK + portée OK | score minimum |
| 11 | Cell distIdeale-3 + LOS OK + portée OK | +30 (W1=10×3) |
| 12 | Cell distIdeale + sort LOS + LOS bloquée | +1000 (W2) |
| 13 | Cell à 12 + porteeMax=8 (hors) | +500×4 (W3=500/case) |
| 14 | Cell à 0 + porteeMin=1 (hors) | +500 |
| 15 | 2 cells score égal → tie-break sommeDist | meilleure = sommeDist optimal |

### `MoteurTactique.CalculerMeilleureCellule`

| # | Scénario | Résultat attendu |
|---|----------|------------------|
| 16 | Cra portée 1-8, dist 8, ennemi 3 PM | recul à dist 11 |
| 17 | Cra portée 1-8, dist 4, ennemi 0 PM | recul à dist 11 (conservatif) |
| 18 | Iop CAC dist 5, ennemi 0 PM | avance à dist 1 |
| 19 | Cra portée 6-12, dist 2 (sous portéeMin) | recul à dist 6 ≤ d ≤ 12 |
| 20 | Sort LOS bloquée par allié sur trajectoire | choisit cell alternative |
| 21 | Multi-mobs 3 ennemis, Fuyard | maximise Σdist + respecte distMinEloigne |
| 22 | Aucune cellule ne fait mieux (déjà optimal) | retourne null (exigeAmelioration=true) |

## Tests d'intégration (combats simulés)

### Scénario A — Cra solo vs Bouftou

**Setup** :
- Map 10°×10°
- Cra cell 100, Bouftou cell 108 (dist=8)
- Cra : sort Flèche Magique portée 1-10, PA=6 PM=3
- Bouftou : PM=3

**Tour Cra attendu** :
1. Cast Flèche Magique à dist=8 (OK)
2. Repositionner fin tour : dist idéale = 10+3 = 13
3. Recul de PM=3 → arrive à dist=11 (max possible)
4. Tour suivant : Bouftou avance 3 → dist=8 → Cra re-cast immédiat

**Tests** :
- [TACTIC] log doit indiquer `distIdéale=13`
- Chemin envoyé doit s'éloigner du Bouftou
- Pas de cast hors portée

### Scénario B — Iop solo vs Bouftou

**Setup** :
- Iop cell 200, Bouftou cell 208 (dist=8)
- Iop : sort Épée Divine CAC (portée 1-1) + Pression (1-3)
- Iop PA=6 PM=3

**Tour Iop attendu** :
1. Bond ou pré-move : avance vers Bouftou
2. Arrive à dist=5 (3 PM consommés)
3. Cast Pression à dist=5 si en portée (sinon skip)
4. Repositionne fin tour : avance ce qui reste

**Tests** :
- distIdéale=1 (CAC)
- ScoreCelluleAvance pénalise les cells loin de Bouftou
- Trajet pathfinder n'évite pas l'ennemi (on veut le coller)

### Scénario C — Cra + Sadida vs 3 mobs

**Setup** :
- Cra cell 50 (dist 8 du mob 1)
- Sadida cell 150 (dist 6 du mob 1)
- 3 mobs : cell 100 (Bouftou), cell 110 (Pichon), cell 120 (Tofu)
- Sadida en Mode=Equilibre dist=6, sort Ronce 1-8

**Tours attendus** :
- Cra Eloigne : recul, maximise Σdist
- Sadida Equilibre : stabilise à dist 6 du Bouftou
- Multi-mobs : Σdist prend en compte les 3

**Tests** :
- ScorePositionCombat.SommeDistances retourne valeur correcte
- Cra ne se rapproche pas du Pichon en s'éloignant du Bouftou
- Sadida cast Ronce sur Bouftou (le + proche)

### Scénario D — Eni avec allié blessé

**Setup** :
- Eni cell 100, allié cell 110 PV=40/100, ennemi cell 120
- Eni : Mot de Soin portée 1-6

**Tour Eni attendu** :
1. DECIDEUR voit allié PV < 60% → Mot de Soin priorité 1
2. Cast Mot de Soin sur allié
3. Si PA restants : cast offensif

**Tests** :
- Focus AllieLePlusBlesse résout vers le bon allié
- CiblePvInfPourcent=80 condition validée
- Pas de cast offensif si soin en attente

### Scénario E — LOS bloquée par allié

**Setup** :
- Cra cell 50, allié cell 55 (sur la trajectoire), ennemi cell 60
- Sort Flèche Magique nécessite LOS

**Tour Cra attendu** :
1. Cellule actuelle : LOS bloquée par allié
2. MoteurTactique pénalise cell actuelle (W2=1000)
3. Cherche cell latérale qui contourne l'allié
4. Si trouvée : déplace, puis cast

**Tests** :
- TesterLos retourne false depuis cell 50 vers cell 60
- Cells latérales (haut/bas) sans allié = LOS OK
- Score cell latérale < score cell 50

### Scénario F — Sort hors portée min

**Setup** :
- Cra cell 100, ennemi cell 103 (dist=3)
- Sort Flèche Punitive portée 6-12 (sort de classe haut niveau)

**Tour Cra attendu** :
1. dist 3 < portéeMin 6 → cast refusé
2. MoteurTactique : distIdeale (Eloigne) = 12+3 = 15
3. Recul max possible (PM=3) → arrive à dist=6
4. Cast Flèche Punitive à dist=6 = portéeMin (OK)

**Tests** :
- ScoreCelluleAvance pénalise (W3) dist=3 (hors portée min)
- Recul correctement calculé pour atteindre dist >= 6
- Cast valide à dist=6 ou plus

### Scénario G — Multi-cast par tour

**Setup** :
- Cra PA=12 (boost Tirelire), sort 4 PA portée 1-10
- 3 ennemis à dist 5, 7, 9

**Tour Cra attendu** :
- 3 casts (1 par ennemi) ou 3 casts sur même ennemi selon Focus
- PA finaux = 0

**Tests** :
- Boucle multi-cast tourne 3 fois
- Compteur NombreParTour respecté si configuré
- PA décrémentés correctement

## Mocking nécessaire

Pour ces tests d'intégration, créer un setup factice :
- `Carte` mock (10×10, toutes cells marchables, pas d'obstacles)
- `Combat` mock avec ennemis fictifs (CombattantMonstre, PV/PA/PM configurables)
- `MembreHeros` mock avec sorts pré-injectés
- `SessionProxy` mock qui capture les paquets envoyés (assertions)

## Conclusion

**24 cas de test spécifiés**, couvrant :
- Fonctions unitaires (1-15)
- Moteur tactique (16-22)
- Intégration end-to-end (A-G = 7 scénarios)

À implémenter dans une session dédiée (estimation 2-3h pour les
unitaires, +2h pour les intégrations avec mocks).
