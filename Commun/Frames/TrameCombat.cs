using System;
using System.Threading.Tasks;
using BotDofus.Commun.Messages.VersClient.Jeu;
using BotDofus.Commun.Messages.VersServeur.Jeu;
using BotDofus.Commun.Reseau;
using BotDofus.Divers;
using BotDofus.Divers.Enums;
using BotDofus.Divers.Combats;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Commun.Frames;

/// <summary>
/// Phase "en combat" : gère le placement initial, le tour des combattants,
/// les actions (déplacement/sort/passer), la fin de combat.
/// </summary>
public sealed class TrameCombat : TrameBase
{
    private readonly Compte _compte;
    private readonly SessionProxy _session;
    private readonly Combat _combat;

    public TrameCombat(Repartiteur repartiteur, Compte compte, SessionProxy session, Combat combat)
        : base(repartiteur)
    {
        _compte = compte;
        _session = session;
        _combat = combat;
    }

    protected override void EnregistrerGestionnaires()
    {
        Ecouter<MessagePositionsCombat>(OnPositions);
        Ecouter<MessageDebutCombat>(_ => { Journaliseur.Info("Début du combat"); _compte.ChangerEtat(EtatsCompte.EnCombat); });
        Ecouter<MessageFinCombat>(_ => { Journaliseur.Info("Fin du combat"); _combat.Reinitialiser(); });
        Ecouter<MessageTourCombat>(OnTour);
        Ecouter<MessageActionJeu>(OnAction);
    }

    private async void OnPositions(MessagePositionsCombat msg)
    {
        if (msg.PositionsDisponibles.Count == 0) return;
        var caseChoisie = msg.PositionsDisponibles[0];
        Journaliseur.Info($"Placement sur la case {caseChoisie}");
        try
        {
            await _session.EnvoyerAuServeurAsync(new MessageJeuPosition { CaseDepart = caseChoisie }.Serialiser()).ConfigureAwait(false);
            await _session.EnvoyerAuServeurAsync(new MessageJeuPret { Pret = true }.Serialiser()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Journaliseur.Erreur("Échec de placement", ex);
        }
    }

    private async void OnTour(MessageTourCombat msg)
    {
        var estMonTour = msg.IdentifiantCombattant == _combat.IdentifiantAllie;
        if (!estMonTour) return;

        Journaliseur.Info("Mon tour");
        // TODO : laisser le moteur IA / script décider de l'action.
        //        Par défaut on passe le tour pour ne rien casser.
        try
        {
            await _session.EnvoyerAuServeurAsync(new MessageJeuFinirTour().Serialiser()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Journaliseur.Erreur("Échec de fin de tour", ex);
        }
    }

    private void OnAction(MessageActionJeu msg)
    {
        Journaliseur.Trace($"Action combat reçue : {msg.SousCode} | {msg.Parametres}");
    }
}
