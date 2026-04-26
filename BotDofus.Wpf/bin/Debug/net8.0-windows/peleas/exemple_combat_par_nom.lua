-- Exemple : configurer le combat en utilisant les NOMS des sorts (pas les IDs)
-- La base de sorts (Resources/data/spells_*.json) contient les 2166 sorts Hystoria

bot.log("=== Setup combat par nom de sort ===")

-- Recherche : afficher tous les sorts contenant "Sacrieur" pour voir les IDs
for _, ligne in ipairs(bot.sorts_chercher("Sacrieur")) do
    bot.log(ligne)
end

-- Configuration combat : on utilise les NOMS, BaseSorts résout l'ID auto
bot.config_combat_vider()
bot.config_combat_strategie("agressif")
bot.config_combat_ajouter_sort_nom("Pied du Sacrieur", 10, "ennemi_proche")
bot.config_combat_ajouter_sort_nom("Attirance",          8, "ennemi_proche")
bot.config_combat_ajouter_sort_nom("Châtiment Forcé",    7, "ennemi_proche")
bot.config_combat_ajouter_sort_nom("Dérobade",           5, "soi")

bot.log("Config combat prête. L'IA utilisera ces sorts dans l'ordre de priorité quand un combat démarre.")
