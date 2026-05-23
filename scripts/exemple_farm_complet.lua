-- =====================================================================
-- EXEMPLE — Farm avec banque + marchand auto + force-fight
-- =====================================================================
-- Ce script illustre les NOUVELLES sections supportées par Luffy-bot :
--   * mouvement()  : trajet de farm en boucle (carte de combats)
--   * banque()     : trajet vers le banquier (déclenché par seuil banque
--                    dans la config banque/<perso>.json — système séparé)
--   * marchand()   : trajet vers le PNJ marchand (vente items équipements)
--                    déclenché par MARCHAND_SEUIL_PODS ci-dessous
--
-- Globals reconnus :
--   * MAX_PODS              : poids max avant arrêt (legacy, info)
--   * MARCHAND_SEUIL_PODS   : % poids qui déclenche le détour marchand
--   * FORCE_FIGHT           : true = re-engage tous les groupes de la map
--                             avant de passer à l'étape suivante
--   * SHOW_FIGHT_COUNTER    : affiche le compteur de combats
-- =====================================================================

SHOW_FIGHT_COUNTER = true
MAX_PODS = 95
MARCHAND_SEUIL_PODS = 80   -- au-dessus de 80% pods → détour taverne
FORCE_FIGHT = true         -- farme TOUS les groupes de chaque map

-- Trajet farm principal — boucle de combats.
function mouvement()
    return {
        { map = "7804", path = "raw:GA001haw", fight = true, from = 36, cell = 22 },
        -- Ajoute ici tes autres maps de farm...
    }
end

-- Trajet jusqu'au banquier (utilisé par le système banque existant).
function banque()
    return {
        -- Exemple : aller à Astrub banque
        -- { map = "10303", path = "...", from = X, cell = Y },
    }
end

-- Trajet jusqu'au PNJ marchand (taverne Astrub par défaut, PNJ 464).
-- Le bot déclenche ce trajet dès que pods >= MARCHAND_SEUIL_PODS.
-- L'étape avec npc_marchand=true déclenche AUTOMATIQUEMENT la vente
-- de tous les items équipements/inconnus configurés.
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
        -- ARRIVÉE à la taverne : npc_marchand=true → vente déclenchée
        { map = "7573", npc = 464, npc_marchand = true, path = "raw:GA001dfG", from = 338, cell = 352 },
        -- Trajet retour vers le farm (mêmes maps en sens inverse)
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

function phenix()
    return {}
end
