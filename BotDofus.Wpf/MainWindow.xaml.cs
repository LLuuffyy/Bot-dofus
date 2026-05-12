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
using BotDofus.Utilitaires.Hystoria;
using BotDofus.Utilitaires.Journaux;
using Microsoft.Win32;

namespace BotDofus.Wpf;

public partial class MainWindow : Window
{
    public ObservableCollection<CompteVm> Comptes { get; } = new();
    private ContexteCompte? _contexteSelectionne;
    private readonly DispatcherTimer _timerRafraichissement;
    private readonly ConfigWpf _configWpf;

    // Migration Aqua : on patche maintenant config.xml via PatcheurConfigXml qui se
    // restaure tout seul dans son finally. Plus de PatcheurCoreSwf / GestionnaireHosts
    // (gardés dans le repo pour référence Hystoria, voir Utilitaires/Hystoria/).
    //
    // Backup défensif : si l'SWF du client tombe quand même sur l'API distante,
    // on a un netsh portproxy au niveau noyau qui redirige 141.94.99.2:7781 → 127.0.0.1:7781.
    // Nettoyé dans OnClosed.
    private BotDofus.Utilitaires.Aqua.RedirecteurPortProxy? _redirecteurPortProxy;

    public MainWindow()
    {
        Journaliseur.ActiverFichier(Path.Combine(AppContext.BaseDirectory, "logs"));
        InitializeComponent();
        LstComptes.ItemsSource = Comptes;
        _configWpf = ConfigWpf.Charger();
        ChargerComptesSauvegardes();

        // Timer 500ms pour MAJ stats UI (sinon on s'abonne a PaquetRecu mais ça peut spammer)
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

    private void BtnAjouterCompteCourant_Click(object sender, RoutedEventArgs e)
    {
        // Cherche un contexte qui a capturé un login depuis le jeu
        var contexteAvecLogin = _contexteSelectionne is { LoginCapture: not null }
            ? _contexteSelectionne
            : Comptes.Select(c => c.Contexte).FirstOrDefault(c => c.LoginCapture != null);

        if (contexteAvecLogin?.LoginCapture == null)
        {
            MessageBox.Show(
                "Aucun login capturé pour le moment. Lance Dofus, connecte-toi avec ton vrai compte, puis re-clique ici.",
                "Ajouter compte courant", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var login = contexteAvecLogin.LoginCapture;

        // Évite le doublon : si un compte avec ce login existe déjà, on ne re-ajoute pas.
        if (Comptes.Any(c => string.Equals(c.Contexte.Compte.Identifiant, login, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show($"Le compte « {login} » est déjà dans la liste.",
                "Ajouter compte courant", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new AjouterCompteDialog(login) { Owner = this };
        if (dlg.ShowDialog() == true && (!string.IsNullOrWhiteSpace(dlg.Login) || !string.IsNullOrWhiteSpace(dlg.Identifiant)))
        {
            var loginFinal = string.IsNullOrWhiteSpace(dlg.Login) ? dlg.Identifiant : dlg.Login;
            var alias = string.IsNullOrWhiteSpace(dlg.Identifiant) ? loginFinal : dlg.Identifiant;
            var entree = new EntreeCompte
            {
                Identifiant = loginFinal,
                MotDePasse = dlg.MotDePasse,
                Commentaire = alias == loginFinal ? string.Empty : alias
            };
            var vm = AjouterCompteDepuisEntree(entree, dlg.ModePassif);
            SauvegarderComptes();
            LstComptes.SelectedItem = vm;
            Journaliseur.Info($"[AUTO-ADD] Compte « {loginFinal} » ajouté depuis le jeu courant");
        }
    }

    private void BtnDemarrerTous_Click(object sender, RoutedEventArgs e)
    {
        // On NE démarre PLUS tous les comptes (collision garantie sur port 450). On démarre
        // seulement le compte actuellement sélectionné, ou le premier à défaut.
        var contexte = _contexteSelectionne ?? Comptes.FirstOrDefault()?.Contexte;
        if (contexte == null)
        {
            MessageBox.Show("Aucun compte disponible. Ajoute un compte d'abord.", "Démarrer compte",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            contexte.ModePassif = ChkModePassif?.IsChecked == true;
            contexte.DemarrerProxy();
            Journaliseur.Info($"[UI] Demarrage compte selectionne : {contexte.Compte.Identifiant} (mode {(contexte.ModePassif ? "passif" : "actif")})");
            RafraichirStatsHeader();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Démarrage {contexte.Compte.Identifiant} échoué : {ex.Message}",
                "Démarrer compte", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnChoixServeur_Click(object sender, RoutedEventArgs e)
    {
        if (_contexteSelectionne == null)
        {
            MessageBox.Show("Sélectionne un compte d'abord.", "Choix serveur",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var actuel = _contexteSelectionne.Compte.ServeurPrefere > 0
            ? _contexteSelectionne.Compte.ServeurPrefere.ToString()
            : "601";

        // Hystoria n'a qu'un serveur (id=601). Dialog WPF custom, pas le VB InputBox qui rend mal.
        var saisie = SaisieDialog.Demander(this, "Choix serveur",
            $"ID du serveur préféré pour {_contexteSelectionne.Compte.Identifiant}\n(Hystoria = 601) :", actuel);

        if (string.IsNullOrWhiteSpace(saisie)) return;
        if (!int.TryParse(saisie, out var idServeur) || idServeur <= 0)
        {
            MessageBox.Show("ID invalide. Doit être un nombre positif.", "Choix serveur",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _contexteSelectionne.Compte.ServeurPrefere = idServeur;
        SauvegarderComptes();
        Journaliseur.Info($"[CONFIG] Serveur préféré de {_contexteSelectionne.Compte.Identifiant} = #{idServeur}");
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
        // Toggle : si le proxy tourne, on l'arrête. Sinon on le démarre.
        var contexte = _contexteSelectionne;
        if (contexte == null)
        {
            MessageBox.Show("Sélectionne un compte d'abord.", "Proxy",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            if (contexte.Proxy.EnEcoute)
            {
                contexte.ArreterProxy();
                Journaliseur.Info($"[UI] Proxy arrêté pour {contexte.Compte.Identifiant}");
            }
            else
            {
                contexte.ModePassif = ChkModePassif?.IsChecked == true;
                contexte.DemarrerProxy();
                Journaliseur.Info($"[UI] Proxy démarré pour {contexte.Compte.Identifiant} (mode {(contexte.ModePassif ? "passif" : "actif")})");
            }

            RafraichirStatsHeader();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur : {ex.Message}", "Proxy", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnLancerJeu_Click(object sender, RoutedEventArgs e)
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

            // Le ModePassif est piloté par le ChkModePassif (checkbox du header) — pas de hardcoding ici.
            contexte.ModePassif = ChkModePassif?.IsChecked == true;

            // Démarre les listeners AVANT de patcher le config, sinon le client se connecte
            // à 127.0.0.1:7781 et trouve port fermé → kick instantané.
            contexte.DemarrerProxy();

            var cheminClientOriginal = ObtenirCheminClientDofus();
            if (string.IsNullOrWhiteSpace(cheminClientOriginal))
            {
                return;
            }

            FermerClientsDofusExistants(cheminClientOriginal);

            // === MIGRATION AQUA ===
            // On ne patche plus core.swf : le client Aqua lit son IP serveur depuis
            // config.xml (élément <conf>/<connexionServers>/<connserver>).
            // PatcheurConfigXml injecte le bloc, lance Dofus.exe, attend 15s pour
            // que Flash ait lu, puis restaure le config original.
            var configXmlPath = BotDofus.Utilitaires.Aqua.PatcheurConfigXml.CheminConfigDefaut;
            if (!File.Exists(configXmlPath))
            {
                // Si le user n'a pas le launcher Bubble, on lance Dofus.exe tel quel
                // (en supposant qu'il l'a redirigé autrement, ex. Synfus en parallèle).
                Journaliseur.Avertir($"[AQUA] config.xml introuvable ({configXmlPath}) — lancement direct sans patch");
                var lanceurDirect = new LanceurDofus(cheminClientOriginal);
                lanceurDirect.Lancer();
                return;
            }

            var patcheur = new BotDofus.Utilitaires.Aqua.PatcheurConfigXml(
                ipLocale: "127.0.0.1",
                portLocal: 7781,
                cheminConfig: configXmlPath);

            // Ceinture-et-bretelles : on tente aussi un netsh portproxy au cas où le SWF
            // ignorerait notre <connserver> et retomberait sur l'API runtime ankama_acc
            // (qui résoudrait à 141.94.99.2). Cette règle système redirige tout trafic
            // sortant Dofus → 141.94.99.2:7781 vers 127.0.0.1:7781.
            // Si admin manquant : non-bloquant, on log un warning et on continue.
            _redirecteurPortProxy ??= new BotDofus.Utilitaires.Aqua.RedirecteurPortProxy(
                ipDistante: "141.94.99.2", portDistant: 7781, portLocal: 7781);
            try { _redirecteurPortProxy.Ajouter(); }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[AQUA] netsh portproxy non installé ({ex.Message}). " +
                                     $"Tu peux le lancer en admin manuellement, OU compter sur le patch config.xml seul.");
                _redirecteurPortProxy = null;
            }

            // Workflow async : Patcher() → lancer Dofus.exe → attente → Restaurer().
            // L'await ici libère le thread UI pendant la fenêtre de 15s.
            await patcheur.LancerClientAsync(cheminClientOriginal);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Impossible de lancer le jeu : {ex.Message}", "Lancer jeu", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDeconnecter_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _contexteSelectionne?.ArreterProxy();
            NettoyerPatchEtHosts();
            Journaliseur.Info("[UI] Deconnexion demandee : proxy arrete, core.swf/hosts nettoyes");
            RafraichirStatsHeader();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Déconnexion incomplète : {ex.Message}", "Déconnexion",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ChkModePassif_Toggle(object sender, RoutedEventArgs e)
    {
        // Propage l'état du checkbox à tous les contextes : décoché = bot tente l'auth lui-même
        // (mode actif, à utiliser uniquement avec un client Dofus qui ne tape PAS le mdp en parallèle).
        var passif = ChkModePassif.IsChecked == true;
        foreach (var c in Comptes)
        {
            c.Contexte.ModePassif = passif;
        }
        Journaliseur.Info($"Mode {(passif ? "PASSIF" : "ACTIF")} appliqué à {Comptes.Count} compte(s)");
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Stoppe tous les proxies (auth + jeu) avant le cleanup système.
        foreach (var c in Comptes)
        {
            try { c.Contexte.ArreterProxy(); } catch { }
        }

        NettoyerPatchEtHosts();
    }

    private void NettoyerPatchEtHosts()
    {
        // Retire la règle netsh portproxy si elle est en place.
        try { _redirecteurPortProxy?.Dispose(); _redirecteurPortProxy = null; }
        catch (Exception ex) { Journaliseur.Avertir($"Retrait portproxy : {ex.Message}"); }

        // Migration Aqua : le PatcheurConfigXml restaure config.xml dans son propre finally
        // (cf. LancerClientAsync). Si pour une raison X le bot a planté entre Patcher()
        // et la restauration, on tente une dernière passe défensive ici en relisant
        // config.xml et en virant tout <connexionServers> traînant.
        try
        {
            var chemin = BotDofus.Utilitaires.Aqua.PatcheurConfigXml.CheminConfigDefaut;
            if (!File.Exists(chemin)) return;

            var doc = System.Xml.Linq.XDocument.Load(chemin);
            var blocs = doc.Root?.Element("conf")?.Elements("connexionServers").ToList();
            if (blocs != null && blocs.Count > 0)
            {
                foreach (var b in blocs) b.Remove();
                File.WriteAllText(chemin, doc.ToString());
                Journaliseur.Info("[AQUA-PATCH] config.xml nettoyé au shutdown (résidu détecté)");
            }
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"Nettoyage config.xml : {ex.Message}");
        }
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

        TxtProxyButton.Text = _contexteSelectionne.Proxy.EnEcoute ? "Stop proxy" : "Proxy";
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

        // Lecture de l'état initial du checkbox (par défaut coché = passif au démarrage).
        var passifInitial = ChkModePassif?.IsChecked == true;
        foreach (var entree in comptes.Where(c => !string.IsNullOrWhiteSpace(c.Identifiant)))
        {
            AjouterCompteDepuisEntree(entree, modePassif: passifInitial);
        }

        if (Comptes.Count > 0)
        {
            LstComptes.SelectedIndex = 0;
            try
            {
                Comptes[0].Contexte.ModePassif = passifInitial;
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

        // Aqua (Bubble launcher) — cible courante
        candidats.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Bubble", "Aqua", "Dofus.exe"));
        // Anciennes cibles (Hystoria) gardées en fallback si jamais
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
