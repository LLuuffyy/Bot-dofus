using System;
using System.Windows.Forms;

namespace BotDofus.Formulaires;

public partial class FormulairePrincipal : Form
{
    public FormulairePrincipal()
    {
        InitializeComponent();
    }

    private void FormulairePrincipal_Load(object sender, EventArgs e)
    {
        // TODO : initialiser la liste des comptes chargés depuis accounts.bot
        //        charger les ressources (maps, items, sorts) via GestionnaireRessources
        //        afficher la fenêtre de GestionCuentas si aucun compte
    }
}
