using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BotDofus.Commun.Reseau;
using BotDofus.Divers;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Wpf;

public partial class MainWindow : Window
{
    public ObservableCollection<CompteVm> Comptes { get; } = new();
    private ContexteCompte? _contexteSelectionne;
    private readonly DispatcherTimer _timerRafraichissement;

    public MainWindow()
    {
        InitializeComponent();
        LstComptes.ItemsSource = Comptes;

        // Timer 500ms pour MAJ stats UI (sinon on s'abonne à PaquetRecu mais ça peut spammer)
        _timerRafraichissement = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timerRafraichissement.Tick += (_, __) => RafraichirStatsHeader();
        _timerRafraichissement.Start();
    }

    private void BtnAjouterCompte_Click(object sender, RoutedEventArgs e)
    {
        // Dialogue minimal pour ajouter un compte
        var win = new AjouterCompteDialog { Owner = this };
        if (win.ShowDialog() == true && !string.IsNullOrWhiteSpace(win.Identifiant))
        {
            var compte = new Compte(win.Identifiant, win.MotDePasse);
            if (!string.IsNullOrWhiteSpace(win.Login)) compte.PseudoAffiche = win.Login;
            var config = new ConfigReseau();
            var contexte = new ContexteCompte(compte, config) { ModePassif = win.ModePassif };

            var vm = new CompteVm(contexte);
            Comptes.Add(vm);
            LstComptes.SelectedItem = vm;
        }
    }

    private void BtnDemarrerTous_Click(object sender, RoutedEventArgs e)
    {
        foreach (var c in Comptes)
        {
            try { c.Contexte.DemarrerProxy(); }
            catch (Exception ex) { Journaliseur.Avertir($"Démarrage {c.Identifiant} échoué : {ex.Message}"); }
        }
    }

    private void LstComptes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstComptes.SelectedItem is CompteVm vm)
        {
            _contexteSelectionne = vm.Contexte;
            // Lier toutes les vues au nouveau contexte
            VueDash.Lier(_contexteSelectionne);
            VuePersoTab.Lier(_contexteSelectionne);
            VueSnif.Lier(_contexteSelectionne);
            VueMap.Lier(_contexteSelectionne);
            VueInvTab.Lier(_contexteSelectionne);
            VueCombatTab.Lier(_contexteSelectionne);
            VueScriptsTab.Lier(_contexteSelectionne);
            VueConfigTab.Lier(_contexteSelectionne);
            RafraichirStatsHeader();
        }
    }

    private void BtnConnecter_Click(object sender, RoutedEventArgs e)
    {
        try { _contexteSelectionne?.DemarrerProxy(); }
        catch (Exception ex) { MessageBox.Show($"Erreur : {ex.Message}", "Démarrage", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void BtnDeconnecter_Click(object sender, RoutedEventArgs e)
    {
        _contexteSelectionne?.ArreterProxy();
    }

    private void RafraichirStatsHeader()
    {
        if (_contexteSelectionne == null) return;
        var p = _contexteSelectionne.EtatJeu.Personnage;

        TxtNomPerso.Text = string.IsNullOrEmpty(p.Nom) ? "—" : p.Nom;
        TxtClassePerso.Text = p.Niveau > 0
            ? $"Classe #{p.IdClasse} · Niveau {p.Niveau}"
            : "Aucun perso connecté";

        BarVie.Maximum = Math.Max(p.VieMax, 1);
        BarVie.Value = p.Vie;
        TxtVie.Text = $"{p.Vie} / {p.VieMax}";

        BarEnergie.Maximum = Math.Max(p.EnergieMax, 1);
        BarEnergie.Value = p.Energie;
        TxtEnergie.Text = $"{p.Energie} / {p.EnergieMax}";

        BarXp.Value = p.PourcentageXp;
        TxtXp.Text = $"{p.PourcentageXp:F1} %";

        BarPoids.Maximum = Math.Max(p.PoidsMax, 1);
        BarPoids.Value = p.PoidsActuel;
        TxtPoids.Text = $"{p.PoidsActuel} / {p.PoidsMax}";

        // MAJ status visuel des comptes (couleur du cercle)
        foreach (var vm in Comptes) vm.Refresh();
    }
}

public sealed class CompteVm : System.ComponentModel.INotifyPropertyChanged
{
    public ContexteCompte Contexte { get; }

    public CompteVm(ContexteCompte ctx) { Contexte = ctx; }

    public string Identifiant => Contexte.Compte.Identifiant;
    public string NomPerso => Contexte.EtatJeu.Personnage.Nom;
    public string StatusTexte =>
        Contexte.SessionJeuActive != null ? "En jeu" :
        Contexte.SessionAuthActive != null ? "Auth" : "Déconnecté";

    public Brush StatusBrush =>
        Contexte.SessionJeuActive != null ? Brushes.LimeGreen :
        Contexte.SessionAuthActive != null ? Brushes.Orange : Brushes.DimGray;

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    public void Refresh()
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(NomPerso)));
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(StatusTexte)));
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(StatusBrush)));
    }
}
