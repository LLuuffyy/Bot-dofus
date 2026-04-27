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
    }

    public void Lier(ContexteCompte contexte)
    {
        _contexte = contexte;
        contexte.Lua.ExecutionDemarree += (_, __) => Dispatcher.Invoke(() => MajBoutons(true));
        contexte.Lua.ExecutionTerminee += (_, __) => Dispatcher.Invoke(() => MajBoutons(false));
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
