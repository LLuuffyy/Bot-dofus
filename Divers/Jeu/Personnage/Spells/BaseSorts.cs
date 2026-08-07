using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        // 2bis) STATS PAR NIVEAU depuis hechizos_dyshay.xml (modèle dyshay/SynFus).
        // Le JSON hystoria n'a qu'UNE entrée par sort (= stats niv 1) → tous les
        // sorts à niv >= 2 voyaient leurs PA/portée mal interprétés (cf user
        // Ronce niv 5 : JSON 1-6 / réalité 1-8 PA 4). dyshay/Bot-Dofus-Retro
        // fournit la table complète niv 1..6 en XML, on la charge ici.
        var fichierXml = Path.Combine(dossier, "hechizos_dyshay.xml");
        if (File.Exists(fichierXml))
        {
            try
            {
                int nbSortsDyshay = 0, nbStatsDyshay = 0;
                var doc = System.Xml.Linq.XDocument.Load(fichierXml);
                foreach (var sortXml in doc.Descendants("HECHIZO"))
                {
                    int id = (int?)sortXml.Attribute("ID") ?? 0;
                    if (id <= 0) continue;
                    if (!bdd.Sorts.TryGetValue(id, out var info))
                    {
                        info = new InfoSort
                        {
                            Identifiant = id,
                            Nom = (string?)sortXml.Element("NOMBRE") ?? ""
                        };
                        bdd.Sorts[id] = info;
                    }
                    // dyshay utilise &lt;NIVEL ...&gt;, SynFus aussi.
                    foreach (var nv in sortXml.Elements("NIVEL"))
                    {
                        int niveau = (int?)nv.Attribute("NIVEL") ?? 0;
                        if (niveau <= 0) continue;
                        var sn = new StatsNiveau
                        {
                            Niveau = niveau,
                            CoutPA = (int?)nv.Attribute("COSTE_PA") ?? 0,
                            PorteeMin = (int?)nv.Attribute("RANGO_MINIMO") ?? 0,
                            PorteeMax = (int?)nv.Attribute("RANGO_MAXIMO") ?? 0,
                            LigneSeule = ((string?)nv.Attribute("LANZ_EN_LINEA") ?? "FALSE").Equals("TRUE", StringComparison.OrdinalIgnoreCase),
                            NecessiteLOS = ((string?)nv.Attribute("NECESITA_VISION") ?? "FALSE").Equals("TRUE", StringComparison.OrdinalIgnoreCase),
                            CelluleVide = ((string?)nv.Attribute("NECESITA_CELDA_LIBRE") ?? "FALSE").Equals("TRUE", StringComparison.OrdinalIgnoreCase),
                            PorteeModifiable = ((string?)nv.Attribute("RANGO_MODIFICABLE") ?? "FALSE").Equals("TRUE", StringComparison.OrdinalIgnoreCase),
                            MaxParTour = (int?)nv.Attribute("MAX_LANZ_POR_TURNO") ?? 0,
                            MaxParCible = (int?)nv.Attribute("MAX_LANZ_POR_OBJETIVO") ?? 0,
                            Cooldown = (int?)nv.Attribute("COOLDOWN") ?? 0
                        };
                        info.StatsParNiveau[niveau] = sn;
                        nbStatsDyshay++;
                    }
                    nbSortsDyshay++;
                }
                Journaliseur.Info($"[SORTS] dyshay XML : {nbSortsDyshay} sorts, {nbStatsDyshay} entrées de stats par niveau.");
            }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[SORTS] Erreur lecture hechizos_dyshay.xml : {ex.Message}");
            }
        }
        else
        {
            Journaliseur.Avertir($"[SORTS] Fichier dyshay introuvable : {fichierXml} — stats par niveau indisponibles, fallback sur niv 1 JSON.");
        }

        // 3) Classification (catégorie) à partir du nom + description FR.
        // Heuristique mots-clés Dofus Retro — l'ordre des tests EST la
        // priorité de désambiguïsation (Invocation avant Soin avant Offensif…).
        int nbOff = 0;
        foreach (var s in bdd.Sorts.Values)
        {
            s.Categorie = ClasserSort(s.Nom, s.Description);
            if (s.Categorie == CategorieSort.Offensif) nbOff++;
        }
        Journaliseur.Info($"[SORTS] Classification : {nbOff} offensifs / {bdd.Sorts.Count} sorts.");

        return bdd;
    }

    private static CategorieSort ClasserSort(string nom, string desc)
    {
        string t = ((nom ?? "") + " || " + (desc ?? "")).ToLowerInvariant();
        if (t.Length == 0) return CategorieSort.Utilitaire;

        bool A(params string[] mots) => mots.Any(m => t.Contains(m));

        // 1) Invocations (mot très discriminant)
        if (A("invocation de", "invoque", "invocation d'", "invocation d\\'"))
            return CategorieSort.Invocation;

        // 2) Soin (rendre des PDV à un allié/soi)
        if (A("rend des points de vie", "pdv rendus", "soigne", "soin ", "rend des pdv",
               "régénère", "regenere", "points de vie rendus"))
            return CategorieSort.Soin;

        // 3) Offensif (dommages / vol de vie)
        if (A("dommage", "dégât", "degat", "vole des points de vie", "vol de vie",
               "occasionne des", "retire des points de vie", "perte de pdv",
               "pdv (fixe)", "explose"))
            return CategorieSort.Offensif;

        // 4) Debuff (retire ressources / affaiblit l'ennemi)
        if (A("retire des pa", "retire des pm", "vole #", "vole des pa", "vole des pm",
               "réduit", "reduit", "affaibli", "malédiction", "malediction",
               "diminue", "envoûte", "envoute", "poison", "immobilis", "entrave"))
            return CategorieSort.Debuff;

        // 5) Buff (armure / bonus / protection)
        if (A("armure", "bonus", "augmente", "ajoute", "protège", "protege",
               "dopage", "résistance", "resistance", "renforce", "boost",
               "bouclier", "force ", "intelligence", "agilité", "chance"))
            return CategorieSort.Buff;

        // 6) Déplacement / positionnement
        if (A("téléporte", "teleporte", "fait reculer", "fait avancer", "attire",
               "saut", "bond", "échange les places", "echange les places", "recul"))
            return CategorieSort.Deplacement;

        return CategorieSort.Utilitaire;
    }

    /// <summary>
    /// Sorts OFFENSIFS appris par le perso, triés du « meilleur attaquant »
    /// au moins bon (heuristique : grande portée d'abord, puis PA décroissant
    /// = sort le plus fort). Base d'une config de combat auto.
    /// </summary>
    public IReadOnlyList<InfoSort> SortsOffensifs(IEnumerable<int> idsAppris)
        => idsAppris
            .Select(Trouver)
            .Where(s => s is { Categorie: CategorieSort.Offensif })
            .Select(s => s!)
            .OrderByDescending(s => s.PorteeMax)
            .ThenByDescending(s => s.CoutPA)
            .ToList();
}

/// <summary>Catégorie fonctionnelle d'un sort (déduite nom+description).</summary>
public enum CategorieSort
{
    Offensif,
    Soin,
    Buff,
    Debuff,
    Invocation,
    Deplacement,
    Utilitaire
}

/// <summary>
/// Stats d'un sort à un niveau précis (1-6 Dofus Retro). Mappées sur le format
/// dyshay/SynFus (XML hechizos.xml). Ronce niv 5 : CoutPA=4, Portée 1-8 ; au
/// niv 6 (max), CoutPA descend à 3. Sans cette granularité, l'IA filtre les
/// sorts avec les stats du niv 1 et rejette à tort.
/// </summary>
public sealed class StatsNiveau
{
    public int Niveau { get; set; }
    public int CoutPA { get; set; }
    public int PorteeMin { get; set; }
    public int PorteeMax { get; set; }
    public bool LigneSeule { get; set; }
    public bool NecessiteLOS { get; set; }
    public bool CelluleVide { get; set; }
    public bool PorteeModifiable { get; set; }
    public int MaxParTour { get; set; }
    public int MaxParCible { get; set; }
    public int Cooldown { get; set; }
}

/// <summary>Métadonnées d'un sort Hystoria.</summary>
public sealed class InfoSort
{
    public int Identifiant { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Champs « legacy » conservés pour compat — correspondent aux stats du
    // NIVEAU 1 (notre ancien JSON les renseignait toujours pour le niv 1).
    public int CoutPA { get; set; }
    public int PorteeMin { get; set; }
    public int PorteeMax { get; set; }
    public bool LigneSeule { get; set; }
    public bool NecessiteLOS { get; set; }
    public bool CelluleVide { get; set; }
    public bool PorteeModifiable { get; set; }
    public int MaxParTour { get; set; }
    public int Cooldown { get; set; }

    /// <summary>
    /// Stats par niveau (1-6) extraites de hechizos_dyshay.xml. Si vide → ne
    /// pas se fier (le sort est inconnu du XML dyshay, on tombe sur les champs
    /// « legacy » ci-dessus = niv 1).
    /// </summary>
    public Dictionary<int, StatsNiveau> StatsParNiveau { get; } = new();

    /// <summary>
    /// Renvoie les stats du niveau appris, ou — à défaut — le niv le plus haut
    /// disponible &lt;= niveau, ou n'importe quel niveau si rien ne correspond.
    /// </summary>
    public StatsNiveau? Stats(int niveau)
    {
        if (StatsParNiveau.Count == 0) return null;
        if (StatsParNiveau.TryGetValue(niveau, out var exact)) return exact;
        // Fallback : prendre le plus haut niveau &lt;= demandé
        StatsNiveau? meilleur = null;
        foreach (var kv in StatsParNiveau)
        {
            if (kv.Key > niveau) continue;
            if (meilleur == null || kv.Key > meilleur.Niveau) meilleur = kv.Value;
        }
        return meilleur ?? StatsParNiveau.Values.First();
    }

    /// <summary>Catégorie déduite (Offensif/Soin/Buff/…), calculée au chargement.</summary>
    public CategorieSort Categorie { get; set; } = CategorieSort.Utilitaire;

    public override string ToString() => $"#{Identifiant} {Nom} ({Categorie}, PA={CoutPA}, range={PorteeMin}-{PorteeMax}, {StatsParNiveau.Count} niv)";
}
