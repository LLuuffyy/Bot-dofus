using System;
using System.IO;
using System.Text.RegularExpressions;
using BotDofus.Commun;
using BotDofus.Commun.Frames;
using BotDofus.Commun.Reseau;
using BotDofus.Divers.Combats.IA;
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
    public ProxyReseau? ProxyJeu { get; private set; }
    public Repartiteur Repartiteur { get; }
    public GestionnaireTrames Trames { get; }
    public EtatJeu EtatJeu { get; }
    public ApiBot Api { get; }
    public ApiLua ApiLua { get; }
    public GestionnaireScripts Scripts { get; }
    public MoteurLuaInteractif Lua { get; }
    public ConfigCombat ConfigCombat { get; }

    public SessionProxy? SessionAuthActive { get; private set; }
    public SessionProxy? SessionJeuActive { get; private set; }
    public SessionProxy? SessionActive => SessionJeuActive ?? SessionAuthActive;
    public EnregistreurPaquets? EnregistreurActif { get; private set; }

    /// <summary>
    /// Login capturé du dernier paquet auth (paquet C→S juste après le HC challenge).
    /// Permet d'ajouter le compte couramment connecté à la liste sans le retaper.
    /// </summary>
    public string? LoginCapture { get; private set; }

    /// <summary>
    /// En mode passif, le proxy se contente de relayer/journaliser les octets
    /// SANS activer la moindre trame d'automatisation. Idéal pour capturer
    /// du trafic réel sans risquer d'envoyer des paquets invalides au serveur.
    /// </summary>
    public bool ModePassif { get; set; }

    public event EventHandler<SessionProxy>? SessionAttachee;
    public event EventHandler<SessionProxy>? SessionJeuAttachee;
    public event EventHandler<EvenementPaquetRecu>? PaquetRecu;

    private static readonly Regex RegexAyk = new(
        @"^AYK(?<ip>\d{1,3}(?:\.\d{1,3}){3}):(?<port>\d+);(?<ticket>.+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ConfigReseau _configReseau;

    public ContexteCompte(Compte compte, ConfigReseau configReseau)
    {
        Compte = compte;
        _configReseau = configReseau;
        Proxy = new ProxyReseau(configReseau);
        Repartiteur = new Repartiteur();
        Trames = new GestionnaireTrames();
        EtatJeu = new EtatJeu();
        Api = new ApiBot(compte, EtatJeu);
        ConfigCombat = ConfigCombat.Charger(Path.Combine("peleas", $"{compte.Identifiant}.json"));
        ApiLua = new ApiLua(Api, EtatJeu, ConfigCombat);
        Scripts = new GestionnaireScripts(compte, Api);
        Lua = new MoteurLuaInteractif(ApiLua);

        Compte.EtatChange += OnEtatCompteChange;
        Proxy.PaquetRecu += OnPaquetRecu;
        Proxy.SessionDemarree += OnSessionDemarree;
    }

    private void OnSessionDemarree(object? sender, SessionProxy session)
    {
        SessionAuthActive = session;
        Api.LierSession(session);
        InstallerInterceptionAyk(session);

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

    private void OnSessionJeuDemarree(object? sender, SessionProxy session)
    {
        SessionJeuActive = session;
        Api.LierSession(session);

        if (ModePassif)
        {
            // Mode passif : on installe quand même TrameJeu pour parser les paquets serveur
            // (carte, HP, kamas, poids, combat) et alimenter EtatJeu / les vues WPF.
            // TrameJeu n'envoie RIEN au serveur — c'est de l'observation pure.
            Trames.RemplacerTrame(new TrameJeu(Repartiteur, Compte, EtatJeu, session));
        }
        else
        {
            Trames.RemplacerTrame(new TrameSelectionPersonnage(Repartiteur, Compte, session, Compte.PersonnagePrefere));
        }

        Journaliseur.Info($"Contexte {Compte.Identifiant} : session jeu attachée");
        Journaliseur.Info("[ORCH] Session JEU attachee - le cipher Hystoria sera gere automatiquement par SessionProxy");
        SessionJeuAttachee?.Invoke(this, session);
    }

    private void OnEtatCompteChange(object? sender, Enums.EtatsCompte etat)
    {
        if (ModePassif)
        {
            return;
        }

        if (etat == Enums.EtatsCompte.SelectionServeur && SessionAuthActive != null &&
            Trames.TrameActive is not TrameSelectionServeur)
        {
            Trames.RemplacerTrame(new TrameSelectionServeur(Repartiteur, Compte, SessionAuthActive, Compte.ServeurPrefere));
            return;
        }

        if (etat == Enums.EtatsCompte.EnJeu && SessionJeuActive != null &&
            Trames.TrameActive is not TrameJeu)
        {
            Trames.RemplacerTrame(new TrameJeu(Repartiteur, Compte, EtatJeu, SessionJeuActive));
        }
    }

    private void InstallerInterceptionAyk(SessionProxy session)
    {
        var modificateurExistant = session.ModificateurPaquet;
        session.ModificateurPaquet = (paquet, direction) =>
        {
            var modifie = modificateurExistant?.Invoke(paquet, direction);
            var aTraiter = modifie ?? paquet;
            if (aTraiter.Length == 0)
            {
                return aTraiter;
            }

            return IntercepterAyk(aTraiter, direction);
        };
    }

    private string? IntercepterAyk(string paquet, DirectionPaquet direction)
    {
        if (direction != DirectionPaquet.VersClient || !paquet.StartsWith("AYK", StringComparison.Ordinal))
        {
            return null;
        }

        var match = RegexAyk.Match(paquet);
        if (!match.Success)
        {
            Journaliseur.Avertir($"AYK reçu mais format inattendu : {paquet}");
            return null;
        }

        var ipJeu = match.Groups["ip"].Value;
        var portJeu = int.Parse(match.Groups["port"].Value);
        var ticket = match.Groups["ticket"].Value;

        Journaliseur.Info($"[ORCH] AYK intercepte : serveur jeu reel = {ipJeu}:{portJeu}, ticket={ticket}");
        DemarrerProxyJeu(ipJeu, portJeu);

        var aykLocal = $"AYK127.0.0.1:{_configReseau.PortEcouteJeuLocal};{ticket}";
        Journaliseur.Info($"[ORCH] AYK reecrit -> {aykLocal}");
        Journaliseur.Info($"[CONTEXTE] AYK redirige : serveur jeu reel {ipJeu}:{portJeu} -> proxy local 127.0.0.1:{_configReseau.PortEcouteJeuLocal}");
        return aykLocal;
    }

    private void DemarrerProxyJeu(string hoteDistant, int portDistant)
    {
        if (ProxyJeu?.EnEcoute == true)
        {
            return;
        }

        var configJeu = new ConfigReseau
        {
            HoteDistant = hoteDistant,
            PortDistant = portDistant,
            HoteJeuDistant = hoteDistant,
            PortJeuDistant = portDistant,
            AdresseEcouteLocale = "0.0.0.0",
            PortEcouteLocal = _configReseau.PortEcouteJeuLocal,
            PortEcouteJeuLocal = _configReseau.PortEcouteJeuLocal,
            DelaiLectureMs = _configReseau.DelaiLectureMs,
            TailleTamponOctets = _configReseau.TailleTamponOctets
        };

        ProxyJeu = new ProxyReseau(configJeu);
        ProxyJeu.PaquetRecu += OnPaquetRecu;
        ProxyJeu.SessionDemarree += OnSessionJeuDemarree;
        _ = ProxyJeu.DemarrerAsync();

        if (EnregistreurActif != null)
        {
            EnregistreurActif.AttacherA(ProxyJeu);
        }

        Journaliseur.Info($"[ORCH] Proxy jeu demarre ({configJeu.AdresseEcouteLocale}:{configJeu.PortEcouteLocal} -> {hoteDistant}:{portDistant})");
    }

    private void OnPaquetRecu(object? sender, EvenementPaquetRecu e)
    {
        // Capture du login : paquet C→S juste après HC challenge, format "<login>\n#1<encrypted>".
        // Le login est en clair, le mdp encrypté avec HC. On garde le login pour permettre
        // l'auto-add du compte courant via la sidebar UI.
        if (e.Paquet.Direction == DirectionPaquet.VersServeur
            && LoginCapture == null
            && e.Paquet.Contenu.Contains('\n')
            && e.Paquet.Contenu.Contains("#1"))
        {
            var ligneLogin = e.Paquet.Contenu.Split('\n')[0];
            // Le login Dofus n'est pas une version (ne commence pas par "1.") ni un keep-alive.
            if (!string.IsNullOrWhiteSpace(ligneLogin)
                && !ligneLogin.StartsWith("1.", System.StringComparison.Ordinal)
                && ligneLogin.Length is >= 2 and <= 64)
            {
                LoginCapture = ligneLogin;
                Journaliseur.Info($"[AUTO-ADD] Login capturé depuis le jeu : {ligneLogin}");
            }
        }

        Repartiteur.TraiterPaquet(e.Paquet);
        PaquetRecu?.Invoke(this, e);
    }

    public void DemarrerProxy() => Proxy.DemarrerAsync();

    public void ArreterProxy()
    {
        ProxyJeu?.Arreter();
        Proxy.Arreter();
    }

    /// <summary>Active l'enregistrement des paquets vers un fichier plat (un par ligne).</summary>
    public void ActiverEnregistrement(string dossier = "logs")
    {
        if (EnregistreurActif != null) return;
        EnregistreurActif = new EnregistreurPaquets(dossier);
        EnregistreurActif.AttacherA(Proxy);
        if (ProxyJeu != null) EnregistreurActif.AttacherA(ProxyJeu);
    }

    public void DesactiverEnregistrement()
    {
        if (EnregistreurActif == null) return;
        EnregistreurActif.Detacher(Proxy);
        if (ProxyJeu != null) EnregistreurActif.Detacher(ProxyJeu);
        EnregistreurActif.Dispose();
        EnregistreurActif = null;
    }

    public void Dispose()
    {
        try { DesactiverEnregistrement(); } catch { }
        try { Lua.Dispose(); } catch { }
        try { Scripts.Dispose(); } catch { }
        try { Trames.Vider(); } catch { }
        try { ProxyJeu?.Dispose(); } catch { }
        try { Proxy.Dispose(); } catch { }
        try { Compte.EtatChange -= OnEtatCompteChange; } catch { }
        try { Compte.Dispose(); } catch { }
    }
}
