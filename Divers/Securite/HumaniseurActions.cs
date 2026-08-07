using System;
using System.Threading;
using System.Threading.Tasks;

namespace BotDofus.Divers.Securite;

/// <summary>
/// Garde anti-burst pour l'injection autonome (Phase 3).
///
/// Quand le bot envoie des paquets tout seul (pas le client humain), le risque
/// #1 de détection serveur c'est la CADENCE : deux actions à &lt;50ms d'intervalle,
/// ou un rythme métronomique parfait. Un humain a du jitter naturel.
///
/// Cette classe garantit qu'entre deux actions du bot il y a TOUJOURS un délai
/// aléatoire tiré dans une <see cref="Plage"/>, et qu'on ne descend jamais sous
/// un plancher absolu. Thread-safe (un seul send à la fois sérialisé).
///
/// En mode passif / instantané, <see cref="Actif"/> = false → aucun délai
/// (le client réel gère le timing, on n'a rien à humaniser).
/// </summary>
public sealed class HumaniseurActions
{
    private readonly SemaphoreSlim _verrou = new(1, 1);
    private DateTime _derniereAction = DateTime.MinValue;
    private static readonly Random Rng = new();

    /// <summary>Plancher absolu : jamais deux actions bot sous ce délai (ms).</summary>
    public int PlancherMs { get; set; } = 60;

    /// <summary>Plage de délai humain appliquée entre deux actions (ms).</summary>
    public Plage Cadence { get; set; } = new(120, 380);

    /// <summary>Si false, aucun délai n'est appliqué (mode passif/instantané).</summary>
    public bool Actif { get; set; }

    /// <summary>Nombre total de fois où un délai anti-burst a été imposé (métrique UI).</summary>
    public long DelaisImposes { get; private set; }

    /// <summary>
    /// À appeler AVANT chaque envoi de paquet initié par le bot. Bloque le temps
    /// nécessaire pour que l'écart avec la dernière action soit humain.
    /// </summary>
    public async Task RespecterCadenceAsync(CancellationToken ct = default)
    {
        if (!Actif) return;

        await _verrou.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var ecoule = (DateTime.UtcNow - _derniereAction).TotalMilliseconds;
            var cible = Math.Max(PlancherMs, Rng.Next(Cadence.Min, Cadence.Max + 1));
            if (ecoule < cible)
            {
                var attente = (int)(cible - ecoule);
                DelaisImposes++;
                await Task.Delay(attente, ct).ConfigureAwait(false);
            }
            _derniereAction = DateTime.UtcNow;
        }
        finally
        {
            _verrou.Release();
        }
    }

    /// <summary>Réinitialise l'horodatage (ex. après une longue pause AFK).</summary>
    public void Reinitialiser() => _derniereAction = DateTime.MinValue;
}
