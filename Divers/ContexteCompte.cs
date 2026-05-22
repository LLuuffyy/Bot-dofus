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
    public BotDofus.Divers.Banque.ConfigBanque ConfigBanque { get; }
    public BotDofus.Divers.MultiAccount.ConfigGroupeHeros ConfigGroupeHeros { get; }
    private readonly BotDofus.Divers.MultiAccount.AutoInviteurHeros _inviteurHeros;
    public readonly BotDofus.Divers.MultiAccount.ActivateurHerosAbrak ActivateurHerosAbrak;

    /// <summary>Bouton « Inviter maintenant » côté UI : déclenche l'auto-inviteur indépendamment du toggle.</summary>
    public System.Threading.Tasks.Task InviterHerosMaintenantAsync()
        => _inviteurHeros.LancerAsync(SessionJeuActive);
    public BotDofus.Divers.Securite.DetecteurStaff DetecteurStaff { get; }
    public StatsSession Stats { get; } = new();
    public BotDofus.Divers.Interception.GestionnaireInterception Interception { get; } = new();

    public SessionProxy? SessionAuthActive { get; private set; }
    public SessionProxy? SessionJeuActive { get; private set; }
    public SessionProxy? SessionActive => SessionJeuActive ?? SessionAuthActive;
    public EnregistreurPaquets? EnregistreurActif { get; private set; }

    /// <summary>
    /// Login capturé du dernier paquet auth (paquet C→S juste après le HC challenge).
    /// Permet d'ajouter le compte couramment connecté à la liste sans le retaper.
    /// </summary>
    public string? LoginCapture { get; private set; }

    private bool _modePassif = true;

    /// <summary>
    /// En mode passif, le proxy se contente de relayer/journaliser les octets
    /// SANS activer la moindre trame d'automatisation. Idéal pour capturer
    /// du trafic réel sans risquer d'envoyer des paquets invalides au serveur.
    ///
    /// Le passage en mode ACTIF (passif=false) active automatiquement la garde
    /// anti-burst de l'humaniseur : tout paquet injecté par le bot sera espacé
    /// de manière humaine (Phase 3, anti-détection cadence).
    /// </summary>
    public bool ModePassif
    {
        get => _modePassif;
        set
        {
            _modePassif = value;
            Api.Humaniseur.Actif = !value;
            // Miroir sur Compte pour que TrameJeu (qui n'a pas accès au
            // ContexteCompte) puisse aussi bloquer son IA combat auto.
            Compte.ModePassif = value;
        }
    }

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
        // Expose la config au Compte pour que TrameJeu y accède au moment
        // de jouer le tour (le décideur IA en a besoin pour appliquer les
        // règles de sorts, focus, conditions — modèle dyshay/SynFus).
        compte.ConfigCombat = ConfigCombat;

        // Config banque (banque/<perso>.json). Désactivée par défaut.
        ConfigBanque = BotDofus.Divers.Banque.ConfigBanque.Charger(
            Path.Combine("banque", $"{compte.Identifiant}.json"));
        compte.ConfigBanque = ConfigBanque;

        // Config groupe héros (multi-account/<perso>.json) + auto-inviteur.
        ConfigGroupeHeros = BotDofus.Divers.MultiAccount.ConfigGroupeHeros.Charger(compte.Identifiant);
        _inviteurHeros = new BotDofus.Divers.MultiAccount.AutoInviteurHeros(compte, ConfigGroupeHeros);
        ActivateurHerosAbrak = new BotDofus.Divers.MultiAccount.ActivateurHerosAbrak(compte);
        compte.ActivateurHerosAbrak = ActivateurHerosAbrak;

        // Hook event poids → check seuil + Discord notif si configuré.
        EtatJeu.Personnage.Mis_A_Jour += OnPersonnageMisAJour;
        // Tag du timestamp à chaque changement de carte — sert au délai de grâce
        // banque (pas de trigger pendant que la map se synchronise).
        EtatJeu.CarteChangee += (_, _) => _dernierChangementCarteUtc = DateTime.UtcNow;
        ApiLua = new ApiLua(Api, EtatJeu, ConfigCombat, Interception);
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
        Interception.PaquetModifie += (_, __) => Stats.NotifierInterception();

        // === AUTO-COMBAT (injection cleartext, contourne le Shield) ===
        // Découverte clé : GR1 (Prêt) et GT (fin de tour) sont en CLAIR côté
        // Abrak (≠ canal chiffré '-' qui ne porte que déplacements/sorts).
        // Formats confirmés par le bot de réf. dyshay (≈ SynFus). Stratégie
        // auto-farm fiable : GR1 au placement → passer chaque tour (GT) →
        // les alliés leech tuent → loot. Aucun pixel, aucun Shield.
        //
        // Placement : on est "Prêt" automatiquement (1 seule fois / combat).
        EtatJeu.Combat.EtatChange += async (_, etat) =>
        {
            if (ModePassif) return;
            // En mode ACTIF (passif=false), le bot fait TOUT : placement, ready,
            // IA combat. Pas de check MITM séparé : si le user veut jouer à la
            // main il toggle le ModePassif. Si conflit (les 2 cliquent ready),
            // le serveur ignore le 2ème. User feedback 16:46 : bot ne plaçait/
            // ready pas en MITM ; cause = ancien check « SessionJeuActive != null
            // → return ». Retiré : mode actif = bot full auto.
            if (etat == BotDofus.Divers.Combats.Enums.EtatCombat.Placement && !_combatPretEnvoye)
            {
                _combatPretEnvoye = true;
                try
                {
                    // G.3 — Placement initial selon Mode (cf. dyshay
                    // get_Celda_Mas_Cercana_O_Lejana). Avant GR1, on choisit
                    // une cell parmi PositionsEquipe1/2 (mon équipe) qui
                    // matche le Mode configuré (Agressif=min dist, Eloigne/
                    // Fuyard=max dist, Equilibre=DistancePreferee).
                    await System.Threading.Tasks.Task.Delay(800);
                    ChoisirEtEnvoyerPlacementInitial();
                    await System.Threading.Tasks.Task.Delay(1400);
                    // Garde anti-reconnexion-mid-combat : si on rejoint un combat
                    // DÉJÀ en cours (le serveur rejoue le placement puis enchaîne
                    // direct sur GS/GTS tour N), l'état n'est plus Placement après
                    // le délai → surtout PAS de GR1 (sinon désync, abandon forcé).
                    if (EtatJeu.Combat.Etat != BotDofus.Divers.Combats.Enums.EtatCombat.Placement)
                    {
                        Journaliseur.Info(
                            "[AUTO-COMBAT] Reconnexion en combat déjà engagé "
                            + $"(état={EtatJeu.Combat.Etat}) → GR1 NON envoyé (anti-désync).");
                        return;
                    }
                    await Api.EnvoyerPaquetBrutAsync("GR1");
                    Journaliseur.Info("[AUTO-COMBAT] Prêt envoyé (GR1).");
                }
                catch (Exception ex) { Journaliseur.Avertir($"[AUTO-COMBAT] GR1 : {ex.Message}"); }
            }
            else if (etat == BotDofus.Divers.Combats.Enums.EtatCombat.Inactif)
            {
                _combatPretEnvoye = false;
            }
        };

        // Mon tour : la vraie IA combat est dans TrameJeu.JouerTourCombatAsync
        // (handler MessageTourCombatAbrak, déjà branché pour Hystoria). Le code
        // legacy DecideurCombat a été supprimé (commit Phase 7 finalisée).
    }

    private bool _combatPretEnvoye;

    private void OnSessionDemarree(object? sender, SessionProxy session)
    {
        SessionAuthActive = session;
        Api.LierSession(session);
        InstallerInterceptionAyk(session);

        // IMPORTANT (modèle MITM-avec-vrai-client Abrak) : le VRAI client Dofus
        // fait l'authentification. Le bot ne DOIT JAMAIS installer
        // TrameAuthentification, même en mode actif : il injecterait ses propres
        // paquets d'auth en parallèle du client → handshake corrompu, le serveur
        // répond AlEv1.48.0 (« Connexion refusée : code v »). Le mode actif
        // n'active QUE l'humaniseur + l'autorisation d'injection (cf. ModePassif),
        // jamais le self-login. La session auth reste en simple observation.
        Journaliseur.Info($"Contexte {Compte.Identifiant} : session auth attachée (observation — auth gérée par le client)"
            + (ModePassif ? " [PASSIF]" : " [ACTIF : injection autorisée, humaniseur ON]"));

        SessionAttachee?.Invoke(this, session);
    }

    private void OnSessionJeuDemarree(object? sender, SessionProxy session)
    {
        SessionJeuActive = session;
        Api.LierSession(session);
        // Pose la session sur l'activateur héros DÈS l'attache jeu : le client
        // Dofus envoie souvent son propre NOL avant que ChangerEtat(EnJeu) ne
        // tire, et on veut que OnHerosOrdre puisse réagir en ayant la session.
        ActivateurHerosAbrak.AttacherSession(session);

        // TOUJOURS TrameJeu (observation + parseurs combat/entités), que l'on
        // soit passif ou actif : le client réel gère sélection perso/serveur.
        // TrameSelectionPersonnage injecterait des paquets de sélection en
        // doublon du client → kick. Le mode actif change seulement l'humaniseur
        // et l'autorisation d'injection (boutons UI / IA), pas la trame.
        Trames.RemplacerTrame(new TrameJeu(Repartiteur, Compte, EtatJeu, session));

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

    // ============================================================
    // Anti-stuck + Discord notif + banque trigger
    // ============================================================
    private bool _banqueDeclenchee;
    private bool _mortNotifiee;
    /// <summary>Timestamp UTC du dernier changement de carte — sert à imposer
    /// un délai de grâce de 2s avant de déclencher la banque (le perso vient
    /// de zaap, l'inventaire peut encore se synchroniser).</summary>
    private DateTime _dernierChangementCarteUtc = DateTime.MinValue;

    private void OnPersonnageMisAJour(object? sender, EventArgs e)
    {
        var perso = EtatJeu.Personnage;

        // ----- Détection MORT (PV = 0 et > 0 précédemment) -----
        if (perso.VieMax > 0 && perso.Vie == 0 && !_mortNotifiee)
        {
            _mortNotifiee = true;
            Stats.NotifierMort();
            Journaliseur.Avertir($"[MORT] 💀 {perso.Nom} est mort sur la map {perso.CarteCourante}");
            if (!string.IsNullOrWhiteSpace(Compte.WebhookDiscordUrl))
            {
                _ = BotDofus.Divers.Notifications.NotificateurDiscord.NotifierMortAsync(
                    Compte.WebhookDiscordUrl, perso.Nom,
                    perso.Niveau, perso.CarteCourante?.ToString() ?? "?");
            }
        }
        else if (perso.Vie > 0)
        {
            _mortNotifiee = false;  // reset si ressuscité
        }

        // ----- Trigger banque (poids ≥ seuil) -----
        // Délai de grâce 2s après changement de carte : sinon le trigger peut
        // fire pendant que la map se synchronise (CarteCourante null, inventaire
        // pas encore complet) → log 17:10:22 cas observé.
        bool grace = (DateTime.UtcNow - _dernierChangementCarteUtc).TotalMilliseconds < 2000;
        if (ConfigBanque.Active
            && !ModePassif                                              // mode passif → AUCUNE action auto, y compris banque
            && !_banqueDeclenchee
            && !grace
            && perso.PourcentagePoids >= ConfigBanque.SeuilPoidsPct
            && ConfigBanque.SeuilPoidsPct > ConfigBanque.CiblePoidsPct  // config valide
            && EtatJeu.Combat.Etat == BotDofus.Divers.Combats.Enums.EtatCombat.Inactif)
        {
            _banqueDeclenchee = true;
            Journaliseur.Avertir(
                $"[BANQUE] 📦 Poids {perso.PourcentagePoids:F1}% ≥ seuil {ConfigBanque.SeuilPoidsPct}% "
                + "→ déclenchement workflow banque");
            if (!string.IsNullOrWhiteSpace(Compte.WebhookDiscordUrl))
            {
                _ = BotDofus.Divers.Notifications.NotificateurDiscord.NotifierBanquePleineAsync(
                    Compte.WebhookDiscordUrl, perso.Nom, (int)perso.PourcentagePoids);
            }
            // Pilote async fire-and-forget. Capture la map AVANT le départ.
            int? carteAvant = perso.CarteCourante;
            var session = SessionJeuActive;
            if (session != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Pause script Lua si actif (l'user relancera après dépôt).
                        bool scriptEnExecution = Scripts.Etat == BotDofus.Divers.Scripts.EtatScript.EnExecution;
                        if (scriptEnExecution) Scripts.MettreEnPause();

                        var pilote = new BotDofus.Divers.Banque.PiloteBanque(Api, session, perso, ConfigBanque);
                        await pilote.WorkflowCompletAsync(carteAvant);

                        if (scriptEnExecution)
                            Journaliseur.Info("[BANQUE] Workflow terminé — script Lua en PAUSE, à reprendre manuellement.");
                    }
                    catch (Exception bex)
                    {
                        Journaliseur.Erreur("[BANQUE] Workflow échec", bex);
                        if (!string.IsNullOrWhiteSpace(Compte.WebhookDiscordUrl))
                        {
                            _ = BotDofus.Divers.Notifications.NotificateurDiscord.NotifierErreurCritiqueAsync(
                                Compte.WebhookDiscordUrl, perso.Nom, $"workflow banque : {bex.Message}");
                        }
                    }
                    finally
                    {
                        // Reset SEULEMENT à la fin du workflow (succès ou exception).
                        // Avant : reset sur `poids < cible` → la moindre fluctuation
                        // pendant les dépôts re-déclenchait un workflow concurrent
                        // (log 17:10:22+17:10:52+17:11:30… 4× workflows pour 1 trigger).
                        _banqueDeclenchee = false;
                        Journaliseur.Debogue("[BANQUE] Flag déclenchement reset après fin workflow");
                    }
                });
            }
            else
            {
                Journaliseur.Avertir("[BANQUE] Session jeu inactive — workflow skip");
                _banqueDeclenchee = false;  // pas de Task → reset immédiat sinon blocage
            }
        }

        // ----- Anti-stuck timer (track dernier changement map) -----
        // (le tracking se fait dans OnDonneesCarte ; ici on déclenche juste
        // un timer périodique si pas de changement.)
    }

    private void OnEtatCompteChange(object? sender, Enums.EtatsCompte etat)
    {
        // Modèle MITM-avec-client : on ne swappe JAMAIS vers des trames qui
        // injectent (TrameSelectionServeur/Authentification) — le vrai client
        // pilote login/sélection. On garantit juste TrameJeu (observation +
        // parseurs combat) sur la session jeu, en passif comme en actif.
        if (etat == Enums.EtatsCompte.EnJeu && SessionJeuActive != null &&
            Trames.TrameActive is not TrameJeu)
        {
            Trames.RemplacerTrame(new TrameJeu(Repartiteur, Compte, EtatJeu, SessionJeuActive));
        }

        // Auto-invitation héros : déclenchée à l'entrée En Jeu si configuré.
        // Skip en mode passif / si déjà tournée (le service est anti-réentrant).
        if (etat == Enums.EtatsCompte.EnJeu && SessionJeuActive != null && !ModePassif)
        {
            // ActivateurHerosAbrak DÉSACTIVÉ (demande user 2026-05-22) :
            // les paquets NOL / NS qu'il envoie déclenchent l'ouverture de
            // la fenêtre « Recherche de héros » côté client Dofus à chaque
            // démarrage, ce qui est gênant. Le mode héros marche déjà sans
            // (les héros sont activés serveur-side via GTSX en combat).
            // → seul l'AutoInviteur PI<Nom> classique tourne, pour les vrais
            //   PJs externes invités via la liste NomsHeros.
            _ = _inviteurHeros.LancerAsync(SessionJeuActive);
        }
    }

    private void InstallerInterceptionAyk(SessionProxy session)
    {
        var modificateurExistant = session.ModificateurPaquet;
        session.ModificateurPaquet = (paquet, direction) =>
        {
            // 1. Chaîne précédente éventuelle (compat)
            var modifie = modificateurExistant?.Invoke(paquet, direction);
            var aTraiter = modifie ?? paquet;
            if (aTraiter.Length == 0)
            {
                return aTraiter; // déjà supprimé en amont
            }

            // 2. Règles d'interception scriptables (Phase 2 : intercept-and-modify).
            //    S'exécute AVANT l'AYK pour pouvoir modifier n'importe quel paquet.
            var modifInterception = Interception.Appliquer(aTraiter, direction);
            if (modifInterception != null)
            {
                if (modifInterception.Length == 0)
                {
                    return string.Empty; // règle a supprimé le paquet
                }
                aTraiter = modifInterception;
            }

            // 3. Réécriture AYK (redirige le serveur jeu vers notre proxy local).
            var ayk = IntercepterAyk(aTraiter, direction);
            if (ayk != null)
            {
                return ayk; // c'était un AYK : version réécrite prioritaire
            }

            // 4. Pas un AYK : on préserve la modif d'interception/chaîne si elle a eu lieu.
            if (modifInterception != null) return modifInterception;
            if (modifie != null) return modifie;
            return null; // vraiment inchangé
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

    /// <summary>
    /// Démarre le proxy JEU SANS attendre l'AYK. Indispensable pour Abrak/WinDivert :
    /// le client ferme la connexion auth après la sélection serveur (comportement
    /// Dofus normal) puis ouvre une connexion NEUVE vers le serveur de jeu
    /// (51.89.153.20:1304). WinDivert la redirige sur 127.0.0.1:1304 — mais si rien
    /// n'écoute encore là (proxy jeu démarré seulement à l'interception AYK, qui
    /// arrive trop tard ou dans un format Abrak non parsé), le client affiche
    /// « serveur introuvable ». On pré-démarre donc le listener jeu dès le lancement.
    /// </summary>
    public void DemarrerProxyJeuEager()
    {
        DemarrerProxyJeu(_configReseau.HoteJeuDistant, _configReseau.PortJeuDistant);
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
            // Marqueur DÉDIÉ jeu (50304) : la connexion sortante du proxy jeu vers
            // 51.89.153.20:1304 doit être exclue du filtre WinDivert avec un port
            // DIFFÉRENT de celui du proxy auth (50303) — sinon bind conflict + boucle.
            PortSourceMarqueur = _configReseau.PortSourceMarqueurJeu,
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

        // Auto-apprentissage des objets interactifs : tout GA500<cell>;<skill>
        // C→S (récolte manuelle OU bot) nous révèle le skill d'une ressource.
        // On résout la cellule → gfx interactif sur la carte courante et on
        // mémorise/persiste le couple (gfx → skill) dans la BDD interactifs.
        if (e.Paquet.Direction == DirectionPaquet.VersServeur
            && e.Paquet.Contenu.StartsWith("GA500", System.StringComparison.Ordinal))
        {
            try
            {
                var corps = e.Paquet.Contenu.Substring(5);
                var bouts = corps.Split(';');
                if (bouts.Length >= 2
                    && int.TryParse(bouts[0], out var celluleRec)
                    && int.TryParse(bouts[1], out var skillRec))
                {
                    var cell = EtatJeu.CarteCourante?.Obtenir(celluleRec);
                    if (cell != null && cell.IdInteractif > 0)
                        Donnees.BaseDonnees.Instance.ApprendreInteractif(cell.IdInteractif, skillRec);
                    else
                        Journaliseur.Info(
                            $"[BDD] GA500 cell {celluleRec};skill {skillRec} : pas d'IdInteractif "
                            + "résolu sur la carte courante (objet non décodé).");
                }
            }
            catch (Exception ex) { Journaliseur.Avertir($"[BDD] apprentissage GA500 : {ex.Message}"); }
        }

        // Dialogue PNJ (serveur → client) : alimente EtatJeu.Dialogue pour
        // l'API de script (npc.hasReply/getRepliesId/reply) — DCK ouvre,
        // DQ<q>[|r1;r2…] = question + réponses, DV ferme.
        if (e.Paquet.Direction == DirectionPaquet.VersClient)
        {
            var d = e.Paquet.Contenu;
            try
            {
                if (d.StartsWith("DCK", StringComparison.Ordinal))
                {
                    var npc = d[3..].Split(',', ';')[0];
                    int.TryParse(npc, out var npcId);
                    EtatJeu.Dialogue.Ouvrir(npcId);
                }
                else if (d.StartsWith("DV", StringComparison.Ordinal))
                {
                    EtatJeu.Dialogue.Fermer();
                }
                else if (d.StartsWith("DQ", StringComparison.Ordinal))
                {
                    var corps = d[2..];
                    var parts = corps.Split('|');
                    int.TryParse(parts[0].Split(';')[0], out var qid);
                    var reps = new System.Collections.Generic.List<int>();
                    if (parts.Length > 1)
                        foreach (var r in parts[1].Split(';', ',',
                                     StringSplitOptions.RemoveEmptyEntries))
                            if (int.TryParse(r, out var rid)) reps.Add(rid);
                    EtatJeu.Dialogue.Question(qid, reps);
                }
            }
            catch { /* parsing dialogue best-effort */ }
        }

        Stats.NotifierPaquet(e);
        Repartiteur.TraiterPaquet(e.Paquet);
        PaquetRecu?.Invoke(this, e);
    }

    /// <summary>
    /// Branche un client AUTONOME (architecture SynFus, sans client officiel)
    /// sur TOUT le cerveau existant : ses paquets déchiffrés alimentent
    /// Repartiteur/TrameJeu → carte, entités, position, combat, IA — et l'API
    /// envoie via lui (clair/chiffré '-' selon whitelist). C'est « SynFus mais
    /// en mieux » : on réutilise le parsing/pathfinder/UI déjà construits.
    /// </summary>
    public void BrancherClientAutonome(ClientAutonomeAbrak client)
    {
        // TrameJeu en mode parsing (pas de SessionProxy : les envois passent
        // par l'API → client autonome, pas par TrameJeu._session).
        if (Trames.TrameActive is not TrameJeu)
            Trames.RemplacerTrame(new TrameJeu(Repartiteur, Compte, EtatJeu, null!));

        Api.LierClientAutonome(client);

        client.PaquetClair += p =>
        {
            try
            {
                OnPaquetRecu(this, new EvenementPaquetRecu(
                    new PaquetBrut(DirectionPaquet.VersClient, p)));
            }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[AUTO] parsing '{(p.Length > 12 ? p[..12] : p)}…' : {ex.Message}");
            }
        };
        Journaliseur.Info("[AUTO] Client autonome branché sur le cerveau (parsing+API).");
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

    /// <summary>
    /// G.3 — Placement initial avant GR1, selon <see cref="Combats.IA.ModeCombat"/>.
    /// Aligné dyshay <c>Fight.get_Celda_Mas_Cercana_O_Lejana</c> + <c>MapFrame</c>
    /// ligne 106-126 : on choisit une cell parmi <c>Combat.PositionsEquipe1/2</c>
    /// (mon équipe) qui matche le Mode configuré, puis on envoie <c>Gp&lt;cell&gt;</c>
    /// si la cell est différente de notre position actuelle. Le serveur répond
    /// <c>GIC</c> (confirmation placement) puis on enchaîne le <c>GR1</c>.
    /// </summary>
    private void ChoisirEtEnvoyerPlacementInitial()
    {
        try
        {
            var combat = EtatJeu.Combat;
            if (combat.PositionsEquipe1.Count == 0 && combat.PositionsEquipe2.Count == 0) return;
            var carte = EtatJeu.CarteCourante;
            if (carte == null) return;

            int monEquipe = combat.EquipePlacement;
            var maTeamCells = monEquipe == 1 ? combat.PositionsEquipe2 : combat.PositionsEquipe1;
            var ennemisCells = monEquipe == 1 ? combat.PositionsEquipe1 : combat.PositionsEquipe2;
            if (maTeamCells.Count == 0 || ennemisCells.Count == 0) return;

            var cfg = ConfigCombat;
            var mode = cfg?.Mode ?? Combats.IA.ModeCombat.Equilibre;
            int distPref = cfg?.DistancePreferee ?? 5;
            int mw = carte.Largeur > 0 ? carte.Largeur : Cartes.Carte.LargeurParDefaut;

            // Helper dist Manhattan (= dyshay GetDistanceBetweenCells, dist cells
            // en combat 4-dir où Manhattan == Chebyshev car pas de diagonale).
            int Manhattan(int idA, int idB)
            {
                var (xA, yA) = Cartes.Cellule.CalculerCoordonnees(idA, mw);
                var (xB, yB) = Cartes.Cellule.CalculerCoordonnees(idB, mw);
                return System.Math.Abs(xA - xB) + System.Math.Abs(yA - yB);
            }
            int SommeDistEnnemis(int cellId)
            {
                int s = 0;
                foreach (var e in ennemisCells) s += Manhattan(cellId, e);
                return s;
            }

            int meilleureCell = -1;
            double meilleurScore = double.MaxValue;
            foreach (var cell in maTeamCells)
            {
                double score = mode switch
                {
                    Combats.IA.ModeCombat.Agressif => SommeDistEnnemis(cell),                 // minimise
                    Combats.IA.ModeCombat.Eloigne  => -SommeDistEnnemis(cell),                // maximise (=min de -dist)
                    Combats.IA.ModeCombat.Fuyard   => -SommeDistEnnemis(cell),                // idem Eloigne en placement
                    Combats.IA.ModeCombat.Equilibre => System.Math.Abs(
                        ennemisCells.Count > 0
                            ? Manhattan(cell, ennemisCells[0]) - distPref
                            : 0),
                    _ => 0
                };
                if (score < meilleurScore)
                {
                    meilleurScore = score;
                    meilleureCell = cell;
                }
            }

            int? maCell = EtatJeu.Personnage.CellulePosition;
            if (meilleureCell < 0) return;
            if (maCell.HasValue && meilleureCell == maCell.Value)
            {
                Journaliseur.Info(
                    $"[PLACEMENT-DEBUT] Mode={mode}, déjà sur la cell optimale {meilleureCell} → pas de Gp");
                return;
            }
            Journaliseur.Info(
                $"[PLACEMENT-DEBUT] Mode={mode}, {maTeamCells.Count} cells dispo, "
                + $"ennemis cells=[{string.Join(",", ennemisCells)}], "
                + $"cell choisie={meilleureCell} (vs ma cell {maCell})");
            // Format : Gp<cellId_decimal> (PAS de hash 2-char) — confirmé dyshay MapFrame.cs:115
            _ = Api.EnvoyerPaquetBrutAsync($"Gp{meilleureCell}");
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[PLACEMENT-DEBUT] Erreur : {ex.Message}");
        }
    }
}
