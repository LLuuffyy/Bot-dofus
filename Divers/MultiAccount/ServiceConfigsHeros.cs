using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BotDofus.Divers.Combats.IA;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.MultiAccount;

/// <summary>
/// Persistance des configs et des sorts par héros lié.
///
/// Fichier par perso : <c>peleas/heros/&lt;idJeu&gt;.json</c>. Sépare le master
/// (toujours <c>peleas/&lt;identifiantCompte&gt;.json</c>) des suiveurs (mode héros).
///
/// Stockage :
/// <list type="bullet">
///   <item>Identité : Nom, Niveau, IdClasse — pour mémoire entre sessions</item>
///   <item>SortsAppris : Dict idSort → niveau (capturé via Nh/Ns)</item>
///   <item>PositionsBarre : Dict idSort → position barre (-1 = hors barre)</item>
///   <item>ConfigCombat : règles, mode, focus, etc. — édité par l'user</item>
/// </list>
/// </summary>
public static class ServiceConfigsHeros
{
    private static readonly string Dossier = Path.Combine("peleas", "heros");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public sealed class ConfigHeros
    {
        public int IdJeu { get; set; }
        public string Nom { get; set; } = string.Empty;
        public int IdClasse { get; set; }
        public int Niveau { get; set; }
        public Dictionary<int, int> SortsAppris { get; set; } = new();
        public Dictionary<int, int> PositionsBarre { get; set; } = new();
        public ConfigCombat? ConfigCombat { get; set; }
    }

    private static string Chemin(int idJeu) => Path.Combine(Dossier, $"{idJeu}.json");

    /// <summary>Charge la config persistée d'un héros, ou null si absente.</summary>
    public static ConfigHeros? Charger(int idJeu)
    {
        try
        {
            var fichier = Chemin(idJeu);
            if (!File.Exists(fichier)) return null;
            var json = File.ReadAllText(fichier);
            return JsonSerializer.Deserialize<ConfigHeros>(json, Options);
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[HEROS-CFG] Lecture {idJeu} échec : {ex.Message}");
            return null;
        }
    }

    /// <summary>Sauvegarde la config d'un membre dans son fichier dédié.</summary>
    public static bool Sauvegarder(MembreHeros membre)
    {
        if (membre is null || membre.IdJeu == 0) return false;
        try
        {
            Directory.CreateDirectory(Dossier);
            var cfg = new ConfigHeros
            {
                IdJeu = membre.IdJeu,
                Nom = membre.Nom,
                IdClasse = membre.IdClasse,
                Niveau = membre.Niveau,
                SortsAppris = new Dictionary<int, int>(membre.SortsAppris),
                PositionsBarre = new Dictionary<int, int>(membre.PositionsBarre),
                ConfigCombat = membre.ConfigCombat,
            };
            var json = JsonSerializer.Serialize(cfg, Options);
            File.WriteAllText(Chemin(membre.IdJeu), json);
            return true;
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[HEROS-CFG] Écriture {membre.IdJeu} échec : {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Applique la config persistée à un MembreHeros tout juste créé. Si pas
    /// de fichier, applique le preset par défaut basé sur la classe.
    /// </summary>
    public static void HydrateMembre(MembreHeros membre)
    {
        if (membre is null || membre.IdJeu == 0) return;
        var cfg = Charger(membre.IdJeu);
        if (cfg is not null)
        {
            // Charge depuis disque : on enrichit les champs vides (ne pas écraser
            // les valeurs runtime déjà observées via PM/GTM/NLK).
            if (string.IsNullOrWhiteSpace(membre.Nom) && !string.IsNullOrWhiteSpace(cfg.Nom))
                membre.Nom = cfg.Nom;
            if (membre.IdClasse == 0 && cfg.IdClasse != 0) membre.IdClasse = cfg.IdClasse;
            if (membre.Niveau == 0 && cfg.Niveau != 0) membre.Niveau = cfg.Niveau;
            foreach (var kv in cfg.SortsAppris) membre.SortsAppris[kv.Key] = kv.Value;
            foreach (var kv in cfg.PositionsBarre) membre.PositionsBarre[kv.Key] = kv.Value;
            membre.ConfigCombat = cfg.ConfigCombat ?? PresetParClasse(membre.IdClasse);
            Journaliseur.Info(
                $"[HEROS-CFG] Chargé {membre.Nom} (id {membre.IdJeu}) — {membre.SortsAppris.Count} sort(s)");
        }
        else
        {
            // Pas de fichier persisté : on applique le preset par défaut.
            membre.ConfigCombat = PresetParClasse(membre.IdClasse);
            Journaliseur.Info(
                $"[HEROS-CFG] Preset par classe {membre.IdClasse} appliqué à {membre.Nom} (id {membre.IdJeu})");
        }
    }

    /// <summary>
    /// Preset de combat par défaut selon la classe. À éditer côté UI ensuite.
    /// </summary>
    public static ConfigCombat PresetParClasse(int idClasse)
    {
        var cfg = idClasse switch
        {
            // Enutrof : kiter en distance, prioriser Lancer de Pièces (sort 51)
            // qui est le sort de farm XP/kamas le plus emblématique.
            3 => new ConfigCombat
            {
                Mode = ModeCombat.Eloigne,
                Strategie = StrategieCombat.Agressif,
                DistancePreferee = 4,
                DistanceMinEloigne = 4,
                Regles =
                {
                    new RegleSort { IdSort = 51, Priorite = 10, NombreParTour = 99 }, // Lancer de Pièces
                    new RegleSort { IdSort = 41, Priorite = 8,  NombreParTour = 99 }, // Lancer de Pelle
                    new RegleSort { IdSort = 43, Priorite = 5,  NombreParTour = 99 }, // Sac Animé
                },
            },
            // Sadida (10) : géré par peleas/<id-master>.json, preset non utilisé.
            _ => new ConfigCombat
            {
                Mode = ModeCombat.Equilibre,
                Strategie = StrategieCombat.Agressif,
                DistancePreferee = 3,
            },
        };
        return cfg;
    }
}
