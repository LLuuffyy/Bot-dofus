using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BotDofus.Commun.Reseau;
using BotDofus.Divers;
using BotDofus.Utilitaires.Config;
using BotDofus.Utilitaires.Journaux;
using Microsoft.Win32;

namespace BotDofus.Wpf;

public partial class MainWindow : Window
{
    public ObservableCollection<CompteVm> Comptes { get; } = new();
    private ContexteCompte? _contexteSelectionne;
    private readonly DispatcherTimer _timerRafraichissement;
    private readonly ConfigWpf _configWpf;

    public MainWindow()
    {
        Journaliseur.ActiverFichier(Path.Combine(AppContext.BaseDirectory, "logs"));
        InitializeComponent();
        LstComptes.ItemsSource = Comptes;
        _configWpf = ConfigWpf.Charger();
        ChargerComptesSauvegardes();

        // Timer 500ms pour MAJ stats UI (sinon on s'abonne Ã  PaquetRecu mais Ã§a peut spammer)
        _timerRafraichissement = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timerRafraichissement.Tick += (_, __) => RafraichirStatsHeader();
        _timerRafraichissement.Start();
    }

    private void BtnAjouterCompte_Click(object sender, RoutedEventArgs e)
    {
        // Dialogue minimal pour ajouter un compte
        var win = new AjouterCompteDialog { Owner = this };
        if (win.ShowDialog() == true && (!string.IsNullOrWhiteSpace(win.Identifiant) || !string.IsNullOrWhiteSpace(win.Login)))
        {
            var login = string.IsNullOrWhiteSpace(win.Login) ? win.Identifiant : win.Login;
            var alias = string.IsNullOrWhiteSpace(win.Identifiant) ? login : win.Identifiant;
            var entree = new EntreeCompte
            {
                Identifiant = login,
                MotDePasse = win.MotDePasse,
                Commentaire = alias == login ? string.Empty : alias
            };

            var vm = AjouterCompteDepuisEntree(entree, win.ModePassif);
            SauvegarderComptes();
            LstComptes.SelectedItem = vm;
        }
    }

    private void BtnDemarrerTous_Click(object sender, RoutedEventArgs e)
    {
        foreach (var c in Comptes)
        {
            try { c.Contexte.DemarrerProxy(); }
            catch (Exception ex) { Journaliseur.Avertir($"DÃ©marrage {c.Identifiant} Ã©chouÃ© : {ex.Message}"); }
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
            VueWorldMapTab.Lier(_contexteSelectionne);
            VueInvTab.Lier(_contexteSelectionne);
            VueCombatTab.Lier(_contexteSelectionne);
            VueToolsTab.Lier(_contexteSelectionne);
            VueScriptsTab.Lier(_contexteSelectionne);
            VueConfigTab.Lier(_contexteSelectionne);
            RafraichirStatsHeader();
        }
    }

    private void BtnConnecter_Click(object sender, RoutedEventArgs e)
    {
        try { _contexteSelectionne?.DemarrerProxy(); }
        catch (Exception ex) { MessageBox.Show($"Erreur : {ex.Message}", "DÃ©marrage", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void BtnLancerJeu_Click(object sender, RoutedEventArgs e)
    {
        var contexte = _contexteSelectionne ?? Comptes.FirstOrDefault()?.Contexte;
        if (contexte == null)
        {
            MessageBox.Show("Ajoute ou importe un compte avant de lancer le jeu.", "Lancer jeu", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            if (LstComptes.SelectedItem == null)
            {
                LstComptes.SelectedItem = Comptes.FirstOrDefault(c => c.Contexte == contexte);
            }

            contexte.ModePassif = true;
            contexte.DemarrerProxy();

            var cheminClientOriginal = ObtenirCheminClientDofus();
            if (string.IsNullOrWhiteSpace(cheminClientOriginal))
            {
                return;
            }

            FermerClientsDofusExistants(cheminClientOriginal);
            var cheminClient = ClientDofusPrepare.PreparerCopieLocale(cheminClientOriginal);

            Process.Start(new ProcessStartInfo
            {
                FileName = cheminClient,
                WorkingDirectory = Path.GetDirectoryName(cheminClient) ?? AppContext.BaseDirectory,
                UseShellExecute = true
            });

            Journaliseur.Info($"Client Dofus lance : {cheminClient}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Impossible de lancer le jeu : {ex.Message}", "Lancer jeu", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDeconnecter_Click(object sender, RoutedEventArgs e)
    {
        _contexteSelectionne?.ArreterProxy();
    }

    private void RafraichirStatsHeader()
    {
        if (_contexteSelectionne == null) return;
        var p = _contexteSelectionne.EtatJeu.Personnage;

        TxtNomPerso.Text = string.IsNullOrEmpty(p.Nom) ? "â€”" : p.Nom;
        TxtClassePerso.Text = p.Niveau > 0
            ? $"Classe #{p.IdClasse} Â· Niveau {p.Niveau}"
            : "Aucun perso connectÃ©";

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

    private void ChargerComptesSauvegardes()
    {
        var comptes = FichierComptes.Charger();
        if (comptes.Count == 0)
        {
            var cheminAccountsBot = TrouverAccountsBot();
            if (!string.IsNullOrWhiteSpace(cheminAccountsBot))
            {
                comptes = ImportateurAccountsBot.Importer(cheminAccountsBot);
                if (comptes.Count > 0)
                {
                    FichierComptes.Sauvegarder(comptes);
                }
            }
        }

        foreach (var entree in comptes.Where(c => !string.IsNullOrWhiteSpace(c.Identifiant)))
        {
            AjouterCompteDepuisEntree(entree, modePassif: true);
        }

        if (Comptes.Count > 0)
        {
            LstComptes.SelectedIndex = 0;
            try
            {
                Comptes[0].Contexte.ModePassif = true;
                Comptes[0].Contexte.DemarrerProxy();
            }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"Proxy automatique non demarre : {ex.Message}");
            }
        }
    }

    private CompteVm AjouterCompteDepuisEntree(EntreeCompte entree, bool modePassif)
    {
        var compte = new Compte(entree.Identifiant, entree.MotDePasse)
        {
            ServeurPrefere = entree.ServeurPrefere,
            PersonnagePrefere = entree.PersonnagePrefere
        };

        if (!string.IsNullOrWhiteSpace(entree.Commentaire))
        {
            compte.PseudoAffiche = entree.Commentaire;
        }

        var contexte = new ContexteCompte(compte, new ConfigReseau()) { ModePassif = modePassif };
        var vm = new CompteVm(contexte);
        Comptes.Add(vm);
        return vm;
    }

    private void SauvegarderComptes()
    {
        var comptes = Comptes.Select(vm => new EntreeCompte
        {
            Identifiant = vm.Contexte.Compte.Identifiant,
            MotDePasse = vm.Contexte.Compte.MotDePasse,
            ServeurPrefere = vm.Contexte.Compte.ServeurPrefere,
            PersonnagePrefere = vm.Contexte.Compte.PersonnagePrefere,
            Commentaire = vm.Contexte.Compte.PseudoAffiche ?? string.Empty
        }).ToList();

        FichierComptes.Sauvegarder(comptes);
    }

    private static string TrouverAccountsBot()
    {
        var candidats = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "accounts.bot"),
            Path.Combine(Environment.CurrentDirectory, "accounts.bot"),
            @"C:\Users\touki\Desktop\SynFus_Hystoria_v1.1.7\accounts.bot"
        };

        return candidats.FirstOrDefault(File.Exists) ?? string.Empty;
    }

    private string ObtenirCheminClientDofus()
    {
        var candidats = new List<string>();
        if (!string.IsNullOrWhiteSpace(_configWpf.CheminClientDofus))
        {
            candidats.Add(_configWpf.CheminClientDofus);
        }

        candidats.Add(@"C:\Users\touki\AppData\Local\Hystoria\Dofus\resources\app\retroclient\Dofus.exe");
        candidats.Add(@"C:\Users\touki\Desktop\SynFus_Hystoria_v1.1.7\Dofus.exe");

        var trouve = candidats.FirstOrDefault(File.Exists);
        if (!string.IsNullOrWhiteSpace(trouve))
        {
            _configWpf.CheminClientDofus = trouve;
            _configWpf.Sauvegarder();
            return trouve;
        }

        var dialogue = new OpenFileDialog
        {
            Title = "Choisir Dofus.exe",
            Filter = "Client Dofus|Dofus.exe|Executables|*.exe|Tous les fichiers|*.*",
            CheckFileExists = true
        };

        if (dialogue.ShowDialog(this) != true)
        {
            return string.Empty;
        }

        _configWpf.CheminClientDofus = dialogue.FileName;
        _configWpf.Sauvegarder();
        return dialogue.FileName;
    }

    private static void FermerClientsDofusExistants(string cheminClient)
    {
        var cheminNormalise = Path.GetFullPath(cheminClient);

        foreach (var processus in Process.GetProcesses())
        {
            try
            {
                var cheminProcessus = processus.MainModule?.FileName;
                var titre = processus.MainWindowTitle;
                var estClientDofus = string.Equals(cheminProcessus, cheminNormalise, StringComparison.OrdinalIgnoreCase)
                    || titre.Contains("Dofus", StringComparison.OrdinalIgnoreCase)
                    || (cheminProcessus?.Contains(@"\retroclient\", StringComparison.OrdinalIgnoreCase) == true
                        && cheminProcessus.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

                if (!estClientDofus)
                {
                    continue;
                }

                Journaliseur.Info($"Fermeture client Dofus existant : pid={processus.Id}, titre={titre}");
                if (!processus.CloseMainWindow() || !processus.WaitForExit(2500))
                {
                    processus.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Certains processus systeme refusent l'inspection du chemin.
            }
            finally
            {
                try { processus.Dispose(); } catch { }
            }
        }
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
        Contexte.SessionAuthActive != null ? "Auth" :
        Contexte.Proxy.EnEcoute ? "Proxy pret" : "Deconnecte";

    public Brush StatusBrush =>
        Contexte.SessionJeuActive != null ? Brushes.LimeGreen :
        Contexte.SessionAuthActive != null ? Brushes.Orange :
        Contexte.Proxy.EnEcoute ? Brushes.DeepSkyBlue : Brushes.DimGray;

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    public void Refresh()
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(NomPerso)));
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(StatusTexte)));
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(StatusBrush)));
    }
}

