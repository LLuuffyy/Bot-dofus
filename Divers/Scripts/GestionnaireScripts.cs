using System;
using System.Threading;
using System.Threading.Tasks;
using BotDofus.Divers.Enums;
using BotDofus.Divers.Scripts.Api;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Scripts;

/// <summary>
/// Orchestrateur d'exécution d'un <see cref="ScriptCharge"/> : parcourt les étapes,
/// invoque les actions correspondantes via l'<see cref="ApiBot"/>, gère pause/reprise
/// et arrêt propre.
/// </summary>
public sealed class GestionnaireScripts : IDisposable
{
    private readonly Compte _compte;
    private readonly ApiBot _api;
    private CancellationTokenSource? _annulation;
    private ManualResetEventSlim _porteMobile = new(true);
    private Task? _tache;

    public ScriptCharge? ScriptCourant { get; private set; }
    public EtatScript Etat { get; private set; } = EtatScript.Inactif;
    public int IndexEtapeCourante { get; private set; }
    public int CompteurCombats { get; private set; }

    public event EventHandler<EtatScript>? EtatChange;
    public event EventHandler<EtapeScript>? EtapeDemarree;
    public event EventHandler<EtapeScript>? EtapeTerminee;

    public GestionnaireScripts(Compte compte, ApiBot api)
    {
        _compte = compte;
        _api = api;
    }

    public Task DemarrerAsync(ScriptCharge script)
    {
        if (Etat == EtatScript.EnExecution)
        {
            Journaliseur.Avertir("Un script est déjà en exécution, arrêt du précédent avant");
            Arreter();
        }

        ScriptCourant = script;
        IndexEtapeCourante = 0;
        CompteurCombats = 0;
        _porteMobile.Set();
        _annulation = new CancellationTokenSource();

        Journaliseur.Info($"Démarrage du script : {script}");
        ChangerEtat(EtatScript.EnExecution);
        _compte.ChangerEtat(EtatsCompte.ScriptEnCours);

        _tache = Task.Run(() => BoucleAsync(_annulation.Token));
        return _tache;
    }

    public void MettreEnPause()
    {
        if (Etat != EtatScript.EnExecution) return;
        _porteMobile.Reset();
        ChangerEtat(EtatScript.EnPause);
        _compte.ChangerEtat(EtatsCompte.ScriptEnPause);
    }

    public void Reprendre()
    {
        if (Etat != EtatScript.EnPause) return;
        _porteMobile.Set();
        ChangerEtat(EtatScript.EnExecution);
        _compte.ChangerEtat(EtatsCompte.ScriptEnCours);
    }

    public void Arreter()
    {
        _annulation?.Cancel();
        _porteMobile.Set();
        ChangerEtat(EtatScript.Interrompu);
    }

    private async Task BoucleAsync(CancellationToken ct)
    {
        if (ScriptCourant == null) return;

        try
        {
            while (!ct.IsCancellationRequested && IndexEtapeCourante < ScriptCourant.EtapesMouvement.Count)
            {
                _porteMobile.Wait(ct);

                var etape = ScriptCourant.EtapesMouvement[IndexEtapeCourante];
                EtapeDemarree?.Invoke(this, etape);
                Journaliseur.Info($"Étape {IndexEtapeCourante + 1}/{ScriptCourant.EtapesMouvement.Count} : {etape}");

                await ExecuterEtapeAsync(etape, ct).ConfigureAwait(false);

                EtapeTerminee?.Invoke(this, etape);
                IndexEtapeCourante++;
            }

            ChangerEtat(ct.IsCancellationRequested ? EtatScript.Interrompu : EtatScript.Termine);
            _compte.ChangerEtat(EtatsCompte.EnJeu);
            Journaliseur.Info($"Script terminé (état : {Etat})");
        }
        catch (OperationCanceledException)
        {
            ChangerEtat(EtatScript.Interrompu);
        }
        catch (Exception ex)
        {
            Journaliseur.Critique($"Script {ScriptCourant?.Nom} arrêté sur erreur", ex);
            ChangerEtat(EtatScript.Erreur);
        }
    }

    private async Task ExecuterEtapeAsync(EtapeScript etape, CancellationToken ct)
    {
        await _api.SeDeplacerVersCarteAsync(etape.IdentifiantCarte, etape.Direction, ct).ConfigureAwait(false);

        if (etape.CelluleCible.HasValue)
        {
            await _api.SeDeplacerVersCelluleAsync(etape.CelluleCible.Value, ct).ConfigureAwait(false);
        }

        if (etape.IdentifiantPNJ.HasValue)
        {
            await _api.ParlerAuPNJAsync(etape.IdentifiantPNJ.Value, etape.ReponsesDialogue, ct).ConfigureAwait(false);
        }

        if (etape.EngagerCombat)
        {
            CompteurCombats++;
            await _api.EngagerCombatAsync(ct).ConfigureAwait(false);
        }

        if (etape.UtiliserBanque)
        {
            await _api.UtiliserBanqueAsync(ct).ConfigureAwait(false);
        }
    }

    private void ChangerEtat(EtatScript nouveau)
    {
        if (Etat == nouveau) return;
        Etat = nouveau;
        EtatChange?.Invoke(this, nouveau);
    }

    public void Dispose()
    {
        Arreter();
        _annulation?.Dispose();
        _porteMobile.Dispose();
    }
}
