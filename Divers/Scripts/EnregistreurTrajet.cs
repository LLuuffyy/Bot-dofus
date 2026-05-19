using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Scripts;

/// <summary>
/// Enregistreur de trajet « façon SynFus RoadCreator ». L'UI affiche une
/// petite fenêtre par carte (Combat / Récolte / Cellule / Direction de
/// sortie / PNJ) ; chaque validation ajoute une ligne. À l'arrêt, génère un
/// script <c>mouvement()</c> rejouable par le moteur de route.
/// </summary>
public sealed class EnregistreurTrajet
{
    public sealed class Ligne
    {
        public int MapId;
        public string Coords = "?,?";
        public bool Fight;
        public bool Gather;
        public int Cellule;          // sortie via cellule précise (optionnel)
        public string Direction = ""; // top/bottom/left/right (optionnel)
        public int Npc;              // dialogue PNJ (optionnel)
        public List<int> Answers = new();
        public string CheminBrut = ""; // GA001 EXACT capturé à la main (rejeu fidèle)
    }

    private readonly List<Ligne> _lignes = new();
    public bool EnCours { get; private set; }
    public int NbLignes => _lignes.Count;
    private string _nom = "trajet";

    public void Demarrer(string nom)
    {
        _lignes.Clear();
        _nom = string.IsNullOrWhiteSpace(nom) ? "trajet" : nom.Trim();
        EnCours = true;
        Journaliseur.Info($"[ROADREC] Enregistrement DÉMARRÉ « {_nom} » — configure chaque carte.");
    }

    public void AjouterLigne(Ligne l)
    {
        _lignes.Add(l);
        Journaliseur.Info(
            $"[ROADREC] +carte {l.Coords} (id {l.MapId}) "
            + $"{(l.Fight ? "combat " : "")}{(l.Gather ? "récolte " : "")}"
            + $"{(l.Npc > 0 ? $"pnj#{l.Npc} " : "")}"
            + $"sortie={(l.Cellule > 0 ? "cell " + l.Cellule : l.Direction)}"
            + $" — {_lignes.Count} waypoint(s)");
    }

    public string? Arreter()
    {
        EnCours = false;
        if (_lignes.Count == 0)
        {
            Journaliseur.Avertir("[ROADREC] Aucun waypoint enregistré.");
            return null;
        }
        var lua = GenererLua();
        var dossier = Directory.Exists("scripts")
            ? Path.GetFullPath("scripts") : Environment.CurrentDirectory;
        var chemin = Path.Combine(dossier,
            $"trajet_enregistre_{DateTime.Now:yyyyMMdd_HHmmss}.lua");
        try
        {
            File.WriteAllText(chemin, lua);
            Journaliseur.Info($"[ROADREC] Trajet ({_lignes.Count} carte(s)) → {chemin}");
            return chemin;
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[ROADREC] écriture : {ex.Message}");
            return null;
        }
    }

    private string GenererLua()
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- Trajet enregistré (RoadCreator)");
        sb.AppendLine($"-- Date : {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("SHOW_FIGHT_COUNTER = true");
        sb.AppendLine();
        sb.AppendLine("function mouvement()");
        sb.AppendLine("    return {");
        foreach (var l in _lignes)
        {
            var sb2 = new StringBuilder($"        {{ map = \"{l.MapId}\"");
            if (l.Fight) sb2.Append(", fight = true");
            if (l.Gather) sb2.Append(", gather = true");
            if (l.Npc > 0)
            {
                sb2.Append($", npc = {l.Npc}");
                if (l.Answers.Count > 0)
                    sb2.Append($", answers = {{ {string.Join(", ", l.Answers)} }}");
            }
            // PRIORITÉ À LA DIRECTION (top/bottom/left/right). C'est la seule
            // sortie GLOBALE : depuis n'importe où sur la carte elle marche
            // vers le bon bord et change de map. Le raw:GA001 était capturé
            // depuis la case EXACTE d'enregistrement → après une récolte le
            // point de départ varie (blé déjà pris…) → rejeu KO. On l'abandonne
            // comme mécanisme principal. Ordre :
            //   1) direction  (changement de carte global, robuste)
            //   2) cell       (se poser sur une case précise : PNJ/récolte —
            //                   PAS un changement de carte)
            if (!string.IsNullOrEmpty(l.Direction))
                sb2.Append($", path = \"{l.Direction}\"");
            // Carte de PURE TRANSITION (ni récolte ni combat) : on émet la
            // CELLULE de sortie seule. Le moteur route vers SortirCarteAsync
            // qui est DIRECTION-first (déduit le côté est/ouest/sud/nord à
            // partir de la cellule vs les cellules Transition de la map, puis
            // ChangerMapDirectionAsync). Robuste depuis N'IMPORTE OÙ sur la
            // map (comme SynFus/dyshay/cadernis). On ABANDONNE le raw:GA001 :
            // il était capturé depuis la case EXACTE d'enregistrement → rejeu
            // dépendant de la position de départ → ~13 s perdues à marcher
            // vers la mauvaise case avant que le secours direction n'agisse.
            else if (l.Cellule > 0)
                sb2.Append($", path = \"{l.Cellule}\"");
            sb2.Append($" }},   -- [{l.Coords}]");
            sb.AppendLine(sb2.ToString());
        }
        sb.AppendLine("    }");
        sb.AppendLine("end");
        sb.AppendLine();
        sb.AppendLine("function banque() return {} end");
        sb.AppendLine("function phenix() return {} end");
        return sb.ToString();
    }
}
