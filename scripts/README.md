# Scripts Lua

Scripts d'exemple pour le bot. Charger via **Vue Scripts → Charger script .lua**.

## Scripts fournis

| Script | Description | Mode requis |
|---|---|---|
| `demo_observe.lua` | Observation passive : log des stats / inventaire / surveillance vie 60s | Passif ou Actif |
| `demo_combat_iop.lua` | Configure une rotation IA combat pour un Iop bas-moyen niveau | Actif (l'IA agit) |

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
