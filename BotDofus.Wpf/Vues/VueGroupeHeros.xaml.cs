using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using BotDofus.Divers;
using BotDofus.Divers.MultiAccount;

namespace BotDofus.Wpf.Vues;

/// <summary>
/// Vue lecture seule du <see cref="GroupeHeros"/> du compte courant.
///
/// Sur Abrak, la composition est instantiée serveur-side (cf. <c>DetecteurModeHeros</c>) :
/// l'UI affiche la liste détectée, l'ordre des tours observé, et l'état d'activation.
/// Aucune action utilisateur ne modifie le groupe — c'est le serveur qui décide.
///
/// Pour configurer le combat du master, le user passe par l'onglet Combat (qui
/// expose la <see cref="BotDofus.Divers.Combats.IA.ConfigCombat"/> du compte).
/// </summary>
public partial class VueGroupeHeros : UserControl
{
    private ContexteCompte? _contexte;
    private GroupeHeros? _groupeLie;

    public ObservableCollection<LigneMembre> Membres { get; } = new();
    public ObservableCollection<LigneOrdre> Ordre { get; } = new();

    public VueGroupeHeros()
    {
        InitializeComponent();
        // Bindings ItemsSource → collections internes (DataTemplate bind sur DataContext = ligne).
        ListeMembres.ItemsSource = Membres;
        ListeOrdre.ItemsSource = Ordre;
        Rafraichir();
    }

    public void Lier(ContexteCompte ctx)
    {
        if (ReferenceEquals(_contexte, ctx)) return;
        Detacher();
        _contexte = ctx;
        AttacherAuGroupe(_contexte?.Compte.GroupeHeros);
        // Le groupe peut être (dés)attaché à chaud → on re-vérifie sur chaque
        // changement de compte / chaque rafraîchissement.
        Rafraichir();
    }

    private void AttacherAuGroupe(GroupeHeros? gh)
    {
        if (ReferenceEquals(_groupeLie, gh)) return;
        if (_groupeLie != null)
        {
            _groupeLie.MembresChanges -= OnGroupeChange;
            _groupeLie.TourDeMembre -= OnTourChange;
            _groupeLie.GroupeActive -= OnGroupeChange;
            _groupeLie.GroupeDissous -= OnGroupeChange;
        }
        _groupeLie = gh;
        if (_groupeLie != null)
        {
            _groupeLie.MembresChanges += OnGroupeChange;
            _groupeLie.TourDeMembre += OnTourChange;
            _groupeLie.GroupeActive += OnGroupeChange;
            _groupeLie.GroupeDissous += OnGroupeChange;
        }
    }

    private void Detacher()
    {
        AttacherAuGroupe(null);
        _contexte = null;
    }

    private void OnGroupeChange(object? sender, EventArgs e)
        => Dispatcher.BeginInvoke(new Action(Rafraichir),
                                  System.Windows.Threading.DispatcherPriority.Background);

    private void OnTourChange(object? sender, MembreHeros membre)
        => Dispatcher.BeginInvoke(new Action(Rafraichir),
                                  System.Windows.Threading.DispatcherPriority.Background);

    private void Rafraichir()
    {
        // Re-attache si le compte a reçu son GroupeHeros entre temps (1er GTSX).
        var gh = _contexte?.Compte.GroupeHeros;
        if (!ReferenceEquals(gh, _groupeLie)) AttacherAuGroupe(gh);

        if (_groupeLie is null)
        {
            TxtEtat.Text = "Aucun groupe";
            BadgeEtat.Background = new SolidColorBrush(Color.FromRgb(0x3D, 0x44, 0x53));
            TxtResume.Text = "Pas de mode héros détecté sur ce compte.";
            TxtTourCourant.Text = "—";
            Membres.Clear();
            Ordre.Clear();
            return;
        }

        // Badge état
        if (_groupeLie.EstActif)
        {
            TxtEtat.Text = "Actif";
            BadgeEtat.Background = new SolidColorBrush(Color.FromRgb(0x15, 0x3C, 0x34));
            TxtEtat.Foreground = new SolidColorBrush(Color.FromRgb(0x65, 0xC5, 0x6F));
        }
        else
        {
            TxtEtat.Text = "Détecté (inactif)";
            BadgeEtat.Background = new SolidColorBrush(Color.FromRgb(0x3D, 0x44, 0x53));
            TxtEtat.Foreground = new SolidColorBrush(Color.FromRgb(0xC0, 0xC6, 0xD4));
        }
        TxtResume.Text = $"{_groupeLie.Membres.Count} membre(s) • leader = {_groupeLie.Leader?.Nom ?? "?"}";

        // Liste membres
        Membres.Clear();
        var actuel = _groupeLie.JoueurActuel;
        foreach (var m in _groupeLie.Membres)
        {
            bool estTour = actuel != null && actuel.IdJeu == m.IdJeu;
            Membres.Add(new LigneMembre(m, estTour));
        }

        // Ordre des tours
        TxtTourCourant.Text = actuel != null
            ? $"Tour actuel : {actuel.Nom} (id {actuel.IdJeu})"
            : "Aucun tour en cours";
        Ordre.Clear();
        var ordreList = _groupeLie.OrdreToursCourant;
        for (int i = 0; i < ordreList.Count; i++)
        {
            int id = ordreList[i];
            var membre = _groupeLie.TrouverParIdJeu(id);
            bool estCourant = i == _groupeLie.IndexMembreActuel;
            Ordre.Add(new LigneOrdre(i + 1, id, membre, estCourant));
        }
    }
}

/// <summary>Ligne de la liste « Membres » — projection lisible pour le DataTemplate.</summary>
public sealed class LigneMembre
{
    public LigneMembre(MembreHeros m, bool estTour)
    {
        IdJeu = m.IdJeu;
        NomAffiche = string.IsNullOrWhiteSpace(m.Nom) ? $"Perso #{m.IdJeu}" : m.Nom;
        SousTitre = m.IdClasse > 0
            ? $"id {m.IdJeu} • classe {m.IdClasse} • niv {m.Niveau}"
            : $"id {m.IdJeu}";
        switch (m.Role)
        {
            case RoleDansGroupe.Leader:
                RoleTexte = "LEADER";
                CouleurRole = new SolidColorBrush(Color.FromRgb(0xFF, 0xCD, 0x52));
                break;
            case RoleDansGroupe.Suiveur:
                RoleTexte = "LIÉ";
                CouleurRole = new SolidColorBrush(Color.FromRgb(0x6C, 0x76, 0xFF));
                break;
            default:
                RoleTexte = "—";
                CouleurRole = new SolidColorBrush(Color.FromRgb(0x55, 0x5C, 0x6D));
                break;
        }
        if (estTour)
        {
            BackgroundLigne = new SolidColorBrush(Color.FromRgb(0x15, 0x3C, 0x34));
            BorderBrushLigne = new SolidColorBrush(Color.FromRgb(0x43, 0xA0, 0x47));
            IndicateurTour = "►";
        }
        else
        {
            BackgroundLigne = new SolidColorBrush(Color.FromRgb(0x2A, 0x30, 0x42));
            BorderBrushLigne = new SolidColorBrush(Color.FromRgb(0x3C, 0x44, 0x58));
            IndicateurTour = string.Empty;
        }
    }

    public int IdJeu { get; }
    public string NomAffiche { get; }
    public string SousTitre { get; }
    public string RoleTexte { get; }
    public Brush CouleurRole { get; }
    public Brush BackgroundLigne { get; }
    public Brush BorderBrushLigne { get; }
    public string IndicateurTour { get; }
}

/// <summary>Ligne de la liste « Ordre des tours » — projection lisible.</summary>
public sealed class LigneOrdre
{
    public LigneOrdre(int rang, int idCombattant, MembreHeros? membre, bool estCourant)
    {
        Index = $"#{rang}";
        Etiquette = membre != null
            ? (string.IsNullOrWhiteSpace(membre.Nom) ? $"id {idCombattant}" : $"{membre.Nom}")
            : $"ext id {idCombattant}";
        if (estCourant)
        {
            BackgroundOrdre = new SolidColorBrush(Color.FromRgb(0x15, 0x3C, 0x34));
            BorderOrdre = new SolidColorBrush(Color.FromRgb(0x43, 0xA0, 0x47));
            Marqueur = "►";
        }
        else
        {
            BackgroundOrdre = new SolidColorBrush(Color.FromRgb(0x2A, 0x30, 0x42));
            BorderOrdre = new SolidColorBrush(Color.FromRgb(0x3C, 0x44, 0x58));
            Marqueur = string.Empty;
        }
    }

    public string Index { get; }
    public string Etiquette { get; }
    public Brush BackgroundOrdre { get; }
    public Brush BorderOrdre { get; }
    public string Marqueur { get; }
}
