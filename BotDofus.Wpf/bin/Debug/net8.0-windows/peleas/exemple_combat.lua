-- Script exemple : configure le combat puis log l'état perso toutes les 5 secondes.
-- Lancer via le moteur Lua interactif (Lua.Charger + Lua.Demarrer dans le bot).

bot.log("=== Démarrage script exemple ===")
bot.log("Personnage : " .. bot.nom() .. " (niveau " .. bot.niveau() .. ")")

-- Configuration combat : stratégie agressive + 2 sorts
bot.config_combat_vider()
bot.config_combat_strategie("agressif")

bot.config_combat_ajouter_sort{
    id = 413,            -- Pied du Sacrieur (à adapter selon ta classe)
    priorite = 10,
    cout_pa = 4,
    portee_min = 1,
    portee_max = 1,      -- corps à corps
    cible = "ennemi_proche"
}

bot.config_combat_ajouter_sort{
    id = 419,            -- Attirance (idem, ID à adapter)
    priorite = 8,
    cout_pa = 2,
    portee_min = 2,
    portee_max = 6,
    cible = "ennemi_proche"
}

bot.log("Combat configuré : " .. 2 .. " sorts, stratégie=agressif")

-- Boucle de monitoring : log état toutes les 5 secondes
local iterations = 0
while iterations < 12 do  -- 12 × 5s = 1 minute, puis stop
    iterations = iterations + 1

    local pos = bot.position() or -1
    local carte_id = bot.carte() or -1
    local pv = bot.vie()
    local pv_max = bot.vie_max()
    local kamas = bot.kamas()
    local en_combat = bot.est_en_combat()

    bot.log(string.format(
        "[#%d] Carte=%d Cell=%d  PV=%d/%d  Kamas=%d  Combat=%s  Mobs=%d",
        iterations, carte_id, pos, pv, pv_max, kamas,
        tostring(en_combat), bot.nb_monstres()
    ))

    bot.attendre(5000)
end

bot.log("=== Script terminé ===")
