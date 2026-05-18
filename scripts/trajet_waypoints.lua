-- =====================================================================
-- TRAJET A POINTS FIXES (waypoints)
-- Exemple : suivre une route de cellules (x,y), parler a un PNJ,
-- changer de map, recolter, en boucle.
-- Adapte les coordonnees a ta zone (survole les cases : "(x, y)" en haut).
-- =====================================================================

bot.log("Trajet waypoints demarre")

-- Liste de points : { x, y } sur la carte courante
local route = {
  { 0, 0 },
  { 3, -2 },
  { 5, 0 },
}

while bot.actif() do
  for _, p in ipairs(route) do
    if not bot.actif() then break end
    bot.log("-> cellule (" .. p[1] .. "," .. p[2] .. ")")
    bot.aller_xy(p[1], p[2])
    bot.attendre(600)

    -- recolte ce qui est sur cette case/zone si possible
    if bot.nb_recoltables() > 0 then
      bot.recolter_tout()
    end

    if bot.est_en_combat() then
      bot.attendre_fin_combat()
    end
  end

  -- exemple : changer de map vers l'est puis recommencer la route
  bot.changer_map("est")
  bot.attendre(3500)
end

bot.log("Trajet waypoints arrete")
