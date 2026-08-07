-- Script généré par SynFus Script Recorder
-- Nom: Bworker | Date: 2026-04-15 06:47

SHOW_FIGHT_COUNTER = true
DUNGEON_MAPS = { 4786, 7530, 7519, 7524, 7525, 7526 }
SOUL_CAPTURE = { spell = 2430, map = 7526 }

function mouvement()
    return {
        { map = "4739", direction = "RIGHT" },
        { map = "4740", direction = "RIGHT" },
        { map = "4880", direction = "RIGHT" },
        { map = "4786", npc = 712, answers = { -1 } },
        { map = "7530", cell = 164 },
        { map = "7519", fight = true },
        { map = "7524", fight = true },
        { map = "7525", fight = true },
        { map = "7526", fight = true },
    }
end

function banque()
    return {
    }
end
