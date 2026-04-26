using System;
using System.Collections.ObjectModel;
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

    public VueSniffer()
    {
        InitializeComponent();
        GridPaquets.ItemsSource = Lignes;
    }

    public void Lier(ContexteCompte contexte)
    {
        contexte.PaquetRecu += OnPaquet;
    }

    private void OnPaquet(object? sender, EvenementPaquetRecu e)
    {
        var paquet = e.Paquet;
        var sens = paquet.Direction == DirectionPaquet.VersClient ? "S→C" : "C→S";
        if (!Correspond(paquet.Prefixe, sens)) return;

        Dispatcher.Invoke(() =>
        {
            if (Lignes.Count >= LimiteLignes) Lignes.RemoveAt(0);
            Lignes.Add(new LignePaquet
            {
                Heure = paquet.Horodatage.ToLocalTime().ToString("HH:mm:ss.fff"),
                Sens = sens,
                Prefixe = paquet.Prefixe,
                Charge = paquet.Contenu.Length > 200 ? paquet.Contenu[..200] + "..." : paquet.Contenu
            });
            TxtCount.Text = $"{Lignes.Count} paquets";
            if (ChkAutoScroll.IsChecked == true && Lignes.Count > 0)
            {
                GridPaquets.ScrollIntoView(Lignes[Lignes.Count - 1]);
            }
        });
    }

    private bool Correspond(string prefixe, string sens)
    {
        if (!string.IsNullOrEmpty(_filtrePrefixe)
            && !prefixe.StartsWith(_filtrePrefixe, StringComparison.OrdinalIgnoreCase))
            return false;
        if (_filtreSens != "Tous" && _filtreSens != sens) return false;
        return true;
    }

    private void TxtFiltre_TextChanged(object sender, TextChangedEventArgs e)
        => _filtrePrefixe = TxtFiltre.Text ?? string.Empty;

    private void CmbDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => _filtreSens = (CmbDirection.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tous";

    private void BtnClear_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        Lignes.Clear();
        TxtCount.Text = "0 paquets";
    }
}

public sealed class LignePaquet
{
    public string Heure { get; set; } = "";
    public string Sens { get; set; } = "";
    public string Prefixe { get; set; } = "";
    public string Charge { get; set; } = "";
}
