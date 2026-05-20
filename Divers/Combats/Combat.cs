using System;
using System.Collections.Generic;
using BotDofus.Divers.Combats.Combattants;
using BotDofus.Divers.Combats.Enums;

namespace BotDofus.Divers.Combats;

/// <summary>
/// État d'un combat en cours. Regroupe les combattants alliés/ennemis,
/// le tour courant, le timer, et les statistiques.
/// </summary>
public sealed class Combat
{
    public EtatCombat Etat { get; private set; } = EtatCombat.Inactif;
    public int NumeroTour { get; private set; }
    public int IdentifiantAllie { get; set; }
    public int IdentifiantCombattantActuel { get; private set; }

    public List<Combattant> Allies { get; } = new();
    public List<Combattant> Ennemis { get; } = new();
    public List<int> PositionsEquipe1 { get; } = new();
    public List<int> PositionsEquipe2 { get; } = new();
    public int EquipePlacement { get; private set; } = -1;

    /// <summary>
    /// Compteur de lancements par sort pour le TOUR courant. Reset à chaque
    /// <see cref="NouveauTour"/>. Utilisé par <see cref="IA.MoteurReglesCombat"/>
    /// pour appliquer <see cref="IA.RegleSort.NombreParTour"/> (limite SynFus :
    /// « lancer ce sort au max N fois par tour »).
    /// </summary>
    public Dictionary<int, int> CompteursRegleParTour { get; } = new();

    public event EventHandler<EtatCombat>? EtatChange;
    public event EventHandler<int>? TourChange;
    public event EventHandler? PositionsChangees;

    public void Demarrer()
    {
        ChangerEtat(EtatCombat.Placement);
    }

    public void PassageEnCombat()
    {
        ChangerEtat(EtatCombat.EnCours);
    }

    public void NouveauTour(int identifiantCombattant)
    {
        NumeroTour++;
        IdentifiantCombattantActuel = identifiantCombattant;
        CompteursRegleParTour.Clear();
        TourChange?.Invoke(this, identifiantCombattant);
    }

    public void DefinirPositionsPlacement(IEnumerable<int> equipe1, IEnumerable<int> equipe2, int equipeCourante)
    {
        PositionsEquipe1.Clear();
        PositionsEquipe1.AddRange(equipe1);
        PositionsEquipe2.Clear();
        PositionsEquipe2.AddRange(equipe2);
        EquipePlacement = equipeCourante;
        ChangerEtat(EtatCombat.Placement);
        PositionsChangees?.Invoke(this, EventArgs.Empty);
    }

    public void Reinitialiser()
    {
        Allies.Clear();
        Ennemis.Clear();
        PositionsEquipe1.Clear();
        PositionsEquipe2.Clear();
        EquipePlacement = -1;
        NumeroTour = 0;
        IdentifiantCombattantActuel = 0;
        CompteursRegleParTour.Clear();
        ChangerEtat(EtatCombat.Inactif);
        PositionsChangees?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// À appeler après mise à jour des combattants (parseur GTM Abrak) pour
    /// rafraîchir les vues (onglet Combat, grille Carte) sans changement d'état.
    /// </summary>
    public void SignalerCombattantsMaj() => PositionsChangees?.Invoke(this, EventArgs.Empty);

    public bool EstMonTour => IdentifiantCombattantActuel == IdentifiantAllie
                            && Etat == EtatCombat.EnCours;

    private void ChangerEtat(EtatCombat nouveau)
    {
        if (Etat == nouveau) return;
        Etat = nouveau;
        EtatChange?.Invoke(this, nouveau);
    }
}
