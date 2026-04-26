-- Script généré par SynFus Script Recorder
-- Nom: DJ MP | Date: 2026-04-14 17:17

SHOW_FIGHT_COUNTER = true
DUNGEON_MAPS = { 8167, 8282, 8328, 8330, 8331, 8497 }
SOUL_CAPTURE = { spell = 2430, map = 8331 }

function mouvement()
    return {
        { map = "8037", direction = "LEFT" },
        { map = "8024", direction = "LEFT" },
        { map = "8011", direction = "LEFT" },
        { map = "7998", direction = "LEFT" },
        { map = "7985", direction = "BOTTOM" },
        { map = "7986", direction = "LEFT" },
        { map = "7973", direction = "BOTTOM" },
        { map = "7974", direction = "LEFT" },
        { map = "7961", direction = "LEFT" },
        { map = "7948", direction = "LEFT" },
        { map = "8168", direction = "TOP" },
        { map = "8167", npc = 706, answers = { -1 } },
        { map = "8282", fight = true },
        { map = "8328", fight = true },
        { map = "8330", fight = true },
        { map = "8331", fight = true },
        { map = "8497", npc = 705, answers = { -1 } },
    }
end

function banque()
    return {
    }
end
