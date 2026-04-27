using System;
using System.IO;
using System.Text.Json;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Wpf;

public sealed class ConfigWpf
{
    private const string NomFichier = "config-wpf.json";

    public string CheminClientDofus { get; set; } = string.Empty;

    public static string CheminParDefaut => Path.Combine(AppContext.BaseDirectory, NomFichier);

    public static ConfigWpf Charger()
    {
        if (!File.Exists(CheminParDefaut))
        {
            return new ConfigWpf();
        }

        try
        {
            var json = File.ReadAllText(CheminParDefaut);
            return JsonSerializer.Deserialize<ConfigWpf>(json) ?? new ConfigWpf();
        }
        catch (Exception ex)
        {
            Journaliseur.Erreur("Lecture config WPF impossible", ex);
            return new ConfigWpf();
        }
    }

    public void Sauvegarder()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(CheminParDefaut, JsonSerializer.Serialize(this, options));
        }
        catch (Exception ex)
        {
            Journaliseur.Erreur("Ecriture config WPF impossible", ex);
        }
    }
}
