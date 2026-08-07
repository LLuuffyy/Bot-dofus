-- Script généré par SynFus Script Recorder
-- Nom: dj bouf | Date: 2026-04-24 11:31

SHOW_FIGHT_COUNTER = true
DUNGEON_MAPS = { 1856, 2073, 2074, 2082, 2084 }

function mouvement()
    return {
        { map = "6954", direction = "TOP" },
        { map = "6942", direction = "RIGHT" },
        { map = "1857", direction = "TOP" },
        { map = "1856", npc = 174, answers = { -1 } },
        { map = "2073", fight = true },
        { map = "2074", fight = true },
        { map = "2082", fight = true },
        { map = "2083", fight = true },
        { map = "2084", npc = 177, answers = { -1 } },
    }
end

function banque()
    return {
    }
end
