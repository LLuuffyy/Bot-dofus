using System;
using System.Linq;
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
/// Phase entre login et jeu : réception de la liste des serveurs (AxK),
/// sélection du serveur préféré, réception du hôte chiffré (AYK) et ticket.
/// </summary>
public sealed class TrameSelectionServeur : TrameBase
{
    private readonly Compte _compte;
    private readonly SessionProxy _session;
    private readonly int _serveurPrefereSeulement;

    public TrameSelectionServeur(Repartiteur repartiteur, Compte compte, SessionProxy session, int serveurPrefere = 0)
        : base(repartiteur)
    {
        _compte = compte;
        _session = session;
        _serveurPrefereSeulement = serveurPrefere;
    }

    protected override void EnregistrerGestionnaires()
    {
        Ecouter<MessageListeServeurs>(OnListeServeurs);
        Ecouter<MessageHoteChiffre>(OnHoteChiffre);
    }

    private async void OnListeServeurs(MessageListeServeurs msg)
    {
        _compte.ChangerEtat(EtatsCompte.SelectionServeur);
        Journaliseur.Info($"{msg.Serveurs.Count} serveurs disponibles");

        // Choisit le serveur préféré, ou le premier en ligne (état 2).
        var cible = _serveurPrefereSeulement > 0
            ? msg.Serveurs.FirstOrDefault(s => s.Identifiant == _serveurPrefereSeulement)
            : msg.Serveurs.FirstOrDefault(s => s.EtatBrut == 2);

        if (cible.Identifiant == 0)
        {
            Journaliseur.Avertir("Aucun serveur sélectionnable");
            return;
        }

        Journaliseur.Info($"Sélection serveur #{cible.Identifiant}");
        try
        {
            await _session.EnvoyerAuServeurAsync(new MessageChoisirServeur
            {
                IdentifiantServeur = cible.Identifiant
            }.Serialiser()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Journaliseur.Erreur("Échec envoi choix serveur", ex);
        }
    }

    private void OnHoteChiffre(MessageHoteChiffre msg)
    {
        if (!string.IsNullOrWhiteSpace(msg.Hote) && msg.Port > 0)
        {
            Journaliseur.Info($"Hôte de jeu reçu : {msg.Hote}:{msg.Port}, ticket len={msg.Ticket.Length}");
            return;
        }

        // cryptedIp = 8 caractères, suivi parfois de cryptedPort = 3 caractères.
        // Format usuel : "<cryptedIp><cryptedPort>;<ticket>" — déjà splité dans MessageHoteChiffre.
        var ipBrute = msg.IpPortChiffre;
        string ip;
        int port;

        if (ipBrute.Length >= 11)
        {
            ip = ChiffrementDofus.DecoderIpChiffree(ipBrute[..8]);
            port = ChiffrementDofus.DecoderPortChiffre(ipBrute.Substring(8, 3));
        }
        else if (ipBrute.Length >= 8)
        {
            ip = ChiffrementDofus.DecoderIpChiffree(ipBrute[..8]);
            port = 0;
        }
        else
        {
            ip = "0.0.0.0";
            port = 0;
        }

        Journaliseur.Info($"Hôte de jeu déchiffré : {ip}:{port}, ticket len={msg.Ticket.Length}");
        // TODO : reconnecter le proxy sur ce serveur de jeu et relayer le ticket via MessageEnvoiTicket.
    }
}
