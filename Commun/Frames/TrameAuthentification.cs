using System;
using System.Threading.Tasks;
using BotDofus.Commun.Messages.VersClient.Authentification;
using BotDofus.Commun.Messages.VersServeur.Authentification;
using BotDofus.Commun.Reseau;
using BotDofus.Divers;
using BotDofus.Divers.Enums;
using BotDofus.Utilitaires.Crypto;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Commun.Frames;

/// <summary>
/// Phase d'authentification : gère les échanges HC (challenge de connexion),
/// AA (envoi identifiants chiffrés), AlK/AlE (résultat), Ad/AV/AQ (métadonnées compte).
///
/// Côté bot, cette trame est responsable d'envoyer <see cref="MessageAuthentification"/>
/// dès réception du <see cref="MessageHelloConnexion"/> (qui contient la clé de chiffrement).
/// </summary>
public sealed class TrameAuthentification : TrameBase
{
    private readonly Compte _compte;
    private readonly SessionProxy _session;

    public TrameAuthentification(Repartiteur repartiteur, Compte compte, SessionProxy session)
        : base(repartiteur)
    {
        _compte = compte;
        _session = session;
    }

    protected override void EnregistrerGestionnaires()
    {
        Ecouter<MessageHelloConnexion>(OnHelloConnexion);
        Ecouter<MessageConnexionSucces>(OnConnexionSucces);
        Ecouter<MessageConnexionEchec>(OnConnexionEchec);
        Ecouter<MessagePseudo>(msg => _compte.PseudoAffiche = msg.Pseudo);
        Ecouter<MessageQueuePosition>(OnQueuePosition);
    }

    private async void OnHelloConnexion(MessageHelloConnexion msg)
    {
        _compte.ChangerEtat(EtatsCompte.Connexion);
        Journaliseur.Info($"Challenge HC reçu, clé = {msg.Cle.Substring(0, Math.Min(8, msg.Cle.Length))}...");

        var auth = new MessageAuthentification
        {
            Login = _compte.Identifiant,
            MotDePasseChiffre = ChiffrerMotDePasse(_compte.MotDePasse, msg.Cle),
            Version = "1.29.1"
        };

        try
        {
            await _session.EnvoyerAuServeurAsync(auth.Serialiser()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Journaliseur.Erreur("Échec de l'envoi de l'authentification", ex);
            _compte.ChangerEtat(EtatsCompte.Erreur);
        }
    }

    private void OnConnexionSucces(MessageConnexionSucces msg)
    {
        Journaliseur.Info($"Connexion acceptée (abonné : {msg.EstAbonne})");
        _compte.ChangerEtat(EtatsCompte.SelectionServeur);
    }

    private void OnConnexionEchec(MessageConnexionEchec msg)
    {
        var raison = msg.Code switch
        {
            'f' => "identifiants invalides",
            'b' => "compte banni",
            's' => "abonnement nécessaire",
            'd' => "connexion déjà active",
            'e' => "erreur de licence",
            _ => $"code {msg.Code}"
        };
        Journaliseur.Avertir($"Connexion refusée : {raison}");
        _compte.ChangerEtat(EtatsCompte.Erreur);
    }

    private void OnQueuePosition(MessageQueuePosition msg)
    {
        _compte.ChangerEtat(EtatsCompte.FileAttente);
        Journaliseur.Info($"File d'attente : position {msg.Position}/{msg.TotalAttente} — temps moyen {msg.TempsMoyen}s");
    }

    /// <summary>
    /// Chiffre le mot de passe avec la clé publique fournie via HC.
    /// Délégué à <see cref="ChiffrementDofus.ChiffrerMotDePasse"/>.
    /// </summary>
    private static string ChiffrerMotDePasse(string motDePasse, string cle)
        => ChiffrementDofus.ChiffrerMotDePasse(motDePasse, cle);
}
