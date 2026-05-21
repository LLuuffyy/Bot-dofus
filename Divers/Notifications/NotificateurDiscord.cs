using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Notifications;

/// <summary>
/// Envoie des notifications à un webhook Discord pour les événements importants :
/// mort du perso, level up, banque pleine, abandon de combat, déconnexion inattendue.
///
/// Usage : <c>await NotificateurDiscord.NotifierAsync(webhookUrl, "🎉 Beiloddurul lvl 17 !");</c>
///
/// Webhook URL = config par perso ou globale (User Settings).
/// Format Discord webhook : POST JSON {"content": "<msg>"} → 204 No Content.
/// </summary>
public static class NotificateurDiscord
{
    // HttpClient statique réutilisable (recommandation Microsoft, évite
    // exhaustion sockets sur usage répété).
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };

    /// <summary>Envoie un message simple au webhook. Silencieux en cas d'erreur (log + swallow).</summary>
    public static async Task NotifierAsync(string webhookUrl, string message)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl) || string.IsNullOrWhiteSpace(message)) return;
        try
        {
            var payload = JsonSerializer.Serialize(new { content = message });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync(webhookUrl, content).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                Journaliseur.Avertir($"[DISCORD] Webhook HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            Journaliseur.Debogue($"[DISCORD] Erreur webhook : {ex.Message}");
        }
    }

    /// <summary>Notif mort avec emoji.</summary>
    public static Task NotifierMortAsync(string webhookUrl, string nomPerso, int niveau, string carteOuMap)
        => NotifierAsync(webhookUrl, $"💀 **{nomPerso}** (niv {niveau}) est mort sur la map {carteOuMap}.");

    /// <summary>Notif level up.</summary>
    public static Task NotifierLevelUpAsync(string webhookUrl, string nomPerso, int nouveauNiveau)
        => NotifierAsync(webhookUrl, $"🎉 **{nomPerso}** vient de passer **niveau {nouveauNiveau}** !");

    /// <summary>Notif banque pleine (poids).</summary>
    public static Task NotifierBanquePleineAsync(string webhookUrl, string nomPerso, int pourcentPoids)
        => NotifierAsync(webhookUrl, $"📦 **{nomPerso}** a {pourcentPoids}% de pods — départ vers la banque.");

    /// <summary>Notif déconnexion inattendue.</summary>
    public static Task NotifierDeconnexionAsync(string webhookUrl, string nomPerso, string raison)
        => NotifierAsync(webhookUrl, $"⚠️ **{nomPerso}** déconnecté : {raison}");

    /// <summary>Notif erreur critique (kick, anti-bot detecté).</summary>
    public static Task NotifierErreurCritiqueAsync(string webhookUrl, string nomPerso, string detail)
        => NotifierAsync(webhookUrl, $"🚨 **{nomPerso}** ERREUR CRITIQUE : {detail}");
}
