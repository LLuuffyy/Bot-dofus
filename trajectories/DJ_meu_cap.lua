-- Script généré par SynFus Script Recorder
-- Nom: DJ meu cap | Date: 2026-04-14 12:56

SHOW_FIGHT_COUNTER = true
DUNGEON_MAPS = { 9787, 9645, 9650, 9656, 9647, 9648, 9649 }
SOUL_CAPTURE = { spell = 2430, map = 9648 }

function mouvement()
    return {
        { map = "4739", direction = "TOP" },
        { map = "4737", direction = "TOP" },
        { map = "4694", direction = "TOP" },
        { map = "4686", direction = "TOP" },
        { map = "4680", direction = "TOP" },
        { map = "4676", direction = "TOP" },
        { map = "4672", direction = "TOP" },
        { map = "4656", direction = "TOP" },
        { map = "4654", direction = "TOP" },
        { map = "4651", direction = "TOP" },
        { map = "4742", direction = "TOP" },
        { map = "4760", cell = 243 },
        { map = "9787", npc = 779, answers = { -1 } },
        { map = "9645", cell = 185 },
        { map = "9650", fight = true },
        { map = "9656", fight = true },
        { map = "9647", fight = true },
        { map = "9648", fight = true },
        { map = "9649", cell = 433, direction = "BOTTOM" },
    }
end

function banque()
    return {
    }
end
