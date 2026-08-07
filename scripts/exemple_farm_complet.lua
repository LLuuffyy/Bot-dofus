-- =====================================================================
-- EXEMPLE — Farm minimaliste avec marchand auto
-- =====================================================================

SHOW_FIGHT_COUNTER = true
MARCHAND_SEUIL_PODS = 80  -- détour marchand à 80% pods

-- =====================================================================
-- mouvement() — STRUCTURE MINIMALE
--
-- Étape [0] : juste `forcefight = true` sur la map de farm → le bot
--             reste sur la map et engage tous les groupes en boucle.
--             PAS BESOIN de path ni cell.
--
-- Étapes [1..N] : maps de RETOUR depuis la taverne, dans l'ordre
--                 (utilisées uniquement après détour marchand).
--                 La PREMIÈRE étape retour = map du PNJ marchand (7573).
-- =====================================================================
function mouvement()
    return {
        -- Zone farm — rien d'autre que forcefight=true
        { map = "7804", forcefight = true },

        -- Maps de retour depuis taverne (chemin 7573 → 7804)
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
    }
end

-- marchand() — aller vers le PNJ
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
        -- ARRIVÉE TAVERNE : npc_marchand=true → vente AUTO
        { map = "7573", npc = 464, npc_marchand = true, path = "raw:GA001dfG", from = 338, cell = 352 },
    }
end

function banque() return {} end
function phenix() return {} end
