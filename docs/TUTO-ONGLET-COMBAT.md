# Tuto onglet Combat — modes, configuration, IA

Guide pratique pour configurer l'IA combat du master ET des héros liés
sur Abrak/Hystoria. Lis ça AVANT d'ouvrir l'onglet Combat — ça t'évitera
de tâtonner.

## 1. Configurer pour : choisir le perso à éditer

En haut de l'onglet : ComboBox **« Configurer pour »** avec avatar
coloré par classe. Tu peux basculer entre :

- **Master (LEADER, badge or)** — ton perso connecté (Sadida).
  Sauvegarde dans `peleas/<id-compte>.json`.
- **Chaque héros lié (LIÉ, badge bleu)** — un Enutrof.
  Sauvegarde dans `peleas/heros/<idJeu>.json`.

Le résumé sous le combo indique le fichier de sauvegarde courant. Toute
modification (rotation, mode, slider) sauvegarde **automatiquement** dans
le bon fichier en moins d'une seconde (debounce 800ms).

## 2. Le ModeCombat — comment le perso bouge

Le **ModeCombat** détermine ce que fait le perso quand il a fini sa
rotation et qu'il a encore des PM, et où il essaie de se positionner
pour caster.

### 2.1 Agressif

> Court au mob et bourrine au CAC.

- Cherche en priorité la cellule de cast **la plus proche** de l'ennemi.
- Si aucun sort ne peut être lancé : **se colle au mob** (cellule
  adjacente) en consommant tous les PM.
- Cas d'usage : Iop, Sadida avec poupées CAC, build « tank-DPS ».

### 2.2 Equilibre (recommandé par défaut)

> Vise `DistancePreferee` mais priorise toujours le cast.

- Cherche la cellule de cast la **plus proche de `DistancePreferee`**.
- Si aucun sort ne porte : **reste sur place** (préserve les PM).
- Règle d'or implémentée : mieux vaut taper à 5 cases qu'attendre à 7
  sans rien lancer.
- Cas d'usage : la plupart des persos, le mode par défaut sensé.

### 2.3 Eloigne

> Kite — reste à la portée max du sort, repousse.

- Cherche la cellule de cast la **plus loin** de l'ennemi (toujours
  dans portée).
- Si aucun sort ne porte : **fuit** au max des PM (éloignement).
- Cas d'usage : Cra, Enutrof Lancer de Pièces, kiteurs.

### 2.4 Fuyard

> Pareil que Eloigne mais avec un seuil PV.

- Identique à Eloigne en théorie.
- (V1) skip cast si PV < `SeuilFuitePv` — pas encore branché dans l'IA
  des liés, prévu Phase G.
- Cas d'usage : perso fragile, build glass-cannon.

### 2.5 PasDeDeplacement

> N'utilise jamais les PM. Cast s'il peut, sinon `Gt` direct.

- Cas d'usage : ne pas exposer un perso au CAC, position fixée.

## 3. La StrategieCombat — focus de cible & priorisation

La **StrategieCombat** affecte plutôt **qui** le perso vise et avec
quels biais. Pour l'IA des liés (V1), c'est moins pris en compte que le
mode — la cible est l'ennemi le plus proche. Ces stratégies vont
gagner en effet à mesure que `Phase G` (refactor IA complet) avance.

| Stratégie | Effet attendu |
|-----------|---------------|
| **Tactique** | Équilibre cast/déplacement. Défaut sensé. |
| **Agressif** | Focus dégâts max, ignore le risque. |
| **Defensif** | Priorise sa propre survie (boucliers, soins). |
| **Soutien** | Boost/soigne les alliés, attaque en dernier. |
| **Passif** | Ne fait rien d'autre que `Gt` (mode observation). |
| **Fugitif** | Fuit toujours, ne cast que sous contrainte. |

## 4. Les sliders Distance — comment ça interagit avec le mode

Trois sliders importants :

### 4.1 DistancePreferee

Utilisé par **Equilibre**. Distance Chebyshev visée par rapport à
l'ennemi. Si le sort considéré a portée [2,8] et `DistancePreferee=4`,
l'IA choisit la cell la plus proche de 4 cases dans cette plage.

Valeur typique : **3-5** pour mêlée, **6-8** pour distance.

### 4.2 DistanceMinEloigne

Utilisé par **Eloigne** / **Fuyard**. Distance minimale absolue qu'on
veut maintenir avec l'ennemi.

Valeur typique : **5-7** pour un Cra, **4** pour un Enutrof
(Lancer de Pièces porte à 1-6 cases).

### 4.3 SeuilFuitePv

Utilisé par **Fuyard**. Pourcentage de PV en-dessous duquel le perso
arrête de cast et fuit.

Valeur typique : **30** (= 30% PV).

## 5. La rotation de sorts — où la magie opère

La **rotation** est la liste ordonnée de règles de sorts. Chaque tour,
l'IA itère par **Priorité décroissante** et lance le premier qui
matche tous les critères (sort connu, PA dispo, portée+LOS OK,
NombreParTour respecté).

### 5.1 Ajouter un sort

1. Sélectionne le perso dans le ComboBox du haut.
2. Dans le panel « Sorts appris » : double-clique le sort à ajouter
   (ou bouton « + »).
3. Une règle apparaît dans la rotation à droite avec des valeurs par
   défaut.

### 5.2 Champs d'une règle

- **Priorité** (1-10) : plus c'est haut, plus le sort est essayé tôt
  dans le tour. Exemple Enutrof : Lancer de Pièces=10, Lancer de
  Pelle=8, Sac Animé=5.
- **NombreParTour** : combien de fois max ce sort peut être lancé dans
  le tour. Mettre 99 pour pas limiter.
- **Focus / Cible** : qui viser (Plus Faible / Plus Fort / Plus Proche
  / Moi-même / etc.).
- **MethodeLancement** : CAC / Distance / Les Deux (filtre la portée
  effective).
- **Conditions** (PV%, distance, situation, etc.) : 18 conditions
  optionnelles. Si une ne matche pas, on passe à la règle suivante.

### 5.3 Preset par classe

Quand un nouveau lié arrive sans config persistée, un preset par
défaut est appliqué. Aujourd'hui :

- **Enutrof (classe 3)** : Mode=Eloigne, distance 4, règles
  51 (Lancer de Pièces) > 41 (Lancer de Pelle) > 43 (Sac Animé).
- **Autres classes** : Mode=Equilibre, distance 3, rotation vide
  (à toi de remplir).

## 6. Workflow recommandé pour les liés

1. Connecte-toi en mode actif sur Abrak.
2. Onglet Groupe — vérifie que les 7 Enu apparaissent avec leurs
   sorts (« Sorts : #51 niv4, #41 niv1, ... »).
3. Click « Éditer » sur Aerawiol → onglet Combat ouvre avec
   Aerawiol sélectionné dans le ComboBox.
4. Vérifie la rotation (preset Enutrof appliqué). Ajuste les
   priorités si besoin.
5. Choisis le ModeCombat (Eloigne pour kiter, Agressif pour CAC).
6. Règle `DistancePreferee` ou `DistanceMinEloigne` selon mode.
7. Sauvegarde auto à chaque clic (badge « ✓ Enregistré HH:mm:ss »).
8. Recommence pour les 6 autres Enu (ou utilise un bouton « Copier
   config » — fonctionnalité à venir).

## 7. Test en combat réel

1. Toggle mode actif (décoche « Mode passif » dans l'header).
2. Engage un combat.
3. Quand c'est le tour d'un Enu : tu vois dans les logs
   `[IA-HEROS:Aerawiol] cast #51 niv4 sur cell X (direct...)` ou
   `[IA-HEROS:Aerawiol] déplacement 256→299 (3 PM) puis cast #51...`.
4. Si rien ne se passe : check `[IA-HEROS:Aerawiol] Aucun sort connu`
   ou `ConfigCombat vide` ou `Aucune cible accessible` → corrige.

## 8. Limites actuelles (Phase F V1) et roadmap

| Aspect | Statut |
|--------|--------|
| Pathfinding A* avec PM | ✅ |
| LoS Bresenham iso (sort `NecessiteLOS`) | ✅ |
| Choix cellule de cast selon mode | ✅ |
| Multi-cast par tour (rotation complète) | ✅ |
| Fallback approche selon mode si rien à cast | ✅ |
| Conditions de règle (PV%, distance, etc.) | ⬜ (V2) |
| Focus de cible (Plus Faible / Plus Fort) | ⬜ (V2) |
| Réutilisation MoteurReglesCombat du master | ⬜ (V3 refactor) |
| Bouton « Copier config » entre persos | ⬜ (V2 UI) |

## 9. Fichiers de persistance

- `peleas/<id-compte>.json` — config master (Sadida).
- `peleas/heros/<idJeu>.json` — config par héros lié (Enutrof…).
- `multi-account/<id-compte>.json` — liste noms héros à inviter +
  toggle AutoInvitationActive.

Tu peux éditer ces fichiers à la main si tu veux pour des tweaks pas
exposés dans l'UI (ex: paliers de seuils fins). L'UI les recharge à
l'ouverture de l'onglet Combat.

## 10. Boutons du header onglet Groupe

- **Auto-invitation** (checkbox) : persiste dans
  `multi-account/<id>.json:AutoInvitationActive`. À la prochaine
  connexion en jeu, le bot envoie automatiquement `PI<nom>` pour
  chaque héros listé.
- **Inviter maintenant** : force la procédure d'invitation tout de
  suite (même si toggle off).
- **Config groupe** : ouvre `multi-account/<id>.json` dans Notepad
  pour éditer la liste des noms.
