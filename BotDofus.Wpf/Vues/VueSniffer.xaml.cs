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
    private bool _afficherPings;
    private const int LimiteLignes = 5000;
    private System.Windows.Controls.ScrollViewer? _scrollViewer;
    private ContexteCompte? _contexteLie;
    private bool _enPause;

    public VueSniffer()
    {
        InitializeComponent();
        GridPaquets.ItemsSource = Lignes;
        Loaded += (_, _) => _scrollViewer = TrouverScrollViewer(GridPaquets);

        // Roulette = on désactive auto-scroll pour pas snapper à chaque paquet.
        GridPaquets.PreviewMouseWheel += (_, _) => ChkAutoScroll.IsChecked = false;
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
        if (ReferenceEquals(_contexteLie, contexte))
        {
            return;
        }

        if (_contexteLie != null)
        {
            _contexteLie.PaquetRecu -= OnPaquet;
        }

        _contexteLie = contexte;
        contexte.PaquetRecu += OnPaquet;
    }

    private void OnPaquet(object? sender, EvenementPaquetRecu e)
    {
        if (_enPause) return;

        var paquet = e.Paquet;
        var sens = paquet.Direction == DirectionPaquet.VersClient ? "S->C" : "C->S";
        if (!Correspond(paquet.Prefixe, sens)) return;

        Dispatcher.Invoke(() =>
        {
            if (Lignes.Count >= LimiteLignes) Lignes.RemoveAt(0);

            var ligne = new LignePaquet
            {
                Heure = paquet.Horodatage.ToLocalTime().ToString("HH:mm:ss.fff"),
                Sens = sens,
                Prefixe = paquet.Prefixe,
                Taille = paquet.Contenu.Length.ToString(),
                // Contenu complet pour la copie ; la grille n'affichera qu'une ligne par défaut
                // mais le panneau "Détail" en bas montre tout, et Ctrl+C copie le contenu réel.
                Charge = paquet.Contenu,
                ContenuComplet = paquet.Contenu
            };

            Lignes.Add(ligne);

            TxtCount.Text = $"{Lignes.Count} paquets";
            // Auto-scroll : différé à DispatcherPriority.Background pour que le layout soit
            // recalculé APRÈS l'ajout du nouvel item, sinon ScrollToEnd ne voit pas le nouveau.
            if (ChkAutoScroll.IsChecked == true)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    GridPaquets.UpdateLayout();
                    GridPaquets.ScrollIntoView(ligne);
                    _scrollViewer ??= TrouverScrollViewer(GridPaquets);
                    _scrollViewer?.ScrollToEnd();
                }), System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
        });
    }

    private bool Correspond(string prefixe, string sens)
    {
        // Pings/keep-alives Hystoria : "CK" (cookie/check côté client) et "CP" (côté serveur).
        // Décochés par défaut car très bavards (un par seconde environ).
        // _afficherPings est un bool simple (mis à jour côté UI) pour pouvoir filtrer
        // depuis le thread de relai sans toucher la DependencyProperty ChkPings.IsChecked.
        if (!_afficherPings
            && (prefixe.StartsWith("CK", StringComparison.Ordinal)
             || prefixe.StartsWith("CP", StringComparison.Ordinal)))
        {
            return false;
        }

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

    private void BtnPause_Click(object sender, RoutedEventArgs e)
    {
        _enPause = BtnPause.IsChecked == true;
        BtnPause.Content = _enPause ? "▶ Reprendre" : "Pause";
    }

    private void ChkPings_Toggled(object sender, RoutedEventArgs e)
        => _afficherPings = ChkPings.IsChecked == true;

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
    public string Taille { get; set; } = "";
    public string Charge { get; set; } = "";
    public string ContenuComplet { get; set; } = "";
}
