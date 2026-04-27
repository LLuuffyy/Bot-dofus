using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Jeu.Personnage.Spells;

/// <summary>
/// Base de données des sorts du serveur Hystoria, chargée une fois au démarrage
/// depuis <c>Resources/data/spells_*.json</c> (extraite du bot SynFus).
///
/// Trois sources :
/// - <c>spells_hystoria.json</c> : id → "nom" (mapping minimal)
/// - <c>spells_fr.json</c> : id → {n, d} (nom + description FR)
/// - <c>spells_stats_hystoria.json</c> : id → {n, pa, rmin, rmax, line, vision, empty, modrange, maxpt, cd}
///
/// Singleton via <see cref="Instance"/> car la base est immuable et partagée.
/// </summary>
public sealed class BaseSorts
{
    private static BaseSorts? _instance;
    public static BaseSorts Instance => _instance ??= Charger();

    public Dictionary<int, InfoSort> Sorts { get; } = new();

    public InfoSort? Trouver(int id) => Sorts.TryGetValue(id, out var s) ? s : null;

    /// <summary>Liste tous les sorts triés par ID croissant — utile pour l'UI.</summary>
    public IEnumerable<InfoSort> Tous => Sorts.Values;

    /// <summary>Recherche par nom (partiel, insensitive case).</summary>
    public IEnumerable<InfoSort> Chercher(string motCle)
    {
        if (string.IsNullOrWhiteSpace(motCle)) return Tous;
        return Sorts.Values.Where(s =>
            s.Nom.Contains(motCle, StringComparison.OrdinalIgnoreCase)
            || s.Identifiant.ToString() == motCle);
    }

    private static BaseSorts Charger()
    {
        var bdd = new BaseSorts();
        var racine = AppContext.BaseDirectory;
        var dossier = Path.Combine(racine, "Resources", "data");

        // 1) Stats complètes (priorité car contient le plus d'info)
        var fichierStats = Path.Combine(dossier, "spells_stats_hystoria.json");
        if (File.Exists(fichierStats))
        {
            try
            {
                var json = File.ReadAllText(fichierStats);
                var doc = JsonDocument.Parse(json);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (!int.TryParse(prop.Name, out var id)) continue;
                    var info = new InfoSort { Identifiant = id };
                    var v = prop.Value;
                    if (v.TryGetProperty("n", out var n)) info.Nom = n.GetString() ?? "";
                    if (v.TryGetProperty("pa", out var pa)) info.CoutPA = pa.GetInt32();
                    if (v.TryGetProperty("rmin", out var rmin)) info.PorteeMin = rmin.GetInt32();
                    if (v.TryGetProperty("rmax", out var rmax)) info.PorteeMax = rmax.GetInt32();
                    if (v.TryGetProperty("line", out var line)) info.LigneSeule = line.GetBoolean();
                    if (v.TryGetProperty("vision", out var vis)) info.NecessiteLOS = vis.GetBoolean();
                    if (v.TryGetProperty("empty", out var emp)) info.CelluleVide = emp.GetBoolean();
                    if (v.TryGetProperty("modrange", out var mr)) info.PorteeModifiable = mr.GetBoolean();
                    if (v.TryGetProperty("maxpt", out var mp)) info.MaxParTour = mp.GetInt32();
                    if (v.TryGetProperty("cd", out var cd)) info.Cooldown = cd.GetInt32();
                    bdd.Sorts[id] = info;
                }
                Journaliseur.Info($"[SORTS] {bdd.Sorts.Count} sorts chargés depuis spells_stats_hystoria.json");
            }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[SORTS] Erreur lecture stats : {ex.Message}");
            }
        }
        else
        {
            Journaliseur.Avertir($"[SORTS] Fichier introuvable : {fichierStats}");
        }

        // 2) Descriptions FR (complète si pas déjà rempli)
        var fichierFr = Path.Combine(dossier, "spells_fr.json");
        if (File.Exists(fichierFr))
        {
            try
            {
                var json = File.ReadAllText(fichierFr);
                var doc = JsonDocument.Parse(json);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (!int.TryParse(prop.Name, out var id)) continue;
                    var v = prop.Value;
                    if (!bdd.Sorts.TryGetValue(id, out var info))
                    {
                        info = new InfoSort { Identifiant = id };
                        bdd.Sorts[id] = info;
                    }
                    if (string.IsNullOrEmpty(info.Nom) && v.TryGetProperty("n", out var n))
                        info.Nom = n.GetString() ?? "";
                    if (v.TryGetProperty("d", out var d))
                        info.Description = d.GetString() ?? "";
                }
            }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[SORTS] Erreur lecture FR : {ex.Message}");
            }
        }

        return bdd;
    }
}

/// <summary>Métadonnées d'un sort Hystoria.</summary>
public sealed class InfoSort
{
    public int Identifiant { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CoutPA { get; set; }
    public int PorteeMin { get; set; }
    public int PorteeMax { get; set; }
    public bool LigneSeule { get; set; }
    public bool NecessiteLOS { get; set; }
    public bool CelluleVide { get; set; }
    public bool PorteeModifiable { get; set; }
    public int MaxParTour { get; set; }
    public int Cooldown { get; set; }

    public override string ToString() => $"#{Identifiant} {Nom} (PA={CoutPA}, range={PorteeMin}-{PorteeMax})";
}
