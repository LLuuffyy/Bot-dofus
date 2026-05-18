using System;
using System.IO;
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
