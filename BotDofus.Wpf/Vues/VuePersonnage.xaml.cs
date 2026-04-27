using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using BotDofus.Divers;
using BotDofus.Divers.Jeu.Personnage;
using BotDofus.Divers.Jeu.Personnage.Spells;

namespace BotDofus.Wpf.Vues;

public partial class VuePersonnage : UserControl
{
    private ContexteCompte? _contexte;
    private Personnage? _personnageLie;
    public ObservableCollection<SortAppris> Sorts { get; } = new();

    public VuePersonnage()
    {
        InitializeComponent();
        ListeSorts.ItemsSource = Sorts;
    }

    public void Lier(ContexteCompte ctx)
    {
        if (ReferenceEquals(_contexte, ctx))
        {
            Rafraichir();
            return;
        }

        if (_personnageLie != null)
        {
            _personnageLie.Mis_A_Jour -= Personnage_Change;
            _personnageLie.SortsChanges -= Personnage_Change;
        }

        _contexte = ctx;
        _personnageLie = ctx.EtatJeu.Personnage;
        _personnageLie.Mis_A_Jour += Personnage_Change;
        _personnageLie.SortsChanges += Personnage_Change;
        Rafraichir();
    }

    private void Personnage_Change(object? sender, EventArgs e)
        => Dispatcher.BeginInvoke(new Action(Rafraichir), System.Windows.Threading.DispatcherPriority.Background);

    private void Rafraichir()
    {
        if (_contexte == null) return;
        var p = _contexte.EtatJeu.Personnage;

        var (nomClasse, abrev, couleur) = InfosClasse(p.IdClasse);
        TxtClasseAbrev.Text = abrev;
        EllipseAvatar.Fill = new SolidColorBrush(couleur);

        TxtNom.Text = string.IsNullOrEmpty(p.Nom) ? "—" : p.Nom;
        TxtClasse.Text = $"{nomClasse} · {(p.Sexe == 1 ? "Femme" : "Homme")}";
        TxtNiveau.Text = p.Niveau > 0 ? $"Niveau {p.Niveau}" : "Niveau —";
        TxtKamas.Text = p.Kamas.ToString("N0");

        TxtPv.Text = $"{p.Vie} / {p.VieMax}";
        TxtEnergie.Text = $"{p.Energie} / {p.EnergieMax}";
        TxtPa.Text = p.PA.ToString();
        TxtPm.Text = p.PM.ToString();
        TxtPoids.Text = $"{p.PoidsActuel} / {p.PoidsMax}";
        TxtXp.Text = $"{p.PourcentageXp:F1} %";
        TxtPointsCaracs.Text = p.PointsCaracteristiques.ToString();
        TxtPointsSorts.Text = p.PointsSorts.ToString();

        TxtPosition.Text = p.CarteCourante.HasValue
            ? $"Carte {p.CarteCourante} · Cellule {p.CellulePosition?.ToString() ?? "—"}"
            : "Carte —";

        Sorts.Clear();
        foreach (var paire in p.SortsAppris.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key))
        {
            var info = BaseSorts.Instance.Trouver(paire.Key);
            var nom = info?.Nom ?? $"Sort #{paire.Key}";
            var details = info != null
                ? $"{info.CoutPA} PA · portée {info.PorteeMin}-{info.PorteeMax}"
                : "—";
            Sorts.Add(new SortAppris
            {
                Id = paire.Key,
                Niveau = paire.Value,
                Nom = nom,
                Details = details,
            });
        }
        TxtNbSorts.Text = $"{Sorts.Count} sort{(Sorts.Count > 1 ? "s" : "")}";
    }

    private static (string Nom, string Abrev, Color Couleur) InfosClasse(int idClasse) => idClasse switch
    {
        1  => ("Feca",      "FEC", Color.FromRgb(0x4A, 0x90, 0xE2)),
        2  => ("Osamodas",  "OSA", Color.FromRgb(0x7D, 0x4F, 0x9F)),
        3  => ("Enutrof",   "ENU", Color.FromRgb(0xC9, 0xA1, 0x4B)),
        4  => ("Sram",      "SRA", Color.FromRgb(0x6B, 0x6B, 0x6B)),
        5  => ("Xelor",     "XEL", Color.FromRgb(0xD9, 0xC9, 0x3B)),
        6  => ("Ecaflip",   "ECA", Color.FromRgb(0xE2, 0x71, 0x4A)),
        7  => ("Eniripsa",  "ENI", Color.FromRgb(0xE5, 0x6B, 0xA8)),
        8  => ("Iop",       "IOP", Color.FromRgb(0xC4, 0x3D, 0x3D)),
        9  => ("Cra",       "CRA", Color.FromRgb(0x4F, 0xA3, 0x6B)),
        10 => ("Sadida",    "SAD", Color.FromRgb(0x5C, 0x8A, 0x4F)),
        11 => ("Sacrieur",  "SAC", Color.FromRgb(0x9C, 0x2A, 0x2A)),
        12 => ("Pandawa",   "PAN", Color.FromRgb(0xD2, 0xB4, 0x8C)),
        13 => ("Roublard",  "ROU", Color.FromRgb(0x3D, 0x3D, 0x6B)),
        14 => ("Zobal",     "ZOB", Color.FromRgb(0x8B, 0x4F, 0x9F)),
        _  => ($"Classe #{idClasse}", "—", Color.FromRgb(0x9E, 0x9E, 0x9E)),
    };
}

public sealed class SortAppris
{
    public int Id { get; set; }
    public int Niveau { get; set; }
    public string Nom { get; set; } = "";
    public string Details { get; set; } = "";
}
