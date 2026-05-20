using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BotDofus.Commun.Reseau;
using BotDofus.Divers;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Wpf.Vues;

public partial class VueDashboard : UserControl
{
    private ContexteCompte? _contexte;
    private readonly List<EntreeJournal> _toutesLignes = new();
    private readonly ObservableCollection<LigneLog> _affichees = new();
    private const int LimiteLignes = 5000;
    private string _recherche = string.Empty;
    private readonly DispatcherTimer _tickStats;

    public VueDashboard()
    {
        InitializeComponent();
        Journaliseur.NiveauMinimum = NiveauJournal.Debug;
        TxtLogs.ItemsSource = _affichees;
        Journaliseur.EntreeAjoutee += OnEntreeJournal;

        // Tick 1s pour rafraîchir le compteur "temps online" sans dépendre des paquets.
        _tickStats = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _tickStats.Tick += (_, __) => RafraichirStats();
        _tickStats.Start();
    }

    // (Ancien hack molette LineUp/LineDown + TrouverScrollViewer supprimés :
    //  la ListBox virtualisée a un ScrollViewer natif fiable.)

    public void Lier(ContexteCompte contexte)
    {
        if (ReferenceEquals(_contexte, contexte))
        {
            RafraichirEntete();
            RafraichirStats();
            return;
        }

        if (_contexte != null)
        {
            _contexte.PaquetRecu -= OnPaquetRecu;
            _contexte.Stats.Change -= OnStatsChange;
            _contexte.DetecteurStaff.ModoDetecteChange -= OnModoChange;
        }

        _contexte = contexte;
        contexte.PaquetRecu += OnPaquetRecu;
        contexte.Stats.Change += OnStatsChange;
        contexte.DetecteurStaff.ModoDetecteChange += OnModoChange;
        RafraichirEntete();
        RafraichirStats();
        RafraichirAlerteStaff();
    }

    private void OnModoChange(object? sender, EventArgs e)
        => Dispatcher.BeginInvoke(new Action(RafraichirAlerteStaff), DispatcherPriority.Send);

    private void RafraichirAlerteStaff()
    {
        if (_contexte == null || BandeauAlerteStaff == null) return;
        var det = _contexte.DetecteurStaff;
        if (det.ModoDetecte)
        {
            BandeauAlerteStaff.Visibility = Visibility.Visible;
            var quand = det.DernierModoDetecte?.ToString("HH:mm:ss") ?? "—";
            var pause = det.EnPauseProtection ? " · BOT EN PAUSE PROTECTION" : "";
            if (TxtAlerteDetail != null)
                TxtAlerteDetail.Text = $"Détecté à {quand}{pause}. Configure la réaction dans Vue Config.";
        }
        else
        {
            BandeauAlerteStaff.Visibility = Visibility.Collapsed;
        }
    }

    private void OnPaquetRecu(object? sender, EvenementPaquetRecu e)
        => Dispatcher.BeginInvoke(new Action(RafraichirEntete), DispatcherPriority.Background);

    private void OnStatsChange(object? sender, EventArgs e)
        => Dispatcher.BeginInvoke(new Action(RafraichirStats), DispatcherPriority.Background);

    private void RafraichirEntete()
    {
        if (_contexte == null) return;
        var p = _contexte.EtatJeu.Personnage;
        if (TxtCarte != null) TxtCarte.Text = p.CarteCourante?.ToString() ?? "—";
        if (TxtPosition != null) TxtPosition.Text = p.CellulePosition?.ToString() ?? "—";
        if (TxtKamas != null) TxtKamas.Text = p.Kamas.ToString("N0");

        if (TxtStatut != null)
        {
            TxtStatut.Text = _contexte.SessionJeuActive != null
                ? "✅ En jeu"
                : (_contexte.SessionAuthActive != null ? "⏳ Auth" : "❌ Déconnecté");
        }
    }

    private void RafraichirStats()
    {
        if (_contexte == null) return;
        var s = _contexte.Stats;

        if (TxtStatTemps != null)
        {
            var t = s.TempsEcoule;
            TxtStatTemps.Text = t.TotalHours >= 1
                ? $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}"
                : $"{t.Minutes:D2}:{t.Seconds:D2}";
        }

        if (TxtStatPaquets != null)
            TxtStatPaquets.Text = $"{s.PaquetsRecus:N0} / {s.PaquetsEnvoyes:N0}";

        if (TxtStatOctets != null)
            TxtStatOctets.Text = $"{FormaterOctets(s.OctetsRecus)} / {FormaterOctets(s.OctetsEnvoyes)}";

        if (TxtStatCombats != null)
            TxtStatCombats.Text = s.CombatsTotaux.ToString();

        if (TxtStatKamas != null)
            TxtStatKamas.Text = (s.KamasGagnes >= 0 ? "+" : "") + s.KamasGagnes.ToString("N0");

        if (TxtStatXp != null)
            TxtStatXp.Text = (s.XpGagnee >= 0 ? "+" : "") + s.XpGagnee.ToString("N0");

        if (TxtStatModifies != null)
            TxtStatModifies.Text = s.PaquetsModifies.ToString("N0");

        if (TxtStatAntiBurst != null)
        {
            var h = _contexte.Api.Humaniseur;
            TxtStatAntiBurst.Text = h.Actif ? $"{h.DelaisImposes:N0} délais" : "OFF (passif)";
            TxtStatAntiBurst.Foreground = h.Actif
                ? System.Windows.Media.Brushes.Orange
                : new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x7B, 0xD3, 0x89));
        }
    }

    private static string FormaterOctets(long o)
    {
        if (o < 1024) return $"{o}";
        if (o < 1024 * 1024) return $"{o / 1024.0:F1} K";
        return $"{o / (1024.0 * 1024.0):F2} M";
    }

    private void OnEntreeJournal(object? sender, EvenementEntreeJournal e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(() => OnEntreeJournal(sender, e)));
            return;
        }

        try
        {
            if (_toutesLignes.Count >= LimiteLignes) _toutesLignes.RemoveAt(0);
            _toutesLignes.Add(e.Entree);

            if (Correspond(e.Entree))
            {
                _affichees.Add(Construire(e.Entree));
                while (_affichees.Count > LimiteLignes) _affichees.RemoveAt(0);
                if (ChkAutoScrollConsole?.IsChecked == true && _affichees.Count > 0)
                    TxtLogs.ScrollIntoView(_affichees[^1]);
            }

            if (TxtCount != null)
            {
                TxtCount.Text = $"{ComptageVisible()} / {_toutesLignes.Count} lignes";
            }
        }
        catch { /* un bug d'affichage de log ne doit jamais tuer le bot */ }
    }

    /// <summary>Construit une ligne affichable colorée depuis l'entrée brute.</summary>
    private static LigneLog Construire(EntreeJournal e)
    {
        var cat = CategorieLog.Resoudre(e.Message, e.Niveau);
        Brush pinceau;
        try
        {
            var col = (Color)ColorConverter.ConvertFromString(cat.CouleurHex);
            var b = new SolidColorBrush(col);
            b.Freeze();
            pinceau = b;
        }
        catch { pinceau = Brushes.Gainsboro; }

        return new LigneLog
        {
            Heure = e.Horodatage.ToString("HH:mm:ss.fff"),
            CategorieNom = cat.Nom,
            Message = e.Message,
            Pinceau = pinceau,
        };
    }

    private bool Correspond(EntreeJournal entree)
    {
        var passe = entree.Niveau switch
        {
            NiveauJournal.Trace => ChkDebug.IsChecked == true,
            NiveauJournal.Debug => ChkDebug.IsChecked == true,
            NiveauJournal.Info => ChkInfo.IsChecked == true,
            NiveauJournal.Avertissement => ChkWarn.IsChecked == true,
            NiveauJournal.Erreur => ChkErr.IsChecked == true,
            NiveauJournal.Critique => ChkErr.IsChecked == true,
            _ => true,
        };
        if (!passe) return false;

        // Filtre par CATÉGORIE (chips colorés ligne 2 du header) : si la
        // catégorie n'est pas activée, on cache. Erreur/Alerte sont déjà
        // gérées par les niveaux ci-dessus ; "Info" (fallback inconnu)
        // toujours affiché (sinon on perd des messages internes utiles).
        var catNom = CategorieLog.Resoudre(entree.Message, entree.Niveau).Nom;
        bool catPasse = catNom switch
        {
            "Action"     => ChkCatAction?.IsChecked     == true,
            "Récolte"    => ChkCatRecolte?.IsChecked    == true,
            "Trajet"     => ChkCatTrajet?.IsChecked     == true,
            "Inventaire" => ChkCatInv?.IsChecked        == true,
            "Banque"     => ChkCatBanque?.IsChecked     == true,
            "Commandes"  => ChkCatCmd?.IsChecked        == true,
            "Script"     => ChkCatScript?.IsChecked     == true,
            "Combat"     => ChkCatCombat?.IsChecked     == true,
            "Bot"        => ChkCatBot?.IsChecked        == true,
            "Auth"       => ChkCatAuth?.IsChecked       == true,
            "Server"     => ChkCatServer?.IsChecked     == true,
            "Network"    => ChkCatNetwork?.IsChecked    == true,
            "Game"       => ChkCatGame?.IsChecked       == true,
            "Quête"      => ChkCatQuete?.IsChecked      == true,
            "Important"  => ChkCatImportant?.IsChecked  == true,
            "Réseau"     => ChkCatReseau?.IsChecked     == true,
            "Jeu"        => ChkCatJeu?.IsChecked        == true,
            _            => true,  // Info / Erreur / Alerte : laissés au filtre niveau
        };
        if (!catPasse) return false;

        if (!string.IsNullOrEmpty(_recherche)
            && entree.Message.IndexOf(_recherche, StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }
        return true;
    }

    // ── Filtres par CATÉGORIE (chips colorés) ──────────────────────────
    private bool _ignorerFiltreCatChange;

    private void FiltreCat_Toggle(object sender, RoutedEventArgs e)
    {
        if (_ignorerFiltreCatChange) return;
        RecalculerTexte();
    }

    private void BtnCatTout_Click(object sender, RoutedEventArgs e)
        => DefinirToutesCategories(true);

    private void BtnCatAucun_Click(object sender, RoutedEventArgs e)
        => DefinirToutesCategories(false);

    /// <summary>« Scénario » = seulement la story du bot (Action / Récolte /
    /// Trajet / Important). Coupe le bruit Debug / Réseau / Jeu pour voir
    /// uniquement ce que le bot FAIT, pas comment.</summary>
    private void BtnCatScenario_Click(object sender, RoutedEventArgs e)
    {
        _ignorerFiltreCatChange = true;
        try
        {
            ChkCatAction.IsChecked = true;
            ChkCatRecolte.IsChecked = true;
            ChkCatTrajet.IsChecked = true;
            ChkCatImportant.IsChecked = true;
            ChkCatQuete.IsChecked = true;
            ChkCatBanque.IsChecked = true;
            ChkCatCmd.IsChecked = true;
            ChkCatCombat.IsChecked = true;
            ChkCatBot.IsChecked = true;      // lifecycle (connexion compte / launcher) = utile
            ChkCatGame.IsChecked = true;     // entrée en jeu / sélection perso / zaap = story
            ChkCatInv.IsChecked = false;
            ChkCatScript.IsChecked = false;
            ChkCatAuth.IsChecked = false;
            ChkCatServer.IsChecked = false;
            ChkCatNetwork.IsChecked = false;
            ChkCatReseau.IsChecked = false;
            ChkCatJeu.IsChecked = false;
        }
        finally { _ignorerFiltreCatChange = false; }
        RecalculerTexte();
    }

    private void DefinirToutesCategories(bool valeur)
    {
        _ignorerFiltreCatChange = true;
        try
        {
            ChkCatAction.IsChecked = valeur;
            ChkCatRecolte.IsChecked = valeur;
            ChkCatTrajet.IsChecked = valeur;
            ChkCatInv.IsChecked = valeur;
            ChkCatBanque.IsChecked = valeur;
            ChkCatCmd.IsChecked = valeur;
            ChkCatScript.IsChecked = valeur;
            ChkCatCombat.IsChecked = valeur;
            ChkCatBot.IsChecked = valeur;
            ChkCatAuth.IsChecked = valeur;
            ChkCatServer.IsChecked = valeur;
            ChkCatNetwork.IsChecked = valeur;
            ChkCatGame.IsChecked = valeur;
            ChkCatQuete.IsChecked = valeur;
            ChkCatImportant.IsChecked = valeur;
            ChkCatReseau.IsChecked = valeur;
            ChkCatJeu.IsChecked = valeur;
        }
        finally { _ignorerFiltreCatChange = false; }
        RecalculerTexte();
    }

    private int ComptageVisible()
    {
        var n = 0;
        foreach (var l in _toutesLignes) if (Correspond(l)) n++;
        return n;
    }

    /// <summary>Reconstruit la liste affichée (colorée) depuis _toutesLignes filtré.</summary>
    private void RecalculerTexte()
    {
        if (TxtLogs == null || TxtCount == null) return;

        _affichees.Clear();
        foreach (var l in _toutesLignes)
        {
            if (!Correspond(l)) continue;
            _affichees.Add(Construire(l));
        }
        while (_affichees.Count > LimiteLignes) _affichees.RemoveAt(0);
        TxtCount.Text = $"{ComptageVisible()} / {_toutesLignes.Count} lignes";

        if (ChkAutoScrollConsole?.IsChecked == true && _affichees.Count > 0)
        {
            TxtLogs.ScrollIntoView(_affichees[^1]);
        }
    }

    private void Filtre_Toggle(object sender, RoutedEventArgs e) => RecalculerTexte();

    private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
    {
        _recherche = TxtRecherche?.Text ?? string.Empty;
        RecalculerTexte();
    }

    private void BtnEffacer_Click(object sender, RoutedEventArgs e)
    {
        _toutesLignes.Clear();
        _affichees.Clear();
        if (TxtCount != null) TxtCount.Text = "0 / 0 lignes";
    }
}

/// <summary>Ligne de log affichable et colorée (binding ListBox console).</summary>
public sealed class LigneLog
{
    public string Heure { get; init; } = string.Empty;
    public string CategorieNom { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public Brush Pinceau { get; init; } = Brushes.Gainsboro;
}
