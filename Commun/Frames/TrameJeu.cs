using BotDofus.Commun.Messages.VersClient.Authentification;
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
        Ecouter<MessageSelectionPersonnage>(OnSelectionPersonnage);
        Ecouter<MessageStats>(OnStats);
        Ecouter<MessageObjetAjout>(OnObjetAjout);
        Ecouter<MessageObjetRetrait>(OnObjetRetrait);
        Ecouter<MessageObjetQuantite>(OnObjetQuantite);
        Ecouter<MessageObjetPoids>(msg => _etat.Personnage.ActualiserPoids(msg.PoidsActuel, msg.PoidsMax));
        Ecouter<MessageChatMessage>(OnChatMessage);
        Ecouter<MessageChatServeur>(msg => Journaliseur.Info($"[SERVEUR] {msg.Texte}"));

        // Mises à jour de l'état combat (GS, GE, GT) — alimente Combat.Etat / NumeroTour pour la vue live.
        Ecouter<BotDofus.Commun.Messages.VersClient.Jeu.MessageDebutCombat>(_ =>
        {
            _etat.Combat.Demarrer();
            _etat.Combat.PassageEnCombat();
            _compte.ChangerEtat(EtatsCompte.EnCombat);
        });
        Ecouter<BotDofus.Commun.Messages.VersClient.Jeu.MessageFinCombat>(_ =>
        {
            _etat.Combat.Reinitialiser();
            _compte.ChangerEtat(EtatsCompte.EnJeu);
            Journaliseur.Info("[COMBAT] Combat terminé");
        });
        Ecouter<MessageTourCombat>(msg =>
        {
            _etat.Combat.NouveauTour(msg.IdentifiantCombattant);
            _compte.ChangerEtat(EtatsCompte.EnCombat);
        });

        Ecouter<MessagePingMoyen>(_ => { /* silence ping */ });
    }

    private void OnSelectionPersonnage(MessageSelectionPersonnage msg)
    {
        // ASK : sert à la fois à confirmer la sélection ET à pousser l'identité du perso (nom/niveau/classe).
        var perso = _etat.Personnage;
        perso.Identifiant = msg.Identifiant;
        perso.Nom = msg.Nom;
        perso.Niveau = msg.Niveau;
        perso.IdClasse = msg.IdClasse;
        _compte.PseudoAffiche = msg.Nom;
        Journaliseur.Info($"Personnage : {msg.Nom} (classe #{msg.IdClasse}, niv {msg.Niveau})");
    }

    private void OnStats(MessageStats msg)
    {
        // As : statistiques complètes du perso (PV / Énergie / PA / PM / Kamas / XP / pts caracs / pts sorts).
        var perso = _etat.Personnage;
        perso.XpActuelle = msg.XpActuelle;
        perso.XpPalierCourant = msg.XpPalier;
        perso.XpPalierSuivant = msg.XpProchainPalier;
        perso.Kamas = msg.Kamas;
        perso.PointsCaracteristiques = msg.PointsCaracteristiques;
        perso.PointsSorts = msg.PointsSorts;
        perso.PA = msg.PA;
        perso.PM = msg.PM;
        perso.ActualiserVie(msg.Vie, msg.VieMax);
        perso.ActualiserEnergie(msg.Energie, msg.EnergieMax);
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

    private void OnObjetAjout(MessageObjetAjout msg)
    {
        var inv = _etat.Personnage.Inventaire;
        foreach (var o in msg.ObjetsParse)
        {
            // Évite les doublons : si l'id existe déjà, on remplace quantité/position.
            var existant = inv.FirstOrDefault(x => x.Identifiant == o.Identifiant);
            if (existant != null)
            {
                existant.Quantite = o.Quantite;
                existant.Position = o.Position;
            }
            else
            {
                inv.Add(new BotDofus.Divers.Jeu.Personnage.ObjetInventaire
                {
                    Identifiant = o.Identifiant,
                    IdTemplate = o.IdTemplate,
                    Quantite = o.Quantite,
                    Position = o.Position
                });
            }
        }
        Journaliseur.Debogue($"[INV] +{msg.ObjetsParse.Count} objets (total = {inv.Count})");
    }

    private void OnObjetRetrait(MessageObjetRetrait msg)
    {
        var inv = _etat.Personnage.Inventaire;
        var n = inv.RemoveAll(x => x.Identifiant == msg.IdentifiantObjet);
        if (n > 0) Journaliseur.Debogue($"[INV] -1 objet (id {msg.IdentifiantObjet}, total = {inv.Count})");
    }

    private void OnObjetQuantite(MessageObjetQuantite msg)
    {
        var existant = _etat.Personnage.Inventaire.FirstOrDefault(x => x.Identifiant == msg.IdentifiantObjet);
        if (existant != null)
        {
            existant.Quantite = msg.NouvelleQuantite;
            Journaliseur.Debogue($"[INV] objet {msg.IdentifiantObjet} → qty {msg.NouvelleQuantite}");
        }
    }

    private void OnInfoVie(MessageInfoVie msg)
    {
        // Sécurité : on n'écrase pas la vie réelle (déjà fournie par le paquet As) avec 0/0
        // si jamais le paquet IL arrive sous une variante non-life-info (ex: "ILS2000").
        if (msg.VieMax <= 0) return;
        _etat.Personnage.ActualiserVie(msg.Vie, msg.VieMax);
    }

    private void OnChatMessage(MessageChatMessage msg)
    {
        Journaliseur.Info($"[{msg.Canal}] {msg.PseudoEmetteur} : {msg.Texte}");
    }
}
