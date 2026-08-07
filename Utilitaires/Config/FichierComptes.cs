using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Utilitaires.Config;

/// <summary>
/// Entrée décrivant un compte bot tel que persisté sur disque.
/// Les futures versions pourront être chiffrées au repos.
/// </summary>
public sealed class EntreeCompte
{
    public string Identifiant { get; set; } = string.Empty;
    public string MotDePasse { get; set; } = string.Empty;
    public int ServeurPrefere { get; set; }
    public int PersonnagePrefere { get; set; }
    public string Commentaire { get; set; } = string.Empty;
}

/// <summary>
/// Persistance JSON simple des comptes connus. À distinguer du fichier
/// <c>accounts.bot</c> hérité (binaire Hystoria) : ici on a un format lisible
/// et versionnable, propre au bot maison.
/// </summary>
public static class FichierComptes
{
    private const string NomFichierParDefaut = "comptes.json";

    public static string CheminParDefaut
        => Path.Combine(AppContext.BaseDirectory, NomFichierParDefaut);

    public static List<EntreeCompte> Charger(string? chemin = null)
    {
        var fichier = chemin ?? CheminParDefaut;
        if (!File.Exists(fichier)) return new List<EntreeCompte>();

        try
        {
            var contenu = File.ReadAllText(fichier);
            return JsonSerializer.Deserialize<List<EntreeCompte>>(contenu) ?? new();
        }
        catch (Exception ex)
        {
            Journaliseur.Erreur("Lecture du fichier comptes impossible", ex);
            return new List<EntreeCompte>();
        }
    }

    public static void Sauvegarder(List<EntreeCompte> comptes, string? chemin = null)
    {
        var fichier = chemin ?? CheminParDefaut;
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(fichier, JsonSerializer.Serialize(comptes, options));
            Journaliseur.Info($"{comptes.Count} comptes sauvegardés dans {fichier}");
        }
        catch (Exception ex)
        {
            Journaliseur.Erreur("Écriture du fichier comptes impossible", ex);
        }
    }
}
