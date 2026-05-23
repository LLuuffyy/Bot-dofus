using System;
using System.Collections.Generic;
using System.IO;
using BotDofus.Utilitaires.Journaux;
using MoonSharp.Interpreter;

namespace BotDofus.Divers.Scripts;

/// <summary>
/// Charge un fichier .lua, exécute son contenu via MoonSharp, puis extrait
/// les globales de configuration et le résultat des fonctions
/// <c>mouvement()</c>/<c>movement()</c> et <c>banque()</c>/<c>bank()</c>.
///
/// Compatible avec le format généré par SynFus Script Recorder.
/// </summary>
public sealed class ChargeurLua
{
    public ScriptCharge Charger(string cheminFichier)
    {
        if (!File.Exists(cheminFichier))
            throw new FileNotFoundException("Fichier script introuvable", cheminFichier);

        var code = File.ReadAllText(cheminFichier);
        var script = new Script(CoreModules.Preset_SoftSandbox);
        try
        {
            script.DoString(code);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erreur à l'exécution du script {Path.GetFileName(cheminFichier)}", ex);
        }

        var config = ExtraireConfiguration(script);
        var mouvements = ExtraireEtapes(script, "mouvement", "movement");
        var banque = ExtraireEtapes(script, "banque", "bank");
        var marchand = ExtraireEtapes(script, "marchand", "merchant");

        return new ScriptCharge
        {
            CheminFichier = cheminFichier,
            Nom = Path.GetFileNameWithoutExtension(cheminFichier),
            Configuration = config,
            EtapesMouvement = mouvements,
            EtapesBanque = banque,
            EtapesMarchand = marchand
        };
    }

    private static ConfigurationScript ExtraireConfiguration(Script script)
    {
        var config = new ConfigurationScript();

        var showCounter = script.Globals.Get("SHOW_FIGHT_COUNTER");
        if (showCounter.Type == DataType.Boolean) config.AfficherCompteurCombats = showCounter.Boolean;

        var maxPods = script.Globals.Get("MAX_PODS");
        if (maxPods.Type == DataType.Number) config.PodsMax = (int)maxPods.Number;

        var seuilMarchand = script.Globals.Get("MARCHAND_SEUIL_PODS");
        if (seuilMarchand.Type == DataType.Number) config.MarchandSeuilPods = (int)seuilMarchand.Number;

        var forceFight = script.Globals.Get("FORCE_FIGHT");
        if (forceFight.Type == DataType.Boolean) config.ForceFight = forceFight.Boolean;

        var autoRegen = script.Globals.Get("AUTO_REGEN");
        if (autoRegen.Type == DataType.Table)
        {
            var t = autoRegen.Table;
            config.RegenerationAutomatique = new RegenerationAuto
            {
                PvMinimumPourcent = (int)(t.Get("MIN_HP").CastToNumber() ?? 50),
                PvMaximumPourcent = (int)(t.Get("MAX_HP").CastToNumber() ?? 90),
                IdentifiantsObjetsSoins = ExtraireListeInt(t.Get("ITEMS"))
            };
        }

        var dungeonMaps = script.Globals.Get("DUNGEON_MAPS");
        if (dungeonMaps.Type == DataType.Table)
        {
            config.CartesDonjon = ExtraireListeInt(dungeonMaps);
        }

        var soulCapture = script.Globals.Get("SOUL_CAPTURE");
        if (soulCapture.Type == DataType.Table)
        {
            config.CaptureAme = new CaptureAme
            {
                IdentifiantSort = (int)(soulCapture.Table.Get("spell").CastToNumber() ?? 0),
                CarteCible = (int)(soulCapture.Table.Get("map").CastToNumber() ?? 0)
            };
        }

        return config;
    }

    private static IReadOnlyList<EtapeScript> ExtraireEtapes(Script script, params string[] nomsFonctions)
    {
        foreach (var nom in nomsFonctions)
        {
            var fn = script.Globals.Get(nom);
            if (fn.Type != DataType.Function) continue;

            try
            {
                var resultat = script.Call(fn);
                if (resultat.Type == DataType.Table)
                {
                    return ConvertirTable(resultat.Table);
                }
            }
            catch (Exception ex)
            {
                Journaliseur.Erreur($"Échec d'appel de {nom}()", ex);
            }
        }
        return new List<EtapeScript>();
    }

    private static List<EtapeScript> ConvertirTable(Table table)
    {
        var resultat = new List<EtapeScript>();
        foreach (var pair in table.Pairs)
        {
            if (pair.Value.Type != DataType.Table) continue;
            var t = pair.Value.Table;

            resultat.Add(new EtapeScript
            {
                IdentifiantCarte = t.Get("map").CastToString() ?? string.Empty,
                Direction = t.Get("direction").Type == DataType.String ? t.Get("direction").String : null,
                IdentifiantPNJ = t.Get("npc").Type == DataType.Number ? (int)t.Get("npc").Number : null,
                ReponsesDialogue = t.Get("answers").Type == DataType.Table ? ExtraireListeInt(t.Get("answers")) : null,
                CelluleCible = t.Get("cell").Type == DataType.Number ? (int)t.Get("cell").Number : null,
                EngagerCombat = t.Get("fight").Type == DataType.Boolean && t.Get("fight").Boolean,
                UtiliserBanque = t.Get("npc_bank").Type == DataType.Boolean && t.Get("npc_bank").Boolean,
                UtiliserMarchand = t.Get("npc_marchand").Type == DataType.Boolean && t.Get("npc_marchand").Boolean,
            });
        }
        return resultat;
    }

    private static List<int> ExtraireListeInt(DynValue valeur)
    {
        var resultat = new List<int>();
        if (valeur.Type != DataType.Table) return resultat;
        foreach (var pair in valeur.Table.Pairs)
        {
            if (pair.Value.Type == DataType.Number) resultat.Add((int)pair.Value.Number);
        }
        return resultat;
    }
}
