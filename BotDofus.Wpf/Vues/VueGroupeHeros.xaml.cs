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
        if (_contexte != null)
        {
            // S'abonner aux (dé)affectations du GroupeHeros : à la liaison le
            // groupe est typiquement encore null (créé au 1er GTSX), il faut
            // se ré-attacher dès qu'il apparaît côté Compte.
            _contexte.Compte.GroupeHerosChange += OnGroupeAffecte;
            // Synchro UI checkbox auto-invit + textarea noms avec la config persistée.
            ChkAutoInvit.IsChecked = _contexte.ConfigGroupeHeros.AutoInvitationActive;
            TxtNomsHeros.Text = string.Join("\r\n", _contexte.ConfigGroupeHeros.NomsHeros);
        }
        AttacherAuGroupe(_contexte?.Compte.GroupeHeros);
        Rafraichir();
    }

    private void BtnSauverNomsHeros_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_contexte is null) return;
        var lignes = (TxtNomsHeros.Text ?? string.Empty)
            .Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Distinct()
            .ToList();
        _contexte.ConfigGroupeHeros.NomsHeros.Clear();
        _contexte.ConfigGroupeHeros.NomsHeros.AddRange(lignes);
        _contexte.ConfigGroupeHeros.Sauvegarder(_contexte.Compte.Identifiant);
        BotDofus.Utilitaires.Journaux.Journaliseur.Info(
            $"[GH-INVIT] Liste sauvegardée — {lignes.Count} héros : {string.Join(", ", lignes)}");
        System.Windows.MessageBox.Show(
            $"{lignes.Count} héros enregistrés. Coche « Auto-invitation » pour activer à la connexion.",
            "Auto-invitation",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    private void OnGroupeAffecte(object? sender, BotDofus.Divers.MultiAccount.GroupeHeros? nouveau)
        => Dispatcher.BeginInvoke(new Action(() =>
        {
            AttacherAuGroupe(nouveau);
            Rafraichir();
        }), System.Windows.Threading.DispatcherPriority.Background);

    private void AttacherAuGroupe(GroupeHeros? gh)
    {
        if (ReferenceEquals(_groupeLie, gh)) return;
        if (_groupeLie != null)
        {
            _groupeLie.MembresChanges -= OnGroupeChange;
            _groupeLie.TourDeMembre -= OnTourChange;
            _groupeLie.GroupeActive -= OnGroupeChange;
            _groupeLie.GroupeDissous -= OnGroupeChange;
            _groupeLie.StatsMembreChange -= OnStatsChange;
            _groupeLie.SortsMembreChange -= OnStatsChange;
        }
        _groupeLie = gh;
        if (_groupeLie != null)
        {
            _groupeLie.MembresChanges += OnGroupeChange;
            _groupeLie.TourDeMembre += OnTourChange;
            _groupeLie.GroupeActive += OnGroupeChange;
            _groupeLie.GroupeDissous += OnGroupeChange;
            _groupeLie.StatsMembreChange += OnStatsChange;
            _groupeLie.SortsMembreChange += OnStatsChange;
        }
    }

    private void Detacher()
    {
        if (_contexte != null)
        {
            _contexte.Compte.GroupeHerosChange -= OnGroupeAffecte;
        }
        AttacherAuGroupe(null);
        _contexte = null;
    }

    private void OnGroupeChange(object? sender, EventArgs e)
        => Dispatcher.BeginInvoke(new Action(Rafraichir),
                                  System.Windows.Threading.DispatcherPriority.Background);

    private void OnTourChange(object? sender, MembreHeros membre)
        => Dispatcher.BeginInvoke(new Action(Rafraichir),
                                  System.Windows.Threading.DispatcherPriority.Background);

    private void OnStatsChange(object? sender, MembreHeros membre)
        => Dispatcher.BeginInvoke(new Action(Rafraichir),
                                  System.Windows.Threading.DispatcherPriority.Background);

    private void ChkAutoInvit_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_contexte is null) return;
        _contexte.ConfigGroupeHeros.AutoInvitationActive = ChkAutoInvit.IsChecked == true;
        _contexte.ConfigGroupeHeros.Sauvegarder(_contexte.Compte.Identifiant);
        BotDofus.Utilitaires.Journaux.Journaliseur.Info(
            $"[GH-INVIT] Auto-invitation {(ChkAutoInvit.IsChecked == true ? "activée" : "désactivée")} pour {_contexte.Compte.Identifiant}");
    }

    private async void BtnInviterMaintenant_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_contexte is null) return;
        // Force l'activation temporaire pour permettre LancerAsync de tourner.
        var save = _contexte.ConfigGroupeHeros.AutoInvitationActive;
        _contexte.ConfigGroupeHeros.AutoInvitationActive = true;
        try
        {
            await _contexte.InviterHerosMaintenantAsync();
        }
        finally
        {
            _contexte.ConfigGroupeHeros.AutoInvitationActive = save;
        }
    }

    private void BtnOuvrirCfgGroupe_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_contexte is null) return;
        var path = System.IO.Path.GetFullPath(
            BotDofus.Divers.MultiAccount.ConfigGroupeHeros.CheminPour(_contexte.Compte.Identifiant));
        // Crée le fichier s'il n'existe pas pour que l'éditeur ouvre quelque chose.
        if (!System.IO.File.Exists(path))
        {
            _contexte.ConfigGroupeHeros.Sauvegarder(_contexte.Compte.Identifiant);
        }
        OuvrirFichierExterne(path);
    }

    private void BtnEditerMembre_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button b) return;
        if (b.Tag is not int idJeu || idJeu == 0) return;
        var membre = _groupeLie?.TrouverParIdJeu(idJeu);
        if (membre is null) return;

        // Bascule vers l'onglet « Combat » du MainWindow et assigne le perso
        // cible à VueCombat — l'user édite ainsi avec la même UI que le master.
        var fenetre = System.Windows.Window.GetWindow(this);
        if (fenetre is null) return;
        var tabsContent = fenetre.FindName("TabsContent") as System.Windows.Controls.TabControl;
        var vueCombat = fenetre.FindName("VueCombatTab") as VueCombat;
        if (tabsContent is null || vueCombat is null) return;

        // Trouve l'index de l'onglet Combat dans le TabControl.
        foreach (var item in tabsContent.Items)
        {
            if (item is System.Windows.Controls.TabItem ti
                && ti.Content is VueCombat)
            {
                tabsContent.SelectedItem = ti;
                break;
            }
        }
        vueCombat.AssignerPersoCible(membre);
    }

    private static void OuvrirFichierExterne(string chemin)
    {
        try
        {
            if (!System.IO.File.Exists(chemin))
            {
                System.Windows.MessageBox.Show(
                    $"Fichier introuvable :\n{chemin}\n\nIl sera créé après la prochaine réception de données serveur.",
                    "Config héros",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = chemin,
                UseShellExecute = true,
            });
        }
        catch (System.Exception ex)
        {
            BotDofus.Utilitaires.Journaux.Journaliseur.Avertir($"[GH-CFG] Ouverture {chemin} échec : {ex.Message}");
        }
    }

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
            // L'éditeur de noms reste toujours visible : c'est précisément ce
            // dont l'user a besoin pour configurer l'invitation avant tout
            // combat (et donc avant détection du groupe).
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
        // Snapshots atomiques pour échapper à la race avec le thread réseau
        // qui peut ajouter/retirer un membre pendant l'itération de l'UI.
        var membresSnap = _groupeLie.SnapshotMembres();
        var ordreSnap = _groupeLie.SnapshotOrdreTours();
        var idxActuel = _groupeLie.IndexMembreActuel;
        var actuel = _groupeLie.JoueurActuel;

        TxtResume.Text = $"{membresSnap.Length} membre(s) • leader = {_groupeLie.Leader?.Nom ?? "?"}";

        // Liste membres
        Membres.Clear();
        foreach (var m in membresSnap)
        {
            bool estTour = actuel != null && actuel.IdJeu == m.IdJeu;
            Membres.Add(new LigneMembre(m, estTour));
        }

        // Ordre des tours
        TxtTourCourant.Text = actuel != null
            ? $"Tour actuel : {actuel.Nom} (id {actuel.IdJeu})"
            : "Aucun tour en cours";
        Ordre.Clear();
        for (int i = 0; i < ordreSnap.Length; i++)
        {
            int id = ordreSnap[i];
            var membre = _groupeLie.TrouverParIdJeu(id);
            bool estCourant = i == idxActuel;
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
        if (!m.EstVivant)
        {
            BackgroundLigne = new SolidColorBrush(Color.FromRgb(0x3D, 0x1F, 0x1F));
            BorderBrushLigne = new SolidColorBrush(Color.FromRgb(0xC9, 0x57, 0x61));
            IndicateurTour = "✖";
        }
        else if (estTour)
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

        // Stats live (depuis GTM via GroupeHeros.NotifierStatsCombattant).
        // Tant que pas observé, on affiche "—" plutôt que "0/0".
        if (m.PvMax > 0)
        {
            StatsTexte = $"PV {m.Pv}/{m.PvMax}   PA {m.Pa}   PM {m.Pm}";
            StatsVisible = System.Windows.Visibility.Visible;
        }
        else
        {
            StatsTexte = "Stats indisponibles (pas en combat)";
            StatsVisible = System.Windows.Visibility.Collapsed;
        }

        // Sorts appris : on liste id+niveau si > 0 (- pas dans la barre).
        if (m.SortsAppris.Count > 0)
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (var kv in m.SortsAppris)
            {
                parts.Add($"#{kv.Key} niv{kv.Value}");
            }
            SortsTexte = "Sorts : " + string.Join(", ", parts);
            SortsVisible = System.Windows.Visibility.Visible;
        }
        else
        {
            SortsTexte = "Sorts non capturés";
            SortsVisible = System.Windows.Visibility.Collapsed;
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
    public string StatsTexte { get; }
    public System.Windows.Visibility StatsVisible { get; }
    public string SortsTexte { get; }
    public System.Windows.Visibility SortsVisible { get; }
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
