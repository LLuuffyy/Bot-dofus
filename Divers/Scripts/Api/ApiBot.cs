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
    public async Task<bool> SeDeplacerVersCelluleAsync(int celluleCible, CancellationToken ct = default)
    {
        if (_session is null)
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

        var chemin = Pathfinder.Trouver(_etat.CarteCourante, depart, arrivee);
        if (chemin == null || chemin.Count < 2)
        {
            Journaliseur.Avertir($"API.SeDeplacerVersCellule : aucun chemin {depart.Identifiant} → {celluleCible}");
            return false;
        }

        string paquet = Pathfinder.PaquetDeplacement(chemin);
        Journaliseur.Info($"API.SeDeplacerVersCellule : chemin {chemin.Count} cellules, packet={paquet[..Math.Min(paquet.Length, 60)]}...");
        await EnvoyerHumaniseAsync(paquet, ct).ConfigureAwait(false);
        return true;
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
    /// Engage le groupe de monstres le plus proche : <c>GA902&lt;idGroupe&gt;</c>
    /// (format confirmé bot réf dyshay + whitelist core.swf → chiffré '-').
    /// </summary>
    public async Task<bool> EngagerCombatAsync(CancellationToken ct)
    {
        var cible = MonstreLePlusProche();
        if (cible == null) { Journaliseur.Info("[FARM] aucun monstre sur la carte."); return false; }
        Journaliseur.Info(
            $"[FARM] cible groupe #{cible.Identifiant} « {cible.Nom} » cell {cible.CellulePosition} → GA902");
        await EnvoyerHumaniseAsync("GA902" + cible.Identifiant, ct).ConfigureAwait(false);
        return true;
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

                // S'approcher : le serveur engage souvent au contact ; sinon
                // on force avec GA902.
                await SeDeplacerVersCelluleAsync(mob.CellulePosition, ct).ConfigureAwait(false);
                await Task.Delay(1800, ct).ConfigureAwait(false);

                if (_etat.Combat.Etat == EtatCombat.Inactif)
                    await EngagerCombatAsync(ct).ConfigureAwait(false);

                await Task.Delay(3000, ct).ConfigureAwait(false);
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
