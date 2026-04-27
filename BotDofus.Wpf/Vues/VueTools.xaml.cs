using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BotDofus.Commun.Reseau;
using BotDofus.Divers;
using BotDofus.Divers.Cartes.Entites;
using BotDofus.Divers.Donnees;
using BotDofus.Utilitaires.Hystoria;

namespace BotDofus.Wpf.Vues;

public partial class VueTools : UserControl
{
    private ContexteCompte? _contexte;
    public ObservableCollection<OutilMapVm> Outils { get; } = new();
    public ObservableCollection<DiagnosticVm> Diagnostics { get; } = new();

    public VueTools()
    {
        InitializeComponent();
        LstOutils.ItemsSource = Outils;
        ListeDiagnostic.ItemsSource = Diagnostics;
        Loaded += (_, __) => RafraichirDiagnostic();
    }

    private void BtnDiagnostic_Click(object sender, RoutedEventArgs e) => RafraichirDiagnostic();

    private void RafraichirDiagnostic()
    {
        Diagnostics.Clear();

        // 1. Hosts file
        try
        {
            var hosts = new GestionnaireHosts(PatcheurCoreSwf.HostnameProxy);
            var installe = hosts.EstInstalle();
            Diagnostics.Add(new DiagnosticVm(installe, "Hosts file",
                installe ? $"{PatcheurCoreSwf.HostnameProxy} → 127.0.0.1" : "Entrée absente — lancer en admin"));
        }
        catch (Exception ex)
        {
            Diagnostics.Add(DiagnosticVm.Erreur("Hosts file", ex.Message));
        }

        // 2. SWF patché
        try
        {
            var racineHystoria = TrouverCoreSwf();
            if (racineHystoria == null)
            {
                Diagnostics.Add(DiagnosticVm.Erreur("Core.swf", "Client Hystoria introuvable"));
            }
            else
            {
                var patcheur = new PatcheurCoreSwf();
                var patche = patcheur.EstPatche(racineHystoria);
                Diagnostics.Add(new DiagnosticVm(patche, "Core.swf",
                    patche ? "Backup core_original.swf présent" : "Pas encore patché"));
            }
        }
        catch (Exception ex)
        {
            Diagnostics.Add(DiagnosticVm.Erreur("Core.swf", ex.Message));
        }

        // 3-5. Ports d'écoute (ports TCP en LISTEN local)
        var ports = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
        AjouterPortDiagnostic(ports, 450, "Auth proxy 450", "Port d'écoute auth Hystoria");
        AjouterPortDiagnostic(ports, 5556, "Game proxy 5556", "Port d'écoute jeu (créé après login)");
        AjouterPortDiagnostic(ports, 843, "Flash policy 843", "Server policy pour Flash");

        // 6. Bases de données
        var nbItems = BaseDonnees.Instance.Items.Count;
        var nbMobs = BaseDonnees.Instance.Monstres.Count;
        var nbMaps = BaseDonnees.Instance.Maps.Count;
        var basesOk = nbItems > 1000 && nbMobs > 100;
        Diagnostics.Add(new DiagnosticVm(basesOk, "Bases de donnees",
            $"{nbItems} items · {nbMobs} mobs · {nbMaps} maps"));

        // 7. Comptes chargés
        if (_contexte != null)
        {
            Diagnostics.Add(new DiagnosticVm(true, "Compte courant",
                $"{_contexte.Compte.Identifiant} ({(_contexte.ModePassif ? "passif" : "actif")})"));
        }
    }

    private static string? TrouverCoreSwf()
    {
        // Cherche le core.swf du client Hystoria dans des emplacements connus.
        var candidats = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Hystoria\Dofus\resources\app\retroclient\modules\core.swf"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Hystoria\Dofus\resources\app\retroclient\modules\core.swf"),
        };
        return candidats.FirstOrDefault(File.Exists);
    }

    private void AjouterPortDiagnostic(System.Net.IPEndPoint[] ports, int port, string libelle, string description)
    {
        var ouvert = ports.Any(p => p.Port == port);
        Diagnostics.Add(new DiagnosticVm(ouvert, libelle, ouvert ? "✓ en écoute" : "non écouté — démarrer le proxy"));
    }

    private void BtnInstallerHosts_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            new GestionnaireHosts(PatcheurCoreSwf.HostnameProxy).Ajouter();
            TxtDerniereAction.Text = "Hosts file modifié — entrée installée.";
            RafraichirDiagnostic();
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show("Lancer Luffy-bot.exe en tant qu'administrateur pour modifier le fichier hosts.",
                "Permissions insuffisantes", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur : {ex.Message}", "Hosts", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnRetirerHosts_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            new GestionnaireHosts(PatcheurCoreSwf.HostnameProxy).Retirer();
            TxtDerniereAction.Text = "Hosts file nettoyé.";
            RafraichirDiagnostic();
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show("Lancer Luffy-bot.exe en tant qu'administrateur pour modifier le fichier hosts.",
                "Permissions insuffisantes", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur : {ex.Message}", "Hosts", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void Lier(ContexteCompte contexte)
    {
        if (ReferenceEquals(_contexte, contexte))
        {
            Rafraichir();
            return;
        }

        if (_contexte != null)
        {
            _contexte.PaquetRecu -= OnPaquetRecu;
        }

        _contexte = contexte;
        contexte.PaquetRecu += OnPaquetRecu;
        Rafraichir();
    }

    private void OnPaquetRecu(object? sender, EvenementPaquetRecu e)
    {
        var contenu = e.Paquet.Contenu;
        if (!contenu.StartsWith("GM", StringComparison.Ordinal)
            && !contenu.StartsWith("GDM", StringComparison.Ordinal)
            && !contenu.StartsWith("GDK", StringComparison.Ordinal)
            && !contenu.StartsWith("GDF", StringComparison.Ordinal))
        {
            return;
        }

        Dispatcher.BeginInvoke(Rafraichir);
    }

    private void Rafraichir()
    {
        Outils.Clear();
        var carte = _contexte?.EtatJeu.CarteCourante;
        if (carte == null) return;

        var map = BaseDonnees.Instance.Map(carte.Identifiant);
        if (map != null)
        {
            TxtTravelX.Text = map.X.ToString();
            TxtTravelY.Text = map.Y.ToString();
        }

        foreach (var entite in carte.Entites.Values)
        {
            var type = entite switch
            {
                EntitePNJ => "PNJ",
                EntiteMonstre => "Monstre",
                EntiteJoueur => "Joueur",
                EntiteInteractif => "Interactif",
                _ => "Entite"
            };

            var extra = entite switch
            {
                EntitePNJ pnj => $"template={pnj.IdGabarit}",
                EntiteMonstre mob => $"template={mob.IdGabarit} niveau={mob.NiveauGroupe}",
                EntiteInteractif io => $"interactif={io.IdInteractif} etat={io.EtatBrut}",
                _ => ""
            };

            Outils.Add(new OutilMapVm
            {
                Identifiant = entite.Identifiant,
                Cellule = entite.CellulePosition,
                Type = type,
                Titre = $"{type} #{entite.Identifiant} {entite.Nom}",
                Details = $"cell={entite.CellulePosition} {extra}".Trim(),
                PaquetOuverture = entite is EntitePNJ ? $"DB{entite.Identifiant}" : null
            });
        }

        TxtDerniereAction.Text = $"{Outils.Count} entite(s) chargee(s)";
    }

    private async void BtnTravelCustom_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtTravelX.Text, out var x) || !int.TryParse(TxtTravelY.Text, out var y))
        {
            MessageBox.Show("Coordonnees invalides. Exemple : -1,-16", "Tools", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await EnvoyerTravel(x, y);
    }

    private async void BtnTravelBanque_Click(object sender, RoutedEventArgs e)
        => await EnvoyerTravel(4, -16);

    private async void BtnTravelHdv_Click(object sender, RoutedEventArgs e)
        => await EnvoyerTravel(5, -18);

    private void BtnBestiaire_Click(object sender, RoutedEventArgs e)
    {
        TxtDerniereAction.Text = $"Bestiaire charge : {BaseDonnees.Instance.Monstres.Count} monstres en base";
    }

    private async System.Threading.Tasks.Task EnvoyerTravel(int x, int y)
    {
        if (_contexte == null)
        {
            MessageBox.Show("Aucun compte selectionne.", "Tools", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await _contexte.Api.EnvoyerTravelAsync(x, y, CancellationToken.None);
        TxtDerniereAction.Text = $"Commande envoyee : .travel {x},{y}";
    }

    private async void BtnEnvoyerPaquet_Click(object sender, RoutedEventArgs e)
    {
        if (_contexte == null) return;
        var paquet = TxtPaquetBrut.Text?.Trim();
        if (string.IsNullOrWhiteSpace(paquet)) return;

        await _contexte.Api.EnvoyerPaquetBrutAsync(paquet, CancellationToken.None);
        TxtDerniereAction.Text = $"Paquet envoye : {paquet}";
    }

    private async void BtnEnvoyerChat_Click(object sender, RoutedEventArgs e)
        => await EnvoyerChat();

    private async void TxtChat_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await EnvoyerChat();
            e.Handled = true;
        }
    }

    private async System.Threading.Tasks.Task EnvoyerChat()
    {
        if (_contexte == null) return;
        var texte = TxtChat.Text?.Trim();
        if (string.IsNullOrWhiteSpace(texte)) return;

        var canal = (CmbCanal.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "*";
        await _contexte.Api.EnvoyerMessageAsync(canal, texte, CancellationToken.None);
        TxtDerniereAction.Text = $"Chat [{canal}] : {texte}";
        TxtChat.Clear();
    }

    private async void BtnQuickPos_Click(object sender, RoutedEventArgs e) => await EnvoyerCommandeRapide(".pos");
    private async void BtnQuickWho_Click(object sender, RoutedEventArgs e) => await EnvoyerCommandeRapide(".who");
    private async void BtnQuickStaff_Click(object sender, RoutedEventArgs e) => await EnvoyerCommandeRapide(".staff");
    private async void BtnQuickHelp_Click(object sender, RoutedEventArgs e) => await EnvoyerCommandeRapide(".help");

    private async System.Threading.Tasks.Task EnvoyerCommandeRapide(string commande)
    {
        if (_contexte == null)
        {
            TxtDerniereAction.Text = "Aucun compte selectionne.";
            return;
        }
        await _contexte.Api.EnvoyerMessageAsync("*", commande, CancellationToken.None);
        TxtDerniereAction.Text = $"Commande envoyee : {commande}";
    }

    private void TxtBestiaire_TextChanged(object sender, TextChangedEventArgs e)
    {
        var filtre = TxtBestiaire.Text?.Trim() ?? "";
        LstBestiaire.Items.Clear();
        if (filtre.Length < 2) return;

        var resultats = BaseDonnees.Instance.Monstres.Values
            .Where(m => m.Nom.Contains(filtre, StringComparison.OrdinalIgnoreCase)
                     || m.Identifiant.ToString().Equals(filtre, StringComparison.Ordinal))
            .OrderBy(m => m.Nom)
            .Take(50);

        foreach (var m in resultats)
        {
            LstBestiaire.Items.Add($"#{m.Identifiant,-4}  Niv.{m.Niveau,-3}  {m.Nom}");
        }

        if (LstBestiaire.Items.Count == 0)
        {
            LstBestiaire.Items.Add($"Aucun monstre trouve pour \"{filtre}\"");
        }
    }

    private void BtnRafraichir_Click(object sender, RoutedEventArgs e) => Rafraichir();

    private void BtnCopierId_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not OutilMapVm item) return;
        Clipboard.SetText($"{item.Type} id={item.Identifiant} cell={item.Cellule}");
        TxtDerniereAction.Text = $"ID copie : {item.Identifiant}";
    }

    private async void BtnOuvrirEntite_Click(object sender, RoutedEventArgs e)
    {
        if (_contexte == null || sender is not Button button || button.Tag is not OutilMapVm item) return;
        if (string.IsNullOrWhiteSpace(item.PaquetOuverture))
        {
            TxtDerniereAction.Text = "Cette entite n'a pas encore d'action directe.";
            return;
        }

        await _contexte.Api.EnvoyerPaquetBrutAsync(item.PaquetOuverture, CancellationToken.None);
        TxtDerniereAction.Text = $"Action envoyee : {item.PaquetOuverture}";
    }
}

public sealed class DiagnosticVm
{
    public string Icone { get; set; } = "";
    public string Libelle { get; set; } = "";
    public string Detail { get; set; } = "";
    public Brush Couleur { get; set; } = Brushes.White;

    public DiagnosticVm() { }

    public DiagnosticVm(bool ok, string libelle, string detail)
    {
        Libelle = libelle;
        Detail = detail;
        Icone = ok ? "●" : "○";
        Couleur = ok
            ? new SolidColorBrush(Color.FromRgb(0x65, 0xC5, 0x6F))
            : new SolidColorBrush(Color.FromRgb(0xE0, 0x6C, 0x6C));
    }

    public static DiagnosticVm Erreur(string libelle, string detail) => new()
    {
        Libelle = libelle,
        Detail = detail,
        Icone = "!",
        Couleur = new SolidColorBrush(Color.FromRgb(0xE0, 0x9C, 0x6C))
    };
}

public sealed class OutilMapVm
{
    public int Identifiant { get; set; }
    public int Cellule { get; set; }
    public string Type { get; set; } = "";
    public string Titre { get; set; } = "";
    public string Details { get; set; } = "";
    public string? PaquetOuverture { get; set; }
}
