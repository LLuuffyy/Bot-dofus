using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BotDofus.Commun.Messages.VersServeur.Chat;
using BotDofus.Commun.Messages.VersServeur.Dialogue;
using BotDofus.Commun.Messages.VersServeur.Jeu;
using BotDofus.Commun.Reseau;
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

    /// <summary>Déplace le personnage vers une cellule précise sur la carte courante.</summary>
    public async Task SeDeplacerVersCelluleAsync(int celluleCible, CancellationToken ct)
    {
        Journaliseur.Debogue($"API.SeDeplacerVersCellule {celluleCible}");
        // TODO : pathfinding CheminA + envoi GA avec clé de chemin sérialisée.
        _ = celluleCible;
        await Task.Delay(200, ct).ConfigureAwait(false);
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
