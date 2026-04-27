using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using BotDofus.Commun.Reseau;
using BotDofus.Divers;
using BotDofus.Divers.Cartes.Entites;
using BotDofus.Divers.Donnees;

namespace BotDofus.Wpf.Vues;

public partial class VueTools : UserControl
{
    private ContexteCompte? _contexte;
    public ObservableCollection<OutilMapVm> Outils { get; } = new();

    public VueTools()
    {
        InitializeComponent();
        LstOutils.ItemsSource = Outils;
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

public sealed class OutilMapVm
{
    public int Identifiant { get; set; }
    public int Cellule { get; set; }
    public string Type { get; set; } = "";
    public string Titre { get; set; } = "";
    public string Details { get; set; } = "";
    public string? PaquetOuverture { get; set; }
}
