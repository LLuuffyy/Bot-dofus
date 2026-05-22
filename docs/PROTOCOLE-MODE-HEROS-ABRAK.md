# Protocole — Mode Héros (Abrak / Hystoria) — Capture forensique

> Analyse des logs Luffy-bot du 2026-05-22 matin. Toutes les citations sont
> données au format `fichier.log:LIGNE` avec horodatage HH:MM:SS.mmm extrait
> directement du log.

---

## A. Identification du log analysé

### A.1. Sélection du log

Quatre logs candidats listés par la mission. Tri par signaux mode héros
(`GTSX`, `GTL` long, plusieurs IDs combattants positifs) :

| Log | Taille | `GTM` | `GTSX` | `Beiloddurul` | Verdict |
|---|---:|---:|---:|---:|---|
| `botdofus-20260522-081349.log` | 858 KB | 104 | **présent (multi)** | 145 | **MODE HÉROS** |
| `botdofus-20260522-024731.log` | 4.4 MB | 181 | 0 | 418 | solo (Beiloddurul seul) |
| `botdofus-20260521-171008.log` | 10.7 MB | 434 | 0 | n/a | solo (Beiloddurul seul) |
| `botdofus-20260522-073116.log` | 25 KB | 0 | 0 | n/a | session vide |

Le préfixe **`GTSX<...>`** est l'empreinte unique du mode héros : il n'apparaît
QUE dans `botdofus-20260522-081349.log` et y figure ~145 fois.

> Vérification croisée : `GTSX` count par log
> ```
> botdofus-20260522-081349.log : > 100 occurrences
> botdofus-20260522-024731.log : 0
> botdofus-20260521-171008.log : 0
> ```

Tous les combats des autres logs sont `1 allié vs N ennemis` (cf.
`botdofus-20260521-171008.log:1020` `2 combattant(s) (2 vivants) — alliés=1 ennemis=1`).

Le log analysé est donc :
`C:\Users\touki\Desktop\Mélange\Bot-dofus\BotDofus.Wpf\bin\Debug\net8.0-windows\logs\botdofus-20260522-081349.log`
de 08:13:49 à 08:39:xx (~26 minutes de jeu).

### A.2. Compte et persos identifiés

Login & roster :

- `botdofus-20260522-081349.log:36` `[VOCAB C→S] Zeliox83`
- `botdofus-20260522-081349.log:127` `ALK0|1|401770;Beiloddurul;21;101;-1;-1;-1;,9aa,9a9,,;0;5;0;0;-1;1;0`
  → **un seul perso** sur le compte (sélecteur perso renvoie 1 entrée)

Le serveur déclenche le mode héros **côté serveur** (lié à Abrak/Hystoria) :
8 personnages liés entrent dans le combat avec un seul socket client.

Roster déduit des paquets `GM` de début de combat
(`botdofus-20260522-081349.log:616-641`) :

| `id` | Nom | Classe | Niv | PV | PA | PM | Rôle |
|---:|---|---:|---:|---:|---:|---:|---|
| 401770 | **Beiloddurul** | 10 (Sadida) | 21 | 195 | 6 | 3 | **MOI** (master) |
| 401775 | Dranariel | 3 (Enutrof) | 7 | 85 | 6 | 3 | héros lié |
| 401774 | Aerawiol | 3 (Enutrof) | 7 | 85 | 6 | 3 | héros lié |
| 401777 | Athabiel | 3 (Enutrof) | 7 | 85 | 6 | 3 | héros lié |
| 401776 | Ukdeshan | 3 (Enutrof) | 7 | 85 | 6 | 3 | héros lié |
| 401779 | Aelardast | 3 (Enutrof) | 7 | 85 | 6 | 3 | héros lié |
| 401778 | Strarabdrall | 3 (Enutrof) | 7 | 85 | 6 | 3 | héros lié |
| 401781 | Vrottigrat | 3 (Enutrof) | 7 | 85 | 6 | 3 | héros lié |

→ 1 Sadida « master » niv 21 + 7 Enutrof niv 7 — **conforme à la description user**.

Lignes de référence (extrait brut, log `081349.log:616-640`) :

```
GM|+141;1;0;401770;Beiloddurul;10;101^100;1;21;0,0,0,401791,0;-1;-1;-1;,9aa,9a9,,;195;6;3;0;0;0;0;0;10;10;0;;
GM|+369;1;0;401781;Vrottigrat;3;30^100;0;7;0,0,0,401788,0;-1;-1;-1;,,,,;85;6;3;...
GM|+215;1;0;401778;Strarabdrall;3;31^100;1;7;0,0,0,401785,0;-1;-1;-1;,,,,;85;6;3;...
GM|+288;1;0;401779;Aelardast;3;30^100;0;7;0,0,0,401786,0;-1;-1;-1;,,,,;85;6;3;...
GM|+68;1;0;401776;Ukdeshan;3;31^100;1;7;0,0,0,401783,0;-1;-1;-1;,,,,;85;6;3;...
GM|+345;1;0;401777;Athabiel;3;31^100;1;7;0,0,0,401784,0;-1;-1;-1;,,,,;85;6;3;...
GM|+424;1;0;401774;Aerawiol;3;31^100;1;7;0,0,0,401781,0;-1;-1;-1;,,,,;85;6;3;...
GM|+26;1;0;401775;Dranariel;3;31^100;1;7;0,0,0,401782,0;-1;-1;-1;,,,,;85;6;3;...
```

> Format `GM|+<cell>;<team>;<?>;<id>;<nom>;<classe>;<skin>^<scale>;<sex>;<niv>;<accessoires>;<...>;<pv>;<pa>;<pm>;...`
>
> Les IDs `401774-401781` sont **séquentiels** (8 IDs consécutifs) et s'enchaînent
> immédiatement après `401770` (master). C'est cohérent avec une création
> atomique du « groupe héros » côté serveur.
>
> Les IDs côté `0,0,0,<id>,0` à la position 11 (`401791`, `401788`,…)
> pointent probablement vers un objet/template du compte héros — pas creusé.

### A.3. Combats observés

13 combats `[COMBAT] Combat terminé` dans le log
(grep `Combat termin` → lignes 795, 1530, 1723, 2127, 2451, 2616, 3248, 3407,
3748, 4285, 4778, 5540, 6160).

| # | Début (`GS` serveur) | Fin (`GE`) | Ennemis | Map |
|---:|---|---|---:|---:|
| 1 | 08:16:37.993 (L656) | 08:16:43.287 (L793) | 1 (`-1`) | 10290 |
| 2 | 08:21:05.234 (L920) | 08:21:58.040 (L1529) | 1 (`-1`) | 10290 |
| 3 | 08:22:53.398 (L1624) | 08:22:58.553 (L1722) | 1 (`-1`) | 10290 |
| 4 | 08:23:05.159 (L1813) | 08:23:46.826 (L2126) | 1 (`-1`) | 10290 |
| 5 | 08:27:14.510 (L2359) | 08:27:19.121 (L2450) | 1 (`-1`) | 10290 |
| 6 | 08:27:24.773 (L2537) | 08:27:29.993 (L2615) | 1 (`-1`) | 10290 |
| 7 | 08:31:19.776 (L3144) | 08:31:27.000 (L3247) | 1 (`-1`) | 10291 |
| 8 | 08:31:32.201 (L3340) | 08:31:36.480 (L3406) | 1 (`-1`) | 10291 |
| 9 | 08:31:43.577 (L3490) | 08:32:19.813 (L3747) | 1 (`-1`) | 10291 |
| 10 | 08:33:01.156 (L3943) | 08:33:42.914 (L4284) | 2 (`-1`, `-2`) | 10291 |
| 11 | 08:35:00.800 (L4389) | 08:35:50.435 (L4777) | 3 (`-1`, `-2`, `-3`) | 10291 |
| 12 | 08:36:09.627 (L4863) | 08:37:33.878 (L5539) | 3 (`-1`, `-2`, `-3`) | 10291 |
| 13 | 08:38:00.476 (L5645) | 08:38:59.671 (L6159) | 2 (`-1`, `-2`) | 10291 |

Côté équipe alliée : **constant 8 alliés**, jamais aucun perso lié n'est mort
(cf. recherche `alliés=[1-7]` → 0 hit, et toutes les lignes `vivants` font
`alliés=8`).

### A.4. Ce que NE PROUVE PAS le log

- Aucune mort de héros lié observée → comportement à mort de membre (paquet,
  ordre des tours impact) **non capturable ici**.
- Aucun combat avec mode héros + groupe d'ennemis adjacents (chaîne
  d'agression / mob défensif) — uniquement combats mob solo / boss bouftou.
- Aucun changement de map en combat (`Im0152` n'apparaît qu'à
  L141/L427/L2899, pré-combats).

---

## B. Paquets mode héros — table récap

Légende : **S→C** = serveur vers client • **C→S** = client vers serveur.

| Préfixe | Sens | Signification mode héros | Exemple brut + ref |
|---|---|---|---|
| `GA907<cellEnnemi>;-<idGroupe>` | C→S (chiffré '-') | Agression d'un groupe de mobs (déclenche le combat héros). `idGroupe` négatif = groupe mob du jeu. | `GA90786;-85` `081349.log:596` à 08:16:33.482 |
| `GA;905;` | S→C | Ack serveur de l'agression (broadcast). | `081349.log:608` à 08:16:35.615 |
| `GJK2\|0\|1\|0\|45000\|4` | S→C | Pré-combat : ouvre la phase placement (timer 45000ms, 4 = ?). Identique en solo et en mode héros. | `081349.log:611` à 08:16:35.617 |
| `GP<casesEq1>\|<casesEq2>\|0` | S→C | Liste des cases de placement disponibles, équipe1 + équipe2 (encodage 2 chars/cell). Identique solo / héros. | `GPaAbecndxeGfzfGfXfZgO\|aQbbbGbYb5b_dqeAfIgl\|0` `081349.log:613` à 08:16:35.736 |
| `GM\|+<cell>;<team>;...;<id>;<nom>;...` | S→C | **Entrée d'un combattant**. En mode héros, **8 GM consécutifs** pour les 8 persos liés + 1+ GM pour les mobs. | `GM\|+141;1;0;401770;Beiloddurul;10;101^100;1;21;...;195;6;3;...` `081349.log:616-640` (8 lignes) |
| `Gd+<id>;<x>;...` | S→C | Défi/challenge groupe-vs-groupe lié au boss (`+` = ouvert, `-` = annulé, `OK<id>` = ack, `KO<id>` = échec). | `Gd+20;41;0;;10;5;10;5;0;0` `081349.log:620` à 08:16:35.980 |
| `GR1` | C→S | **Un seul** ready envoyé par le master pour LES 8 persos. Pas de GR1 par perso lié. | `[INJ ->SRV] GR1` `081349.log:652` à 08:16:37.955 |
| `GR1<idMaster>` | S→C | Broadcast confirmation prêt — uniquement le master. | `GR1401770` `081349.log:654` à 08:16:37.993 |
| `GS` | S→C | Début du combat (transitionne placement → tour 1). | `081349.log:656` à 08:16:37.993 |
| **`GTL\|<id1>\|<id2>\|...`** | S→C | **Ordre des tours**. En mode héros : 8 alliés intercalés avec les ennemis selon l'initiative. | `GTL\|401770\|-1\|401775\|401774\|401777\|401776\|401779\|401778\|401781` `081349.log:659` à 08:16:38.114 |
| **`GTSX<idMaster>;<idPerso>;0;1;0;<stats>`** | S→C | **Nouveau paquet héros** : pour chaque héros lié, le serveur envoie un GTSX (turn state extended) AVANT le `GTM` initial. 8 GTSX au début (1 par perso lié, y compris master ?). Stats =  buffs initiaux. | `GTSX401770;401781;0;1;0;49~1~-1,51~1~3,41~1~1,42~1~-1,43~1~2` `081349.log:668` à 08:16:40.007 |
| `As<...>` | S→C | Stats actualisés d'UN combattant (PV/PA/PM/initiative...). En mode héros, série de 8 `As` consécutifs après chaque action significative. | `As9416,7300,10500\|206\|30\|6\|...\|85,85\|10000,10000\|...` `081349.log:670` |
| `GTM\|<entree1>\|<entree2>\|...` | S→C | **État global des combattants** (refresh complet) après chaque tour. Chaque entrée = `<id>;<team>;<pv>;<pa>;<pm>;<cell>;;<pvmax>`. 9 entrées attendues (8 alliés + 1 ennemi). | `GTM\|-1;0;10;3;5;65;;10\|401781;0;85;6;3;369;;85\|401778;0;85;6;3;215;;85\|...\|401770;0;195;6;3;141;;195` `081349.log:694` à 08:16:40.139 |
| `GTS<id>\|<timerMs>\|<numTour>` | S→C | Démarre le tour d'un combattant. **Format inchangé vs solo** — c'est le même paquet pour le master, les héros liés et les mobs. | `GTS401770\|45000\|1` `081349.log:698` à 08:16:40.264 (MOI) `GTS401775\|45000\|1` `081349.log:1023` (héros lié) `GTS-1\|45000\|1` `081349.log:1003` (mob) |
| `GAS<id>` / `GAF<code>\|<id>` | S→C | Début / fin d'action — uniquement vu pour `401770` (MOI). Pas observé pour `40177x` ni `-N`. | `GAS401770` `081349.log:719` / `GAF2\|401770` `081349.log:729` |
| `GA0;1;<id>;<chemin>` | S→C | Mouvement d'un combattant — déchiffré par le bot. En mode héros : émis pour CHAQUE perso qui bouge (master + héros liés + mobs). | `GA0;1;401770;ad_hbZgbZ` `081349.log:599` `GA0;1;-1;agldgz` (mob) `081349.log:1006` (vu via `[GA0] acteur #401775` `081349.log:4038`, `[GA0] acteur #-2` `081349.log:4014`) |
| `GTF<id>` | S→C | Tour fini de `<id>`. | `GTF401770` `081349.log:991`, `GTF-2` `081349.log:4019`, `GTF401775` `081349.log:4044` |
| `GTR<idMaster>` | S→C | **Le serveur envoie TOUJOURS `GTR401770`** après CHAQUE `GTF<n'importe-qui>` — c'est l'indication que **le client (master) doit envoyer son `GT` ack** avant que le prochain tour démarre. | `GTR401770` apparaît après tour mob (`081349.log:1017`) et après tour héros lié (`081349.log:1037`, `1057`...). |
| `GT` | C→S | Turn ready ack — **un seul** envoyé par le client (master) après chaque GTR du serveur, qu'il s'agisse du tour d'un héros lié ou d'un mob. | `[PKT C→S] GT` `081349.log:1000`, `1021`, `1040`... (89 occurrences) |
| `Gt` | C→S | Pass turn (minuscule). Le client n'envoie `Gt` que pour le master ; pour les héros liés c'est probablement le serveur qui passe automatiquement (IA serveur), ou bien le client envoie aussi mais pas vu en mode passif. | `[INJ ->SRV] Gt` `081349.log:855` (master), `986`, `1183`... (8 occurrences, toutes après tour master) |
| **`NO<numTour>~<id1>;<n1>\|<id2>;<n2>\|...`** | S→C | **Nouveau paquet héros** : statut/initiative numérique par combattant en début de tour. Pas observé en combat solo. Valeurs : `0` ou `3`/`4` — peut-être tours d'attente ou ordre lié à l'initiative. | `NO32~401781;0\|401778;0\|401779;0\|401776;0\|401777;4\|401774;0\|401775;0\|401770;3` `081349.log:4388` à 08:34:36.962 |
| `SLo+<id>` | S→C | Mise à jour des sorts (Spell List Owner). Émis post-combat pour le master uniquement. | `SLo+401770` `081349.log:819` à 08:16:43.655 |
| `GKK0` | C→S (chiffré '-') | Ack action générique. **Émis seulement par le master** (1 ack par action effective du master). Pas vu pour les héros liés. | `clair='GKK0'` `081349.log:606`, `726`... |
| `_l2\|TerminateFight with sequencerId : <idMaster>` | C→S | Message debug client (Flash trace) — confirme côté client que le combat a été terminé par le master. | `_l2\|TerminateFight with sequencerId : 401770` `081349.log:828` à 08:16:43.918 |
| `GE<xp>;<kamas>\|<idMaster>\|<...>\|<butin1>\|<butin2>\|...` | S→C | **Fin de combat**. En mode héros : le paquet liste un butin par perso lié (8 blocs `2;<id>;<nom>~<skin>;<niv>;0;0;0;<actuel>;<xp>;<xpmax>;<level>;0;0;<items>;<niveau-up?>`). | `GE4813;46\|401770\|0\|2;401781;Vrottigrat~30;7;0;0;0;7300;9428;10500;12;0;0;8545~1,519~1;20\|2;401778;Strarabdrall~31;...\|2;401779;Aelardast~30;...\|...` `081349.log:792` à 08:16:43.287 |

### B.1. Format `GTSX` détaillé

Tous les `GTSX` du log :

```
GTSX401770;401781;0;1;0;49~1~-1,51~1~3,41~1~1,42~1~-1,43~1~2
GTSX401770;401778;0;1;0;49~1~-1,51~1~3,41~1~1,42~1~-1,43~1~2
GTSX401770;401779;0;1;0;49~1~-1,51~1~3,41~1~1,42~1~-1,43~1~2
GTSX401770;401776;0;1;0;49~1~-1,51~1~3,41~1~1,42~1~-1,43~1~2
GTSX401770;401777;0;1;0;49~1~-1,51~1~3,41~1~1,42~1~-1,43~1~2
GTSX401770;401774;0;1;0;49~1~-1,51~3~3,41~1~1,42~1~-1,43~1~2
GTSX401770;401775;0;1;0;49~1~-1,51~3~3,41~1~1,42~1~-1,43~1~2
```

Notes :
- 1er champ = **id master** (`401770` — Beiloddurul). Constant.
- 2e champ = **id du perso lié** (`401781`..`401774`). Pas `401770` lui-même
  → le master n'a pas son propre GTSX, juste les 7 autres.
- 3e/4e/5e = `0;1;0` constant — flags inconnus.
- 6e = liste d'effets/buffs sous forme `49~1~-1,51~1~3,41~1~1,42~1~-1,43~1~2`
  → 5 entrées (`49,51,41,42,43`) — codes statistiques (force/vita/intel/agilité/chance ?).
  Pour 401774/401775 le code `51` passe à `~3~3` (valeur stat modifiée).

Le serveur émet ces GTSX **avant** le premier `GTM` du combat
(`081349.log:668-688` puis `694`), donc c'est une **annonce d'initialisation
des héros liés** — leurs buffs/altérations sont diffusés une fois pour toutes.

### B.2. Format `NO<num>~...`

Émis hors début de combat, observé 4 fois :

```
081349.log:1577 NO32~401781;4|401778;0|401779;4|401776;0|401777;4|401774;0|401775;0|401770;3
081349.log:2206 NO48~401781;0|401778;0|401779;0|401776;0|401777;4|401774;0|401775;0|401770;3
081349.log:3837 NO48~401781;0|401778;0|401779;0|401776;0|401777;4|401774;0|401775;0|401770;3
081349.log:4388 NO32~401781;0|401778;0|401779;0|401776;0|401777;4|401774;0|401775;0|401770;3
```

- Préfixe `NO<n>` avec `n` ∈ {32, 48} — fonction non identifiée
  (timer ? identifiant de tour ? entité de quête ?)
- Format : `<id>;<flag>` par perso, séparateur `|`.
- Valeurs : seuls `401777=Athabiel` (4) et `401770=Beiloddurul` (3) ont des
  valeurs non-nulles dans 3/4 captures.
- → **Hypothèse forte** : indicateur d'initiative ou de buff persistant
  par héros. **À recapturer** avec un GTSX de référence pour valider.

### B.3. Format `GE` complet (multi-héros)

Bloc `GE` (fin de combat, 1er combat L792) — la partie après `|401770|0|` est
une liste de blocs séparés par `|`, **un bloc par perso lié** plus 1 bloc
master :

```
GE4813;46|401770|0|
2;401781;Vrottigrat~30;7;0;0;0;7300;9428;10500;12;0;0;8545~1,519~1;20|
2;401778;Strarabdrall~31;7;0;0;0;7300;9428;10500;12;0;0;307~1,519~1;19|
2;401779;Aelardast~30;7;0;0;0;7300;9428;10500;12;0;0;8545~1,307~1,519~1;22|
2;401776;Ukdeshan;...
```

Décodage du préfixe `GE4813;46|401770|0|` :
- `4813` = durée du combat en centièmes de seconde (≈ 48,13 s) — à valider
- `46` = nombre de tours total ? (vu combat 2 avec `54`, combat 11 avec `54`...)
- `401770` = id master / vainqueur
- `0` = flag (issue ? team gagnante ?)

Décodage d'un bloc perso (`2;401781;Vrottigrat~30;7;0;0;0;7300;9428;10500;12;0;0;8545~1,519~1;20`) :
- `2` = type (perso lié vs `1` = master ?)
- `401781` = id perso
- `Vrottigrat~30` = nom~skin
- `7` = niveau
- `0;0;0` = ?
- `7300;9428;10500` = xp courante, xp gagnée (`9428-7300=2128`), xp suivante
- `12` = ?
- `0;0` = ?
- `8545~1,519~1` = drops `<itemTemplate>~<qty>` séparés `,`
- `20` = ?

Bloc master plus court (juste `401770|0|`) — l'XP de Beiloddurul est gérée
dans les paquets `As` précédents (`081349.log:691` `As220714,202000,235000`
= xp courante, xp tour précédent, xp suivante).

---

## C. Différences vs combat solo

Comparaison directe avec `botdofus-20260521-171008.log` (solo, Beiloddurul
seul) :

| Aspect | Solo | Mode héros |
|---|---|---|
| `ALK0\|...` (sélecteur perso) | `ALK0\|1\|401770;...` | `ALK0\|1\|401770;...` **identique** — le « groupe héros » est créé côté serveur, pas annoncé à la connexion. |
| Nombre d'entrées `GM` à l'entrée combat | 1 entrée master + N mobs | **8 entrées GM consécutives** (master + 7 héros liés) + N mobs |
| `GTL\|...` (ordre des tours) | 2-4 ids (master + mobs) ex. `GTL\|401770\|-1` `171008.log:1004` | **9-11 ids** (master + 7 liés + mobs) ex. `GTL\|401770\|-1\|401775\|401774\|401777\|401776\|401779\|401778\|401781` `081349.log:659` |
| `GTSX` (post-`GTL`, pré-`GTM`) | **absent** | **présent** — 1 paquet par héros lié sauf master (7 GTSX par init combat) |
| `NO<n>~...` | **absent** (0 occurrence dans 171008.log) | présent (4 occurrences en milieu de combat) |
| `GTM` (refresh d'état) | 2-4 entrées | **9-11 entrées** (toujours = #combattants total) |
| `GR1` (ready) C→S | 1 envoi (master) | **toujours 1 envoi** (master) — pas un par héros lié |
| `GR1<id>` (ack ready) S→C | `GR1401770` | `GR1401770` **identique** — le master est le seul auteur du ready |
| `GTS<id>` (start turn) | uniquement `401770` ou `-N` | **`401770`, `40177x` (7 IDs), `-N`** — un par perso lié + master + mobs |
| `GTR<id>` (turn ready S→C) | `GTR401770` après chaque GTF | **`GTR401770` après CHAQUE GTF** — même si le GTF concerne un héros lié (`401775`, `401774`...) ou un mob. Le master est toujours le destinataire du « ready signal ». |
| `GT` (ack ready C→S) | envoyé après chaque GTR | **idem** — un seul `GT` par GTR, peu importe à qui appartient le tour. Le client master coordonne les 8 persos via le même socket. |
| `Gt` (pass turn C→S) | envoyé pendant tour master | **envoyé uniquement pendant tour master** (mode passif → user a probablement pass) — les tours des héros liés sont passés automatiquement par le serveur (l'IA héros joue côté serveur ?) |
| `GAS<id>` / `GAF<code>\|<id>` | uniquement master | **uniquement master** — pas vu pour `40177x`. Les actions des héros liés (mouvements GA001) sont diffusées via `GA0;1;<id>;<chemin>` directement. |
| Paquet `GE` (fin combat) | 1 bloc master | **8 blocs** (1 par perso lié + master condensé) avec drops par perso |

### C.1. Synthèse

**Ce qui change vs solo :**

1. **Multi-`GM` à l'entrée** : 7 GM additionnels en plus du master, IDs séquentiels.
2. **`GTL` long** : ordre des tours mélangé alliés/ennemis sur 9+ ids.
3. **`GTSX` introductif** : annonce des buffs initiaux des héros liés (7 paquets pré-`GTM`).
4. **`GTS<id>` pour chaque héros lié** : tour de chaque perso joué dans l'ordre `GTL`.
5. **`NO<n>~...` runtime** : émis 1-4× par combat — sémantique pas confirmée.
6. **`GE` multi-blocs** : un récap XP/drop par perso lié.

**Ce qui ne change PAS :**

- Le proxy MITM `SessionProxy` voit **un seul socket** (`Zeliox83#1NLmv1XjgtCNVY...`)
  pour les 8 persos.
- Le master gère TOUT le flot d'ack (`GR1`, `GT`, `GKK0`, `Gt`).
- Les paquets internes (`GA0`, `GA300`, `GA001`, `As`) sont identiques au solo,
  mais référencent les IDs des héros liés.
- Pas de préfixe « MODE HÉROS » dédié à l'entrée — c'est la **multiplicité
  d'IDs alliés positifs dans `GTL` + présence de `GTSX`** qui signe.

---

## D. Flow capturé

### D.1. Diagramme ASCII — entrée d'un combat héros

```
Client (Zeliox83)                  Proxy MITM                Serveur Hystoria
─────────────────                  ──────────                ───────────────
[déplacement libre]
GA001<chemin>          ─chiffré'-'─►  relayé        ─►  (joueur se déplace)
GA907<cellMob>;-<grp>  ─chiffré'-'─►  relayé        ─►  AGRESSION
                                                          │
                       ◄────────── GA;905;          ◄─── (broadcast agression)
                       ◄────────── GJK2|0|1|0|45000|4   (timer 45s placement)
                       ◄────────── GP<cases1>|<cases2>|0 (cases placement)
                                                          │
                       ◄────────── GM|+141;1;0;401770;Beiloddurul;10;...;195;6;3;... (MASTER)
                       ◄────────── GM|+65;1;0;-1;974;-2;1572;...;10;3;5;...           (MOB)
                       ◄────────── GM|+369;1;0;401781;Vrottigrat;3;...;85;6;3;...     (HÉROS-LIÉ 1)
                       ◄────────── GM|+215;1;0;401778;Strarabdrall;3;...;85;6;3;...   (HÉROS-LIÉ 2)
                       ◄────────── GM|+288;1;0;401779;Aelardast;3;...                 (HÉROS-LIÉ 3)
                       ◄────────── GM|+68;1;0;401776;Ukdeshan;3;...                   (HÉROS-LIÉ 4)
                       ◄────────── GM|+345;1;0;401777;Athabiel;3;...                  (HÉROS-LIÉ 5)
                       ◄────────── GM|+424;1;0;401774;Aerawiol;3;...                  (HÉROS-LIÉ 6)
                       ◄────────── GM|+26;1;0;401775;Dranariel;3;...                  (HÉROS-LIÉ 7)
                       ◄────────── Gd+20;41;0;;10;5;10;5;0;0  (challenge boss ?)
                                                          │
[user clic prêt]
                                   GR1 ─chiffré'-'─►       (ready master)
                       ◄────────── GR1401770              (ack ready)
                       ◄────────── GS                     (start fight)
                       ◄────────── GTL|401770|-1|401775|401774|401777|401776|401779|401778|401781
                                                          │
                       ◄────────── GTSX401770;401781;0;1;0;49~1~-1,51~1~3,...   (init H-L 1)
                       ◄────────── As<...>|369|...  (stats H-L 1)
                       ◄────────── GTSX401770;401778;0;1;0;49~1~-1,51~1~3,...   (init H-L 2)
                       ◄────────── As<...>|215|...
                       ◄────────── GTSX401770;401779;0;1;0;...                  (init H-L 3)
                       ◄────────── As<...>|288|...
                       ◄────────── GTSX401770;401776;0;1;0;...                  (init H-L 4)
                       ◄────────── GTSX401770;401777;0;1;0;...                  (init H-L 5)
                       ◄────────── GTSX401770;401774;0;1;0;49~1~-1,51~3~3,...   (init H-L 6)
                       ◄────────── GTSX401770;401775;0;1;0;49~1~-1,51~3~3,...   (init H-L 7)
                       ◄────────── As<...>|141|...  (stats master)
                       ◄────────── GTM|-1;0;10;3;5;65;;10|401781;0;85;6;3;369;;85|...|401770;0;195;6;3;141;;195
                                                          │
                       ◄────────── GTS401770|45000|1     (TOUR 1 — MASTER)

```

### D.2. Diagramme ASCII — un tour de chaque type d'acteur

```
─────────────────────────── TOUR MASTER (Beiloddurul) ─────────────────────────
                       ◄────────── GTS401770|45000|1
                                   GA001<chemin> ─'-'─►        (pré-mouvement)
                       ◄────────── GAS401770                   (action start)
                       ◄────────── GA0;1;401770;<chemin-confirmé>
                                   GKK0 ─'-'─►
                       ◄────────── GAF<code>|401770            (action fini)
                                   GA300<sortId>;<cell> ─'-'─► (cast)
                       ◄────────── (effets sort As + dégâts)
                                   GKK0 ─'-'─►
                                   Gt ─'-'─►                   (pass turn)
                       ◄────────── GTF401770                   (turn fini)
                       ◄────────── GTR401770                   (turn ready)
                                   GT ─►                       (ack ready)

─────────────────────────── TOUR HÉROS LIÉ (#401775) ──────────────────────────
                       ◄────────── GTS401775|45000|1
                                   (rien envoyé par le client master !)
                       ◄────────── GA0;1;401775;<chemin>       (mouvement IA serveur ?)
                       ◄────────── As<stats>|<cell>|...        (stats mises à jour)
                       ◄────────── GTF401775                   (turn fini)
                       ◄────────── GTR401770                   (turn ready au MASTER)
                                   GT ─►                       (ack)

─────────────────────────── TOUR MOB (#-1) ────────────────────────────────────
                       ◄────────── GTS-1|45000|1
                       ◄────────── GA0;1;-1;<chemin>           (IA serveur)
                       ◄────────── (As + actions du mob)
                       ◄────────── GTF-1                       (mob fini)
                       ◄────────── GTR401770                   (turn ready au MASTER)
                                   GT ─►                       (ack)
```

**Observation clé** : pendant les tours des héros liés ET des mobs, le client
master n'envoie qu'un `GT` à la fin. **Aucun input par perso lié n'est requis
côté client** dans le log (mode passif). Les déplacements/actions des héros
liés sont décidés **côté serveur**, le client reçoit juste les `GA0`/`As`.

→ Conclusion : **l'IA héros vit côté serveur Abrak/Hystoria**, le client a
juste à accuser les tours. Le bot Luffy n'aura pas à piloter les Enutrof —
seulement Beiloddurul, le master.

### D.3. Cas observé : combat #11 avec 3 mobs (08:35:xx)

`GTL` mélangé alliés/ennemis selon initiative :
```
GTL|401770|-2|401775|-1|401777|-3|401776|401779|401778|401774|401781  (081349.log:4392)
```

Ordre des tours observés en log L4453 à L4694 :
1. `GTS401770` → MOI (`COMBAT] Tour de #401770 (tour 1) ← MOI`)
2. `GTS-2`     → mob `-2`
3. `GTS401775` → héros lié Dranariel
4. `GTS-1`     → mob `-1`
5. `GTS401777` → héros lié Athabiel
6. `GTS401776` → héros lié Ukdeshan (mob -3 sauté = mort ?)
7. `GTS401779` → héros lié Aelardast
8. `GTS401778` → héros lié Strarabdrall
9. `GTS401781` → héros lié Vrottigrat
10. `GTS401774` → héros lié Aerawiol

→ L'ordre du `GTL` est **respecté à 100%** dans la séquence des `GTS`,
sauf qu'un mob mort fait sauter son tour (`-3` à `0` PV est skip).

---

## E. Points à valider / questions ouvertes

> Important : ces points sont **ambigus** dans le log analysé et nécessitent
> une nouvelle capture ciblée pour être confirmés.

### E.1. Sens exact du `GTSX`

- **6 champs** : `<master>;<peruID>;0;1;0;<5stats>` — l'extension `X` du `GTS`
  classique reste à mapper. Hypothèse : `X` = `eXtended` ou `eXternal` (ie.
  perso non maître).
- Le 4e champ (`1`) pourrait être un flag « auto-played » indiquant que
  le serveur joue ce perso → à corroborer en intercepant un mode héros où
  l'utilisateur contrôle manuellement les 7 autres.
- Les codes stats (`49,51,41,42,43`) sont les **5 caractéristiques Dofus**
  (Vitalité=49, Sagesse=51, Force=41, Intel=42, Chance=43) — à confirmer
  en croisant avec un perso fraîchement levé.

### E.2. Mort d'un héros lié

**Aucune mort de perso lié observée** dans les 13 combats. Questions ouvertes :
- Format du paquet annoncé : `GTF<id>` immédiat ? `GTM` avec PV=0 ? Préfixe spécial type `Gd-<id>` (vu pour le challenge boss `Gd-20` L1622) ?
- L'ordre des tours `GTL` reste-t-il, ou est-il rejoué (re-emis) ?
- Le master peut-il continuer si tous les héros liés meurent ? Inversement, le master peut-il mourir et les héros liés rester ?

→ Capture à recommander : aller chercher un mob trop fort qui tue 1-2 Enutrof,
voir comment le serveur réagit.

### E.3. Sémantique du `NO<n>~...`

Aucune corrélation évidente entre les valeurs (`3`, `4`, `0`) et l'état
visible (cellule, PV, PA, PM). Hypothèses ouvertes :
- `n` = numéro de tour absolu (32, 48) → cohérent avec timings 08:22:08 vs 08:24:33.
- valeur `3`/`4` = compteur de tactiques type « mode défensif/offensif » ou
  indicateur d'initiative dynamique.
- À recapturer avec **mode actif et IA bot pilotant le master** pour voir
  si on peut faire varier les valeurs.

### E.4. Initiative et calcul de l'ordre `GTL`

`GTL` semble figé pour les 6 premiers combats sur map 10290 :
```
GTL|401770|-1|401775|401774|401777|401776|401779|401778|401781
```
puis varie sur map 10291 selon les mobs présents. **Question** : quel champ
de `GM` (entrée combat) pilote l'initiative ? Le champ `niveau` ou un champ
dédié ? Pas pu inférer du log.

### E.5. Absence de `GAS` / `GAF` pour les héros liés

Le serveur n'envoie **que** `GAS401770`/`GAF<code>|401770` (uniquement master).
Pour les héros liés, le bot voit juste `GA0;1;<id>;<chemin>` (mouvement)
sans action wrapper. Hypothèses :
- Les héros liés ne lancent pas de sorts (les Enutrof niv 7 n'ont qu'un sort
  par défaut, à confirmer).
- Le serveur skip le wrapper `GAS/GAF` pour les actions auto-IA.

→ À recapturer avec **mode héros + boss + héros liés qui castent** (faut
les monter en niveau d'abord pour qu'ils aient des sorts utiles).

### E.6. Persistance des persos liés hors combat

- `ALK0|1|401770` confirme que **côté sélecteur perso**, seul le master existe.
- Le « groupe héros » est instantié au combat uniquement ? Ou bien il a une
  existence persistante côté serveur (sauvegarde de PV/équipement) ?
- Le log ne montre aucun équipement détaillé pour les héros liés (`,,,,;`
  vide dans les `GM` `081349.log:622`). → soit ils n'ont **aucun équipement**,
  soit le serveur ne l'envoie pas en mode héros.

### E.7. Le `Gd+20;41;0;...` est-il lié au mode héros ?

Le paquet `Gd+20;41;0;;10;5;10;5;0;0` (`081349.log:620`) apparaît une fois par
combat juste après les 8 GM. Format suspect : 2 sous-stats `(10;5)` apparaissent
2 fois → peut-être PA/PM master + PA/PM moyens héros liés ?

Le préfixe `Gd` est documenté comme « challenge » dans le protocole Dofus
1.29 classique (`Gd+<id>` = défi proposé, `Gd-` = refusé, `GdOK<id>` = accepté).
Or ici `GdOK41` (L748) arrive en plein combat, après le start `GS`.

→ Peut-être un challenge bonus XP/drops automatique en mode héros Abrak ?
Aller voir le client Hystoria Lua/AS3 si dispo.

### E.8. Le serveur diffuse-t-il les `GTSX` à chaque combat ?

Oui, vérifié : 7 `GTSX` par combat × 13 combats = 91 GTSX attendus, on en a 145
dans le log (donc certaines retransmissions, p.ex. au redébut de combat ou
sur reprise). → mais cela fait plus de 91, à investiguer : peut-être que
le serveur les retransmet aussi à mi-combat ?

Recherche `081349.log` : `GTSX` n'apparaît qu'après les `GS` du début de
chaque combat (1 séquence de 7 GTSX par combat). 13 × 7 = 91, écart à
~145 = ~54 paquets supplémentaires non identifiés. → **À recapturer pour
voir où ces GTSX additionnels apparaissent** (peut-être après mort/résurrection
d'un perso lié, ou changement de buff).

---

## F. Annexe — protocole minimal pour piloter le mode héros

Ce que le bot doit gérer côté code (vs solo) :

1. **Parser `GTL` avec N ids** (déjà supporté pour solo, juste extensible).
2. **Mémoriser que tous les IDs positifs proches du master sont des alliés**
   (`401770..401781` dans ce log → master+7).
3. **Ignorer les `GTSX`** au début (juste les loguer pour debug).
4. **Ne JAMAIS envoyer `Gt`/`GA300`/`GA001` pendant un `GTS<id>` ≠ master** —
   c'est le serveur qui joue. Le bot doit juste attendre `GTF<id>` puis
   répondre `GT` à `GTR<idMaster>`.
5. **Détection « MOI »** : `GTS<id>` avec `id == idMaster` (idMaster = 1er
   id du `GTL`, OU id capturé via `GR1<id>` initial).
6. **Parser `GE` multi-blocs** pour stats post-combat — utile pour le suivi
   XP par perso lié (UI dashboard).
7. **Pas d'IA combat pour les héros liés** — pas besoin de configurer leurs
   sorts, le serveur les pilote.

---

*Document généré par analyse forensique des logs Luffy-bot. Toutes les
citations sont vérifiables par grep sur le fichier de log original.*
