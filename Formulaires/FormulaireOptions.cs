using System;
using System.Configuration;
using System.Drawing;
using System.Windows.Forms;

namespace BotDofus.Formulaires;

/// <summary>
/// Fenêtre d'options minimaliste : lecture/écriture de App.config
/// (hôte serveur, port distant, port local, niveau de journalisation).
/// </summary>
public sealed class FormulaireOptions : Form
{
    private readonly TextBox _hoteDistant;
    private readonly NumericUpDown _portDistant;
    private readonly NumericUpDown _portLocal;
    private readonly ComboBox _niveauJournal;

    public FormulaireOptions()
    {
        Text = "Options";
        ClientSize = new Size(420, 240);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9f);

        var lblHote = new Label { Text = "Hôte serveur Hystoria :", Left = 16, Top = 16, Width = 200 };
        _hoteDistant = new TextBox { Left = 16, Top = 36, Width = 300 };

        var lblPortDistant = new Label { Text = "Port serveur :", Left = 16, Top = 70, Width = 140 };
        _portDistant = new NumericUpDown { Left = 16, Top = 90, Width = 100, Minimum = 1, Maximum = 65535, Value = 443 };

        var lblPortLocal = new Label { Text = "Port local (proxy) :", Left = 180, Top = 70, Width = 140 };
        _portLocal = new NumericUpDown { Left = 180, Top = 90, Width = 100, Minimum = 1, Maximum = 65535, Value = 5555 };

        var lblJournal = new Label { Text = "Niveau de journalisation :", Left = 16, Top = 124, Width = 200 };
        _niveauJournal = new ComboBox
        {
            Left = 16, Top = 144, Width = 160,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Items = { "Trace", "Debug", "Info", "Avertissement", "Erreur", "Critique" }
        };

        var btnOk = new Button { Text = "Enregistrer", Left = 200, Top = 190, Width = 100, DialogResult = DialogResult.OK };
        btnOk.Click += (_, _) => Sauvegarder();
        var btnAnnuler = new Button { Text = "Annuler", Left = 306, Top = 190, Width = 100, DialogResult = DialogResult.Cancel };

        Controls.AddRange(new Control[]
        {
            lblHote, _hoteDistant,
            lblPortDistant, _portDistant,
            lblPortLocal, _portLocal,
            lblJournal, _niveauJournal,
            btnOk, btnAnnuler
        });

        AcceptButton = btnOk;
        CancelButton = btnAnnuler;

        Charger();
    }

    private void Charger()
    {
        _hoteDistant.Text = ConfigurationManager.AppSettings["ServeurAuth.Hote"] ?? "162.19.127.156";
        if (int.TryParse(ConfigurationManager.AppSettings["ServeurAuth.Port"], out var pd)) _portDistant.Value = pd;
        if (int.TryParse(ConfigurationManager.AppSettings["Proxy.PortLocal"], out var pl)) _portLocal.Value = pl;
        var niveau = ConfigurationManager.AppSettings["Journalisation.Niveau"] ?? "Info";
        _niveauJournal.SelectedIndex = _niveauJournal.Items.IndexOf(niveau);
        if (_niveauJournal.SelectedIndex < 0) _niveauJournal.SelectedIndex = 2;
    }

    private void Sauvegarder()
    {
        // Mise à jour en mémoire seulement — l'écriture dans App.config nécessite
        // ConfigurationManager.OpenExeConfiguration pour modifier le fichier réel.
        try
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            DefinirValeur(config, "ServeurAuth.Hote", _hoteDistant.Text);
            DefinirValeur(config, "ServeurAuth.Port", ((int)_portDistant.Value).ToString());
            DefinirValeur(config, "Proxy.PortLocal", ((int)_portLocal.Value).ToString());
            DefinirValeur(config, "Journalisation.Niveau", _niveauJournal.SelectedItem?.ToString() ?? "Info");
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Options", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void DefinirValeur(System.Configuration.Configuration config, string cle, string valeur)
    {
        if (config.AppSettings.Settings[cle] is null)
        {
            config.AppSettings.Settings.Add(cle, valeur);
        }
        else
        {
            config.AppSettings.Settings[cle].Value = valeur;
        }
    }
}
