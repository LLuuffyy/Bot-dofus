# Scripts Lua — Documentation

## Setup

1. Crée un dossier `scripts/` dans le répertoire du bot :
   ```
   C:\Users\touki\Desktop\Bot-dofus\.claude\worktrees\vigorous-murdock\temp-clone\bin\Debug\net8.0-windows\scripts\
   ```

2. Crée un fichier `<identifiant_compte>.lua` (ex: `zel.lua`)

3. Lance le bot — quand la session game s'attache, le script s'exécute automatiquement.

## API Lua disponible

Toutes les méthodes sont accessibles via l'objet global `bot`.

### Logging
```lua
bot.log("message info")
bot.avertir("message warning")
```

### État perso
```lua
bot.nom()        -- string : pseudo du perso
bot.niveau()     -- int : niveau
bot.vie()        -- int : PV actuels
bot.vie_max()    -- int : PV max
bot.vie_pct()    -- double : pourcentage 0-100
bot.kamas()      -- long : kamas
bot.pa()         -- int : Points d'Action (en combat)
bot.pm()         -- int : Points de Mouvement (en combat)
bot.carte()      -- int? : ID carte courante
bot.position()   -- int? : ID cellule courante
bot.poids()      -- int : poids actuel
bot.poids_max()  -- int : poids max
bot.poids_pct()  -- double : pourcentage poids
```

### État monde
```lua
bot.est_en_combat()  -- bool
bot.nb_monstres()    -- int : monstres sur la carte
```

### Actions (bloquantes)
```lua
bot.deplacer(cellule_id)        -- déplace vers cellule (utilise pathfinder A*)
bot.dire("*", "Hello")           -- envoie message canal *=général, %=guilde, $=équipe, etc.
bot.attendre(milliseconds)       -- attend X ms
bot.finir_tour()                  -- termine le tour combat
```

### Configuration combat (utilisée par l'IA combat automatique)

```lua
bot.config_combat_vider()
bot.config_combat_strategie("agressif")   -- "agressif" | "tactique" | "defensif" | "soutien" | "passif"

bot.config_combat_ajouter_sort{
    id = 413,                -- ID du sort
    priorite = 10,           -- plus haut = utilisé en premier
    cout_pa = 4,             -- coût en PA
    portee_min = 1,          -- portée min
    portee_max = 1,          -- portée max
    cible = "ennemi_proche"  -- "ennemi_proche" | "ennemi_faible" | "ennemi_fort" | "soi" | "allie_blesse"
}
```

Dès qu'un combat démarre, l'IA combat applique automatiquement ces sorts dans l'ordre de priorité.

## Exemple de farming complet

```lua
bot.log("Démarrage farming auto")

-- Configure les sorts une fois
bot.config_combat_vider()
bot.config_combat_strategie("agressif")
bot.config_combat_ajouter_sort{ id = 413, priorite = 10, cout_pa = 4, portee_min = 1, portee_max = 1, cible = "ennemi_proche" }
bot.config_combat_ajouter_sort{ id = 419, priorite = 8,  cout_pa = 2, portee_min = 2, portee_max = 6, cible = "ennemi_proche" }

-- Trajet : aller-retour entre 2 cellules avec attaque des mobs en chemin
local cellules_trajet = { 250, 350 }  -- IDs cellules à patrouiller

while bot.vie_pct() > 30 and bot.poids_pct() < 90 do
    for _, cell in ipairs(cellules_trajet) do
        if bot.est_en_combat() then
            -- L'IA combat gère, on attend
            while bot.est_en_combat() do
                bot.attendre(2000)
            end
            bot.log("Combat terminé")
        else
            bot.log("Déplacement vers cellule " .. cell)
            bot.deplacer(cell)
            bot.attendre(3000)  -- attend que le déplacement se résolve
        end
    end
end

bot.log("Pause farming : PV bas ou inventaire plein")
```

## Architecture

```
Script Lua (.lua)
     │ via MoonSharp
     ▼
ApiLua (wrapper)
     │
     ├──→ ApiBot (déplacement, chat, etc.) → SessionProxy → packets game
     ├──→ EtatJeu (lecture position, vie, mobs)
     └──→ ConfigCombat (config sorts pour IA combat)

Quand un combat démarre (packet GS), BoucleIaCombat se déclenche automatiquement
et applique les RegleSort configurées via Lua.
```

## Limitations actuelles

- Pas de stop interactif : pour arrêter un script, il faut fermer le bot.
- Le pathfinding A* fonctionne mais l'encoding du chemin (packet GA001) n'a pas encore été
  testé en jeu réel — peut nécessiter des ajustements.
- Les coordonnées (x,y) des cellules sont calculées avec la formule Dofus standard
  (mapWidth=14) mais certaines cartes spéciales peuvent avoir d'autres dimensions.
- L'IA combat ne gère pas encore : ligne de vue (LOS), placement initial, capture d'âme.
