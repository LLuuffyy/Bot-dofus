using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BotDofus.Controles.ProgresBar;

/// <summary>
/// Barre de progression personnalisée affichant la valeur en pourcentage ou en
/// brut au centre, avec couleurs configurables. Utilisée pour les jauges de
/// vie, d'énergie, de poids, d'XP dans UI_Personnage.
/// </summary>
[ToolboxItem(true)]
public sealed class BarreDeProgression : Control
{
    private int _valeur;
    private int _minimum = 0;
    private int _maximum = 100;
    private string _formatTexte = "{0} / {1}";
    private Color _couleurBarre = Color.DodgerBlue;
    private Color _couleurArrierePlan = Color.Gainsboro;

    public BarreDeProgression()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.AllPaintingInWmPaint
               | ControlStyles.UserPaint
               | ControlStyles.ResizeRedraw, true);
        Font = new Font("Segoe UI", 8f, FontStyle.Bold);
        ForeColor = Color.White;
        MinimumSize = new Size(60, 16);
    }

    [Category("Données")]
    public int Valeur { get => _valeur; set { _valeur = value; Invalidate(); } }

    [Category("Données")]
    public int Minimum { get => _minimum; set { _minimum = value; Invalidate(); } }

    [Category("Données")]
    public int Maximum { get => _maximum; set { _maximum = value; Invalidate(); } }

    [Category("Apparence BotDofus")]
    public Color CouleurBarre { get => _couleurBarre; set { _couleurBarre = value; Invalidate(); } }

    [Category("Apparence BotDofus")]
    public Color CouleurArrierePlan { get => _couleurArrierePlan; set { _couleurArrierePlan = value; Invalidate(); } }

    /// <summary>Format appliqué via <see cref="string.Format(string, object, object)"/> ; {0}=valeur, {1}=max.</summary>
    [Category("Apparence BotDofus")]
    public string FormatTexte { get => _formatTexte; set { _formatTexte = value; Invalidate(); } }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var fondBrosse = new SolidBrush(_couleurArrierePlan);
        e.Graphics.FillRectangle(fondBrosse, ClientRectangle);

        if (_maximum > _minimum)
        {
            var ratio = (float)(_valeur - _minimum) / (_maximum - _minimum);
            ratio = System.Math.Clamp(ratio, 0f, 1f);
            var largeur = (int)(ClientRectangle.Width * ratio);
            if (largeur > 0)
            {
                using var barreBrosse = new SolidBrush(_couleurBarre);
                e.Graphics.FillRectangle(barreBrosse, 0, 0, largeur, ClientRectangle.Height);
            }
        }

        var texte = string.Format(_formatTexte, _valeur, _maximum);
        using var texteBrosse = new SolidBrush(ForeColor);
        var taille = e.Graphics.MeasureString(texte, Font);
        var x = (ClientRectangle.Width - taille.Width) / 2;
        var y = (ClientRectangle.Height - taille.Height) / 2;
        e.Graphics.DrawString(texte, Font, texteBrosse, x, y);

        using var bordure = new Pen(Color.FromArgb(120, Color.Black));
        e.Graphics.DrawRectangle(bordure, 0, 0, ClientRectangle.Width - 1, ClientRectangle.Height - 1);
    }
}
