using System.Windows.Controls;
using BotDofus.Divers;

namespace BotDofus.Wpf.Vues;

public partial class VueDashboard : UserControl
{
    private ContexteCompte? _contexte;

    public VueDashboard()
    {
        InitializeComponent();
    }

    public void Lier(ContexteCompte contexte)
    {
        _contexte = contexte;
        contexte.PaquetRecu += (_, __) => Dispatcher.Invoke(Rafraichir);
        Rafraichir();
    }

    private void Rafraichir()
    {
        if (_contexte == null) return;
        var p = _contexte.EtatJeu.Personnage;
        TxtCarte.Text = p.CarteCourante?.ToString() ?? "—";
        TxtPosition.Text = p.CellulePosition?.ToString() ?? "—";
        TxtKamas.Text = p.Kamas.ToString("N0");

        TxtStatut.Text = _contexte.SessionJeuActive != null
            ? "✅ Connecté (session jeu)"
            : (_contexte.SessionAuthActive != null ? "⏳ Auth en cours" : "❌ Déconnecté");

        var combat = _contexte.EtatJeu.Combat;
        TxtCombatEtat.Text = combat.Etat == BotDofus.Divers.Combats.Enums.EtatCombat.Inactif
            ? "Hors combat"
            : $"En combat ({combat.Etat})";
        TxtCombatTour.Text = combat.NumeroTour > 0 ? $"Tour {combat.NumeroTour}" : "";
    }
}
