using System.Drawing;
using System.Windows.Forms;
using BotDofus.Divers;

namespace BotDofus.Interfaces;

/// <summary>
/// Onglet « Métiers » : liste des métiers appris avec XP, niveau et compétences.
/// Sera rempli via les paquets JOB_SKILLS / JOB_XP / JOB_LEVEL.
/// </summary>
public sealed class UI_Metiers : UserControl
{
    private readonly ListView _liste;

    public UI_Metiers()
    {
        Dock = DockStyle.Fill;
        _liste = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = new Font("Segoe UI", 9f)
        };
        _liste.Columns.Add("Métier", 180);
        _liste.Columns.Add("Niveau", 70);
        _liste.Columns.Add("XP", 120);
        _liste.Columns.Add("Palier suivant", 120);
        Controls.Add(_liste);
    }

    public void LierContexte(ContexteCompte contexte)
    {
        _ = contexte;
    }
}
