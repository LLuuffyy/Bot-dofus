using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BotDofus.Commun.Messages.VersClient.Jeu;
using BotDofus.Commun.Reseau;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.MultiAccount;

/// <summary>
/// Active le mode héros Abrak Hystoria en envoyant la séquence officielle
/// du client Dofus 1.29 :
/// <list type="number">
///   <item><c>NOL</c> (C→S) — demande la liste des héros disponibles</item>
///   <item><c>NO&lt;flags&gt;~&lt;id1&gt;;&lt;etat&gt;|&lt;id2&gt;;&lt;etat&gt;|...</c> (S→C) — reçu, parsé via <see cref="MessageHerosOrdre"/></item>
///   <item><c>NS</c> (C→S) — optionnel, demande les détails (noms / comptes)</item>
///   <item><c>NA&lt;id1&gt;,&lt;id2&gt;,...,&lt;idN&gt;</c> (C→S) — ACTIVE tous les héros liés
///         (sauf master) dans le groupe héros. Au prochain combat, ils
///         apparaîtront comme combattants via <c>GTSX</c>.</item>
/// </list>
///
/// <para>
/// Différence vs <see cref="AutoInviteurHeros"/> qui envoyait <c>PI&lt;Nom&gt;</c>
/// (refusé par le serveur Abrak avec <c>PIEa</c>) : on utilise le mécanisme
/// natif du mode héros qui forme le groupe de 8 en UN seul paquet, sans timeout
/// ni risque de PIEa. Capture forensic 2026-05-22 15:22:34 dans le log.
/// </para>
/// </summary>
public sealed class ActivateurHerosAbrak
{
    private const int TimeoutListeMs = 4000;
    private const int DelaiInitialMs = 4000;

    private readonly Compte _compte;
    private int _enCours; // anti double-exécution LancerAsync
    private int _dejaActive; // anti double-activation depuis OnHerosOrdre

    private TaskCompletionSource<IReadOnlyList<int>>? _attenteListe;

    /// <summary>Référence à la session active, posée par <c>LancerAsync</c> et
    /// utilisée par <see cref="OnHerosOrdre"/> pour enchaîner la séquence
    /// quand c'est le client Dofus lui-même qui a envoyé <c>NOL</c>.</summary>
    private SessionProxy? _sessionActive;

    public ActivateurHerosAbrak(Compte compte)
    {
        _compte = compte ?? throw new ArgumentNullException(nameof(compte));
    }

    /// <summary>Pose la session active pour permettre l'activation réactive
    /// sur réception d'un NO non-sollicité (cas où le client Dofus envoie son
    /// propre NOL ~25s après la connexion).</summary>
    public void AttacherSession(SessionProxy? session) => _sessionActive = session;

    /// <summary>
    /// Appelé par <c>TrameJeu</c> à chaque <see cref="MessageHerosOrdre"/> reçu.
    /// Deux cas :
    /// <list type="bullet">
    ///   <item>Une activation en cours via <see cref="LancerAsync"/> attendait
    ///         la liste → on résout son TaskCompletionSource.</item>
    ///   <item>Sinon (cas observé Hystoria : client Dofus envoie NOL spontané),
    ///         on enchaîne directement NS → PV → NA depuis ici, une seule fois
    ///         par session.</item>
    /// </list>
    /// </summary>
    public void OnHerosOrdre(MessageHerosOrdre msg)
    {
        var liste = msg.Entrees
            .Where(e => !e.EstMaster && e.IdPerso > 0)
            .Select(e => e.IdPerso)
            .ToList();
        Journaliseur.Info(
            $"[ACTIV-HEROS] NO reçu : {msg.Entrees.Count} entrées, "
            + $"{liste.Count} héros lié(s) hors master.");

        if (_attenteListe is not null)
        {
            _attenteListe.TrySetResult(liste);
            return;
        }

        // Réactif : NOL envoyé par le client Dofus, pas par nous. On enchaîne
        // NS → PV → NA pour finaliser l'activation. Idempotent via _dejaActive.
        if (Interlocked.Exchange(ref _dejaActive, 1) == 1)
        {
            Journaliseur.Debogue("[ACTIV-HEROS] NO reçu mais déjà activé ce cycle, skip.");
            return;
        }
        if (liste.Count == 0)
        {
            Journaliseur.Info("[ACTIV-HEROS] NO sans héros lié, rien à activer.");
            return;
        }
        var session = _sessionActive;
        if (session is null)
        {
            Journaliseur.Avertir("[ACTIV-HEROS] NO reçu mais pas de session attachée, skip activation auto.");
            return;
        }
        _ = ExecuterSequenceFinaleAsync(session, liste, default);
    }

    /// <summary>
    /// Séquence finale NS → PV → NA. Appelée soit par <see cref="LancerAsync"/>
    /// après NOL/NO, soit par <see cref="OnHerosOrdre"/> si NOL spontané du
    /// client.
    /// </summary>
    private static async Task ExecuterSequenceFinaleAsync(
        SessionProxy session, IReadOnlyList<int> ids, CancellationToken ct)
    {
        // 1. NS — demande les détails (réponse NSK<id>;<login>;<perso>;...).
        await Task.Delay(300, ct).ConfigureAwait(false);
        try { await session.EnvoyerAuServeurAsync("NS").ConfigureAwait(false); Journaliseur.Info("[ACTIV-HEROS] NS envoyé"); }
        catch { /* facultatif */ }

        // 2. PV — Party leaVe : QUITTE le sous-groupe virtuel serveur.
        await Task.Delay(500, ct).ConfigureAwait(false);
        try
        {
            await session.EnvoyerAuServeurAsync("PV").ConfigureAwait(false);
            Journaliseur.Info("[ACTIV-HEROS] PV envoyé (quitte sous-groupe virtuel)");
        }
        catch (Exception ex) { Journaliseur.Avertir($"[ACTIV-HEROS] Échec PV : {ex.Message}"); }

        // 3. NA<id1>,<id2>,... — ACTIVE tous les héros.
        await Task.Delay(500, ct).ConfigureAwait(false);
        var payload = string.Join(",", ids);
        try
        {
            await session.EnvoyerAuServeurAsync($"NA{payload}").ConfigureAwait(false);
            Journaliseur.Info(
                $"[ACTIV-HEROS] ✓ NA{payload} envoyé — {ids.Count} héros activés.");
        }
        catch (Exception ex) { Journaliseur.Avertir($"[ACTIV-HEROS] Échec NA : {ex.Message}"); }
    }

    /// <summary>
    /// Lance la séquence complète <c>NOL → NA&lt;ids&gt;</c>. No-op si :
    /// <list type="bullet">
    ///   <item>session = null</item>
    ///   <item>compte en mode passif</item>
    ///   <item>déjà en cours</item>
    /// </list>
    /// </summary>
    public async Task LancerAsync(SessionProxy? session, CancellationToken ct = default)
    {
        if (session is null)
        {
            Journaliseur.Debogue("[ACTIV-HEROS] skip : pas de session jeu active");
            return;
        }
        if (_compte.ModePassif)
        {
            Journaliseur.Info("[ACTIV-HEROS] skip : mode passif activé");
            return;
        }
        if (Interlocked.Exchange(ref _enCours, 1) == 1)
        {
            Journaliseur.Debogue("[ACTIV-HEROS] skip : procédure déjà en cours");
            return;
        }

        // Pose la session ici même : si le client Dofus envoie son propre NOL
        // pendant qu'on attend le délai initial, OnHerosOrdre saura quoi faire.
        AttacherSession(session);

        try
        {
            Journaliseur.Info($"[ACTIV-HEROS] Démarrage (délai initial {DelaiInitialMs}ms)…");
            await Task.Delay(DelaiInitialMs, ct).ConfigureAwait(false);

            // Si OnHerosOrdre a déjà fait le travail (client Dofus a envoyé NOL
            // spontanément pendant l'attente), on n'envoie pas un 2e NOL.
            if (System.Threading.Volatile.Read(ref _dejaActive) == 1)
            {
                Journaliseur.Info("[ACTIV-HEROS] Activation déjà déclenchée par NO réactif, skip NOL proactif.");
                return;
            }

            Journaliseur.Info("[ACTIV-HEROS] Séquence Hystoria proactive : NOL → NS → PV → NA<ids>");
            _attenteListe = new TaskCompletionSource<IReadOnlyList<int>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            try
            {
                await session.EnvoyerAuServeurAsync("NOL").ConfigureAwait(false);
                Journaliseur.Info("[ACTIV-HEROS] NOL envoyé, attente NO<flags>~...");
            }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[ACTIV-HEROS] Échec envoi NOL : {ex.Message}");
                return;
            }

            // Attente NO<flags>~... (timeout 4s).
            var tListe = _attenteListe.Task;
            var tTimeout = Task.Delay(TimeoutListeMs, ct);
            var gagnant = await Task.WhenAny(tListe, tTimeout).ConfigureAwait(false);
            if (gagnant != tListe)
            {
                Journaliseur.Avertir($"[ACTIV-HEROS] Timeout {TimeoutListeMs}ms : pas de NO reçu.");
                return;
            }
            var ids = tListe.Result;
            if (ids.Count == 0)
            {
                Journaliseur.Info("[ACTIV-HEROS] Aucun héros lié disponible sur le compte.");
                return;
            }
            // Marque l'activation comme déclenchée pour éviter un re-trigger via OnHerosOrdre.
            Interlocked.Exchange(ref _dejaActive, 1);
            await ExecuterSequenceFinaleAsync(session, ids, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { /* propagation OK */ }
        finally
        {
            _attenteListe = null;
            Interlocked.Exchange(ref _enCours, 0);
        }
    }
}
