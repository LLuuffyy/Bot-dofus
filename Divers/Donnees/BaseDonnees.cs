using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Donnees;

/// <summary>
/// Base de données complète Hystoria : items, monstres, NPCs, maps, skills,
/// objets interactifs, dialogues. Singleton lazy-loaded au premier accès.
/// Source : extraits du bot SynFus Hystoria v1.1.7 (Resources/data/*.json).
/// </summary>
public sealed class BaseDonnees
{
    private static BaseDonnees? _instance;
    public static BaseDonnees Instance => _instance ??= Charger();

    public Dictionary<int, InfoItem> Items { get; } = new();
    public Dictionary<int, InfoMonstre> Monstres { get; } = new();
    public Dictionary<int, InfoNpc> Npcs { get; } = new();
    public Dictionary<int, InfoMap> Maps { get; } = new();
    public Dictionary<int, string> Skills { get; } = new();
    public Dictionary<int, InfoInteractif> Interactifs { get; } = new();
    public Dictionary<int, string> Dialogues { get; } = new();

    public InfoItem? Item(int id) => Items.TryGetValue(id, out var v) ? v : null;
    public InfoMonstre? Monstre(int id) => Monstres.TryGetValue(id, out var v) ? v : null;
    public InfoNpc? Npc(int id) => Npcs.TryGetValue(id, out var v) ? v : null;
    public InfoMap? Map(int id) => Maps.TryGetValue(id, out var v) ? v : null;
    public string? Skill(int id) => Skills.TryGetValue(id, out var v) ? v : null;

    public IEnumerable<InfoItem> ChercherItem(string motCle)
        => Items.Values.Where(i => i.Nom.Contains(motCle, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<InfoMonstre> ChercherMonstre(string motCle)
        => Monstres.Values.Where(m => m.Nom.Contains(motCle, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<InfoNpc> ChercherNpc(string motCle)
        => Npcs.Values.Where(n => n.Nom.Contains(motCle, StringComparison.OrdinalIgnoreCase));

    private static BaseDonnees Charger()
    {
        var bdd = new BaseDonnees();
        var dossier = Path.Combine(AppContext.BaseDirectory, "Resources", "data");

        ChargerItems(bdd, dossier);
        ChargerMonstres(bdd, dossier);
        ChargerNpcs(bdd, dossier);
        ChargerMaps(bdd, dossier);
        ChargerSkills(bdd, dossier);
        ChargerInteractifs(bdd, dossier);
        ChargerDialogues(bdd, dossier);

        Journaliseur.Info($"[BDD] Chargé : {bdd.Items.Count} items, {bdd.Monstres.Count} monstres, "
                       + $"{bdd.Npcs.Count} npcs, {bdd.Maps.Count} maps, {bdd.Skills.Count} skills, "
                       + $"{bdd.Interactifs.Count} interactifs");
        return bdd;
    }

    private static void ChargerItems(BaseDonnees bdd, string dossier)
    {
        // items_merged.json : { "id": { "n": "Nom", "d": "Description", "t": typeId, "l": niveau, "w": poids, ... } }
        // Attention : certains champs ("l", "wd", "g") sont parfois des nombres, parfois des objets selon
        // l'item. JsonElement.TryGetInt32 THROWS si la value n'est PAS un Number — il faut checker ValueKind
        // avant. Sans ça, item 3 (où "l":{"entry":{}}) cassait la boucle et seuls 9 items étaient chargés.
        var fichier = Path.Combine(dossier, "items_merged.json");
        if (!File.Exists(fichier)) fichier = Path.Combine(dossier, "items_hystoria.json");
        if (!File.Exists(fichier)) return;
        try
        {
            var doc = JsonDocument.Parse(File.ReadAllText(fichier));
            int total = 0, errs = 0;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                try
                {
                    if (!int.TryParse(prop.Name, out var id)) continue;
                    var v = prop.Value;
                    var item = new InfoItem { Identifiant = id };
                    if (v.ValueKind == JsonValueKind.Object)
                    {
                        if (v.TryGetProperty("n", out var n) && n.ValueKind == JsonValueKind.String)
                            item.Nom = n.GetString() ?? "";
                        if (v.TryGetProperty("d", out var d) && d.ValueKind == JsonValueKind.String)
                            item.Description = d.GetString() ?? "";
                        item.IdType = LireIntSur(v, "t");
                        item.Niveau = LireIntSur(v, "l", "lvl");
                        item.Poids = LireIntSur(v, "w");
                        item.IdGfx = LireIntSur(v, "g", "gfx");
                    }
                    else if (v.ValueKind == JsonValueKind.String)
                    {
                        item.Nom = v.GetString() ?? "";
                    }
                    bdd.Items[id] = item;
                    total++;
                }
                catch { errs++; /* item individuel mal formé : on saute, on n'arrête pas tout */ }
            }
            if (errs > 0) Journaliseur.Avertir($"[BDD] items : {total} chargés, {errs} ignorés (champ malformé)");
        }
        catch (Exception ex) { Journaliseur.Avertir($"[BDD] items : {ex.Message}"); }
    }

    /// <summary>Lit un int depuis un JsonElement, peu importe le type effectif (number / objet / null).
    /// Retourne 0 si absent ou non-numérique. Évite les InvalidOperationException de TryGetInt32.</summary>
    private static int LireIntSur(JsonElement parent, params string[] nomsCandidats)
    {
        foreach (var nom in nomsCandidats)
        {
            if (!parent.TryGetProperty(nom, out var v)) continue;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)) return i;
            // Sinon (objet, array, etc.) on ignore et on essaie le candidat suivant.
        }
        return 0;
    }

    private static void ChargerMonstres(BaseDonnees bdd, string dossier)
    {
        // monsters_hystoria.json + monsters_fr.json + monsters_hystoria_stats.json
        var fStats = Path.Combine(dossier, "monsters_hystoria_stats.json");
        if (File.Exists(fStats))
        {
            try
            {
                var doc = JsonDocument.Parse(File.ReadAllText(fStats));
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    try
                    {
                        if (!int.TryParse(prop.Name, out var id)) continue;
                        var v = prop.Value;
                        var mob = new InfoMonstre { Identifiant = id };
                        if (v.TryGetProperty("n", out var n) && n.ValueKind == JsonValueKind.String)
                            mob.Nom = n.GetString() ?? "";
                        mob.Niveau = LireIntSur(v, "lvl", "l");
                        mob.PointsVie = LireIntSur(v, "hp");
                        bdd.Monstres[id] = mob;
                    }
                    catch { /* skip malformed */ }
                }
            }
            catch (Exception ex) { Journaliseur.Avertir($"[BDD] monsters_stats : {ex.Message}"); }
        }
        var fNoms = Path.Combine(dossier, "monsters_fr.json");
        if (File.Exists(fNoms))
        {
            try
            {
                var doc = JsonDocument.Parse(File.ReadAllText(fNoms));
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (!int.TryParse(prop.Name, out var id)) continue;
                    var v = prop.Value;
                    if (!bdd.Monstres.TryGetValue(id, out var mob))
                    {
                        mob = new InfoMonstre { Identifiant = id };
                        bdd.Monstres[id] = mob;
                    }
                    if (v.ValueKind == JsonValueKind.Object && v.TryGetProperty("n", out var n))
                        mob.Nom = n.GetString() ?? mob.Nom;
                    else if (v.ValueKind == JsonValueKind.String)
                        mob.Nom = v.GetString() ?? mob.Nom;
                }
            }
            catch (Exception ex) { Journaliseur.Avertir($"[BDD] monsters_fr : {ex.Message}"); }
        }
    }

    private static void ChargerNpcs(BaseDonnees bdd, string dossier)
    {
        var fStats = Path.Combine(dossier, "npcs_hystoria.json");
        var fNoms = Path.Combine(dossier, "npcs_fr.json");
        foreach (var fichier in new[] { fStats, fNoms })
        {
            if (!File.Exists(fichier)) continue;
            try
            {
                var doc = JsonDocument.Parse(File.ReadAllText(fichier));
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (!int.TryParse(prop.Name, out var id)) continue;
                    var v = prop.Value;
                    if (!bdd.Npcs.TryGetValue(id, out var npc))
                    {
                        npc = new InfoNpc { Identifiant = id };
                        bdd.Npcs[id] = npc;
                    }
                    if (v.ValueKind == JsonValueKind.Object && v.TryGetProperty("n", out var n))
                        npc.Nom = n.GetString() ?? npc.Nom;
                    else if (v.ValueKind == JsonValueKind.String)
                        npc.Nom = v.GetString() ?? npc.Nom;
                }
            }
            catch (Exception ex) { Journaliseur.Avertir($"[BDD] npcs {Path.GetFileName(fichier)} : {ex.Message}"); }
        }
    }

    private static void ChargerMaps(BaseDonnees bdd, string dossier)
    {
        // maps_hystoria.json : { id : { x, y, area, subarea, ... } }
        var fichier = Path.Combine(dossier, "maps_hystoria.json");
        if (!File.Exists(fichier)) return;
        try
        {
            var doc = JsonDocument.Parse(File.ReadAllText(fichier));
            var racine = doc.RootElement.TryGetProperty("maps", out var maps) ? maps : doc.RootElement;
            foreach (var prop in racine.EnumerateObject())
            {
                try
                {
                    if (!int.TryParse(prop.Name, out var id)) continue;
                    var v = prop.Value;
                    var map = new InfoMap { Identifiant = id };
                    if (v.ValueKind == JsonValueKind.Object)
                    {
                        map.X = LireIntSur(v, "x");
                        map.Y = LireIntSur(v, "y");
                        map.IdArea = LireIntSur(v, "area");
                        map.IdSubArea = LireIntSur(v, "sub", "sa");
                    }
                    bdd.Maps[id] = map;
                }
                catch { /* skip malformed */ }
            }
        }
        catch (Exception ex) { Journaliseur.Avertir($"[BDD] maps : {ex.Message}"); }
    }

    private static void ChargerSkills(BaseDonnees bdd, string dossier)
    {
        var fichier = Path.Combine(dossier, "skills_hystoria.json");
        if (!File.Exists(fichier)) return;
        try
        {
            var doc = JsonDocument.Parse(File.ReadAllText(fichier));
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (!int.TryParse(prop.Name, out var id)) continue;
                var v = prop.Value;
                if (v.ValueKind == JsonValueKind.String)
                    bdd.Skills[id] = v.GetString() ?? "";
                else if (v.ValueKind == JsonValueKind.Object && v.TryGetProperty("n", out var n))
                    bdd.Skills[id] = n.GetString() ?? "";
            }
        }
        catch (Exception ex) { Journaliseur.Avertir($"[BDD] skills : {ex.Message}"); }
    }

    private static void ChargerInteractifs(BaseDonnees bdd, string dossier)
    {
        var fichier = Path.Combine(dossier, "interactiveobjects_hystoria.json");
        if (!File.Exists(fichier)) return;
        try
        {
            var doc = JsonDocument.Parse(File.ReadAllText(fichier));
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (!int.TryParse(prop.Name, out var id)) continue;
                var v = prop.Value;
                var io = new InfoInteractif { Identifiant = id };
                if (v.ValueKind == JsonValueKind.Object)
                {
                    if (v.TryGetProperty("n", out var n)) io.Nom = n.GetString() ?? "";
                    if (v.TryGetProperty("skill", out var s) && s.TryGetInt32(out var si)) io.IdSkill = si;
                }
                else if (v.ValueKind == JsonValueKind.String)
                {
                    io.Nom = v.GetString() ?? "";
                }
                bdd.Interactifs[id] = io;
            }
        }
        catch (Exception ex) { Journaliseur.Avertir($"[BDD] interactiveobjects : {ex.Message}"); }
    }

    private static void ChargerDialogues(BaseDonnees bdd, string dossier)
    {
        var fichier = Path.Combine(dossier, "dialogs_fr.json");
        if (!File.Exists(fichier)) return;
        try
        {
            var doc = JsonDocument.Parse(File.ReadAllText(fichier));
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (!int.TryParse(prop.Name, out var id)) continue;
                if (prop.Value.ValueKind == JsonValueKind.String)
                    bdd.Dialogues[id] = prop.Value.GetString() ?? "";
            }
        }
        catch (Exception ex) { Journaliseur.Avertir($"[BDD] dialogues : {ex.Message}"); }
    }
}

public sealed class InfoItem
{
    public int Identifiant { get; set; }
    public string Nom { get; set; } = "";
    public string Description { get; set; } = "";
    public int IdType { get; set; }
    public int IdGfx { get; set; }
    public int Niveau { get; set; }
    public int Poids { get; set; }
    public override string ToString() => $"#{Identifiant} {Nom} (lvl {Niveau}, {Poids}p)";
}

public sealed class InfoMonstre
{
    public int Identifiant { get; set; }
    public string Nom { get; set; } = "";
    public int Niveau { get; set; }
    public int PointsVie { get; set; }
    public override string ToString() => $"#{Identifiant} {Nom} (Niv.{Niveau}, {PointsVie} PV)";
}

public sealed class InfoNpc
{
    public int Identifiant { get; set; }
    public string Nom { get; set; } = "";
    public override string ToString() => $"#{Identifiant} {Nom}";
}

public sealed class InfoMap
{
    public int Identifiant { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int IdArea { get; set; }
    public int IdSubArea { get; set; }
    public override string ToString() => $"Map #{Identifiant} ({X},{Y})";
}

public sealed class InfoInteractif
{
    public int Identifiant { get; set; }
    public string Nom { get; set; } = "";
    public int IdSkill { get; set; }
    public override string ToString() => $"#{Identifiant} {Nom} (skill {IdSkill})";
}
