using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using BotDofus.Divers;
using Microsoft.Win32;

namespace BotDofus.Wpf.Vues;

public partial class VueScripts : UserControl
{
    private ContexteCompte? _contexte;
    private string? _cheminCharge;

    public VueScripts()
    {
        InitializeComponent();
        Loaded += (_, __) => RafraichirListe();
        IsVisibleChanged += (_, __) => { if (IsVisible) RafraichirListe(); };
    }

    private static string DossierScripts()
        => Directory.Exists("scripts") ? Path.GetFullPath("scripts")
                                       : Environment.CurrentDirectory;

    /// <summary>Liste tous les .lua du dossier scripts (trajets enregistrés
    /// inclus), le plus récent en haut. Un clic charge le script.</summary>
    private void RafraichirListe()
    {
        if (ListeScripts == null) return;
        var selErr = (ListeScripts.SelectedItem as ListBoxItem)?.Tag as string;
        ListeScripts.Items.Clear();
        try
        {
            var dossier = DossierScripts();
            if (!Directory.Exists(dossier)) return;
            var fichiers = new DirectoryInfo(dossier).GetFiles("*.lua");
            Array.Sort(fichiers, (a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
            foreach (var f in fichiers)
            {
                var item = new ListBoxItem
                {
                    Content = $"{f.Name}\n{f.LastWriteTime:dd/MM HH:mm}",
                    Tag = f.FullName,
                    ToolTip = f.FullName
                };
                ListeScripts.Items.Add(item);
                if (f.FullName == selErr) ListeScripts.SelectedItem = item;
            }
        }
        catch { /* dossier illisible : liste vide */ }
    }

    private void BtnRafraichir_Click(object sender, RoutedEventArgs e) => RafraichirListe();

    private void ListeScripts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListeScripts.SelectedItem is not ListBoxItem it || it.Tag is not string chemin)
            return;
        if (!File.Exists(chemin)) { RafraichirListe(); return; }
        _cheminCharge = chemin;
        TxtFichier.Text = Path.GetFileName(chemin);
        try { TxtScriptContenu.Text = File.ReadAllText(chemin); } catch { }
        BtnDemarrer.IsEnabled = _contexte != null
            && !(_contexte?.Lua.EnExecution ?? false);
        BtnEnregistrer.IsEnabled = ChkEditable?.IsChecked == true;
    }

    public void Lier(ContexteCompte contexte)
    {
        _contexte = contexte;
        contexte.Lua.ExecutionDemarree += (_, __) => Dispatcher.Invoke(() => MajBoutons(true));
        contexte.Lua.ExecutionTerminee += (_, __) => Dispatcher.Invoke(() =>
        { MajBoutons(false); RafraichirListe(); });
        contexte.Lua.ExecutionErreur += (_, ex) => Dispatcher.Invoke(() =>
        {
            MajBoutons(false);
            MessageBox.Show($"Erreur Lua : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }

    private void MajBoutons(bool enExecution)
    {
        BtnDemarrer.IsEnabled = !enExecution && _cheminCharge != null;
        BtnArreter.IsEnabled = enExecution;
        BtnCharger.IsEnabled = !enExecution;
    }

    private void BtnCharger_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Scripts Lua (*.lua)|*.lua",
            InitialDirectory = Directory.Exists("scripts") ? Path.GetFullPath("scripts") : Environment.CurrentDirectory
        };
        if (dlg.ShowDialog() == true)
        {
            _cheminCharge = dlg.FileName;
            TxtFichier.Text = Path.GetFileName(_cheminCharge);
            TxtScriptContenu.Text = File.ReadAllText(_cheminCharge);
            BtnDemarrer.IsEnabled = _contexte != null;
            BtnEnregistrer.IsEnabled = ChkEditable?.IsChecked == true;
        }
    }

    private void ChkEditable_Toggled(object sender, RoutedEventArgs e)
    {
        if (TxtScriptContenu == null) return;
        var editable = ChkEditable?.IsChecked == true;
        TxtScriptContenu.IsReadOnly = !editable;
        if (BtnEnregistrer != null) BtnEnregistrer.IsEnabled = editable && _cheminCharge != null;
    }

    private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
    {
        if (_cheminCharge == null) return;
        try
        {
            File.WriteAllText(_cheminCharge, TxtScriptContenu.Text ?? "");
            MessageBox.Show($"Sauvegardé : {Path.GetFileName(_cheminCharge)}",
                "OK", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur sauvegarde : {ex.Message}",
                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDemarrer_Click(object sender, RoutedEventArgs e)
    {
        if (_contexte == null || _cheminCharge == null) return;
        try
        {
            _contexte.Lua.Charger(_cheminCharge);
            _contexte.Lua.Demarrer();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Échec démarrage : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnArreter_Click(object sender, RoutedEventArgs e)
    {
        _contexte?.Lua.Arreter();
        MajBoutons(false);
    }
}
