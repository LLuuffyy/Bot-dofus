using System;
using System.Drawing;
using System.Windows.Forms;
using BotDofus.Divers;
using BotDofus.Divers.Cartes;

namespace BotDofus.Interfaces;

/// <summary>
/// Onglet « Carte » : rendu 2D simplifié de la carte courante.
/// Chaque cellule est peinte selon sa marchabilité et les entités
/// présentes (joueurs, monstres, PNJ, ressources).
/// </summary>
public sealed class UI_Carte : UserControl
{
    private const int TailleCellulePx = 16;

    private ContexteCompte? _contexte;
    private readonly Label _labelEntete;
    private readonly DoubleBufferedPanel _panneauCarte;

    /// <summary>Panel avec double buffer activé (protected accessible en héritant).</summary>
    private sealed class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel() { DoubleBuffered = true; }
    }

    public UI_Carte()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(240, 240, 240);

        _labelEntete = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Text = "Carte : (aucune)"
        };

        _panneauCarte = new DoubleBufferedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(60, 80, 60)
        };
        _panneauCarte.Paint += OnPaintCarte;

        Controls.Add(_panneauCarte);
        Controls.Add(_labelEntete);
    }

    public void LierContexte(ContexteCompte contexte)
    {
        _contexte = contexte;
        contexte.EtatJeu.CarteChangee += (_, _) =>
        {
            SurInvoke(() =>
            {
                _labelEntete.Text = $"Carte : #{contexte.EtatJeu.CarteCourante?.Identifiant}";
                _panneauCarte.Invalidate();
            });
        };
    }

    private void SurInvoke(Action a) { if (InvokeRequired) BeginInvoke(a); else a(); }

    private void OnPaintCarte(object? sender, PaintEventArgs e)
    {
        var carte = _contexte?.EtatJeu.CarteCourante;
        if (carte is null) return;

        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;

        for (int i = 0; i < carte.Cellules.Length; i++)
        {
            var cellule = carte.Cellules[i];
            var couleur = cellule.Type switch
            {
                TypesCellule.Obstacle => Color.FromArgb(60, 60, 60),
                TypesCellule.LignDeVueSeule => Color.FromArgb(110, 140, 110),
                TypesCellule.Interactif => Color.Goldenrod,
                TypesCellule.Zaap => Color.DeepSkyBlue,
                TypesCellule.Zaapi => Color.MediumTurquoise,
                TypesCellule.Transition => Color.Violet,
                _ => Color.FromArgb(120, 170, 120)
            };

            using var brosse = new SolidBrush(couleur);
            var x = cellule.X * TailleCellulePx;
            var y = cellule.Y * TailleCellulePx;
            g.FillRectangle(brosse, x, y, TailleCellulePx - 1, TailleCellulePx - 1);
        }

        // Positionnement du personnage sur la carte.
        if (_contexte?.EtatJeu.Personnage.CellulePosition is int idCasePerso
            && carte.Obtenir(idCasePerso) is { } cellPerso)
        {
            using var pinceau = new SolidBrush(Color.Cyan);
            g.FillEllipse(pinceau,
                cellPerso.X * TailleCellulePx + 2,
                cellPerso.Y * TailleCellulePx + 2,
                TailleCellulePx - 4, TailleCellulePx - 4);
        }
    }
}
