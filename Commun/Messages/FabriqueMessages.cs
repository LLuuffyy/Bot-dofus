using System;
using System.Collections.Generic;
using BotDofus.Commun.Reseau;

namespace BotDofus.Commun.Messages;

/// <summary>
/// Registre et factory : associe un préfixe de message (ex. "HG", "AlK") à
/// un constructeur de classe typée. Si aucun constructeur n'est enregistré
/// pour un préfixe donné, un <see cref="MessageInconnu"/> est retourné.
///
/// Le registre est peuplé par <see cref="EnregistrerMessagesStandards"/> au
/// démarrage ; les consommateurs peuvent aussi enregistrer des variantes.
/// </summary>
public static class FabriqueMessages
{
    private static readonly Dictionary<string, Func<MessageDofus>> _registreVersClient = new();
    private static readonly Dictionary<string, Func<MessageDofus>> _registreVersServeur = new();
    private static bool _standardsEnregistres;

    /// <summary>Enregistre un constructeur pour un préfixe donné (sens VersClient).</summary>
    public static void EnregistrerVersClient<T>(string prefixe) where T : MessageDofus, IMessageVersClient, new()
    {
        _registreVersClient[prefixe] = () => new T();
    }

    /// <summary>Enregistre un constructeur pour un préfixe donné (sens VersServeur).</summary>
    public static void EnregistrerVersServeur<T>(string prefixe) where T : MessageDofus, IMessageVersServeur, new()
    {
        _registreVersServeur[prefixe] = () => new T();
    }

    /// <summary>
    /// Construit le message typé à partir d'un paquet brut. Recherche d'abord
    /// un préfixe 3 caractères (ex. "AlK") puis se replie sur 2 caractères ("Al"),
    /// puis enfin sur un <see cref="MessageInconnu"/>.
    /// </summary>
    public static MessageDofus FabriquerDepuis(PaquetBrut paquet)
    {
        if (!_standardsEnregistres)
        {
            EnregistrerMessagesStandards();
        }

        var registre = paquet.Direction == DirectionPaquet.VersClient
            ? _registreVersClient
            : _registreVersServeur;

        // Essai préfixe 3 caractères (ex. "AlK", "AYK", "ALK")
        if (paquet.Contenu.Length >= 3)
        {
            var prefixe3 = paquet.Contenu[..3];
            if (registre.TryGetValue(prefixe3, out var ctor3))
            {
                var msg = ctor3();
                msg.Source = paquet;
                msg.Desserialiser(StripSeparateurInitial(paquet.Contenu[3..]));
                return msg;
            }
        }

        // Essai préfixe 2 caractères
        if (paquet.Contenu.Length >= 2)
        {
            var prefixe2 = paquet.Contenu[..2];
            if (registre.TryGetValue(prefixe2, out var ctor2))
            {
                var msg = ctor2();
                msg.Source = paquet;
                msg.Desserialiser(StripSeparateurInitial(paquet.Contenu[2..]));
                return msg;
            }
        }

        // Repli : message inconnu, mais on préserve le contenu brut
        var inconnu = new MessageInconnu(paquet.Prefixe, paquet.Direction)
        {
            Source = paquet
        };
        inconnu.Desserialiser(paquet.Charge);
        return inconnu;
    }

    /// <summary>
    /// Strip le séparateur '|' initial s'il est présent juste après le préfixe.
    /// Sans ça, les Desserialiser qui font Split('|') récupèrent un parts[0]="" et tout est
    /// décalé d'un cran (bug observé sur ASK, AYK, AxK, etc.).
    /// </summary>
    private static string StripSeparateurInitial(string charge)
        => charge.Length > 0 && charge[0] == '|' ? charge[1..] : charge;

    /// <summary>
    /// Enregistre tous les types de messages standards du protocole Dofus Retro 1.29.
    /// Les classes concrètes se trouvent dans <c>Commun/Messages/VersClient</c>
    /// et <c>Commun/Messages/VersServeur</c>.
    /// </summary>
    public static void EnregistrerMessagesStandards()
    {
        if (_standardsEnregistres) return;
        _standardsEnregistres = true;

        // --- VersClient : Authentification ---
        EnregistrerVersClient<VersClient.Authentification.MessageHelloConnexion>("HC");
        EnregistrerVersClient<VersClient.Authentification.MessageHelloJeu>("HG");
        EnregistrerVersClient<VersClient.Authentification.MessageConnexionSucces>("AlK");
        EnregistrerVersClient<VersClient.Authentification.MessageConnexionEchec>("AlE");
        EnregistrerVersClient<VersClient.Authentification.MessagePseudo>("Ad");
        EnregistrerVersClient<VersClient.Authentification.MessageCommunaute>("AV");
        EnregistrerVersClient<VersClient.Authentification.MessageQuestion>("AQ");
        EnregistrerVersClient<VersClient.Authentification.MessageListeServeurs>("AxK");
        EnregistrerVersClient<VersClient.Authentification.MessageServeursDisponibles>("AH");
        EnregistrerVersClient<VersClient.Authentification.MessageHoteChiffre>("AYK");
        EnregistrerVersClient<VersClient.Authentification.MessageTicket>("ATK");
        EnregistrerVersClient<VersClient.Authentification.MessageListePersonnages>("ALK");
        EnregistrerVersClient<VersClient.Authentification.MessageSelectionPersonnage>("ASK");
        EnregistrerVersClient<VersClient.Authentification.MessageRestrictions>("AR");
        EnregistrerVersClient<VersClient.Authentification.MessageStats>("As");
        EnregistrerVersClient<VersClient.Authentification.MessageListeSorts>("SL");
        EnregistrerVersClient<VersClient.Authentification.MessageQueuePosition>("Af");
        EnregistrerVersClient<VersClient.Authentification.MessageNouveauNiveau>("ANK");

        // --- VersClient : Base ---
        EnregistrerVersClient<VersClient.Base.MessageConfirmation>("BC");
        EnregistrerVersClient<VersClient.Base.MessagePingMoyen>("BP");
        EnregistrerVersClient<VersClient.Base.MessageDate>("BD");
        EnregistrerVersClient<VersClient.Base.MessageTempsReference>("BT");

        // --- VersClient : Jeu ---
        EnregistrerVersClient<VersClient.Jeu.MessageDonneesCarte>("GDM");
        EnregistrerVersClient<VersClient.Jeu.MessageDonneesCarteFin>("GDF");
        EnregistrerVersClient<VersClient.Jeu.MessageDonneesCarteKeyframe>("GDK");
        EnregistrerVersClient<VersClient.Jeu.MessageMetiersSkills>("JSK");
        EnregistrerVersClient<VersClient.Jeu.MessageMetiersXp>("JXK");
        EnregistrerVersClient<VersClient.Jeu.MessageRejoindreJeu>("GJ");
        EnregistrerVersClient<VersClient.Jeu.MessagePositionsCombat>("GP");
        EnregistrerVersClient<VersClient.Jeu.MessageDebutCombat>("GS");
        EnregistrerVersClient<VersClient.Jeu.MessageFinCombat>("GE");
        EnregistrerVersClient<VersClient.Jeu.MessageTourCombat>("GT");
        EnregistrerVersClient<VersClient.Jeu.MessageActionJeu>("GA");
        EnregistrerVersClient<VersClient.Jeu.MessagePret>("GR");
        EnregistrerVersClient<VersClient.Jeu.MessageCreationJeu>("GC");
        EnregistrerVersClient<VersClient.Jeu.MessageMouvementCarte>("GM");
        // Abrak v1.48 : acteurs map via la famille N* (identité only, position chiffrée).
        EnregistrerVersClient<VersClient.Jeu.MessageActeurAbrak>("NL");
        EnregistrerVersClient<VersClient.Jeu.MessageActeurAbrakRetrait>("Nx");
        // Abrak combat EN CLAIR : GTM = combattants+cellules, GTS = à qui le tour.
        // 3 chars → prioritaires sur le "GT" générique.
        EnregistrerVersClient<VersClient.Jeu.MessageCombattantsAbrak>("GTM");
        EnregistrerVersClient<VersClient.Jeu.MessageTourCombatAbrak>("GTS");
        EnregistrerVersClient<VersClient.Jeu.MessageNombreCombats>("fC");
        // Party (mode héros Abrak) — PCK (3) prioritaire avant PI/PM/PL (2).
        EnregistrerVersClient<VersClient.Jeu.MessagePartyCheck>("PCK");
        EnregistrerVersClient<VersClient.Jeu.MessagePartyMembres>("PM");
        EnregistrerVersClient<VersClient.Jeu.MessagePartyLeader>("PL");
        EnregistrerVersClient<VersClient.Jeu.MessagePartyInvitation>("PI");
        // Nh (S→C) : sorts d'un héros lié (réponse à Nh<id>/Ns<id> C→S).
        EnregistrerVersClient<VersClient.Jeu.MessageHerosSorts>("Nh");

        // --- VersClient : Info ---
        EnregistrerVersClient<VersClient.Info.MessageInfoMessage>("Im");
        EnregistrerVersClient<VersClient.Info.MessageInfoCarte>("IO");
        EnregistrerVersClient<VersClient.Info.MessageInfoVie>("IL");

        // --- VersClient : Chat ---
        EnregistrerVersClient<VersClient.Chat.MessageChatMessage>("cMK");
        EnregistrerVersClient<VersClient.Chat.MessageChatServeur>("cMS");

        // --- VersClient : Objet ---
        EnregistrerVersClient<VersClient.Objet.MessageObjetAjout>("OAK");
        EnregistrerVersClient<VersClient.Objet.MessageObjetRetrait>("OR");
        EnregistrerVersClient<VersClient.Objet.MessageObjetQuantite>("OQ");
        EnregistrerVersClient<VersClient.Objet.MessageObjetPoids>("Ow");
        EnregistrerVersClient<VersClient.Objet.MessageObjetDeplacement>("OM");
        EnregistrerVersClient<VersClient.Objet.MessageEchangeFin>("EV");

        // --- VersServeur : Authentification ---
        EnregistrerVersServeur<VersServeur.Authentification.MessageAuthentification>("AA");
        EnregistrerVersServeur<VersServeur.Authentification.MessageDemandeServeurs>("AX");
        EnregistrerVersServeur<VersServeur.Authentification.MessageChoisirServeur>("Ax");
        EnregistrerVersServeur<VersServeur.Authentification.MessageObtenirPersonnages>("AL");
        EnregistrerVersServeur<VersServeur.Authentification.MessageChoisirPersonnage>("AS");
        EnregistrerVersServeur<VersServeur.Authentification.MessageEnvoiTicket>("AT");
        EnregistrerVersServeur<VersServeur.Authentification.MessageIdentite>("Ai");

        // --- VersServeur : Jeu ---
        EnregistrerVersServeur<VersServeur.Jeu.MessageJeuPret>("GR");
        EnregistrerVersServeur<VersServeur.Jeu.MessageJeuAction>("GA");
        EnregistrerVersServeur<VersServeur.Jeu.MessageJeuCreer>("GC");
        EnregistrerVersServeur<VersServeur.Jeu.MessageJeuFinirTour>("GE");
        EnregistrerVersServeur<VersServeur.Jeu.MessageJeuPosition>("GP");
        EnregistrerVersServeur<VersServeur.Jeu.MessageJeuQuitter>("GQ");
        EnregistrerVersServeur<VersServeur.Jeu.MessageFinMouvement>("GKK");
        EnregistrerVersServeur<VersServeur.Jeu.MessageDemandeInfosJeu>("GI");
        EnregistrerVersServeur<VersServeur.Jeu.MessageDemandeDate>("BD");
        EnregistrerVersServeur<VersServeur.Jeu.MessageCanalChat>("cC");

        // --- VersServeur : Chat ---
        EnregistrerVersServeur<VersServeur.Chat.MessageChatEnvoyer>("BM");
        EnregistrerVersServeur<VersServeur.Chat.MessageSmileyEnvoyer>("BS");

        // --- VersServeur : Dialogue ---
        EnregistrerVersServeur<VersServeur.Dialogue.MessageDialogueDebuter>("DC");
        EnregistrerVersServeur<VersServeur.Dialogue.MessageDialogueReponse>("DR");
        EnregistrerVersServeur<VersServeur.Dialogue.MessageDialogueQuitter>("DV");
    }
}
