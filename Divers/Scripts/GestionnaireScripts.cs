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
                // suit la section marchand() (aller jusqu'au PNJ + vente),
                // puis le bot cherche dans EtapesMouvement une étape qui matche
                // la map courante (= continuation du trajet mouvement depuis
                // l'endroit où il s'est arrêté pour aller au marchand).
                // L'user doit s'assurer que mouvement() couvre AUSSI les maps
                // de retour depuis la taverne.
                int seuilM = ScriptCourant.Configuration.MarchandSeuilPods;
                if (seuilM > 0 && ScriptCourant.EtapesMarchand.Count > 0
                    && _api.PourcentagePoids >= seuilM)
                {
                    Journaliseur.Info($"[SCRIPT] 📦 Poids {_api.PourcentagePoids:F1}% ≥ MARCHAND_SEUIL_PODS {seuilM}% → détour trajet marchand");
                    await ExecuterSectionMarchandAsync(ct).ConfigureAwait(false);
                    // Cherche une étape mouvement matchant la map courante pour
                    // reprendre le farm. Si introuvable → reset à 0 (= début).
                    int mapApres = _api.CartesCourantes;
                    int idxRetour = ScriptCourant.EtapesMouvement
                        .Select((e, i) => (e, i))
                        .Where(x => int.TryParse(x.e.IdentifiantCarte, out int m) && m == mapApres)
                        .Select(x => (int?)x.i)
                        .FirstOrDefault() ?? 0;
                    IndexEtapeCourante = idxRetour;
                    Journaliseur.Info($"[SCRIPT] Détour marchand terminé → reprise mouvement à l'étape {idxRetour + 1} (map {mapApres})");
                    continue;
                }

                var etape = ScriptCourant.EtapesMouvement[IndexEtapeCourante];
                EtapeDemarree?.Invoke(this, etape);
                Journaliseur.Info($"Étape {IndexEtapeCourante + 1}/{ScriptCourant.EtapesMouvement.Count} : {etape}");

                await ExecuterEtapeAsync(etape, ct).ConfigureAwait(false);

                // === FORCEFIGHT : boucle de combats sur la même map ===
                // Si l'étape a forcefight=true, on reste sur la map et on engage
                // tous les groupes attaquables (= filtres OK/NO_MONSTER + MIN/MAX
                // appliqués via MonstreLePlusProche) avant de passer à l'étape
                // suivante. Le fight=true classique fait UN combat puis avance.
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
