using System;
using System.IO;
using System.Text.Json;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Securite;

/// <summary>
/// Profil de délais aléatoires pour humaniser les actions du bot.
/// Tous les délais sont en millisecondes ; chaque action utilise un Random entre Min et Max.
///
/// Profils prédéfinis : Instantané (0ms), Rapide, Humain normal, Humain lent.
/// Source : SynFus Bot v1.1.7 onglet Combat → Délais.
/// </summary>
public sealed class ConfigDelais
{
    public string ProfilNom { get; set; } = "Humain normal";

    // Délais en combat
    public Plage ActionCombatGeneral { get; set; } = new(150, 400);
    public Plage CliquerPret { get; set; } = new(300, 800);
    public Plage LancerSort { get; set; } = new(100, 300);
    public Plage EntreDeuxSorts { get; set; } = new(150, 400);
    public Plage PasserLeTour { get; set; } = new(100, 250);
    public Plage ApresDeplacement { get; set; } = new(50, 150);
    public Plage CaptureDame { get; set; } = new(800, 1200);
    public Plage TimeoutSort { get; set; } = new(500, 800);
    public Plage TimeoutMouvement { get; set; } = new(1500, 2500);
    public Plage PlacementCombat { get; set; } = new(200, 400);
    public Plage MouvementTactique { get; set; } = new(800, 1200);
    public Plage MonstreVole { get; set; } = new(200, 500);

    // Délais hors combat
    public Plage DeplacementMap { get; set; } = new(100, 350);
    public Plage ChangementMap { get; set; } = new(200, 600);
    public Plage ApresFinCombat { get; set; } = new(300, 1000);
    public Plage EngagerCombat { get; set; } = new(200, 700);
    public Plage ReponsePnj { get; set; } = new(250, 600);

    private static readonly Random Rng = new();

    /// <summary>Retourne un délai aléatoire entre Min et Max (inclusifs).</summary>
    public static int Tirer(Plage plage)
        => Rng.Next(plage.Min, plage.Max + 1);

    public static ConfigDelais ProfilHumainNormal() => new();
    public static ConfigDelais ProfilHumainLent() => new()
    {
        ProfilNom = "Humain lent",
        ActionCombatGeneral = new(300, 800),
        LancerSort = new(200, 600),
        EntreDeuxSorts = new(300, 700),
        DeplacementMap = new(200, 500),
        ChangementMap = new(400, 1200),
    };
    public static ConfigDelais ProfilRapide() => new()
    {
        ProfilNom = "Rapide",
        ActionCombatGeneral = new(50, 150),
        LancerSort = new(30, 100),
        EntreDeuxSorts = new(50, 150),
    };
    public static ConfigDelais ProfilInstantane() => new()
    {
        ProfilNom = "Instantané",
        ActionCombatGeneral = new(0, 0),
        LancerSort = new(0, 0),
        EntreDeuxSorts = new(0, 0),
        DeplacementMap = new(0, 0),
        ChangementMap = new(0, 0),
    };

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static ConfigDelais Charger(string chemin)
    {
        if (!File.Exists(chemin)) return ProfilHumainNormal();
        try { return JsonSerializer.Deserialize<ConfigDelais>(File.ReadAllText(chemin), Options) ?? ProfilHumainNormal(); }
        catch (Exception ex) { Journaliseur.Avertir($"[CFG-DEL] {ex.Message}"); return ProfilHumainNormal(); }
    }

    public void Sauvegarder(string chemin)
    {
        var dir = Path.GetDirectoryName(chemin);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(chemin, JsonSerializer.Serialize(this, Options));
    }
}

public sealed record Plage(int Min, int Max);
