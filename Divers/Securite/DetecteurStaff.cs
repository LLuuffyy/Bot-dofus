using System;
using System.Threading;
using System.Threading.Tasks;
using BotDofus.Commun.Reseau;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Securite;

/// <summary>
/// Anti-modération : envoie périodiquement la commande <c>.staff</c> au serveur Hystoria
/// (qui répond avec la liste des modos en ligne via un message Im custom).
///
/// Si un modo est détecté, applique la réaction configurée dans <see cref="ConfigSecurite"/> :
/// arrêt complet, ralentissement, pauses entre combats, ou alerte uniquement.
///
/// Détection : on observe les paquets serveur Im qui contiennent le mot "modo", "staff",
/// "[STAFF]" ou similaire. Le format exact dépend de Hystoria.
/// </summary>
public sealed class DetecteurStaff : IDisposable
{
    private readonly ConfigSecurite _config;
    private SessionProxy? _session;
    private CancellationTokenSource? _annulation;
    private static readonly Random Rng = new();

    public bool ModoDetecte { get; private set; }
    public DateTime? DernierCheck { get; private set; }
    public DateTime? DernierModoDetecte { get; private set; }

    /// <summary>True quand le bot doit se mettre en pause (suite à détection modo).</summary>
    public bool EnPauseProtection { get; private set; }

    public event EventHandler? ModoDetecteChange;

    public DetecteurStaff(ConfigSecurite config)
    {
        _config = config;
    }

    public void LierSession(SessionProxy session)
    {
        _session = session;
        // S'abonner aux paquets serveur pour détecter les réponses .staff
        session.PaquetRecu += OnPaquet;
    }

    public Task DemarrerAsync(CancellationToken ct = default)
    {
        if (!_config.VerificationStaffActive) return Task.CompletedTask;
        _annulation?.Cancel();
        _annulation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        return Task.Run(() => BoucleVerificationAsync(_annulation.Token), _annulation.Token);
    }

    private async Task BoucleVerificationAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                int minutes = Rng.Next(_config.IntervalleCheckMinMin, _config.IntervalleCheckMinMax + 1);
                await Task.Delay(TimeSpan.FromMinutes(minutes), ct).ConfigureAwait(false);
                if (_session == null) continue;

                Journaliseur.Info("[SEC] Check .staff périodique");
                DernierCheck = DateTime.Now;
                ModoDetecte = false; // reset, sera mis à true si on détecte une réponse modo

                // Envoyer .staff dans le canal général
                await _session.EnvoyerAuServeurAsync("BMA*.staff", ct).ConfigureAwait(false);

                // Attendre 3s la réponse, puis évaluer
                await Task.Delay(3000, ct).ConfigureAwait(false);

                if (ModoDetecte) await ReagirAModoAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[SEC] Erreur boucle staff : {ex.Message}");
            }
        }
    }

    private void OnPaquet(object? sender, EvenementPaquetRecu e)
    {
        // Détecte les réponses contenant "[STAFF]", "modo", "modérateur" etc.
        if (e.Paquet.Direction != DirectionPaquet.VersClient) return;
        var c = e.Paquet.Contenu;
        if (c.Contains("[STAFF]", StringComparison.OrdinalIgnoreCase)
            || c.Contains("modérateur", StringComparison.OrdinalIgnoreCase)
            || c.Contains("animateur", StringComparison.OrdinalIgnoreCase)
            || (c.StartsWith("Im") && c.Contains("staff", StringComparison.OrdinalIgnoreCase) && !c.Contains("aucun", StringComparison.OrdinalIgnoreCase)))
        {
            if (!ModoDetecte)
            {
                ModoDetecte = true;
                DernierModoDetecte = DateTime.Now;
                Journaliseur.Avertir($"[SEC] ⚠️ MODO DÉTECTÉ : {c[..Math.Min(c.Length, 100)]}");
                ModoDetecteChange?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private async Task ReagirAModoAsync(CancellationToken ct)
    {
        if (_config.ArretCompletSiModo)
        {
            int minutes = Rng.Next(_config.PauseSiModoMin, _config.PauseSiModoMax + 1);
            Journaliseur.Avertir($"[SEC] ARRÊT du bot pour {minutes} min");
            EnPauseProtection = true;
            await Task.Delay(TimeSpan.FromMinutes(minutes), ct).ConfigureAwait(false);
            EnPauseProtection = false;
        }
        if (_config.AlerteUniquementSiModo)
        {
            Journaliseur.Avertir("[SEC] 🔔 ALERTE MODO — humanise tes actions");
            try { System.Media.SystemSounds.Exclamation.Play(); } catch { }
        }
        // Ralentissement et pauses combats : appliqués indirectement via ConfigDelais.MultiplicateurEffectif()
        // (à implémenter dans le code combat). Cette classe expose juste l'état.
    }

    public double MultiplicateurDelaisActuel
    {
        get
        {
            if (!ModoDetecte || !_config.RalentirSiModo) return 1.0;
            return _config.FacteurRalentissement / 100.0;
        }
    }

    public void Dispose()
    {
        _annulation?.Cancel();
        _annulation?.Dispose();
        if (_session != null) _session.PaquetRecu -= OnPaquet;
    }
}
