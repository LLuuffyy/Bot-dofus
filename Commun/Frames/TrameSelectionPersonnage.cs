using System;
using System.Linq;
using System.Threading.Tasks;
using BotDofus.Commun.Messages.VersClient.Authentification;
using BotDofus.Commun.Messages.VersServeur.Authentification;
using BotDofus.Commun.Reseau;
using BotDofus.Divers;
using BotDofus.Divers.Enums;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Commun.Frames;

/// <summary>
/// Phase de sélection du personnage : après connexion au serveur de jeu,
/// le client reçoit ALK (liste) et choisit un personnage via AS.
/// </summary>
public sealed class TrameSelectionPersonnage : TrameBase
{
    private readonly Compte _compte;
    private readonly SessionProxy _session;
    private readonly int _personnagePrefere;

    public TrameSelectionPersonnage(Repartiteur repartiteur, Compte compte, SessionProxy session, int personnagePrefere = 0)
        : base(repartiteur)
    {
        _compte = compte;
        _session = session;
        _personnagePrefere = personnagePrefere;
    }

    protected override void EnregistrerGestionnaires()
    {
        Ecouter<MessageListePersonnages>(OnListePersonnages);
        Ecouter<MessageSelectionPersonnage>(OnSelectionPersonnage);
    }

    private async void OnListePersonnages(MessageListePersonnages msg)
    {
        _compte.ChangerEtat(EtatsCompte.SelectionPersonnage);
        Journaliseur.Info($"{msg.Personnages.Count} personnages disponibles (max {msg.NombreMax})");
        if (msg.Personnages.Count == 0)
        {
            Journaliseur.Avertir("Aucun personnage sur ce compte");
            return;
        }

        var cible = _personnagePrefere > 0
            ? msg.Personnages.FirstOrDefault(p => p.Identifiant == _personnagePrefere)
            : msg.Personnages.First();

        if (cible.Identifiant == 0) cible = msg.Personnages.First();

        Journaliseur.Info($"Sélection personnage « {cible.Nom} » niveau {cible.Niveau}");
        try
        {
            await _session.EnvoyerAuServeurAsync(new MessageChoisirPersonnage
            {
                IdentifiantPersonnage = cible.Identifiant
            }.Serialiser()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Journaliseur.Erreur("Échec envoi choix personnage", ex);
        }
    }

    private void OnSelectionPersonnage(MessageSelectionPersonnage msg)
    {
        _compte.PseudoAffiche = msg.Nom;
        _compte.ChangerEtat(EtatsCompte.EnJeu);
        Journaliseur.Info($"Personnage « {msg.Nom} » (classe #{msg.IdClasse}, niv {msg.Niveau}) sélectionné — entrée en jeu");
    }
}
