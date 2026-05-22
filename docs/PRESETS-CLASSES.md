# Presets de combat par classe — Dofus Retro 1.29

**Date** : 2026-05-22
**Source** : `Divers/MultiAccount/ServiceConfigsHeros.cs:PresetParClasse`
**Couverture** : 12 classes Dofus Retro 1.29 (Feca → Pandawa)

## Application

Trois moyens d'appliquer un preset :
1. **Auto au chargement** : nouveau membre lié sans `ConfigCombat` → preset
   appliqué automatiquement par `ServiceConfigsHeros.HydrateMembre`.
2. **Manuel UI** : onglet Combat → bouton « ✨ Préset auto (classe du perso) »
   → détecte la classe du perso édité et applique le preset officiel.
3. **JSON édition** : modifier directement `peleas/heros/<id>.json` ou
   `peleas/<compte>.json` (master).

## Tableau des presets

| ID | Classe | Mode | DistPref | Sorts principaux |
|----|--------|------|----------|------------------|
| 1 | Feca | Equilibre | 4 | Aveuglement, Attaque Naturelle, Glyphe Aveuglant, Armure Terrestre |
| 2 | Osamodas | Equilibre | 5 | Invoc Bouftou (T1), Crapaud, Plume Karnage, Fouet |
| 3 | **Enutrof** | **Eloigne** | **8** | **Lancer de Pièces, Lancer de Pelle, Sac Animé (T1)** |
| 4 | Sram | Agressif | 1 | Attaque Mortelle (CAC), Coup Sournois, Pelle Spectrale, Invisibilité (<40% PV) |
| 5 | Xelor | Eloigne | 7 | Aiguille, Foudre, Téléportation (<50% PV), Démotivation |
| 6 | Ecaflip | Equilibre | 4 | Roulette, Pile ou Face, Réflexes (T1) |
| 7 | Eniripsa | Equilibre | 5 | Mot de Soin (<80%), Mot Curatif (<60%), Mot d'Épine, Voile Plumes |
| 8 | Iop | Agressif | 1 | Épée Divine (CAC), Pression, Bond, Concentration (T1) |
| 9 | **Cra** | **Eloigne** | **10** | **Flèche Magique, Empoisonnée, Cinglante, Punitive, Recul (<50%)** |
| 10 | **Sadida** | **Equilibre** | **6** | **La Folle (T1), Ronce, Poison Paralysant (cd 3), Ronce Apaisante (heal)** |
| 11 | Sacrieur | Agressif | 1 | Châtiment, Folie Sanguinaire (CAC), Attirance, Sacrifice (<40%) |
| 12 | Pandawa | Equilibre | 4 | Explosion Pandawesque, Karcham, Vulnérabilité, Souffle Alcoolisé (CAC) |

## Justifications par classe

### 1 — Feca (tank)
Stratégie défensive. Distance moyenne car pas de gros DPS distance.
Aveuglement = priorité (debuff puissant), Armure Terrestre en T1 si dispo.

### 2 — Osamodas (invocateur)
Premier tour : sort 33 (Invocation Bouftou) pour avoir un tank dès T1.
Ensuite Crapaud + Plume Karnage + Fouet pour DPS multi-élément.

### 3 — Enutrof (kite kamas)
Mode Eloigne car le sort 51 Lancer de Pièces a une portée modulable
jusqu'à 11+ au niveau max. Distance préférée 8 = équilibre entre safety
et damage. Sac Animé invoqué T1 pour aggro / mob secondaire.

### 4 — Sram (assassin CAC)
Mode Agressif strict. Attaque Mortelle CAC seulement (SeulementCAC=true).
Invisibilité auto-cast quand PV < 40% pour escape.

### 5 — Xelor (contrôle temporel)
Mode Eloigne distance 7. Aiguille retire des PM à l'ennemi (clé pour le
kite). Téléportation auto-cast si PV < 50% pour s'extraire d'un piège.

### 6 — Ecaflip (RNG DPS)
Mode Equilibre. Roulette + Pile ou Face multi-cast. Réflexes en T1.

### 7 — Eniripsa (soin/support)
Mode Equilibre, distance 5. Priorité absolue Mot de Soin (allié < 80%).
Mot d'Épine en offensif quand pas d'allié à soigner.

### 8 — Iop (DPS CAC)
Mode Agressif. Épée Divine CAC + Pression à distance. Bond pour
positionnement (atterrissage adjacent à un ennemi). Concentration T1.

### 9 — Cra (DPS distance kite)
**Mode Eloigne distance 10** (= portée max Flèche Punitive niv 5).
KITE INTELLIGENT ACTIVÉ via ADR-008 :
- Si ennemi 3 PM, le Cra s'arrête à 10+3=13
- Au tour suivant, ennemi arrive à 10 → Cra re-cast sans être tacle
Recul auto-cast si PV < 50% pour escape critique.

### 10 — Sadida (DoT + invocations + soin)
**Master Beiloddurul utilise ce preset si pas de config dans peleas/<compte>.json**.
- La Folle T1 (invocation principale, sort 182)
- Ronce niv 5 = 1-8 portée, DPS principal
- Poison Paralysant cooldown 3 tours (sort à effet retardé qui retire 4 PA)
- Ronce Apaisante en heal sur allié < 60% PV

### 11 — Sacrieur (tank DPS CAC)
Mode Agressif. Châtiment + Folie Sanguinaire CAC. Attirance pour rapprocher
l'ennemi le plus fort. Sacrifice pour récupérer PV d'un allié bas (< 40%).

### 12 — Pandawa (tank + portage)
Mode Equilibre. Explosion Pandawesque distance + Karcham proximité.
Vulnérabilité 1x par tour. Souffle Alcoolisé pour push CAC.

## Customisation

L'user peut ensuite raffiner via l'UI Combat :
- Onglet Combat → sélectionner perso dans ComboBox haut
- Changer Mode / DistancePref / sliders
- Ajouter/retirer/réorganiser règles dans le DataGrid
- Activer conditions (PV%, PremierTour, Cooldown, etc.)

La sauvegarde est auto-debouncée toutes les 800ms (cf. ADR-001).
