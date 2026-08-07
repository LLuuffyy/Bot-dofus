using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.MultiAccount;

/// <summary>
/// Configuration du groupe héros pour un compte master donné.
///
/// Persistée dans <c>multi-account/&lt;identifiantCompte&gt;.json</c>. Contient
/// la liste des noms de héros à inviter automatiquement à la connexion + le
/// toggle d'activation.
/// </summary>
public sealed class ConfigGroupeHeros
{
    private static readonly string Dossier = "multi-account";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    /// <summary>Activer l'auto-invitation à chaque connexion en jeu.</summary>
    public bool AutoInvitationActive { get; set; }

    /// <summary>Noms des héros à inviter (ordre = ordre d'envoi des PI).</summary>
    public List<string> NomsHeros { get; set; } = new();

    /// <summary>Délai entre 2 invitations PI consécutives (ms). Min 200 pour ne pas spammer.</summary>
    public int DelaiEntreInvitsMs { get; set; } = 800;

    /// <summary>Délai avant la 1ère invitation après connexion (ms).</summary>
    public int DelaiInitialMs { get; set; } = 3000;

    public static string CheminPour(string identifiantCompte)
        => Path.Combine(Dossier, $"{identifiantCompte}.json");

    public static ConfigGroupeHeros Charger(string identifiantCompte)
    {
        var fichier = CheminPour(identifiantCompte);
        if (!File.Exists(fichier)) return new ConfigGroupeHeros();
        try
        {
            var json = File.ReadAllText(fichier);
            return JsonSerializer.Deserialize<ConfigGroupeHeros>(json, Options)
                   ?? new ConfigGroupeHeros();
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[GH-CFG] Lecture {identifiantCompte} échec : {ex.Message}");
            return new ConfigGroupeHeros();
        }
    }

    public bool Sauvegarder(string identifiantCompte)
    {
        try
        {
            Directory.CreateDirectory(Dossier);
            var json = JsonSerializer.Serialize(this, Options);
            File.WriteAllText(CheminPour(identifiantCompte), json);
            return true;
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[GH-CFG] Écriture {identifiantCompte} échec : {ex.Message}");
            return false;
        }
    }
}
