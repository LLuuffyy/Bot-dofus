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
/// <remarks>
/// LEGACY : cette trame n'est plus utilisée en production — TrameJeu fait tout
/// le travail. Conservée comme squelette pour migration future éventuelle.
/// Refactor Phase 7 : suppression du DecideurCombat legacy.
/// </remarks>
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
        Ecouter<MessageDebutCombat>(_ =>
        {
            // Compte rapide des ennemis pour avoir un libellé clair.
            int nbEnnemis = _combat.Ennemis.Count;
            Journaliseur.Info(nbEnnemis > 0
                ? $"[ACTION] Combat démarré ({nbEnnemis} ennemi(s))"
                : "[ACTION] Combat démarré");
            _compte.ChangerEtat(EtatsCompte.EnCombat);
        });
        Ecouter<MessageFinCombat>(_ =>
        {
            // Win/lose détaillé pas trivial à déduire sans parser GE/Im :
            // on log juste « Combat terminé », l'enchaînement « Recherche
            // d'un nouveau combat » ou « +XP » indique implicitement le résultat.
            Journaliseur.Info($"[ACTION] Combat terminé (tour {_combat.NumeroTour})");
            _combat.Reinitialiser();
        });
        Ecouter<MessageTourCombat>(OnTour);
        Ecouter<MessageActionJeu>(OnAction);
    }

    private async void OnPositions(MessagePositionsCombat msg)
    {
        if (msg.PositionsDisponibles.Count == 0) return;
        _combat.DefinirPositionsPlacement(msg.PositionsEquipe1, msg.PositionsEquipe2, msg.EquipeCourante);
        var caseChoisie = msg.PositionsDisponibles[0];
        Journaliseur.Info($"[ACTION] Placement combat sur cellule {caseChoisie}");
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
        // LEGACY : la vraie IA est dans TrameJeu.JouerTourCombatAsync.
        // Ce handler reste actif comme filet de sécurité (passe le tour).
        var estMonTour = msg.IdentifiantCombattant == _combat.IdentifiantAllie;
        if (!estMonTour) return;
        try
        {
            await _session.EnvoyerAuServeurAsync(new MessageJeuFinirTour().Serialiser()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Journaliseur.Erreur("Échec lors du tour de combat (legacy)", ex);
        }
    }

    private void OnAction(MessageActionJeu msg)
    {
        Journaliseur.Trace($"Action combat reçue : {msg.SousCode} | {msg.Parametres}");
    }
}
