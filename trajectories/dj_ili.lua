-- Script généré par SynFus Script Recorder
-- Nom: dj ili | Date: 2026-04-16 03:14

SHOW_FIGHT_COUNTER = true
DUNGEON_MAPS = { 4383, 11971, 11973, 11974, 11976 }
SOUL_CAPTURE = { spell = 2430, map = 11976 }

function mouvement()
    return {
        { map = "3022", direction = "TOP" },
        { map = "3021", direction = "TOP" },
        { map = "3020", direction = "TOP" },
        { map = "3019", direction = "LEFT" },
        { map = "2979", direction = "TOP" },
        { map = "2978", direction = "TOP" },
        { map = "2977", direction = "TOP" },
        { map = "4421", direction = "TOP" },
        { map = "4389", direction = "TOP" },
        { map = "4387", direction = "TOP" },
        { map = "4383", npc = 98657, answers = { -1 } },
        { map = "11971", fight = true },
        { map = "11973", fight = true },
        { map = "11974", fight = true },
        { map = "11976", fight = true },
    }
end

function banque()
    return {
    }
end
