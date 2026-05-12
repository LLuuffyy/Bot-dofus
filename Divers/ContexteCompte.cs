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
    public BotDofus.Divers.Securite.DetecteurStaff DetecteurStaff { get; }
    public StatsSession Stats { get; } = new();

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
    public bool ModePassif { get; set; } = true;

    public event EventHandler<SessionProxy>? SessionAttachee;
    public event EventHandler<SessionProxy>? SessionJeuAttachee;
    public event EventHandler<EvenementPaquetRecu>? PaquetRecu;

    // AYK Hystoria : "AYK<ip>:<port>;<ticket>"
    // AYK Aqua     : "AYKaqua.play-astra.net:5562;<ticket>"  → hostname accepté en plus de l'IP
    // Le groupe <hote> capture l'un OU l'autre ; la résolution DNS éventuelle est faite à
    // l'ouverture du proxy jeu sortant.
    private static readonly Regex RegexAyk = new(
        @"^AYK(?<hote>[A-Za-z0-9.\-_]+):(?<port>\d+);(?<ticket>.+)$",
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

        // Détecteur staff : check périodique .staff, alerte si modo détecté.
        var configSec = BotDofus.Divers.Securite.ConfigSecurite.Charger(
            Path.Combine("config", $"securite-{compte.Identifiant}.json"));
        DetecteurStaff = new BotDofus.Divers.Securite.DetecteurStaff(configSec);

        Compte.EtatChange += OnEtatCompteChange;
        Proxy.PaquetRecu += OnPaquetRecu;
        Proxy.SessionDemarree += OnSessionDemarree;

        // Stats de session : on suit l'évolution kamas/xp côté Personnage et l'état combat.
        EtatJeu.Personnage.Mis_A_Jour += (_, __) =>
        {
            Stats.NotifierKamas(EtatJeu.Personnage.Kamas);
            Stats.NotifierXp(EtatJeu.Personnage.XpActuelle);
        };
        EtatJeu.Combat.EtatChange += (_, etat) => Stats.NotifierEtatCombat(etat);
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
            Trames.RemplacerTrame(new TrameJeu(Repartiteur, Compte, EtatJeu, session));
        }
        else
        {
            Trames.RemplacerTrame(new TrameSelectionPersonnage(Repartiteur, Compte, session, Compte.PersonnagePrefere));
        }

        // Détecteur staff branché sur la session jeu (pour observer les paquets Im).
        DetecteurStaff.LierSession(session);
        _ = DetecteurStaff.DemarrerAsync();

        // Stats de session : on attend que le packet As (stats) ait peuplé Kamas/Xp avant de figer le baseline.
        // En attendant, on snapshot immédiatement avec les valeurs courantes (souvent 0) — le baseline
        // sera mis à jour par TrameJeu via Stats.NotifierKamas/NotifierXp.
        Stats.DemarrerSession(EtatJeu.Personnage);

        DemarrerAutoScriptSiPresent();

        Journaliseur.Info($"Contexte {Compte.Identifiant} : session jeu attachée");
        Journaliseur.Info("[ORCH] Session JEU attachee - le cipher Hystoria sera gere automatiquement par SessionProxy");
        SessionJeuAttachee?.Invoke(this, session);
    }

    /// <summary>
    /// Si <c>scripts/{identifiant}.lua</c> existe, le charge et le lance dans <see cref="MoteurLuaInteractif"/>.
    /// Permet de personnaliser automatiquement la rotation IA combat / le comportement du bot
    /// dès que la session jeu est attachée. Silencieux si aucun script n'est trouvé.
    /// </summary>
    private void DemarrerAutoScriptSiPresent()
    {
        try
        {
            var chemin = Path.Combine("scripts", $"{Compte.Identifiant}.lua");
            if (!File.Exists(chemin)) return;

            Lua.Charger(chemin);
            Lua.Demarrer();
            Journaliseur.Info($"[AUTO-SCRIPT] {chemin} démarré pour {Compte.Identifiant}");
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[AUTO-SCRIPT] Echec démarrage script auto : {ex.Message}");
        }
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

        var hoteJeu = match.Groups["hote"].Value;
        var portJeu = int.Parse(match.Groups["port"].Value);
        var ticket = match.Groups["ticket"].Value;

        Journaliseur.Info($"[ORCH] AYK intercepte : serveur jeu reel = {hoteJeu}:{portJeu}, ticket={ticket}");
        // hoteJeu peut être un hostname (Aqua : aqua.play-astra.net) ou une IP (Hystoria).
        // La résolution DNS est faite par ProxyReseau quand il ouvre la socket sortante,
        // donc on peut transmettre la chaîne telle quelle.
        DemarrerProxyJeu(hoteJeu, portJeu);

        var aykLocal = $"AYK127.0.0.1:{_configReseau.PortEcouteJeuLocal};{ticket}";
        Journaliseur.Info($"[ORCH] AYK reecrit -> {aykLocal}");
        Journaliseur.Info($"[CONTEXTE] AYK redirige : serveur jeu reel {hoteJeu}:{portJeu} -> proxy local 127.0.0.1:{_configReseau.PortEcouteJeuLocal}");
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

        Stats.NotifierPaquet(e);
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
        try { DetecteurStaff.Dispose(); } catch { }
        try { Lua.Dispose(); } catch { }
        try { Scripts.Dispose(); } catch { }
        try { Trames.Vider(); } catch { }
        try { ProxyJeu?.Dispose(); } catch { }
        try { Proxy.Dispose(); } catch { }
        try { Compte.EtatChange -= OnEtatCompteChange; } catch { }
        try { Compte.Dispose(); } catch { }
    }
}
