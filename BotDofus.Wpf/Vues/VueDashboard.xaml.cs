using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BotDofus.Commun.Reseau;
using BotDofus.Divers;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Wpf.Vues;

public partial class VueDashboard : UserControl
{
    private ContexteCompte? _contexte;
    private readonly List<EntreeJournal> _toutesLignes = new();
    private const int LimiteLignes = 5000;
    private string _recherche = string.Empty;
    private readonly DispatcherTimer _tickStats;

    public VueDashboard()
    {
        InitializeComponent();
        Journaliseur.NiveauMinimum = NiveauJournal.Debug;
        Journaliseur.EntreeAjoutee += OnEntreeJournal;

        // Tick 1s pour rafraîchir le compteur "temps online" sans dépendre des paquets.
        _tickStats = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _tickStats.Tick += (_, __) => RafraichirStats();
        _tickStats.Start();
    }

    private void TxtLogs_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        // La molette doit VRAIMENT scroller (le TextBox readonly imbriqué ne le
        // faisait pas — un parent captait l'event). On scrolle le ScrollViewer
        // interne nous-mêmes, on coupe l'auto-scroll si on remonte, et on le
        // réactive automatiquement dès qu'on revient en bas.
        var sv = TrouverScrollViewer(TxtLogs);
        if (sv == null) return;

        sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);  // delta>0 = molette vers le haut
        e.Handled = true;

        if (ChkAutoScrollConsole != null)
        {
            bool enBas = sv.VerticalOffset >= sv.ScrollableHeight - 2;
            ChkAutoScrollConsole.IsChecked = enBas; // suit le texte seulement collé en bas
        }
    }

    private static System.Windows.Controls.ScrollViewer? TrouverScrollViewer(System.Windows.DependencyObject? racine)
    {
        if (racine == null) return null;
        if (racine is System.Windows.Controls.ScrollViewer sv) return sv;
        int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(racine);
        for (int i = 0; i < n; i++)
        {
            var r = TrouverScrollViewer(System.Windows.Media.VisualTreeHelper.GetChild(racine, i));
            if (r != null) return r;
        }
        return null;
    }

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
                AjouterLigneAuTextBox(e.Entree);
            }

            if (TxtCount != null)
            {
                TxtCount.Text = $"{ComptageVisible()} / {_toutesLignes.Count} lignes";
            }
        }
        catch { /* un bug d'affichage de log ne doit jamais tuer le bot */ }
    }

    private void AjouterLigneAuTextBox(EntreeJournal entree)
    {
        if (TxtLogs == null) return;
        var ligne = $"[{entree.Horodatage:HH:mm:ss.fff}] {AbreviationNiveau(entree.Niveau)} {entree.Message}\r\n";
        TxtLogs.AppendText(ligne);

        // Auto-scroll si la checkbox est cochée — TxtLogs.ScrollToEnd est éprouvé et fiable.
        if (ChkAutoScrollConsole?.IsChecked == true)
        {
            TxtLogs.ScrollToEnd();
        }
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

        if (!string.IsNullOrEmpty(_recherche)
            && entree.Message.IndexOf(_recherche, StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }
        return true;
    }

    private int ComptageVisible()
    {
        var n = 0;
        foreach (var l in _toutesLignes) if (Correspond(l)) n++;
        return n;
    }

    /// <summary>Reconstruit complètement le contenu du TextBox depuis _toutesLignes filtré.</summary>
    private void RecalculerTexte()
    {
        if (TxtLogs == null || TxtCount == null) return;

        var sb = new StringBuilder();
        foreach (var l in _toutesLignes)
        {
            if (!Correspond(l)) continue;
            sb.Append('[').Append(l.Horodatage.ToString("HH:mm:ss.fff")).Append("] ")
              .Append(AbreviationNiveau(l.Niveau)).Append(' ').Append(l.Message).Append("\r\n");
        }
        TxtLogs.Text = sb.ToString();
        TxtCount.Text = $"{ComptageVisible()} / {_toutesLignes.Count} lignes";

        if (ChkAutoScrollConsole?.IsChecked == true)
        {
            TxtLogs.ScrollToEnd();
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
        if (TxtLogs != null) TxtLogs.Clear();
        if (TxtCount != null) TxtCount.Text = "0 / 0 lignes";
    }

    private static string AbreviationNiveau(NiveauJournal n) => n switch
    {
        NiveauJournal.Trace => "TRC",
        NiveauJournal.Debug => "DBG",
        NiveauJournal.Info => "INF",
        NiveauJournal.Avertissement => "WRN",
        NiveauJournal.Erreur => "ERR",
        NiveauJournal.Critique => "CRT",
        _ => "???",
    };
}
