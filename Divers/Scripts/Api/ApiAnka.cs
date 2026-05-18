using System;
using System.Linq;
using System.Threading;
using BotDofus.Divers.Cartes;
using BotDofus.Divers.Cartes.Entites;
using BotDofus.Divers.Combats.Enums;
using BotDofus.Divers.Donnees;
using BotDofus.Divers.Jeu;
using BotDofus.Utilitaires.Journaux;
using MoonSharp.Interpreter;

namespace BotDofus.Divers.Scripts.Api;

/// <summary>
/// Couche de compatibilité « AnkaBot » : expose des modules globaux Lua
/// (<c>character</c>, <c>map</c>, <c>inventory</c>, <c>npc</c>, <c>fight</c>,
/// <c>chat</c>, <c>exchange</c>, <c>mount</c>, <c>quest</c>) avec les NOMS de
/// méthodes de la doc https://doc.ankabot.dev afin que les scripts écrits dans
/// ce standard tournent ici. Les fonctions réellement supportées par notre
/// moteur (déplacement, carte, perso, inventaire lecture, récolte, combat
/// simple, dialogue PNJ, chat) sont câblées ; le reste est stubé (log + valeur
/// sûre) pour ne pas casser les scripts.
/// </summary>
public sealed class ApiAnka
{
    private readonly ApiBot _api;
    private readonly EtatJeu _etat;
    private readonly Func<CancellationToken> _ct;

    public ApiAnka(ApiBot api, EtatJeu etat, Func<CancellationToken> ct)
    {
        _api = api; _etat = etat; _ct = ct;
        Character = new ModuleCharacter(this);
        Map = new ModuleMap(this);
        Inventory = new ModuleInventory(this);
        Npc = new ModuleNpc(this);
        Fight = new ModuleFight(this);
        Chat = new ModuleChat(this);
        Exchange = new ModuleExchange(this);
        Mount = new ModuleMount(this);
        Quest = new ModuleQuest(this);
    }

    public ModuleCharacter Character { get; }
    public ModuleMap Map { get; }
    public ModuleInventory Inventory { get; }
    public ModuleNpc Npc { get; }
    public ModuleFight Fight { get; }
    public ModuleChat Chat { get; }
    public ModuleExchange Exchange { get; }
    public ModuleMount Mount { get; }
    public ModuleQuest Quest { get; }

    private CancellationToken Ct => _ct();
    private static void Stub(string m) => Journaliseur.Avertir($"[ANKA] {m} : non supporté (ignoré)");

    // =================================================================
    // character
    // =================================================================
    [MoonSharpUserData]
    public sealed class ModuleCharacter
    {
        private readonly ApiAnka _a;
        public ModuleCharacter(ApiAnka a) => _a = a;
        public string name() => _a._etat.Personnage.Nom;
        public int id() => _a._etat.Personnage.Identifiant;
        public int level() => _a._etat.Personnage.Niveau;
        public double kamas() => _a._etat.Personnage.Kamas;
        public int sex() => _a._etat.Personnage.Sexe;
        public int breed() => _a._etat.Personnage.IdClasse;
        public string breedName() => $"Classe {_a._etat.Personnage.IdClasse}";
        public int lifePoints() => _a._etat.Personnage.Vie;
        public int maxLifePoints() => _a._etat.Personnage.VieMax;
        public double lifePointsP() => _a._etat.Personnage.PourcentageVie;
        public int energyPoints() => _a._etat.Personnage.Energie;
        public int maxEnergyPoints() => _a._etat.Personnage.EnergieMax;
        public int statsPoint() => _a._etat.Personnage.PointsCaracteristiques;
        public bool isInFight() => _a._etat.Combat.Etat != EtatCombat.Inactif;
        public bool isBusy() => _a._etat.Dialogue.Ouvert
            || _a._etat.Combat.Etat != EtatCombat.Inactif;
        public bool isCaptchaPresent() => false;
        public bool isTeamLeader() => true;
        public bool freeMode() => true;
        public int server() => 0;
        public string serverName() => "Hystoria";
        public void giveUpFight() => Stub("character.giveUpFight");
        public void getBonusPack() => Stub("character.getBonusPack");
    }

    // =================================================================
    // map
    // =================================================================
    [MoonSharpUserData]
    public sealed class ModuleMap
    {
        private readonly ApiAnka _a;
        public ModuleMap(ApiAnka a) => _a = a;

        public int currentMapId() => _a._etat.Personnage.CarteCourante ?? 0;
        public int currentCell() => _a._etat.Personnage.CellulePosition ?? -1;
        public int currentArea() => 0;
        public int currentSubArea() => 0;

        public string currentMap()
        {
            var m = BaseDonnees.Instance.Map(_a._etat.Personnage.CarteCourante ?? 0);
            return m != null ? $"{m.X},{m.Y}" : "?,?";
        }
        public int getX(int mapId) => BaseDonnees.Instance.Map(mapId)?.X ?? 0;
        public int getY(int mapId) => BaseDonnees.Instance.Map(mapId)?.Y ?? 0;

        public bool onMap(int x, int y)
        {
            var m = BaseDonnees.Instance.Map(_a._etat.Personnage.CarteCourante ?? 0);
            return m != null && m.X == x && m.Y == y;
        }
        public bool onCell(int cell) => _a._etat.Personnage.CellulePosition == cell;

        public bool moveToCell(int cell)
            => _a._api.SeDeplacerVersCelluleAsync(cell, _a.Ct).GetAwaiter().GetResult();
        public bool door(int cell)
            => _a._api.SeDeplacerVersCelluleAsync(cell, _a.Ct).GetAwaiter().GetResult();
        public bool changeMap(string direction)
            => _a._api.ChangerMapDirectionAsync(direction, _a.Ct).GetAwaiter().GetResult();
        public bool moveToward(string direction)
            => _a._api.ChangerMapDirectionAsync(direction, _a.Ct).GetAwaiter().GetResult();

        public bool fight() => _a._api.EngagerCombatAsync(_a.Ct).GetAwaiter().GetResult();
        public bool forceFight() => fight();
        public int gather() => _a._api.RecolterToutAsync(_a.Ct).GetAwaiter().GetResult();

        public int countPlayers()
            => _a._etat.CarteCourante?.Entites.Values.OfType<EntiteJoueur>().Count() ?? 0;
        public bool npcInMap(int npcId)
            => _a._etat.CarteCourante?.Entites.Values.OfType<EntitePNJ>()
                   .Any(p => p.Identifiant == npcId || p.IdGabarit == npcId) ?? false;

        public Table monsterGroups()
        {
            var t = new Table(null); int i = 1;
            foreach (var m in _a._etat.CarteCourante?.Entites.Values.OfType<EntiteMonstre>()
                         ?? Enumerable.Empty<EntiteMonstre>())
                t[i++] = m.Identifiant;
            return t;
        }
        public Table getWalkableCells()
        {
            var t = new Table(null); int i = 1;
            foreach (var c in _a._etat.CarteCourante?.Cellules
                         .Where(c => c.EstMarchable) ?? Enumerable.Empty<Cellule>())
                t[i++] = c.Identifiant;
            return t;
        }
        public void saveZaap() => Stub("map.saveZaap");
        public void zaap(int mapId) => Stub("map.zaap");
        public void zaapi(int mapId) => Stub("map.zaapi");
    }

    // =================================================================
    // inventory
    // =================================================================
    [MoonSharpUserData]
    public sealed class ModuleInventory
    {
        private readonly ApiAnka _a;
        public ModuleInventory(ApiAnka a) => _a = a;
        public int pods() => _a._etat.Personnage.PoidsActuel;
        public int podsMax() => _a._etat.Personnage.PoidsMax;
        public double podsP() => _a._etat.Personnage.PourcentagePoids;
        public int itemCount(int gid)
            => _a._etat.Personnage.Inventaire.Where(o => o.IdTemplate == gid)
                   .Sum(o => o.Quantite);
        public string itemNameId(int gid) => BaseDonnees.Instance.Item(gid)?.Nom ?? $"#{gid}";
        public Table items()
        {
            var t = new Table(null); int i = 1;
            foreach (var o in _a._etat.Personnage.Inventaire) t[i++] = o.IdTemplate;
            return t;
        }
        public Table inventoryContent()
        {
            var t = new Table(null); int i = 1;
            foreach (var o in _a._etat.Personnage.Inventaire)
            {
                var e = new Table(null);
                e["gid"] = o.IdTemplate; e["uid"] = o.Identifiant;
                e["qte"] = o.Quantite; e["pos"] = o.Position;
                e["nom"] = BaseDonnees.Instance.Item(o.IdTemplate)?.Nom ?? $"#{o.IdTemplate}";
                t[i++] = e;
            }
            return t;
        }
        public void openBank() => Stub("inventory.openBank");
        public void useItem(int gid) => Stub("inventory.useItem");
        public void useMultipleItem(int gid, int n) => Stub("inventory.useMultipleItem");
        public void equipItem(int gid) => Stub("inventory.equipItem");
        public void deleteItem(int gid, int n) => Stub("inventory.deleteItem");
    }

    // =================================================================
    // npc
    // =================================================================
    [MoonSharpUserData]
    public sealed class ModuleNpc
    {
        private readonly ApiAnka _a;
        public ModuleNpc(ApiAnka a) => _a = a;
        public void npc(int npcId)
            => _a._api.ParlerPnjAsync(0, npcId, _a.Ct).GetAwaiter().GetResult();
        public bool npcInMap(int npcId)
            => _a._etat.CarteCourante?.Entites.Values.OfType<EntitePNJ>()
                   .Any(p => p.Identifiant == npcId || p.IdGabarit == npcId) ?? false;
        public bool hasReply() => _a._etat.Dialogue.Reponses.Count > 0;
        public Table getRepliesId()
        {
            var t = new Table(null); int i = 1;
            foreach (var r in _a._etat.Dialogue.Reponses) t[i++] = r;
            return t;
        }
        public void reply(int replyId)
            => _a._api.RepondreDialogueAsync(
                   _a._etat.Dialogue.QuestionId, replyId, _a.Ct).GetAwaiter().GetResult();
        public void leave()
            => _a._api.QuitterDialogueAsync(_a.Ct).GetAwaiter().GetResult();
        public void npcBank() => Stub("npc.npcBank");
        public void npcSale() => Stub("npc.npcSale");
        public void npcBuy() => Stub("npc.npcBuy");
    }

    // =================================================================
    // fight (surface minimale — l'IA combat tourne en interne via config)
    // =================================================================
    [MoonSharpUserData]
    public sealed class ModuleFight
    {
        private readonly ApiAnka _a;
        public ModuleFight(ApiAnka a) => _a = a;
        public bool isInFight() => _a._etat.Combat.Etat != EtatCombat.Inactif;
        public int turn() => 0;
        public void endTurn()
            => _a._api.FinirTourAsync(_a.Ct).GetAwaiter().GetResult();
        public void launch() => _a._api.EngagerCombatAsync(_a.Ct).GetAwaiter().GetResult();
        public void giveUp() => Stub("fight.giveUp");
    }

    // =================================================================
    // chat
    // =================================================================
    [MoonSharpUserData]
    public sealed class ModuleChat
    {
        private readonly ApiAnka _a;
        public ModuleChat(ApiAnka a) => _a = a;
        public void send(string message)
            => _a._api.EnvoyerMessageAsync("*", message, _a.Ct).GetAwaiter().GetResult();
        public void sendMessage(string canal, string message)
            => _a._api.EnvoyerMessageAsync(canal, message, _a.Ct).GetAwaiter().GetResult();
    }

    // =================================================================
    // Stubs : exchange / mount / quest (à implémenter quand le protocole
    // banque/HDV/monture/quête sera capturé — log clair, pas de crash)
    // =================================================================
    [MoonSharpUserData]
    public sealed class ModuleExchange
    {
        public ModuleExchange(ApiAnka _) { }
        public void putItem(int gid, int n) => Stub("exchange.putItem");
        public void getItem(int gid, int n) => Stub("exchange.getItem");
        public void putAllItems() => Stub("exchange.putAllItems");
        public void getAllItems() => Stub("exchange.getAllItems");
        public void putKamas(int n) => Stub("exchange.putKamas");
        public void getKamas(int n) => Stub("exchange.getKamas");
        public int storageKamas() => 0;
        public bool isInExchange() => false;
        public void ready() => Stub("exchange.ready");
        public void leave() => Stub("exchange.leave");
    }
    [MoonSharpUserData]
    public sealed class ModuleMount
    {
        public ModuleMount(ApiAnka _) { }
        public bool isRiding() => false;
        public void toggleRiding() => Stub("mount.toggleRiding");
    }
    [MoonSharpUserData]
    public sealed class ModuleQuest
    {
        public ModuleQuest(ApiAnka _) { }
        public bool isQuestActive(int id) => false;
        public bool isStepActive(int id) => false;
    }
}
