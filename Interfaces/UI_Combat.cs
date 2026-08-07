using System;
using System.Drawing;
using System.Windows.Forms;
using BotDofus.Divers;
using BotDofus.Divers.Combats;

namespace BotDofus.Interfaces;

/// <summary>
/// Onglet « Combat » : affiche l'état du combat en cours (tour actuel, combattants
/// alliés/ennemis avec PV, PA, PM). Sera complété avec les règles IA de l'étape
/// suivante (choix des sorts, positionnement tactique).
/// </summary>
public sealed class UI_Combat : UserControl
{
    private readonly Label _etat;
    private readonly Label _tour;
    private readonly ListView _listeAllies;
    private readonly ListView _listeEnnemis;

    private ContexteCompte? _contexte;

    public UI_Combat()
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(8);
        Font = new Font("Segoe UI", 9f);

        _etat = new Label { Dock = DockStyle.Top, Height = 22, Font = new Font("Segoe UI", 10f, FontStyle.Bold), Text = "État : inactif" };
        _tour = new Label { Dock = DockStyle.Top, Height = 20, Text = "Tour : -" };

        _listeAllies = CreerListe("Alliés");
        _listeEnnemis = CreerListe("Ennemis");

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 400 };
        split.Panel1.Controls.Add(_listeAllies);
        split.Panel2.Controls.Add(_listeEnnemis);

        Controls.Add(split);
        Controls.Add(_tour);
        Controls.Add(_etat);
    }

    private static ListView CreerListe(string titre)
    {
        var lv = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        lv.Columns.Add(titre, 180);
        lv.Columns.Add("PV", 90);
        lv.Columns.Add("PA", 50);
        lv.Columns.Add("PM", 50);
        lv.Columns.Add("Cellule", 70);
        return lv;
    }

    public void LierContexte(ContexteCompte contexte)
    {
        _contexte = contexte;
        contexte.EtatJeu.Combat.EtatChange += (_, etat) => SurInvoke(() => _etat.Text = $"État : {etat}");
        contexte.EtatJeu.Combat.TourChange += (_, id) => SurInvoke(() => _tour.Text = $"Tour : combattant #{id} (tour {contexte.EtatJeu.Combat.NumeroTour})");
        contexte.EtatJeu.Combat.EtatChange += (_, _) => SurInvoke(Rafraichir);
    }

    private void SurInvoke(Action a) { if (InvokeRequired) BeginInvoke(a); else a(); }

    private void Rafraichir()
    {
        if (_contexte is null) return;
        Remplir(_listeAllies, _contexte.EtatJeu.Combat.Allies);
        Remplir(_listeEnnemis, _contexte.EtatJeu.Combat.Ennemis);
    }

    private static void Remplir(ListView lv, System.Collections.Generic.IEnumerable<BotDofus.Divers.Combats.Combattants.Combattant> combattants)
    {
        lv.BeginUpdate();
        try
        {
            lv.Items.Clear();
            foreach (var c in combattants)
            {
                lv.Items.Add(new ListViewItem(new[]
                {
                    c.Nom,
                    $"{c.PV}/{c.PVMax}",
                    c.PA.ToString(),
                    c.PM.ToString(),
                    c.CellulePosition.ToString()
                }));
            }
        }
        finally { lv.EndUpdate(); }
    }
}
