# FORENSIC-SCREENSHOTS-BANQUE

Agent 2 (FORENSIC-SCREENSHOTS) — analyse visuelle des captures pour le bug "dépôt banque incomplet après le 1er cycle".

## Screenshots analysés

Ordre = du plus récent au plus ancien (par date de capture), tels que listés dans le brief.

1. `Capture d'écran 2026-05-28 175310.png` — Dofus client : inventaire + UI "Forgeron Sombre" (combat / mort) — **PERTINENT**
2. `Capture d'écran 2026-05-28 175248.png` — Dofus client : inventaire + interface "Personnage" cote-a-cote — **PERTINENT**
3. `Capture d'écran 2026-05-28 081847.png` — Luffy-bot onglet Carte (MapViewer) sur une map "Donjon" sombre — non pertinent banque
4. `Capture d'écran 2026-05-28 080736.png` — Mix client Dofus + Luffy-bot (MapViewer en grille) — non pertinent banque
5. `Capture d'écran 2026-05-28 080656.png` — Luffy-bot onglet Carte (Beiloddurul Lv.173, map [12,18]/294) — non pertinent banque
6. `Capture d'écran 2026-05-28 080000.png` — Luffy-bot onglet Carte avec menu contextuel cellule — non pertinent banque
7. `Capture d'écran 2026-05-26 201349.png` — Avatar Iop sur écran de sélection persos Dofus — non pertinent
8. `Capture d'écran 2026-05-26 201314.png` — Écran d'accueil/connexion Dofus (mode "Classique") — non pertinent
9. `Capture d'écran 2026-05-22 102740.png` — Client Dofus : panneau "Tes sorts - Dranariel" (Eni) — non pertinent
10. `Capture d'écran 2026-05-21 170342.png` — Luffy-bot onglet Bot, console technique scrollée (debug paquets) — semi-pertinent
11. `Capture d'écran 2026-05-20 195314.png` — Luffy-bot onglet Combat sur combat "test" tour 2 — non pertinent
12. `image.png` — SynfusBot (référence externe), onglet Combat → Sorts — non pertinent banque
13. `image1.png` — SynfusBot, onglet Combat → General + Consommable de soin — non pertinent banque
14. `imag2e.png` — MoonBot (autre bot tiers), onglet Combat → Sorts — non pertinent banque

## Pour chaque screenshot

### Capture d'écran 2026-05-28 175310.png (28 mai 2026 17:53:10)

- **Contexte** : Client Dofus Retro en jeu, perso "Beiloddurul" (top right) avec la fenêtre Inventaire ouverte. Panneau gauche d'un PNJ/monstre "Forgeron Sombre Niv. 48" visible (UI de fin de combat / loot ?). Onglet "Ressources" sélectionné côté inventaire (grille de droite).
- **Observations factuelles** :
  - Kamas affichés : `4 440 150`.
  - Sur la grille inventaire (panneau droit, onglet Ressources, filtre "Tous types"), on distingue ~3 lignes pleines d'icônes : poissons (sardines/petite épave bleue), céréales (bottes jaunes — blé/avoine/etc.), bois (rondins), fruits/légumes (oranges, citrons), gemmes/métaux (icônes colorées rouge/violet/jaune).
  - La barre "Pods" est visible côté équipement central mais la jauge exacte n'est pas lisible à cette résolution (texte "Pods" identifié, niveau de remplissage indéterminable).
  - En bas, journal de combat : "Beiloddurul perd 3 PA", "Forgeron Sombre perd 130 PDV", message "Challenge raté - Élémentaire(...) Beiloddurul que l'on doit cet exploit", "Beiloddurul perd 82 PDV".
  - Aucune fenêtre Banque n'est ouverte dans ce screenshot.
- **Pertinence pour le bug** : **MED** — montre l'état d'inventaire **Ressources** plein post-combat (avant un éventuel dépôt). Utile comme référence "avant banque". Ne montre pas le scénario après-dépôt-partiel.

### Capture d'écran 2026-05-28 175248.png (28 mai 2026 17:52:48)

- **Contexte** : Client Dofus Retro, deux fenêtres ouvertes côte à côte (gauche : inventaire perso "Beiloddurul", droite : autre grille — vraisemblablement la **banque ApS mobile** ou un autre inventaire — l'agencement double grille est caractéristique d'un échange/banque ouverte).
- **Observations factuelles** :
  - Grille de gauche : inventaire onglet Ressources, plusieurs lignes d'items visibles (céréales jaunes, bois, poissons bleus, fruits oranges/jaunes/rouges).
  - Grille de droite : pleine d'items "ressources" très similaire à la grille de gauche (mêmes types d'icônes — laine, poissons, céréales, gemmes, fruits).
  - Texte petit, mais on voit clairement une **double-grille items ressources des deux côtés** = état "banque/échange ouvert avec items dans les 2 caisses".
  - Barre de combat (sorts) en bas avec icônes — le client semble encore avoir un overlay de combat ou interface farm active.
- **Pertinence pour le bug** : **HIGH** — c'est la seule capture qui montre simultanément **inventaire + banque/réceptacle ouverts**, avec **des ressources encore présentes côté inventaire alors que la banque est ouverte**. C'est exactement le visuel "dépôt partiel" attendu : ressources résiduelles dans l'inventaire malgré ouverture banque.

### Capture d'écran 2026-05-28 081847.png (28 mai 2026 08:18:47)

- **Contexte** : Luffy-bot WPF, onglet **Carte** (MapViewer) sur une map sombre type donjon (cellules noires marbrées, légende visible).
- **Observations factuelles** : Aucun élément banque/inventaire visible. Header bot avec boutons habituels (Mode passif, Lancer jeu, Client Auto, Farm Auto, Récolte Auto…).
- **Pertinence pour le bug** : **LOW** — pas de banque.

### Capture d'écran 2026-05-28 080736.png (28 mai 2026 08:07:36)

- **Contexte** : Mixed — client Dofus en haut à gauche (zone côtière) + Luffy-bot MapViewer en bas à droite (grille iso vide).
- **Observations factuelles** : Pas de banque, pas d'inventaire détaillé.
- **Pertinence pour le bug** : **LOW**.

### Capture d'écran 2026-05-28 080656.png (28 mai 2026 08:06:56)

- **Contexte** : Luffy-bot WPF, onglet **Carte** sur Beiloddurul Lv.173 map [12,18]/294, cellule 332.
- **Observations factuelles** :
  - Panneau droit : Position [12,18], MONSTRES = "Forgeron Sombre Lv.0 [289]" et plusieurs autres ("Boulanger Sombre, Mineur Sombre, Forgeron Sombre Lv.0 [289]" + cell 447), NPC "Layol Nidalap #-257076 cell:289 gfx:200".
  - **RESSOURCES = "Aucune"** sur cette map.
  - SORTIES listées (cell 0, 14, 464, 478).
- **Pertinence pour le bug** : **LOW** — pas d'état banque. Indique juste que sur la map active, il n'y a pas de ressource à récolter (cohérent avec map donjon).

### Capture d'écran 2026-05-28 080000.png (28 mai 2026 08:00:00)

- **Contexte** : Luffy-bot onglet Carte, popup menu cellule "Cellule 29 - marchable, Coords (1,1) niveau 0" avec actions (Copier cellId, Travel vers, Aller cellule…).
- **Observations factuelles** : Aucun élément banque.
- **Pertinence pour le bug** : **LOW**.

### Capture d'écran 2026-05-26 201349.png

- **Contexte** : Avatar Iop / Sacrieur dans l'écran d'écran de sélection ou de personnages de Dofus Retro.
- **Pertinence pour le bug** : **LOW** — hors sujet.

### Capture d'écran 2026-05-26 201314.png

- **Contexte** : Écran d'accueil Dofus Retro (Hystoria?) avec carte "Aventure Classique".
- **Pertinence pour le bug** : **LOW**.

### Capture d'écran 2026-05-22 102740.png

- **Contexte** : Client Dofus, perso "Dranariel" (Eni), panneau "Tes sorts" ouvert (Lancer de Pelle, Lancer de Pièces, Sac Animé, Pelle Fantomatique, Chance, etc.).
- **Pertinence pour le bug** : **LOW** — pas de banque/inventaire.

### Capture d'écran 2026-05-21 170342.png

- **Contexte** : Luffy-bot WPF, onglet Bot (Dashboard?), console technique visible avec lignes de log (paquets, erreurs).
- **Observations factuelles** :
  - Header normal (Beiloddurul Lv 173, kamas, etc.).
  - Console (panneau central bas) : texte trop petit pour être lu finement, mais beaucoup de lignes [INFO]/[Debug] type traces réseau (`Gp`, `GA…`, `MAJ`, etc.).
- **Pertinence pour le bug** : **LOW–MED** — pas spécifique banque mais montre un état de session Luffy-bot fonctionnelle (utile pour valider que les screenshots de bug et l'UI banque devraient apparaître dans cette console).

### Capture d'écran 2026-05-20 195314.png

- **Contexte** : Luffy-bot WPF, onglet **Combat**, tour 2 d'un combat actif (Beiloddurul PV 130/130 PA 6 PM 3, ennemi #-1 PV 10/10 PA 4 PM 3). Sorts configurés vide. Header "test" (compte test). Mode passif OFF.
- **Pertinence pour le bug** : **LOW**.

### image.png

- **Contexte** : **SynfusBot** (bot tiers de référence), onglet Combat → Sorts → liste de règles (Baroud d'honneur, Fou rire de Shato, Djetunsou, Force des géants…) + formulaire d'ajout de sort avec conditions Distance/Cible/Joueur/Situation/Avancé.
- **Pertinence pour le bug** : **LOW** — référence UI pour l'onglet Combat futur, pas banque.

### image1.png

- **Contexte** : **SynfusBot**, onglet Combat → General (Positionnement, Style, Distance, Bloquer le combat) + Consommable de soin (Rougely +21 PV x1501, seuils PV%, délais ms).
- **Pertinence pour le bug** : **LOW** — référence config combat.

### imag2e.png

- **Contexte** : **MoonBot** v0.1.0 (autre bot tiers), onglet Combat avec un compte "Ju-Kurodk" en jeu. Sort configuré "Flèche Magique - Ennemi le plus proche", formulaire d'ajout sort + Tactique globale.
- **Pertinence pour le bug** : **LOW** — référence UI tierce.

## Indices visuels forts

Les seuls indices visuels exploitables pour le bug banque proviennent des **deux screenshots du 28 mai 2026 17:52–17:53** :

1. **17:52:48 (175248.png)** — **Double grille ressources visible** côte à côte (inventaire + banque/réceptacle), avec **ressources présentes des deux côtés**. C'est compatible avec :
   - un état "post-1er-cycle-de-dépôt" où une partie des ressources est passée (la grille droite contient déjà des items), mais
   - l'inventaire de gauche **contient encore beaucoup d'items ressources** (mêmes types : céréales, bois, poissons, gemmes).
   - Cela appuie le bug décrit : à l'ouverture du 2e cycle (ou en fin du 1er cycle), il reste des items côté joueur alors que la banque accepterait encore.

2. **17:53:10 (175310.png)** — **20 secondes plus tard**, inventaire toujours rempli de la même grille de ressources, fenêtre banque/échange désormais **fermée** (seul l'inventaire reste + un panneau "Forgeron Sombre" de combat). Kamas = `4 440 150`.
   - **C'est l'état "après dépôt incomplet"** : la banque a été ouverte (visible au screenshot précédent), puis fermée (screenshot courant), mais l'inventaire ressources est **toujours visuellement bien rempli** sur les 3+ premières lignes (céréales, poissons, fruits, bois, gemmes encore présents).

**La paire 175248 → 175310 est le smoking gun visuel du bug** : banque ouverte avec items dans inventaire → ~20s plus tard banque fermée et inventaire toujours plein des mêmes catégories ressources.

Aucun screenshot ne montre la console Luffy-bot **pendant** ce dépôt, donc impossible de croiser avec les logs `[BANQUE]` ou les paquets `EMO+`/`ER2`/`EK` du protocole banque/marchand.

## Confirmation/infirmation hypothèses

Note : les hypothèses H1–H8 n'ont pas été fournies dans le brief. Je liste ici les hypothèses standards évoquées dans CLAUDE.md et déductibles pour ce bug, et l'apport visuel sur chacune.

### H1 — Snapshot inventaire figé entre les cycles (la liste d'UID à déposer est cachée et pas rafraîchie)

- **Pour** : 175310 montre des **mêmes catégories d'items** que 175248 dans l'inventaire — si chaque cycle relit l'inventaire actuel, on s'attendrait à vider plus. Cohérent avec un snapshot pris au cycle 1 puis réutilisé partiellement.
- **Contre** : visuel insuffisant — on ne voit pas les UID ni les quantités exactes.
- **Verdict visuel** : **plausible**, non confirmable sans logs.

### H2 — Filtre catégorie Ressources cassé après refresh

- **Pour** : dans 175248 et 175310, l'onglet inventaire affiché est bien "Ressources" (icônes ressources visibles). Pas d'indication visuelle directe d'un filtre qui aurait basculé.
- **Contre** : si le filtre était cassé, l'inventaire afficherait des items hors-ressources ou rien. Ici, on voit des ressources standard (céréales, bois, fruits, poissons, gemmes).
- **Verdict visuel** : **peu probable**.

### H3 — Quantités banque/inventaire désynchronisées (le serveur a fusionné des stacks et le bot ne l'a pas vu)

- **Pour** : si le serveur a fusionné en banque côté droit (175248 montre la banque pleine), les UID inventaire ont pu changer, et le bot continue à demander des UID inexistants.
- **Verdict visuel** : **plausible** mais non confirmable sans logs.

### H4 — Délai trop court entre 2 paquets EMO+ → le serveur drop silencieusement les suivants

- **Pour** : cohérent avec la note MEMORY `Findings cadernis+dyshay 2026-05-28` qui rappelle "Banque dyshay = burst EMO+ 300ms SANS wait OR". Si Luffy-bot envoie trop vite ou n'attend pas l'ack, des dépôts manquent.
- **Verdict visuel** : **non observable** sur screenshot — exige les logs paquets de la fenêtre Luffy-bot pendant le dépôt.

### H5 — Pods inventaire / map non rechargée après le 1er dépôt

- **Pour** : si la barre Pods n'est pas mise à jour entre cycles, le bot peut croire qu'il est encore overweight et stop, ou inversement.
- **Visuel** : la barre Pods est présente sur 175310 mais illisible.
- **Verdict visuel** : **non confirmable**.

### H6 — Liste items à déposer construite avant ouverture banque, items pris/utilisés entre temps (combat, loot)

- **Pour** : 175310 montre un journal de combat actif ("Forgeron Sombre perd 130 PDV", "Beiloddurul perd 82 PDV") = **le perso est encore en combat ou vient de combattre**. Si le pipeline farm/banque enchaîne combat → banque sans attendre la fin de loot OQ, l'inventaire change pendant que la banque travaille.
- **Verdict visuel** : **fortement plausible**, c'est le seul indice "factuel" supplémentaire — il y a une **interaction combat ↔ banque** sur la même séquence temporelle.

### H7 — Banque fermée prématurément (EK envoyé trop tôt)

- **Pour** : 175310 montre la banque fermée alors que l'inventaire est encore plein. Compatible avec un EK envoyé après seulement quelques EMO+.
- **Verdict visuel** : **plausible**.

### H8 — Catégorie "Ressources" coche UI désélectionnée à chaud entre cycles

- **Pour** : non visible (l'UI Luffy-bot config banque n'apparaît sur aucun screenshot).
- **Verdict visuel** : **non observable**.

---

**Synthèse forensic** : la séquence 175248 → 175310 confirme visuellement le **symptôme** (banque ouverte+fermée, inventaire toujours rempli de ressources des mêmes catégories), mais ne permet pas seule de trancher entre les hypothèses H1, H3, H4, H6, H7. L'élément le plus saillant est H6 (interférence combat/banque) car le journal combat actif est visible sur le screenshot post-fermeture banque, suggérant que le pipeline farm → banque se déclenche sans isolation propre de la phase combat.

Pour aller plus loin : il faudrait des screenshots de la **console Luffy-bot** (onglet Bot → Console, catégorie BANQUE/PAQUETS) capturés *pendant* le dépôt incomplet pour voir les `EMO+`, `OR`, `EK`, `OQ`, et le détail du snapshot inventaire envoyé au `PiloteBanque`.
