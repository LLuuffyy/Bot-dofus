-- =====================================================================
-- TRAJET DE RÉCOLTE EN BOUCLE
-- Récolte tout ce que ton métier permet sur la map, change de map, recommence.
-- Onglet Scripts → Charger → Démarrer. « Arrêter » le stoppe proprement.
-- =====================================================================

bot.log("Trajet récolte démarré")

local sorties = { "est", "sud", "ouest", "nord" }
local i = 1

while bot.actif() do
  if bot.est_en_combat() then
    bot.log("Combat en cours, attente...")
    bot.attendre_fin_combat()
  end

  local n = bot.nb_recoltables()
  if n > 0 then
    bot.log(n .. " ressource(s) ici -> recolte")
    bot.recolter_tout()
  else
    local dir = sorties[i]
    i = i + 1
    if i > #sorties then i = 1 end
    bot.log("Rien a recolter -> sortie " .. dir)
    bot.changer_map(dir)
    bot.attendre(3500)
  end

  bot.attendre(800)
end

bot.log("Trajet recolte arrete")
