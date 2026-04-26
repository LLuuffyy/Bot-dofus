-- Script généré par SynFus Script Recorder
-- Nom: djblop | Date: 2026-04-21 21:30

SHOW_FIGHT_COUNTER = true
DUNGEON_MAPS = { 11878, 11879, 11880, 11885 }

function mouvement()
    return {
        { map = "11878", npc = 1034, answers = { -1 } },
        { map = "11879", fight = true },
        { map = "11880", fight = true },
        { map = "11885", fight = true },
        { map = "11886", npc = 1034, answers = { -1 } },
        { map = "11887", fight = true },
        { map = "11891", npc = 1034, answers = { -1 } },
    }
end

function banque()
    return {
    }
end
