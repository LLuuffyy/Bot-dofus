-- =====================================================================
-- EXEMPLE — Farm avec marchand auto (aller seul, retour dans mouvement)
-- =====================================================================
-- Globals :
--   MAX_PODS               : % poids déclenche le détour BANQUE (auto)
--   MARCHAND_SEUIL_PODS    : % poids déclenche le détour MARCHAND
--   MIN_MONSTERS / MAX_MONSTERS : taille groupe acceptable
--   OK_MONSTER / NO_MONSTER     : whitelist / blacklist gabarits
--
-- Flags d'étape :
--   forcefight = true  → boucle combats sur la map (zone farm)
--   fight = true       → 1 seul combat puis étape suivante
--   npc_marchand = true + npc = X → vente AUTO au PNJ X
--
-- COMPORTEMENT :
--   1. Bot reste sur l'étape forcefight=true tant qu'il y a des mobs
--      (re-engage). Quand plus de mobs sur la map, il reste sur la même
--      étape (boucle de check + delai 1s) — pas d'avance prématurée.
--   2. Quand pods >= MARCHAND_SEUIL_PODS, le bot suit marchand() (aller
--      seul jusqu'au PNJ + vente).
--   3. Après vente, le bot est sur la map du PNJ (ex. 7573). Il cherche
--      dans mouvement() une étape avec cette même map → reprend là, ce
--      qui démarre le chemin retour vers la zone farm.
-- =====================================================================

SHOW_FIGHT_COUNTER = true
MAX_PODS = 90
MARCHAND_SEUIL_PODS = 80
MIN_MONSTERS = 1
MAX_MONSTERS = 8
OK_MONSTER = {}
NO_MONSTER = {}
ELEMENTS_TO_GATHER = {}

-- =====================================================================
-- mouvement() — Zone farm + chemin retour depuis taverne.
-- Structure attendue :
--   [0]      = map de farm avec forcefight=true (boucle ici par défaut)
--   [1..N-1] = maps du chemin retour depuis le PNJ (incluant la map
--              du PNJ elle-même en première étape retour). Atteintes
--              UNIQUEMENT après le détour marchand().
-- =====================================================================
function mouvement()
    return {
        -- === ZONE FARM (le bot reste ici par défaut) ===
        { map = "7804", forcefight = true, path = "raw:GA001haw", from = 36, cell = 22 },

        -- === RETOUR : commence par la MAP DU PNJ (7573) puis remonte ===
        -- Ces étapes sont parcourues UNIQUEMENT quand le bot finit
        -- marchand() sur la map 7573 → il cherche 7573 ici → trouve
        -- étape 1 → reprend à partir d'ici jusqu'à revenir sur 7804.
        { map = "7573", path = "raw:GA001efGfhd-fbA",   from = 352, cell = 457 },
        { map = "7429", path = "raw:GA001cgRbhj",        from = 282, cell = 457 },
        { map = "7430", path = "raw:GA001dcJcdadd4ed3",  from = 37,  cell = 247 },
        { map = "7414", path = "raw:GA001dgSchj",        from = 260, cell = 457 },
        { map = "7415", path = "raw:GA001cbTbcPcgUbhm",  from = 51,  cell = 460 },
        { map = "7416", path = "raw:GA001dcichh",        from = 38,  cell = 455 },
        { map = "7417", path = "raw:GA001cePdftcgQbhi", from = 36,  cell = 456 },
        { map = "7761", path = "raw:GA001cg6",           from = 36,  cell = 442 },
        { map = "7762", path = "raw:GA001cgpdg5",        from = 51,  cell = 441 },
        { map = "7763", path = "raw:GA001cbSdcicd8deychg", from = 50, cell = 454 },
        { map = "7799", path = "raw:GA001bbEcc1bdecd-bencg7dhj", from = 34, cell = 457 },
        -- Fin retour → étape 0 (zone farm 7804) reprend forcefight
    }
end

-- =====================================================================
-- marchand() — TRAJET ALLER SEUL vers la taverne (PNJ 464).
-- Pas de retour : le retour est dans mouvement() via la convention
-- "map du PNJ + maps suivantes = chemin retour".
-- =====================================================================
function marchand()
    return {
        { map = "7804", path = "raw:GA001haw",               from = 36,  cell = 22 },
        { map = "7799", path = "raw:GA001gdWfdHgdefc1gbEfat", from = 443, cell = 19 },
        { map = "7763", path = "raw:GA001fgPgdbhcLgbohbagaJ", from = 440, cell = 35 },
        { map = "7762", path = "raw:GA001ge2hd-gaZfaK",      from = 426, cell = 36 },
        { map = "7761", path = "raw:GA001hgDgaKfav",         from = 427, cell = 21 },
        { map = "7417", path = "raw:GA001fgQgfthePgcyfbRgaXhav", from = 441, cell = 21 },
        { map = "7416", path = "raw:GA001gfehdIgax",         from = 440, cell = 23 },
        { map = "7415", path = "raw:GA001gepfeagbsfaw",      from = 445, cell = 22 },
        { map = "7414", path = "raw:GA001heH",               from = 443, cell = 289 },
        { map = "7430", path = "raw:GA001hbqgaw",            from = 276, cell = 22 },
        { map = "7429", path = "raw:GA001gfhfePgem",         from = 443, cell = 268 },
        -- ARRIVÉE TAVERNE : npc_marchand=true → VENTE AUTO
        { map = "7573", npc = 464, npc_marchand = true, path = "raw:GA001dfG", from = 338, cell = 352 },
    }
end

-- banque() : NON UTILISÉ. Système banque autonome (config UI onglet
-- Banque + PiloteBanque) dépose à MAX_PODS%.
function banque() return {} end
function phenix() return {} end
