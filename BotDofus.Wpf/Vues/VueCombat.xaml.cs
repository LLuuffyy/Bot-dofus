using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
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
    /// <summary>
    /// Perso cible de l'édition. null = on édite la ConfigCombat du master
    /// (cas legacy). Non-null = on édite la ConfigCombat d'un membre lié
    /// (peleas/heros/&lt;idJeu&gt;.json), avec ses sorts à lui.
    /// </summary>
    private BotDofus.Divers.MultiAccount.MembreHeros? _persoCible;
    /// <summary>Timer debounce de la sauvegarde auto config combat (800 ms, ADR-001 §5).</summary>
    private DispatcherTimer? _timerSauvegarde;
    /// <summary>true pendant l'init des contrôles UI depuis ConfigCombat (évite déclencher les Changed).</summary>
    private bool _initEnCours;

    /// <summary>
    /// ConfigCombat en cours d'édition. Si <see cref="_persoCible"/> est null,
    /// retourne la ConfigCombat du contexte (legacy master). Sinon retourne
    /// celle du membre lié (peuplée par <see cref="BotDofus.Divers.MultiAccount.ServiceConfigsHeros"/>).
    /// </summary>
    public BotDofus.Divers.Combats.IA.ConfigCombat? ConfigActive
        => _persoCible?.ConfigCombat ?? _contexte?.ConfigCombat;

    /// <summary>Sorts à afficher dans la rotation : ceux du master ou ceux du membre lié.</summary>
    private System.Collections.Generic.IDictionary<int, int> SortsLookup
        => _persoCible?.SortsAppris
           ?? (System.Collections.Generic.IDictionary<int, int>?)_contexte?.EtatJeu.Personnage.SortsAppris
           ?? new System.Collections.Generic.Dictionary<int, int>();
    /// <summary>
    /// Timer de polling défensif des sorts appris (G.1) : si le paquet SL
    /// arrive APRÈS que la vue soit liée, l'event SortsChanges peut être
    /// manqué selon le timing UI. Ce timer re-tente RafraichirSortsAppris
    /// pendant 30s tant que la collection est vide.
    /// </summary>
    private DispatcherTimer? _timerSortsPoll;
    private int _tentativesSortsPoll;
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
        // ComboBox élément des conditions avancées (Aucun/Force/Intel/Chance/Agi/Neutre)
        CmbElement.ItemsSource = System.Enum.GetValues(typeof(ElementSort));
    }

    public void Lier(ContexteCompte ctx)
    {
        if (ReferenceEquals(_contexte, ctx))
        {
            PeuplerComboPersoCible();
            Rafraichir();
            RafraichirSortsAppris();
            RafraichirCombatLive();
            return;
        }

        Detacher();
        _contexte = ctx;
        _personnageLie = ctx.EtatJeu.Personnage;
        _combatLie = ctx.EtatJeu.Combat;
        // Bascule par défaut sur le master à chaque nouveau contexte attaché.
        _persoCible = null;
        InitialiserModeEtTactique(ctx.ConfigCombat);
        ctx.PaquetRecu += OnPaquetRecu;
        _personnageLie.SortsChanges += OnSortsChanges;
        _combatLie.EtatChange += OnCombatChange;
        _combatLie.TourChange += OnCombatChange;
        ctx.Compte.GroupeHerosChange += OnGroupeAffecte;
        AttacherAuGroupePourCombo(ctx.Compte.GroupeHeros);
        PeuplerComboPersoCible();
        Rafraichir();
        RafraichirSortsAppris();
        RafraichirCombatLive();
    }

    /// <summary>Item d'affichage du combo « Configurer pour ».</summary>
    public sealed class PersoCibleVm
    {
        public BotDofus.Divers.MultiAccount.MembreHeros? Membre { get; init; } // null = master
        public string Etiquette { get; init; } = string.Empty;
        public string Nom { get; init; } = string.Empty;
        public string SousTitre { get; init; } = string.Empty;
        public string Initiale { get; init; } = "?";
        public string Badge { get; init; } = string.Empty;
        public System.Windows.Media.Brush CouleurClasse { get; init; } = System.Windows.Media.Brushes.Gray;
        public System.Windows.Media.Brush CouleurBadge { get; init; } = System.Windows.Media.Brushes.Gray;
        public override string ToString() => Etiquette;
    }

    /// <summary>Palette couleurs Dofus Retro par idClasse (Feca=1, Osamodas=2, ..., Sadida=10).</summary>
    private static System.Windows.Media.Brush CouleurPourClasse(int idClasse) => idClasse switch
    {
        1  => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x6F, 0xC4, 0xE8)), // Féca (bleu)
        2  => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x9C, 0xD8, 0x5D)), // Osamodas (vert)
        3  => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF2, 0xC8, 0x4B)), // Enutrof (or)
        4  => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE0, 0x84, 0xC6)), // Sram (rose)
        5  => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD0, 0x67, 0x5F)), // Xelor (rouge sombre)
        6  => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC8, 0xB0, 0x84)), // Ecaflip (brun-or)
        7  => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xA8, 0x5C)), // Eniripsa (orange)
        8  => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xEF, 0x6B, 0x6B)), // Iop (rouge)
        9  => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xB5, 0xC8, 0xD8)), // Cra (gris-bleu)
        10 => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8B, 0xD0, 0x8B)), // Sadida (vert clair)
        11 => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD2, 0x9A, 0xE8)), // Sacrieur (violet)
        12 => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE8, 0xC8, 0x6F)), // Pandawa (jaune-ocre)
        _  => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xA0, 0xA8, 0xB8)),
    };

    private static string NomClasse(int idClasse) => idClasse switch
    {
        1 => "Féca", 2 => "Osamodas", 3 => "Enutrof", 4 => "Sram",
        5 => "Xelor", 6 => "Ecaflip", 7 => "Eniripsa", 8 => "Iop",
        9 => "Cra", 10 => "Sadida", 11 => "Sacrieur", 12 => "Pandawa",
        _ => $"Classe {idClasse}",
    };

    private void PeuplerComboPersoCible()
    {
        if (CmbPersoCible is null) return;
        var items = new System.Collections.Generic.List<PersoCibleVm>();
        // Master (toujours en haut, null = ConfigCombat du contexte = legacy).
        var nomMaster = _contexte?.EtatJeu.Personnage.Nom ?? _contexte?.Compte.Identifiant ?? "Master";
        var idClasseMaster = _contexte?.EtatJeu.Personnage.IdClasse ?? 0;
        int nivMaster = _contexte?.EtatJeu.Personnage.Niveau ?? 0;
        items.Add(new PersoCibleVm
        {
            Membre = null,
            Etiquette = $"{nomMaster} (master)",
            Nom = nomMaster,
            SousTitre = idClasseMaster > 0
                ? $"{NomClasse(idClasseMaster)} • niv {nivMaster} • master"
                : "Master",
            Initiale = string.IsNullOrEmpty(nomMaster) ? "?" : nomMaster[0].ToString().ToUpperInvariant(),
            Badge = "LEADER",
            CouleurClasse = CouleurPourClasse(idClasseMaster),
            CouleurBadge = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xCD, 0x52)),
        });
        // Liés en RAM (groupe détecté).
        var groupe = _contexte?.Compte.GroupeHeros;
        var idsDejaListes = new System.Collections.Generic.HashSet<int>();
        if (groupe is not null)
        {
            foreach (var m in groupe.SnapshotMembres())
            {
                if (m.Role == BotDofus.Divers.MultiAccount.RoleDansGroupe.Leader) continue;
                idsDejaListes.Add(m.IdJeu);
                var nom = string.IsNullOrWhiteSpace(m.Nom) ? $"Perso #{m.IdJeu}" : m.Nom;
                items.Add(new PersoCibleVm
                {
                    Membre = m,
                    Etiquette = $"{nom} (id {m.IdJeu})",
                    Nom = nom,
                    SousTitre = m.IdClasse > 0
                        ? $"{NomClasse(m.IdClasse)} • niv {m.Niveau} • id {m.IdJeu}"
                        : $"id {m.IdJeu}",
                    Initiale = nom[0].ToString().ToUpperInvariant(),
                    Badge = "LIÉ",
                    CouleurClasse = CouleurPourClasse(m.IdClasse),
                    CouleurBadge = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x6C, 0x76, 0xFF)),
                });
            }
        }

        // Persos PERSISTÉS sur disque (peleas/heros/<id>.json) qui ne sont
        // pas en RAM — permet de configurer l'IA des autres héros sans avoir
        // à lancer un combat au préalable (cas user 2026-05-22 : « ils
        // n'apparaissent pas dans la liste avant le combat »).
        foreach (var cfg in BotDofus.Divers.MultiAccount.ServiceConfigsHeros.ListerToutes())
        {
            if (idsDejaListes.Contains(cfg.IdJeu)) continue;
            var nom = string.IsNullOrWhiteSpace(cfg.Nom) ? $"Perso #{cfg.IdJeu}" : cfg.Nom;
            // Crée un MembreHeros « fantôme » (pas attaché au groupe RAM) pour
            // permettre l'édition. Sa ConfigCombat est celle persistée.
            var membreFantome = new BotDofus.Divers.MultiAccount.MembreHeros
            {
                IdJeu = cfg.IdJeu,
                Nom = cfg.Nom,
                IdClasse = cfg.IdClasse,
                Niveau = cfg.Niveau,
                Role = BotDofus.Divers.MultiAccount.RoleDansGroupe.Suiveur,
                ConfigCombat = cfg.ConfigCombat
                    ?? BotDofus.Divers.MultiAccount.ServiceConfigsHeros.PresetParClasse(cfg.IdClasse),
            };
            foreach (var kv in cfg.SortsAppris) membreFantome.SortsAppris[kv.Key] = kv.Value;
            foreach (var kv in cfg.PositionsBarre) membreFantome.PositionsBarre[kv.Key] = kv.Value;
            items.Add(new PersoCibleVm
            {
                Membre = membreFantome,
                Etiquette = $"{nom} (id {cfg.IdJeu}) — hors combat",
                Nom = nom,
                SousTitre = cfg.IdClasse > 0
                    ? $"{NomClasse(cfg.IdClasse)} • niv {cfg.Niveau} • id {cfg.IdJeu} • disque"
                    : $"id {cfg.IdJeu} • disque",
                Initiale = nom[0].ToString().ToUpperInvariant(),
                Badge = "DISQUE",
                CouleurClasse = CouleurPourClasse(cfg.IdClasse),
                CouleurBadge = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x55, 0x5C, 0x6D)),
            });
        }
        CmbPersoCible.ItemsSource = items;
        // Resélectionne le perso courant.
        int idx = 0;
        if (_persoCible is not null)
        {
            for (int i = 0; i < items.Count; i++)
                if (ReferenceEquals(items[i].Membre, _persoCible)) { idx = i; break; }
        }
        CmbPersoCible.SelectedIndex = idx;
        MajResumePersoCible();
    }

    private void CmbPersoCible_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (CmbPersoCible?.SelectedItem is not PersoCibleVm vm) return;
        if (ReferenceEquals(vm.Membre, _persoCible)) return;
        _persoCible = vm.Membre;
        BotDofus.Utilitaires.Journaux.Journaliseur.Info(
            $"[VUE-COMBAT] Bascule sur {(vm.Membre is null ? "master" : vm.Membre.Nom)} (config + sorts associés)");
        // Re-init complète depuis la nouvelle ConfigActive.
        if (ConfigActive is not null)
            InitialiserModeEtTactique(ConfigActive);
        Rafraichir();
        RafraichirSortsAppris();
        MajResumePersoCible();
    }

    /// <summary>
    /// API publique : sélectionne un perso cible (master si null). Appelée
    /// par <c>VueGroupeHeros</c> via le MainWindow quand l'user clique sur
    /// « Éditer » dans la liste des membres.
    /// </summary>
    public void AssignerPersoCible(BotDofus.Divers.MultiAccount.MembreHeros? membre)
    {
        if (CmbPersoCible is null) return;
        if (CmbPersoCible.ItemsSource is not System.Collections.Generic.IEnumerable<PersoCibleVm> items) return;
        foreach (var vm in items)
        {
            if (ReferenceEquals(vm.Membre, membre))
            {
                CmbPersoCible.SelectedItem = vm;
                return;
            }
        }
        // Pas trouvé (membre absent du combo) : ne fait rien.
    }

    private void MajResumePersoCible()
    {
        if (TxtPersoCibleResume is null) return;
        if (_persoCible is null)
        {
            TxtPersoCibleResume.Text = $"Config = peleas/{_contexte?.Compte.Identifiant}.json";
        }
        else
        {
            var nbSorts = _persoCible.SortsAppris.Count;
            TxtPersoCibleResume.Text = $"Config = peleas/heros/{_persoCible.IdJeu}.json • {nbSorts} sort(s) connu(s)";
        }
    }

    private BotDofus.Divers.MultiAccount.GroupeHeros? _groupePourCombo;

    private void AttacherAuGroupePourCombo(BotDofus.Divers.MultiAccount.GroupeHeros? gh)
    {
        if (ReferenceEquals(_groupePourCombo, gh)) return;
        if (_groupePourCombo is not null)
        {
            _groupePourCombo.MembresChanges -= OnGroupeMembresChanges;
            _groupePourCombo.SortsMembreChange -= OnSortsMembreChange;
        }
        _groupePourCombo = gh;
        if (_groupePourCombo is not null)
        {
            _groupePourCombo.MembresChanges += OnGroupeMembresChanges;
            _groupePourCombo.SortsMembreChange += OnSortsMembreChange;
        }
    }

    private void OnGroupeAffecte(object? sender, BotDofus.Divers.MultiAccount.GroupeHeros? gh)
        => Dispatcher.BeginInvoke(new Action(() =>
        {
            AttacherAuGroupePourCombo(gh);
            PeuplerComboPersoCible();
        }), DispatcherPriority.Background);

    private void OnGroupeMembresChanges(object? sender, EventArgs e)
        => Dispatcher.BeginInvoke(new Action(PeuplerComboPersoCible),
                                  DispatcherPriority.Background);

    private void OnSortsMembreChange(object? sender, BotDofus.Divers.MultiAccount.MembreHeros m)
        => Dispatcher.BeginInvoke(new Action(() =>
        {
            MajResumePersoCible();
            // Si on est en train d'éditer ce membre, refresh la rotation.
            if (ReferenceEquals(m, _persoCible))
                RafraichirSortsAppris();
        }), DispatcherPriority.Background);

    private void Detacher()
    {
        if (_contexte != null)
        {
            _contexte.PaquetRecu -= OnPaquetRecu;
            _contexte.Compte.GroupeHerosChange -= OnGroupeAffecte;
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

        AttacherAuGroupePourCombo(null);
        _persoCible = null;
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

        // Snapshot atomique pour échapper à la race avec le thread réseau qui
        // mute combat.Allies/Ennemis (CRASH observé 22-05 09:30 :
        // ArgumentException « Destination array was not long enough » dans
        // List<T>.CopyTo, car ToList() voit la collection rétrécir entre la
        // pré-allocation et la copie). On retente une fois en silence si la
        // collection bouge encore — le prochain event UI ré-affichera.
        BotDofus.Divers.Combats.Combattants.Combattant[] alliesSnap;
        BotDofus.Divers.Combats.Combattants.Combattant[] ennemisSnap;
        int essai = 0;
        while (true)
        {
            try
            {
                alliesSnap = combat.Allies.ToArray();
                ennemisSnap = combat.Ennemis.ToArray();
                break;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                if (++essai >= 3)
                {
                    BotDofus.Utilitaires.Journaux.Journaliseur.Debogue(
                        $"[UI-COMBAT] race snapshot abandonnée : {ex.GetType().Name}");
                    return;
                }
            }
        }

        TxtTour.Text = combat.NumeroTour > 0 ? combat.NumeroTour.ToString() : "-";
        TxtNbAllies.Text = alliesSnap.Length.ToString();
        TxtNbEnnemis.Text = ennemisSnap.Length.ToString();

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
        var idActuel = combat.IdentifiantCombattantActuel;
        foreach (var allie in alliesSnap)
        {
            CombattantsLive.Add(new CombattantVm(allie, estAllie: true,
                joueActuellement: idActuel == allie.Identifiant));
        }
        foreach (var ennemi in ennemisSnap)
        {
            CombattantsLive.Add(new CombattantVm(ennemi, estAllie: false,
                joueActuellement: idActuel == ennemi.Identifiant));
        }

        var horsCombat = combat.Etat == BotDofus.Divers.Combats.Enums.EtatCombat.Inactif
                      && CombattantsLive.Count == 0;
        TxtCombatVide.Visibility = horsCombat ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RafraichirSortsAppris()
    {
        if (_contexte == null) return;

        SortsAppris.Clear();
        // Snapshot ToList() AVANT OrderBy (cf. crash 06:27 VuePersonnage L 84).
        foreach (var (id, niv) in SortsLookup.ToList().OrderBy(kv => kv.Key))
        {
            var info = BaseSorts.Instance.Trouver(id);
            SortsAppris.Add(new SortItemVm(id, niv, info?.Nom ?? $"Sort #{id}", info));
        }

        TxtSortsAppris.Text = $"SORTS APPRIS ({SortsAppris.Count})";

        // G.1 — defensive polling : si la collection est vide après attach,
        // c'est probablement que le paquet SL n'a pas encore été reçu. Relancer
        // un timer 1s qui re-tente jusqu'à ce que ce soit peuplé (max 30s).
        if (SortsAppris.Count == 0)
            DemarrerPollSortsAppris();
        else
            ArreterPollSortsAppris();
    }

    private void DemarrerPollSortsAppris()
    {
        if (_timerSortsPoll != null) return;
        _tentativesSortsPoll = 0;
        _timerSortsPoll = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timerSortsPoll.Tick += (_, _) =>
        {
            _tentativesSortsPoll++;
            if (_contexte == null || _tentativesSortsPoll > 30)
            {
                ArreterPollSortsAppris();
                return;
            }
            if (SortsLookup.Count > 0)
            {
                RafraichirSortsAppris();  // peuple + arrête le timer
            }
        };
        _timerSortsPoll.Start();
    }

    private void ArreterPollSortsAppris()
    {
        _timerSortsPoll?.Stop();
        _timerSortsPoll = null;
    }

    private void Rafraichir()
    {
        if (_contexte == null) return;

        SortsConfig.Clear();
        int n = 1;
        foreach (var r in ConfigActive!.Regles)
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
        ConfigActive!.Strategie = (StrategieCombat)CmbStrategie.SelectedIndex;
    }

    private void BtnAjouter_Click(object sender, RoutedEventArgs e)
    {
        if (_contexte == null) return;
        if (CmbSort.SelectedItem is not SortItemVm sortVm)
        {
            MessageBox.Show("Sélectionne un sort dans le combo avant d'ajouter à la rotation.",
                "Validation règle", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        // Validation : doublons (1 règle par sort suffit la plupart du temps,
        // mais l'user peut en vouloir 2 si conditions différentes — juste warn).
        if (ConfigActive!.Regles.Any(r => r.IdSort == sortVm.Identifiant))
        {
            var rep = MessageBox.Show(
                $"Le sort #{sortVm.Identifiant} '{sortVm.Affichage}' est déjà dans la rotation.\nL'ajouter en double ?",
                "Doublon", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (rep != MessageBoxResult.Yes) return;
        }

        var info = sortVm.Info;
        // Lire Tag du ComboBoxItem sélectionné (= nom enum) → parse robuste.
        // Permet d'ajouter/réordonner des items sans casser le code-behind.
        var focus = FocusSort.EnnemiLePlusProche;
        if (CmbCible.SelectedItem is ComboBoxItem cbiFocus && cbiFocus.Tag is string tagFocus)
        {
            if (System.Enum.TryParse<FocusSort>(tagFocus, out var f)) focus = f;
        }
        // CmbMethode : 0=CAC, 1=Distance, 2=LesDeux (matche l'ordre XAML)
        MethodeLancement methode = CmbMethode.SelectedIndex switch
        {
            0 => MethodeLancement.CAC,
            1 => MethodeLancement.Distance,
            _ => MethodeLancement.LesDeux
        };

        var regle = new RegleSort
        {
            IdSort = sortVm.Identifiant,
            Nom = info?.Nom ?? sortVm.Affichage,
            Priorite = int.TryParse(TxtPriorite.Text, out var p) ? p : 5,
            CoutPA = info?.CoutPA ?? 4,
            PorteeMin = info?.PorteeMin ?? 1,
            PorteeMax = info?.PorteeMax ?? 6,
            Focus = focus,
            MethodeLancement = methode,
            NombreParTour = int.TryParse(TxtFoisTour.Text, out var nt) ? nt : 1,
            NombreParCible = int.TryParse(TxtFoisCible.Text, out var nc) ? nc : 0,
        };

        ConfigActive!.Regles.Add(regle);
        DemanderSauvegardeDebouncee();
        Rafraichir();
    }

    private void BtnViderTout_Click(object sender, RoutedEventArgs e)
    {
        _contexte?.ConfigCombat.Regles.Clear();
        DemanderSauvegardeDebouncee();
        Rafraichir();
    }

    /// <summary>
    /// Clic sur ⓘ d'une ligne du DataGrid maison : la <see cref="RegleSort"/>
    /// du sort sélectionné devient le DataContext de <c>PanelConditions</c>
    /// → les 22 conditions binées TwoWay reflètent l'état actuel et toute
    /// modif est persistée via le debounce auto.
    /// </summary>
    private void BtnInfo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.Tag is not SortConfigureVm vm) return;
        PanelConditions.DataContext = vm.Regle;
        PanelConditions.IsEnabled = true;
        TxtCondTitre.Text = $"« {vm.NomSort} » (sort #{vm.Regle.IdSort})";
        // Scroll vers le panel pour confort visuel (rotation peut être longue).
        PanelConditions.BringIntoView();
        // Highlights MapViewer : expose le sort sélectionné à VueMapViewer
        // pour que les cells dans la portée soient surlignées sur la grille.
        if (_contexte != null)
        {
            int niv = SortsLookup.TryGetValue(vm.Regle.IdSort, out var n) ? n : 1;
            var info = vm.Info;
            int pmin = info?.Stats(niv)?.PorteeMin ?? vm.Regle.PorteeMin;
            int pmax = info?.Stats(niv)?.PorteeMax ?? vm.Regle.PorteeMax;
            _contexte.EtatJeu.Combat.DefinirSortSelectionne(vm.Regle.IdSort, pmin, pmax, vm.NomSort);
        }
    }

    // ---------------------------------------------------------------------
    // Handlers conditions — toggle CheckBox = set valeur défaut / null sur
    // les propriétés `int?` de RegleSort (cf. blueprint UIBLUEPRINT §6).
    // Utilise reflection pour éviter 10 handlers explicites quasi-identiques.
    // ---------------------------------------------------------------------

    private static readonly System.Collections.Generic.Dictionary<string, (string prop, int defaut)> _mapChkNullable = new()
    {
        ["ChkDistMin"]      = ("DistanceMin", 1),
        ["ChkDistMax"]      = ("DistanceMax", 8),
        ["ChkCiblePvInf"]   = ("CiblePvInfPourcent", 50),
        ["ChkCiblePvSup"]   = ("CiblePvSupPourcent", 50),
        ["ChkMesPvInf"]     = ("MesPvInfPourcent", 50),
        ["ChkMesPvSup"]     = ("MesPvSupPourcent", 50),
        ["ChkEnnemisMin"]   = ("EnnemisMin", 1),
        ["ChkEnnemisMax"]   = ("EnnemisMax", 8),
        ["ChkTousLesN"]     = ("TousLesNTours", 2),
        ["ChkAPartirTour"]  = ("APartirDuTour", 1),
    };

    private void ChkDistMin_Toggled(object sender, RoutedEventArgs e)        => ToggleNullable("ChkDistMin");
    private void ChkDistMax_Toggled(object sender, RoutedEventArgs e)        => ToggleNullable("ChkDistMax");
    private void ChkCiblePvInf_Toggled(object sender, RoutedEventArgs e)     => ToggleNullable("ChkCiblePvInf");
    private void ChkCiblePvSup_Toggled(object sender, RoutedEventArgs e)     => ToggleNullable("ChkCiblePvSup");
    private void ChkMesPvInf_Toggled(object sender, RoutedEventArgs e)       => ToggleNullable("ChkMesPvInf");
    private void ChkMesPvSup_Toggled(object sender, RoutedEventArgs e)       => ToggleNullable("ChkMesPvSup");
    private void ChkEnnemisMin_Toggled(object sender, RoutedEventArgs e)     => ToggleNullable("ChkEnnemisMin");
    private void ChkEnnemisMax_Toggled(object sender, RoutedEventArgs e)     => ToggleNullable("ChkEnnemisMax");
    private void ChkTousLesN_Toggled(object sender, RoutedEventArgs e)       => ToggleNullable("ChkTousLesN");
    private void ChkAPartirTour_Toggled(object sender, RoutedEventArgs e)    => ToggleNullable("ChkAPartirTour");

    private bool _enToggleCondition;
    private void ToggleNullable(string chkName)
    {
        // H.3 — Évite cascade infinie : si on est déjà en train de modifier
        // un toggle (déclenché par le user), on ignore les events réflectifs
        // qui pourraient cascader (refresh binding → IsChecked re-set → event).
        if (_enToggleCondition) return;
        _enToggleCondition = true;
        try
        {
            if (PanelConditions?.DataContext is not RegleSort r) return;
            if (!_mapChkNullable.TryGetValue(chkName, out var mapping)) return;
            if (FindName(chkName) is not CheckBox cb) return;
            var prop = typeof(RegleSort).GetProperty(mapping.prop);
            if (prop == null) return;

            if (cb.IsChecked == true)
            {
                if (prop.GetValue(r) is null)
                    prop.SetValue(r, (int?)mapping.defaut);
            }
            else
            {
                prop.SetValue(r, null);
            }
            // H.3 — Plus de toggle DataContext (cascade → crash quand l'user
            // coche/décoche vite). Le TextBox bindé TwoWay reflète la valeur
            // quand l'user clique dedans/perd le focus. Pas idéal pour le
            // refresh immédiat de la valeur écrite, mais évite le crash.
            // Long-terme : implémenter INotifyPropertyChanged sur RegleSort.
            DemanderSauvegardeDebouncee();
        }
        catch (Exception ex)
        {
            BotDofus.Utilitaires.Journaux.Journaliseur.Avertir(
                $"[UI-COMBAT] ToggleNullable({chkName}) error : {ex.Message}");
        }
        finally { _enToggleCondition = false; }
    }

    /// <summary>Trigger debounce save quand un input numérique de condition change.</summary>
    private void CondInt_TextChanged(object sender, TextChangedEventArgs e) => DemanderSauvegardeDebouncee();

    /// <summary>Trigger debounce save quand une CheckBox booléenne (sans valeur défaut) toggle.</summary>
    private void CondBool_Click(object sender, RoutedEventArgs e) => DemanderSauvegardeDebouncee();

    /// <summary>Trigger debounce save quand l'ElementRequis change.</summary>
    private void CmbElement_SelectionChanged(object sender, SelectionChangedEventArgs e) => DemanderSauvegardeDebouncee();

    private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is SortConfigureVm vm && _contexte != null)
        {
            ConfigActive!.Regles.Remove(vm.Regle);
            DemanderSauvegardeDebouncee();
            Rafraichir();
        }
    }

    private void BtnMonter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is SortConfigureVm vm && _contexte != null)
        {
            int idx = ConfigActive!.Regles.IndexOf(vm.Regle);
            if (idx > 0)
            {
                ConfigActive!.Regles.RemoveAt(idx);
                ConfigActive!.Regles.Insert(idx - 1, vm.Regle);
                Rafraichir();
            }
        }
    }

    private void BtnDescendre_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is SortConfigureVm vm && _contexte != null)
        {
            int idx = ConfigActive!.Regles.IndexOf(vm.Regle);
            if (idx >= 0 && idx < ConfigActive!.Regles.Count - 1)
            {
                ConfigActive!.Regles.RemoveAt(idx);
                ConfigActive!.Regles.Insert(idx + 1, vm.Regle);
                Rafraichir();
            }
        }
    }

    private void BtnSauver_Click(object sender, RoutedEventArgs e)
    {
        if (_contexte == null) return;
        var chemin = SauverConfigActive();
        if (chemin != null)
            MessageBox.Show($"Sauvegarde : {chemin}", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCopierVers_Click(object sender, RoutedEventArgs e)
    {
        if (_contexte == null || ConfigActive == null) return;
        var fen = new FenetreCopierConfig(_contexte, ConfigActive, _persoCible)
        {
            Owner = Window.GetWindow(this),
        };
        fen.ShowDialog();
    }

    /// <summary>Route la sauvegarde vers peleas/&lt;master&gt;.json ou peleas/heros/&lt;id&gt;.json selon le perso cible courant.</summary>
    private string? SauverConfigActive()
    {
        if (_contexte == null || ConfigActive == null) return null;
        if (_persoCible is null)
        {
            // Master : sauvegarde legacy peleas/<id-compte>.json
            var chemin = Path.Combine("peleas", $"{_contexte.Compte.Identifiant}.json");
            ConfigActive.Sauvegarder(chemin);
            return chemin;
        }
        else
        {
            // Lié : sauvegarde dans peleas/heros/<idJeu>.json via ServiceConfigsHeros.
            BotDofus.Divers.MultiAccount.ServiceConfigsHeros.Sauvegarder(_persoCible);
            return Path.GetFullPath(Path.Combine("peleas", "heros", $"{_persoCible.IdJeu}.json"));
        }
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

    // ---------------------------------------------------------------------
    // Section Mode de combat & Tactique (ADR-001 §4)
    // ---------------------------------------------------------------------

    /// <summary>
    /// Initialise les radios Mode et les sliders depuis la <see cref="ConfigCombat"/>
    /// chargée au démarrage (peleas/&lt;perso&gt;.json). Pose <see cref="_initEnCours"/>
    /// pour empêcher les handlers de déclencher une sauvegarde pendant le set.
    /// </summary>
    private void InitialiserModeEtTactique(ConfigCombat cfg)
    {
        _initEnCours = true;
        try
        {
            CmbStrategie.SelectedIndex = (int)cfg.Strategie;
            switch (cfg.Mode)
            {
                case ModeCombat.Agressif:  RbModeAgressif.IsChecked  = true; break;
                case ModeCombat.Eloigne:   RbModeEloigne.IsChecked   = true; break;
                case ModeCombat.Fuyard:    RbModeFuyard.IsChecked    = true; break;
                default:                   RbModeEquilibre.IsChecked = true; break;
            }
            SldDistancePref.Value = cfg.DistancePreferee;
            TxtDistancePref.Text  = cfg.DistancePreferee.ToString();
            SldDistanceMin.Value  = cfg.DistanceMinEloigne;
            TxtDistanceMin.Text   = cfg.DistanceMinEloigne.ToString();
            SldSeuilFuite.Value   = cfg.SeuilFuitePv;
            TxtSeuilFuite.Text    = cfg.SeuilFuitePv.ToString();
            SldDelaiActions.Value = cfg.DelaiEntreActionsMs;
            TxtDelaiActions.Text  = cfg.DelaiEntreActionsMs.ToString();
        }
        finally
        {
            _initEnCours = false;
        }
    }

    private void ModeCombat_Checked(object sender, RoutedEventArgs e)
    {
        if (_initEnCours || _contexte == null || sender is not RadioButton rb || rb.Tag is not string tag) return;
        if (System.Enum.TryParse<ModeCombat>(tag, out var mode))
        {
            ConfigActive!.Mode = mode;
            DemanderSauvegardeDebouncee();
        }
    }

    private void SldDistancePref_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        int v = (int)e.NewValue;
        if (TxtDistancePref != null) TxtDistancePref.Text = v.ToString();
        if (_initEnCours || _contexte == null) return;
        ConfigActive!.DistancePreferee = v;
        DemanderSauvegardeDebouncee();
    }

    private void SldSeuilFuite_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        int v = (int)e.NewValue;
        if (TxtSeuilFuite != null) TxtSeuilFuite.Text = v.ToString();
        if (_initEnCours || _contexte == null) return;
        ConfigActive!.SeuilFuitePv = v;
        DemanderSauvegardeDebouncee();
    }

    private void SldDistanceMin_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        int v = (int)e.NewValue;
        if (TxtDistanceMin != null) TxtDistanceMin.Text = v.ToString();
        if (_initEnCours || _contexte == null) return;
        ConfigActive!.DistanceMinEloigne = v;
        DemanderSauvegardeDebouncee();
    }

    /// <summary>
    /// Préset Sadida : crée une rotation typique invocations + offensifs +
    /// soin. L'user peut ensuite affiner via le DataGrid.
    /// </summary>
    private void BtnPresetSadida_Click(object sender, RoutedEventArgs e)
    {
        if (_contexte == null) return;
        var rep = MessageBox.Show(
            "Cela va REMPLACER toute la rotation actuelle par un préset Sadida\n"
            + "(La Folle + La Bloqueuse + Ronce + Ronce Apaisante + Larme).\nContinuer ?",
            "Préset Sadida", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (rep != MessageBoxResult.Yes) return;

        var sortsAppris = SortsLookup;
        ConfigActive!.Regles.Clear();
        void Ajout(int id, string nom, FocusSort focus, int prio, int nbParTour = 1,
                   MethodeLancement methode = MethodeLancement.LesDeux, bool premierTour = false,
                   int cooldown = 0)
        {
            if (!sortsAppris.ContainsKey(id)) return;
            ConfigActive!.Regles.Add(new RegleSort
            {
                IdSort = id, Nom = nom, Focus = focus, Priorite = prio,
                NombreParTour = nbParTour, MethodeLancement = methode,
                PremierTour = premierTour, CooldownTours = cooldown,
            });
        }
        // Priorités décroissantes : invocations 1er tour, puis offensifs, puis utilitaires.
        // La Folle = invocation adjacente à MOI avec PRIORISATION (entre moi/ennemi).
        // NombreParTour=3 pour permettre plusieurs poupées sur le même tour si PA dispo.
        Ajout(182, "La Folle",            FocusSort.CelluleAdjacenteMoi, 100, 3, MethodeLancement.LesDeux, premierTour: false);
        Ajout(193, "La Bloqueuse",        FocusSort.CelluleAdjacenteEnnemi, 95, 1, MethodeLancement.LesDeux, premierTour: true);
        Ajout(183, "Ronce",               FocusSort.EnnemiLePlusFaible,   80, 2, MethodeLancement.LesDeux);
        Ajout(195, "Larme",               FocusSort.EnnemiLePlusFaible,   75, 1, MethodeLancement.LesDeux);
        Ajout(200, "Poison Paralysant",   FocusSort.EnnemiLePlusFort,     70, 1, MethodeLancement.LesDeux);
        Ajout(192, "Ronce Apaisante",     FocusSort.AllieLePlusBlesse,    60, 1, MethodeLancement.LesDeux);
        Ajout(197, "Puissance Sylvestre", FocusSort.InvocationLaPlusBlessee, 50, 1, MethodeLancement.LesDeux);

        DemanderSauvegardeDebouncee();
        Rafraichir();
        TxtEtatSauvegarde.Text = $"Préset Sadida appliqué ({ConfigActive!.Regles.Count} règles)";
    }

    /// <summary>
    /// Préset Cra : rotation typique archer à distance.
    /// </summary>
    private void BtnPresetCra_Click(object sender, RoutedEventArgs e)
    {
        if (_contexte == null) return;
        var rep = MessageBox.Show(
            "Cela va REMPLACER toute la rotation actuelle par un préset Cra\n"
            + "(Flèche Magique + Flèche Empoisonnée + Tir Critique + Flèche Punitive).\nContinuer ?",
            "Préset Cra", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (rep != MessageBoxResult.Yes) return;

        var sortsAppris = SortsLookup;
        ConfigActive!.Regles.Clear();
        void Ajout(int id, string nom, FocusSort focus, int prio, int nbParTour = 1,
                   MethodeLancement methode = MethodeLancement.Distance)
        {
            if (!sortsAppris.ContainsKey(id)) return;
            ConfigActive!.Regles.Add(new RegleSort
            {
                IdSort = id, Nom = nom, Focus = focus, Priorite = prio,
                NombreParTour = nbParTour, MethodeLancement = methode,
                IgnorerCAC = true,  // Cra évite le CAC
            });
        }
        Ajout(161, "Flèche Magique",       FocusSort.EnnemiLePlusFaible, 90, 2);
        Ajout(162, "Flèche Punitive",      FocusSort.EnnemiLePlusFaible, 85, 1);
        Ajout(163, "Flèche Empoisonnée",   FocusSort.EnnemiLePlusFort,   80, 1);
        Ajout(164, "Tir Critique",         FocusSort.EnnemiLePlusFort,   75, 1);
        Ajout(165, "Flèche de Recul",      FocusSort.EnnemiLePlusProche, 70, 1, MethodeLancement.CAC);
        Ajout(166, "Flèche Glacée",        FocusSort.EnnemiLePlusFort,   65, 1);
        Ajout(167, "Tir de Diversion",     FocusSort.EnnemiLePlusFaible, 60, 1);

        ConfigActive!.Mode = ModeCombat.Eloigne;  // Cra = kite par défaut
        ConfigActive!.DistanceMinEloigne = 6;
        InitialiserModeEtTactique(ConfigActive!);
        DemanderSauvegardeDebouncee();
        Rafraichir();
        TxtEtatSauvegarde.Text = $"Préset Cra appliqué ({ConfigActive!.Regles.Count} règles, mode=Eloigne)";
    }

    /// <summary>
    /// Auto : crée une rotation basique depuis TOUS les sorts offensifs appris.
    /// Focus = EnnemiLePlusProche, NombreParTour=1, Méthode=LesDeux.
    /// </summary>
    private void BtnPresetOffensifAuto_Click(object sender, RoutedEventArgs e)
    {
        if (_contexte == null) return;
        var rep = MessageBox.Show(
            "Cela va REMPLACER toute la rotation actuelle par tous les sorts offensifs appris.\nContinuer ?",
            "Auto-config", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (rep != MessageBoxResult.Yes) return;
        var def = ConfigCombat.GenererParDefaut(SortsLookup.Keys);
        ConfigActive!.Regles.Clear();
        foreach (var r in def.Regles) ConfigActive!.Regles.Add(r);
        DemanderSauvegardeDebouncee();
        Rafraichir();
        TxtEtatSauvegarde.Text = $"Auto-config offensifs : {def.Regles.Count} règles";
    }

    /// <summary>
    /// Charger une config combat depuis un fichier JSON (peleas/*.json) —
    /// remplace la config courante et re-initialise tous les contrôles UI.
    /// </summary>
    private void BtnCharger_Click(object sender, RoutedEventArgs e)
    {
        if (_contexte == null) return;
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Charger une config combat",
            Filter = "Config JSON (*.json)|*.json|Tous les fichiers|*.*",
            InitialDirectory = System.IO.Path.GetFullPath("peleas"),
        };
        if (dlg.ShowDialog() != true) return;
        var nouvelleConfig = BotDofus.Divers.Combats.IA.ConfigCombat.Charger(dlg.FileName);
        // Remplace la config dans le contexte et resynchronise l'UI
        ConfigActive!.Regles.Clear();
        foreach (var r in nouvelleConfig.Regles) ConfigActive!.Regles.Add(r);
        ConfigActive!.Mode = nouvelleConfig.Mode;
        ConfigActive!.Strategie = nouvelleConfig.Strategie;
        ConfigActive!.DistancePreferee = nouvelleConfig.DistancePreferee;
        ConfigActive!.DistanceMinEloigne = nouvelleConfig.DistanceMinEloigne;
        ConfigActive!.SeuilFuitePv = nouvelleConfig.SeuilFuitePv;
        ConfigActive!.DelaiEntreActionsMs = nouvelleConfig.DelaiEntreActionsMs;
        ConfigActive!.ModeDeplacementOptimisteSecours = nouvelleConfig.ModeDeplacementOptimisteSecours;
        InitialiserModeEtTactique(ConfigActive!);
        Rafraichir();
        TxtEtatSauvegarde.Text = $"Chargé : {System.IO.Path.GetFileName(dlg.FileName)} ({nouvelleConfig.Regles.Count} règle(s))";
    }

    private void SldDelaiActions_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        int v = (int)e.NewValue;
        if (TxtDelaiActions != null) TxtDelaiActions.Text = v.ToString();
        if (_initEnCours || _contexte == null) return;
        ConfigActive!.DelaiEntreActionsMs = v;
        DemanderSauvegardeDebouncee();
    }

    /// <summary>
    /// (Re)déclenche le timer debounce de sauvegarde à 800 ms (ADR-001 §5).
    /// Chaque modif UI repousse l'écriture JSON ; quand l'user arrête de
    /// toucher, le timer tick et persiste. Évite d'écrire à chaque
    /// glissement de slider.
    /// </summary>
    private void DemanderSauvegardeDebouncee()
    {
        if (_contexte == null) return;
        // Indicateur visuel "en attente" pendant le debounce.
        if (TxtEtatSauvegarde != null)
        {
            TxtEtatSauvegarde.Text = "⚠ Modif en attente…";
            TxtEtatSauvegarde.Foreground = (System.Windows.Media.Brush)(FindResource("WarningBrush") ?? System.Windows.Media.Brushes.Orange);
        }
        if (_timerSauvegarde == null)
        {
            _timerSauvegarde = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
            _timerSauvegarde.Tick += (_, _) =>
            {
                _timerSauvegarde!.Stop();
                if (_contexte == null) return;
                _ = SauverConfigActive();
                if (TxtEtatSauvegarde != null)
                {
                    TxtEtatSauvegarde.Text = $"✓ Enregistré ({DateTime.Now:HH:mm:ss})";
                    TxtEtatSauvegarde.Foreground = (System.Windows.Media.Brush)(FindResource("SuccessBrush") ?? System.Windows.Media.Brushes.LightGreen);
                }
            };
        }
        _timerSauvegarde.Stop();
        _timerSauvegarde.Start();
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
    /// <summary>Cellule actuelle du combattant (utile debug).</summary>
    public string CelluleTexte => $"#{_src.CellulePosition}";
    /// <summary>Pourcentage PV pour afficher une barre de vie proportionnelle.</summary>
    public double PourcentagePv => _src.PVMax > 0 ? (100.0 * _src.PV / _src.PVMax) : 0;
    public string PourcentagePvTexte => _src.PVMax > 0 ? $"{(int)PourcentagePv}%" : "-";
    /// <summary>Indique si c'est l'invocation (Sadida poupées etc.) — pas d'humain.</summary>
    public bool EstInvocation => _src.EstInvocation;

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
    /// <summary>Focus du sort (SynFus colonne « Focus »).</summary>
    public string FocusTexte => Regle.Focus switch
    {
        FocusSort.EnnemiLePlusProche => "Ennemi + proche",
        FocusSort.EnnemiLePlusFaible => "Ennemi + faible",
        FocusSort.EnnemiLePlusFort   => "Ennemi + fort",
        FocusSort.Moi                => "Moi",
        FocusSort.AllieLePlusBlesse  => "Allie + blesse",
        FocusSort.CelluleVide        => "Cellule vide",
        _ => "-"
    };
    /// <summary>Méthode de lancement (SynFus colonne « Lancement »).</summary>
    public string MethodeTexte => Regle.MethodeLancement switch
    {
        MethodeLancement.CAC      => "CAC",
        MethodeLancement.Distance => "Distance",
        MethodeLancement.LesDeux  => "Les deux",
        _ => "-"
    };
    public string NombreParTourTexte => Regle.NombreParTour > 0 ? $"x{Regle.NombreParTour}" : "max";
    // Affichage Focus via la NOUVELLE enum FocusSort (12 valeurs vs 6).
    // CibleSort (5 valeurs) est l'alias obsolète pré-N.5 ; on utilise Focus direct.
    public string CibleTexte => Regle.Focus switch
    {
        FocusSort.EnnemiLePlusProche       => "Ennemi le + proche",
        FocusSort.EnnemiLePlusFaible       => "Ennemi le + faible",
        FocusSort.EnnemiLePlusFort         => "Ennemi le + fort",
        FocusSort.EnnemiLePlusLoin         => "Ennemi le + loin",
        FocusSort.Moi                      => "Moi",
        FocusSort.AllieLePlusBlesse        => "Allié le + blessé",
        FocusSort.AllieLePlusProche        => "Allié le + proche",
        FocusSort.AlliePlusGrosHeal        => "Allié plus gros heal",
        FocusSort.InvocationLaPlusBlessee  => "Mes invoc la + blessée",
        FocusSort.InvocationLaPlusProche   => "Mes invoc la + proche",
        FocusSort.CelluleVide              => "Cellule vide",
        FocusSort.CelluleAdjacenteEnnemi   => "Cellule adj. ennemi",
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
