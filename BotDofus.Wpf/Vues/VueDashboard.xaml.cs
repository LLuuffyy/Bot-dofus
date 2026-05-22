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
    // Deux collections séparées : Chat (messages in-game) et Console (tout
    // le reste). Routage par tag à l'arrivée dans OnEntreeJournal. Plus de
    // filtre catégorie : si tu veux la story → onglet Chat, sinon Console.
    private readonly ObservableCollection<LigneLog> _afficheesChat = new();
    private readonly ObservableCollection<LigneLog> _afficheesConsole = new();
    private const int LimiteLignes = 5000;
    private string _recherche = string.Empty;
    private readonly DispatcherTimer _tickStats;

    public VueDashboard()
    {
        InitializeComponent();
        Journaliseur.NiveauMinimum = NiveauJournal.Debug;
        ListeChat.ItemsSource = _afficheesChat;
        ListeConsole.ItemsSource = _afficheesConsole;
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
        {
            var perHeure = s.CombatsParHeure;
            var suffix = perHeure > 0 ? $" ({perHeure:F1}/h)" : "";
            TxtStatCombats.Text = s.CombatsTotaux.ToString() + suffix;
        }

        if (TxtStatKamas != null)
        {
            var perHeure = s.KamasParHeure;
            var suffix = perHeure >= 1000
                ? $" ({perHeure / 1000:F1}k/h)"
                : perHeure > 0 ? $" ({perHeure:F0}/h)" : "";
            TxtStatKamas.Text = (s.KamasGagnes >= 0 ? "+" : "") + s.KamasGagnes.ToString("N0") + suffix;
        }

        if (TxtStatXp != null)
        {
            var perHeure = s.XpParHeure;
            var suffix = perHeure >= 1000
                ? $" ({perHeure / 1000:F1}k/h)"
                : perHeure > 0 ? $" ({perHeure:F0}/h)" : "";
            TxtStatXp.Text = (s.XpGagnee >= 0 ? "+" : "") + s.XpGagnee.ToString("N0") + suffix;
        }

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
                var ligne = Construire(e.Entree);
                // Routage par tag : Chat in-game ([:] [^] [%] $ ? # ! …) →
                // onglet Chat. Tout le reste (bot, récolte, technique…) →
                // Console. Pas de filtre catégorie : 1 ligne = 1 onglet.
                bool estChat = EstChatJeu(e.Entree.Message);
                var cible = estChat ? _afficheesChat : _afficheesConsole;
                cible.Add(ligne);
                while (cible.Count > LimiteLignes) cible.RemoveAt(0);
                if (ChkAutoScrollConsole?.IsChecked == true)
                {
                    var liste = estChat ? ListeChat : ListeConsole;
                    if (cible.Count > 0) liste.ScrollIntoView(cible[^1]);
                }
            }

            if (TxtCount != null)
            {
                TxtCount.Text = $"Chat {_afficheesChat.Count} · Console {_afficheesConsole.Count} · Total {_toutesLignes.Count}";
            }
        }
        catch { /* un bug d'affichage de log ne doit jamais tuer le bot */ }
    }

    /// <summary>
    /// Détecte si une entrée doit aller dans l'onglet « Chat ». Inclut :
    ///  • Le chat in-game : canaux <c>[:]</c> général, <c>[^]</c> allié,
    ///    <c>[%]</c> groupe, <c>[$]</c> commerce, <c>[?]</c> recrutement,
    ///    <c>[#]</c> admin, <c>[!]</c> privé, <c>[*]</c>. Plus <c>[SERVEUR]</c>.
    ///  • Les actions du bot avec tag <c>[ACTION]</c> (story lisible :
    ///    récolte, combat, changement de carte, zaap, level-up…).
    /// Tout le reste va dans « Console ».
    /// </summary>
    private static bool EstChatJeu(string message)
    {
        if (string.IsNullOrEmpty(message) || message[0] != '[') return false;
        var fin = message.IndexOf(']');
        if (fin <= 1) return false;
        var tag = message.Substring(1, fin - 1);
        // Bot actions = story du bot → Chat.
        if (tag.Equals("ACTION", StringComparison.OrdinalIgnoreCase)) return true;
        // Annonces serveur (« Bienvenue sur DOFUS Retro », maintenance…).
        if (tag.Equals("SERVEUR", StringComparison.OrdinalIgnoreCase)) return true;
        // Canaux de chat in-game 1.29 : 1 caractère non alphanumérique.
        return tag.Length == 1 && ":^%$?#!*".Contains(tag[0]);
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

    /// <summary>
    /// Filtre commun aux 2 onglets : niveau (Info/Debug/Warn/Err) +
    /// recherche texte. Le tri Chat vs Console se fait par tag d'entrée
    /// dans OnEntreeJournal (pas par filtre — c'est un routage fixe).
    /// </summary>
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

    /// <summary>Quand l'utilisateur change d'onglet, on (re)scroll en bas
    /// de l'onglet visible si auto-scroll est activé.</summary>
    private void TabLogs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ChkAutoScrollConsole?.IsChecked != true) return;
        if (_afficheesChat.Count > 0 && ListeChat.IsVisible)
            ListeChat.ScrollIntoView(_afficheesChat[^1]);
        if (_afficheesConsole.Count > 0 && ListeConsole.IsVisible)
            ListeConsole.ScrollIntoView(_afficheesConsole[^1]);
    }

    /// <summary>Reconstruit les 2 listes (Chat / Console) depuis _toutesLignes
    /// filtré. Appelée quand le filtre niveau ou la recherche change.</summary>
    private void RecalculerTexte()
    {
        if (TxtCount == null) return;

        _afficheesChat.Clear();
        _afficheesConsole.Clear();
        foreach (var l in _toutesLignes)
        {
            if (!Correspond(l)) continue;
            var ligne = Construire(l);
            if (EstChatJeu(l.Message))
                _afficheesChat.Add(ligne);
            else
                _afficheesConsole.Add(ligne);
        }
        while (_afficheesChat.Count > LimiteLignes) _afficheesChat.RemoveAt(0);
        while (_afficheesConsole.Count > LimiteLignes) _afficheesConsole.RemoveAt(0);

        TxtCount.Text = $"Chat {_afficheesChat.Count} · Console {_afficheesConsole.Count} · Total {_toutesLignes.Count}";

        if (ChkAutoScrollConsole?.IsChecked == true)
        {
            if (_afficheesChat.Count > 0) ListeChat.ScrollIntoView(_afficheesChat[^1]);
            if (_afficheesConsole.Count > 0) ListeConsole.ScrollIntoView(_afficheesConsole[^1]);
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
        _afficheesChat.Clear();
        _afficheesConsole.Clear();
        if (TxtCount != null) TxtCount.Text = "Chat 0 · Console 0 · Total 0";
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
