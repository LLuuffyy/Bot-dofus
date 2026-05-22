using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BotDofus.Commun.Reseau;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.MultiAccount;

/// <summary>
/// Envoie une série de <c>PI&lt;nom&gt;</c> au serveur Abrak pour inviter les
/// héros listés dans <see cref="ConfigGroupeHeros.NomsHeros"/> à former le
/// groupe automatiquement.
///
/// Skip en mode passif (cf. <see cref="Compte.ModePassif"/>) et hors session.
/// Anti double-envoi : ne réinvite pas un nom déjà membre du
/// <see cref="GroupeHeros"/>.
/// </summary>
public sealed class AutoInviteurHeros
{
    private readonly Compte _compte;
    private readonly ConfigGroupeHeros _config;
    private int _enCours; // flag atomique pour éviter les exécutions concurrentes

    public AutoInviteurHeros(Compte compte, ConfigGroupeHeros config)
    {
        _compte = compte ?? throw new ArgumentNullException(nameof(compte));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Lance la procédure d'invitation. No-op si :
    /// <list type="bullet">
    /// <item>session = null</item>
    /// <item>compte en mode passif</item>
    /// <item><c>AutoInvitationActive</c> = false</item>
    /// <item>aucun nom configuré</item>
    /// <item>une procédure tourne déjà</item>
    /// </list>
    /// </summary>
    public async Task LancerAsync(SessionProxy? session, CancellationToken ct = default)
    {
        if (session is null)
        {
            Journaliseur.Debogue("[GH-INVIT] skip : pas de session jeu active");
            return;
        }
        if (_compte.ModePassif)
        {
            Journaliseur.Info("[GH-INVIT] skip : mode passif activé");
            return;
        }
        if (!_config.AutoInvitationActive)
        {
            Journaliseur.Info("[GH-INVIT] skip : auto-invitation désactivée (cocher dans onglet Groupe ou éditer multi-account/<id>.json)");
            return;
        }
        if (_config.NomsHeros.Count == 0)
        {
            Journaliseur.Info("[GH-INVIT] skip : aucun nom configuré (éditer la liste dans l'onglet Groupe)");
            return;
        }
        if (Interlocked.Exchange(ref _enCours, 1) == 1)
        {
            Journaliseur.Debogue("[GH-INVIT] skip : procédure déjà en cours");
            return;
        }

        try
        {
            await Task.Delay(Math.Max(0, _config.DelaiInitialMs), ct).ConfigureAwait(false);
            Journaliseur.Info(
                $"[GH-INVIT] Auto-invitation activée — {_config.NomsHeros.Count} héros à inviter");

            foreach (var nom in _config.NomsHeros.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                ct.ThrowIfCancellationRequested();
                if (_compte.ModePassif) { Journaliseur.Info("[GH-INVIT] Mode passif activé en cours d'invitation → arrêt."); break; }
                if (DejaMembre(nom))
                {
                    Journaliseur.Debogue($"[GH-INVIT] « {nom} » déjà membre, skip.");
                    continue;
                }
                try
                {
                    await session.EnvoyerAuServeurAsync($"PI{nom}").ConfigureAwait(false);
                    Journaliseur.Info($"[GH-INVIT] PI{nom} envoyé");
                }
                catch (Exception ex)
                {
                    Journaliseur.Avertir($"[GH-INVIT] Échec invitation « {nom} » : {ex.Message}");
                }
                await Task.Delay(Math.Max(200, _config.DelaiEntreInvitsMs), ct).ConfigureAwait(false);
            }

            Journaliseur.Info("[GH-INVIT] Procédure d'invitation terminée");
        }
        catch (OperationCanceledException) { /* propagation OK */ }
        finally
        {
            Interlocked.Exchange(ref _enCours, 0);
        }
    }

    private bool DejaMembre(string nom)
    {
        var groupe = _compte.GroupeHeros;
        if (groupe is null) return false;
        foreach (var m in groupe.Membres)
            if (string.Equals(m.Nom, nom, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
