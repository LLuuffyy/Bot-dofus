using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BotDofus.Divers;
using BotDofus.Divers.Enums;
using BotDofus.Divers.Scripts;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Interfaces;

/// <summary>
/// Onglet principal d'un compte : état de connexion, démarrage/arrêt du proxy
/// MITM, chargement d'un script Lua, boutons Play/Pause/Stop.
/// </summary>
public sealed class UI_Principal : UserControl
{
    private readonly Label _etiquetteCompte;
    private readonly Label _etiquetteEtat;
    private readonly Button _btnDemarrerProxy;
    private readonly Button _btnArreterProxy;
    private readonly ComboBox _choixScript;
    private readonly Button _btnDemarrerScript;
    private readonly Button _btnPauseScript;
    private readonly Button _btnArreterScript;
    private readonly Label _etiquetteEtapeCourante;
    private readonly Label _etiquetteCombats;
    private readonly CheckBox _caseEnregistrement;

    private ContexteCompte? _contexte;
    private readonly ChargeurLua _chargeur = new();

    public UI_Principal()
    {
        Dock = DockStyle.Fill;
        Font = new Font("Segoe UI", 9f);
        Padding = new Padding(12);

        _etiquetteCompte = new Label { Left = 12, Top = 12, Width = 400, Height = 24, Font = new Font("Segoe UI", 12f, FontStyle.Bold) };
        _etiquetteEtat   = new Label { Left = 12, Top = 44, Width = 400, Height = 18 };

        _btnDemarrerProxy = new Button { Text = "Démarrer proxy", Left = 12, Top = 80, Width = 130, Height = 30 };
        _btnDemarrerProxy.Click += (_, _) => DemarrerProxy();

        _btnArreterProxy = new Button { Text = "Arrêter proxy", Left = 150, Top = 80, Width = 130, Height = 30, Enabled = false };
        _btnArreterProxy.Click += (_, _) => ArreterProxy();

        var lblScript = new Label { Text = "Script :", Left = 12, Top = 140, Width = 50, Height = 22 };
        _choixScript = new ComboBox { Left = 66, Top = 138, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
        var btnActualiser = new Button { Text = "↻", Left = 352, Top = 138, Width = 28, Height = 22 };
        btnActualiser.Click += (_, _) => ListerScripts();

        _btnDemarrerScript = new Button { Text = "Lancer", Left = 12, Top = 170, Width = 90, Height = 28, Enabled = false };
        _btnPauseScript = new Button { Text = "Pause", Left = 108, Top = 170, Width = 90, Height = 28, Enabled = false };
        _btnArreterScript = new Button { Text = "Stop", Left = 204, Top = 170, Width = 90, Height = 28, Enabled = false };
        _btnDemarrerScript.Click += async (_, _) => await DemarrerScriptAsync();
        _btnPauseScript.Click += (_, _) => AlternerPauseScript();
        _btnArreterScript.Click += (_, _) => ArreterScript();

        _etiquetteEtapeCourante = new Label { Left = 12, Top = 210, Width = 500, Height = 20 };
        _etiquetteCombats       = new Label { Left = 12, Top = 232, Width = 500, Height = 20 };

        _caseEnregistrement = new CheckBox
        {
            Left = 12, Top = 260, Width = 300, Height = 22,
            Text = "Enregistrer les paquets dans un fichier",
            Checked = false
        };
        _caseEnregistrement.CheckedChanged += (_, _) => BasculerEnregistrement();

        Controls.AddRange(new Control[]
        {
            _etiquetteCompte, _etiquetteEtat,
            _btnDemarrerProxy, _btnArreterProxy,
            lblScript, _choixScript, btnActualiser,
            _btnDemarrerScript, _btnPauseScript, _btnArreterScript,
            _etiquetteEtapeCourante, _etiquetteCombats,
            _caseEnregistrement
        });
    }

    public void LierContexte(ContexteCompte contexte)
    {
        _contexte = contexte;
        _etiquetteCompte.Text = $"Compte : {contexte.Compte.Identifiant}";
        Rafraichir();

        contexte.Compte.EtatChange += (_, _) => SurInvoke(Rafraichir);
        contexte.Scripts.EtatChange += (_, _) => SurInvoke(Rafraichir);
        contexte.Scripts.EtapeDemarree += (_, etape) => SurInvoke(() => _etiquetteEtapeCourante.Text = $"Étape : {etape}");

        ListerScripts();
    }

    private void SurInvoke(Action action)
    {
        if (InvokeRequired) BeginInvoke(action);
        else action();
    }

    private void ListerScripts()
    {
        _choixScript.Items.Clear();
        var dossier = Path.Combine(AppContext.BaseDirectory, "trajectories");
        if (!Directory.Exists(dossier)) dossier = "trajectories";
        if (Directory.Exists(dossier))
        {
            foreach (var f in Directory.GetFiles(dossier, "*.lua", SearchOption.AllDirectories))
            {
                _choixScript.Items.Add(f);
            }
        }
        if (_choixScript.Items.Count > 0) _choixScript.SelectedIndex = 0;
    }

    private void DemarrerProxy()
    {
        if (_contexte == null) return;
        try
        {
            _contexte.DemarrerProxy();
            _btnDemarrerProxy.Enabled = false;
            _btnArreterProxy.Enabled = true;
            _btnDemarrerScript.Enabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "Démarrage du proxy impossible :\n\n" + ex.Message,
                "Erreur proxy",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ArreterProxy()
    {
        if (_contexte == null) return;
        _contexte.ArreterProxy();
        _btnDemarrerProxy.Enabled = true;
        _btnArreterProxy.Enabled = false;
        _btnDemarrerScript.Enabled = false;
    }

    private async System.Threading.Tasks.Task DemarrerScriptAsync()
    {
        if (_contexte == null || _choixScript.SelectedItem is not string chemin) return;
        try
        {
            var script = _chargeur.Charger(chemin);
            await _contexte.Scripts.DemarrerAsync(script);
            _btnPauseScript.Enabled = true;
            _btnArreterScript.Enabled = true;
        }
        catch (Exception ex)
        {
            Journaliseur.Erreur("Échec du lancement du script", ex);
            MessageBox.Show(this, ex.Message, "Erreur script", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AlternerPauseScript()
    {
        if (_contexte == null) return;
        if (_contexte.Scripts.Etat == EtatScript.EnPause) _contexte.Scripts.Reprendre();
        else _contexte.Scripts.MettreEnPause();
        _btnPauseScript.Text = _contexte.Scripts.Etat == EtatScript.EnPause ? "Reprendre" : "Pause";
    }

    private void ArreterScript()
    {
        _contexte?.Scripts.Arreter();
        _btnPauseScript.Enabled = false;
        _btnArreterScript.Enabled = false;
        _btnPauseScript.Text = "Pause";
    }

    private void Rafraichir()
    {
        if (_contexte == null) return;
        _etiquetteEtat.Text = $"État : {_contexte.Compte.Etat}" +
                              (string.IsNullOrEmpty(_contexte.Compte.PseudoAffiche)
                                  ? string.Empty
                                  : $"  —  {_contexte.Compte.PseudoAffiche}");
        _etiquetteCombats.Text = $"Combats réalisés : {_contexte.Scripts.CompteurCombats}";
    }

    private void BasculerEnregistrement()
    {
        if (_contexte == null) return;
        if (_caseEnregistrement.Checked) _contexte.ActiverEnregistrement();
        else _contexte.DesactiverEnregistrement();
    }
}
