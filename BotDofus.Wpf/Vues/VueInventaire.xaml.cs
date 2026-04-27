using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BotDofus.Divers;
using BotDofus.Divers.Donnees;
using BotDofus.Divers.Jeu.Personnage;

namespace BotDofus.Wpf.Vues;

public partial class VueInventaire : UserControl
{
    private ContexteCompte? _contexte;
    private Personnage? _personnageLie;
    public ObservableCollection<LigneObjet> Lignes { get; } = new();
    private string _filtre = "";

    public VueInventaire()
    {
        InitializeComponent();
        GridObjets.ItemsSource = Lignes;
    }

    public void Lier(ContexteCompte ctx)
    {
        if (ReferenceEquals(_contexte, ctx)) return;

        if (_personnageLie != null)
        {
            _personnageLie.InventaireChange -= Personnage_Change;
            _personnageLie.Mis_A_Jour -= Personnage_Change;
        }

        _contexte = ctx;
        _personnageLie = ctx.EtatJeu.Personnage;
        _personnageLie.InventaireChange += Personnage_Change;
        _personnageLie.Mis_A_Jour += Personnage_Change;
        Rafraichir();
    }

    private void Personnage_Change(object? sender, EventArgs e)
        => Dispatcher.BeginInvoke(new Action(Rafraichir), System.Windows.Threading.DispatcherPriority.Background);

    private void Rafraichir()
    {
        if (_contexte == null) return;
        var perso = _contexte.EtatJeu.Personnage;
        Lignes.Clear();
        var totalPods = 0;
        var equipes = 0;

        foreach (var obj in perso.Inventaire
                     .OrderBy(o => o.Position == 63 ? 1 : 0)
                     .ThenBy(o => o.Position)
                     .ThenBy(o => o.IdTemplate))
        {
            var info = BaseDonnees.Instance.Item(obj.IdTemplate);
            var nom = string.IsNullOrWhiteSpace(info?.Nom) ? $"Item #{obj.IdTemplate}" : info!.Nom;
            var type = NomTypeItem(info?.IdType ?? 0);
            var position = NomPosition(obj.Position);
            var poidsUnitaire = info?.Poids ?? 0;
            var poidsTotal = poidsUnitaire * Math.Max(1, obj.Quantite);

            totalPods += poidsTotal;
            if (obj.Position != 63) equipes++;

            if (!CorrespondFiltre(obj, nom, type, position)) continue;

            Lignes.Add(new LigneObjet
            {
                Identifiant = obj.Identifiant,
                IdTemplate = obj.IdTemplate,
                Nom = nom,
                Type = type,
                Niveau = info?.Niveau ?? 0,
                Quantite = obj.Quantite,
                PoidsUnitaire = poidsUnitaire,
                PoidsTotal = poidsTotal,
                PositionTexte = position,
                Etat = obj.Position == 63 ? "Sac" : "Equipe",
                IdGfx = info?.IdGfx ?? 0,
                Details = ConstruireDetails(obj, info, position),
                IconeTexte = IconeTexte(info?.IdType ?? 0, nom),
                IconeBrush = new SolidColorBrush(CouleurType(info?.IdType ?? 0))
            });
        }

        TxtCount.Text = $"{Lignes.Count} / {perso.Inventaire.Count}";
        TxtEquipes.Text = equipes.ToString();
        TxtPodsObjets.Text = totalPods.ToString("N0");
        TxtPodsPerso.Text = $"{perso.PoidsActuel:N0} / {perso.PoidsMax:N0}";
    }

    private bool CorrespondFiltre(ObjetInventaire obj, string nom, string type, string position)
    {
        if (string.IsNullOrWhiteSpace(_filtre)) return true;
        return nom.Contains(_filtre, StringComparison.OrdinalIgnoreCase)
            || type.Contains(_filtre, StringComparison.OrdinalIgnoreCase)
            || position.Contains(_filtre, StringComparison.OrdinalIgnoreCase)
            || obj.Quantite.ToString().Contains(_filtre, StringComparison.OrdinalIgnoreCase)
            || obj.IdTemplate.ToString().Contains(_filtre, StringComparison.OrdinalIgnoreCase)
            || obj.Identifiant.ToString().Contains(_filtre, StringComparison.OrdinalIgnoreCase);
    }

    private void TxtFiltre_TextChanged(object sender, TextChangedEventArgs e)
    {
        _filtre = TxtFiltre.Text ?? "";
        Rafraichir();
    }

    private void BtnRafraichir_Click(object sender, RoutedEventArgs e) => Rafraichir();

    private static string NomPosition(int position) => position switch
    {
        0 => "Amulette",
        1 => "Arme",
        2 => "Anneau gauche",
        3 => "Ceinture",
        4 => "Anneau droit",
        5 => "Bottes",
        6 => "Chapeau",
        7 => "Cape",
        8 => "Familier",
        9 => "Dofus 1",
        10 => "Dofus 2",
        11 => "Dofus 3",
        12 => "Dofus 4",
        13 => "Dofus 5",
        14 => "Dofus 6",
        15 => "Bouclier",
        63 => "Sac",
        _ => $"Pos {position}"
    };

    private static string NomTypeItem(int type) => type switch
    {
        1 => "Amulette",
        2 => "Arc",
        3 => "Baguette",
        4 => "Baton",
        5 => "Dague",
        6 => "Epee",
        7 => "Marteau",
        8 => "Pelle",
        9 => "Anneau",
        10 => "Ceinture",
        11 => "Bottes",
        16 => "Chapeau",
        17 => "Cape",
        18 => "Familier",
        19 => "Hache",
        20 => "Outil",
        21 => "Pioche",
        22 => "Faux",
        23 => "Dofus",
        24 => "Quete",
        33 => "Potion",
        34 => "Parchemin",
        35 => "Ressource",
        36 => "Nourriture",
        82 => "Bouclier",
        84 => "Sac",
        _ => type > 0 ? $"Type #{type}" : "Inconnu"
    };

    private static string ConstruireDetails(ObjetInventaire obj, InfoItem? info, string position)
    {
        var gfx = info?.IdGfx > 0 ? $"gfx {info.IdGfx}" : "gfx ?";
        var id = info?.Identifiant > 0 ? $"item {info.Identifiant}" : $"item {obj.IdTemplate}";
        return $"{position} | {id} | {gfx} | uid {obj.Identifiant}";
    }

    private static string IconeTexte(int type, string nom)
    {
        var symbole = type switch
        {
            1 => "A",
            2 or 3 or 4 or 5 or 6 or 7 or 8 or 19 or 20 or 21 or 22 => "W",
            9 => "R",
            10 => "B",
            11 => "S",
            16 => "H",
            17 => "C",
            18 => "P",
            23 => "D",
            24 or 34 => "Q",
            33 => "F",
            35 => "M",
            36 => "N",
            82 => "O",
            84 => "B",
            _ => ""
        };
        if (!string.IsNullOrWhiteSpace(symbole)) return symbole;
        return string.IsNullOrWhiteSpace(nom) ? "?" : nom.Trim()[0].ToString().ToUpperInvariant();
    }

    private static Color CouleurType(int type) => type switch
    {
        1 or 9 or 10 or 11 or 16 or 17 or 18 or 23 or 82 or 84 => Color.FromRgb(0x6C, 0x76, 0xFF),
        2 or 3 or 4 or 5 or 6 or 7 or 8 or 19 or 20 or 21 or 22 => Color.FromRgb(0xC9, 0x57, 0x61),
        24 or 34 => Color.FromRgb(0xC9, 0xA1, 0x4B),
        33 or 36 => Color.FromRgb(0x65, 0xC5, 0x6F),
        35 => Color.FromRgb(0x3B, 0xA6, 0xB8),
        _ => Color.FromRgb(0x55, 0x5C, 0x6D)
    };
}

public sealed class LigneObjet
{
    public int Identifiant { get; set; }
    public int IdTemplate { get; set; }
    public string Nom { get; set; } = "";
    public string Type { get; set; } = "";
    public int Niveau { get; set; }
    public int Quantite { get; set; }
    public int PoidsUnitaire { get; set; }
    public int PoidsTotal { get; set; }
    public string PositionTexte { get; set; } = "";
    public string Etat { get; set; } = "";
    public int IdGfx { get; set; }
    public string Details { get; set; } = "";
    public string IconeTexte { get; set; } = "?";
    public Brush IconeBrush { get; set; } = Brushes.DimGray;
}
