using System;
using BotDofus.Commun;
using BotDofus.Commun.Frames;
using BotDofus.Commun.Reseau;
using BotDofus.Divers.Jeu;
using BotDofus.Divers.Scripts;
using BotDofus.Divers.Scripts.Api;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers;

/// <summary>
/// Regroupe tous les sous-systèmes d'UN compte bot : proxy MITM, répartiteur,
/// gestionnaire de trames, état de jeu, API, gestionnaire de scripts.
///
/// Un <see cref="ContexteCompte"/> est créé lorsque l'utilisateur clique « Démarrer »
/// pour un compte donné et détruit à « Arrêter ».
/// </summary>
public sealed class ContexteCompte : IDisposable
{
    public Compte Compte { get; }
    public ProxyReseau Proxy { get; }
    public Repartiteur Repartiteur { get; }
    public GestionnaireTrames Trames { get; }
    public EtatJeu EtatJeu { get; }
    public ApiBot Api { get; }
    public GestionnaireScripts Scripts { get; }

    public SessionProxy? SessionActive { get; private set; }
    public EnregistreurPaquets? EnregistreurActif { get; private set; }

    public event EventHandler<SessionProxy>? SessionAttachee;

    public ContexteCompte(Compte compte, ConfigReseau configReseau)
    {
        Compte = compte;
        Proxy = new ProxyReseau(configReseau);
        Repartiteur = new Repartiteur();
        Trames = new GestionnaireTrames();
        EtatJeu = new EtatJeu();
        Api = new ApiBot(compte, EtatJeu);
        Scripts = new GestionnaireScripts(compte, Api);

        Proxy.PaquetRecu += (_, e) => Repartiteur.TraiterPaquet(e.Paquet);
        Proxy.SessionDemarree += OnSessionDemarree;
    }

    private void OnSessionDemarree(object? sender, SessionProxy session)
    {
        SessionActive = session;
        Api.LierSession(session);

        // Démarre sur l'état d'authentification
        Trames.RemplacerTrame(new TrameAuthentification(Repartiteur, Compte, session));

        SessionAttachee?.Invoke(this, session);
        Journaliseur.Info($"Contexte {Compte.Identifiant} : session proxy attachée");
    }

    public void DemarrerProxy() => Proxy.DemarrerAsync();
    public void ArreterProxy() => Proxy.Arreter();

    /// <summary>Active l'enregistrement des paquets vers un fichier plat (un par ligne).</summary>
    public void ActiverEnregistrement(string dossier = "logs")
    {
        if (EnregistreurActif != null) return;
        EnregistreurActif = new EnregistreurPaquets(dossier);
        EnregistreurActif.AttacherA(Proxy);
    }

    public void DesactiverEnregistrement()
    {
        if (EnregistreurActif == null) return;
        EnregistreurActif.Detacher(Proxy);
        EnregistreurActif.Dispose();
        EnregistreurActif = null;
    }

    public void Dispose()
    {
        try { DesactiverEnregistrement(); } catch { }
        try { Scripts.Dispose(); } catch { }
        try { Trames.Vider(); } catch { }
        try { Proxy.Dispose(); } catch { }
        try { Compte.Dispose(); } catch { }
    }
}
