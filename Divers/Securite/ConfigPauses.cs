using System;
using System.IO;
using System.Text.Json;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Securite;

/// <summary>
/// Pauses anti-détection : AFK aléatoire, pause longue (café/toilettes), variations de rythme,
/// horaires de jeu, durée max session, reconnexion auto.
/// Source : SynFus Bot v1.1.7 onglet Combat → Pauses.
/// </summary>
public sealed class ConfigPauses
{
    // AFK aléatoire
    public bool AfkAleatoireActive { get; set; } = false;
    public int AfkIntervalleMin { get; set; } = 15; // minutes entre AFK
    public int AfkDureeSecMin { get; set; } = 5;
    public int AfkDureeSecMax { get; set; } = 30;

    // Pause longue
    public bool PauseLongueActive { get; set; } = false;
    public int PauseLongueTousLesXCombats { get; set; } = 30;
    public int PauseLongueDureeMinutesMin { get; set; } = 5;
    public int PauseLongueDureeMinutesMax { get; set; } = 15;

    // Variations naturelles
    public bool FatigueProgressive { get; set; } = false;
    /// <summary>Pourcentage de ralentissement par 2 heures de jeu.</summary>
    public int RalentissementParDeuxHeures { get; set; } = 20;

    public bool PauseAleatoireSurCellule { get; set; } = false;
    public int ChanceParCellulePct { get; set; } = 10;

    // Horaires de jeu
    public bool HorairesActifs { get; set; } = false;
    public int HeureDebut { get; set; } = 8;
    public int HeureFin { get; set; } = 23;

    // Durée max session
    public bool LimiterDureeSession { get; set; } = false;
    public int DureeMaxHeures { get; set; } = 6;

    // Reconnexion auto
    public bool ReconnexionAutomatique { get; set; } = false;

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static ConfigPauses Charger(string chemin)
    {
        if (!File.Exists(chemin)) return new ConfigPauses();
        try { return JsonSerializer.Deserialize<ConfigPauses>(File.ReadAllText(chemin), Options) ?? new(); }
        catch (Exception ex) { Journaliseur.Avertir($"[CFG-PAUSE] {ex.Message}"); return new(); }
    }

    public void Sauvegarder(string chemin)
    {
        var dir = Path.GetDirectoryName(chemin);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(chemin, JsonSerializer.Serialize(this, Options));
    }

    /// <summary>True si on est dans la plage horaire autorisée.</summary>
    public bool EstDansHoraires()
    {
        if (!HorairesActifs) return true;
        int h = DateTime.Now.Hour;
        if (HeureDebut <= HeureFin) return h >= HeureDebut && h < HeureFin;
        return h >= HeureDebut || h < HeureFin;
    }
}
