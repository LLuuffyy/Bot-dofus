using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BotDofus.Commun.Messages.VersServeur.Chat;
using BotDofus.Commun.Messages.VersServeur.Dialogue;
using BotDofus.Commun.Messages.VersServeur.Jeu;
using BotDofus.Commun.Reseau;
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
    public void LierSession(SessionProxy session) => _session = session;

    /// <summary>Lie l'API au client AUTONOME (architecture SynFus, sans client
    /// officiel). Prioritaire sur la session MITM si présent.</summary>
    public void LierClientAutonome(ClientAutonomeAbrak? client) => _clientAuto = client;

    /// <summary>
    /// Point d'envoi UNIQUE pour TOUT paquet initié par le bot. Passe
    /// systématiquement par la garde anti-burst <see cref="Humaniseur"/> :
    /// en mode passif elle ne fait rien, en mode actif elle impose un délai
    /// humain (jitter) entre deux actions → pas de pattern métronomique
    /// détectable côté serveur. C'EST le point de furtivité de l'examen :
    /// aucune méthode ne doit envoyer au serveur en court-circuitant ceci.
    /// </summary>
    private async Task EnvoyerHumaniseAsync(string paquet, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(paquet)) return;
        await Humaniseur.RespecterCadenceAsync(ct).ConfigureAwait(false);
        // Client autonome prioritaire (route clair/chiffré '-' via whitelist).
        if (_clientAuto is { EstConnecte: true })
            await _clientAuto.EnvoyerAuServeurAsync(paquet, ct).ConfigureAwait(false);
        else if (_session is not null)
            await _session.EnvoyerAuServeurAsync(paquet, ct).ConfigureAwait(false);
    }

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

        var depart = _etat.CarteCourante.Obtenir(_etat.Personnage.CellulePosition.Value);
        var arrivee = _etat.CarteCourante.Obtenir(celluleCible);
        if (depart == null || arrivee == null)
        {
            Journaliseur.Avertir($"API.SeDeplacerVersCellule : depart {_etat.Personnage.CellulePosition} ou arrivée {celluleCible} hors map");
            return false;
        }

        // arreterDevant : pour approcher un monstre, sa cellule est occupée
        // (non marchable) → un chemin « dessus » échoue toujours. On demande
        // au pathfinder de s'arrêter à 1 case de la cible.
        var chemin = Pathfinder.Trouver(_etat.CarteCourante, depart, arrivee,
            arreterDevant: arreterDevant, distanceArret: 1);
        if (chemin == null || chemin.Count < 2)
        {
            Journaliseur.Avertir($"API.SeDeplacerVersCellule : aucun chemin {depart.Identifiant} → {celluleCible}"
                + (arreterDevant ? " (approche)" : ""));
            return false;
        }

        string paquet = Pathfinder.PaquetDeplacement(chemin);
        // Trace décisive : chemin A* complet (ids cellules) + paquet encodé.
        // Permet de comparer à un GA001 du VRAI client sur la même carte pour
        // trancher : bug d'encodage vs marchabilité carte fausse (le serveur
        // renvoie un GA0 no-op « reste sur place » si le chemin est invalide).
        var cellsChemin = string.Join(">", chemin.ConvertAll(c => c.Identifiant));
        Journaliseur.Info($"API.SeDeplacerVersCellule : dep={depart.Identifiant} arr={celluleCible} "
            + $"chemin[{chemin.Count}]={cellsChemin} paquet={paquet}");
        await EnvoyerHumaniseAsync(paquet, ct).ConfigureAwait(false);

        // CONFIRMATION FIN DE DÉPLACEMENT — décisif. Le vrai client envoie
        // « GKK0 » ~1-1,5 s après chaque GA001 (fin d'animation de marche).
        // Le serveur considère le perso « en marche » tant qu'il ne l'a pas
        // reçu et IGNORE toute action suivante (GA902 combat, etc.) → sans
        // ça, en injection, le perso bouge côté serveur mais rien ne se
        // passe ensuite en jeu. Durée ≈ nb de cases × ~300 ms (vitesse course
        // Dofus Retro), bornée. Cf. capture live : GA001df_ → +1,0 s → GKK0.
        // Durée de marche raccourcie : le serveur a déjà traité le GA0 (perso
        // déplacé) ; GKK0 = ack d'arrivée. ~180 ms/case, borné [250, 3000] →
        // approche bien plus directe/snappy (demande utilisateur), sans
        // désync (GKK0 reste après que le serveur ait bougé le perso).
        int dureeMarcheMs = Math.Clamp((chemin.Count - 1) * 180, 250, 3000);
        await Task.Delay(dureeMarcheMs, ct).ConfigureAwait(false);
        await EnvoyerHumaniseAsync("GKK0", ct).ConfigureAwait(false);
        // NE PAS écraser la position locale avec chemin[^1] : le SERVEUR fait
        // foi (GA0 → OnActionJeu met _etat.Personnage.CellulePosition à la
        // VRAIE case d'arrivée, souvent différente du chemin demandé car le
        // serveur clampe/tronque). L'écraser ici créait une désync (le perso
        // se croyait sur la cellule du monstre → « aucun chemin 368→368 » →
        // farm bloqué en boucle, cf. log 11:38-11:39). On laisse un délai
        // pour que le GA0 arrive avant la prochaine action.
        await Task.Delay(250, ct).ConfigureAwait(false);
        Journaliseur.Info($"API.SeDeplacerVersCellule : GA001+GKK0 envoyés (marche {dureeMarcheMs} ms), "
            + $"position réelle via GA0 = cell {_etat.Personnage.CellulePosition}");
        return true;
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
    /// Parle à un PNJ depuis la carte : approche (GA001) puis DC&lt;idPnj&gt;
    /// (canal « - » re-chiffré, n'est plus ignoré). Même logique que le
    /// combat : il faut être au contact. NB : format DC à confirmer sur
    /// capture réelle si le serveur ne répond pas (clic PNJ manuel dans
    /// Dofus.exe → le proxy loggue le vrai paquet déchiffré).
    /// </summary>
    public async Task ParlerPnjAsync(int cellule, int idPnj, CancellationToken ct = default)
    {
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
                    await Task.Delay(Math.Clamp((chemin.Count - 1) * 180, 250, 3000), ct).ConfigureAwait(false);
                    await EnvoyerHumaniseAsync("GKK0", ct).ConfigureAwait(false);
                    await Task.Delay(300, ct).ConfigureAwait(false);
                }
            }
        }
        Journaliseur.Info($"[UI] Parler PNJ #{idPnj} cell {cellule} → DC{idPnj}");
        await EnvoyerHumaniseAsync($"DC{idPnj}", ct).ConfigureAwait(false);
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
        while (!ct.IsCancellationRequested && _etat.Personnage.PointsCaracteristiques > 0 && garde++ < 500)
        {
            await MonterCaracteristiqueAsync(statId, 1, ct).ConfigureAwait(false);
            await Task.Delay(450, ct).ConfigureAwait(false); // laisse le As revenir
        }
        Journaliseur.Info($"[STATS] Auto-distribution terminée (capital restant {_etat.Personnage.PointsCaracteristiques}).");
    }

    /// <summary>Monte tous les sorts connus tant qu'il reste des points de
    /// sort (1 passe par sort, re-check via SL/As).</summary>
    public async Task AutoMonterSortsAsync(CancellationToken ct = default)
    {
        Journaliseur.Info($"[SORTS] Auto-montée sorts (pts {_etat.Personnage.PointsSorts}).");
        int garde = 0;
        while (!ct.IsCancellationRequested && _etat.Personnage.PointsSorts > 0 && garde++ < 200)
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
