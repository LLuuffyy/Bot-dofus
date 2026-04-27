using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using BotDofus.Commun.Reseau;
using BotDofus.Divers;

namespace BotDofus.Wpf.Vues;

public partial class VueSniffer : UserControl
{
    public ObservableCollection<LignePaquet> Lignes { get; } = new();
    private string _filtrePrefixe = string.Empty;
    private string _filtreSens = "Tous";
    private const int LimiteLignes = 5000;
    private System.Windows.Controls.ScrollViewer? _scrollViewer;

    public VueSniffer()
    {
        InitializeComponent();
        GridPaquets.ItemsSource = Lignes;
        Loaded += (_, _) => _scrollViewer = TrouverScrollViewer(GridPaquets);

        // Quand l'utilisateur utilise la roulette ou clique sur la scrollbar, on décoche
        // Auto-scroll automatiquement. Sans ça, le snap auto-scroll au moindre nouveau paquet
        // empêche toute lecture/copie. L'utilisateur recoche manuellement quand il veut suivre live.
        GridPaquets.PreviewMouseWheel += (_, _) => ChkAutoScroll.IsChecked = false;
        GridPaquets.PreviewMouseDown += (_, e) =>
        {
            // Drag scrollbar = clic dans le ScrollViewer hors des cellules ; on désactive aussi.
            if (e.OriginalSource is System.Windows.Controls.Primitives.Thumb
                || e.OriginalSource is System.Windows.Controls.Primitives.RepeatButton)
            {
                ChkAutoScroll.IsChecked = false;
            }
        };
    }

    private static System.Windows.Controls.ScrollViewer? TrouverScrollViewer(System.Windows.DependencyObject racine)
    {
        if (racine is System.Windows.Controls.ScrollViewer sv) return sv;
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(racine); i++)
        {
            var enfant = System.Windows.Media.VisualTreeHelper.GetChild(racine, i);
            var trouve = TrouverScrollViewer(enfant);
            if (trouve != null) return trouve;
        }
        return null;
    }

    public void Lier(ContexteCompte contexte)
    {
        contexte.PaquetRecu += OnPaquet;
    }

    private void OnPaquet(object? sender, EvenementPaquetRecu e)
    {
        var paquet = e.Paquet;
        var sens = paquet.Direction == DirectionPaquet.VersClient ? "S->C" : "C->S";
        if (!Correspond(paquet.Prefixe, sens)) return;

        Dispatcher.Invoke(() =>
        {
            if (Lignes.Count >= LimiteLignes) Lignes.RemoveAt(0);

            Lignes.Add(new LignePaquet
            {
                Heure = paquet.Horodatage.ToLocalTime().ToString("HH:mm:ss.fff"),
                Sens = sens,
                Prefixe = paquet.Prefixe,
                // Contenu complet pour la copie ; la grille n'affichera qu'une ligne par défaut
                // mais le panneau "Détail" en bas montre tout, et Ctrl+C copie le contenu réel.
                Charge = paquet.Contenu,
                ContenuComplet = paquet.Contenu
            });

            TxtCount.Text = $"{Lignes.Count} paquets";
            // Auto-scroll : différé à DispatcherPriority.Background pour que le layout soit
            // recalculé APRÈS l'ajout du nouvel item, sinon ScrollToEnd ne voit pas le nouveau.
            if (ChkAutoScroll.IsChecked == true && _scrollViewer != null)
            {
                var sv = _scrollViewer;
                Dispatcher.BeginInvoke(new Action(sv.ScrollToEnd), System.Windows.Threading.DispatcherPriority.Background);
            }
        });
    }

    private bool Correspond(string prefixe, string sens)
    {
        if (!string.IsNullOrEmpty(_filtrePrefixe)
            && !prefixe.StartsWith(_filtrePrefixe, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (_filtreSens != "Tous" && _filtreSens != sens)
        {
            return false;
        }

        return true;
    }

    private void TxtFiltre_TextChanged(object sender, TextChangedEventArgs e)
        => _filtrePrefixe = TxtFiltre.Text ?? string.Empty;

    private void CmbDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => _filtreSens = (CmbDirection.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tous";

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        Lignes.Clear();
        TxtCount.Text = "0 paquets";
        TxtDetail.Text = "(sélectionne une ligne ci-dessus pour voir le contenu complet)";
    }

    private void GridPaquets_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Quand l'utilisateur sélectionne une ligne, on dump le contenu complet du paquet
        // dans le TextBox de détail (long, copiable, scrollable).
        if (GridPaquets.SelectedItem is LignePaquet ligne)
        {
            TxtDetail.Text = $"[{ligne.Heure}] {ligne.Sens} {ligne.Prefixe}\n{ligne.ContenuComplet}";
        }
    }

    private void BtnCopierDetail_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TxtDetail.Text))
        {
            try { Clipboard.SetText(TxtDetail.Text); }
            catch { /* clipboard occupé, ignoré */ }
        }
    }
}

public sealed class LignePaquet
{
    public string Heure { get; set; } = "";
    public string Sens { get; set; } = "";
    public string Prefixe { get; set; } = "";
    public string Charge { get; set; } = "";
    public string ContenuComplet { get; set; } = "";
}
