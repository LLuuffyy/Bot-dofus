using System;
using System.Collections.Generic;
using BotDofus.Commun.Messages;

namespace BotDofus.Commun.Frames;

/// <summary>
/// Base abstraite pour une "Frame" (trame) d'un état du jeu : authentification,
/// sélection de serveur, en jeu, en combat, en dialogue.
///
/// Une Frame s'abonne au <see cref="Repartiteur"/> au moment de son activation
/// (<see cref="Activer"/>) et se désabonne à sa désactivation (<see cref="Desactiver"/>),
/// garantissant qu'un même message n'est pas traité par deux handlers concurrents
/// d'états différents.
/// </summary>
public abstract class TrameBase
{
    private readonly List<Abonnement> _abonnements = new();
    protected Repartiteur Repartiteur { get; }
    public bool EstActive { get; private set; }

    protected TrameBase(Repartiteur repartiteur)
    {
        Repartiteur = repartiteur;
    }

    /// <summary>Activation : la Frame peut enregistrer ses gestionnaires via <see cref="Ecouter"/>.</summary>
    public void Activer()
    {
        if (EstActive) return;
        EstActive = true;
        EnregistrerGestionnaires();
    }

    /// <summary>Désactivation : tous les abonnements de cette Frame sont retirés.</summary>
    public void Desactiver()
    {
        if (!EstActive) return;
        EstActive = false;
        foreach (var a in _abonnements) a.Annuler();
        _abonnements.Clear();
        DemantelerGestionnaires();
    }

    /// <summary>Hook appelé après activation pour abonner les gestionnaires via <see cref="Ecouter"/>.</summary>
    protected abstract void EnregistrerGestionnaires();

    /// <summary>Hook appelé avant désactivation pour libérer tout état non abonné.</summary>
    protected virtual void DemantelerGestionnaires() { }

    /// <summary>Abonne un gestionnaire dont la durée de vie suit la Frame.</summary>
    protected void Ecouter<T>(Action<T> gestionnaire) where T : MessageDofus
    {
        Repartiteur.Abonner(gestionnaire);
        _abonnements.Add(new Abonnement(() => Repartiteur.Desabonner(gestionnaire)));
    }

    private sealed record Abonnement(Action Annuler);
}
