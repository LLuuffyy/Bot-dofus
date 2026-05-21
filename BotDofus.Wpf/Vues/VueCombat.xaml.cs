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
    /// <summary>Timer debounce de la sauvegarde auto config combat (800 ms, ADR-001 §5).</summary>
    private DispatcherTimer? _timerSauvegarde;
    /// <summary>true pendant l'init des contrôles UI depuis ConfigCombat (évite déclencher les Changed).</summary>
    private bool _initEnCours;
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
            Rafraichir();
            RafraichirSortsAppris();
            RafraichirCombatLive();
            return;
        }

        Detacher();
        _contexte = ctx;
        _personnageLie = ctx.EtatJeu.Personnage;
        _combatLie = ctx.EtatJeu.Combat;
        InitialiserModeEtTactique(ctx.ConfigCombat);
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
        // Snapshot : combat.Allies/Ennemis mutés par le thread réseau.
        foreach (var allie in System.Linq.Enumerable.ToList(combat.Allies))
        {
            CombattantsLive.Add(new CombattantVm(allie, estAllie: true,
                joueActuellement: combat.IdentifiantCombattantActuel == allie.Identifiant));
        }
        foreach (var ennemi in System.Linq.Enumerable.ToList(combat.Ennemis))
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
        // Snapshot ToList() AVANT OrderBy (cf. crash 06:27 VuePersonnage L 84).
        foreach (var (id, niv) in _contexte.EtatJeu.Personnage.SortsAppris.ToList().OrderBy(kv => kv.Key))
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
            if (_contexte.EtatJeu.Personnage.SortsAppris.Count > 0)
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
        if (_contexte == null) return;
        if (CmbSort.SelectedItem is not SortItemVm sortVm)
        {
            MessageBox.Show("Sélectionne un sort dans le combo avant d'ajouter à la rotation.",
                "Validation règle", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        // Validation : doublons (1 règle par sort suffit la plupart du temps,
        // mais l'user peut en vouloir 2 si conditions différentes — juste warn).
        if (_contexte.ConfigCombat.Regles.Any(r => r.IdSort == sortVm.Identifiant))
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

        _contexte.ConfigCombat.Regles.Add(regle);
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
            int niv = _contexte.EtatJeu.Personnage.SortsAppris.TryGetValue(vm.Regle.IdSort, out var n) ? n : 1;
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
            _contexte.ConfigCombat.Regles.Remove(vm.Regle);
            DemanderSauvegardeDebouncee();
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
            _contexte.ConfigCombat.Mode = mode;
            DemanderSauvegardeDebouncee();
        }
    }

    private void SldDistancePref_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        int v = (int)e.NewValue;
        if (TxtDistancePref != null) TxtDistancePref.Text = v.ToString();
        if (_initEnCours || _contexte == null) return;
        _contexte.ConfigCombat.DistancePreferee = v;
        DemanderSauvegardeDebouncee();
    }

    private void SldSeuilFuite_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        int v = (int)e.NewValue;
        if (TxtSeuilFuite != null) TxtSeuilFuite.Text = v.ToString();
        if (_initEnCours || _contexte == null) return;
        _contexte.ConfigCombat.SeuilFuitePv = v;
        DemanderSauvegardeDebouncee();
    }

    private void SldDistanceMin_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        int v = (int)e.NewValue;
        if (TxtDistanceMin != null) TxtDistanceMin.Text = v.ToString();
        if (_initEnCours || _contexte == null) return;
        _contexte.ConfigCombat.DistanceMinEloigne = v;
        DemanderSauvegardeDebouncee();
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
        _contexte.ConfigCombat.Regles.Clear();
        foreach (var r in nouvelleConfig.Regles) _contexte.ConfigCombat.Regles.Add(r);
        _contexte.ConfigCombat.Mode = nouvelleConfig.Mode;
        _contexte.ConfigCombat.Strategie = nouvelleConfig.Strategie;
        _contexte.ConfigCombat.DistancePreferee = nouvelleConfig.DistancePreferee;
        _contexte.ConfigCombat.DistanceMinEloigne = nouvelleConfig.DistanceMinEloigne;
        _contexte.ConfigCombat.SeuilFuitePv = nouvelleConfig.SeuilFuitePv;
        _contexte.ConfigCombat.DelaiEntreActionsMs = nouvelleConfig.DelaiEntreActionsMs;
        _contexte.ConfigCombat.ModeDeplacementOptimisteSecours = nouvelleConfig.ModeDeplacementOptimisteSecours;
        InitialiserModeEtTactique(_contexte.ConfigCombat);
        Rafraichir();
        TxtEtatSauvegarde.Text = $"Chargé : {System.IO.Path.GetFileName(dlg.FileName)} ({nouvelleConfig.Regles.Count} règle(s))";
    }

    private void SldDelaiActions_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        int v = (int)e.NewValue;
        if (TxtDelaiActions != null) TxtDelaiActions.Text = v.ToString();
        if (_initEnCours || _contexte == null) return;
        _contexte.ConfigCombat.DelaiEntreActionsMs = v;
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
                var chemin = Path.Combine("peleas", $"{_contexte.Compte.Identifiant}.json");
                _contexte.ConfigCombat.Sauvegarder(chemin);
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
