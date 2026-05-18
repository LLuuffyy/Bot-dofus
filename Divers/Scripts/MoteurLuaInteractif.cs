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
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (anka.Character.lifePoints() <= 0
                    && _script!.Globals.Get("phenix").Type == DataType.Function)
                { SuivreUnTour("phenix", rnd, ct); continue; }

                var fnBanque = NomFn("bank", "banque");
                if (anka.Inventory.podsP() >= 98 && fnBanque != null)
                { SuivreUnTour(fnBanque, rnd, ct); continue; }

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

    private void SuivreUnTour(string fn, Random rnd, CancellationToken ct)
    {
        var res = _script!.Call(_script.Globals.Get(fn));
        if (res.Type != DataType.Table) { Pause(2000, ct); return; }

        var coords = _api.Anka.Map.currentMap();          // "x,y"
        var mapId = _api.Anka.Map.currentMapId().ToString();
        DynValue? ligne = null;
        foreach (var p in res.Table.Pairs)
        {
            if (p.Value.Type != DataType.Table) continue;
            // SynFus utilise map="<idMap>" (ex. "9127"), AnkaBot map="x,y" :
            // on accepte les deux.
            var m = p.Value.Table.Get("map").CastToString();
            if (m == coords || m == mapId) { ligne = p.Value; break; }
        }
        if (ligne == null)
        {
            Journaliseur.Info($"[ANKA] carte {coords} (id {mapId}) non prévue dans {fn}() — attente");
            Pause(3000, ct);
            return;
        }
        ExecuterLigne(ligne.Table, rnd, ct);
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
        var npcV = row.Get("npc");
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
                    // -1 = première réponse disponible (convention SynFus)
                    if (rep == -1)
                    {
                        var ids = anka.Npc.getRepliesId();
                        if (ids.Length > 0) rep = (int)ids.Get(1).Number;
                        else { anka.Npc.leave(); break; }
                    }
                    anka.Npc.reply(rep);
                    Pause(900, ct);
                }
            else anka.Npc.leave();
        }
        // 4) custom / lockedCustom
        var custom = row.Get("custom");
        if (custom.Type == DataType.Function) _script!.Call(custom);
        var lcustom = row.Get("lockedCustom");
        if (lcustom.Type == DataType.Function) _script!.Call(lcustom);
        // 5) npcBank (protocole banque pas encore branché)
        if (B("npcBank")) Journaliseur.Avertir("[ANKA] npcBank non supporté (ignoré)");
        // 6) Changement de carte (path)
        var path = S("path");
        if (path.Length > 0)
        {
            int avant = anka.Map.currentMapId();
            AppliquerPath(path, rnd, ct);
            AttendreChangementCarte(avant, ct);
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
        // "top(364)" / "left(12)" → on marche sur la cellule de sortie
        int po = path.IndexOf('(');
        if (po >= 0 && path.EndsWith(")"))
        {
            var inner = path.Substring(po + 1, path.Length - po - 2);
            if (int.TryParse(inner, out var cellExit)) { _api.Anka.Map.moveToCell(cellExit); return; }
        }
        // "364" → cellule déclencheuse directe
        if (int.TryParse(path, out var cell)) { _api.Anka.Map.moveToCell(cell); return; }
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
            if (_api.Anka.Map.currentMapId() != mapIdAvant) return;
            Pause(300, ct);
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
