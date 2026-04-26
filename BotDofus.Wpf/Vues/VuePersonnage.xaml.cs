using System.Windows.Controls;
using BotDofus.Divers;

namespace BotDofus.Wpf.Vues;

public partial class VuePersonnage : UserControl
{
    private ContexteCompte? _contexte;

    public VuePersonnage()
    {
        InitializeComponent();
    }

    public void Lier(ContexteCompte ctx)
    {
        _contexte = ctx;
        ctx.PaquetRecu += (_, __) => Dispatcher.Invoke(Rafraichir);
        Rafraichir();
    }

    private void Rafraichir()
    {
        if (_contexte == null) return;
        var p = _contexte.EtatJeu.Personnage;

        TxtNom.Text = string.IsNullOrEmpty(p.Nom) ? "—" : p.Nom;
        TxtClasse.Text = $"Classe #{p.IdClasse} · Sexe {(p.Sexe == 1 ? "F" : "H")}";
        TxtNiveau.Text = p.Niveau > 0 ? $"Niveau {p.Niveau}" : "Niveau —";
        TxtKamas.Text = p.Kamas.ToString("N0");

        TxtPv.Text = $"{p.Vie} / {p.VieMax}";
        TxtEnergie.Text = $"{p.Energie} / {p.EnergieMax}";
        TxtPa.Text = p.PA.ToString();
        TxtPm.Text = p.PM.ToString();
        TxtPoids.Text = $"{p.PoidsActuel} / {p.PoidsMax}";
        TxtXp.Text = $"{p.PourcentageXp:F1} %";
        TxtPointsCaracs.Text = p.PointsCaracteristiques.ToString();
        TxtPointsSorts.Text = p.PointsSorts.ToString();

        TxtPosition.Text = p.CarteCourante.HasValue
            ? $"Carte {p.CarteCourante} · Cellule {p.CellulePosition?.ToString() ?? "—"}"
            : "Carte —";
    }
}
