-- Script généré par SynFus Script Recorder
-- Nom: Q tahn | Date: 2026-04-16 02:54

SHOW_FIGHT_COUNTER = true
DUNGEON_MAPS = { 4919, 11965, 11967, 11968, 11975 }
SOUL_CAPTURE = { spell = 2430, map = 11975 }

function mouvement()
    return {
	    { map = "4739", direction = "RIGHT" },
        { map = "4740", direction = "BOTTOM" },
        { map = "4949", direction = "RIGHT" },
        { map = "4947", direction = "BOTTOM" },
        { map = "4948", direction = "RIGHT" },
        { map = "4946", direction = "BOTTOM" },
        { map = "4801", direction = "BOTTOM" },
        { map = "4805", direction = "RIGHT" },
        { map = "4806", direction = "RIGHT" },
        { map = "4807", direction = "BOTTOM" },
        { map = "4811", direction = "RIGHT" },
        { map = "4910", direction = "RIGHT" },
        { map = "4925", direction = "RIGHT" },
        { map = "4899", direction = "RIGHT" },
        { map = "4909", direction = "RIGHT" },
        { map = "4915", direction = "TOP" },
        { map = "4919", npc = 98658, answers = { -1 } },
        { map = "11965", fight = true },
        { map = "11967", fight = true },
        { map = "11968", fight = true },
        { map = "11975", fight = true },
    }
end

function banque()
    return {
    }
end
