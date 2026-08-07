using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BotDofus.Divers.Scripts.Api;
using BotDofus.Utilitaires.Journaux;
using MoonSharp.Interpreter;

namespace BotDofus.Divers.Scripts;

/// <summary>
/// Moteur d'exécution de scripts Lua "interactifs" : le script peut faire ses
/// propres boucles, appeler les méthodes <see cref="ApiLua"/> pour piloter le bot,
/// et tourner indéfiniment jusqu'à ce qu'on l'arrête.
///
/// Différent du <see cref="ChargeurLua"/> historique qui est statique
/// (le script déclare des étapes, le moteur les exécute).
///
/// Usage typique côté UI : Charger(chemin) → Demarrer() → Arreter().
/// </summary>
public sealed class MoteurLuaInteractif : IDisposable
{
    private readonly ApiLua _api;
    private Script? _script;
    private CancellationTokenSource? _annulation;
    private Task? _tache;
    private string? _chemin;

    public bool EnExecution { get; private set; }
    public string? CheminScript => _chemin;

    public event EventHandler? ExecutionDemarree;
    public event EventHandler<string?>? ExecutionTerminee;
    public event EventHandler<Exception>? ExecutionErreur;

    public MoteurLuaInteractif(ApiLua api)
    {
        _api = api;
        UserData.RegisterAssembly(typeof(ApiLua).Assembly);
    }

    /// <summary>Charge le contenu d'un fichier .lua sans l'exécuter.</summary>
    public void Charger(string cheminFichier)
    {
        if (!File.Exists(cheminFichier))
            throw new FileNotFoundException($"Script Lua introuvable : {cheminFichier}");

        _script = new Script(CoreModules.Preset_SoftSandbox);
        _idxRoute = -1; // nouveau script → resynchro au 1er tour
        _script.Globals["bot"] = _api;
        // Modules compatibles AnkaBot (https://doc.ankabot.dev) : les scripts
        // écrits dans ce standard utilisent character.*, map.*, inventory.*, …
        _script.Globals["character"] = _api.Anka.Character;
        _script.Globals["map"] = _api.Anka.Map;
        _script.Globals["inventory"] = _api.Anka.Inventory;
        _script.Globals["npc"] = _api.Anka.Npc;
        _script.Globals["fight"] = _api.Anka.Fight;
        _script.Globals["chat"] = _api.Anka.Chat;
        _script.Globals["exchange"] = _api.Anka.Exchange;
        _script.Globals["mount"] = _api.Anka.Mount;
        _script.Globals["quest"] = _api.Anka.Quest;
        _script.Globals["job"] = _api.Anka.Job;
        // Modules supplémentaires Frigost (doc.frigost.dev)
        _script.Globals["console"] = _api.Anka.Console;
        _script.Globals["global"] = _api.Anka.Global;
        _script.Globals["memory"] = _api.Anka.Memory;
        _script.Globals["script"] = _api.Anka.Script;
        _script.Globals["storage"] = _api.Anka.Storage;
        // Fonctions globales AnkaBot
        _script.Globals["delay"] = (System.Action<double>)(ms =>
        {
            try { Task.Delay((int)ms, _annulation?.Token ?? CancellationToken.None)
                      .GetAwaiter().GetResult(); }
            catch (OperationCanceledException) { }
        });
        System.Action<object> imprime = o => Journaliseur.Info($"[LUA] {o}");
        _script.Globals["print"] = imprime;
        _script.Globals["printText"] = imprime;
        _script.Globals["printError"] =
            (System.Action<object>)(o => Journaliseur.Avertir($"[LUA] {o}"));
        _chemin = cheminFichier;
        Journaliseur.Info($"[LUA] Script chargé : {cheminFichier}");
    }

    /// <summary>Lance l'exécution du script en arrière-plan. Non-bloquant.</summary>
    public void Demarrer()
    {
        if (EnExecution)
        {
            Journaliseur.Avertir("[LUA] Un script est déjà en exécution");
            return;
        }
        if (_script == null || _chemin == null)
        {
            Journaliseur.Avertir("[LUA] Aucun script chargé");
            return;
        }

        var code = File.ReadAllText(_chemin);
        _annulation = new CancellationTokenSource();
        _api.DefinirAnnulation(_annulation.Token); // trajets : actif()/attendre() arrêtables
        EnExecution = true;
        ExecutionDemarree?.Invoke(this, EventArgs.Empty);
        Journaliseur.Info($"[LUA] Démarrage script : {Path.GetFileName(_chemin)}");

        _tache = Task.Run(() =>
        {
            try
            {
                _script.DoString(code);
                // Script au format AnkaBot (définit move()/bank()/phenix()) :
                // on pilote la route. Sinon script libre déjà exécuté ci-dessus.
                // move() (AnkaBot) ou mouvement() (SynFus) → moteur de route.
                if (_script.Globals.Get("move").Type == DataType.Function
                    || _script.Globals.Get("mouvement").Type == DataType.Function)
                {
                    Journaliseur.Info("[ANKA] Script de route détecté → moteur de trajet");
                    ExecuterRoute();
                }
                Journaliseur.Info($"[LUA] Script terminé normalement : {Path.GetFileName(_chemin)}");
                ExecutionTerminee?.Invoke(this, null);
            }
            catch (ScriptRuntimeException ex)
            {
                Journaliseur.Erreur($"[LUA] Erreur runtime : {ex.DecoratedMessage}", ex);
                ExecutionErreur?.Invoke(this, ex);
            }
            catch (SyntaxErrorException ex)
            {
                Journaliseur.Erreur($"[LUA] Erreur syntaxe : {ex.DecoratedMessage}", ex);
                ExecutionErreur?.Invoke(this, ex);
            }
            catch (Exception ex)
            {
                Journaliseur.Erreur("[LUA] Erreur générique", ex);
                ExecutionErreur?.Invoke(this, ex);
            }
            finally
            {
                EnExecution = false;
            }
        });
    }

    // =================================================================
    // Moteur de ROUTE façon AnkaBot : exécute move()/bank()/phenix() qui
    // renvoient { { map="x,y", path="top", gather=true, fight=true,
    //              door="254", custom=fn, ... }, ... }
    // =================================================================
    private void ExecuterRoute()
    {
        var rnd = new Random();
        var ct = _annulation!.Token;
        var anka = _api.Anka;

        // Lecture des seuils globaux du script (chaque tour pour permettre
        // override à chaud — mais en pratique lus 1 fois après loadString).
        int seuilMarchand = 0;
        int seuilBanque = 98;
        var smp = _script!.Globals.Get("MARCHAND_SEUIL_PODS");
        if (smp.Type == DataType.Number) seuilMarchand = (int)smp.Number;
        var mxp = _script.Globals.Get("MAX_PODS");
        if (mxp.Type == DataType.Number) seuilBanque = (int)mxp.Number;
        Journaliseur.Info($"[ANKA] Seuils farm : banque={seuilBanque}% (MAX_PODS), marchand={seuilMarchand}% (MARCHAND_SEUIL_PODS), si marchand=0 → désactivé");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (anka.Character.lifePoints() <= 0
                    && _script!.Globals.Get("phenix").Type == DataType.Function)
                { SuivreUnTour("phenix", rnd, ct); continue; }

                // Détour MARCHAND auto si seuil défini ET section marchand() présente.
                var fnMarchand = NomFn("marchand", "merchant");
                if (seuilMarchand > 0 && anka.Inventory.podsP() >= seuilMarchand && fnMarchand != null)
                {
                    Journaliseur.Info($"[ANKA] 📦 Pods {anka.Inventory.podsP()}% ≥ MARCHAND_SEUIL_PODS {seuilMarchand}% → détour marchand()");
                    SuivreUnTour(fnMarchand, rnd, ct);
                    continue;
                }

                var fnBanque = NomFn("bank", "banque");
                if (anka.Inventory.podsP() >= seuilBanque && fnBanque != null)
                {
                    Journaliseur.Info($"[ANKA] 💰 Pods {anka.Inventory.podsP()}% ≥ MAX_PODS {seuilBanque}% → détour banque()");
                    SuivreUnTour(fnBanque, rnd, ct);
                    continue;
                }

                SuivreUnTour(NomFn("move", "mouvement") ?? "move", rnd, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[ANKA] {ex.Message}");
                Pause(2500, ct);
            }
        }
    }

    /// <summary>1re fonction globale existante parmi les alias donnés.</summary>
    private string? NomFn(params string[] alias)
    {
        foreach (var a in alias)
            if (_script!.Globals.Get(a).Type == DataType.Function) return a;
        return null;
    }

    // Index du waypoint courant dans la liste mouvement(). -1 = pas encore
    // synchronisé (on se cale sur la carte de départ au 1er tour).
    private int _idxRoute = -1;

    private void SuivreUnTour(string fn, Random rnd, CancellationToken ct)
    {
        var res = _script!.Call(_script.Globals.Get(fn));
        if (res.Type != DataType.Table) { Pause(2000, ct); return; }

        // Liste ORDONNÉE des waypoints (clés 1..n).
        var rows = new System.Collections.Generic.List<Table>();
        for (int i = 1; ; i++)
        {
            var v = res.Table.Get(i);
            if (v.Type == DataType.Nil) break;
            if (v.Type == DataType.Table) rows.Add(v.Table);
        }
        if (rows.Count == 0) { Pause(2000, ct); return; }

        var coords = _api.Anka.Map.currentMap();          // "x,y"
        var mapId = _api.Anka.Map.currentMapId().ToString();
        bool Match(Table r)
        {
            var m = r.Get("map").CastToString();
            return m == coords || m == mapId;
        }
        // Cherche un waypoint correspondant à la carte courante À PARTIR de
        // `depart` (en bouclant). Crucial : sur une carte qui revient 2×, on
        // repart APRÈS l'index courant → on prend la 2e occurrence (donc la
        // 2e action), pas la 1re. C'est ça « passer 2× sans refaire la 1re ».
        int Depuis(int depart)
        {
            for (int k = 0; k < rows.Count; k++)
            {
                int j = (depart + k) % rows.Count;
                if (Match(rows[j])) return j;
            }
            return -1;
        }

        if (_idxRoute < 0 || _idxRoute >= rows.Count)
        {
            // 1er tour : on se cale sur la carte où on est.
            _idxRoute = Depuis(0);
            if (_idxRoute < 0)
            {
                Journaliseur.Info($"[ANKA] carte {coords} (id {mapId}) hors trajet — attente (lance le script depuis une carte du trajet)");
                Pause(3000, ct);
                return;
            }
        }
        else if (!Match(rows[_idxRoute]))
        {
            // Désync (mauvaise sortie, déplacement manuel…) : on resynchronise
            // en cherchant À PARTIR de l'index courant (pas depuis 0) → on ne
            // retombe pas sur une occurrence précédente de cette carte.
            int j = Depuis(_idxRoute);
            if (j < 0)
            {
                Journaliseur.Info($"[ANKA] carte {coords} (id {mapId}) imprévue — attente (resynchro)");
                Pause(2500, ct);
                return;
            }
            _idxRoute = j;
        }

        ExecuterLigne(rows[_idxRoute], rnd, ct);
        // Étape suivante dans l'ORDRE (boucle en fin de liste).
        _idxRoute = (_idxRoute + 1) % rows.Count;
    }

    private void ExecuterLigne(Table row, Random rnd, CancellationToken ct)
    {
        var anka = _api.Anka;
        bool B1(string k) { var v = row.Get(k); return v.Type != DataType.Nil && v.CastToBool(); }
        // Tolère les variantes de casse (AnkaBot forcefight / Frigost forceFight).
        bool B(params string[] ks) => ks.Any(B1);
        string S(string k) { var v = row.Get(k); return v.Type == DataType.Nil ? "" : v.CastToString(); }

        // 1) Récolte
        if (B("gather", "forcegather", "forceGather"))
        {
            bool force = B("forcegather", "forceGather");
            int n;
            do
            {
                if (ct.IsCancellationRequested) return;
                n = anka.Map.gather();
            }
            while (force && n > 0 && anka.Inventory.podsP() < 98
                   && !ct.IsCancellationRequested);
        }
        // 2) Combat
        if (B("fight", "forcefight", "forceFight"))
        {
            bool force = B("forcefight", "forceFight");
            do
            {
                if (ct.IsCancellationRequested) return;
                anka.Map.fight();
                while (anka.Character.isInFight() && !ct.IsCancellationRequested)
                    Pause(1000, ct);
            }
            while (force && anka.Map.monsterGroups().Length > 0 && !ct.IsCancellationRequested);
        }
        // 3) Door (élément interactif → change souvent de carte)
        if (S("door") is { Length: > 0 } d && int.TryParse(d, out var dcell))
        {
            int avant = anka.Map.currentMapId();
            anka.Map.door(dcell);
            AttendreChangementCarte(avant, ct);
        }
        // 3b) PNJ : npc = <idPnj>, answers = { r1, r2, ... }  (format SynFus)
        // Si npc_marchand=true, on déclenche la VENTE AUTO complète au PNJ
        // (DC → EMO+ par item → DV) au lieu du dialogue classique.
        var npcV = row.Get("npc");
        if (npcV.Type == DataType.Number && B("npc_marchand", "npcMarchand"))
        {
            int idPnj = (int)npcV.Number;
            Journaliseur.Info($"[ANKA] npc_marchand=true → vente auto au PNJ #{idPnj}");
            _api.vendre_tout_pnj(idPnj);
            return;  // sortie de l'étape, prochaine étape démarre
        }
        if (npcV.Type == DataType.Number)
        {
            int idPnj = (int)npcV.Number;
            anka.Npc.npc(idPnj);
            Pause(900, ct);
            var ans = row.Get("answers");
            if (ans.Type == DataType.Table)
                foreach (var ap in ans.Table.Pairs)
                {
                    if (ct.IsCancellationRequested) return;
                    if (ap.Value.Type != DataType.Number) continue;
                    int rep = (int)ap.Value.Number;
                    // Convention AnkaBot/SynFus : un nombre NÉGATIF = la N-ième
                    // réponse proposée (-1 = 1ère, -2 = 2ème, …). Un nombre
                    // positif = un replyId brut (compat ascendante).
                    if (rep < 0)
                    {
                        int n = -rep;                       // -1→1, -2→2 …
                        var ids = anka.Npc.getRepliesId();
                        if (ids.Length >= n) rep = (int)ids.Get(n).Number;
                        else if (ids.Length > 0) rep = (int)ids.Get(1).Number;
                        else break;
                    }
                    anka.Npc.reply(rep);
                    Pause(900, ct);
                }
            // TOUJOURS fermer le dialogue (DV) — qu'il y ait eu des réponses
            // ou non. Tant que le dialogue PNJ reste ouvert, le SERVEUR
            // IGNORE tout déplacement (GA001) → le perso reste coincé sur sa
            // case (cf. log : après le PNJ, raw + secours cell échouent,
            // perso bloqué 310). À l'enregistrement, « Terminer le dialogue »
            // envoyait ce DV ; au rejeu il manquait.
            anka.Npc.leave();
            Pause(700, ct);
        }
        // 4) custom / lockedCustom
        var custom = row.Get("custom");
        if (custom.Type == DataType.Function) _script!.Call(custom);
        var lcustom = row.Get("lockedCustom");
        if (lcustom.Type == DataType.Function) _script!.Call(lcustom);
        // 5) npcBank (protocole banque pas encore branché)
        if (B("npcBank")) Journaliseur.Avertir("[ANKA] npcBank non supporté (ignoré)");
        // 5b) ZAAP — sortie via téléportation (capturée par RoadCreator).
        // Protocole Hystoria : GA500<cellZaap>;114 + WU<mapDest> (cf.
        // tech_zaap_protocole_hystoria.md). Si la téléportation aboutit,
        // on a déjà changé de carte → on saute le bloc path/door normal.
        var zaapV = row.Get("zaap");
        if (zaapV.Type == DataType.Number && (int)zaapV.Number > 0)
        {
            int mapDest = (int)zaapV.Number;
            int avant = anka.Map.currentMapId();
            Journaliseur.Info($"[ANKA] zaap vers map #{mapDest} (depuis #{avant})");
            bool ok = _api.zaap(mapDest);
            if (ok)
            {
                AttendreChangementCarte(avant, ct);
                return; // sortie réussie, zaap remplace path/cell/direction
            }
            Journaliseur.Avertir($"[ANKA] zaap #{mapDest} échoué → fallback path classique.");
            // (on continue vers le bloc path/cell ci-dessous)
        }
        // 6) Changement de carte (path)
        var path = S("path");
        if (path.Length > 0)
        {
            // raw:GA001 CONDITIONNEL — garde-fou « vérité terrain ».
            // Le chemin brut n'est fidèle que depuis la cellule de départ
            // EXACTE de l'enregistrement (champ `from`). En entrant par une
            // transition la cellule d'arrivée est déterministe → from matche
            // → rejeu fidèle (zéro pathfinder, le pathfinder se trompe de
            // route sur ces maps 15×17). Sinon (map#1 / départ arbitraire)
            // on saute DIRECT à la cellule de sortie (SortirCarteAsync
            // transition-first/direction) → zéro détour, zéro 13 s perdues.
            if (path.StartsWith("raw:", StringComparison.OrdinalIgnoreCase))
            {
                var fromV = row.Get("from");
                var cellG = row.Get("cell");
                if (fromV.Type == DataType.Number
                    && cellG.Type == DataType.Number)
                {
                    AttendrePositionConnue(ct);
                    int cur = anka.Map.currentCell();
                    int frm = (int)fromV.Number;
                    if (cur != frm)
                    {
                        Journaliseur.Info($"[ANKA] départ {cur} ≠ enregistré "
                            + $"{frm} → raw ignoré, sortie directe cellule "
                            + $"{(int)cellG.Number}.");
                        path = ((int)cellG.Number).ToString();
                    }
                    else
                        Journaliseur.Info($"[ANKA] départ {cur} = enregistré "
                            + $"{frm} → rejeu fidèle du chemin brut.");
                }
            }
            int avant = anka.Map.currentMapId();
            AppliquerPath(path, rnd, ct);
            AttendreChangementCarte(avant, ct);

            // SECOURS : si la carte n'a pas changé après un raw:GA001 (on est
            // entré ailleurs qu'à l'enregistrement), on retombe sur la CELLULE
            // de sortie réelle capturée (champ cell=…) et on la rejoue.
            if (anka.Map.currentMapId() == avant)
            {
                var cellV = row.Get("cell");
                if (cellV.Type == DataType.Number)
                {
                    int cs = (int)cellV.Number;
                    Journaliseur.Info(
                        $"[ANKA] raw inchangé → secours cellule de sortie {cs}.");
                    AttendrePositionConnue(ct);
                    for (int e = 0; e < 4 && !ct.IsCancellationRequested
                                    && anka.Map.currentMapId() == avant; e++)
                    {
                        anka.Map.sortie(cs);
                        AttendreChangementCarte(avant, ct);
                        if (anka.Map.currentMapId() == avant) Pause(700, ct);
                    }
                }
            }
            // Toujours bloqué : ni le raw ni la cellule n'ont fait sortir.
            if (anka.Map.currentMapId() == avant)
            {
                Journaliseur.Avertir(
                    $"[ANKA] carte {avant} inchangée après path='{path}' "
                    + "(et secours cellule). Sortie non déclenchée — "
                    + "essaie une DIRECTION (top/bottom/left/right).");
                Pause(2500, ct);
            }
        }
        else Pause(800, ct);
    }

    private void AppliquerPath(string path, Random rnd, CancellationToken ct)
    {
        path = path.Trim();
        if (path.Contains('|'))
        {
            var choix = path.Split('|', StringSplitOptions.RemoveEmptyEntries);
            path = choix[rnd.Next(choix.Length)].Trim();
        }
        // "raw:GA001…" → rejeu VERBATIM du déplacement capturé à la main
        // (chemin déjà accepté par le serveur → zéro pathfinder, zéro
        // rollback). Le plus fiable pour sortir d'auberge / changer de map.
        if (path.StartsWith("raw:", StringComparison.OrdinalIgnoreCase))
        {
            AttendrePositionConnue(ct);
            _api.Anka.Map.replayPath(path.Substring(4));
            return;
        }
        // "top(364)" / "left(12)" → on marche sur la cellule de sortie
        int po = path.IndexOf('(');
        if (po >= 0 && path.EndsWith(")"))
        {
            var inner = path.Substring(po + 1, path.Length - po - 2);
            if (int.TryParse(inner, out var cellExit)) { _api.Anka.Map.sortie(cellExit); return; }
        }
        // "364" → cellule déclencheuse directe. On attend que la position soit
        // connue (sinon, juste après un changement de carte, le déplacement
        // échoue « position perso inconnue ») puis on RÉESSAIE.
        if (int.TryParse(path, out var cell))
        {
            AttendrePositionConnue(ct);
            for (int essai = 0; essai < 4 && !ct.IsCancellationRequested; essai++)
            {
                if (_api.Anka.Map.sortie(cell)) return;
                Pause(700, ct);
            }
            return;
        }
        // "zaap(...)" / "zaapi(...)" / "havenbag" : non supportés
        if (path.StartsWith("zaap") || path.StartsWith("havenbag"))
        { Journaliseur.Avertir($"[ANKA] path '{path}' non supporté (ignoré)"); return; }
        // Direction : top/bottom/left/right
        var dir = path switch
        {
            "top" => "nord", "haut" => "nord",
            "bottom" => "sud", "bas" => "sud",
            "right" => "est", "droite" => "est",
            "left" => "ouest", "gauche" => "ouest",
            _ => path
        };
        _api.Anka.Map.changeMap(dir);
    }

    private void AttendreChangementCarte(int mapIdAvant, CancellationToken ct)
    {
        for (int i = 0; i < 30 && !ct.IsCancellationRequested; i++)
        {
            if (_api.Anka.Map.currentMapId() != mapIdAvant)
            {
                // Carte changée : on attend en plus que la position du perso
                // soit connue, sinon l'action suivante (moveToCell/npc) part
                // trop tôt et échoue « position perso inconnue ».
                AttendrePositionConnue(ct);
                return;
            }
            Pause(300, ct);
        }
    }

    /// <summary>Attend (≈6 s max) que la cellule du perso soit connue, p.ex.
    /// juste après un GDM/GM de changement de carte.</summary>
    private void AttendrePositionConnue(CancellationToken ct)
    {
        for (int i = 0; i < 24 && !ct.IsCancellationRequested; i++)
        {
            if (_api.Anka.Map.currentCell() >= 0) { Pause(250, ct); return; }
            Pause(250, ct);
        }
    }

    private static void Pause(int ms, CancellationToken ct)
    {
        try { Task.Delay(ms, ct).GetAwaiter().GetResult(); }
        catch (OperationCanceledException) { }
    }

    /// <summary>Arrête l'exécution en cours. Le script peut prendre du temps à réagir si bloqué dans Task.Delay.</summary>
    public void Arreter()
    {
        if (!EnExecution) return;
        Journaliseur.Info("[LUA] Demande d'arrêt du script...");
        _annulation?.Cancel();
        // Note : MoonSharp n'a pas de mécanisme natif pour arrêter un script en cours d'exécution.
        // Si le script fait `while true do end`, il faut le terminer en killant le bot.
        // Pour les scripts bien écrits qui appellent bot.attendre() régulièrement,
        // on pourrait propager le cancellation token à Task.Delay (TODO si besoin).
    }

    public void Dispose()
    {
        Arreter();
        _annulation?.Dispose();
    }
}
