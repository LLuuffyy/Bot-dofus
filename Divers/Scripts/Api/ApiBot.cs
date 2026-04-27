using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BotDofus.Commun.Messages.VersServeur.Chat;
using BotDofus.Commun.Messages.VersServeur.Dialogue;
using BotDofus.Commun.Messages.VersServeur.Jeu;
using BotDofus.Commun.Reseau;
using BotDofus.Divers.Cartes.Deplacement;
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

    public ApiBot(Compte compte, EtatJeu etat)
    {
        _compte = compte;
        _etat = etat;
    }

    /// <summary>Lie l'API à la session MITM active (appelée quand le client se connecte).</summary>
    public void LierSession(SessionProxy session) => _session = session;

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
        await _session.EnvoyerAuServeurAsync(paquet, ct).ConfigureAwait(false);
        return true;
    }

    /// <summary>Ouvre un dialogue avec un PNJ, puis enchaîne les réponses indiquées.</summary>
    public async Task ParlerAuPNJAsync(int idPNJ, IReadOnlyList<int>? reponses, CancellationToken ct)
    {
        if (_session is null) return;
        Journaliseur.Debogue($"API.ParlerAuPNJ #{idPNJ}");

        await _session.EnvoyerAuServeurAsync(new MessageDialogueDebuter { IdentifiantPNJ = idPNJ }.Serialiser(), ct).ConfigureAwait(false);

        if (reponses is null) return;
        foreach (var reponse in reponses)
        {
            await Task.Delay(400, ct).ConfigureAwait(false);
            var choix = reponse == -1 ? 1 : reponse;
            await _session.EnvoyerAuServeurAsync(new MessageDialogueReponse { IdentifiantReponse = choix }.Serialiser(), ct).ConfigureAwait(false);
        }
    }

    /// <summary>Tente d'engager un combat sur la carte courante (monstre le plus proche).</summary>
    public async Task EngagerCombatAsync(CancellationToken ct)
    {
        if (_session is null) return;
        Journaliseur.Debogue("API.EngagerCombat");
        // TODO : repérer le groupe de monstres le plus proche via EtatJeu.CarteCourante.Entites,
        //        puis émettre GC avec cible.
        await Task.Delay(300, ct).ConfigureAwait(false);
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
        if (_session is null) return;
        await _session.EnvoyerAuServeurAsync(new MessageChatEnvoyer
        {
            Canal = canal,
            Texte = texte
        }.Serialiser(), ct).ConfigureAwait(false);
    }

    /// <summary>Envoie la commande serveur Hystoria ".travel x,y".</summary>
    public Task EnvoyerTravelAsync(int x, int y, CancellationToken ct = default)
        => EnvoyerMessageAsync("*", $".travel {x},{y}", ct);

    /// <summary>Envoie un paquet brut au serveur depuis les outils UI.</summary>
    public async Task EnvoyerPaquetBrutAsync(string paquet, CancellationToken ct = default)
    {
        if (_session is null || string.IsNullOrWhiteSpace(paquet)) return;
        await _session.EnvoyerAuServeurAsync(paquet.Trim(), ct).ConfigureAwait(false);
    }

    /// <summary>Fin de tour en combat.</summary>
    public async Task FinirTourAsync(CancellationToken ct = default)
    {
        if (_session is null) return;
        await _session.EnvoyerAuServeurAsync(new MessageJeuFinirTour().Serialiser(), ct).ConfigureAwait(false);
    }

    /// <summary>Placement initial en combat.</summary>
    public async Task SePlacerEnCombatAsync(int celluleDepart, CancellationToken ct = default)
    {
        if (_session is null) return;
        await _session.EnvoyerAuServeurAsync(new MessageJeuPosition { CaseDepart = celluleDepart }.Serialiser(), ct).ConfigureAwait(false);
        await _session.EnvoyerAuServeurAsync(new MessageJeuPret { Pret = true }.Serialiser(), ct).ConfigureAwait(false);
    }

    /// <summary>Récupère le pseudo du personnage actif (exposable aux scripts Lua).</summary>
    public string ObtenirPseudo() => _compte.PseudoAffiche ?? _compte.Identifiant;

    /// <summary>Récupère les PV actuels / max du personnage (exposable aux scripts Lua).</summary>
    public (int pv, int pvMax) ObtenirVie() => (_etat.Personnage.Vie, _etat.Personnage.VieMax);
}
