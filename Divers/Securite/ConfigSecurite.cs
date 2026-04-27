using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Securite;

/// <summary>
/// Configuration anti-modération. Quand le bot détecte un modérateur en ligne (via .staff),
/// il peut : s'arrêter complètement, ralentir, faire des pauses entre combats, ou juste alerter.
///
/// Source d'inspiration : SynFus Bot v1.1.7.
/// </summary>
public sealed class ConfigSecurite
{
    /// <summary>Activer le check périodique de .staff. Si false, aucune protection.</summary>
    public bool VerificationStaffActive { get; set; } = true;

    /// <summary>Intervalle min/max entre deux check .staff (en minutes, aléatoire).</summary>
    public int IntervalleCheckMinMin { get; set; } = 20;
    public int IntervalleCheckMinMax { get; set; } = 40;

    // ----- Réactions face à un modo détecté -----

    /// <summary>Option 1 : arrêter complètement le bot pendant une période aléatoire.</summary>
    public bool ArretCompletSiModo { get; set; } = false;
    public int PauseSiModoMin { get; set; } = 15;
    public int PauseSiModoMax { get; set; } = 30;

    /// <summary>Option 2a : ralentir TOUS les délais bot par un facteur (ex: 2.5x plus lent).</summary>
    public bool RalentirSiModo { get; set; } = false;
    /// <summary>Facteur en pourcentage (250 = ×2.5). 100 = pas de changement.</summary>
    public int FacteurRalentissement { get; set; } = 250;

    /// <summary>Option 2b : faire des pauses aléatoires entre combats.</summary>
    public bool PausesEntreCombatsSiModo { get; set; } = false;
    public int PauseCombatsXMin { get; set; } = 2;
    public int PauseCombatsXMax { get; set; } = 4;
    public int DureePauseSecMin { get; set; } = 60;
    public int DureePauseSecMax { get; set; } = 180;

    /// <summary>Option 3 : juste alerter (son + console rouge).</summary>
    public bool AlerteUniquementSiModo { get; set; } = false;

    // ----- Sérialisation -----
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static ConfigSecurite Charger(string chemin)
    {
        if (!File.Exists(chemin)) return new ConfigSecurite();
        try { return JsonSerializer.Deserialize<ConfigSecurite>(File.ReadAllText(chemin), Options) ?? new(); }
        catch (Exception ex) { Journaliseur.Avertir($"[CFG-SEC] {ex.Message}"); return new(); }
    }

    public void Sauvegarder(string chemin)
    {
        var dir = Path.GetDirectoryName(chemin);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(chemin, JsonSerializer.Serialize(this, Options));
    }
}
