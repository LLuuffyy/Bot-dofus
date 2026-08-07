using System;
using System.Drawing;
using System.Windows.Forms;
using BotDofus.Divers;

namespace BotDofus.Interfaces;

/// <summary>
/// Onglet « Inventaire » : liste des objets portés par le personnage, avec
/// identifiant, quantité, emplacement d'équipement.
/// </summary>
public sealed class UI_Inventaire : UserControl
{
    private readonly ListView _listeObjets;
    private readonly Label _totaux;

    private ContexteCompte? _contexte;

    public UI_Inventaire()
    {
        Dock = DockStyle.Fill;
        Font = new Font("Segoe UI", 9f);

        _totaux = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Padding = new Padding(8, 4, 0, 0),
            Text = "0 objets"
        };

        _listeObjets = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        _listeObjets.Columns.Add("ID", 60);
        _listeObjets.Columns.Add("Gabarit", 80);
        _listeObjets.Columns.Add("Quantité", 80);
        _listeObjets.Columns.Add("Emplacement", 100);
        _listeObjets.Columns.Add("Effets (bruts)", 500);

        Controls.Add(_listeObjets);
        Controls.Add(_totaux);
    }

    public void LierContexte(ContexteCompte contexte)
    {
        _contexte = contexte;
        contexte.EtatJeu.Personnage.Mis_A_Jour += (_, _) => SurInvoke(Rafraichir);
        Rafraichir();
    }

    private void SurInvoke(Action a) { if (InvokeRequired) BeginInvoke(a); else a(); }

    private void Rafraichir()
    {
        if (_contexte is null) return;
        var inv = _contexte.EtatJeu.Personnage.Inventaire;
        _listeObjets.BeginUpdate();
        try
        {
            _listeObjets.Items.Clear();
            foreach (var obj in inv)
            {
                _listeObjets.Items.Add(new ListViewItem(new[]
                {
                    obj.Identifiant.ToString(),
                    obj.IdTemplate.ToString(),
                    obj.Quantite.ToString(),
                    obj.Position.ToString(),
                    string.Join(" | ", obj.EffetsBruts)
                }));
            }
            _totaux.Text = $"{inv.Count} objets";
        }
        finally { _listeObjets.EndUpdate(); }
    }
}
