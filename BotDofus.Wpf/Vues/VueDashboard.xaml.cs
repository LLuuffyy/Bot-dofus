using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using BotDofus.Divers;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Wpf.Vues;

public partial class VueDashboard : UserControl
{
    private ContexteCompte? _contexte;
    private readonly List<EntreeJournal> _toutesLignes = new();
    private const int LimiteLignes = 5000;
    private string _recherche = string.Empty;

    public VueDashboard()
    {
        InitializeComponent();
        Journaliseur.NiveauMinimum = NiveauJournal.Debug;
        Journaliseur.EntreeAjoutee += OnEntreeJournal;
    }

    private void TxtLogs_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        // Roulette = interaction utilisateur : on décoche auto-scroll pour pas snapper en bas.
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
