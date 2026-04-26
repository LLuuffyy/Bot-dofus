-- Script généré par SynFus Script Recorder
-- Nom: DJ tanu | Date: 2026-04-14 16:49

SHOW_FIGHT_COUNTER = true
DUNGEON_MAPS = { 8236, 8303, 8313, 8305, 8314, 8317 }
SOUL_CAPTURE = { spell = 2430, map = 8314 }

function mouvement()
    return {
        { map = "8037", direction = "RIGHT" },
        { map = "8050", direction = "RIGHT" },
        { map = "8063", direction = "TOP" },
        { map = "8062", direction = "RIGHT" },
        { map = "8075", direction = "TOP" },
        { map = "8074", direction = "RIGHT" },
        { map = "8087", direction = "RIGHT" },
        { map = "8216", direction = "BOTTOM" },
        { map = "8239", direction = "RIGHT" },
        { map = "8236", npc = 707, answers = { -1 } },
        { map = "8502", cell = 133 },
        { map = "8303", fight = true },
        { map = "8313", fight = true },
        { map = "8305", fight = true },
        { map = "8314", fight = true },
        { map = "8317", npc = 708, answers = { -1 } },
    }
end

function banque()
    return {
    }
end
