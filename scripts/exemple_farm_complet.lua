-- =====================================================================
-- EXEMPLE — Farm avec marchand auto + force-fight par étape
-- =====================================================================
-- Globals Luffy-bot reconnus en haut de script :
--
--   SHOW_FIGHT_COUNTER     : affiche le compteur de combats à l'écran
--   MAX_PODS               : % poids déclenchant le détour BANQUE (auto,
--                            via la config banque/<perso>.json)
--   MARCHAND_SEUIL_PODS    : % poids déclenchant le détour MARCHAND
--   MIN_MONSTERS           : nb min mobs / groupe pour engager
--   MAX_MONSTERS           : nb max mobs / groupe pour engager
--   OK_MONSTER             : IdGabarit obligatoire dans le groupe (vide = pas de filtre)
--   NO_MONSTER             : IdGabarit à éviter
--   ELEMENTS_TO_GATHER     : IDs ressources à récolter
--
-- Flags d'étape (dans mouvement() / marchand()) :
--
--   { map="X", fight = true }       → 1 combat puis étape suivante
--   { map="X", forcefight = true }  → boucle combats sur cette map
--                                     tant qu'il y a des groupes
--   { map="X", npc_marchand = true, npc = 464 }
--                                   → arrivée taverne + vente auto
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
-- mouvement() — Boucle de farm circulaire.
-- L'étape de farm utilise `forcefight = true` pour rester sur la map
-- jusqu'à avoir tué tous les groupes.
-- Les maps de retour depuis la taverne sont aussi listées ici (pour que
-- le bot puisse y reprendre quand marchand() se termine sur la map 7573).
-- =====================================================================
function mouvement()
    return {
        -- ZONE DE FARM (forcefight = boucle combats jusqu'à vidée)
        { map = "7804", forcefight = true, path = "raw:GA001haw", from = 36, cell = 22 },

        -- Maps de RETOUR depuis taverne (parcourues si le bot revient
        -- depuis 7573 après marchand() — il chercher l'étape matchant
        -- sa map actuelle dans cette liste pour reprendre)
        { map = "7429", path = "raw:GA001cgRbhj", from = 282, cell = 457 },
        { map = "7430", path = "raw:GA001dcJcdadd4ed3", from = 37, cell = 247 },
        { map = "7414", path = "raw:GA001dgSchj", from = 260, cell = 457 },
        { map = "7415", path = "raw:GA001cbTbcPcgUbhm", from = 51, cell = 460 },
        { map = "7416", path = "raw:GA001dcichh", from = 38, cell = 455 },
        { map = "7417", path = "raw:GA001cePdftcgQbhi", from = 36, cell = 456 },
        { map = "7761", path = "raw:GA001cg6", from = 36, cell = 442 },
        { map = "7762", path = "raw:GA001cgpdg5", from = 51, cell = 441 },
        { map = "7763", path = "raw:GA001cbSdcicd8deychg", from = 50, cell = 454 },
        { map = "7799", path = "raw:GA001bbEcc1bdecd-bencg7dhj", from = 34, cell = 457 },
    }
end

-- =====================================================================
-- marchand() — TRAJET ALLER UNIQUEMENT vers le PNJ marchand.
-- Quand le bot atteint MARCHAND_SEUIL_PODS (80%) il :
--   1. Parcourt ce trajet jusqu'au PNJ (carte par carte)
--   2. Sur l'étape `npc_marchand = true`, vend AUTO tous les items
--   3. Reprend mouvement() à l'étape matchant la map courante (7573
--      → premier match dans mouvement(), donc passe par toutes les
--      maps de retour jusqu'à 7804 et reprend le farm)
-- =====================================================================
function marchand()
    return {
        { map = "7804", path = "raw:GA001haw", from = 36, cell = 22 },
        { map = "7799", path = "raw:GA001gdWfdHgdefc1gbEfat", from = 443, cell = 19 },
        { map = "7763", path = "raw:GA001fgPgdbhcLgbohbagaJ", from = 440, cell = 35 },
        { map = "7762", path = "raw:GA001ge2hd-gaZfaK", from = 426, cell = 36 },
        { map = "7761", path = "raw:GA001hgDgaKfav", from = 427, cell = 21 },
        { map = "7417", path = "raw:GA001fgQgfthePgcyfbRgaXhav", from = 441, cell = 21 },
        { map = "7416", path = "raw:GA001gfehdIgax", from = 440, cell = 23 },
        { map = "7415", path = "raw:GA001gepfeagbsfaw", from = 445, cell = 22 },
        { map = "7414", path = "raw:GA001heH", from = 443, cell = 289 },
        { map = "7430", path = "raw:GA001hbqgaw", from = 276, cell = 22 },
        { map = "7429", path = "raw:GA001gfhfePgem", from = 443, cell = 268 },
        -- ARRIVÉE TAVERNE : npc_marchand=true → VendreToutAuPnjAsync(464)
        { map = "7573", npc = 464, npc_marchand = true, path = "raw:GA001dfG", from = 338, cell = 352 },
    }
end

-- banque() : NON UTILISÉ. La config banque (UI onglet Banque + le système
-- de PiloteBanque) gère le dépôt automatique à MAX_PODS%, en ouvrant le
-- coffre DIRECTEMENT sur la carte courante (ouvertureDirecte=true) ou en
-- allant à la map banque selon la config.
function banque() return {} end
function phenix() return {} end
