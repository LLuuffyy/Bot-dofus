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
///
/// <para>
/// Refonte 2026-05-22 : le LEADER (master) envoie les invitations <b>une par une
/// et attend l'acceptation</b> (via <see cref="GroupeHeros.MembresChanges"/> qui
/// s'émet quand le serveur broadcast un <c>PM+&lt;id&gt;;&lt;nom&gt;;...</c>) avant
/// de passer à la suivante. Évite l'erreur <c>PIEa</c> côté serveur observée
/// quand on envoyait toutes les invitations en rafale (le serveur refuse les
/// nouvelles invits tant que la précédente est en cours / le groupe n'est pas stable).
/// </para>
/// </summary>
public sealed class AutoInviteurHeros
{
    /// <summary>Timeout d'attente d'acceptation par invité (ms). Au-delà, on log
    /// un avertissement et on passe au suivant.</summary>
    private const int TimeoutAcceptationMs = 4000;

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
                $"[GH-INVIT] Auto-invitation activée — {_config.NomsHeros.Count} héros à inviter (séquentiel)");

            int accept = 0, timeouts = 0;
            foreach (var nom in _config.NomsHeros.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                ct.ThrowIfCancellationRequested();
                if (_compte.ModePassif) { Journaliseur.Info("[GH-INVIT] Mode passif activé en cours d'invitation → arrêt."); break; }
                if (DejaMembre(nom))
                {
                    Journaliseur.Debogue($"[GH-INVIT] « {nom} » déjà membre, skip.");
                    continue;
                }

                // Envoi de l'invitation + attente de l'event MembresChanges
                // qui signifie que le serveur a broadcast PM+<id>;<nom>... =
                // l'invité a accepté et fait partie du groupe.
                try
                {
                    await session.EnvoyerAuServeurAsync($"PI{nom}").ConfigureAwait(false);
                    Journaliseur.Info($"[GH-INVIT] PI{nom} envoyé, attente acceptation (max {TimeoutAcceptationMs}ms)…");
                }
                catch (Exception ex)
                {
                    Journaliseur.Avertir($"[GH-INVIT] Échec invitation « {nom} » : {ex.Message}");
                    continue;
                }

                bool accepte = await AttendreMembreAjouteAsync(nom, TimeoutAcceptationMs, ct).ConfigureAwait(false);
                if (accepte)
                {
                    accept++;
                    Journaliseur.Info($"[GH-INVIT] ✓ « {nom} » a rejoint le groupe.");
                }
                else
                {
                    timeouts++;
                    Journaliseur.Avertir(
                        $"[GH-INVIT] ✗ « {nom} » : pas de PM+ dans les {TimeoutAcceptationMs}ms "
                        + "(probable refus serveur PIEa = perso offline / déjà dans un groupe / nom invalide). "
                        + "Passe au suivant.");
                }

                // Délai humain inter-invitations (court mais > 0 pour ne pas
                // empiler les PI au même instant côté serveur).
                int delai = Math.Max(200, _config.DelaiEntreInvitsMs);
                await Task.Delay(delai, ct).ConfigureAwait(false);
            }

            Journaliseur.Info(
                $"[GH-INVIT] Procédure terminée — {accept} accepté(s), {timeouts} timeout(s).");
        }
        catch (OperationCanceledException) { /* propagation OK */ }
        finally
        {
            Interlocked.Exchange(ref _enCours, 0);
        }
    }

    /// <summary>
    /// Attend que <paramref name="nomAttendu"/> apparaisse dans
    /// <see cref="GroupeHeros.Membres"/> (case-insensitive), ou qu'un
    /// <c>PI&lt;code&gt;</c> d'erreur arrive (= refus serveur, fast-fail), ou
    /// que <paramref name="timeoutMs"/> s'écoule.
    ///
    /// <para>
    /// Subtilité : <see cref="Compte.GroupeHeros"/> est lui-même <c>null</c> tant
    /// que le 1er <c>PM+</c> n'est pas arrivé (créé à la volée par
    /// <see cref="DetecteurModeHeros"/>). Donc on poll <c>_compte.GroupeHeros</c>
    /// toutes les 100 ms — sans ça l'attente échouait même quand le serveur
    /// répondait en 36 ms (forensic 2026-05-22 15:02:04 Vrottigrat).
    /// </para>
    /// <para>
    /// Fast-fail PIEa : on s'abonne à <see cref="Compte.InvitationRefusee"/>.
    /// Si on reçoit un PIEa pendant l'attente, on sort immédiatement (gain
    /// 4 s par perso refusé — utile sur Abrak où les 6 héros liés répondent
    /// systématiquement PIEa avant d'être activés par GTSX en combat).
    /// </para>
    /// </summary>
    private async Task<bool> AttendreMembreAjouteAsync(string nomAttendu, int timeoutMs, CancellationToken ct)
    {
        bool refusServeur = false;
        void OnRefus(object? s, string code) => refusServeur = true;
        _compte.InvitationRefusee += OnRefus;
        try
        {
            var t0 = Environment.TickCount;
            while (Environment.TickCount - t0 < timeoutMs)
            {
                ct.ThrowIfCancellationRequested();
                var groupe = _compte.GroupeHeros;
                if (groupe != null && EstDansGroupe(groupe, nomAttendu))
                    return true;
                if (refusServeur)
                {
                    Journaliseur.Debogue($"[GH-INVIT] Fast-fail : serveur a refusé l'invitation de « {nomAttendu} ».");
                    return false;
                }
                await Task.Delay(100, ct).ConfigureAwait(false);
            }
            return false;
        }
        finally
        {
            _compte.InvitationRefusee -= OnRefus;
        }
    }

    private static bool EstDansGroupe(GroupeHeros groupe, string nom)
    {
        foreach (var m in groupe.Membres)
            if (string.Equals(m.Nom, nom, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
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
