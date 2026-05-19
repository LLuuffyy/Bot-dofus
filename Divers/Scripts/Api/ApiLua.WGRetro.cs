using System;
using MoonSharp.Interpreter;

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
    // bot.pa / bot.pm / bot.kamas : PROPRIÉTÉS définies dans ApiLua.cs
    // (uniformisé WGRetro, plus de parenthèses).
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

    // ---- PNJ / dialogue (suite) -----------------------------------------
    public bool isInDialog => _etat.Dialogue.Ouvert;
    public string getQuestionId() => Convert.ToString(_etat.Dialogue.QuestionId) ?? "";
    /// <summary>IDs des réponses possibles (Table Lua 1-based).</summary>
    public Table getResponses() => Anka.Npc.getRepliesId();
    /// <summary>Répondre par ID de réponse (WGRetro bot.reply).</summary>
    public void reply(int responseId) => Anka.Npc.reply(responseId);
    /// <summary>Répondre par INDEX 1-based dans la liste des réponses.</summary>
    public void npcReply(int index)
    {
        int i = 1;
        foreach (var r in _etat.Dialogue.Reponses)
        {
            if (i++ == index) { Anka.Npc.reply(Convert.ToInt32(r)); return; }
        }
        avertir($"[bot] npcReply({index}) : pas de réponse à cet index");
    }
    public void respond(int index) => npcReply(index);
    /// <summary>Attend (≈timeout ms, def 5000) qu'une question PNJ arrive.</summary>
    public void waitForDialog(double timeout = 5000)
    {
        int reste = (int)timeout;
        while (reste > 0 && !_ct.IsCancellationRequested
               && !(_etat.Dialogue.Ouvert && _etat.Dialogue.Reponses.Count > 0))
        { attendre(120); reste -= 120; }
    }

    // ---- Mémoire de session (bot.memory.set/get/has/delete) -------------
    public ApiAnka.ModuleMemory memory => Anka.Memory;
    public void memorySet(string k, object v) => Anka.Memory.set(k, v);
    public object? memoryGet(string k) => Anka.Memory.get(k);
    public bool memoryHas(string k) => Anka.Memory.has(k);
    public void memoryDelete(string k) => Anka.Memory.delete(k);
    public void remember(string k, object v) => Anka.Memory.set(k, v);

    // ---- Stats & sorts ---------------------------------------------------
    private static int StatNameToId(string name) => (name ?? "").Trim().ToLowerInvariant() switch
    {
        "strength" or "force" => 10,
        "vitality" or "vitalite" or "vitalité" => 11,
        "wisdom" or "sagesse" => 12,
        "chance" => 13,
        "agility" or "agilite" or "agilité" => 14,
        "intelligence" or "intel" => 15,
        _ => -1
    };
    /// <summary>Boost une stat de +1 (10=force,11=vita,12=sagesse,13=chance,
    /// 14=agi,15=intel).</summary>
    public void boostStat(int statId)
        => _api.MonterCaracteristiqueAsync(statId, 1, _ct).GetAwaiter().GetResult();
    /// <summary>Verse tout le capital dispo dans une stat de base (best
    /// effort ; la cible est indicative).</summary>
    public bool statUpgrade(string name, int targetBase = 0)
    {
        int id = StatNameToId(name);
        if (id < 0) { avertir($"[bot] statUpgrade : stat inconnue '{name}'"); return false; }
        _api.AutoDistribuerCaracteristiquesAsync(id, _ct).GetAwaiter().GetResult();
        return true;
    }
    public void UpgradeStrength(int pts = 1) { for (int i = 0; i < Math.Max(1, pts); i++) boostStat(10); }
    public void UpgradeVitality(int pts = 1) { for (int i = 0; i < Math.Max(1, pts); i++) boostStat(11); }
    public void UpgradeWisdom(int pts = 1) { for (int i = 0; i < Math.Max(1, pts); i++) boostStat(12); }
    public void UpgradeChance(int pts = 1) { for (int i = 0; i < Math.Max(1, pts); i++) boostStat(13); }
    public void UpgradeAgility(int pts = 1) { for (int i = 0; i < Math.Max(1, pts); i++) boostStat(14); }
    public void UpgradeIntelligence(int pts = 1) { for (int i = 0; i < Math.Max(1, pts); i++) boostStat(15); }
    public void boostSpell(int spellId)
        => _api.MonterSortAsync(spellId, _ct).GetAwaiter().GetResult();
    /// <summary>Monte un sort vers un niveau cible (best effort, +1 par
    /// itération).</summary>
    public bool spellUpgrade(int spellId, int targetLevel)
    {
        for (int i = 0; i < Math.Max(1, targetLevel) && !_ct.IsCancellationRequested; i++)
            _api.MonterSortAsync(spellId, _ct).GetAwaiter().GetResult();
        return true;
    }
    public void upgradeSpell(int spellId) => boostSpell(spellId);

    // ---- Chat (par canal) ------------------------------------------------
    private void Canal(string c, string m)
        => Anka.Chat.sendMessage(c, m);
    public void say(string channel, string message) => Canal(channel, message);
    public void general(string m) => Canal("*", m);
    public void team(string m) => Canal("#", m);
    public void guild(string m) => Canal("%", m);
    public void trade(string m) => Canal(":", m);
    public void recruit(string m) => Canal("?", m);
    public void partyChat(string m) => Canal("^", m);
}
