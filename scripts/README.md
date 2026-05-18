# Scripts Lua

Scripts d'exemple pour le bot. Charger via **Vue Scripts → Charger script .lua**.

## Scripts fournis

| Script | Description | Mode requis |
|---|---|---|
| `demo_observe.lua` | Observation passive : log des stats / inventaire / surveillance vie 60s | Passif ou Actif |
| `demo_combat_iop.lua` | Configure une rotation IA combat pour un Iop bas-moyen niveau | Actif (l'IA agit) |

## Auto-script

Si un fichier `scripts/<identifiant_compte>.lua` existe (ex. `scripts/zel.lua`), il est **chargé et démarré automatiquement** à l'attache de la session jeu.

Pour activer le démo combat sur le compte `zel`, il suffit de :

```bash
copy scripts/demo_combat_iop.lua scripts/zel.lua
```

## API Lua disponible

Toutes les fonctions sont sur l'objet global `bot.*`. Voir `Divers/Scripts/Api/ApiLua.cs` pour la liste complète.

### État perso
- `bot.nom()`, `bot.niveau()`, `bot.vie()`, `bot.vie_max()`, `bot.vie_pct()`
- `bot.kamas()`, `bot.pa()`, `bot.pm()`
- `bot.carte()`, `bot.position()`
- `bot.poids()`, `bot.poids_max()`, `bot.poids_pct()`

### État monde
- `bot.est_en_combat()`
- `bot.nb_monstres()`

### Actions (envoient des paquets — Mode Actif uniquement)
- `bot.deplacer(cellule)`
- `bot.dire(canal, texte)` — `*` = général, `%` = guilde, `$` = équipe
- `bot.travel(x, y)` — `.travel` Hystoria
- `bot.attendre(ms)`
- `bot.finir_tour()`

### Configuration IA combat
- `bot.config_combat_vider()`
- `bot.config_combat_strategie("agressif" | "tactique" | "defensif" | "soutien" | "passif")`
- `bot.config_combat_ajouter_sort_nom(nom, priorite, cible)` — `cible` ∈ {`ennemi_proche`, `ennemi_faible`, `ennemi_fort`, `soi`, `allie_blesse`}

### Bases de données
- `bot.item_nom(id)`, `bot.item_id(nom)`
- `bot.monstre_nom(id)`, `bot.monstre_niveau(id)`, `bot.monstre_pv(id)`
- `bot.sort_id(nom)`, `bot.sort_nom(id)`, `bot.sort_cout_pa(id)`, `bot.sort_portee(id)`
- `bot.map_coords(id)`

### Inventaire
- `bot.inventaire()` — table `{ id, template, nom, quantite, position }[]`
- `bot.inventaire_compter(template_id)`

## Logs

`bot.log(msg)` → console (niveau Info)
`bot.avertir(msg)` → console (niveau Avertissement)

## Trajets / routes (scripting de déplacement & farm)

`bot.actif()` → false quand on clique Arrêter (boucle : `while bot.actif() do ... end`)
`bot.aller_xy(x, y)` → va sur la cellule (x,y) de la carte (pathfinding)
`bot.deplacer(cell)` → va sur une cellule par id
`bot.changer_map("est")` → sortie nord/sud/est/ouest (change de map)
`bot.recolter(cell)` → récolte la ressource d'une cellule (skill auto)
`bot.recolter_tout()` → récolte toutes les ressources du métier sur la map (renvoie le nombre)
`bot.nb_recoltables()` → nb de ressources récoltables ici par ce perso
`bot.engager_proche()` → engage le groupe de monstres le plus proche
`bot.attendre_fin_combat()` → bloque jusqu'à la fin du combat
`bot.parler_pnj(idPnj)` / `bot.repondre(question, reponse)` / `bot.quitter_dialogue()`
`bot.pos_x()` / `bot.pos_y()` → coordonnées du perso sur la carte
`bot.attendre(ms)` → pause (interrompue net à l'arrêt du script)

Exemples fournis : `trajet_recolte.lua` (récolte en boucle multi-maps),
`trajet_waypoints.lua` (route à points fixes).

## Scripts format AnkaBot (https://doc.ankabot.dev)

Le moteur reconnaît les scripts standard AnkaBot (Dofus Retro) : si le
script définit une fonction `move()`, il pilote la route automatiquement.

Structure :
```lua
function move() return {
  { map="5,-18", path="bottom", gather=true },
  { map="7,12",  fight=true,    path="right" },
  { map="4,-20", door="254" },
  { map="0,0",   custom=maFn,   path="top" },
} end
function bank()  return { ... } end   -- appelée si pods >= 98%
function phenix() return { ... } end  -- appelée si mort
```

Actions par ligne : `gather`/`forcegather`, `fight`/`forcefight`,
`door="cell"`, `custom=fn`, `lockedCustom=fn`, `path`.
`path` : `"top|bottom|left|right"` (=nord/sud/ouest/est),
`"top(364)"` (sortie via cellule), `"364"` (cellule déclencheuse),
`"a|b"` (aléatoire). `zaap/zaapi/havenbag/npcBank` : stub (à venir).

Modules type AnkaBot disponibles : `character`, `map`, `inventory`,
`npc`, `fight`, `chat`, `job`, `exchange`, `mount`, `quest` +
globaux `delay(ms)`, `print/printText/printError`.
Exemple : `anka_recolte.lua`.
