using System;
using System.Collections.Generic;
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
        Job = new ModuleJob(this);
        Console = new ModuleConsole(this);
        Global = new ModuleGlobal(this);
        Memory = new ModuleMemory(this);
        Script = new ModuleScript(this);
        Storage = new ModuleStorage(this);
    }

    public ModuleJob Job { get; }
    public ModuleConsole Console { get; }
    public ModuleGlobal Global { get; }
    public ModuleMemory Memory { get; }
    public ModuleScript Script { get; }
    public ModuleStorage Storage { get; }

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

    // Compteurs façon Frigost (fightCount/gatherCount/wasInFight).
    private int _nbCombats;
    private int _nbRecoltes;
    private bool _dernierCombatFini;
    public readonly Dictionary<string, object> MemoireScript = new();

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
        public string serverName() => BotDofus.Commun.Reseau.ConfigReseau.ChargerOuDefaut().NomServeur;
        public void giveUpFight() => Stub("character.giveUpFight");
        public void getBonusPack() => Stub("character.getBonusPack");

        // --- Alias Frigost ---
        public bool wasInFight() => _a._dernierCombatFini;
        public int fightCount() => _a._nbCombats;
        public void resetFightCount() => _a._nbCombats = 0;
        public int gatherCount() => _a._nbRecoltes;
        public void resetGatherCount() => _a._nbRecoltes = 0;
        public bool playerInMap(string nom)
            => _a._etat.CarteCourante?.Entites.Values.OfType<EntiteJoueur>()
                   .Any(j => string.Equals(j.Nom, nom, StringComparison.OrdinalIgnoreCase)) ?? false;
        public void launchExchange(string nom) => Stub("character.launchExchange");
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
        /// <summary>Sortie « intelligente » : si la cellule enregistrée est une
        /// transition → sortie GLOBALE par direction (robuste depuis partout) ;
        /// sinon déplacement simple. Rend les trajets cell=… fiables.</summary>
        public bool sortie(int cell)
            => _a._api.SortirCarteAsync(cell, _a.Ct).GetAwaiter().GetResult();
        /// <summary>Rejoue VERBATIM un GA001 capturé à la main (Road Creator).</summary>
        public void replayPath(string ga001)
            => _a._api.RejouerCheminBrutAsync(ga001, _a.Ct).GetAwaiter().GetResult();
        public bool changeMap(string direction)
            => _a._api.ChangerMapDirectionAsync(direction, _a.Ct).GetAwaiter().GetResult();
        public bool moveToward(string direction)
            => _a._api.ChangerMapDirectionAsync(direction, _a.Ct).GetAwaiter().GetResult();

        public bool fight()
        {
            _a._dernierCombatFini = false;
            var r = _a._api.EngagerCombatAsync(_a.Ct).GetAwaiter().GetResult();
            if (r) { _a._nbCombats++; _a._dernierCombatFini = true; }
            return r;
        }
        public bool forceFight() => fight();
        public int gather()
        {
            int n = _a._api.RecolterToutAsync(_a.Ct).GetAwaiter().GetResult();
            _a._nbRecoltes += n;
            return n;
        }

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

        // --- Alias Frigost (doc.frigost.dev) ---
        public int currentCellId() => currentCell();
        public int x() => BaseDonnees.Instance.Map(_a._etat.Personnage.CarteCourante ?? 0)?.X ?? 0;
        public int y() => BaseDonnees.Instance.Map(_a._etat.Personnage.CarteCourante ?? 0)?.Y ?? 0;
        public bool move(int cell) => moveToCell(cell);
        /// <summary>Frigost map.change : direction ("top"/"left"/…) ou id de cellule.</summary>
        public bool change(object cible)
        {
            var s = cible?.ToString() ?? "";
            if (int.TryParse(s, out var cell)) return moveToCell(cell);
            var d = s switch
            {
                "top" => "nord", "bottom" => "sud", "right" => "est",
                "left" => "ouest", _ => s
            };
            return changeMap(d);
        }
        public bool waitChange()
        {
            int avant = currentMapId();
            for (int i = 0; i < 40; i++)
            {
                if (currentMapId() != avant) return true;
                System.Threading.Thread.Sleep(250);
            }
            return false;
        }
        public void interactive(int cell) => move(cell);
        public void teleport(int mapId) => Stub("map.teleport");
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

        // --- Alias Frigost ---
        public double podsPercent() => podsP();
        public double kamas() => _a._etat.Personnage.Kamas;
        public Table content() => inventoryContent();
        public int objectQuantity(int gid) => itemCount(gid);
        public string objectName(int gid) => itemNameId(gid);
        public int objectPosition(int gid)
            => _a._etat.Personnage.Inventaire.FirstOrDefault(o => o.IdTemplate == gid)?.Position ?? -1;
        public long objectUid(int gid)
            => _a._etat.Personnage.Inventaire.FirstOrDefault(o => o.IdTemplate == gid)?.Identifiant ?? -1;
        public void useObject(int gid) => Stub("inventory.useObject");
        public void deleteObject(int gid, int n) => Stub("inventory.deleteObject");
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

        // --- Alias Frigost ---
        public bool exists(int npcId) => npcInMap(npcId);
        public void talk(int npcId) => npc(npcId);
        public void interact(int npcId) => npc(npcId);
        public bool inDialog() => _a._etat.Dialogue.Ouvert;
        public bool indialog() => _a._etat.Dialogue.Ouvert;
        public void leaveDialog() => leave();
        public void leavedialog() => leave();
        public Table possibleReplies() => getRepliesId();
        public Table possiblereplies() => getRepliesId();
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
        public int turn() => _a._etat.Combat.NumeroTour;
        public void endTurn()
            => _a._api.FinirTourAsync(_a.Ct).GetAwaiter().GetResult();
        public void launch() => _a._api.EngagerCombatAsync(_a.Ct).GetAwaiter().GetResult();
        public void giveUp() => Stub("fight.giveUp");

        // ========================================================================
        // === API COMBAT SCRIPTÉ (Option A — mode impératif) ====================
        // ========================================================================
        // Permet à l'utilisateur d'écrire une fonction combat() Lua qui appelle
        // directement cast(sortId, cible), move(cell), pass(), etc. Le moteur
        // IA classique (MoteurReglesCombat) est bypassé si combat() existe.

        /// <summary>Ma cellule courante en combat (0 si pas en combat).</summary>
        public int myCell() => _a._etat.Personnage.CellulePosition ?? 0;

        /// <summary>PA restants ce tour (0 si pas en combat).</summary>
        public int pa()
        {
            var moi = _a._etat.Combat.Allies.FirstOrDefault(c => c.Identifiant == _a._etat.Personnage.Identifiant);
            return moi?.PA ?? 0;
        }

        /// <summary>PM restants ce tour.</summary>
        public int pm()
        {
            var moi = _a._etat.Combat.Allies.FirstOrDefault(c => c.Identifiant == _a._etat.Personnage.Identifiant);
            return moi?.PM ?? 0;
        }

        /// <summary>PV / PVMax du perso en combat.</summary>
        public int pv()
        {
            var moi = _a._etat.Combat.Allies.FirstOrDefault(c => c.Identifiant == _a._etat.Personnage.Identifiant);
            return moi?.PV ?? _a._etat.Personnage.Vie;
        }

        public int pvMax()
        {
            var moi = _a._etat.Combat.Allies.FirstOrDefault(c => c.Identifiant == _a._etat.Personnage.Identifiant);
            return moi?.PVMax ?? _a._etat.Personnage.VieMax;
        }

        /// <summary>Cellule de l'ennemi vivant le plus proche, ou -1 si aucun.</summary>
        public int enemyClosest()
        {
            int moi = _a._etat.Personnage.CellulePosition ?? 0;
            var carte = _a._etat.CarteCourante;
            if (carte == null) return -1;
            var ennemi = _a._etat.Combat.Ennemis
                .Where(e => !e.EstMort && e.PV > 0 && e.CellulePosition > 0)
                .OrderBy(e => Math.Abs(e.CellulePosition - moi))
                .FirstOrDefault();
            return ennemi?.CellulePosition ?? -1;
        }

        /// <summary>Cellule de l'ennemi vivant avec le moins de PV.</summary>
        public int enemyWeakest()
        {
            var ennemi = _a._etat.Combat.Ennemis
                .Where(e => !e.EstMort && e.PV > 0 && e.CellulePosition > 0)
                .OrderBy(e => e.PV)
                .FirstOrDefault();
            return ennemi?.CellulePosition ?? -1;
        }

        /// <summary>Cellule de l'ennemi vivant avec le plus de PV.</summary>
        public int enemyStrongest()
        {
            var ennemi = _a._etat.Combat.Ennemis
                .Where(e => !e.EstMort && e.PV > 0 && e.CellulePosition > 0)
                .OrderByDescending(e => e.PV)
                .FirstOrDefault();
            return ennemi?.CellulePosition ?? -1;
        }

        /// <summary>Nombre d'ennemis vivants.</summary>
        public int enemiesAlive()
            => _a._etat.Combat.Ennemis.Count(e => !e.EstMort && e.PV > 0);

        /// <summary>Une cellule adjacente vide à ma position (pour invocations). -1 si aucune.</summary>
        public int freeCellNearMe()
        {
            var carte = _a._etat.CarteCourante;
            if (carte == null) return -1;
            int moiCell = _a._etat.Personnage.CellulePosition ?? 0;
            var moi = carte.Obtenir(moiCell);
            if (moi == null) return -1;
            var occupees = new System.Collections.Generic.HashSet<int>();
            foreach (var c in _a._etat.Combat.Allies) if (!c.EstMort) occupees.Add(c.CellulePosition);
            foreach (var c in _a._etat.Combat.Ennemis) if (!c.EstMort) occupees.Add(c.CellulePosition);
            // Voisinage iso 14×40 : ±1 sur l'axe linéaire + ±14 + ±15 (les 4 dirs diag).
            int[] offsets = { -1, +1, -14, +14, -15, +15, -13, +13 };
            foreach (var d in offsets)
            {
                int candId = moiCell + d;
                if (candId < 0 || candId >= 560) continue;
                var cand = carte.Obtenir(candId);
                if (cand == null || !cand.EstMarchable) continue;
                if (occupees.Contains(candId)) continue;
                if (cand.IdInteractif >= 0) continue;
                return candId;
            }
            return -1;
        }

        /// <summary>
        /// Lance un sort sur une cellule cible.
        /// Cible : int (cellule directe) ou string ("plus_proche", "plus_faible",
        /// "plus_fort", "moi", "case_libre_proche").
        /// Retourne true si le paquet GA300 a été envoyé (= pas de validation
        /// de réussite côté serveur).
        /// </summary>
        public bool cast(int sortId, DynValue cible)
        {
            int cell = ResoudreCible(cible);
            if (cell < 0)
            {
                Journaliseur.Avertir($"[COMBAT-LUA] cast({sortId}, {cible}) : cible introuvable.");
                return false;
            }
            try
            {
                _a._api.EnvoyerPaquetBrutAsync($"GA300{sortId};{cell}", _a.Ct).GetAwaiter().GetResult();
                System.Threading.Thread.Sleep(300);
                _a._api.EnvoyerPaquetBrutAsync("GKK0", _a.Ct).GetAwaiter().GetResult();
                System.Threading.Thread.Sleep(400);
                Journaliseur.Info($"[COMBAT-LUA] cast({sortId}, cell={cell}) → GA300 envoyé.");
                return true;
            }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[COMBAT-LUA] cast({sortId}, {cell}) échec : {ex.Message}");
                return false;
            }
        }

        /// <summary>Passe le tour (Gt).</summary>
        public void pass()
        {
            try
            {
                _a._api.FinirTourAsync(_a.Ct).GetAwaiter().GetResult();
                Journaliseur.Info("[COMBAT-LUA] pass() → Gt envoyé.");
            }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[COMBAT-LUA] pass() échec : {ex.Message}");
            }
        }

        /// <summary>Placement initial (Gp + GR1). À appeler en phase placement uniquement.</summary>
        public void placement(int cell)
        {
            try
            {
                _a._api.SePlacerEnCombatAsync(cell, _a.Ct).GetAwaiter().GetResult();
                Journaliseur.Info($"[COMBAT-LUA] placement(cell={cell}) → Gp+GR1 envoyés.");
            }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[COMBAT-LUA] placement({cell}) échec : {ex.Message}");
            }
        }

        /// <summary>
        /// Résolveur de cible : int direct OU string ("plus_proche", "plus_faible",
        /// "plus_fort", "moi", "case_libre_proche"). Retourne -1 si non résolvable.
        /// </summary>
        private int ResoudreCible(DynValue v)
        {
            if (v.Type == DataType.Number) return (int)v.Number;
            if (v.Type != DataType.String) return -1;
            return v.String?.ToLowerInvariant() switch
            {
                "plus_proche" or "closest" => enemyClosest(),
                "plus_faible" or "weakest" => enemyWeakest(),
                "plus_fort" or "strongest" => enemyStrongest(),
                "moi" or "me" or "self" => myCell(),
                "case_libre_proche" or "free_cell_near" or "free" => freeCellNearMe(),
                _ => -1
            };
        }
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
        public bool questActive(int id) => false;
        public int questCurrentStep(int id) => 0;
    }

    // =================================================================
    // job  (métiers : nom + niveau, depuis JSK/JXK déjà parsés)
    // =================================================================
    [MoonSharpUserData]
    public sealed class ModuleJob
    {
        private readonly ApiAnka _a;
        public ModuleJob(ApiAnka a) => _a = a;

        /// <summary>Niveau du métier (jobId Dofus). 0 si inconnu.</summary>
        public int level(int jobId)
            => _a._etat.Personnage.MetiersNiveaux.TryGetValue(jobId, out var n) ? n : 0;

        /// <summary>Nom déduit du métier via le 1er skill connu (Bois/Céréale/…).</summary>
        public string name(int jobId)
        {
            if (_a._etat.Personnage.MetiersSkills.TryGetValue(jobId, out var sk)
                && sk.Count > 0)
                return BaseDonnees.FamilleRessource(BaseDonnees.Instance.Skill(sk[0]));
            return $"Métier {jobId}";
        }
    }

    // =================================================================
    // console (Frigost) : sortie texte
    // =================================================================
    [MoonSharpUserData]
    public sealed class ModuleConsole
    {
        public ModuleConsole(ApiAnka _) { }
        public void print(object m) => Journaliseur.Info($"[LUA] {m}");
        public void error(object m) => Journaliseur.Avertir($"[LUA] {m}");
        public void success(object m) => Journaliseur.Info($"[LUA] ✔ {m}");
        public void clear() { }
        public int lines() => 0;
    }

    // =================================================================
    // global (Frigost) : utilitaires
    // =================================================================
    [MoonSharpUserData]
    public sealed class ModuleGlobal
    {
        private readonly ApiAnka _a;
        private static readonly Random _rng = new();
        public ModuleGlobal(ApiAnka a) => _a = a;
        public void sleep(int ms) => Pause(ms, _a.Ct);
        public void delay(int ms) => Pause(ms, _a.Ct);
        public int random(int min, int max) => _rng.Next(min, max + 1);
        public bool isInTeam() => false;
        public bool isTeamLeader() => true;
        public int teamCount() => 1;
        public int teamNumber() => 1;
        public string username() => _a._etat.Personnage.Nom;
        public long timestamp() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public void leaveDialog()
            => _a._api.QuitterDialogueAsync(_a.Ct).GetAwaiter().GetResult();
        public void disconnect() => Stub("global.disconnect");

        private static void Pause(int ms, CancellationToken ct)
        { try { System.Threading.Tasks.Task.Delay(ms, ct).GetAwaiter().GetResult(); }
          catch (OperationCanceledException) { } }
    }

    // =================================================================
    // memory (Frigost) : variables persistantes en RAM
    // =================================================================
    [MoonSharpUserData]
    public sealed class ModuleMemory
    {
        private readonly ApiAnka _a;
        public ModuleMemory(ApiAnka a) => _a = a;
        public void set(string k, object v) => _a.MemoireScript[k] = v;
        public object? get(string k) => _a.MemoireScript.TryGetValue(k, out var v) ? v : null;
        public void erase(string k) => _a.MemoireScript.Remove(k);
        // Compat WGRetro : bot.memory.has / bot.memory.delete
        public bool has(string k) => _a.MemoireScript.ContainsKey(k);
        public void delete(string k) => _a.MemoireScript.Remove(k);
    }

    // =================================================================
    // script (Frigost) : contrôle du script
    // =================================================================
    [MoonSharpUserData]
    public sealed class ModuleScript
    {
        public ModuleScript(ApiAnka _) { }
        public string name() => "script";
        public string folder() => System.IO.Directory.Exists("scripts")
            ? System.IO.Path.GetFullPath("scripts") : Environment.CurrentDirectory;
        public void restart() => Stub("script.restart");
        public void load(string f) => Stub("script.load");
        public void stop() => Stub("script.stop");
    }

    // =================================================================
    // storage (Frigost) : banque/coffre — protocole pas encore branché
    // =================================================================
    [MoonSharpUserData]
    public sealed class ModuleStorage
    {
        public ModuleStorage(ApiAnka _) { }
        public void putObject(int gid, int n) => Stub("storage.putObject");
        public void getObject(int gid, int n) => Stub("storage.getObject");
        public void putAllObjects() => Stub("storage.putAllObjects");
        public void getAllObjects() => Stub("storage.getAllObjects");
        public void putKamas(int n) => Stub("storage.putKamas");
        public void getKamas(int n) => Stub("storage.getKamas");
        public int kamas() => 0;
        public Table content() => new(null);
        public void leave() => Stub("storage.leave");
    }
}
