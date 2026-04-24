-- Script généré par SynFus Script Recorder
-- Nom: Nouveau DJ AA | Date: 2026-04-14 07:09

SHOW_FIGHT_COUNTER = true
DUNGEON_MAPS = { 9127, 8713, 8721, 8724, 8711, 10234 }
SOUL_CAPTURE = { spell = 2430, map = 10234 }

function mouvement()
    return {
        { map = "2191", direction = "TOP" },
        { map = "2072", direction = "LEFT" },
        { map = "2071", direction = "TOP" },
        { map = "2185", direction = "TOP" },
        { map = "2184", direction = "TOP" },
        { map = "2134", direction = "LEFT" },
        { map = "2133", direction = "TOP" },
        { map = "2131", direction = "LEFT" },
        { map = "2137", direction = "TOP" },
        { map = "2155", direction = "TOP" },
        { map = "2154", direction = "LEFT" },
        { map = "2181", direction = "BOTTOM" },
        { map = "9127", npc = 797, answers = { -1, -1 } },
        { map = "8713", fight = true },
        { map = "8721", fight = true },
        { map = "8724", fight = true },
        { map = "8711", fight = true },
        { map = "10234", fight = true },
    }
end

function banque()
    return {
    }
end
