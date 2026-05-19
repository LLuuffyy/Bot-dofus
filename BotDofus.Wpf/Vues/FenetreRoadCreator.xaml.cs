using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BotDofus.Divers;
using BotDofus.Divers.Cartes.Entites;
using BotDofus.Divers.Donnees;
using BotDofus.Divers.Scripts;

namespace BotDofus.Wpf.Vues;

/// <summary>
/// Fenêtre « Road Creator » : pour la carte courante on coche Combat/Récolte,
/// on peut PARLER à un PNJ directement (les réponses cliquées sont envoyées
/// ET enregistrées), puis on choisit la sortie. Chaque validation lève
/// <see cref="Validee"/> avec la ligne (npc + réponses inclus).
/// </summary>
public partial class FenetreRoadCreator : Window
{
    private int _mapId;
    private string _coords = "?,?";
    private ContexteCompte? _ctx;
    private int _npcChoisi;        // id CONTEXTUEL (négatif) — pour le DC live
    private int _npcTemplate;      // id TEMPLATE (gabarit) — pour le script .lua
    private int _dernierMapPrepare = -1;
    private string _dernierGa001 = "";   // dernier déplacement C→S fait à la main
    private readonly List<int> _reponses = new();

    public event EventHandler<EnregistreurTrajet.Ligne>? Validee;

    public FenetreRoadCreator()
    {
        InitializeComponent();
        MouseLeftButtonDown += (_, e) =>
        { if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            try { DragMove(); } catch { } };
    }

    /// <summary>Lie le contexte (API + état) pour l'interaction PNJ live.</summary>
    public void Initialiser(ContexteCompte ctx)
    {
        if (_ctx != null) _ctx.PaquetRecu -= OnPaquet;
        _ctx = ctx;
        _ctx.PaquetRecu += OnPaquet;
    }

    public void Preparer(int mapId, string coords)
    {
        _mapId = mapId;
        _coords = coords;
        TxtTitre.Text = $"Map [{coords}] — ID: {mapId}";
        ChkCombat.IsChecked = false;
        ChkRecolte.IsChecked = false;
        ChkDonjon.IsChecked = false;
        ChkBoss.IsChecked = false;
        TxtCellule.Text = "";
        // IMPORTANT : on ne vide le PNJ/les réponses que si la carte a VRAIMENT
        // changé. Sinon un simple rafraîchissement GM (même carte) effacerait
        // le PNJ auquel on vient de parler avant qu'on valide la sortie
        // (= « rien dans le script »).
        if (mapId != _dernierMapPrepare)
        {
            _npcChoisi = 0;
            _npcTemplate = 0;
            _reponses.Clear();
            _dernierGa001 = ""; // nouveau terrain : on repart sans chemin brut
            BoxDialogue.Visibility = Visibility.Collapsed;
            TxtDlgEnregistre.Text = "";
        }
        _dernierMapPrepare = mapId;
        RemplirPnj();
        if (!IsVisible) Show();
        Activate();
    }

    private void RemplirPnj()
    {
        var carte = _ctx?.EtatJeu.CarteCourante;
        if (carte == null) return;
        var pnjs = carte.Entites.Values.OfType<EntitePNJ>().ToList();
        // Rien à afficher encore (GM pas arrivé) : on garde la liste telle
        // quelle pour ne pas la vider inutilement.
        if (pnjs.Count == 0) return;

        // Préserve la sélection courante (gabarit) au cas où on rafraîchit
        // pendant que l'utilisateur a déjà choisi un PNJ.
        int gabaritSel = (CmbPnj.SelectedItem as ComboBoxItem)?.Tag
            is ValueTuple<int, int> t ? t.Item2 : 0;

        CmbPnj.Items.Clear();
        int idx = 0, aSelectionner = -1;
        foreach (var p in pnjs)
        {
            var nom = !string.IsNullOrWhiteSpace(p.Nom) ? p.Nom : $"PNJ {p.IdGabarit}";
            CmbPnj.Items.Add(new ComboBoxItem
            {
                Content = $"{nom} (id {p.IdGabarit}) cell {p.CellulePosition}",
                // Pas de Foreground ici : un style sombre (fond gris foncé /
                // texte blanc) est défini en XAML (ComboBox.Resources). Forcer
                // la couleur ici (valeur locale) écraserait ce style → texte
                // illisible sur fond sombre.
                // On retient les DEUX : contextuel (DC live) + gabarit (script).
                Tag = (p.Identifiant, p.IdGabarit)
            });
            if (p.IdGabarit == gabaritSel && aSelectionner < 0) aSelectionner = idx;
            idx++;
        }
        if (CmbPnj.Items.Count > 0)
            CmbPnj.SelectedIndex = aSelectionner >= 0 ? aSelectionner : 0;
        return;
    }

    private async void BtnParlerPnj_Click(object sender, RoutedEventArgs e)
    {
        if (_ctx == null || CmbPnj.SelectedItem is not ComboBoxItem it
            || it.Tag is not ValueTuple<int, int> ids) return;
        _npcChoisi = ids.Item1;     // contextuel (négatif) → DC live
        _npcTemplate = ids.Item2;   // gabarit → écrit dans le script
        _reponses.Clear();
        TxtDlgEnregistre.Text = "";
        BoxDialogue.Visibility = Visibility.Visible;
        TxtDlgQuestion.Text = "(ouverture du dialogue…)";
        DlgReponses.Children.Clear();
        await _ctx.Api.ParlerPnjAsync(0, _npcChoisi);
    }

    private void OnPaquet(object? s, BotDofus.Commun.Reseau.EvenementPaquetRecu e)
    {
        // C→S : on mémorise le DERNIER déplacement GA001 fait à la main
        // (chemin EXACT, serveur-valide) pour le rejouer fidèlement.
        if (e.Paquet.Direction == BotDofus.Commun.Reseau.DirectionPaquet.VersServeur)
        {
            var cs = e.Paquet.Contenu;
            if (cs.StartsWith("GA001", StringComparison.Ordinal) && cs.Length > 6)
                _dernierGa001 = cs;
            return;
        }
        var c = e.Paquet.Contenu;
        if (c.Length < 2) return;
        // GM = (ré)apparition d'entités : les PNJ arrivent souvent APRÈS
        // l'ouverture de la fenêtre (Preparer appelé au changement de carte,
        // GM PNJ ~250 ms plus tard) → la liste était vide. On la re-remplit.
        if (c[0] == 'G' && c[1] == 'M')
        {
            Dispatcher.BeginInvoke(new Action(RemplirPnj));
            return;
        }
        if (c[0] != 'D' || !char.IsUpper(c[1])) return;
        Dispatcher.BeginInvoke(new Action(RafraichirDialogue));
    }

    private void RafraichirDialogue()
    {
        if (_ctx == null || _npcChoisi == 0) return;
        var d = _ctx.EtatJeu.Dialogue;
        if (!d.Ouvert) { return; }
        BoxDialogue.Visibility = Visibility.Visible;
        var bdd = BaseDonnees.Instance;
        TxtDlgQuestion.Text = bdd.DialogueQ(d.QuestionId) ?? $"(dialogue #{d.QuestionId})";
        DlgReponses.Children.Clear();
        for (int i = 0; i < d.Reponses.Count; i++)
        {
            var rid = d.Reponses[i];
            int position = i + 1;             // 1ère, 2ème, … réponse proposée
            int code = -position;             // AnkaBot : -1, -2, -3 …
            var libelle = bdd.DialogueA(rid) ?? $"Réponse #{rid}";
            var btn = new Button
            {
                Content = $"[{code}] {libelle}",
                Margin = new Thickness(0, 2, 0, 2),
                Padding = new Thickness(8, 4, 8, 4),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x2B, 0x33, 0x40)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand,
                Tag = rid
            };
            btn.Click += async (_, __) =>
            {
                if (_ctx == null) return;
                int q = _ctx.EtatJeu.Dialogue.QuestionId;
                // On enregistre la POSITION (AnkaBot : -1/-2/…), pas l'id brut.
                _reponses.Add(code);
                TxtDlgEnregistre.Text = "Réponses enregistrées : "
                    + string.Join(", ", _reponses);
                await _ctx.Api.RepondreDialogueAsync(q, rid);
            };
            DlgReponses.Children.Add(btn);
        }
        if (d.Reponses.Count == 0)
        {
            var fin = new Button
            {
                Content = "Terminer le dialogue ▸",
                Margin = new Thickness(0, 4, 0, 0), Padding = new Thickness(8, 5, 8, 5),
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x2D, 0x4A, 0x33)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand
            };
            fin.Click += async (_, __) =>
            {
                if (_ctx != null) await _ctx.Api.QuitterDialogueAsync();
                // Dialogue terminé : on referme la boîte dans le créateur.
                BoxDialogue.Visibility = Visibility.Collapsed;
                DlgReponses.Children.Clear();
            };
            DlgReponses.Children.Add(fin);
        }
    }

    private void Dir_Click(object sender, RoutedEventArgs e)
    {
        var l = new EnregistreurTrajet.Ligne
        {
            MapId = _mapId,
            Coords = _coords,
            Fight = ChkCombat.IsChecked == true,
            Gather = ChkRecolte.IsChecked == true,
            Direction = (sender as Button)?.Tag as string ?? ""
        };
        if (int.TryParse(TxtCellule.Text?.Trim(), out var cell) && cell > 0)
            l.Cellule = cell;
        // Dans le script on écrit le GABARIT (ex. npc = 228) façon AnkaBot :
        // stable entre sessions. Le moteur le re-résout vers l'id contextuel
        // de la carte au moment de rejouer.
        if (_npcTemplate != 0)
        {
            l.Npc = _npcTemplate;
            l.Answers.AddRange(_reponses);
        }
        Validee?.Invoke(this, l);
        Hide();
    }

    /// <summary>
    /// Construit AUTOMATIQUEMENT la ligne de la carte qu'on vient de quitter
    /// (sortie d'auberge / changement de map fait À LA MAIN) : on capture la
    /// VRAIE cellule de sortie (celle où le perso était juste avant que la map
    /// change) + l'état des cases Combat/Récolte + le PNJ éventuel. Aucune
    /// saisie ni clic « Valider » nécessaire — façon SynFus.
    /// </summary>
    public EnregistreurTrajet.Ligne ConstruireLigneAuto(int mapId, string coords, int celluleSortie)
    {
        var l = new EnregistreurTrajet.Ligne
        {
            MapId = mapId,
            Coords = coords,
            Fight = ChkCombat.IsChecked == true,
            Gather = ChkRecolte.IsChecked == true,
        };
        if (celluleSortie > 0) l.Cellule = celluleSortie; // fallback
        // Chemin EXACT que tu viens de faire à la main → rejeu fidèle,
        // serveur-valide (plus de rollback/pathfinder qui se trompe).
        if (!string.IsNullOrEmpty(_dernierGa001)) l.CheminBrut = _dernierGa001;
        if (_npcTemplate != 0)
        {
            l.Npc = _npcTemplate;
            l.Answers.AddRange(_reponses);
        }
        // On consomme le PNJ/les réponses/le chemin (rattachés à cette carte).
        _npcChoisi = 0;
        _npcTemplate = 0;
        _reponses.Clear();
        _dernierGa001 = "";
        return l;
    }

    public void MajCompteur(int n)
        => TxtCompteur.Text = $"● REC — {n} waypoint{(n > 1 ? "s" : "")}";

    private void BtnFermer_Click(object sender, RoutedEventArgs e) => Hide();

    private bool _fermetureReelle;
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_fermetureReelle) { e.Cancel = true; Hide(); }
        base.OnClosing(e);
    }

    public void FermerVraiment()
    {
        Validee = null;
        if (_ctx != null) _ctx.PaquetRecu -= OnPaquet;
        _fermetureReelle = true;
        try { Close(); } catch { }
    }
}
