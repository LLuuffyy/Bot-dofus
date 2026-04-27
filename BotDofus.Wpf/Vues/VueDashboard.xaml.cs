using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BotDofus.Divers;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Wpf.Vues;

public partial class VueDashboard : UserControl
{
    private ContexteCompte? _contexte;
    private readonly ObservableCollection<EntreeLogUi> _toutesLignes = new();
    private readonly ObservableCollection<EntreeLogUi> _lignesAffichees = new();
    private const int LimiteLignes = 5000;
    private string _recherche = string.Empty;

    public VueDashboard()
    {
        InitializeComponent();
        ListLogs.ItemsSource = _lignesAffichees;

        Journaliseur.NiveauMinimum = NiveauJournal.Debug;
        Journaliseur.EntreeAjoutee += OnEntreeJournal;
    }

    private void ScrollLogs_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        // Roulette = interaction utilisateur : on décoche auto-scroll pour pas le snapper en bas.
        if (ChkAutoScrollConsole != null) ChkAutoScrollConsole.IsChecked = false;
    }

    public void Lier(ContexteCompte contexte)
    {
        _contexte = contexte;
        contexte.PaquetRecu += (_, __) => Dispatcher.Invoke(RafraichirEntete);
        RafraichirEntete();
    }

    private void RafraichirEntete()
    {
        if (_contexte == null) return;
        var p = _contexte.EtatJeu.Personnage;
        TxtCarte.Text = p.CarteCourante?.ToString() ?? "—";
        TxtPosition.Text = p.CellulePosition?.ToString() ?? "—";
        TxtKamas.Text = p.Kamas.ToString("N0");

        TxtStatut.Text = _contexte.SessionJeuActive != null
            ? "✅ En jeu"
            : (_contexte.SessionAuthActive != null ? "⏳ Auth" : "❌ Déconnecté");
    }

    private void OnEntreeJournal(object? sender, EvenementEntreeJournal e)
    {
        // Marshalling vers le thread UI (Journaliseur peut être appelé depuis n'importe où).
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(() => OnEntreeJournal(sender, e)));
            return;
        }

        try
        {
            var entree = e.Entree;
            var ligne = new EntreeLogUi
            {
                Heure = entree.Horodatage.ToString("HH:mm:ss.fff"),
                Niveau = entree.Niveau,
                NiveauTexte = AbreviationNiveau(entree.Niveau),
                Message = entree.Message,
                Couleur = CouleurNiveau(entree.Niveau),
            };

            if (_toutesLignes.Count >= LimiteLignes) _toutesLignes.RemoveAt(0);
            _toutesLignes.Add(ligne);

            if (Correspond(ligne))
            {
                if (_lignesAffichees.Count >= LimiteLignes) _lignesAffichees.RemoveAt(0);
                _lignesAffichees.Add(ligne);

                // Auto-scroll : on utilise notre ScrollViewer explicite (pas un caché de ListBox).
                // Différé en Background pour que le layout soit fini après l'ajout.
                if (ChkAutoScrollConsole?.IsChecked == true && ScrollLogs != null)
                {
                    Dispatcher.BeginInvoke(new Action(ScrollLogs.ScrollToEnd), System.Windows.Threading.DispatcherPriority.Background);
                }
            }

            if (TxtCount != null)
            {
                TxtCount.Text = $"{_lignesAffichees.Count}/{_toutesLignes.Count} lignes";
            }
        }
        catch
        {
            // Un bug d'affichage de log ne doit jamais tuer le bot.
        }
    }

    private bool Correspond(EntreeLogUi ligne)
    {
        // Filtre par niveau via checkboxes
        var passe = ligne.Niveau switch
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

        // Filtre texte (recherche par sous-chaîne, insensible à la casse)
        if (!string.IsNullOrEmpty(_recherche)
            && ligne.Message.IndexOf(_recherche, StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        return true;
    }

    private void RecalculerListe()
    {
        // Garde : les checkboxes ont IsChecked="True" en XAML, ce qui fire Filtre_Toggle
        // PENDANT InitializeComponent — moment où TxtCount/ListLogs ne sont pas encore créés.
        if (TxtCount == null || ListLogs == null) return;

        _lignesAffichees.Clear();
        foreach (var ligne in _toutesLignes)
        {
            if (Correspond(ligne)) _lignesAffichees.Add(ligne);
        }
        TxtCount.Text = $"{_lignesAffichees.Count}/{_toutesLignes.Count} lignes";

        if (ChkAutoScrollConsole?.IsChecked == true && ScrollLogs != null)
        {
            ScrollLogs.ScrollToEnd();
        }
    }

    private void Filtre_Toggle(object sender, RoutedEventArgs e) => RecalculerListe();

    private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
    {
        _recherche = TxtRecherche?.Text ?? string.Empty;
        RecalculerListe();
    }

    private void BtnEffacer_Click(object sender, RoutedEventArgs e)
    {
        _toutesLignes.Clear();
        _lignesAffichees.Clear();
        if (TxtCount != null) TxtCount.Text = "0/0 lignes";
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

    private static Brush CouleurNiveau(NiveauJournal n) => n switch
    {
        NiveauJournal.Trace => Brushes.Gray,
        NiveauJournal.Debug => new SolidColorBrush(Color.FromRgb(0x9A, 0xA8, 0xB6)),
        NiveauJournal.Info => new SolidColorBrush(Color.FromRgb(0xE0, 0xE5, 0xEC)),
        NiveauJournal.Avertissement => new SolidColorBrush(Color.FromRgb(0xF1, 0xC4, 0x4F)),
        NiveauJournal.Erreur => new SolidColorBrush(Color.FromRgb(0xF0, 0x80, 0x8A)),
        NiveauJournal.Critique => new SolidColorBrush(Color.FromRgb(0xFF, 0x55, 0x66)),
        _ => Brushes.LightGray,
    };
}

public sealed class EntreeLogUi
{
    public string Heure { get; set; } = "";
    public NiveauJournal Niveau { get; set; }
    public string NiveauTexte { get; set; } = "";
    public string Message { get; set; } = "";
    public Brush Couleur { get; set; } = Brushes.White;
}
