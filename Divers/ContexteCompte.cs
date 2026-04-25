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

    /// <summary>
    /// En mode passif, le proxy se contente de relayer/journaliser les octets
    /// SANS activer la moindre trame d'automatisation. Idéal pour capturer
    /// du trafic réel sans risquer d'envoyer des paquets invalides au serveur.
    /// </summary>
    public bool ModePassif { get; set; }

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

        if (ModePassif)
        {
            Journaliseur.Info($"Contexte {Compte.Identifiant} : session attachée en MODE PASSIF (relais pur, aucune trame active)");
        }
        else
        {
            // Démarre sur l'état d'authentification
            Trames.RemplacerTrame(new TrameAuthentification(Repartiteur, Compte, session));
            Journaliseur.Info($"Contexte {Compte.Identifiant} : session attachée, TrameAuthentification activée");
        }

        SessionAttachee?.Invoke(this, session);
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
