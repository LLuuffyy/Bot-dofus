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
    // Note : netsh portproxy retiré du flux (architecturalement inefficace pour
    // rediriger une connexion SORTANTE vers une IP distante — il ne fait que du
    // listener inbound local). La redirection Abrak repose sur le patch config.xml.

    // Redirecteur WinDivert (Abrak) : interception packet-level, fermé sur OnClosed.
    private BotDofus.Utilitaires.Reseau.RedirecteurWinDivert? _redirecteurWd;

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
            : "2";

        // Aqua = serveur id 2 (vu dans le AH : "AH2;1;10;1|..."). Dialog WPF custom.
        var saisie = SaisieDialog.Demander(this, "Choix serveur",
            $"ID du serveur préféré pour {_contexteSelectionne.Compte.Identifiant}\n(Aqua = 2) :", actuel);

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

            // === REDIRECTION ABRAK = WinDivert (packet-level) ===
            // Le config.xml patch est PROUVÉ inefficace pour Abrak (client se connecte
            // direct à l'IP serveur, ignorée du config). Seule méthode fiable : WinDivert
            // intercepte les paquets sortants vers 51.89.153.20:1303/1304 et les
            // redirige sur notre proxy local 127.0.0.1 (anti-boucle via port marqueur).
            try
            {
                _redirecteurWd ??= new BotDofus.Utilitaires.Reseau.RedirecteurWinDivert(
                    ipServeur: "51.89.153.20", portAuth: 1303, portJeu: 1304, portMarqueur: 50303);
                if (!_redirecteurWd.Actif) _redirecteurWd.Demarrer();
                Journaliseur.Info("[WD] Interception WinDivert active — lance le jeu, ça sera redirigé.");
            }
            catch (Exception exWd)
            {
                MessageBox.Show(
                    $"WinDivert n'a pas pu démarrer :\n{exWd.Message}\n\n" +
                    $"→ Lance Luffy-bot en ADMINISTRATEUR (le driver réseau l'exige).",
                    "WinDivert", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // On lance le client Abrak normalement : WinDivert capte la connexion
            // quelle que soit la façon dont le client trouve son serveur.
            var lanceur = new LanceurDofus(cheminClientOriginal);
            lanceur.Lancer();
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
            Journaliseur.Info("[UI] Deconnexion demandee : proxy arrete, config.xml/portproxy nettoyes");
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
        // Arrête l'interception WinDivert (libère le driver / restaure le trafic normal).
        try { _redirecteurWd?.Dispose(); _redirecteurWd = null; }
        catch (Exception ex) { Journaliseur.Avertir($"Arrêt WinDivert : {ex.Message}"); }


        // Passe défensive : si le bot a planté en laissant un <connexionServers>
        // résiduel dans config.xml, on le vire.
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
        if (TxtQuotaComptes != null) TxtQuotaComptes.Text = $"{Comptes.Count}/200";
    }

    // ----------------------------------------------------------------
    // Navigation header (style MoonBot — boutons fonctionnels)
    // ----------------------------------------------------------------

    private void SelectionnerOnglet(string header)
    {
        foreach (var item in TabsContent.Items)
        {
            if (item is System.Windows.Controls.TabItem t && (t.Header as string) == header)
            {
                TabsContent.SelectedItem = t;
                return;
            }
        }
    }

    private void BtnParametres_Click(object sender, RoutedEventArgs e) => SelectionnerOnglet("Config");

    private void BtnAdmin_Click(object sender, RoutedEventArgs e) => SelectionnerOnglet("Tools");

    private void BtnDeconnecterTout_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            foreach (var vm in Comptes)
            {
                try { vm.Contexte.ArreterProxy(); } catch { }
            }
            NettoyerPatchEtHosts();
            Journaliseur.Info($"[UI] Déconnexion globale : {Comptes.Count} compte(s) arrêté(s)");
            RafraichirStatsHeader();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Déconnexion globale incomplète : {ex.Message}", "Déconnexion",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnDocs_Click(object sender, RoutedEventArgs e)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("LUFFY-BOT — RÉFÉRENCE RAPIDE");
        sb.AppendLine("════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine("CIBLE : Abrak (abrak.fr) — Dofus Retro, TCP brut");
        sb.AppendLine("  Auth : 51.89.153.20:1303");
        sb.AppendLine("  Jeu  : 51.89.153.20:1304");
        sb.AppendLine();
        sb.AppendLine("FLUX :");
        sb.AppendLine("  Lancer jeu → patch config.xml + netsh portproxy");
        sb.AppendLine("  → Abrak.exe → proxy MITM 127.0.0.1:1303 → serveur");
        sb.AppendLine("  → restore config.xml (furtif, hash inchangé)");
        sb.AppendLine();
        sb.AppendLine("ONGLETS :");
        sb.AppendLine("  Chat       : console + stats session");
        sb.AppendLine("  Personnage : stats + sorts appris");
        sb.AppendLine("  Combat     : config IA sorts + contrôle manuel");
        sb.AppendLine("  Carte      : terrain décodé (GDM)");
        sb.AppendLine("  Sniffer    : tous les paquets bruts");
        sb.AppendLine("  Tools      : chat, diagnostic, INTERCEPTION (Phase 2)");
        sb.AppendLine("  Scripts    : éditeur Lua + auto-script");
        sb.AppendLine();
        sb.AppendLine("INTERCEPTION FURTIVE (Phase 2) — API Lua :");
        sb.AppendLine("  bot.intercepter(nom, prefixe, fn)  fn(p,sens)->nil/\"\"/str");
        sb.AppendLine("  bot.intercepter_retirer(nom)");
        sb.AppendLine("  bot.intercepter_vider()");
        sb.AppendLine("  bot.intercepter_actif(bool)  -- kill-switch");
        sb.AppendLine();
        sb.AppendLine("API LUA (extrait) :");
        sb.AppendLine("  bot.vie() bot.pa() bot.pm() bot.kamas()");
        sb.AppendLine("  bot.carte() bot.position() bot.est_en_combat()");
        sb.AppendLine("  bot.deplacer(cell) bot.dire(canal,txt) bot.travel(x,y)");
        sb.AppendLine("  bot.config_combat_ajouter_sort_nom(nom,prio,cible)");
        sb.AppendLine();
        sb.AppendLine("SÉCURITÉ :");
        sb.AppendLine("  Mode passif coché = observe seulement (0 paquet injecté)");
        sb.AppendLine("  DetecteurStaff = pause auto si modo détecté");
        sb.AppendLine("  ConfigDelais = délais humanisés (Vue Config)");
        sb.AppendLine();
        sb.AppendLine("CLI : --testparsers (valide les parsers Aqua), --smoke");

        MessageBox.Show(sb.ToString(), "Luffy-bot — Documentation",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ChargerComptesSauvegardes()
    {
        var comptes = FichierComptes.Charger();
        if (comptes.Count == 0)
        {
            var cheminAccountsBot = TrouverAccountsBot();
            // On n'importe QUE si accounts.bot existe ET n'est pas vide. Un fichier
            // vide (état "tout supprimé") ne doit pas déclencher d'erreur d'import.
            if (!string.IsNullOrWhiteSpace(cheminAccountsBot)
                && File.Exists(cheminAccountsBot)
                && new FileInfo(cheminAccountsBot).Length > 4)
            {
                try
                {
                    comptes = ImportateurAccountsBot.Importer(cheminAccountsBot);
                    if (comptes.Count > 0)
                    {
                        FichierComptes.Sauvegarder(comptes);
                    }
                }
                catch (Exception ex)
                {
                    Journaliseur.Avertir($"[COMPTES] Import accounts.bot ignoré : {ex.Message}");
                    comptes = new System.Collections.Generic.List<EntreeCompte>();
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

    private void BtnSupprimerCompte_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.Tag is not CompteVm vm) return;

        var rep = MessageBox.Show(
            $"Supprimer le compte « {vm.Contexte.Compte.Identifiant} » ?",
            "Supprimer compte", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (rep != MessageBoxResult.Yes) return;

        try { vm.Contexte.ArreterProxy(); } catch { }
        try { vm.Contexte.Dispose(); } catch { }

        Comptes.Remove(vm);
        if (ReferenceEquals(_contexteSelectionne, vm.Contexte)) _contexteSelectionne = null;

        SauvegarderComptes();
        Journaliseur.Info($"[UI] Compte supprimé : {vm.Contexte.Compte.Identifiant}");

        if (Comptes.Count > 0) LstComptes.SelectedIndex = 0;
        RafraichirStatsHeader();
    }

    private void BtnSupprimerTousComptes_Click(object sender, RoutedEventArgs e)
    {
        var rep = MessageBox.Show(
            $"Supprimer TOUS les comptes ({Comptes.Count}) et repartir de zéro ?\n" +
            "comptes.json et accounts.bot seront vidés.",
            "Tout supprimer", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (rep != MessageBoxResult.Yes) return;

        foreach (var vm in Comptes.ToList())
        {
            try { vm.Contexte.ArreterProxy(); } catch { }
            try { vm.Contexte.Dispose(); } catch { }
        }
        Comptes.Clear();
        _contexteSelectionne = null;

        // Vide les deux sources de persistance pour éviter tout réimport au prochain lancement.
        try
        {
            FichierComptes.Sauvegarder(new System.Collections.Generic.List<EntreeCompte>());
            foreach (var chemin in new[]
            {
                Path.Combine(AppContext.BaseDirectory, "accounts.bot"),
                Path.Combine(Environment.CurrentDirectory, "accounts.bot"),
            })
            {
                if (File.Exists(chemin)) File.WriteAllText(chemin, string.Empty);
            }
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[UI] Nettoyage persistance comptes : {ex.Message}");
        }

        Journaliseur.Info("[UI] Tous les comptes supprimés (liste + comptes.json + accounts.bot vidés)");
        RafraichirStatsHeader();
        MessageBox.Show("Tous les comptes ont été supprimés. Tu peux repartir propre avec « + ».",
            "Tout supprimer", MessageBoxButton.OK, MessageBoxImage.Information);
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

        // PRIORITÉ ABSOLUE : Abrak (launcher Electron, Roaming) — cible courante.
        // On ne consulte le chemin mémorisé qu'EN DERNIER pour éviter une régression
        // silencieuse depuis une ancienne config Aqua/Hystoria (config-wpf.json).
        var abrakDefaut = BotDofus.Utilitaires.Aqua.PatcheurConfigXml.CheminExecutableDefaut;
        candidats.Add(abrakDefaut);

        // Chemin mémorisé d'une session précédente — accepté SAUF s'il pointe vers
        // un ancien serveur (Hystoria / SynFus / Aqua-Bubble) → auto-migration Abrak.
        if (!string.IsNullOrWhiteSpace(_configWpf.CheminClientDofus)
            && !_configWpf.CheminClientDofus.Contains("Hystoria", StringComparison.OrdinalIgnoreCase)
            && !_configWpf.CheminClientDofus.Contains("SynFus", StringComparison.OrdinalIgnoreCase)
            && !_configWpf.CheminClientDofus.Contains(@"Bubble\Aqua", StringComparison.OrdinalIgnoreCase))
        {
            candidats.Add(_configWpf.CheminClientDofus);
        }

        // Anciennes cibles gardées en dernier secours.
        candidats.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Bubble", "Aqua", "Dofus.exe"));
        candidats.Add(@"C:\Users\touki\AppData\Local\Hystoria\Dofus\resources\app\retroclient\Dofus.exe");

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
