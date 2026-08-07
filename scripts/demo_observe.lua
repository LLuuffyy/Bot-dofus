-- =============================================================================
-- Demo Lua : Observation passive
--
-- Script-démo qui ne fait que LIRE l'état du jeu et le journaliser.
-- Aucun paquet n'est envoyé au serveur — sûr pour Mode Passif comme Mode Actif.
--
-- Usage :
--   Vue Scripts → Charger script .lua → demo_observe.lua → Démarrer
-- =============================================================================

bot.log("=== Démo observation démarrée ===")

bot.log("Personnage : " .. bot.nom() .. " (niveau " .. bot.niveau() .. ")")
bot.log(string.format("Vie       : %d / %d (%.1f%%)", bot.vie(), bot.vie_max(), bot.vie_pct()))
bot.log("PA / PM   : " .. bot.pa() .. " / " .. bot.pm())
bot.log("Kamas     : " .. bot.kamas())
bot.log(string.format("Poids     : %d / %d (%.1f%%)", bot.poids(), bot.poids_max(), bot.poids_pct()))

local carte = bot.carte()
local pos   = bot.position()
if carte then
    bot.log("Carte courante : " .. carte .. ", cellule " .. (pos or "?"))
    local coords = bot.map_coords(carte)
    bot.log(string.format("Coordonnées    : (%d, %d)", coords.x, coords.y))
else
    bot.log("Aucune carte chargée pour le moment")
end

if bot.est_en_combat() then
    bot.log("⚔ En combat actuellement")
else
    bot.log("Hors combat — " .. bot.nb_monstres() .. " monstre(s) sur la map")
end

-- Inventaire : on liste les 5 premiers objets
bot.log("--- Inventaire (5 premiers objets) ---")
local inv = bot.inventaire()
local n = 0
for _, obj in pairs(inv) do
    n = n + 1
    if n > 5 then break end
    bot.log(string.format("  [%d] %s x%d (slot %d)", obj.template, obj.nom, obj.quantite, obj.position))
end
bot.log("Total objets : " .. (#inv or n))

-- Boucle d'observation : log la vie toutes les 10s pendant 1 minute
bot.log("--- Surveillance vie (60s) ---")
for i = 1, 6 do
    bot.attendre(10000)
    bot.log(string.format("[t+%ds] Vie = %d / %d", i * 10, bot.vie(), bot.vie_max()))
end

bot.log("=== Démo terminée ===")
