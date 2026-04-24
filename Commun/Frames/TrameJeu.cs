using BotDofus.Commun.Messages.VersClient.Base;
using BotDofus.Commun.Messages.VersClient.Chat;
using BotDofus.Commun.Messages.VersClient.Info;
using BotDofus.Commun.Messages.VersClient.Jeu;
using BotDofus.Commun.Messages.VersClient.Objet;
using BotDofus.Commun.Reseau;
using BotDofus.Divers;
using BotDofus.Divers.Enums;
using BotDofus.Divers.Jeu;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Commun.Frames;

/// <summary>
/// Phase "en jeu" principale : hors combat, hors dialogue. Écoute les changements
/// de carte, les messages d'information, les mises à jour d'inventaire, les messages
/// de chat, et maintient le modèle <see cref="EtatJeu"/> à jour.
///
/// Cette trame peut être recouverte par <see cref="TrameCombat"/> ou
/// <see cref="TrameDialogue"/> de manière transitoire (empilement).
/// </summary>
public sealed class TrameJeu : TrameBase
{
    private readonly Compte _compte;
    private readonly EtatJeu _etat;
    private readonly SessionProxy _session;

    public TrameJeu(Repartiteur repartiteur, Compte compte, EtatJeu etat, SessionProxy session)
        : base(repartiteur)
    {
        _compte = compte;
        _etat = etat;
        _session = session;
    }

    protected override void EnregistrerGestionnaires()
    {
        Ecouter<MessageDonneesCarte>(OnDonneesCarte);
        Ecouter<MessageInfoMessage>(OnInfoMessage);
        Ecouter<MessageInfoVie>(OnInfoVie);
        Ecouter<MessageObjetAjout>(_ => Journaliseur.Debogue("Objet ajouté à l'inventaire"));
        Ecouter<MessageObjetRetrait>(msg => Journaliseur.Debogue($"Objet {msg.IdentifiantObjet} retiré"));
        Ecouter<MessageObjetQuantite>(msg => Journaliseur.Debogue($"Objet {msg.IdentifiantObjet} → quantité {msg.NouvelleQuantite}"));
        Ecouter<MessageObjetPoids>(msg => _etat.Personnage.ActualiserPoids(msg.PoidsActuel, msg.PoidsMax));
        Ecouter<MessageChatMessage>(OnChatMessage);
        Ecouter<MessageChatServeur>(msg => Journaliseur.Info($"[SERVEUR] {msg.Texte}"));
        Ecouter<MessageTourCombat>(_ => _compte.ChangerEtat(EtatsCompte.EnCombat));
        Ecouter<MessagePingMoyen>(_ => { /* silence ping */ });
    }

    private void OnDonneesCarte(MessageDonneesCarte msg)
    {
        Journaliseur.Info($"Changement de carte : #{msg.IdentifiantCarte}");
        _etat.ChangerCarte(msg.IdentifiantCarte, msg.Clef);
    }

    private void OnInfoMessage(MessageInfoMessage msg)
    {
        Journaliseur.Info($"Info #{msg.Code} : {msg.Arguments}");
    }

    private void OnInfoVie(MessageInfoVie msg)
    {
        _etat.Personnage.ActualiserVie(msg.Vie, msg.VieMax);
    }

    private void OnChatMessage(MessageChatMessage msg)
    {
        Journaliseur.Info($"[{msg.Canal}] {msg.PseudoEmetteur} : {msg.Texte}");
    }
}
