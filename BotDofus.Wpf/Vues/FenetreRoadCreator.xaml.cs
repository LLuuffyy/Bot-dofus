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
        CmbPnj.Items.Clear();
        var carte = _ctx?.EtatJeu.CarteCourante;
        if (carte == null) return;
        foreach (var p in carte.Entites.Values.OfType<EntitePNJ>())
        {
            var nom = !string.IsNullOrWhiteSpace(p.Nom) ? p.Nom : $"PNJ {p.IdGabarit}";
            CmbPnj.Items.Add(new ComboBoxItem
            {
                Content = $"{nom} (id {p.IdGabarit}) cell {p.CellulePosition}",
                // On retient les DEUX : contextuel (DC live) + gabarit (script).
                Tag = (p.Identifiant, p.IdGabarit)
            });
        }
        if (CmbPnj.Items.Count > 0) CmbPnj.SelectedIndex = 0;
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
        if (e.Paquet.Direction != BotDofus.Commun.Reseau.DirectionPaquet.VersClient) return;
        var c = e.Paquet.Contenu;
        if (c.Length < 2 || c[0] != 'D' || !char.IsUpper(c[1])) return;
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
        foreach (var rid in d.Reponses)
        {
            var libelle = bdd.DialogueA(rid) ?? $"Réponse #{rid}";
            var btn = new Button
            {
                Content = libelle,
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
                _reponses.Add(rid);
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
            { if (_ctx != null) await _ctx.Api.QuitterDialogueAsync(); };
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
