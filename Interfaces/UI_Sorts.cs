using System.Drawing;
using System.Windows.Forms;
using BotDofus.Divers;

namespace BotDofus.Interfaces;

/// <summary>
/// Onglet « Sorts » : liste des sorts appris (placeholder — sera rempli quand
/// on aura géré les paquets SL/SU/SB/SF).
/// </summary>
public sealed class UI_Sorts : UserControl
{
    private readonly ListView _liste;

    public UI_Sorts()
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
        _liste.Columns.Add("ID", 60);
        _liste.Columns.Add("Nom", 240);
        _liste.Columns.Add("Niveau", 60);
        _liste.Columns.Add("Position", 80);
        Controls.Add(_liste);
    }

    public void LierContexte(ContexteCompte contexte)
    {
        // TODO : s'abonner à SPELL_LIST_CHANGE / SPELL_UPGRADE pour remplir la liste.
        _ = contexte;
    }
}
