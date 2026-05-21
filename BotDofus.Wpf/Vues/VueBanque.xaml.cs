using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BotDofus.Divers;
using BotDofus.Divers.Banque;

namespace BotDofus.Wpf.Vues;

/// <summary>
/// Onglet « Banque » : configuration du dépôt automatique. Quand le poids
/// dépasse le seuil, le bot interrompt le farm, zaap à la banque, dépose
/// les items selon les filtres, puis reprend. Protocole Hystoria confirmé
/// (capture user 2026-05-21 : ApS → EMO+&lt;uid&gt;|&lt;qte&gt; → EV).
/// </summary>
public partial class VueBanque : UserControl
{
    private ContexteCompte? _contexte;
    private bool _initEnCours;

    public VueBanque()
    {
        InitializeComponent();
    }

    public void Lier(ContexteCompte ctx)
    {
        _contexte = ctx;
        Rafraichir();
    }

    private string CheminConfig()
    {
        if (_contexte == null) return string.Empty;
        return Path.Combine("banque", $"{_contexte.Compte.Identifiant}.json");
    }

    /// <summary>Pousse la config courante (en mémoire) vers les contrôles UI.</summary>
    private void Rafraichir()
    {
        if (_contexte == null) return;
        _initEnCours = true;
        try
        {
            var cfg = _contexte.ConfigBanque;
            ChkActive.IsChecked = cfg.Active;
            SldSeuil.Value = cfg.SeuilPoidsPct;
            TxtSeuil.Text = cfg.SeuilPoidsPct.ToString();
            SldCible.Value = cfg.CiblePoidsPct;
            TxtCible.Text = cfg.CiblePoidsPct.ToString();
            TxtMapBanque.Text = cfg.MapBanqueId.ToString();
            ChkRetourFarm.IsChecked = cfg.RetourFarmApresDepot;
            TxtDelaiMin.Text = cfg.DelaiActionMinMs.ToString();
            TxtDelaiMax.Text = cfg.DelaiActionMaxMs.ToString();

            ChkEquipements.IsChecked = cfg.DeposerEquipements;
            ChkRessources.IsChecked = cfg.DeposerRessources;
            ChkConsommables.IsChecked = cfg.DeposerConsommables;
            ChkQuetes.IsChecked = cfg.DeposerQuetes;
            ChkInconnus.IsChecked = cfg.DeposerInconnus;

            TxtIdsGarder.Text = string.Join(", ", cfg.IdsAGarder.OrderBy(x => x));
            TxtIdsDeposer.Text = string.Join(", ", cfg.IdsADeposerForce.OrderBy(x => x));
            TxtSeuils.Text = string.Join(Environment.NewLine,
                cfg.SeuilParTemplate.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"));

            TxtEtatLive.Text = cfg.Active
                ? $"✅ Actif — déclenchement automatique à {cfg.SeuilPoidsPct}% de pods"
                : "⏸ Inactif (cocher 'Activer' pour démarrer)";
        }
        finally
        {
            _initEnCours = false;
        }
    }

    // === Lecture des contrôles → ConfigBanque ===
    private static HashSet<int> ParseListeIds(string texte)
    {
        var ids = new HashSet<int>();
        if (string.IsNullOrWhiteSpace(texte)) return ids;
        foreach (var brut in texte.Split(new[] { ',', ';', '\n', '\r', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(brut.Trim(), out var id) && id > 0) ids.Add(id);
        }
        return ids;
    }

    private static Dictionary<int, int> ParseSeuils(string texte)
    {
        var dict = new Dictionary<int, int>();
        if (string.IsNullOrWhiteSpace(texte)) return dict;
        foreach (var ligne in texte.Split(new[] { '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var s = ligne.Trim();
            var sep = s.IndexOf('=');
            if (sep <= 0) continue;
            if (!int.TryParse(s[..sep].Trim(), out var template) || template <= 0) continue;
            if (!int.TryParse(s[(sep + 1)..].Trim(), out var qte) || qte < 0) continue;
            dict[template] = qte;
        }
        return dict;
    }

    private void AppliquerEnConfig()
    {
        if (_contexte == null || _initEnCours) return;
        var cfg = _contexte.ConfigBanque;
        cfg.Active = ChkActive.IsChecked == true;
        cfg.SeuilPoidsPct = (int)SldSeuil.Value;
        cfg.CiblePoidsPct = (int)SldCible.Value;
        if (int.TryParse(TxtMapBanque.Text, out var map) && map > 0) cfg.MapBanqueId = map;
        cfg.RetourFarmApresDepot = ChkRetourFarm.IsChecked == true;
        if (int.TryParse(TxtDelaiMin.Text, out var dmin) && dmin >= 50) cfg.DelaiActionMinMs = dmin;
        if (int.TryParse(TxtDelaiMax.Text, out var dmax) && dmax >= cfg.DelaiActionMinMs) cfg.DelaiActionMaxMs = dmax;

        cfg.DeposerEquipements = ChkEquipements.IsChecked == true;
        cfg.DeposerRessources = ChkRessources.IsChecked == true;
        cfg.DeposerConsommables = ChkConsommables.IsChecked == true;
        cfg.DeposerQuetes = ChkQuetes.IsChecked == true;
        cfg.DeposerInconnus = ChkInconnus.IsChecked == true;

        cfg.IdsAGarder = ParseListeIds(TxtIdsGarder.Text);
        cfg.IdsADeposerForce = ParseListeIds(TxtIdsDeposer.Text);
        cfg.SeuilParTemplate = ParseSeuils(TxtSeuils.Text);

        TxtEtatLive.Text = cfg.Active
            ? $"✅ Actif — déclenchement automatique à {cfg.SeuilPoidsPct}% de pods"
            : "⏸ Inactif (cocher 'Activer' pour démarrer)";
    }

    // === Handlers UI ===
    private void ChkActive_Changed(object sender, RoutedEventArgs e) => AppliquerEnConfig();
    private void SldSeuil_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtSeuil != null) TxtSeuil.Text = ((int)e.NewValue).ToString();
        AppliquerEnConfig();
    }
    private void SldCible_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtCible != null) TxtCible.Text = ((int)e.NewValue).ToString();
        AppliquerEnConfig();
    }
    private void TxtMapBanque_Changed(object sender, TextChangedEventArgs e) => AppliquerEnConfig();
    private void ChkRetourFarm_Changed(object sender, RoutedEventArgs e) => AppliquerEnConfig();
    private void TxtDelai_Changed(object sender, TextChangedEventArgs e) => AppliquerEnConfig();
    private void ChkCategorie_Changed(object sender, RoutedEventArgs e) => AppliquerEnConfig();
    private void TxtIdsGarder_Changed(object sender, TextChangedEventArgs e) => AppliquerEnConfig();
    private void TxtIdsDeposer_Changed(object sender, TextChangedEventArgs e) => AppliquerEnConfig();
    private void TxtSeuils_Changed(object sender, TextChangedEventArgs e) => AppliquerEnConfig();

    private void BtnSauver_Click(object sender, RoutedEventArgs e)
    {
        if (_contexte == null) return;
        AppliquerEnConfig();
        try
        {
            _contexte.ConfigBanque.Sauvegarder(CheminConfig());
            TxtEtatSauvegarde.Text = $"✅ Sauvegardé dans {CheminConfig()}";
        }
        catch (Exception ex)
        {
            TxtEtatSauvegarde.Foreground = System.Windows.Media.Brushes.IndianRed;
            TxtEtatSauvegarde.Text = $"❌ Erreur sauvegarde : {ex.Message}";
        }
    }

    private void BtnRecharger_Click(object sender, RoutedEventArgs e)
    {
        if (_contexte == null) return;
        var fresh = ConfigBanque.Charger(CheminConfig());
        // On copie les champs vers la config existante (préserve la référence).
        var cfg = _contexte.ConfigBanque;
        cfg.Active = fresh.Active;
        cfg.SeuilPoidsPct = fresh.SeuilPoidsPct;
        cfg.CiblePoidsPct = fresh.CiblePoidsPct;
        cfg.MapBanqueId = fresh.MapBanqueId;
        cfg.RetourFarmApresDepot = fresh.RetourFarmApresDepot;
        cfg.DelaiActionMinMs = fresh.DelaiActionMinMs;
        cfg.DelaiActionMaxMs = fresh.DelaiActionMaxMs;
        cfg.DeposerEquipements = fresh.DeposerEquipements;
        cfg.DeposerRessources = fresh.DeposerRessources;
        cfg.DeposerConsommables = fresh.DeposerConsommables;
        cfg.DeposerQuetes = fresh.DeposerQuetes;
        cfg.DeposerInconnus = fresh.DeposerInconnus;
        cfg.IdsAGarder = fresh.IdsAGarder;
        cfg.IdsADeposerForce = fresh.IdsADeposerForce;
        cfg.SeuilParTemplate = fresh.SeuilParTemplate;
        Rafraichir();
        TxtEtatSauvegarde.Text = "🔄 Config rechargée depuis disque";
    }

    private async void BtnTester_Click(object sender, RoutedEventArgs e)
    {
        if (_contexte == null) return;
        var rep = MessageBox.Show(
            "Cela va déclencher MAINTENANT le workflow banque (zaap → dépôt → retour) "
            + "peu importe le poids actuel.\nContinuer ?",
            "Test workflow banque", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (rep != MessageBoxResult.Yes) return;

        AppliquerEnConfig();
        BtnTester.IsEnabled = false;
        TxtEtatSauvegarde.Text = "▶ Workflow banque démarré…";
        try
        {
            var session = _contexte.SessionActive
                ?? throw new InvalidOperationException("Pas de session active.");
            var pilote = new PiloteBanque(_contexte.Api, session, _contexte.EtatJeu.Personnage, _contexte.ConfigBanque);
            int? carteFarm = _contexte.EtatJeu.Personnage.CarteCourante;
            bool ok = await Task.Run(() => pilote.WorkflowCompletAsync(carteFarm)).ConfigureAwait(true);
            TxtEtatSauvegarde.Text = ok
                ? $"✅ Workflow OK — poids final {_contexte.EtatJeu.Personnage.PourcentagePoids:F1}%"
                : $"⚠ Workflow incomplet — poids final {_contexte.EtatJeu.Personnage.PourcentagePoids:F1}%";
        }
        catch (Exception ex)
        {
            TxtEtatSauvegarde.Foreground = System.Windows.Media.Brushes.IndianRed;
            TxtEtatSauvegarde.Text = $"❌ {ex.Message}";
        }
        finally
        {
            BtnTester.IsEnabled = true;
        }
    }
}
