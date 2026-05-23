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

                // === FORCEFIGHT : reste sur la map et farme tous les groupes ===
                // CAS SPÉCIAL : on n'exécute le path/cell qu'au PREMIER passage
                // (ou si on a quitté la map). Sinon on lance directement la boucle
                // combats. Sans ce check, le path "raw:GA001..." était rejoué à
                // chaque combat → le perso traversait la transition vers la map
                // voisine (bug forensic 17:17-23 où bot oscillait 7804↔7799).
                if (etape.ForcerFightBoucle)
                {
                    int mapEtape = int.TryParse(etape.IdentifiantCarte, out var mE) ? mE : 0;
                    int mapActuelle = _api.CartesCourantes;
                    if (mapActuelle != mapEtape)
                    {
                        // On n'est pas sur la map cible — exécuter le déplacement.
                        await ExecuterEtapeAsync(etape, ct).ConfigureAwait(false);
                    }
                    while (!ct.IsCancellationRequested
                        && _api.MonstreLePlusProche() != null)
                    {
                        Journaliseur.Info("[SCRIPT] forcefight=true : encore un groupe sur la map → re-engage");
                        bool ok = await _api.EngagerCombatAsync(ct).ConfigureAwait(false);
                        if (!ok) break;
                        CompteurCombats++;
                    }
                    EtapeTerminee?.Invoke(this, etape);
                    // Reste sur la même étape — délai 2s pour laisser respawn éventuel.
                    await System.Threading.Tasks.Task.Delay(2000, ct).ConfigureAwait(false);
                    continue;
                }

                // Étape normale : exécute déplacement + dialogue + combat éventuel.
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
