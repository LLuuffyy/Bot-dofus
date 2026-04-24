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

    public event EventHandler<EtatCombat>? EtatChange;
    public event EventHandler<int>? TourChange;

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
        TourChange?.Invoke(this, identifiantCombattant);
    }

    public void Reinitialiser()
    {
        Allies.Clear();
        Ennemis.Clear();
        NumeroTour = 0;
        IdentifiantCombattantActuel = 0;
        ChangerEtat(EtatCombat.Inactif);
    }

    public bool EstMonTour => IdentifiantCombattantActuel == IdentifiantAllie
                            && Etat == EtatCombat.EnCours;

    private void ChangerEtat(EtatCombat nouveau)
    {
        if (Etat == nouveau) return;
        Etat = nouveau;
        EtatChange?.Invoke(this, nouveau);
    }
}
