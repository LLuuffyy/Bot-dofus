using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BotDofus.Utilitaires.Config;

namespace BotDofus.Formulaires;

/// <summary>
/// Fenêtre de gestion des comptes bot : liste, ajout, modification,
/// suppression, import/export. Persiste dans comptes.json à côté de l'exécutable.
/// </summary>
public sealed class FormulaireGestionComptes : Form
{
    private readonly ListView _liste;
    private readonly TextBox _identifiant;
    private readonly TextBox _motDePasse;
    private readonly NumericUpDown _serveurPrefere;
    private readonly NumericUpDown _personnagePrefere;
    private readonly TextBox _commentaire;

    public List<EntreeCompte> Comptes { get; private set; }

    public FormulaireGestionComptes()
    {
        Text = "Gestion des comptes";
        ClientSize = new Size(780, 420);
        MinimumSize = Size;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9f);

        Comptes = FichierComptes.Charger();

        _liste = new ListView
        {
            Dock = DockStyle.Left,
            Width = 360,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = false
        };
        _liste.Columns.Add("Identifiant", 160);
        _liste.Columns.Add("Serveur", 70);
        _liste.Columns.Add("Personnage", 90);
        _liste.Columns.Add("Commentaire", 200);
        _liste.SelectedIndexChanged += (_, _) => ChargerSelection();

        var panneauDroit = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };

        var lblIdentifiant = new Label { Text = "Identifiant :", Left = 12, Top = 12, Width = 100 };
        _identifiant = new TextBox { Left = 12, Top = 32, Width = 380 };

        var lblMdp = new Label { Text = "Mot de passe :", Left = 12, Top = 62, Width = 100 };
        _motDePasse = new TextBox { Left = 12, Top = 82, Width = 380, UseSystemPasswordChar = true };

        var lblServeur = new Label { Text = "Serveur préféré (ID, 0 = auto) :", Left = 12, Top = 112, Width = 230 };
        _serveurPrefere = new NumericUpDown { Left = 12, Top = 132, Width = 120, Minimum = 0, Maximum = 9999 };

        var lblPerso = new Label { Text = "Personnage préféré (ID, 0 = auto) :", Left = 152, Top = 112, Width = 230 };
        _personnagePrefere = new NumericUpDown { Left = 152, Top = 132, Width = 150, Minimum = 0, Maximum = long.MaxValue };

        var lblCommentaire = new Label { Text = "Commentaire :", Left = 12, Top = 168, Width = 100 };
        _commentaire = new TextBox { Left = 12, Top = 188, Width = 380, Multiline = true, Height = 60 };

        var btnAjouter = new Button { Text = "Ajouter", Left = 12, Top = 268, Width = 90 };
        btnAjouter.Click += (_, _) => AjouterCompte();
        var btnMettreAJour = new Button { Text = "Mettre à jour", Left = 108, Top = 268, Width = 100 };
        btnMettreAJour.Click += (_, _) => MettreAJour();
        var btnSupprimer = new Button { Text = "Supprimer", Left = 214, Top = 268, Width = 90 };
        btnSupprimer.Click += (_, _) => Supprimer();

        var btnSauvegarder = new Button { Text = "Enregistrer", Left = 12, Top = 310, Width = 110, DialogResult = DialogResult.OK };
        btnSauvegarder.Click += (_, _) => SauvegarderEtFermer();
        var btnAnnuler = new Button { Text = "Annuler", Left = 128, Top = 310, Width = 90, DialogResult = DialogResult.Cancel };

        panneauDroit.Controls.AddRange(new Control[]
        {
            lblIdentifiant, _identifiant,
            lblMdp, _motDePasse,
            lblServeur, _serveurPrefere,
            lblPerso, _personnagePrefere,
            lblCommentaire, _commentaire,
            btnAjouter, btnMettreAJour, btnSupprimer,
            btnSauvegarder, btnAnnuler
        });

        Controls.Add(panneauDroit);
        Controls.Add(_liste);

        AcceptButton = btnSauvegarder;
        CancelButton = btnAnnuler;

        Rafraichir();
    }

    private void Rafraichir()
    {
        _liste.Items.Clear();
        foreach (var c in Comptes)
        {
            _liste.Items.Add(new ListViewItem(new[]
            {
                c.Identifiant,
                c.ServeurPrefere.ToString(),
                c.PersonnagePrefere.ToString(),
                c.Commentaire
            }));
        }
    }

    private void ChargerSelection()
    {
        if (_liste.SelectedIndices.Count == 0) return;
        var c = Comptes[_liste.SelectedIndices[0]];
        _identifiant.Text = c.Identifiant;
        _motDePasse.Text = c.MotDePasse;
        _serveurPrefere.Value = c.ServeurPrefere;
        _personnagePrefere.Value = c.PersonnagePrefere;
        _commentaire.Text = c.Commentaire;
    }

    private void AjouterCompte()
    {
        if (string.IsNullOrWhiteSpace(_identifiant.Text)) return;
        if (Comptes.Any(x => string.Equals(x.Identifiant, _identifiant.Text, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(this, "Ce compte existe déjà.", "Gestion des comptes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        Comptes.Add(new EntreeCompte
        {
            Identifiant = _identifiant.Text.Trim(),
            MotDePasse = _motDePasse.Text,
            ServeurPrefere = (int)_serveurPrefere.Value,
            PersonnagePrefere = (int)_personnagePrefere.Value,
            Commentaire = _commentaire.Text
        });
        Rafraichir();
    }

    private void MettreAJour()
    {
        if (_liste.SelectedIndices.Count == 0) return;
        var c = Comptes[_liste.SelectedIndices[0]];
        c.Identifiant = _identifiant.Text.Trim();
        c.MotDePasse = _motDePasse.Text;
        c.ServeurPrefere = (int)_serveurPrefere.Value;
        c.PersonnagePrefere = (int)_personnagePrefere.Value;
        c.Commentaire = _commentaire.Text;
        Rafraichir();
    }

    private void Supprimer()
    {
        if (_liste.SelectedIndices.Count == 0) return;
        Comptes.RemoveAt(_liste.SelectedIndices[0]);
        Rafraichir();
    }

    private void SauvegarderEtFermer()
    {
        FichierComptes.Sauvegarder(Comptes);
        DialogResult = DialogResult.OK;
        Close();
    }
}
