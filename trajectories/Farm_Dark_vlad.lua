-- Script généré par SynFus Script Recorder
-- Nom: Farm Dark vlad | Date: 2026-04-18 08:13

SHOW_FIGHT_COUNTER = true
DUNGEON_MAPS = { 625, 310, 319, 318, 328, 327, 337, 347, 348, 338, 339, 340, 330, 329, 320, 321, 322, 332, 342, 410, 351, 361, 371, 370, 369, 368, 358, 357, 346, 411, 335, 325 }
SOUL_CAPTURE = { spell = 2430, map = 325 }

function mouvement()
    return {
        { map = "935", direction = "LEFT" },
        { map = "936", direction = "BOTTOM" },
        { map = "625", npc = 88, answers = { -1, -1 } },
        { map = "310", cell = 456 },
        { map = "319", cell = 131 },
        { map = "318", cell = 456 },
        { map = "328", cell = 218 },
        { map = "327", cell = 461 },
        { map = "337", cell = 455 },
        { map = "347", cell = 202 },
        { map = "348", cell = 22 },
        { map = "338", cell = 347 },
        { map = "339", cell = 173 },
        { map = "340", cell = 18 },
        { map = "330", cell = 305 },
        { map = "329", cell = 144 },
        { map = "330", cell = 23 },
        { map = "320", cell = 260 },
        { map = "321", cell = 202 },
        { map = "322", cell = 460 },
        { map = "332", cell = 460 },
        { map = "342", cell = 458 },
        { map = "410", cell = 276 },
        { map = "351", cell = 458 },
        { map = "361", cell = 456 },
        { map = "371", cell = 247 },
        { map = "370", cell = 131 },
        { map = "369", cell = 160 },
        { map = "368", cell = 23 },
        { map = "358", cell = 131 },
        { map = "357", cell = 22 },
        { map = "347", cell = 247 },
        { map = "346", cell = 24 },
        { map = "411", cell = 276 },
        { map = "335", cell = 21 },
        { map = "325", fight = true },
    }
end

function banque()
    return {
    }
end
