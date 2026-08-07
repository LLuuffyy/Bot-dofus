# 📖 BIBLE CADERNIS V3 — Classes / Sorts / Forgemagie / Donjons / Interactifs

> **Suite de BIBLE-CADERNIS V1+V2**. Focus sur les sujets "gameplay" Dofus Retro :
> classes, sorts par classe, forgemagie, donjons, objets interactifs.
>
> Note : ces sujets sont **peu couverts sur cadernis** (majorité D2.0/Touch). Cette
> bible combine les rares threads cadernis Retro + le savoir Dofus Retro standard
> (wiki, jeu, sources désobfusquées dofedex) + notre XML dyshay.

---

## 📑 SOMMAIRE V3

1. [Les 12 classes Dofus Retro](#-1-les-12-classes-dofus-retro)
2. [Sorts par classe — vue d'ensemble](#-2-sorts-par-classe)
3. [Stats / Caractéristiques](#-3-stats--caractéristiques)
4. [Forgemagie — règles et formules](#-4-forgemagie)
5. [Donjons Retro principaux](#-5-donjons-retro-principaux)
6. [Capture d'âmes (Sram)](#-6-capture-dâmes)
7. [Objets interactifs — table complète gfx](#-7-objets-interactifs--table-gfx)
8. [Métiers — détails Retro](#-8-métiers--détails-retro)
9. [Dragodindes / montures](#-9-dragodindes--montures)
10. [Pets / familiers](#-10-pets--familiers)
11. [HDV / commerce / banque](#-11-hdv--commerce--banque)
12. [PNJ / dialogues](#-12-pnj--dialogues)
13. [Guildes / alliances / familiarités](#-13-guildes--alliances)
14. [Anti-bot Ankama — vision détaillée](#-14-anti-bot-ankama--vision-détaillée)
15. [Sources externes recommandées](#-15-sources-externes-recommandées)

---

## ⚔ 1. Les 12 classes Dofus Retro

### Table récap (1.29)
| Classe | Élément principal | Style | PA/PM start | Difficulté |
|--------|-------------------|-------|------|---|
| **Iop** | Force | DPS mêlée brut | 6/3 | Facile |
| **Cra** | Agilité | DPS distance | 6/3 | Facile |
| **Sadida** | Intelligence/Chance | Invocateur + DPS | 6/3 | Moyen |
| **Sram** | Chance | Furtif / pose pièges | 6/3 | Difficile |
| **Eniripsa** | Intelligence | Soigneur | 6/3 | Moyen |
| **Féca** | Intelligence | Tank/protecteur | 6/3 | Moyen |
| **Xélor** | Chance | Vol PA | 6/3 | Difficile |
| **Enutrof** | Chance | Loot/butin (PO) | 6/3 | Moyen |
| **Ecaflip** | Force/Chance | Aléatoire DPS | 6/3 | Facile |
| **Sacrieur** | Force/Vita | Tank PV | 6/3 | Moyen |
| **Pandawa** | Force | Manipulation positionnement | 6/3 | Difficile |
| **Osamodas** | Chance/Force | Invocations monstres | 6/3 | Moyen |

### IDs de classe (paquet `ASK|...|<classe>|...`)
```
1 = Feca
2 = Osamodas
3 = Enutrof
4 = Sram
5 = Xelor
6 = Ecaflip
7 = Eniripsa
8 = Iop
9 = Cra
10 = Sadida          ← TON PERSO Beiloddurul
11 = Sacrieur
12 = Pandawa
```

### Spécialités Hystoria (custom)
Hystoria a des sorts custom au-delà des 281 standard dyshay (notre XML).
Le JSON `spells_stats_hystoria.json` (440 KB, 2145 entrées) contient les variants.

---

## ✨ 2. Sorts par classe

### Sadida (TA CLASSE — Beiloddurul) — IDs sorts standards 1.29
| ID | Nom | PA niv1 | Range niv1 | Cible | Note |
|----|-----|---------|-----------|-------|------|
| 181 | Tremblement | 4 | 1-4 | ennemi | Force, dégâts vol PA |
| **183** | **Ronce** | **5 (niv5=4)** | **1-6 (niv5=1-8)** | **ennemi** | **TON DPS principal** |
| 184 | Feu de Brousse | 4 | 1-1 | zone | Intel, AOE |
| 185 | Herbe Folle | 4 | 0-6 | ennemi | Chance, vol PV |
| 186 | Arbre | 6 | 1-1 | case vide | Invoc tank |
| 187 | La Surpuissante | 5 | 1-1 | case vide | Invoc DPS poupée |
| 188 | Ronce Insolente | 5 | 0-3 | ennemi | AOE 1 case |
| 189 | La Sacrifiée | 4 | 1-1 | case vide | Invoc kamikaze |
| 190 | La Gonflable | 5 | 1-1 | case vide | Invoc tank gros PV |
| 191 | Ronces Multiples | 4 | 0-3 | zone ennemis | AOE losange |
| 192 | Ronce Apaisante | 2 | 1-4 | ennemi/allié | Heal + retire PM |
| 193 | La Bloqueuse | 4 | 1-1 | case vide | Invoc bloque (pas dégâts) |
| 194 | Ronces Agressives | 4 | 1-4 | ennemi | AOE croix |
| 195 | Larme | 5 | 1-3 | ennemi | Eau, soin si tue |
| 196 | Vent Empoisonné | 5 | 1-5 | ennemi | Air, poison continu |
| 197 | Puissance Sylvestre | 3 | 0-0 | soi | Buff dégâts |
| 198 | Sacrifice Poupesque | 6 | 1-1 | poupée | Sacrifie poupée = soin |
| 199 | Connaissance des Poupées | 1 | 0-0 | soi | Buff invocations |
| 200 | Poison Paralysant | 3 | 1-2 | ennemi | Feu, dégâts si PA utilisé |

### Iop — DPS mêlée
| ID | Nom | Type |
|----|-----|------|
| 0 | Coup de Poing | corps-à-corps |
| 8 | Retour du Bâton | renvoi |
| 9 | Bond | déplacement |
| 16 | Pression | DPS Force, 4 PA, range 1-5 |
| 17 | Tempête de Puissance | AOE |
| 18 | Concentration | buff Force |
| 19 | Compulsion | DPS |
| 20 | Intimidation | repousse ennemi |
| 21 | Mutilation | DPS critique |
| 22 | Vitalité | buff PV |
| 23 | Colère de Iop | DPS gros |

### Cra — DPS distance
| ID | Nom | Type |
|----|-----|------|
| 161 | Flèche Magique | base |
| 162 | Flèche Glacée | Eau |
| 163 | Flèche Enflammée | Feu |
| 164 | Flèche Empoisonnée | poison |
| 165 | Tir Critique | crit garanti |
| 166 | Flèche Repoussante | knockback |
| 167 | Œil de Taupe | buff vision |
| 168 | Flèche Aspirante | vol vie |
| 169 | Tir Lourd | range courte gros dégâts |
| 170 | Flèche Plombée | retire PA |
| 171 | Tir Mortel | DPS finisseur |

### Sram — Furtif / pièges
| ID | Nom | Type |
|----|-----|------|
| 76 | Invisibilité | cache |
| 77 | Peur | déplace ennemi |
| 78 | Piège Cubique | piège AOE |
| 79 | Piège Mortel | piège DPS |
| 80 | Piège de Masse | piège anti-positionnement |
| 81 | Piège d'Immobilisation | piège retire PM |
| 82 | Piège Empoisonné | poison continu |
| 83 | Piège d'Insignifiance | retire PA |
| 84 | Vol de Vie | DPS + heal |
| 85 | Possession | retire stats |
| 86 | Chance | buff crit |
| 87 | Pulsion de Chakra | déclenche tous pièges |
| **88** | **Capture d'âme** | **5 PA, capture âme mob mort** |

### Eniripsa — Soigneur
| ID | Nom | Type |
|----|-----|------|
| 91 | Mot Soignant | heal base |
| 92 | Mot Curatif | heal CC |
| 93 | Mot Stimulant | buff |
| 94 | Mot d'Épine | DPS contre |
| 95 | Mot Tourmentant | DPS |
| 96 | Mot de Reconstruction | gros heal |
| 97 | Mot Lénifiant | retire débuff |
| 99 | Mot Drainant | vol vie |
| 100 | Mot de Sacrifice | redirige dégâts |
| 101 | Mot de Régénération | heal sur N tours |
| 102 | Mot de Silence | retire sorts ennemi |

### Pour les autres classes (Féca, Xélor, Enutrof, Ecaflip, Sacrieur, Pandawa, Osamodas)
Voir notre **`Resources/data/hechizos_dyshay.xml`** (281 sorts × 6 niveaux complets) — tous les sorts standard 1.29 y sont avec PA/portée/effets par niveau.

---

## 📊 3. Stats / Caractéristiques

### IDs des stats (paquet `As`)
| ID | Stat | Notes |
|----|------|-------|
| 10 | Force | dégâts Terre, dégâts mêlée |
| 11 | Vitalité | PV (+1/point) |
| 12 | Sagesse | XP/résistance vol stats |
| 13 | Chance | dégâts Eau |
| 14 | Agilité | dégâts Air, esquive PA/PM |
| 15 | Intelligence | dégâts Feu, soin |

### Ordre des stats dans le paquet `As` Hystoria
```
As<sumXp>,<xpPalierCourant>,<xpPalierSuivant>|<kamas>|<ptsStats>|...
   |<initialPa>,<currentPa>,<maxPa>|...
   |<initialPm>,<currentPm>,<maxPm>|...
```
(format simplifié — varie selon serveur)

### Caractéristiques d'équipement (parsed depuis OS / OAK)
- `100..104` : Vitalité/Force/Intel/Chance/Agi par item
- `120..125` : dégâts Force/Eau/Feu/Air/Chance/Neutre
- `175` : PO max
- Voir `effects_hystoria.json` pour la table complète des IDs

---

## 🔨 4. Forgemagie

### Source : threads cadernis `forgemagie-donnees-pour-debuter.1898`, `calcul-forgemagie.2122`

### Principe Retro
- Un item a un **poids max** = somme des points possibles
- Chaque rune apportée a un **poids** (= effet apporté × multiplicateur)
- Le forgemagicien apporte des runes pour atteindre les stats voulues
- Risque : si surcharge, échec → perte des runes

### Poids des runes (Retro 1.29)
| Effect ID | Stat | Poids/point |
|-----------|------|---|
| 125 | Vitalité | 0.25 |
| 119 | Sagesse | 3 |
| 118 | Force | 1 |
| 123 | Intelligence | 1 |
| 122 | Agilité | 1 |
| 124 | Chance | 1 |
| 117 | PA | 100 |
| 116 | PM | 90 |
| 160 | Portée | 51 |
| 138 | Dommages | 30 |
| 178 | Pods | 0.5 |
| ... | ... | ... |

(Liste complète : voir Towzeur thread #1898 ou JSON `effects_hystoria.json`)

### Formules
- **Coût d'un effet** : `points × poids_unitaire`
- **Limite max poids item** : encodée dans le SWF item (`poids_max`)
- **Probabilité réussite** : dépend `(charge_actuelle / poids_max)`

### Exo (1 stat en plus du max)
- Cas spécial : tenter d'ajouter une stat alors que poids item dépassé
- Très faible % de réussite
- Mécanique custom Hystoria parfois (peut différer du 1.29 vanilla)

### Outils
- **EasyFM** (legacy 1.29) — simulateur
- **dofuspourlesnoobs.com/guide-forgemagie** — guide texte
- **Pas de bot FM Retro libre** — la plupart sont pour D2.0

---

## 🏰 5. Donjons Retro principaux

### Donjons low-level (niv 20-80)
- **Donjon Tofu** (niv ~20) : Tofu Royal — Vol de PA
- **Donjon Bouftou** (niv ~25) : Bouftou Royal — Bourrin physique
- **Donjon Champs des Ignés** (niv ~50) : Drhellzerker — Feu
- **Donjon Larve** (niv ~50) : Larve Royale — Poison
- **Donjon Famille Sanglante / Maître Corbac** (niv ~60-80) — variés

### Donjons mid-level (niv 80-150)
- **Donjon Squelette** (niv ~80) : Major Sgt Chouque
- **Donjon Wabbit** (niv ~80) : Wa Wabbit — heal +ennemis
- **Donjon Trade Royal Mosquito** (niv ~100)
- **Donjon Sphincter Cell** (niv ~100) : Sphincter Cell — Tank
- **Donjon Bworker** (niv ~100) : Bworker — chef
- **Donjon Korriandre** (niv ~100) : Soft Oak — Air
- **Donjon Kanigrou** (niv ~120) : Kanigrou — Sram

### Donjons high-level (niv 150+)
- **Donjon Kanigroule** (niv ~150) : Kanigroule
- **Donjon Skeunk** (niv ~150) : Skeunk — Force
- **Donjon Kralamour** (niv ~155) : Kralamour — Eau
- **Donjon Bworker** (niv ~155) : Bworker Glourséleste — boss
- **Donjon Kolosso** (niv ~180-200) : Kolosso — boss Pandala

### Mécaniques communes
- Salle 1-4 : mobs simples
- Salle 5 : boss + invocations
- Loot : items spécifiques + percepteurs si guilde
- Clé requise : forgéable par Bricoleur (Forgemagie/Maçonnerie)

### Sur Hystoria
- Donjons custom possibles (à vérifier sur le forum officiel)
- Loot rates parfois ajustés

---

## 👻 6. Capture d'âmes

### Sort spécifique Sram : ID 88 "Capture d'âme"
- Coût : 5 PA
- Portée : 1-1 (corps-à-corps avec le mob mort)
- Cible : monstre mort (cadavre)
- Effet : capture l'âme dans une "Pierre d'âme" en inventaire

### Pierres d'âmes
- Item template variable selon mob
- Utilisées pour invoquer le mob dans une arène d'élevage (DropDonjon)
- Mob possède un % "âme" (0-100%) lors du cast — proba capture = `âme%/100`

### Protocole
```
C→S : GA300 88 ; <cellMort>     ← cast capture sur cell du mob mort
S→C : (effet animé)
S→C : OAK<obj data>              ← ajout pierre d'âme inventaire
```
Pas spécifique à un paquet — c'est un sort comme un autre, juste sa cible est un cadavre.

### Bot capture
- Vérifier `Ennemi.EstMort == true` avant cast
- Reste à portée 1 du mob mort
- Cast → check OAK reçu pour confirmer succès

---

## 🌳 7. Objets interactifs — table gfx

### Notre catalogue interne (`CatalogueInteractifs.cs`)
53 ressources mappées gfx → skill métier compatible.

### Table principale (à valider sur le client Retro désobfusqué dofedex)
| gfx | Ressource | Skill primaire | Métier |
|-----|-----------|---|---|
| 7000 | Zaap | 114 (Hystoria) / 157 (vanilla) | tout le monde |
| 7001 | Zaapi | 121 | tout le monde |
| 7002 | Coffre Banque | 73 | tout le monde |
| 7100-7150 | Arbres divers | **45** | Bûcheron |
| 7200-7250 | Plantes | **68** (Cueillir) | Alchimiste |
| 7300-7320 | Minerai | **99** (Miner) | Mineur |
| 7400-7430 | Poissons | **89** (Pêcher) | Pêcheur |
| 7500-7510 | Bois variants | **45** | Bûcheron |
| 7511 | Blé | **50** (Faucher) | Paysan |
| 7512 | Orge | **50** | Paysan |
| 7513 | Lin | **68** OU **50** | Alchi OU Paysan |
| 7514 | Avoine | **50** | Paysan |
| 7515 | Houblon | **50** | Paysan |
| 7516 | Chanvre | **68** OU **50** | Alchi OU Paysan |
| 7517 | Seigle | **50** | Paysan |
| 7518 | Malt | **50** | Paysan |
| 7519 | Riz | **50** | Paysan |
| 7520 | Lin Doré | **68** | Alchi |
| ... | ... | ... | ... |

### Source de vérité
- `Resources/data/interactiveobjects_hystoria.json` (chez nous = 2 octets seulement, à compléter !)
- `Resources/data/skills_hystoria.json` (3.8 KB) — table skills custom Hystoria
- **Notre catalogue C# `CatalogueInteractifs.cs`** est le seul à jour pour 53 entrées

### Comment ajouter une nouvelle ressource
1. En mode passif, agresser un mob spécifique ou se mettre devant la ressource
2. Cliquer manuellement dessus dans le vrai client
3. Capturer le paquet `GA500<cell>;<skill>` envoyé par le client
4. Noter le gfx de la cell (via MapData déchiffrée) et le skill
5. Ajouter dans `CatalogueInteractifs.cs`

---

## 🛠 8. Métiers — détails Retro

### Liste des métiers Retro 1.29
| ID | Métier | Skill principal |
|----|--------|---|
| 1 | Bûcheron | 45 (Couper) |
| 2 | Paysan | 50 (Faucher) |
| 11 | Alchimiste | 68 (Cueillir) |
| 13 | Pêcheur | 89 (Pêcher) |
| 15 | Bijoutier | craft |
| 16 | Forgeron | craft armes |
| 17 | Sculpteur | craft armes bois |
| 18 | Cordonnier | craft bottes |
| 19 | Tailleur | craft cape, chapeau |
| 20 | Boulanger | craft pains |
| 26 | Boucher | craft viandes |
| 27 | Poissonnier | craft poissons |
| 28 | Mineur | 99 (Miner) |
| 31 | Chasseur | combats mobs |
| 32 | Sculpteur d'arc | craft arcs |
| 33 | Sculpteur de dagues | craft dagues |
| 34 | Sculpteur de baguettes | craft baguettes |
| 35 | Sculpteur de marteaux | craft marteaux |
| 36 | Bricoleur | craft potions/cadeaux |
| 37 | Mineur | 99 (Miner) |
| 38 | Sculpteur de pelles | craft pelles |
| 41 | Forgemage | FM armes |
| 42 | Joailler | FM bijoux |
| 43 | Cordomage | FM bottes |
| 44 | Costumage | FM cape/chapeau |
| 45 | Sculpteur d'arc | FM arcs |
| 50 | Faucheur | (alt name) |
| 53 | Bûcheron Élite | (Hystoria custom?) |

### Niveaux et XP
- Cap niveau métier : 100
- XP requise : ~10K niv 1 → ~10M niv 100
- Hystoria peut avoir des modifs (XP boost, etc.)

### Paquets métiers
```
S→C : JS|<id>;<skill~niv~xp>|<id>;...   ← liste métiers
S→C : JX|<id>;<niveau>;<xpActuel>;<xpPalier>|<id>;...   ← XP par niveau
S→C : JO<id>|<...>|<...>                ← options métier
S→C : JN<id>|<nouveauNiv>               ← level-up métier
```

### Multi-skill (53 ressources)
Voir `CatalogueInteractifs.SkillCompatible(gfx, skillsConnusPerso)` :
- Pour Lin (gfx 7513) : si perso connaît skill 68 (Alchi) → skill 68 ; sinon si connaît skill 50 (Paysan) → skill 50
- Logique : prendre LE PREMIER skill compatible dans la liste catalogue

---

## 🐴 9. Dragodindes / montures

### Spécificités Retro
- Dragodinde = monture rapide (pas de dragoturkey/muldo 2.0)
- Élevage : reproduction, dressage
- Stats : vitesse, endurance, etc.

### Paquets en jeu
- `Re|<color>` : reproduction
- `Rp...` : info dragodinde portée
- `Eq<itemId>;<position>` : équiper dragodinde (slot ITEM_DRAGOTURKEY)

### Sur Hystoria
- Dragodinde de course possible (gain XP)
- Custom items possibles

---

## 🐾 10. Pets / familiers

### Spécificités Retro 1.29
- Familiers = petit animal compagnon (stats bonus)
- Faim/PV : il faut le nourrir (sinon mort permanente)
- Items : nourriture spécifique par type

### Paquets
- `OAK|...` : ajout familier inventaire
- `Eq<itemId>;7` : équiper familier (slot 7)
- `Of|<repas>` : nourrir familier

### Familiers Retro célèbres
- Chacha (chat)
- Wabbit (lapin)
- Boufton (mouton)
- Crocomanche (croco)
- ... etc.

---

## 💰 11. HDV / commerce / banque

### Hôtel des Ventes (HDV)
Type d'HDV par catégorie d'item (HDV armes, HDV armures, HDV consommables, etc.).

### Paquets HDV
```
C→S : EC|<idHdv>                       ouvrir HDV
S→C : ECK|<typeHdv>|<niveau>           confirmation
C→S : EH<idHdv>|<categorie>;<sousCat>  filtrer prix moyen
S→C : EHL...                           liste prix moyens
C→S : EHm<lot>|<categorie>|<idObj>     mettre en vente
S→C : EHmK...                          confirmation vente
C→S : EHp<idObj>|<qte>|<prix>          acheter
```

### Banque
```
C→S : DC<idPnj banque>                 ouvrir dialogue banquier
S→C : DM|<lignes dialogue>             menu (50 kamas/100k/500k...)
C→S : DR<id reponse>                   choisir réponse
S→C : Ec<contenuBanque>                contenu banque
C→S : Ed<idObj>;<qte>                  déposer en banque
C→S : Eg<idObj>;<qte>                  retirer de la banque
```

### Échanges joueurs
```
C→S : ER<idJoueur>                     demande échange
S→C : ERK<idJoueur>                    autre joueur accepte
C→S : EM<idObj>;<qte>                  mettre item échange
S→C : EMK<...>                         confirmation
C→S : EV                               valider échange
S→C : EK0 / EK1                        échec / succès
```

---

## 💬 12. PNJ / dialogues

### Format paquets dialogue Retro
```
C→S : DC<idPnj>                        commencer dialogue
S→C : DCK                              accepter dialogue
S→C : DQ<numQuestion>;<idQuestion>     question PNJ
S→C : DR3731|3279|...                  réponses possibles (séparées par |)
C→S : DR<idReponse>                    choisir une réponse
S→C : DQ<...>                          nouvelle question (ou fin)
C→S : DV                               quitter dialogue
S→C : DVK                              confirmation quit
```

### Exemple complet : parler à un PNJ et acheter
```
C→S : DC872                            (PNJ id 872)
S→C : DCK
S→C : DQ100;42                         "Bonjour, que veux-tu ?"
S→C : DR1001|1002|1003                 (3 réponses)
C→S : DR1002                           (je choisis "Acheter")
S→C : DQ101;43                         "Voici mes articles..."
... continue selon le pnj ...
C→S : DV                               (quitter)
S→C : DVK
```

### IDs PNJ
- Variable selon serveur (les IDs custom Hystoria peuvent différer)
- Voir `npcs_hystoria.json` (30 KB chez nous)

---

## 🏛 13. Guildes / alliances

### Paquets guilde Retro
```
C→S : gK<infos>                        créer guilde (avec un Percepteur)
C→S : gU<...>                          quitter guilde
C→S : gJI<idPlayer>                    inviter joueur
C→S : gJR0/1                           refuser/accepter invitation
S→C : gM<...>                          info membre
C→S : gT<rank>;<perm>                  changer rang/permission
S→C : gK0|<nom>;<level>;<emblem>       infos guilde
```

### Percepteur
```
C→S : Et<cell>                         poser percepteur
S→C : EtK<percepId>                    confirmation
C→S : Er<percepId>                     récupérer percepteur
```

### Pas critique pour notre bot solo
On peut ignorer la plupart de ces paquets pour le moment. Surtout utile si le bot doit gérer un compte en guilde.

---

## 🚨 14. Anti-bot Ankama — vision détaillée

Source : thread `la-pave-de-ma-vision-sur-lanti-bot-et-son-fonctionnement.2419` (Unnomcommun, 2019)

### Concept "indice de bot"
Le système anti-bot donne un score à chaque action / pattern :
- Cadence trop régulière (toutes les 800ms exact) → +X points
- Réaction trop rapide après évènement (< 200ms) → +Y points
- Patterns récurrents (toujours même séquence d'actions) → +Z points
- Jamais de pause humaine (toilettes, repas) → +W points

Quand le score dépasse un seuil : flag → modo check ou ban auto.

### Indicateurs de bot (du moins évident au plus évident)
1. **Action immédiate après packet** (< 100ms)
2. **Cadence régulière** (variance < 50ms)
3. **Toujours mêmes choix** (jamais d'ouverture inventaire, jamais de check stats)
4. **Pas de fautes** (un humain fait des erreurs : sort raté, mauvaise cible)
5. **24h/24** sans pause cohérente
6. **Patterns géographiques** (boucle exacte sur 5 maps)
7. **Pas de chat / commandes /who** (un humain regarde qui est en ligne)
8. **Réactions au chat = 0** (un humain répond parfois aux MP)
9. **Pas d'achats HDV variés**
10. **VM détectée** (fingerprint hardware)

### Mesures anti-détection conseillées
- **Random delays** : variance ≥ 30% (ex: 1500-2200ms au lieu de 1800ms exact)
- **Pauses planifiées** : 5-15 min toutes les 1-2h, longues nuits, jours off
- **Variations de comportement** : occasionnellement, ouvrir inventaire, regarder stats, /who, etc.
- **Erreurs simulées** : 1-2% de "rater" un sort, mauvaise cible
- **Pas de 24/24** : 8-14h/jour max
- **Multi-comptes** : pas sur la même IP (utiliser VPN différents)
- **Chat occasionnel** : répondre à un MP de temps en temps (très avancé)

### Spécificités Hystoria
- Serveur privé → peut avoir ses propres heuristiques anti-bot
- Modos actifs (Cadernis #3001 : ban après 2 sem 24/24)
- Cibles principales : zones loot (Astrub, Bonta, etc.) et zones XP (donjons populaires)

---

## 🌐 15. Sources externes recommandées

### Wikis & sites Dofus Retro
- **dofus-pour-les-noobs.com** : guides, sorts, classes, FM (legacy mais utile)
- **dofus-retro.fr** : wiki communautaire (si actif)
- **wiki.dofus-retro.fr** : wiki spécifique Retro
- **forums Ankama Retro** : annonces officielles, MAJ

### Bases de données items / monstres
- **dofusbook.net** (legacy 1.29) — items, stuffs
- **dofus-wiki.fandom.com** : wiki Fandom
- **encyclopediadofus.com** : (si encore en ligne)

### Outils techniques
- **JPEXS Free Flash Decompiler** : décompile DofusInvoker.swf
- **Wireshark** : sniff réseau
- **MinHook** (GitHub) : hook DLL pour Windows
- **Frida** : hook dynamique Python/JS
- **Process Hacker** : inspect et inject DLL

### Repos GitHub utiles
- `dyshay/Bot-Dofus-Retro` — base de notre bot
- `dofera/dofedex` — sources Retro désobfusquées
- `Manghao/DofusMapDataDecypher` — déchiffrement mapData
- `Azzary/NebulaR-Bot` — bot Retro hook+Lua

### Forums communauté Hystoria
- **Discord Hystoria officiel** (lien à demander à l'admin)
- **Forum Hystoria** (si existe)

---

## 🎯 STRATÉGIE D'APPROFONDISSEMENT PAR SUJET

### Pour les classes
1. Consulter **dofus-pour-les-noobs.com/guide-classes**
2. Lire les descriptions sorts dans notre **`spells_fr.json`**
3. Inspecter dans le client : voir les sorts à différents niveaux pour comprendre les évolutions

### Pour la forgemagie
1. Lire le guide **dofus-pour-les-noobs.com/guide-forgemagie**
2. Récupérer un EasyFM (simulateur)
3. Tester sur des items low-level d'abord (perte limitée si raté)
4. Pour automation : nécessite hook client ou MITM avancé (paquets EM/EmK)

### Pour les donjons
1. Faire un donjon **manuellement en mode passif** avec ton bot pour capturer la séquence complète
2. Identifier : entrée donjon (PNJ + clé), salle par salle (combat → couloir), sortie
3. Les donjons ont des mécaniques uniques (boss spécifiques) → souvent custom Hystoria

### Pour les interactifs
1. Notre `CatalogueInteractifs.cs` est le bon point de départ
2. Pour étendre : capturer chaque ressource interactive en mode passif
3. Croiser gfx (depuis MapData) avec skill envoyé par le client
4. Ajouter dans le catalogue

---

## 📜 NOTES FINALES V3

- Cadernis a **peu de threads** dédiés à ces sujets gameplay Retro (le forum est centré sur le côté technique : protocole, MITM, bot).
- Pour les sujets gameplay (classes, FM, donjons), les wikis Dofus et l'expérience JEU sont plus complets.
- **Notre `hechizos_dyshay.xml`** est la référence absolue pour les sorts (281 sorts × 6 niveaux, structuré).
- **Le client Retro lui-même** (via dofedex désob) est l'autorité pour les détails de protocole non documentés.

🌿 **Avec V1+V2+V3 tu as ~85 KB de doc projet** + tous les liens externes pour creuser. C'est plus que la doc officielle Dofus pour bots.
