using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BotDofus.Commun.Reseau;
using BotDofus.Divers.Cartes;
using BotDofus.Divers.Combats.Enums;
using BotDofus.Divers.Donnees;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Scripts;

/// <summary>
/// Enregistreur de trajet « façon RoadCreator / SynFus » : pendant que tu
/// joues normalement (MITM), il note chaque carte traversée, la cellule de
/// sortie utilisée, et si tu as récolté / combattu dessus. À l'arrêt il
/// génère un script <c>move()</c> au format AnkaBot prêt à rejouer.
/// </summary>
public sealed class EnregistreurTrajet
{
    private sealed class Ligne
    {
        public string Coords = "?,?";
        public int CelluleSortie;
        public bool Gather;
        public bool Fight;
    }

    private ContexteCompte? _ctx;
    private readonly List<Ligne> _lignes = new();
    private Ligne? _courante;
    private int _mapEnCours = -1;

    public bool EnCours { get; private set; }
    public int NbLignes => _lignes.Count;

    public void Demarrer(ContexteCompte ctx)
    {
        if (EnCours) return;
        _ctx = ctx;
        _lignes.Clear();
        _courante = null;
        _mapEnCours = ctx.EtatJeu.Personnage.CarteCourante ?? -1;
        DemarrerLigne(_mapEnCours);

        ctx.PaquetRecu += OnPaquet;
        ctx.EtatJeu.CarteChangee += OnCarteChangee;
        ctx.EtatJeu.Combat.EtatChange += OnCombat;
        EnCours = true;
        Journaliseur.Info("[ROADREC] Enregistrement de trajet DÉMARRÉ — joue normalement.");
    }

    /// <summary>Arrête, génère le script Lua et le sauvegarde. Retourne le chemin.</summary>
    public string? Arreter()
    {
        if (!EnCours || _ctx == null) return null;
        _ctx.PaquetRecu -= OnPaquet;
        _ctx.EtatJeu.CarteChangee -= OnCarteChangee;
        _ctx.EtatJeu.Combat.EtatChange -= OnCombat;
        EnCours = false;

        // Ferme la dernière carte (sans sortie connue).
        if (_courante != null) _lignes.Add(_courante);
        _courante = null;

        if (_lignes.Count == 0)
        {
            Journaliseur.Avertir("[ROADREC] Aucun trajet enregistré.");
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
            Journaliseur.Info(
                $"[ROADREC] Trajet enregistré ({_lignes.Count} carte(s)) → {chemin}");
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[ROADREC] écriture script : {ex.Message}");
            return null;
        }
        return chemin;
    }

    private void DemarrerLigne(int mapId)
    {
        var m = BaseDonnees.Instance.Map(mapId);
        _courante = new Ligne { Coords = m != null ? $"{m.X},{m.Y}" : mapId.ToString() };
    }

    private void OnPaquet(object? s, EvenementPaquetRecu e)
    {
        // GA500 C→S = récolte sur la carte courante.
        if (_courante != null
            && e.Paquet.Direction == DirectionPaquet.VersServeur
            && e.Paquet.Contenu.StartsWith("GA500", StringComparison.Ordinal))
            _courante.Gather = true;
    }

    private void OnCombat(object? s, EtatCombat etat)
    {
        if (_courante != null && etat != EtatCombat.Inactif) _courante.Fight = true;
    }

    private void OnCarteChangee(object? s, Carte nouvelle)
    {
        if (_ctx == null) return;
        // La cellule de SORTIE = position du perso juste avant le changement
        // (le GA0 de transition l'a posée sur la case « soleil »).
        if (_courante != null)
        {
            _courante.CelluleSortie = _ctx.EtatJeu.Personnage.CellulePosition ?? 0;
            _lignes.Add(_courante);
        }
        _mapEnCours = nouvelle.Identifiant;
        DemarrerLigne(_mapEnCours);
    }

    private string GenererLua()
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- Trajet enregistré automatiquement (RoadCreator)");
        sb.AppendLine("-- Format AnkaBot : rejoue ce parcours via Scripts → Charger.");
        sb.AppendLine();
        sb.AppendLine("function move()");
        sb.AppendLine("  return {");
        foreach (var l in _lignes)
        {
            var sb2 = new StringBuilder($"    {{ map = \"{l.Coords}\"");
            if (l.CelluleSortie > 0) sb2.Append($", path = \"{l.CelluleSortie}\"");
            if (l.Gather) sb2.Append(", gather = true");
            if (l.Fight) sb2.Append(", fight = true");
            sb2.Append(" },");
            sb.AppendLine(sb2.ToString());
        }
        sb.AppendLine("  }");
        sb.AppendLine("end");
        sb.AppendLine();
        sb.AppendLine("function bank()   return {} end");
        sb.AppendLine("function phenix() return {} end");
        return sb.ToString();
    }
}
