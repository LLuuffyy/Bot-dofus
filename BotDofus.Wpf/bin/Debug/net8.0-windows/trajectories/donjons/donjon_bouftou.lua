-- ============================================
-- Script Donjon Bouftou (Velora)
-- Lancer depuis la map d'entrée (2,-34)
-- ============================================

SHOW_FIGHT_COUNTER = true
MAX_PODS = 90

AUTO_REGEN =
{
	MIN_HP = 50,
	MAX_HP = 90,
	ITEMS = { 528 } -- Pain de blé complet
}

DUNGEON_MAPS = { 2079, 2080, 2081, 2082, 2083, 2084 }
SOUL_CAPTURE = { spell = 413, map = 2083 } -- Capture d'âme sur le boss (salle 5)

function movement()
	return
	{
		-- Entrée du donjon : parler au PNJ model 31255, choix 1 > choix 1
		{ map = "1856", npc = 31255, answers = { -1, -1 } },

		-- Salle 1
		{ map = "2079", fight = true },

		-- Salle 2
		{ map = "2080", fight = true },

		-- Salle 3
		{ map = "2081", fight = true },

		-- Salle 4
		{ map = "2082", fight = true },

		-- Salle 5 - Boss Bouftou Royal
		{ map = "2083", fight = true },

		-- Salle 6 - Sortie : parler au PNJ model 177, choix 1
		{ map = "2084", npc = 177, answers = { -1 } },
	}
end

function bank()
	return
	{
		-- Velora : dépôt à distance
		{ map = "1856", npc_bank = true },
	}
end
