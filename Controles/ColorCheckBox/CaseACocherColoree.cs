using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BotDofus.Controles.ColorCheckBox;

/// <summary>
/// CheckBox personnalisée avec couleur de fond configurable selon l'état coché,
/// rendu plat sans bordure 3D. Reprend l'esprit du contrôle ColorCheckBox de
/// dyshay/Bot-Dofus-Retro.
/// </summary>
[ToolboxItem(true)]
public sealed class CaseACocherColoree : CheckBox
{
    private Color _couleurCoche = Color.MediumSeaGreen;
    private Color _couleurNonCoche = Color.Gainsboro;

    public CaseACocherColoree()
    {
        FlatStyle = FlatStyle.Flat;
        Appearance = Appearance.Button;
        TextAlign = ContentAlignment.MiddleCenter;
        FlatAppearance.BorderSize = 0;
        MinimumSize = new Size(24, 24);
        AjusterCouleurs();
    }

    [Category("Apparence BotDofus")]
    public Color CouleurCoche
    {
        get => _couleurCoche;
        set { _couleurCoche = value; AjusterCouleurs(); }
    }

    [Category("Apparence BotDofus")]
    public Color CouleurNonCoche
    {
        get => _couleurNonCoche;
        set { _couleurNonCoche = value; AjusterCouleurs(); }
    }

    protected override void OnCheckedChanged(System.EventArgs e)
    {
        base.OnCheckedChanged(e);
        AjusterCouleurs();
    }

    private void AjusterCouleurs()
    {
        BackColor = Checked ? _couleurCoche : _couleurNonCoche;
        ForeColor = Checked ? Color.White : Color.Black;
    }
}
