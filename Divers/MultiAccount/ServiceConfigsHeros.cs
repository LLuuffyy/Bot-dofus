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

    /// <summary>
    /// Liste toutes les configs héros persistées (un fichier par <c>peleas/heros/&lt;id&gt;.json</c>).
    /// Utilisé par l'UI pour proposer l'édition des persos même sans groupe
    /// actif en RAM (= avant le 1er combat).
    /// </summary>
    public static IReadOnlyList<ConfigHeros> ListerToutes()
    {
        var resultat = new List<ConfigHeros>();
        if (!Directory.Exists(Dossier)) return resultat;
        foreach (var fichier in Directory.EnumerateFiles(Dossier, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(fichier);
                var cfg = JsonSerializer.Deserialize<ConfigHeros>(json, Options);
                if (cfg is null) continue;
                if (cfg.IdJeu == 0)
                {
                    // Fallback : nom du fichier = id.
                    var nom = Path.GetFileNameWithoutExtension(fichier);
                    if (int.TryParse(nom, out var idParse)) cfg.IdJeu = idParse;
                }
                if (cfg.IdJeu != 0) resultat.Add(cfg);
            }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[HEROS-CFG] Scan {fichier} échec : {ex.Message}");
            }
        }
        return resultat;
    }

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
    /// Preset de combat par défaut selon la classe Dofus Retro 1.29. Couvre les
    /// 12 classes (Feca/Osa/Enutrof/Sram/Xelor/Eca/Eni/Iop/Cra/Sadida/Sacri/Panda).
    ///
    /// <para>
    /// Chaque preset positionne :
    /// </para>
    /// <list type="bullet">
    ///   <item>Mode (Agressif/Eloigne/Fuyard/Equilibre) → distance d'arrêt idéale</item>
    ///   <item>DistancePreferee + DistanceMinEloigne → kite parameters</item>
    ///   <item>Stratégie (Agressif/Defensif/Soutien) → priorités secondaires</item>
    ///   <item>Rotation sorts par priorité décroissante (NombreParTour=99 = sans limite)</item>
    /// </list>
    ///
    /// <para>
    /// Les sorts sont identifiés par leur ID Retro 1.29 (cf. Resources/data/hechizos_dyshay.xml).
    /// L'user peut ensuite raffiner via l'UI Combat.
    /// </para>
    /// </summary>
    public static ConfigCombat PresetParClasse(int idClasse)
    {
        return idClasse switch
        {
            // === 1 — FECA : tank, boucliers, glyphes (distance moyenne) ===
            1 => new ConfigCombat
            {
                Mode = ModeCombat.Equilibre,
                Strategie = StrategieCombat.Defensif,
                DistancePreferee = 4,
                DistanceMinEloigne = 4,
                Regles =
                {
                    new RegleSort { IdSort = 5,  Priorite = 10, NombreParTour = 1,  Nom = "Aveuglement" },
                    new RegleSort { IdSort = 6,  Priorite = 9,  NombreParTour = 99, Nom = "Attaque Naturelle" },
                    new RegleSort { IdSort = 10, Priorite = 7,  NombreParTour = 99, Nom = "Glyphe Aveuglant" },
                    new RegleSort { IdSort = 11, Priorite = 5,  NombreParTour = 1,  Nom = "Armure Terrestre" },
                },
            },

            // === 2 — OSAMODAS : invocations + crapauds (distance moyenne) ===
            2 => new ConfigCombat
            {
                Mode = ModeCombat.Equilibre,
                Strategie = StrategieCombat.Agressif,
                DistancePreferee = 5,
                DistanceMinEloigne = 4,
                Regles =
                {
                    new RegleSort { IdSort = 33, Priorite = 10, NombreParTour = 1,  PremierTour = true, Nom = "Invocation Bouftou", Focus = FocusSort.CelluleAdjacenteMoi },
                    new RegleSort { IdSort = 35, Priorite = 9,  NombreParTour = 99, Nom = "Crapaud" },
                    new RegleSort { IdSort = 36, Priorite = 7,  NombreParTour = 99, Nom = "Plume Karnage" },
                    new RegleSort { IdSort = 37, Priorite = 5,  NombreParTour = 99, Nom = "Fouet" },
                },
            },

            // === 3 — ENUTROF : kite distance, Lancer de Pièces emblématique ===
            3 => new ConfigCombat
            {
                Mode = ModeCombat.Eloigne,
                Strategie = StrategieCombat.Agressif,
                DistancePreferee = 8,
                DistanceMinEloigne = 6,
                Regles =
                {
                    new RegleSort { IdSort = 51, Priorite = 10, NombreParTour = 99, Nom = "Lancer de Pièces" },
                    new RegleSort { IdSort = 41, Priorite = 8,  NombreParTour = 99, Nom = "Lancer de Pelle" },
                    new RegleSort { IdSort = 43, Priorite = 7,  NombreParTour = 1,  Nom = "Sac Animé", Focus = FocusSort.CelluleAdjacenteMoi },
                    new RegleSort { IdSort = 45, Priorite = 5,  NombreParTour = 1,  Nom = "Prospection (buff)", Focus = FocusSort.Moi, PremierTour = true },
                },
            },

            // === 4 — SRAM : CAC + pièges + invisibilité ===
            4 => new ConfigCombat
            {
                Mode = ModeCombat.Agressif,
                Strategie = StrategieCombat.Agressif,
                DistancePreferee = 1,
                DistanceMinEloigne = 6,
                Regles =
                {
                    new RegleSort { IdSort = 65, Priorite = 10, NombreParTour = 99, Nom = "Attaque Mortelle", SeulementCAC = true },
                    new RegleSort { IdSort = 66, Priorite = 9,  NombreParTour = 99, Nom = "Coup Sournois" },
                    new RegleSort { IdSort = 67, Priorite = 7,  NombreParTour = 99, Nom = "Pelle Spectrale" },
                    new RegleSort { IdSort = 73, Priorite = 5,  NombreParTour = 1,  Nom = "Invisibilité", Focus = FocusSort.Moi, MesPvInfPourcent = 40 },
                },
            },

            // === 5 — XELOR : distance + retrait PM/PA, télé tactique ===
            5 => new ConfigCombat
            {
                Mode = ModeCombat.Eloigne,
                Strategie = StrategieCombat.Defensif,
                DistancePreferee = 7,
                DistanceMinEloigne = 5,
                Regles =
                {
                    new RegleSort { IdSort = 99,  Priorite = 10, NombreParTour = 99, Nom = "Aiguille" },
                    new RegleSort { IdSort = 102, Priorite = 9,  NombreParTour = 99, Nom = "Foudre" },
                    new RegleSort { IdSort = 105, Priorite = 7,  NombreParTour = 1,  Nom = "Téléportation", Focus = FocusSort.CelluleVide, MesPvInfPourcent = 50 },
                    new RegleSort { IdSort = 107, Priorite = 5,  NombreParTour = 99, Nom = "Démotivation" },
                },
            },

            // === 6 — ECAFLIP : mixte aléatoire, multi-cast ===
            6 => new ConfigCombat
            {
                Mode = ModeCombat.Equilibre,
                Strategie = StrategieCombat.Agressif,
                DistancePreferee = 4,
                DistanceMinEloigne = 4,
                Regles =
                {
                    new RegleSort { IdSort = 117, Priorite = 10, NombreParTour = 99, Nom = "Roulette" },
                    new RegleSort { IdSort = 119, Priorite = 9,  NombreParTour = 99, Nom = "Pile ou Face" },
                    new RegleSort { IdSort = 122, Priorite = 7,  NombreParTour = 1,  Nom = "Réflexes (buff)", Focus = FocusSort.Moi, PremierTour = true },
                },
            },

            // === 7 — ENI : soin + dégât, alterne selon alliés ===
            7 => new ConfigCombat
            {
                Mode = ModeCombat.Equilibre,
                Strategie = StrategieCombat.Soutien,
                DistancePreferee = 5,
                DistanceMinEloigne = 4,
                Regles =
                {
                    new RegleSort { IdSort = 132, Priorite = 10, NombreParTour = 99, Nom = "Mot de Soin", Focus = FocusSort.AllieLePlusBlesse, CiblePvInfPourcent = 80 },
                    new RegleSort { IdSort = 144, Priorite = 9,  NombreParTour = 1,  Nom = "Mot Curatif", Focus = FocusSort.AllieLePlusBlesse, CiblePvInfPourcent = 60 },
                    new RegleSort { IdSort = 139, Priorite = 7,  NombreParTour = 99, Nom = "Mot d'Épine" },
                    new RegleSort { IdSort = 138, Priorite = 5,  NombreParTour = 99, Nom = "Voile de Plumes (buff)", Focus = FocusSort.Moi },
                },
            },

            // === 8 — IOP : CAC pur DPS, charge avec Bond ===
            8 => new ConfigCombat
            {
                Mode = ModeCombat.Agressif,
                Strategie = StrategieCombat.Agressif,
                DistancePreferee = 1,
                DistanceMinEloigne = 6,
                Regles =
                {
                    new RegleSort { IdSort = 7,   Priorite = 10, NombreParTour = 99, Nom = "Épée Divine", SeulementCAC = true },
                    new RegleSort { IdSort = 4,   Priorite = 9,  NombreParTour = 99, Nom = "Pression" },
                    new RegleSort { IdSort = 9,   Priorite = 7,  NombreParTour = 1,  Nom = "Bond (positionnement)", Focus = FocusSort.CelluleAdjacenteEnnemi },
                    new RegleSort { IdSort = 8,   Priorite = 5,  NombreParTour = 1,  Nom = "Concentration", Focus = FocusSort.Moi, PremierTour = true },
                },
            },

            // === 9 — CRA : kite distance pur, Flèches ===
            9 => new ConfigCombat
            {
                Mode = ModeCombat.Eloigne,
                Strategie = StrategieCombat.Agressif,
                DistancePreferee = 10,
                DistanceMinEloigne = 6,
                Regles =
                {
                    new RegleSort { IdSort = 161, Priorite = 10, NombreParTour = 99, Nom = "Flèche Magique" },
                    new RegleSort { IdSort = 167, Priorite = 9,  NombreParTour = 99, Nom = "Flèche Empoisonnée" },
                    new RegleSort { IdSort = 169, Priorite = 7,  NombreParTour = 99, Nom = "Flèche Cinglante" },
                    new RegleSort { IdSort = 173, Priorite = 5,  NombreParTour = 99, Nom = "Flèche Punitive" },
                    new RegleSort { IdSort = 175, Priorite = 3,  NombreParTour = 1,  Nom = "Recul (escape)", Focus = FocusSort.Moi, MesPvInfPourcent = 50 },
                },
            },

            // === 10 — SADIDA : invocations + DoT + soin (Beiloddurul) ===
            // NOTE : pour le master Beiloddurul, la config est dans peleas/<compte>.json
            // (édité par l'user via UI). Ce preset sert quand un héros lié est Sadida.
            // Sorts complets (sorts appris Beiloddurul) :
            // 182 La Folle | 183 Ronce | 192 Ronce Apaisante | 193 La Bloqueuse
            // 195 Larme | 198 Sacrifice Poupesque | 200 Poison Paralysant
            10 => new ConfigCombat
            {
                Mode = ModeCombat.Equilibre,
                Strategie = StrategieCombat.Agressif,
                DistancePreferee = 6,
                DistanceMinEloigne = 5,
                Regles =
                {
                    // T1 invocations : poser La Folle (bloque) puis La Bloqueuse (tank).
                    new RegleSort { IdSort = 182, Priorite = 100, NombreParTour = 1, PremierTour = true, Nom = "La Folle (invocation)", Focus = FocusSort.CelluleAdjacenteMoi },
                    new RegleSort { IdSort = 193, Priorite = 95,  NombreParTour = 1, PremierTour = true, Nom = "La Bloqueuse (invocation)", Focus = FocusSort.CelluleAdjacenteMoi },
                    // Dégâts principaux.
                    new RegleSort { IdSort = 183, Priorite = 90,  NombreParTour = 99, Nom = "Ronce" },
                    new RegleSort { IdSort = 195, Priorite = 80,  NombreParTour = 99, Nom = "Larme" },
                    // Poison long terme.
                    new RegleSort { IdSort = 200, Priorite = 70,  NombreParTour = 1, NombreParCible = 1, CooldownTours = 3, Nom = "Poison Paralysant", Focus = FocusSort.EnnemiLePlusFaible },
                    // Soin allié blessé.
                    new RegleSort { IdSort = 192, Priorite = 60,  NombreParTour = 2, Nom = "Ronce Apaisante", Focus = FocusSort.AllieLePlusBlesse, CiblePvInfPourcent = 60 },
                    // Sacrifice Poupesque (sacrifie une invoc pour PV) — uniquement si bas PV.
                    new RegleSort { IdSort = 198, Priorite = 50,  NombreParTour = 1, Nom = "Sacrifice Poupesque", Focus = FocusSort.Moi, MesPvInfPourcent = 30, SiInvocPresente = true },
                },
            },

            // === 11 — SACRIEUR : CAC + Attirance, se mettre au CAC ===
            11 => new ConfigCombat
            {
                Mode = ModeCombat.Agressif,
                Strategie = StrategieCombat.Agressif,
                DistancePreferee = 1,
                DistanceMinEloigne = 6,
                Regles =
                {
                    new RegleSort { IdSort = 184, Priorite = 10, NombreParTour = 99, Nom = "Châtiment Spirituel" },
                    new RegleSort { IdSort = 188, Priorite = 9,  NombreParTour = 99, Nom = "Folie Sanguinaire", SeulementCAC = true },
                    new RegleSort { IdSort = 186, Priorite = 7,  NombreParTour = 1,  Nom = "Attirance (rapproche)", Focus = FocusSort.EnnemiLePlusFort },
                    new RegleSort { IdSort = 189, Priorite = 5,  NombreParTour = 1,  Nom = "Sacrifice", Focus = FocusSort.AllieLePlusBlesse, CiblePvInfPourcent = 40 },
                },
            },

            // === 12 — PANDAWA : tank + portage, distance moyenne ===
            12 => new ConfigCombat
            {
                Mode = ModeCombat.Equilibre,
                Strategie = StrategieCombat.Defensif,
                DistancePreferee = 4,
                DistanceMinEloigne = 4,
                Regles =
                {
                    new RegleSort { IdSort = 691, Priorite = 10, NombreParTour = 99, Nom = "Explosion Pandawesque" },
                    new RegleSort { IdSort = 695, Priorite = 9,  NombreParTour = 99, Nom = "Karcham" },
                    new RegleSort { IdSort = 696, Priorite = 7,  NombreParTour = 1,  Nom = "Vulnérabilité" },
                    new RegleSort { IdSort = 692, Priorite = 5,  NombreParTour = 1,  Nom = "Souffle Alcoolisé (push)", SeulementCAC = true },
                },
            },

            // === DÉFAUT : configuration neutre (classe inconnue ou non gérée) ===
            _ => new ConfigCombat
            {
                Mode = ModeCombat.Equilibre,
                Strategie = StrategieCombat.Agressif,
                DistancePreferee = 4,
                DistanceMinEloigne = 4,
            },
        };
    }
}
