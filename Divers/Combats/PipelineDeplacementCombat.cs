using System;
using System.Threading;
using System.Threading.Tasks;

namespace BotDofus.Divers.Combats.IA;

/// <summary>
/// Issue d'un déplacement combat envoyé par l'IA, après attente du broadcast
/// serveur (cf. ADR-002 §3.3).
/// </summary>
public enum ResultatDeplacementCombat
{
    /// <summary>Broadcast <c>GA;0/1;&lt;monId&gt;</c> reçu avec la cellule
    /// d'arrivée attendue → mouvement intégralement accepté.</summary>
    Confirme,

    /// <summary>Broadcast reçu mais la cellule d'arrivée annoncée par le
    /// serveur est ≠ celle envoyée par le bot → le serveur a tronqué le
    /// chemin (cellule traversée occupée, PM réduits, etc.). Le cast peut
    /// rester possible si la dist réelle reste dans la portée.</summary>
    ConfirmePartiel,

    /// <summary>Timeout : aucun broadcast reçu dans le délai imparti. Le
    /// serveur a rejeté silencieusement le GA001 (cause possible : cell
    /// d'arrivée invalide, conflit action client en cours, etc.). Ne PAS
    /// envoyer GKK0 (= signature anti-bot d'ack sans action) ni le cast.</summary>
    TimeoutSilencieux,
}

/// <summary>
/// Pipeline event-based pour confirmer les déplacements combat envoyés par
/// l'IA. Remplace l'optimistic update aveugle (commit 67ac8bd) qui supposait
/// que le serveur acceptait toujours le GA001 — comportement faux observé
/// dans le log botdofus-20260520-203358 (cf. ADR-002 §1).
/// </summary>
/// <remarks>
/// Asymétrie protocolaire 1.29 : le serveur n'émet PAS de <c>GAS</c>/<c>GAF</c>
/// pour les déplacements combat (uniquement pour les casts PA-consumming).
/// Donc le SEUL signal métier disponible est le broadcast <c>GA;0/1;&lt;id&gt;;&lt;chemin&gt;</c>.
/// </remarks>
public static class PipelineDeplacementCombat
{
    /// <summary>
    /// Attend le broadcast <c>GA;0/1;&lt;idMoi&gt;</c> (via
    /// <see cref="Combat.MouvementBotConfirme"/>) ou un timeout. Retourne
    /// le résultat (<see cref="ResultatDeplacementCombat"/>).
    /// </summary>
    /// <param name="combat">État combat courant (source des events).</param>
    /// <param name="idMoi">Identifiant du combattant bot (= <see cref="Combat.IdentifiantAllie"/>).</param>
    /// <param name="cellAttendue">Cellule d'arrivée envoyée dans le GA001.</param>
    /// <param name="timeoutMs">Timeout en ms (recommandation ADR-002 : 2500 + marge nbPas).</param>
    /// <param name="ct">Token d'annulation (utilisé pour le Task.Delay du timeout).</param>
    /// <returns>
    /// <see cref="ResultatDeplacementCombat.Confirme"/> si la cellule reçue
    /// match exactement. <see cref="ResultatDeplacementCombat.ConfirmePartiel"/>
    /// si elle diffère (troncature serveur). <see cref="ResultatDeplacementCombat.TimeoutSilencieux"/>
    /// si rien n'arrive dans le délai.
    /// </returns>
    public static async Task<ResultatDeplacementCombat> AttendreMouvementOuTimeoutAsync(
        Combat combat,
        int idMoi,
        int cellAttendue,
        int timeoutMs,
        CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<(int cellAtteinte, bool exact)>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        void OnMv(object? s, MouvementBotArgs e)
        {
            if (e.IdActeur != idMoi) return;
            tcs.TrySetResult((e.CellArrivee, e.CellArrivee == cellAttendue));
        }
        combat.MouvementBotConfirme += OnMv;
        try
        {
            var tEvent = tcs.Task;
            var tTimeout = Task.Delay(timeoutMs, ct);
            var won = await Task.WhenAny(tEvent, tTimeout).ConfigureAwait(false);
            if (won == tTimeout)
                return ResultatDeplacementCombat.TimeoutSilencieux;
            var (_, exact) = tEvent.Result;
            return exact
                ? ResultatDeplacementCombat.Confirme
                : ResultatDeplacementCombat.ConfirmePartiel;
        }
        finally { combat.MouvementBotConfirme -= OnMv; }
    }
}
