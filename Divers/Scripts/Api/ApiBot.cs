using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BotDofus.Commun.Messages.VersServeur.Chat;
using BotDofus.Commun.Messages.VersServeur.Dialogue;
using BotDofus.Commun.Messages.VersServeur.Jeu;
using BotDofus.Commun.Reseau;
using BotDofus.Divers.Cartes;
using BotDofus.Divers.Cartes.Deplacement;
using BotDofus.Divers.Cartes.Entites;
using BotDofus.Divers.Combats.Enums;
using BotDofus.Divers.Jeu;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Scripts.Api;

/// <summary>
/// API exposée aux scripts (Lua ou .NET) pour piloter le bot de haut niveau :
/// déplacement, dialogue, combat, banque, chat.
///
/// Chaque méthode est asynchrone et respecte le <see cref="CancellationToken"/>
/// du script pour permettre une interruption propre.
/// </summary>
public sealed class ApiBot
{
    private readonly Compte _compte;
    private readonly EtatJeu _etat;
    private SessionProxy? _session;
    private ClientAutonomeAbrak? _clientAuto;

    // Garde anti-flood déplacement : un seul GA001 en vol à la fois. Sans ça,
    // un clic-carte répété ou la boucle farm spamment 6+ GA001 en 1 s (constaté
    // log 11:41) → le serveur clampe → désync de position → farm bloqué.
    private readonly System.Threading.SemaphoreSlim _verrouDeplacement = new(1, 1);

    /// <summary>
    /// Garde anti-burst (Phase 3) : impose un espacement humain entre les paquets
    /// que le BOT envoie de son propre chef. Désactivé par défaut (mode passif).
    /// </summary>
    public BotDofus.Divers.Securite.HumaniseurActions Humaniseur { get; } = new();

    public ApiBot(Compte compte, EtatJeu etat)
    {
        _compte = compte;
        _etat = etat;
    }

    /// <summary>Lie l'API à la session MITM active (appelée quand le client se connecte).</summary>
    public void LierSession(SessionProxy session) { _session = session; SessionMorte = false; }

    /// <summary>Lie l'API au client AUTONOME (architecture SynFus, sans client
    /// officiel). Prioritaire sur la session MITM si présent.</summary>
    public void LierClientAutonome(ClientAutonomeAbrak? client) { _clientAuto = client; SessionMorte = false; }

    /// <summary>
    /// Point d'envoi UNIQUE pour TOUT paquet initié par le bot. Passe
    /// systématiquement par la garde anti-burst <see cref="Humaniseur"/> :
    /// en mode passif elle ne fait rien, en mode actif elle impose un délai
    /// humain (jitter) entre deux actions → pas de pattern métronomique
    /// détectable côté serveur. C'EST le point de furtivité de l'examen :
    /// aucune méthode ne doit envoyer au serveur en court-circuitant ceci.
    /// </summary>
    /// <summary>true si la session/socket est morte → on n'envoie plus rien
    /// (les boucles auto-stats/scripts s'arrêtent au lieu de crasher l'UI).</summary>
    public bool SessionMorte { get; private set; }

    private async Task EnvoyerHumaniseAsync(string paquet, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(paquet) || SessionMorte) return;
        try
        {
            await Humaniseur.RespecterCadenceAsync(ct).ConfigureAwait(false);
            // Client autonome prioritaire (route clair/chiffré '-' via whitelist).
            if (_clientAuto is { EstConnecte: true })
                await _clientAuto.EnvoyerAuServeurAsync(paquet, ct).ConfigureAwait(false);
            else if (_session is not null)
            {
                // false = écriture KO (socket disposé) : la session est morte
                // mais l'exception est avalée plus bas → on coupe ICI sinon
                // les boucles auto (récolte/road) spamment à l'infini.
                bool ok = await _session.EnvoyerAuServeurAsync(paquet, ct).ConfigureAwait(false);
                if (!ok)
                {
                    SessionMorte = true;
                    Journaliseur.Avertir(
                        "[RÉSEAU] envoi KO (session fermée) — actions auto stoppées. "
                        + "Relance/relie une session.");
                }
            }
        }
        catch (OperationCanceledException) { /* arrêt normal */ }
        catch (Exception ex) when (ex is ObjectDisposedException
            or System.IO.IOException or System.Net.Sockets.SocketException
            or InvalidOperationException)
        {
            // Socket fermée (déconnexion / changement de session) : on coupe
            // proprement au lieu de laisser l'exception tuer le thread → crash UI.
            SessionMorte = true;
            Journaliseur.Avertir(
                $"[RÉSEAU] envoi impossible (session fermée) : {ex.GetType().Name}. "
                + "Les actions auto sont stoppées — relance/relie une session.");
        }
    }

    /// <summary>Réarme l'envoi quand une nouvelle session est attachée.</summary>
    public void ReinitialiserSession() => SessionMorte = false;

    /// <summary>Déplace le personnage vers une carte adjacente (si une direction est donnée).</summary>
    public async Task SeDeplacerVersCarteAsync(string idCarte, string? direction, CancellationToken ct)
    {
        Journaliseur.Debogue($"API.SeDeplacerVersCarte {idCarte} dir={direction ?? "-"}");
        // TODO : utiliser map_coordinates.json pour déterminer la transition,
        //        marcher jusqu'à la case de bord puis envoyer GA (déplacement).
        _ = idCarte; _ = direction;
        await Task.Delay(200, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Déplace le personnage vers une cellule précise sur la carte courante.
    /// Utilise le pathfinder A* pour calculer le chemin et envoie un packet GA001 au serveur.
    /// Retourne true si le packet a été envoyé, false si pas de chemin trouvé ou pré-conditions non remplies.
    /// </summary>
    public async Task<bool> SeDeplacerVersCelluleAsync(int celluleCible, CancellationToken ct = default, bool arreterDevant = false)
    {
        // Anti-flood : si un déplacement est déjà en cours (marche + GKK0 pas
        // encore terminés), on IGNORE cet appel au lieu de spammer le serveur.
        if (!await _verrouDeplacement.WaitAsync(0, ct).ConfigureAwait(false))
        {
            Journaliseur.Debogue("API.SeDeplacerVersCellule : déplacement déjà en cours, ignoré.");
            return false;
        }
        try
        {
        if (_session is null && _clientAuto is not { EstEnJeu: true })
        {
            Journaliseur.Avertir("API.SeDeplacerVersCellule : pas de session active");
            return false;
        }
        if (_etat.CarteCourante == null)
        {
            Journaliseur.Avertir("API.SeDeplacerVersCellule : carte non chargée");
            return false;
        }
        if (_etat.Personnage.CellulePosition == null)
        {
            Journaliseur.Avertir("API.SeDeplacerVersCellule : position perso inconnue");
            return false;
        }

        // ===== DÉPLACEMENT SEGMENTÉ ====================================
        // Le serveur TRONQUE les longs chemins A* (>~6 cases) : il refait son
        // propre routage et s'arrête en chemin → la cellule de sortie n'est
        // jamais atteinte, le GDM ne part pas. PREUVE log : chemin[10] tronqué
        // à mi-route, chemin[2] toujours OK. On envoie donc le trajet en
        // PETITS SAUTS (≤ Kseg cases), en RE-PATHFINDANT depuis la VRAIE
        // position serveur à chaque saut (robuste au clamp/troncature, aux
        // mobs qui bougent), jusqu'à atteindre la cible ou changer de carte.
        const int Kseg = 4;          // sauts courts = jamais tronqués
        const int maxSeg = 18;
        int mapAvant = _etat.Personnage.CarteCourante ?? 0;
        bool aBouge = false;

        for (int iter = 0; iter < maxSeg && !ct.IsCancellationRequested; iter++)
        {
            if ((_etat.Personnage.CarteCourante ?? 0) != mapAvant)
                break; // transition franchie → fini

            var carte = _etat.CarteCourante;
            if (carte == null) break;
            if (_etat.Personnage.CellulePosition is not int posCur)
            { await Task.Delay(300, ct).ConfigureAwait(false); continue; }
            if (posCur == celluleCible) { aBouge = true; break; }

            var dep = carte.Obtenir(posCur);
            var arr = carte.Obtenir(celluleCible);
            if (dep == null || arr == null)
            {
                if (iter == 0)
                    Journaliseur.Avertir($"API.SeDeplacerVersCellule : depart {posCur} ou arrivée {celluleCible} hors map");
                break;
            }

            // Cases occupées (mobs/joueurs/PNJ) → interdites au pathfinder,
            // sinon l'A* passe « à travers » et le serveur tronque.
            var occ = new List<Cellule>();
            foreach (var ent in carte.Entites.Values)
            {
                if (ent.CellulePosition == dep.Identifiant
                    || ent.CellulePosition == arr.Identifiant) continue;
                var co = carte.Obtenir(ent.CellulePosition);
                if (co != null) occ.Add(co);
            }

            var full = Pathfinder.Trouver(carte, dep, arr,
                cellulesInterdites: occ,
                arreterDevant: arreterDevant, distanceArret: 1);
            if (full == null || full.Count < 2)
            {
                if (iter == 0)
                    Journaliseur.Avertir($"API.SeDeplacerVersCellule : aucun chemin {dep.Identifiant} → {celluleCible}"
                        + (arreterDevant ? " (approche)" : ""));
                break;
            }

            int take = Math.Min(Kseg + 1, full.Count); // dep inclus
            var sub = full.GetRange(0, take);
            bool dernier = take == full.Count;          // ce saut atteint la cible
            bool versTransition = dernier
                && arr.Type == BotDofus.Divers.Cartes.TypesCellule.Transition;

            var paquet = Pathfinder.PaquetDeplacement(sub);
            Journaliseur.Info($"API.SeDeplacerVersCellule : saut {iter} {dep.Identifiant}→{celluleCible} "
                + $"sous[{sub.Count}]={string.Join(">", sub.ConvertAll(c => c.Identifiant))} {paquet}");
            await EnvoyerHumaniseAsync(paquet, ct).ConfigureAwait(false);

            // Transition = GKK0 APRÈS la vraie marche (sinon pas de GDM) ;
            // saut interne = court/snappy.
            int duree = versTransition
                ? Math.Clamp(sub.Count * 450, 1100, 6000)
                : Math.Clamp((sub.Count - 1) * 200, 250, 1400);
            await Task.Delay(duree, ct).ConfigureAwait(false);
            await EnvoyerHumaniseAsync("GKK0", ct).ConfigureAwait(false);
            await Task.Delay(350, ct).ConfigureAwait(false); // GA0 / GDM

            // Anti-blocage : aucun progrès après ce saut (serveur refuse) →
            // on arrête (sinon boucle infinie).
            if ((_etat.Personnage.CarteCourante ?? 0) == mapAvant
                && (_etat.Personnage.CellulePosition ?? -1) == posCur)
            {
                Journaliseur.Avertir($"API.SeDeplacerVersCellule : saut sans progrès (pos {posCur}) — arrêt.");
                break;
            }
            aBouge = true;
        }

        bool carteChangee = (_etat.Personnage.CarteCourante ?? 0) != mapAvant;
        Journaliseur.Info($"API.SeDeplacerVersCellule : fini pos={_etat.Personnage.CellulePosition} "
            + (carteChangee ? "carte CHANGÉE" : "carte idem"));
        return carteChangee
               || _etat.Personnage.CellulePosition == celluleCible
               || aBouge;
        }
        finally { _verrouDeplacement.Release(); }
    }

    /// <summary>
    /// Engage UN groupe précis (clic carte « Combattre ce groupe ») : on
    /// APPROCHE d'abord (GA001+GKK0, arreterDevant) PUIS GA907&lt;cell&gt;;&lt;id&gt;.
    /// GA907 de loin est ignoré par le serveur (prouvé) → l'approche est
    /// indispensable, comme dans le farm. Réutilisable depuis l'UI carte.
    /// </summary>
    public async Task EngagerGroupeAsync(int cellule, int idGroupe, CancellationToken ct = default)
    {
        // === Engage DIRECT (style SynFus / vrai client) ===
        // Capture réelle 11:48 : le client envoie GA001<path> PUIS GA907
        // IMMÉDIATEMENT (sans attendre la marche, sans GKK0 entre). Le
        // SERVEUR fait marcher le perso puis lance le combat. On reproduit :
        // pathfind (arrêt à 1 case) → GA001 → GA907 tout de suite → GKK0
        // après. Plus de « marche puis pause puis engage » visible.
        if (_etat.CarteCourante == null || _etat.Personnage.CellulePosition == null)
        {
            await EnvoyerHumaniseAsync($"GA907{cellule};{idGroupe}", ct).ConfigureAwait(false);
            return;
        }
        var dep = _etat.CarteCourante.Obtenir(_etat.Personnage.CellulePosition.Value);
        var arr = _etat.CarteCourante.Obtenir(cellule);
        string? paquet = null;
        int cases = 0;
        if (dep != null && arr != null)
        {
            var chemin = Pathfinder.Trouver(_etat.CarteCourante, dep, arr,
                arreterDevant: true, distanceArret: 1);
            if (chemin is { Count: >= 2 })
            {
                paquet = Pathfinder.PaquetDeplacement(chemin);
                cases = chemin.Count;
            }
        }
        Journaliseur.Info($"[UI] Engage DIRECT groupe #{idGroupe} cell {cellule} "
            + (paquet != null ? $"(GA001 {cases} cases + GA907)" : "(GA907 seul, pas de chemin)"));
        if (paquet != null)
        {
            await EnvoyerHumaniseAsync(paquet, ct).ConfigureAwait(false);
            await Task.Delay(120, ct).ConfigureAwait(false); // GA001 puis GA907 collés
        }
        await EnvoyerHumaniseAsync($"GA907{cellule};{idGroupe}", ct).ConfigureAwait(false);
        // GKK0 après la durée de marche (le serveur a fait marcher le perso).
        if (paquet != null)
        {
            await Task.Delay(Math.Clamp((cases - 1) * 180, 250, 3000), ct).ConfigureAwait(false);
            await EnvoyerHumaniseAsync("GKK0", ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Parle à un PNJ depuis la carte. FORMAT RÉEL capturé du vrai client
    /// (log 15:05:41) : <c>DC&lt;idPerso&gt;,&lt;idPnj&gt;</c> (ex.
    /// <c>DC401770,-6</c>). L'ancien <c>DC&lt;idPnj&gt;</c> recevait bien
    /// <c>DCK</c> mais le serveur n'envoyait JAMAIS la question (<c>DQ</c>)
    /// car la requête était mal formée → « le pnj ça fonctionne pas ».
    /// Pas de déplacement (le dialogue Retro n'exige pas la proximité).
    /// </summary>
    /// <summary>
    /// Rejoue VERBATIM un GA001 capturé à la main pendant l'enregistrement
    /// (Road Creator). Le chemin est déjà encodé et a été accepté par le
    /// serveur lors de la capture → pas de pathfinder, pas de rollback. On
    /// reproduit la séquence du vrai client : GA001&lt;path&gt; puis, après la
    /// marche, GKK0 (ack fin de déplacement).
    /// </summary>
    public async Task RejouerCheminBrutAsync(string ga001, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ga001)
            || !ga001.StartsWith("GA001", StringComparison.Ordinal))
        {
            Journaliseur.Avertir($"[ANKA] chemin brut invalide : '{ga001}'");
            return;
        }
        // Durée de marche ≈ nb de pas (2 chars/pas après "GA001"), bornée.
        // IMPORTANT : à l'enregistrement le vrai client envoie GKK0 ~2,76 s
        // après le GA001 (marche réelle terminée) → le serveur enregistre le
        // franchissement de bord et déclenche GDM. Si on envoie GKK0 trop tôt
        // (~1 s), le serveur replace le perso SANS changer de carte
        // (« bug de la TP bizarre »). On colle donc au vrai client :
        // ~400 ms/pas, plafond 6 s.
        int pas = Math.Max(1, (ga001.Length - 5) / 2);
        int dureeMarcheMs = Math.Clamp(pas * 400, 400, 6000);
        Journaliseur.Info($"[ANKA] Rejeu chemin brut « {ga001} » (~{pas} pas, {dureeMarcheMs} ms)");
        await EnvoyerHumaniseAsync(ga001, ct).ConfigureAwait(false);
        await Task.Delay(dureeMarcheMs, ct).ConfigureAwait(false);
        await EnvoyerHumaniseAsync("GKK0", ct).ConfigureAwait(false);
        await Task.Delay(250, ct).ConfigureAwait(false);
    }

    public async Task ParlerPnjAsync(int cellule, int idPnj, CancellationToken ct = default)
    {
        // Le DC du serveur attend l'id CONTEXTUEL (négatif, propre à la carte).
        // Les scripts AnkaBot/RoadCreator stockent le GABARIT (positif, stable).
        // Si on reçoit un gabarit, on le re-résout vers l'entité présente sur
        // la carte courante (IdGabarit → Identifiant contextuel).
        int idEnvoye = idPnj;
        if (idPnj >= 0)
        {
            var pnj = _etat.CarteCourante?.Entites.Values
                .OfType<BotDofus.Divers.Cartes.Entites.EntitePNJ>()
                .FirstOrDefault(p => p.IdGabarit == idPnj);
            if (pnj != null) idEnvoye = pnj.Identifiant;
            else Journaliseur.Avertir(
                $"[UI] PNJ gabarit {idPnj} introuvable sur la carte — envoi tel quel.");
        }
        var paquet = $"DC{_etat.Personnage.Identifiant},{idEnvoye}";
        Journaliseur.Info($"[UI] Parler PNJ gabarit#{idPnj} → ctx {idEnvoye} (cell {cellule}) → {paquet}");
        await EnvoyerHumaniseAsync(paquet, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Choisit une réponse dans le dialogue PNJ. FORMAT RÉEL capturé du vrai
    /// client (log 15:28:19) : <c>DR&lt;questionId&gt;|&lt;idReponse&gt;</c>
    /// (ex. <c>DR3731|3352</c>). L'ancien <c>DR&lt;idReponse&gt;</c> (sans la
    /// question) était ignoré du serveur → « impossible de cliquer / le
    /// dialogue n'avance pas ».
    /// </summary>
    public async Task RepondreDialogueAsync(int questionId, int idReponse, CancellationToken ct = default)
    {
        var paquet = questionId > 0 ? $"DR{questionId}|{idReponse}" : $"DR{idReponse}";
        Journaliseur.Info($"[UI] Réponse dialogue → {paquet}");
        await EnvoyerHumaniseAsync(paquet, ct).ConfigureAwait(false);
    }

    /// <summary>Quitte le dialogue PNJ en cours : <c>DV</c>.</summary>
    public async Task QuitterDialogueAsync(CancellationToken ct = default)
    {
        Journaliseur.Info("[UI] Quitter dialogue → DV");
        await EnvoyerHumaniseAsync("DV", ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Change de map dans une direction (sortie/« soleil ») : marche sur la
    /// cellule de TRANSITION la plus extrême du côté demandé. Le pathfinder
    /// autorise la transition comme destination → marcher dessus change la
    /// map côté serveur. Directions : "ouest","est","nord","sud".
    /// Réutilisable depuis les scripts (bot.changerMap("est")).
    /// </summary>
    public async Task<bool> ChangerMapDirectionAsync(string direction, CancellationToken ct = default)
    {
        var carte = _etat.CarteCourante;
        if (carte == null) { Journaliseur.Avertir("[MAP] pas de carte courante"); return false; }
        var transitions = carte.Cellules
            .OfType<Cellule>()
            .Where(c => c.Type == BotDofus.Divers.Cartes.TypesCellule.Transition)
            .ToList();
        if (transitions.Count == 0)
        {
            Journaliseur.Avertir($"[MAP] aucune sortie (transition) sur cette carte pour « {direction} »");
            return false;
        }
        // Projection iso écran : sx = x - y (horizontal), sy = x + y (vertical).
        // Clé de tri « côté » : plus c'est petit, plus c'est du bon côté.
        Func<Cellule, int>? ordreCote = direction.ToLowerInvariant() switch
        {
            "est" or "droite" => c => -(c.X - c.Y),
            "ouest" or "gauche" => c => (c.X - c.Y),
            "sud" or "bas" => c => -(c.X + c.Y),
            "nord" or "haut" => c => (c.X + c.Y),
            _ => null
        };
        if (ordreCote == null)
        {
            Journaliseur.Avertir($"[MAP] direction inconnue « {direction} »");
            return false;
        }

        // Une seule transition « la plus extrême » peut être inatteignable
        // (bloquée par un mob, sur un îlot…). On TENTE plusieurs sorties du
        // bon côté : la plus extrême d'abord, puis à extrême ≈ égal la plus
        // proche du perso. On s'arrête dès que la CARTE a changé (GDM). C'est
        // ça la sortie « globale » fiable depuis n'importe où sur la carte.
        var dep = _etat.Personnage.CellulePosition is int p
            ? carte.Obtenir(p) : null;

        // ÉQUATIONS DE BORDURE du modèle dyshay (Movimiento.get_Puede_Cambiar_Mapa).
        // Notre Cellule.X/Y suit EXACTEMENT la convention dyshay Cell.cs, donc
        // ces prédicats identifient les VRAIES cellules de sortie d'un côté
        // (≠ tri « extrémité » approximatif). Si une carte a des bordures
        // irrégulières et qu'aucune transition ne matche, on retombe sur
        // l'ancien comportement (tri par extrémité). 27 = mapWidth*2-1 (14),
        // 31 = constante BOTTOM standard Retro.
        Func<Cellule, bool> estBordure = direction.ToLowerInvariant() switch
        {
            "est" or "droite" => c => (c.X - 27) == c.Y,
            "ouest" or "gauche" => c => (c.X - 1) == c.Y,
            "sud" or "bas" => c => (c.X + c.Y) == 31,
            "nord" or "haut" => c => c.Y < 0 && (c.X - Math.Abs(c.Y)) == 1,
            _ => _ => false
        };
        Func<Cellule, double> distDep = c => dep == null ? 0
            : (c.X - dep.X) * (c.X - dep.X) + (c.Y - dep.Y) * (c.Y - dep.Y);

        var bordures = transitions.Where(estBordure)
            .OrderBy(distDep).ToList();
        IEnumerable<Cellule> ordonnees = bordures.Count > 0
            ? bordures
            : transitions.OrderBy(ordreCote).ThenBy(distDep);
        var candidats = ordonnees.Take(4).ToList();
        if (bordures.Count > 0)
            Journaliseur.Info($"[MAP] « {direction} » : {bordures.Count} "
                + "cellule(s) de bordure exacte (équations dyshay).");
        int mapAvant = _etat.Personnage.CarteCourante ?? 0;

        foreach (var cible in candidats)
        {
            if (ct.IsCancellationRequested) return false;
            Journaliseur.Info($"[MAP] Sortie « {direction} » → essai cellule "
                + $"{cible.Identifiant} ({cible.X},{cible.Y})");
            await SeDeplacerVersCelluleAsync(cible.Identifiant, ct)
                .ConfigureAwait(false);
            await Task.Delay(400, ct).ConfigureAwait(false); // laisse venir le GDM
            if ((_etat.Personnage.CarteCourante ?? 0) != mapAvant)
            {
                Journaliseur.Info($"[MAP] Sortie « {direction} » OK → carte "
                    + $"{_etat.Personnage.CarteCourante}");
                return true;
            }
            Journaliseur.Avertir($"[MAP] cellule {cible.Identifiant} n'a pas "
                + "changé la carte — essai suivant…");
        }
        Journaliseur.Avertir($"[MAP] Sortie « {direction} » : aucune des "
            + $"{candidats.Count} transitions testées n'a changé la carte.");
        return false;
    }

    /// <summary>
    /// Sortie « intelligente » vers une cellule enregistrée.
    ///
    /// PRIORITÉ À LA CELLULE EXACTE : une carte peut avoir PLUSIEURS sorties
    /// du même côté menant à des cartes DIFFÉRENTES (ex. 10302 : cell 327 →
    /// 10354, cell 458 → 10338, toutes deux « sud »). Déduire une simple
    /// direction est donc AMBIGU et envoyait vers la mauvaise carte. On vise
    /// donc la cellule de transition EXACTE enregistrée (destination
    /// déterministe) ; le pathfinder évite déjà les mobs et le GKK0 long
    /// (case transition) déclenche le GDM → fiable depuis n'importe où.
    ///
    /// La sortie par DIRECTION ne sert plus que de SECOURS si la cellule
    /// exacte n'a pas changé la carte (cellule injoignable / entrée ailleurs).
    /// </summary>
    public async Task<bool> SortirCarteAsync(int celluleCible, CancellationToken ct = default)
    {
        var carte = _etat.CarteCourante;
        if (carte == null)
        {
            Journaliseur.Avertir("[MAP] SortirCarte : pas de carte courante");
            return false;
        }
        var c = carte.Obtenir(celluleCible);
        bool estTransition = c != null
            && c.Type == BotDofus.Divers.Cartes.TypesCellule.Transition;
        int mapAvant = _etat.Personnage.CarteCourante ?? 0;

        // 1) Cellule EXACTE (sortie déterministe).
        await SeDeplacerVersCelluleAsync(celluleCible, ct).ConfigureAwait(false);
        await Task.Delay(400, ct).ConfigureAwait(false); // laisse venir le GDM
        if (!estTransition || (_etat.Personnage.CarteCourante ?? 0) != mapAvant)
            return (_etat.Personnage.CarteCourante ?? 0) != mapAvant
                   || !estTransition;

        // 2) SECOURS uniquement : la cellule exacte n'a pas fait changer de
        //    carte → on tente la sortie par direction déduite de cette case.
        var trans = carte.Cellules
            .OfType<Cellule>()
            .Where(t => t.Type == BotDofus.Divers.Cartes.TypesCellule.Transition)
            .ToList();
        if (trans.Count == 0 || c == null) return false;
        double maxXmY = trans.Max(t => t.X - t.Y);
        double minXmY = trans.Min(t => t.X - t.Y);
        double maxXpY = trans.Max(t => t.X + t.Y);
        double minXpY = trans.Min(t => t.X + t.Y);
        double dE = maxXmY - (c.X - c.Y);
        double dO = (c.X - c.Y) - minXmY;
        double dS = maxXpY - (c.X + c.Y);
        double dN = (c.X + c.Y) - minXpY;
        double m = Math.Min(Math.Min(dE, dO), Math.Min(dS, dN));
        string dir = m == dE ? "est" : m == dO ? "ouest"
                   : m == dS ? "sud" : "nord";
        Journaliseur.Avertir($"[MAP] cellule {celluleCible} n'a pas changé "
            + $"la carte → SECOURS sortie par direction « {dir} ».");
        return await ChangerMapDirectionAsync(dir, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Récolte un élément interactif (arbre, minerai, blé…) : approche au
    /// contact puis envoie le VRAI paquet d'interaction capturé en jeu manuel :
    /// <c>GA500&lt;cellule&gt;;&lt;skillId&gt;</c> (capture : <c>GA500168;45</c>,
    /// 45 = « Faucher », blé niv. 1). Le <paramref name="skillId"/> dépend du
    /// type de ressource (Couper=bois, Faucher=blé, Cueillir=plantes,
    /// Pêcher=poisson…) ; faute de base d'objets interactifs (le JSON Hystoria
    /// est vide), on prend par défaut 45 (Faucher) et on permet de surcharger.
    /// </summary>
    public async Task RecolterAsync(int cellule, int idInteractif, int skillId = 45, CancellationToken ct = default)
    {
        // SÉQUENCE RÉELLE capturée d'une récolte manuelle qui FONCTIONNE
        // (log 14:43:51, ressource cell 211) :
        //   GA001<chemin>        ← le client pathfind jusqu'à une case adjacente
        //   GA500<cellObj>;<sk>  ← ENVOYÉ IMMÉDIATEMENT, collé au GA001
        //   (serveur ACK déplacement)
        //   GKK0                 ← ~0.7 s après, fin de déplacement
        //   (serveur GA0 « 211,11800,201 » = résultat récolte, puis GDF/OQ/IQ)
        // L'ancien code envoyait GA001 → attente → GKK0 → attente → GA500 :
        // le GA500 arrivait trop tard, hors contexte d'interaction → serveur
        // muet (« rien ne se passe depuis le bot »). On colle donc GA001+GA500
        // exactement comme le combat direct (GA001+GA907).
        var paquet = $"GA500{cellule};{skillId}";
        int dureeMarche = 0;

        if (_etat.CarteCourante != null && _etat.Personnage.CellulePosition is int pc)
        {
            var dep = _etat.CarteCourante.Obtenir(pc);
            var arr = _etat.CarteCourante.Obtenir(cellule);
            if (dep != null && arr != null)
            {
                var chemin = Pathfinder.Trouver(_etat.CarteCourante, dep, arr,
                    arreterDevant: true, distanceArret: 1);
                if (chemin is { Count: >= 2 })
                {
                    await EnvoyerHumaniseAsync(Pathfinder.PaquetDeplacement(chemin), ct).ConfigureAwait(false);
                    dureeMarche = Math.Clamp((chemin.Count - 1) * 180, 250, 3000);
                }
            }
        }

        Journaliseur.Info($"[UI] Récolte cell {cellule} objet #{idInteractif} : "
            + $"GA001+« {paquet} » collés (skill {skillId}). Si la ressource n'est "
            + "pas du blé, le skill diffère (Couper=bois, Cueillir=plantes, "
            + "Pêcher=poisson) : surcharge skillId.");

        // GA500 collé au GA001 (pas d'attente entre les deux).
        await EnvoyerHumaniseAsync(paquet, ct).ConfigureAwait(false);

        // Puis on laisse marcher et on acquitte la fin de déplacement.
        if (dureeMarche > 0)
        {
            await Task.Delay(dureeMarche, ct).ConfigureAwait(false);
            await EnvoyerHumaniseAsync("GKK0", ct).ConfigureAwait(false);
        }
    }

    /// <summary>Ouvre un dialogue avec un PNJ, puis enchaîne les réponses indiquées.</summary>
    public async Task ParlerAuPNJAsync(int idPNJ, IReadOnlyList<int>? reponses, CancellationToken ct)
    {
        if (_session is null) return;
        Journaliseur.Debogue($"API.ParlerAuPNJ #{idPNJ}");

        await EnvoyerHumaniseAsync(new MessageDialogueDebuter { IdentifiantPNJ = idPNJ }.Serialiser(), ct).ConfigureAwait(false);

        if (reponses is null) return;
        foreach (var reponse in reponses)
        {
            var choix = reponse == -1 ? 1 : reponse;
            await EnvoyerHumaniseAsync(new MessageDialogueReponse { IdentifiantReponse = choix }.Serialiser(), ct).ConfigureAwait(false);
        }
    }

    /// <summary>Groupe de monstres le plus proche du perso sur la carte courante.</summary>
    public EntiteMonstre? MonstreLePlusProche()
    {
        var carte = _etat.CarteCourante;
        if (carte == null) return null;
        int moi = _etat.Personnage.CellulePosition ?? 0;
        return carte.Entites.Values.OfType<EntiteMonstre>()
            .OrderBy(m => Math.Abs(m.CellulePosition - moi))
            .FirstOrDefault();
    }

    /// <summary>
    /// Engage le groupe de monstres le plus proche. Format RÉEL capturé du
    /// vrai client (log 11:24:15, '-' déchiffré) : <c>GA907&lt;cellGroupe&gt;;&lt;idGroupe&gt;</c>
    /// (ex. <c>GA907288;-78</c>). L'ancien <c>GA902&lt;id&gt;</c> était une
    /// supposition fausse → serveur muet. Opcode 'GA' ⇒ chiffré '-'.
    /// </summary>
    public async Task<bool> EngagerCombatAsync(CancellationToken ct)
    {
        var cible = MonstreLePlusProche();
        if (cible == null) { Journaliseur.Info("[FARM] aucun monstre sur la carte."); return false; }
        var paquet = $"GA907{cible.CellulePosition};{cible.Identifiant}";
        Journaliseur.Info(
            $"[FARM] cible groupe #{cible.Identifiant} « {cible.Nom} » cell {cible.CellulePosition} → {paquet}");
        await EnvoyerHumaniseAsync(paquet, ct).ConfigureAwait(false);
        return true;
    }

    // ===================== CARACS & SORTS ============================

    /// <summary>Monte une caractéristique : <c>AB&lt;id&gt;;&lt;statId&gt;;&lt;n&gt;</c>
    /// (clair, core.swf BonusStats). statId 10=Vita 11=Sag 12=For 13=Int
    /// 14=Cha 15=Agi.</summary>
    public async Task MonterCaracteristiqueAsync(int statId, int n = 1, CancellationToken ct = default)
    {
        if (_etat.Personnage.Identifiant == 0 || n <= 0) return;
        Journaliseur.Info($"[STATS] AB stat {statId} +{n} (capital {_etat.Personnage.PointsCaracteristiques}).");
        await EnvoyerHumaniseAsync($"AB{_etat.Personnage.Identifiant};{statId};{n}", ct).ConfigureAwait(false);
    }

    /// <summary>Monte un sort : <c>SB&lt;id&gt;;&lt;spellId&gt;</c>
    /// (chiffré '-', core.swf Spells.as).</summary>
    public async Task MonterSortAsync(int spellId, CancellationToken ct = default)
    {
        if (_etat.Personnage.Identifiant == 0) return;
        Journaliseur.Info($"[SORTS] SB sort {spellId} (pts sorts {_etat.Personnage.PointsSorts}).");
        await EnvoyerHumaniseAsync($"SB{_etat.Personnage.Identifiant};{spellId}", ct).ConfigureAwait(false);
    }

    /// <summary>Dépense TOUT le capital dans une stat (1 pt à la fois, le
    /// serveur renvoie As → on s'arrête quand le capital est épuisé).</summary>
    public async Task AutoDistribuerCaracteristiquesAsync(int statId, CancellationToken ct = default)
    {
        Journaliseur.Info($"[STATS] Auto-distribution capital → {BotDofus.Divers.Jeu.Personnage.Personnage.NomsCaracteristiques.GetValueOrDefault(statId, statId.ToString())}.");
        int garde = 0;
        int capitalAvant = _etat.Personnage.PointsCaracteristiques;
        int sansProgres = 0;
        while (!ct.IsCancellationRequested && !SessionMorte
               && _etat.Personnage.PointsCaracteristiques > 0 && garde++ < 500)
        {
            await MonterCaracteristiqueAsync(statId, 1, ct).ConfigureAwait(false);
            await Task.Delay(450, ct).ConfigureAwait(false); // laisse le As revenir

            // Garde-fou anti-spam : si après plusieurs envois le capital n'a
            // PAS bougé, le serveur n'applique pas (mauvais canal/format/zone)
            // → on arrête au lieu de marteler 500 fois pour rien.
            if (_etat.Personnage.PointsCaracteristiques >= capitalAvant)
            {
                if (++sansProgres >= 5)
                {
                    Journaliseur.Avertir(
                        "[STATS] Capital inchangé après 5 essais — le serveur "
                        + "n'applique pas (montée impossible ici ?). Arrêt.");
                    break;
                }
            }
            else { sansProgres = 0; capitalAvant = _etat.Personnage.PointsCaracteristiques; }
        }
        Journaliseur.Info($"[STATS] Auto-distribution terminée (capital restant {_etat.Personnage.PointsCaracteristiques}).");
    }

    /// <summary>Monte tous les sorts connus tant qu'il reste des points de
    /// sort (1 passe par sort, re-check via SL/As).</summary>
    public async Task AutoMonterSortsAsync(CancellationToken ct = default)
    {
        Journaliseur.Info($"[SORTS] Auto-montée sorts (pts {_etat.Personnage.PointsSorts}).");
        int garde = 0;
        while (!ct.IsCancellationRequested && !SessionMorte && _etat.Personnage.PointsSorts > 0 && garde++ < 200)
        {
            var sorts = _etat.Personnage.SortsAppris.Keys.ToList();
            if (sorts.Count == 0) break;
            foreach (var sid in sorts)
            {
                if (ct.IsCancellationRequested || _etat.Personnage.PointsSorts <= 0) break;
                await MonterSortAsync(sid, ct).ConfigureAwait(false);
                await Task.Delay(450, ct).ConfigureAwait(false);
            }
        }
        Journaliseur.Info($"[SORTS] Auto-montée terminée (pts restants {_etat.Personnage.PointsSorts}).");
    }

    private CancellationTokenSource? _farmCts;
    public bool FarmActif => _farmCts is { IsCancellationRequested: false };

    /// <summary>Démarre la boucle de farm autonome (idempotent).</summary>
    public void LancerFarmAuto()
    {
        if (FarmActif) return;
        _farmCts = new CancellationTokenSource();
        _ = BoucleFarmAsync(_farmCts.Token);
    }

    public void ArreterFarmAuto()
    {
        _farmCts?.Cancel();
        _farmCts = null;
        Journaliseur.Info("[FARM] arrêt demandé.");
    }

    /// <summary>
    /// Boucle de farm : hors combat → cible le mob le plus proche, s'en
    /// approche (déplacement chiffré GA001) puis engage (GA902). En combat,
    /// l'auto-combat (GR1/GT, ContexteCompte) prend le relais. Loot auto
    /// (serveur). Tout passe par le client autonome (clair/chiffré whitelist).
    /// </summary>
    private async Task BoucleFarmAsync(CancellationToken ct)
    {
        Journaliseur.Info("[FARM] boucle autonome DÉMARRÉE.");
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // GATE : ne rien faire tant que le client autonome n'est pas
                // réellement EN JEU (sinon on agit sur des données périmées /
                // un socket fermé → spam d'erreurs, cf. test 09:05).
                if (_clientAuto is { EstEnJeu: true } || _session is not null)
                {
                    // ok : soit client autonome en jeu, soit mode MITM
                }
                else
                {
                    Journaliseur.Info("[FARM] en attente : client autonome pas encore EN JEU…");
                    await Task.Delay(3000, ct).ConfigureAwait(false);
                    continue;
                }

                if (_etat.Combat.Etat != EtatCombat.Inactif)
                {
                    await Task.Delay(2000, ct).ConfigureAwait(false);
                    continue;
                }

                var mob = MonstreLePlusProche();
                if (mob == null)
                {
                    Journaliseur.Info("[FARM] pas de monstre — attente (carte vide / repop).");
                    await Task.Delay(5000, ct).ConfigureAwait(false);
                    continue;
                }

                // Engage DIRECT (GA001+GA907 collés, le serveur marche puis
                // lance le combat) — flux SynFus, plus de pause visible.
                int dist = DistanceCarte(_etat.Personnage.CellulePosition, mob.CellulePosition);
                Journaliseur.Info($"[FARM] cible #{mob.Identifiant} « {mob.Nom} » cell {mob.CellulePosition} "
                    + $"(perso {_etat.Personnage.CellulePosition}, dist {dist}) → engage direct");

                await EngagerGroupeAsync(mob.CellulePosition, mob.Identifiant, ct).ConfigureAwait(false);
                await Task.Delay(2500, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[FARM] {ex.Message}");
                try { await Task.Delay(3000, ct).ConfigureAwait(false); } catch { break; }
            }
        }
        Journaliseur.Info("[FARM] boucle autonome ARRÊTÉE.");
    }

    // ===================== RÉCOLTE AUTO EN BOUCLE ====================

    private CancellationTokenSource? _recolteCts;
    public bool RecolteActive => _recolteCts is { IsCancellationRequested: false };

    /// <summary>Démarre la récolte autonome (idempotent).</summary>
    public void LancerRecolteAuto()
    {
        if (RecolteActive) return;
        _recolteCts = new CancellationTokenSource();
        _ = BoucleRecolteAsync(_recolteCts.Token);
    }

    public void ArreterRecolteAuto()
    {
        _recolteCts?.Cancel();
        _recolteCts = null;
        Journaliseur.Info("[RÉCOLTE] arrêt demandé.");
    }

    /// <summary>
    /// Cellules récoltables PAR CE PERSO sur la carte courante : interactif
    /// présent, ressource dispo (GDF), gfx connu en BDD ET skill dans les
    /// métiers du perso (JSK). Triées par distance réelle au perso.
    /// </summary>
    // Cellules récemment récoltées : on les ignore le temps que le serveur
    // confirme l'épuisement (GDF arrive ~10-15 s APRÈS le GA500, bien après
    // notre attente → sans ça on récoltait 2× la même case, log 15:53→15:54).
    private readonly Dictionary<int, DateTime> _recolteCooldown = new();
    private static readonly TimeSpan CooldownRecolte = TimeSpan.FromSeconds(25);

    private List<Cellule> CellulesRecoltables()
    {
        var carte = _etat.CarteCourante;
        if (carte == null) return new List<Cellule>();
        var bdd = Divers.Donnees.BaseDonnees.Instance;
        var skills = _etat.Personnage.SkillsConnus;
        var moi = _etat.Personnage.CellulePosition;
        var maintenant = DateTime.UtcNow;

        var liste = new List<Cellule>();
        foreach (var c in carte.Cellules)
        {
            if (c is not { IdInteractif: >= 0, RessourceDisponible: true }) continue;
            if (_recolteCooldown.TryGetValue(c.Identifiant, out var jusqua)
                && jusqua > maintenant) continue;                     // récoltée récemment
            var io = bdd.Interactif(c.IdInteractif);
            if (io is not { IdSkill: > 0 }) continue;                 // gfx pas encore appris
            if (skills.Count > 0 && !skills.Contains(io.IdSkill)) continue; // pas le métier
            liste.Add(c);
        }
        liste.Sort((a, b) => DistanceCarte(moi, a.Identifiant)
            .CompareTo(DistanceCarte(moi, b.Identifiant)));
        return liste;
    }

    /// <summary>
    /// Boucle de récolte : récolte toutes les ressources exploitables de la
    /// carte (couleur verte « récoltable »), puis change de map (sortie en
    /// cyclant les 4 directions) et recommence. Annulable.
    /// </summary>
    private async Task BoucleRecolteAsync(CancellationToken ct)
    {
        Journaliseur.Info("[RÉCOLTE] boucle autonome DÉMARRÉE.");
        var directions = new[] { "est", "sud", "ouest", "nord" };
        int dirIdx = 0;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_clientAuto is not { EstEnJeu: true } && _session is null)
                {
                    await Task.Delay(3000, ct).ConfigureAwait(false);
                    continue;
                }
                if (_etat.Combat.Etat != EtatCombat.Inactif)
                {
                    await Task.Delay(2000, ct).ConfigureAwait(false);
                    continue;
                }

                var cibles = CellulesRecoltables();
                if (cibles.Count == 0)
                {
                    var dir = directions[dirIdx++ % directions.Length];
                    Journaliseur.Info($"[RÉCOLTE] rien à récolter ici → sortie « {dir} ».");
                    await ChangerMapDirectionAsync(dir, ct).ConfigureAwait(false);
                    await Task.Delay(3500, ct).ConfigureAwait(false);
                    continue;
                }

                var cible = cibles[0];
                var io = Divers.Donnees.BaseDonnees.Instance.Interactif(cible.IdInteractif);
                int skill = io?.IdSkill ?? 45;
                Journaliseur.Info($"[RÉCOLTE] {cibles.Count} ressource(s) — cible cell "
                    + $"{cible.Identifiant} ({io?.Nom ?? $"#{cible.IdInteractif}"}) skill {skill}.");

                // Cooldown AVANT la récolte : la case ne sera pas re-ciblée
                // tant que le GDF d'épuisement n'est pas revenu (ou 25 s).
                _recolteCooldown[cible.Identifiant] = DateTime.UtcNow + CooldownRecolte;

                await RecolterAsync(cible.Identifiant, cible.IdInteractif, skill, ct)
                    .ConfigureAwait(false);

                // Attend l'épuisement (GDF) de cette cellule, max ~9 s ; dès
                // que le serveur confirme, on libère le cooldown et on enchaîne.
                for (int i = 0; i < 18 && cible.RessourceDisponible
                                       && !ct.IsCancellationRequested; i++)
                    await Task.Delay(500, ct).ConfigureAwait(false);
                if (!cible.RessourceDisponible)
                    _recolteCooldown.Remove(cible.Identifiant);

                await Task.Delay(600, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[RÉCOLTE] {ex.Message}");
                try { await Task.Delay(3000, ct).ConfigureAwait(false); } catch { break; }
            }
        }
        Journaliseur.Info("[RÉCOLTE] boucle autonome ARRÊTÉE.");
    }

    /// <summary>Nombre de ressources récoltables PAR CE PERSO sur la carte courante (pour Lua).</summary>
    public int NbRecoltables() => CellulesRecoltables().Count;

    /// <summary>
    /// Récolte UNE FOIS toutes les ressources exploitables de la carte
    /// courante (snapshot au début), en attendant l'épuisement de chacune.
    /// Renvoie le nombre récolté. Utilisé par les scripts Lua de trajet.
    /// </summary>
    public async Task<int> RecolterToutAsync(CancellationToken ct = default)
    {
        int n = 0;
        foreach (var c in CellulesRecoltables())
        {
            if (ct.IsCancellationRequested) break;
            if (_etat.Combat.Etat != EtatCombat.Inactif) break;
            var io = Divers.Donnees.BaseDonnees.Instance.Interactif(c.IdInteractif);
            int skill = io?.IdSkill ?? 45;
            _recolteCooldown[c.Identifiant] = DateTime.UtcNow + CooldownRecolte;
            await RecolterAsync(c.Identifiant, c.IdInteractif, skill, ct).ConfigureAwait(false);
            for (int i = 0; i < 18 && c.RessourceDisponible
                                   && !ct.IsCancellationRequested; i++)
                await Task.Delay(500, ct).ConfigureAwait(false);
            if (!c.RessourceDisponible) _recolteCooldown.Remove(c.Identifiant);
            n++;
        }
        Journaliseur.Info($"[LUA] recolter_tout : {n} ressource(s) récoltée(s).");
        return n;
    }

    /// <summary>
    /// Distance Chebyshev RÉELLE entre 2 cellules via les coordonnées X/Y
    /// décodées de la carte (≠ approximation id%14). Retourne 99 si la
    /// carte/position est inconnue (force l'approche).
    /// </summary>
    private int DistanceCarte(int? cellA, int cellB)
    {
        var carte = _etat.CarteCourante;
        if (carte == null || cellA == null) return 99;
        var a = carte.Obtenir(cellA.Value);
        var b = carte.Obtenir(cellB);
        if (a == null || b == null) return 99;
        return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }

    /// <summary>Ouvre un dialogue avec le banquier / phénix le plus proche et dépose selon les règles.</summary>
    public async Task UtiliserBanqueAsync(CancellationToken ct)
    {
        Journaliseur.Debogue("API.UtiliserBanque");
        // TODO : résoudre le PNJ banquier, exécuter dialogue jusqu'à l'inventaire,
        //        déposer selon règles de banque/<nomPerso>.json.
        await Task.Delay(300, ct).ConfigureAwait(false);
    }

    /// <summary>Envoie un message sur un canal de chat.</summary>
    public async Task EnvoyerMessageAsync(string canal, string texte, CancellationToken ct = default)
    {
        await EnvoyerHumaniseAsync(new MessageChatEnvoyer
        {
            Canal = canal,
            Texte = texte
        }.Serialiser(), ct).ConfigureAwait(false);
    }

    /// <summary>Envoie la commande serveur Hystoria ".travel x,y".</summary>
    public Task EnvoyerTravelAsync(int x, int y, CancellationToken ct = default)
        => EnvoyerMessageAsync("*", $".travel {x},{y}", ct);

    /// <summary>Envoie un paquet brut au serveur depuis les outils UI / scripts.</summary>
    public async Task EnvoyerPaquetBrutAsync(string paquet, CancellationToken ct = default)
    {
        await EnvoyerHumaniseAsync(paquet.Trim(), ct).ConfigureAwait(false);
    }

    /// <summary>Fin de tour en combat.</summary>
    public async Task FinirTourAsync(CancellationToken ct = default)
    {
        await EnvoyerHumaniseAsync(new MessageJeuFinirTour().Serialiser(), ct).ConfigureAwait(false);
    }

    /// <summary>Placement initial en combat.</summary>
    public async Task SePlacerEnCombatAsync(int celluleDepart, CancellationToken ct = default)
    {
        await EnvoyerHumaniseAsync(new MessageJeuPosition { CaseDepart = celluleDepart }.Serialiser(), ct).ConfigureAwait(false);
        await EnvoyerHumaniseAsync(new MessageJeuPret { Pret = true }.Serialiser(), ct).ConfigureAwait(false);
    }

    /// <summary>Récupère le pseudo du personnage actif (exposable aux scripts Lua).</summary>
    public string ObtenirPseudo() => _compte.PseudoAffiche ?? _compte.Identifiant;

    /// <summary>Récupère les PV actuels / max du personnage (exposable aux scripts Lua).</summary>
    public (int pv, int pvMax) ObtenirVie() => (_etat.Personnage.Vie, _etat.Personnage.VieMax);
}
