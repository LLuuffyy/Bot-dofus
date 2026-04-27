using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BotDofus.Commun.Reseau;
using BotDofus.Divers;
using BotDofus.Divers.Combats;
using BotDofus.Divers.Combats.IA;
using BotDofus.Divers.Jeu.Personnage;
using BotDofus.Divers.Jeu.Personnage.Spells;

namespace BotDofus.Wpf.Vues;

public partial class VueCombat : UserControl
{
    private ContexteCompte? _contexte;
    private Personnage? _personnageLie;
    private Combat? _combatLie;
    public ObservableCollection<SortItemVm> SortsAppris { get; } = new();
    public ObservableCollection<SortConfigureVm> SortsConfig { get; } = new();
    public ObservableCollection<CombattantVm> CombattantsLive { get; } = new();

    public VueCombat()
    {
        InitializeComponent();
        ListeSortsConfig.ItemsSource = SortsConfig;
        ListeSortsAppris.ItemsSource = SortsAppris;
        ListeCombattants.ItemsSource = CombattantsLive;
        CmbSort.ItemsSource = SortsAppris;
    }

    public void Lier(ContexteCompte ctx)
    {
        if (ReferenceEquals(_contexte, ctx))
        {
            Rafraichir();
            RafraichirSortsAppris();
            RafraichirCombatLive();
            return;
        }

        Detacher();
        _contexte = ctx;
        _personnageLie = ctx.EtatJeu.Personnage;
        _combatLie = ctx.EtatJeu.Combat;
        CmbStrategie.SelectedIndex = (int)ctx.ConfigCombat.Strategie;
        ctx.PaquetRecu += OnPaquetRecu;
        _personnageLie.SortsChanges += OnSortsChanges;
        _combatLie.EtatChange += OnCombatChange;
        _combatLie.TourChange += OnCombatChange;
        Rafraichir();
        RafraichirSortsAppris();
        RafraichirCombatLive();
    }

    private void Detacher()
    {
        if (_contexte != null)
        {
            _contexte.PaquetRecu -= OnPaquetRecu;
        }

        if (_personnageLie != null)
        {
            _personnageLie.SortsChanges -= OnSortsChanges;
        }

        if (_combatLie != null)
        {
            _combatLie.EtatChange -= OnCombatChange;
            _combatLie.TourChange -= OnCombatChange;
        }

        _personnageLie = null;
        _combatLie = null;
    }

    private void OnPaquetRecu(object? sender, EvenementPaquetRecu e)
    {
        var contenu = e.Paquet.Contenu;
        if (!contenu.StartsWith("G", StringComparison.Ordinal)
            && !contenu.StartsWith("SL", StringComparison.Ordinal)
            && !contenu.StartsWith("SM", StringComparison.Ordinal)
            && !contenu.StartsWith("SR", StringComparison.Ordinal))
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            Rafraichir();
            RafraichirCombatLive();
        });
    }

    private void OnSortsChanges(object? sender, EventArgs e)
        => Dispatcher.BeginInvoke(RafraichirSortsAppris);

    private void OnCombatChange(object? sender, BotDofus.Divers.Combats.Enums.EtatCombat e)
        => Dispatcher.BeginInvoke(new Action(RafraichirCombatLive));

    private void OnCombatChange(object? sender, int e)
        => Dispatcher.BeginInvoke(new Action(RafraichirCombatLive));

    private void RafraichirCombatLive()
    {
        if (_contexte == null) return;
        var combat = _contexte.EtatJeu.Combat;

        TxtTour.Text = combat.NumeroTour > 0 ? combat.NumeroTour.ToString() : "-";
        TxtNbAllies.Text = combat.Allies.Count.ToString();
        TxtNbEnnemis.Text = combat.Ennemis.Count.ToString();

        var (libelle, couleur) = combat.Etat switch
        {
            BotDofus.Divers.Combats.Enums.EtatCombat.Inactif => ("Hors combat", "#3D3D3D"),
            BotDofus.Divers.Combats.Enums.EtatCombat.Placement => ("Placement", "#C9A14B"),
            BotDofus.Divers.Combats.Enums.EtatCombat.EnCours => ("En combat", "#65C56F"),
            BotDofus.Divers.Combats.Enums.EtatCombat.Termine => ("Termine", "#9AA0AC"),
            _ => (combat.Etat.ToString(), "#3D3D3D"),
        };
        TxtEtat.Text = libelle;
        BadgeEtat.Background = (Brush)new BrushConverter().ConvertFromString(couleur)!;

        CombattantsLive.Clear();
        foreach (var allie in combat.Allies)
        {
            CombattantsLive.Add(new CombattantVm(allie, estAllie: true,
                joueActuellement: combat.IdentifiantCombattantActuel == allie.Identifiant));
        }
        foreach (var ennemi in combat.Ennemis)
        {
            CombattantsLive.Add(new CombattantVm(ennemi, estAllie: false,
                joueActuellement: combat.IdentifiantCombattantActuel == ennemi.Identifiant));
        }

        var horsCombat = combat.Etat == BotDofus.Divers.Combats.Enums.EtatCombat.Inactif
                      && CombattantsLive.Count == 0;
        TxtCombatVide.Visibility = horsCombat ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RafraichirSortsAppris()
    {
        if (_contexte == null) return;

        SortsAppris.Clear();
        foreach (var (id, niv) in _contexte.EtatJeu.Personnage.SortsAppris.OrderBy(kv => kv.Key))
        {
            var info = BaseSorts.Instance.Trouver(id);
            SortsAppris.Add(new SortItemVm(id, niv, info?.Nom ?? $"Sort #{id}", info));
        }

        TxtSortsAppris.Text = $"SORTS APPRIS ({SortsAppris.Count})";
    }

    private void Rafraichir()
    {
        if (_contexte == null) return;

        SortsConfig.Clear();
        int n = 1;
        foreach (var r in _contexte.ConfigCombat.Regles)
        {
            var info = BaseSorts.Instance.Trouver(r.IdSort);
            SortsConfig.Add(new SortConfigureVm(n++, r, info));
        }
    }

    private void CmbSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbSort.SelectedItem is SortItemVm)
        {
            // Placeholder for future derived values from spell metadata.
        }
    }

    private void CmbStrategie_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_contexte == null) return;
        _contexte.ConfigCombat.Strategie = (StrategieCombat)CmbStrategie.SelectedIndex;
    }

    private void BtnAjouter_Click(object sender, RoutedEventArgs e)
    {
        if (_contexte == null || CmbSort.SelectedItem is not SortItemVm sortVm) return;

        var info = sortVm.Info;
        var regle = new RegleSort
        {
            IdSort = sortVm.Identifiant,
            Priorite = int.TryParse(TxtPriorite.Text, out var p) ? p : 5,
            CoutPA = info?.CoutPA ?? 4,
            PorteeMin = info?.PorteeMin ?? 1,
            PorteeMax = info?.PorteeMax ?? 6,
            Cible = (CibleSort)CmbCible.SelectedIndex,
        };

        _contexte.ConfigCombat.Regles.Add(regle);
        Rafraichir();
    }

    private void BtnViderTout_Click(object sender, RoutedEventArgs e)
    {
        _contexte?.ConfigCombat.Regles.Clear();
        Rafraichir();
    }

    private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is SortConfigureVm vm && _contexte != null)
        {
            _contexte.ConfigCombat.Regles.Remove(vm.Regle);
            Rafraichir();
        }
    }

    private void BtnMonter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is SortConfigureVm vm && _contexte != null)
        {
            int idx = _contexte.ConfigCombat.Regles.IndexOf(vm.Regle);
            if (idx > 0)
            {
                _contexte.ConfigCombat.Regles.RemoveAt(idx);
                _contexte.ConfigCombat.Regles.Insert(idx - 1, vm.Regle);
                Rafraichir();
            }
        }
    }

    private void BtnDescendre_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is SortConfigureVm vm && _contexte != null)
        {
            int idx = _contexte.ConfigCombat.Regles.IndexOf(vm.Regle);
            if (idx >= 0 && idx < _contexte.ConfigCombat.Regles.Count - 1)
            {
                _contexte.ConfigCombat.Regles.RemoveAt(idx);
                _contexte.ConfigCombat.Regles.Insert(idx + 1, vm.Regle);
                Rafraichir();
            }
        }
    }

    private void BtnSauver_Click(object sender, RoutedEventArgs e)
    {
        if (_contexte == null) return;

        var chemin = Path.Combine("peleas", $"{_contexte.Compte.Identifiant}.json");
        _contexte.ConfigCombat.Sauvegarder(chemin);
        MessageBox.Show($"Sauvegarde : {chemin}", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void BtnFinirTour_Click(object sender, RoutedEventArgs e)
    {
        if (_contexte == null) return;
        await _contexte.Api.FinirTourAsync(System.Threading.CancellationToken.None);
    }

    private async void BtnReady_Click(object sender, RoutedEventArgs e)
    {
        // GR<id_perso> : annonce ready en placement (Dofus Retro 1.29)
        if (_contexte == null) return;
        await _contexte.Api.EnvoyerPaquetBrutAsync($"GR{_contexte.EtatJeu.Personnage.Identifiant}",
            System.Threading.CancellationToken.None);
    }

    private async void BtnQuitterCombat_Click(object sender, RoutedEventArgs e)
    {
        // Gv : abandonner combat (Dofus Retro 1.29)
        if (_contexte == null) return;
        await _contexte.Api.EnvoyerPaquetBrutAsync("Gv",
            System.Threading.CancellationToken.None);
    }
}

public sealed class SortItemVm
{
    public int Identifiant { get; }
    public int Niveau { get; }
    public string Nom { get; }
    public InfoSort? Info { get; }

    public SortItemVm(int id, int niv, string nom, InfoSort? info)
    {
        Identifiant = id;
        Niveau = niv;
        Nom = nom;
        Info = info;
    }

    public string Affichage => $"[{Identifiant}] {Nom} (niv.{Niveau})";

    public Brush CouleurBrush
    {
        get
        {
            int h = (int)((Identifiant * 2654435761) & int.MaxValue);
            byte r = (byte)((h >> 16) & 0xFF);
            byte g = (byte)((h >> 8) & 0xFF);
            byte b = (byte)(h & 0xFF);
            r = Math.Max(r, (byte)80);
            g = Math.Max(g, (byte)80);
            b = Math.Max(b, (byte)80);
            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }
    }

    public override string ToString() => Affichage;
}

public sealed class CombattantVm
{
    private readonly BotDofus.Divers.Combats.Combattants.Combattant _src;
    private readonly bool _estAllie;
    private readonly bool _joueActuellement;

    public CombattantVm(BotDofus.Divers.Combats.Combattants.Combattant src, bool estAllie, bool joueActuellement)
    {
        _src = src;
        _estAllie = estAllie;
        _joueActuellement = joueActuellement;
    }

    public string Nom => string.IsNullOrEmpty(_src.Nom) ? $"#{_src.Identifiant}" : _src.Nom;
    public string PvTexte => $"PV {_src.PV}/{_src.PVMax}";
    public string PaTexte => $"PA {_src.PA}";
    public string PmTexte => $"PM {_src.PM}";

    public Brush CouleurEquipe => _estAllie
        ? new SolidColorBrush(Color.FromRgb(0x65, 0xC5, 0x6F))
        : new SolidColorBrush(Color.FromRgb(0xC9, 0x57, 0x61));

    public Brush CouleurFond => _joueActuellement
        ? new SolidColorBrush(Color.FromRgb(0x3A, 0x42, 0x55))
        : new SolidColorBrush(Color.FromRgb(0x2A, 0x2D, 0x35));

    public Brush CouleurBordure => _joueActuellement
        ? new SolidColorBrush(Color.FromRgb(0x6C, 0x76, 0xFF))
        : new SolidColorBrush(Color.FromRgb(0x39, 0x3D, 0x47));
}

public sealed class SortConfigureVm
{
    public int Numero { get; }
    public RegleSort Regle { get; }
    public InfoSort? Info { get; }

    public SortConfigureVm(int numero, RegleSort regle, InfoSort? info)
    {
        Numero = numero;
        Regle = regle;
        Info = info;
    }

    public string NomSort => Info?.Nom ?? $"Sort #{Regle.IdSort}";
    public string InfoSort => Info != null ? $"PA {Info.CoutPA} | portee {Info.PorteeMin}-{Info.PorteeMax}" : $"PA {Regle.CoutPA}";
    public string CibleTexte => Regle.Cible switch
    {
        CibleSort.EnnemiPlusProche => "Ennemi le plus proche",
        CibleSort.EnnemiPlusFaible => "Ennemi le plus faible",
        CibleSort.EnnemiPlusFort => "Ennemi le plus fort",
        CibleSort.Soi => "Soi",
        CibleSort.AlliePlusBlesse => "Allie le plus blesse",
        _ => "-"
    };
    public string ConditionsTexte => $"P:{Regle.Priorite}";

    public Brush CouleurBrush
    {
        get
        {
            int h = (int)((Regle.IdSort * 2654435761) & int.MaxValue);
            byte r = (byte)Math.Max((h >> 16) & 0xFF, 80);
            byte g = (byte)Math.Max((h >> 8) & 0xFF, 80);
            byte b = (byte)Math.Max(h & 0xFF, 80);
            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }
    }
}
