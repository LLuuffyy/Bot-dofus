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
        // Expose la config script au Compte pour que ApiBot.MonstreLePlusProche
        // puisse appliquer les filtres OK_MONSTER / NO_MONSTER / MIN-MAX_MONSTERS.
        _compte.ConfigScriptCourante = script.Configuration;
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

                // === Détour MARCHAND auto ===
                // Si pods >= MARCHAND_SEUIL_PODS et trajet marchand défini, on
                // suit la section marchand() (aller jusqu'au PNJ + vente).
                // Après vente, le bot est sur la map du PNJ. Il cherche dans
                // EtapesMouvement une étape qui matche cette map pour reprendre
                // le mouvement à cet index (= début du chemin retour).
                // Convention : l'user place les maps de retour APRÈS la zone
                // farm dans mouvement(), la PREMIÈRE étape retour étant la
                // map du PNJ marchand elle-même.
                int seuilM = ScriptCourant.Configuration.MarchandSeuilPods;
                if (seuilM > 0 && ScriptCourant.EtapesMarchand.Count > 0
                    && _api.PourcentagePoids >= seuilM)
                {
                    Journaliseur.Info($"[SCRIPT] 📦 Poids {_api.PourcentagePoids:F1}% ≥ MARCHAND_SEUIL_PODS {seuilM}% → détour trajet marchand");
                    await ExecuterSectionMarchandAsync(ct).ConfigureAwait(false);
                    int mapApres = _api.CartesCourantes;
                    int idxRetour = -1;
                    for (int i = 0; i < ScriptCourant.EtapesMouvement.Count; i++)
                    {
                        if (int.TryParse(ScriptCourant.EtapesMouvement[i].IdentifiantCarte, out int m) && m == mapApres)
                        { idxRetour = i; break; }
                    }
                    IndexEtapeCourante = idxRetour >= 0 ? idxRetour : 0;
                    Journaliseur.Info($"[SCRIPT] Détour marchand terminé → reprise mouvement étape {IndexEtapeCourante + 1} (map {mapApres})");
                    continue;
                }

                var etape = ScriptCourant.EtapesMouvement[IndexEtapeCourante];
                EtapeDemarree?.Invoke(this, etape);
                Journaliseur.Info($"Étape {IndexEtapeCourante + 1}/{ScriptCourant.EtapesMouvement.Count} : {etape}");

                await ExecuterEtapeAsync(etape, ct).ConfigureAwait(false);

                // === FORCEFIGHT : boucle de combats sur la même map ===
                // Si l'étape a forcefight=true, on reste sur la map et on engage
                // tous les groupes attaquables. Quand plus de mobs, on NE passe
                // PAS à l'étape suivante (sinon le bot quitterait la zone farm
                // pour aller sur les maps retour). On boucle indéfiniment sur
                // cette étape — seul un détour marchand/banque peut sortir.
                if (etape.ForcerFightBoucle)
                {
                    while (!ct.IsCancellationRequested
                        && _api.MonstreLePlusProche() != null)
                    {
                        Journaliseur.Info("[SCRIPT] forcefight=true : encore un groupe sur la map → re-engage");
                        bool ok = await _api.EngagerCombatAsync(ct).ConfigureAwait(false);
                        if (!ok) break;
                        CompteurCombats++;  // n'incrémente qu'après succès
                    }
                    EtapeTerminee?.Invoke(this, etape);
                    // PAS d'incrément : on reste sur la même étape (rebascule
                    // dans la boucle while du début pour re-check marchand/pods
                    // puis re-tenter la map).
                    await System.Threading.Tasks.Task.Delay(1000, ct).ConfigureAwait(false);
                    continue;
                }

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

    /// <summary>
    /// Suit la section <c>marchand()</c> du script : déplacements jusqu'au
    /// PNJ + vente automatique de tous les items équipements sur place
    /// (via <see cref="EtapeScript.UtiliserMarchand"/> ou
    /// <see cref="EtapeScript.IdentifiantPNJ"/>).
    /// </summary>
    private async Task ExecuterSectionMarchandAsync(CancellationToken ct)
    {
        if (ScriptCourant == null) return;
        foreach (var etape in ScriptCourant.EtapesMarchand)
        {
            if (ct.IsCancellationRequested) break;
            Journaliseur.Info($"[SCRIPT-MARCHAND] {etape}");
            await ExecuterEtapeAsync(etape, ct).ConfigureAwait(false);

            // Vente PNJ : si l'étape est marquée UtiliserMarchand OU si elle
            // a un IdentifiantPNJ et qu'on est arrivé sur la map du marchand,
            // déclenche la vente complète.
            if (etape.UtiliserMarchand && etape.IdentifiantPNJ.HasValue)
            {
                await _api.VendreToutAuPnjAsync(etape.IdentifiantPNJ.Value, null, ct).ConfigureAwait(false);
            }
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
            bool ok = await _api.EngagerCombatAsync(ct).ConfigureAwait(false);
            if (ok) CompteurCombats++;  // n'incrémente que si combat vraiment lancé
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
