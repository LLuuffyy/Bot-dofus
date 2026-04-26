-- Template Donjon Generique
-- Adapte les map IDs, cellules, NPC et reponses a ton donjon

-- Config
local MAP_ENTREE_DONJON = 10000  -- Map devant le donjon (avec le PNJ)
local MAP_SORTIE_DONJON = 10006  -- Map ou tu sors apres le boss
local MAP_RETOUR_1 = 10007       -- Map intermediaire pour revenir (sans combat)
local MAP_RETOUR_2 = 10008       -- Map intermediaire 2

local NPC_ENTREE = 999           -- ID du PNJ devant le donjon
local NPC_REPONSES = {0, 0}      -- Reponses au dialogue du PNJ

function mouvement()
    return {
        -- === ENTREE DONJON : Parler au PNJ ===
        {
            map = MAP_ENTREE_DONJON,
            npc = NPC_ENTREE,
            reponses = NPC_REPONSES
        },

        -- === SALLE 1 ===
        {
            map = 10001,
            combat = true,
            cell = 300
        },

        -- === SALLE 2 ===
        {
            map = 10002,
            combat = true,
            cell = 350
        },

        -- === SALLE 3 ===
        {
            map = 10003,
            combat = true,
            cell = 280
        },

        -- === SALLE BOSS ===
        {
            map = 10004,
            combat = true,
            cell = 320,
            -- Active la capture d'ame sur le boss si besoin :
            -- custom = function() CAPTURE_AME = true end
        },

        -- === SORTIE DONJON (teleport auto apres boss) ===
        {
            map = MAP_SORTIE_DONJON,
            cell = 250,
            direction = "BOTTOM"
        },

        -- === RETOUR VERS LE DONJON (SANS COMBAT) ===
        {
            map = MAP_RETOUR_1,
            combat = false,
            cell = 200,
            direction = "LEFT"
        },

        {
            map = MAP_RETOUR_2,
            combat = false,
            cell = 150,
            direction = "TOP"
        },

        -- === RETOUR DEVANT LE DONJON : Reparler au PNJ (boucle) ===
        {
            map = MAP_ENTREE_DONJON,
            npc = NPC_ENTREE,
            reponses = NPC_REPONSES
        },
    }
end

function banque()
    return {
        -- Chemin vers la banque quand pods pleins
        -- Adapte avec les maps entre le donjon et la banque
        {
            map = MAP_ENTREE_DONJON,
            cell = 100,
            direction = "RIGHT"
        },
        -- ... maps intermediaires vers la banque ...
        {
            map = 9999,         -- Map de la banque
            npc_banque = true   -- Velora : depot a distance via XD8
        },
        -- ... maps retour vers le donjon ...
        {
            map = MAP_ENTREE_DONJON,
            cell = 200
        },
    }
end
