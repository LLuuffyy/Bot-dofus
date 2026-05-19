using System;

namespace BotDofus.Divers.Scripts.Api;

/// <summary>
/// Façade « WGRetro Script API v4.0 » — PAQUET A (le sous-ensemble déjà
/// supporté par le moteur existant, exposé sous les noms WGRetro).
///
/// But : qu'un script écrit façon WGRetro (objet global <c>bot</c>, propriétés
/// <c>bot.hp</c>, méthodes <c>bot.move()</c>, <c>bot.changeMap()</c>…) tourne
/// directement. Tout ici ne fait que DÉLÉGUER aux méthodes ApiLua/ApiBot/Anka
/// déjà éprouvées (zéro nouvelle logique réseau → zéro régression).
///
/// Les paquets B (zaap/banque/craft/HDV…) et C (IA de combat avancée) seront
/// ajoutés ensuite, par couches.
/// </summary>
public sealed partial class ApiLua
{
    // ---- Personnage : identité ------------------------------------------
    public string name => _etat.Personnage.Nom;
    public int level => _etat.Personnage.Niveau;
    public int classId => _etat.Personnage.IdClasse;
    public int breed => _etat.Personnage.IdClasse;
    public bool sex => _etat.Personnage.Sexe != 0;
    public string characterId => _etat.Personnage.Identifiant.ToString();

    // ---- PV / énergie ----------------------------------------------------
    public int hp => _etat.Personnage.Vie;
    public int hpMax => _etat.Personnage.VieMax;
    public int hpPercent => (int)Math.Round(_etat.Personnage.PourcentageVie);
    public int energy => _etat.Personnage.Energie;
    public int energyMax => _etat.Personnage.EnergieMax;

    // ---- Stats de combat -------------------------------------------------
    // NB : pa()/pm()/kamas() existent déjà comme MÉTHODES dans ApiLua →
    // pas de propriété homonyme (collision). Les scripts WGRetro qui font
    // bot.pa/bot.kamas devront utiliser bot.pa()/bot.kamas() pour l'instant
    // (sera unifié dans une passe ultérieure).
    public int statsPoints => _etat.Personnage.PointsCaracteristiques;

    // ---- Pods ------------------------------------------------------------
    public int pods => _etat.Personnage.PoidsActuel;
    public int podsMax => _etat.Personnage.PoidsMax;
    public double podRatio => _etat.Personnage.PoidsMax > 0
        ? (double)_etat.Personnage.PoidsActuel / _etat.Personnage.PoidsMax : 0.0;
    public int podsPercent => (int)Math.Round(_etat.Personnage.PourcentagePoids);
    public bool isPodsFull(double seuil = 0.9) => podRatio >= seuil;

    // ---- État ------------------------------------------------------------
    public bool inFight => est_en_combat();

    // ---- Position / map --------------------------------------------------
    public string mapId => (_etat.Personnage.CarteCourante ?? 0).ToString();
    public int cellId => _etat.Personnage.CellulePosition ?? -1;
    public int x => pos_x();
    public int y => pos_y();
    public string mapName => $"[{pos_x()},{pos_y()}]";

    // ---- Déplacement -----------------------------------------------------
    public bool move(int cell) => deplacer(cell);
    public bool moveToCell(int cell) => deplacer(cell);
    public int currentCell() => _etat.Personnage.CellulePosition ?? -1;
    public string currentMapId() => mapId;

    /// <summary>WGRetro : 'top','bottom','left','right' (mappe vers nos
    /// directions FR). Sortie GLOBALE robuste (multi-essais, évite mobs).</summary>
    public bool changeMap(string direction)
    {
        var d = (direction ?? "").Trim().ToLowerInvariant() switch
        {
            "top" or "haut" or "nord" => "nord",
            "bottom" or "bas" or "sud" => "sud",
            "left" or "gauche" or "ouest" => "ouest",
            "right" or "droite" or "est" => "est",
            _ => direction ?? ""
        };
        return changer_map(d);
    }
    public bool moveToward(string direction) => changeMap(direction);

    // ---- Récolte ---------------------------------------------------------
    public bool gather() => recolter_tout() > 0;
    public int gatherAll() => recolter_tout();
    public bool hasResourcesOnMap() => nb_recoltables() > 0;

    // ---- Combat (base) ---------------------------------------------------
    public bool fight() => engager_proche();
    public bool hasMonstersOnMap() => nb_monstres() > 0;
    public void waitForFightEnd() => attendre_fin_combat();

    // ---- PNJ / dialogue --------------------------------------------------
    public void talkTo(int npcId) => parler_pnj(npcId);
    public void talkToNpc(int npcId) => parler_pnj(npcId);
    public void closeDialog() => quitter_dialogue();
    public void leaveDialog() => quitter_dialogue();

    // ---- Utilitaires -----------------------------------------------------
    /// <summary>WGRetro : pause en millisecondes (bloquant).</summary>
    public void sleep(double ms) => attendre((int)ms);
    public void printMessage(string msg) => log(msg);
    public void printError(string msg) => avertir("[ERROR] " + msg);
    public void printSuccess(string msg) => log("[SUCCESS] " + msg);
    private readonly Random _rngWg = new();
    public int random(int min, int max) => _rngWg.Next(min, max + 1);
}
