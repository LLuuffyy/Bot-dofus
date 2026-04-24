using System;
using System.Drawing;
using System.Windows.Forms;
using BotDofus.Controles.ProgresBar;
using BotDofus.Divers;

namespace BotDofus.Interfaces;

/// <summary>
/// Onglet « Personnage » : affiche le pseudo, le niveau, la classe, les jauges
/// de vie / énergie / XP / poids, les kamas, et les caractéristiques.
/// </summary>
public sealed class UI_Personnage : UserControl
{
    private readonly Label _titre;
    private readonly Label _classe;
    private readonly Label _kamas;
    private readonly BarreDeProgression _barreVie;
    private readonly BarreDeProgression _barreEnergie;
    private readonly BarreDeProgression _barreXp;
    private readonly BarreDeProgression _barrePoids;
    private readonly Label _pointsCarac;
    private readonly Label _pointsSorts;

    private ContexteCompte? _contexte;

    public UI_Personnage()
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(12);
        BackColor = Color.White;

        _titre = new Label { Left = 12, Top = 12, Width = 500, Height = 28, Font = new Font("Segoe UI", 14f, FontStyle.Bold) };
        _classe = new Label { Left = 12, Top = 44, Width = 500, Height = 20 };
        _kamas = new Label { Left = 12, Top = 68, Width = 500, Height = 20, ForeColor = Color.Goldenrod, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };

        _barreVie = CreerBarre(12, 100, Color.Crimson, "Vie : {0} / {1}");
        _barreEnergie = CreerBarre(12, 126, Color.MediumPurple, "Énergie : {0} / {1}");
        _barreXp = CreerBarre(12, 152, Color.DarkOrange, "XP : {0} / {1}");
        _barrePoids = CreerBarre(12, 178, Color.SteelBlue, "Poids : {0} / {1}");

        _pointsCarac = new Label { Left = 12, Top = 216, Width = 500, Height = 20 };
        _pointsSorts = new Label { Left = 12, Top = 238, Width = 500, Height = 20 };

        Controls.AddRange(new Control[]
        {
            _titre, _classe, _kamas,
            _barreVie, _barreEnergie, _barreXp, _barrePoids,
            _pointsCarac, _pointsSorts
        });
    }

    private static BarreDeProgression CreerBarre(int x, int y, Color couleur, string format)
        => new()
        {
            Left = x, Top = y, Width = 360, Height = 22,
            CouleurBarre = couleur,
            FormatTexte = format
        };

    public void LierContexte(ContexteCompte contexte)
    {
        _contexte = contexte;
        contexte.EtatJeu.Personnage.Mis_A_Jour += (_, _) => SurInvoke(Rafraichir);
        contexte.Compte.EtatChange += (_, _) => SurInvoke(Rafraichir);
        Rafraichir();
    }

    private void SurInvoke(Action a) { if (InvokeRequired) BeginInvoke(a); else a(); }

    private void Rafraichir()
    {
        if (_contexte == null) return;
        var p = _contexte.EtatJeu.Personnage;

        _titre.Text = string.IsNullOrEmpty(p.Nom) ? "(aucun personnage sélectionné)" : $"{p.Nom}  —  niveau {p.Niveau}";
        _classe.Text = $"Classe #{p.IdClasse}  |  Sexe : {(p.Sexe == 0 ? "Homme" : "Femme")}";
        _kamas.Text = $"{p.Kamas:N0} kamas";

        _barreVie.Valeur = p.Vie;
        _barreVie.Maximum = System.Math.Max(1, p.VieMax);
        _barreEnergie.Valeur = p.Energie;
        _barreEnergie.Maximum = System.Math.Max(1, p.EnergieMax);

        _barreXp.Valeur = (int)(p.XpActuelle - p.XpPalierCourant);
        _barreXp.Maximum = System.Math.Max(1, (int)(p.XpPalierSuivant - p.XpPalierCourant));

        _barrePoids.Valeur = p.PoidsActuel;
        _barrePoids.Maximum = System.Math.Max(1, p.PoidsMax);

        _pointsCarac.Text = $"Points de caractéristique à dépenser : {p.PointsCaracteristiques}";
        _pointsSorts.Text = $"Points de sort à dépenser : {p.PointsSorts}";
    }
}
