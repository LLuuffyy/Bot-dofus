using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BotDofus.Divers.Scripts;

namespace BotDofus.Wpf.Vues;

/// <summary>
/// Petite fenêtre « RoadCreator » (façon SynFus) : pour la carte courante,
/// on coche Combat / Récolte, on indique éventuellement une cellule de
/// sortie ou un PNJ, puis on choisit la direction de sortie. Chaque
/// validation lève <see cref="Validee"/> avec la ligne construite.
/// </summary>
public partial class FenetreRoadCreator : Window
{
    private int _mapId;
    private string _coords = "?,?";

    /// <summary>Levé quand l'utilisateur valide la carte (direction ou « sans »).</summary>
    public event EventHandler<EnregistreurTrajet.Ligne>? Validee;

    public FenetreRoadCreator()
    {
        InitializeComponent();
        // Fenêtre sans chrome → déplaçable en glissant n'importe où.
        MouseLeftButtonDown += (_, e) =>
        { if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            try { DragMove(); } catch { } };
    }

    /// <summary>Prépare la fenêtre pour une nouvelle carte.</summary>
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
        TxtNpc.Text = "";
        TxtAnswers.Text = "";
        if (!IsVisible) Show();
        Activate();
    }

    /// <summary>Met à jour le compteur de waypoints affiché.</summary>
    public void MajCompteur(int n)
        => TxtCompteur.Text = $"● REC — {n} waypoint{(n > 1 ? "s" : "")}";

    private void BtnFermer_Click(object sender, RoutedEventArgs e) => Hide();

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
        if (int.TryParse(TxtNpc.Text?.Trim(), out var npc) && npc != 0)
        {
            l.Npc = npc;
            foreach (var a in (TxtAnswers.Text ?? "")
                         .Split(',', StringSplitOptions.RemoveEmptyEntries))
                if (int.TryParse(a.Trim(), out var r)) l.Answers.Add(r);
        }
        Validee?.Invoke(this, l);
        Hide(); // réaffichée à la prochaine carte
    }

    private bool _fermetureReelle;

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // On masque au lieu de fermer (la fenêtre est réutilisée par carte),
        // sauf fermeture explicite de fin d'enregistrement.
        if (!_fermetureReelle) { e.Cancel = true; Hide(); }
        base.OnClosing(e);
    }

    /// <summary>Ferme réellement (fin d'enregistrement).</summary>
    public void FermerVraiment()
    {
        Validee = null;
        _fermetureReelle = true;
        try { Close(); } catch { /* déjà fermée */ }
    }
}
