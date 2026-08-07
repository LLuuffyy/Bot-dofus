-- =====================================================================
-- SCRIPT FORMAT ANKABOT (Dofus Retro)  -  https://doc.ankabot.dev
-- Structure standard : move() / bank() / phenix() + return { ... }
-- Le moteur trouve la ligne dont map == carte courante, execute ses
-- actions (gather/fight/door/custom) puis change de carte via path.
-- Onglet Scripts -> Charger -> Demarrer. Arreter = stop propre.
-- Adapte les coordonnees "x,y" a TA zone (survol case = (x,y) en haut).
-- =====================================================================

AUTO_DELETE  = {}
GATHER       = {}
MIN_MONSTERS = 1
MAX_MONSTERS = 8

function move()
  return {
    { map = "5,-18", path = "bottom", gather = true },
    { map = "5,-17", path = "right",  gather = true },
    { map = "6,-17", path = "top",    gather = true },
    { map = "6,-18", path = "left",   gather = true },
    -- exemple combat puis sortie : { map = "7,12", fight = true, path = "right" }
    -- exemple porte             : { map = "4,-20", door = "254" }
    -- exemple fonction perso    : { map = "0,0", custom = maRoutine, path = "top" }
  }
end

-- Appelee automatiquement quand l'inventaire est plein (pods >= 98%)
function bank()
  return {
    -- route vers la banque + { map = "...", npcBank = true }
  }
end

-- Appelee si le personnage meurt
function phenix()
  return {}
end

function stopped() printText("Script arrete") end
function banned()  printText("Compte banni !") end

-- Exemple de fonction custom utilisable dans une ligne move()
function maRoutine()
  printText("Custom : " .. character.name() .. " pods " .. inventory.podsP() .. "%")
end
