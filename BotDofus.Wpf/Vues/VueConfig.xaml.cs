using System.IO;
using System.Windows;
using System.Windows.Controls;
using BotDofus.Divers;
using BotDofus.Divers.Securite;

namespace BotDofus.Wpf.Vues;

public partial class VueConfig : UserControl
{
    private ContexteCompte? _contexte;
    public ConfigSecurite ConfigSecurite { get; private set; } = new();
    public ConfigDelais ConfigDelais { get; private set; } = ConfigDelais.ProfilHumainNormal();
    public ConfigPauses ConfigPauses { get; private set; } = new();

    public VueConfig()
    {
        InitializeComponent();
        // À l'attachement à la fenêtre, applique l'état persisté du mode dev.
        Loaded += (_, _) =>
        {
            try
            {
                var fichier = System.IO.Path.Combine(System.AppContext.BaseDirectory, "ui-prefs.json");
                if (System.IO.File.Exists(fichier))
                {
                    var json = System.IO.File.ReadAllText(fichier);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("ModeDev", out var v) && v.GetBoolean())
                    {
                        ChkModeDev.IsChecked = true;
                        AppliquerModeDev(true);
                    }
                }
            }
            catch { /* swallow — defaults to off */ }
        };
    }

    private void ChkModeDev_Click(object sender, RoutedEventArgs e)
    {
        bool actif = ChkModeDev.IsChecked == true;
        AppliquerModeDev(actif);
        // Persistance simple dans ui-prefs.json à la racine.
        try
        {
            var fichier = System.IO.Path.Combine(System.AppContext.BaseDirectory, "ui-prefs.json");
            var json = System.Text.Json.JsonSerializer.Serialize(new { ModeDev = actif });
            System.IO.File.WriteAllText(fichier, json);
        }
        catch (System.Exception ex)
        {
            BotDofus.Utilitaires.Journaux.Journaliseur.Avertir($"[UI-PREFS] Écriture échec : {ex.Message}");
        }
    }

    private void AppliquerModeDev(bool actif)
    {
        var fen = Window.GetWindow(this);
        if (fen is null) return;
        var sniffer = fen.FindName("OngletSniffer") as TabItem;
        var tools = fen.FindName("OngletTools") as TabItem;
        var visibility = actif ? Visibility.Visible : Visibility.Collapsed;
        if (sniffer is not null) sniffer.Visibility = visibility;
        if (tools is not null) tools.Visibility = visibility;
    }

    public void Lier(ContexteCompte ctx)
    {
        _contexte = ctx;
        var dossier = Path.Combine("config", ctx.Compte.Identifiant);
        ConfigSecurite = ConfigSecurite.Charger(Path.Combine(dossier, "securite.json"));
        ConfigDelais = ConfigDelais.Charger(Path.Combine(dossier, "delais.json"));
        ConfigPauses = ConfigPauses.Charger(Path.Combine(dossier, "pauses.json"));
        Refresher();
    }

    private void Refresher()
    {
        ChkSecuActive.IsChecked = ConfigSecurite.VerificationStaffActive;
        TxtSecuMin.Text = ConfigSecurite.IntervalleCheckMinMin.ToString();
        TxtSecuMax.Text = ConfigSecurite.IntervalleCheckMinMax.ToString();
        ChkArretComplet.IsChecked = ConfigSecurite.ArretCompletSiModo;
        ChkRalentir.IsChecked = ConfigSecurite.RalentirSiModo;
        ChkPausesCombats.IsChecked = ConfigSecurite.PausesEntreCombatsSiModo;
        ChkAlerteUniquement.IsChecked = ConfigSecurite.AlerteUniquementSiModo;

        ChkAfk.IsChecked = ConfigPauses.AfkAleatoireActive;
        TxtAfkInterval.Text = ConfigPauses.AfkIntervalleMin.ToString();
        TxtAfkMin.Text = ConfigPauses.AfkDureeSecMin.ToString();
        TxtAfkMax.Text = ConfigPauses.AfkDureeSecMax.ToString();
        ChkPauseLongue.IsChecked = ConfigPauses.PauseLongueActive;
        ChkFatigue.IsChecked = ConfigPauses.FatigueProgressive;
        ChkHoraires.IsChecked = ConfigPauses.HorairesActifs;
        TxtHoraireDebut.Text = ConfigPauses.HeureDebut.ToString();
        TxtHoraireFin.Text = ConfigPauses.HeureFin.ToString();

        TxtDelaisInfo.Text = $"Action combat {ConfigDelais.ActionCombatGeneral.Min}-{ConfigDelais.ActionCombatGeneral.Max} ms · "
                           + $"Lancer sort {ConfigDelais.LancerSort.Min}-{ConfigDelais.LancerSort.Max} ms · "
                           + $"Déplacement {ConfigDelais.DeplacementMap.Min}-{ConfigDelais.DeplacementMap.Max} ms";
    }

    private void CmbProfil_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ConfigDelais = (CmbProfil.SelectedIndex switch
        {
            0 => ConfigDelais.ProfilInstantane(),
            1 => ConfigDelais.ProfilRapide(),
            2 => ConfigDelais.ProfilHumainNormal(),
            3 => ConfigDelais.ProfilHumainLent(),
            _ => ConfigDelais.ProfilHumainNormal()
        });
        if (TxtDelaisInfo != null) Refresher();
    }

    private void BtnSauver_Click(object sender, RoutedEventArgs e)
    {
        if (_contexte == null) return;
        ConfigSecurite.VerificationStaffActive = ChkSecuActive.IsChecked == true;
        if (int.TryParse(TxtSecuMin.Text, out var sn)) ConfigSecurite.IntervalleCheckMinMin = sn;
        if (int.TryParse(TxtSecuMax.Text, out var sx)) ConfigSecurite.IntervalleCheckMinMax = sx;
        ConfigSecurite.ArretCompletSiModo = ChkArretComplet.IsChecked == true;
        ConfigSecurite.RalentirSiModo = ChkRalentir.IsChecked == true;
        ConfigSecurite.PausesEntreCombatsSiModo = ChkPausesCombats.IsChecked == true;
        ConfigSecurite.AlerteUniquementSiModo = ChkAlerteUniquement.IsChecked == true;

        ConfigPauses.AfkAleatoireActive = ChkAfk.IsChecked == true;
        if (int.TryParse(TxtAfkInterval.Text, out var ai)) ConfigPauses.AfkIntervalleMin = ai;
        if (int.TryParse(TxtAfkMin.Text, out var amn)) ConfigPauses.AfkDureeSecMin = amn;
        if (int.TryParse(TxtAfkMax.Text, out var amx)) ConfigPauses.AfkDureeSecMax = amx;
        ConfigPauses.PauseLongueActive = ChkPauseLongue.IsChecked == true;
        ConfigPauses.FatigueProgressive = ChkFatigue.IsChecked == true;
        ConfigPauses.HorairesActifs = ChkHoraires.IsChecked == true;
        if (int.TryParse(TxtHoraireDebut.Text, out var hd)) ConfigPauses.HeureDebut = hd;
        if (int.TryParse(TxtHoraireFin.Text, out var hf)) ConfigPauses.HeureFin = hf;

        var dossier = Path.Combine("config", _contexte.Compte.Identifiant);
        ConfigSecurite.Sauvegarder(Path.Combine(dossier, "securite.json"));
        ConfigDelais.Sauvegarder(Path.Combine(dossier, "delais.json"));
        ConfigPauses.Sauvegarder(Path.Combine(dossier, "pauses.json"));

        MessageBox.Show("Config sauvegardée", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
