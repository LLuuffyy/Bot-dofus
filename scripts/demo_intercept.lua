-- =============================================================================
-- Demo Lua : Interception furtive (Phase 2)
--
-- Démontre la modification de paquets à la volée DANS le flux du vrai client.
-- C'est la méthode la plus furtive : le serveur voit des paquets parfaitement
-- formés, au bon timing, avec les vrais keep-alives du client officiel.
--
-- ⚠ MODE ACTIF requis (décoche "Mode passif" dans le header).
--
-- Usage :
--   Vue Scripts → Charger demo_intercept.lua → Démarrer
--   (ou copie en scripts/<identifiant>.lua pour auto-start au login)
-- =============================================================================

bot.log("=== Demo interception démarrée ===")

-- On repart propre
bot.intercepter_vider()

-- ---------------------------------------------------------------------------
-- Règle 1 : OBSERVER tout le trafic combat (lecture seule, ne modifie rien)
-- ---------------------------------------------------------------------------
bot.intercepter("observe-combat", "G", function(p, sens)
    -- G* = paquets de jeu (GA déplacement, GM map, GS combat...)
    bot.log("[" .. sens .. "] " .. p)
    return nil          -- nil = laisser passer inchangé
end)

-- ---------------------------------------------------------------------------
-- Règle 2 : LOG des messages de chat reçus (Bm = broadcast message)
-- ---------------------------------------------------------------------------
bot.intercepter("log-chat", "cMK", function(p, sens)
    bot.log("CHAT reçu : " .. p)
    return nil
end)

-- ---------------------------------------------------------------------------
-- Règle 3 (COMMENTÉE — exemple de MODIFICATION) :
-- Réécrit la destination d'un déplacement. Décommente pour tester :
-- quand TU cliques pour bouger, le bot change la cellule cible.
-- ---------------------------------------------------------------------------
-- bot.intercepter("redir-deplacement", "GA", function(p, sens)
--     if sens == "C2S" then
--         bot.log("Déplacement intercepté : " .. p)
--         -- exemple : on ne modifie pas ici, juste on observe.
--         -- pour modifier réellement il faudrait reconstruire le path encodé.
--     end
--     return nil
-- end)

bot.log("3 règles installées (2 actives, 1 en exemple commenté).")
bot.log("Décoche 'Mode passif' pour que l'interception agisse.")
bot.log("Kill-switch : bot.intercepter_actif(false)")
