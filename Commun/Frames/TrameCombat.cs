using System;
using System.Threading.Tasks;
using BotDofus.Commun.Messages.VersClient.Jeu;
using BotDofus.Commun.Messages.VersServeur.Jeu;
using BotDofus.Commun.Reseau;
using BotDofus.Divers;
using BotDofus.Divers.Enums;
using BotDofus.Divers.Combats;
using BotDofus.Divers.Combats.IA;
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
    private readonly DecideurCombat _decideur;

    public TrameCombat(Repartiteur repartiteur, Compte compte, SessionProxy session, Combat combat, DecideurCombat? decideur = null)
        : base(repartiteur)
    {
        _compte = compte;
        _session = session;
        _combat = combat;
        _decideur = decideur ?? new DecideurCombat(StrategieCombat.Tactique, System.Array.Empty<RegleSort>());
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

        Journaliseur.Info("Mon tour — interrogation du décideur IA");

        try
        {
            var action = _decideur.Decider(_combat);
            await ExecuterActionAsync(action).ConfigureAwait(false);
            await _session.EnvoyerAuServeurAsync(new MessageJeuFinirTour().Serialiser()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Journaliseur.Erreur("Échec lors du tour de combat", ex);
        }
    }

    private async System.Threading.Tasks.Task ExecuterActionAsync(ActionCombat action)
    {
        switch (action)
        {
            case ActionCombat.PasserTour:
                Journaliseur.Debogue("Décideur : passer le tour");
                break;

            case ActionCombat.SeDeplacer dep:
                Journaliseur.Debogue($"Décideur : se déplacer vers {dep.CelluleCible}");
                // TODO : émettre un GA[sous-code mouvement] avec la clef de chemin sérialisée
                //        une fois le pathfinding sur la carte de combat finalisé.
                _ = dep;
                break;

            case ActionCombat.LancerSort sort:
                Journaliseur.Debogue($"Décideur : sort {sort.IdSort} sur cellule {sort.CelluleCible}");
                var msgSort = new MessageJeuAction
                {
                    SousCode = "300", // GA300xxx = lancer sort (sous-code à confirmer sur capture réelle)
                    Parametres = $"{sort.IdSort};{sort.CelluleCible}"
                };
                await _session.EnvoyerAuServeurAsync(msgSort.Serialiser()).ConfigureAwait(false);
                break;

            case ActionCombat.UtiliserObjet obj:
                Journaliseur.Debogue($"Décideur : utiliser objet {obj.IdObjet}");
                // TODO : émettre un OU (Object Use) avec l'identifiant de l'objet.
                _ = obj;
                break;
        }
    }

    private void OnAction(MessageActionJeu msg)
    {
        Journaliseur.Trace($"Action combat reçue : {msg.SousCode} | {msg.Parametres}");
    }
}
