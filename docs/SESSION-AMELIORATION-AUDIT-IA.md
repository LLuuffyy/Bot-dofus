# SESSION — Audit IA combat (statut réel)

Source : `Divers/Combats/IA/*`, `Commun/Frames/TrameJeu.cs`, `Divers/MultiAccount/*`, `Divers/Cartes/LigneVisuelle.cs`.

## 1. Kite intelligent (porteeMax + PM_ennemi) — OK

`ScorePositionCombat.DistanceIdeale` (ScorePositionCombat.cs:156-175) applique exactement la formule. Et le contexte est correctement injecté côté master (`TrameJeu.cs:2019` → `PmEnnemiCible: cible.PM`) et côté héros (`IACombatHerosSimple.cs:233` → `PmEnnemiCible: ciblePrincipale.PM`).

Garde-fou : si `PmEnnemiCible <= 0`, fallback hardcodé à `3` (ScorePositionCombat.cs:170). Risque : si le serveur ne fournit pas `Combattant.PM` pour un mob (champ pas peuplé via GTM/GTS), on tombe systématiquement sur 3 sans s'en rendre compte. À vérifier au runtime sur Hystoria (logs `pmEnnemi=X` dans le tag `[TACTIC]`).

## 2. Portée minimum (PorteeMin) — OK

`MoteurReglesCombat.cs:184` : `if (dist < porteeMin) continue;` rejette correctement les casts en-dessous de la portée min. La règle s'applique avant LOS et avant compteurs cible.

Cas spécial `PorteeMin=0 PorteeMax=0` : aucun sort de la table dyshay n'a `RANGO_MINIMO=0 RANGO_MAXIMO=0`. Sacrifice Poupesque (198) est `1-1` (CAC). Tremblement (6526) est rangé en CAC aussi. → Aucun sort « self-cast portée=0 » dans la base, le test `dist < porteeMin` reste sain.

Gap : pas de gestion du flag `RANGO_MODIFICABLE` (boost portée par buffs) ; sorts à portée +1/+2 via stuff ne sont pas reconnus.

## 3. LOS Bresenham — PARTIAL

`LigneVisuelle.cs` :
- Surcharge `(a, b, ISet<int>)` : ligne 104-106, **retourne toujours `false`** (LOS OK) — c'est explicite dans le code (« V1 simplifiée »).
- Surcharge `(carte, a, b, ISet<int>)` : ligne 113-167, teste correctement les **combattants occupants** via lookup x,y.

L'utilisation effective passe par la surcharge `carte`-aware (cf. `MoteurReglesCombat.cs:218`, `TrameJeu.cs:2104`, `IACombatHerosSimple.cs` testLos), donc OK pour les combattants. **Murs/decor MapData non implémentés** — c'est documenté comme TODO ligne 19. Impact : sorts NécessiteLOS=true cast à travers murs → GAF serveur → tour perdu sur cartes avec obstacles statiques (très rare en zone Astrub / extérieur).

## 4. Décideur multi-cast — OK

- `MaxCastsParTour = 8` (const) :
  - `IACombatHerosSimple.cs:46` puis boucle `while (castsEffectues < MaxCastsParTour)` ligne 120.
  - `TrameJeu.cs:1324` `const int MAX_CASTS_PAR_TOUR = 8;` puis boucle ligne 1325.
- Compteur clé `(idCaster, idSort)` correctement cloisonné par caster — `Combat.cs:37`, incrément `TrameJeu.cs:2144-2145` et `IACombatHerosSimple.cs:531-532`, lecture `MoteurReglesCombat.cs:91`.
- `NombreParCible` clé `(idCaster, idSort, idCible)` géré pareil (`MoteurReglesCombat.cs:166-172`).

Bonus subtilité : en cas de refus ANTI-BAN (cast hors portée ou LOS bloquée détectée APRÈS choix de règle), le compteur est gonflé à `max(1, NombreParTour)` pour éviter re-essai de la même règle (`TrameJeu.cs:2086-2088`).

## 5. Presets classe — PARTIAL

Les 12 presets sont présents (`ServiceConfigsHeros.cs:179-380`) : 1 Feca, 2 Osa, 3 Enu, 4 Sram, 5 Xelor, 6 Eca, 7 Eni, 8 Iop, 9 Cra, 10 Sadida, 11 Sacri, 12 Panda + default.

Sadida preset (classe 10, ligne 325-338) contient : `182 (La Folle), 183 (Ronce), 200 (Poison Paralysant), 192 (Ronce Apaisante)`. **MANQUE** pour Beiloddurul niv 13 :
- `193` La Bloqueuse
- `195` Larme
- `198` Sacrifice Poupesque

Note ligne 323 : « pour le master Beiloddurul, la config est dans `peleas/<compte>.json` édité par l'user ». Donc le preset ne s'applique qu'aux héros liés Sadida — acceptable mais sous-optimal pour un user qui ne touche pas au JSON.

Autres presets : IDs invocations ou sorts haut-level (ex. 51 Lancer Pièces niv 1, 161 Flèche Magique) ne sont pas filtrés par niveau perso → si un héros niv 5 a le preset Cra, il tente Flèche Cinglante (169) qui n'est pas appris → rejeté par `Evaluer` ligne 81 (sort non appris). Pas un bug, mais bruit log.

## 6. Chaîne `Evaluer` — OK

Ordre vérifié `MoteurReglesCombat.cs:67-223` :
1. Tri `OrderByDescending(r => r.Priorite)` ligne 67 ✓
2. Sort connu + appris ✓
3. **`NombreParTour` consulté AVANT choix cible** (ligne 88-94) ✓ — évite over-cast d'une règle déjà saturée
4. Cooldown multi-tours ✓
5. Conditions Joueur (PV%, invoc présente) ✓
6. Conditions Situation (ennemis min/max, PremierTour, APartirDuTour, TousLesNTours) ✓
7. PA dispo (au niveau appris) ✓
8. Focus → cible (ligne 148 `ChoisirCible`), avec overrides `CiblePlusFaible/Forte` ✓
9. `NombreParCible` après cible choisie (ligne 166-172) ✓
10. Conditions Cible (PV%) ✓
11. Portée min/max au niveau appris ✓
12. Distance/Méthode/IgnorerCAC/SeulementCAC ✓
13. LOS si requise ✓

Le mapping Focus → cible inclut tous les nouveaux focus (Moi, AllieLePlusBlesse, InvocationLaPlusProche, CelluleVide, CelluleAdjacenteEnnemi/Moi). Heuristique `EnnemiLePlusProche` raffinée pour préférer mob NON-invocation low-HP dans le radar 5 cases.

---

## TOP 3 IA IMPROVEMENTS

1. **LOS murs MapData** — actuellement les sorts NécessiteLOS=true passent à travers les murs/arbres statiques. Décoder le bit LOS de chaque cellule à partir de `Carte.DonneesBrutes` (cf. BIBLE-CADERNIS-V2 §1) et le tester dans `LigneVisuelle.EstObstruee`. Élimine la classe entière des « cast OK côté bot, GAF côté serveur ».
2. **Preset Sadida complet (193/195/198)** — ajouter La Bloqueuse, Larme et Sacrifice Poupesque au preset classe 10 pour qu'un Sadida niv 13 sans config user ait une rotation complète out-of-the-box.
3. **Détection runtime `PmEnnemiCible=0`** — si plusieurs mobs renvoient PM=0 sur GTM, le fallback hardcodé 3 fausse le kite. Logger un warning quand `ctx.PmEnnemiCible<=0` et tracker via `[INTELLIGENCE]` ; si récurrent, brancher un mapping `Monstre.IdTemplate → PM` statique depuis `monsters.json`.
