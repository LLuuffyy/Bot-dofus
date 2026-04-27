-- =============================================================================
-- Demo Lua : Configuration IA combat pour un Iop
--
-- Configure une rotation de sorts simple destinée à un Iop bas-moyen niveau.
-- L'IA combat (BoucleIaCombat) lit cette config à chaque tour.
--
-- Usage :
--   Vue Scripts → Charger script .lua → demo_combat_iop.lua → Démarrer
--
-- ⚠ MODE ACTIF UNIQUEMENT : ce script ne fait que CONFIGURER ;
--   ce sera l'IA qui décidera d'envoyer les sorts en jeu.
-- =============================================================================

bot.log("=== Configuration IA combat — Iop ===")

-- On repart d'une config vierge
bot.config_combat_vider()

-- Stratégie : agressif (cherche à infliger un max de dégâts)
bot.config_combat_strategie("agressif")

-- Rotation par priorité décroissante (le 1er trouvable gagne)
-- Priorité haute = utilisé en premier si applicable.

-- Pied du Sacrieur (4 PA, CaC) — gros burst
bot.config_combat_ajouter_sort_nom("Pied du Sacrieur", 30, "ennemi_proche")

-- Bond (3 PA, déplacement) — pour se rapprocher
bot.config_combat_ajouter_sort_nom("Bond", 25, "ennemi_proche")

-- Pression (4 PA, distance) — fallback range
bot.config_combat_ajouter_sort_nom("Pression", 20, "ennemi_proche")

-- Intimidation (2 PA, debuff)
bot.config_combat_ajouter_sort_nom("Intimidation", 10, "ennemi_proche")

bot.log("Configuration terminée — l'IA prendra le relais en combat.")
bot.log("Stratégie active : agressif")
bot.log("Astuce : modifiez ce fichier pour ajuster la rotation.")
