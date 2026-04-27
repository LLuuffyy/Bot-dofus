using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using BotDofus.Divers;
using BotDofus.Divers.Donnees;

namespace BotDofus.Wpf.Vues;

public partial class VueInventaire : UserControl
{
    private ContexteCompte? _contexte;
    public ObservableCollection<LigneObjet> Lignes { get; } = new();
    private string _filtre = "";

    public VueInventaire()
    {
        InitializeComponent();
        GridObjets.ItemsSource = Lignes;
    }

    public void Lier(ContexteCompte ctx)
    {
        _contexte = ctx;
        // Refresh ciblé : seulement quand l'inventaire change réellement (event dédié),
        // pas sur chaque paquet réseau (économie CPU sur les sessions longues).
        ctx.EtatJeu.Personnage.InventaireChange += (_, __) => Dispatcher.Invoke(Rafraichir);
        Rafraichir();
    }

    private void Rafraichir()
    {
        if (_contexte == null) return;
        Lignes.Clear();
        foreach (var obj in _contexte.EtatJeu.Personnage.Inventaire)
        {
            var nom = BaseDonnees.Instance.Item(obj.IdTemplate)?.Nom ?? $"Item #{obj.IdTemplate}";
            if (!string.IsNullOrEmpty(_filtre) && !nom.Contains(_filtre, StringComparison.OrdinalIgnoreCase)) continue;
            Lignes.Add(new LigneObjet
            {
                Identifiant = obj.Identifiant,
                IdTemplate = obj.IdTemplate,
                Nom = nom,
                Quantite = obj.Quantite,
                PositionTexte = obj.Position == 63 ? "NOT_EQUIPPED" : $"Pos {obj.Position}"
            });
        }
        TxtCount.Text = $"{Lignes.Count} objets";
    }

    private void TxtFiltre_TextChanged(object sender, TextChangedEventArgs e)
    {
        _filtre = TxtFiltre.Text ?? "";
        Rafraichir();
    }

    private void BtnRafraichir_Click(object sender, RoutedEventArgs e) => Rafraichir();
}

public sealed class LigneObjet
{
    public int Identifiant { get; set; }
    public int IdTemplate { get; set; }
    public string Nom { get; set; } = "";
    public int Quantite { get; set; }
    public string PositionTexte { get; set; } = "";
}
