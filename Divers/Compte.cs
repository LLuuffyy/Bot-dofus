using System;
using BotDofus.Divers.Enums;

namespace BotDofus.Divers;

/// <summary>
/// Représente un compte bot chargé en mémoire : identifiants, état de session,
/// et futures références vers la couche réseau, le personnage et le moteur de scripts.
/// </summary>
public sealed class Compte : IEffacable
{
    public Compte(string identifiant, string motDePasse)
    {
        Identifiant = identifiant ?? throw new ArgumentNullException(nameof(identifiant));
        MotDePasse = motDePasse ?? throw new ArgumentNullException(nameof(motDePasse));
        Etat = EtatsCompte.Deconnecte;
    }

    public string Identifiant { get; }
    public string MotDePasse { get; }
    public string? PseudoAffiche { get; set; }
    public int ServeurPrefere { get; set; }
    public int PersonnagePrefere { get; set; }

    public EtatsCompte Etat { get; set; }

    /// <summary>
    /// MODE PASSIF GLOBAL — quand true, le bot N'AGIT JAMAIS automatiquement :
    /// pas d'IA combat, pas de script Lua, pas de récolte auto, pas de réponses
    /// de placement, rien. Le bot devient un sniffer pur : il observe le trafic
    /// et c'est tout. Sert pour les sessions de capture protocole où l'utilisateur
    /// joue à la main et veut un log propre sans interférence du bot.
    ///
    /// Miroir de ContexteCompte.ModePassif (qui contrôle aussi l'humaniseur).
    /// Lu par TrameJeu.JouerTourCombatAsync pour skip l'IA combat.
    /// </summary>
    public bool ModePassif { get; set; }

    /// <summary>
    /// Config combat persistée (peleas/&lt;perso&gt;.json) — règles de sorts,
    /// stratégie, positionnement, consommable de soin. Set par
    /// <see cref="BotDofus.Divers.ContexteCompte"/> au démarrage et exposée
    /// ici pour que <see cref="BotDofus.Commun.Frames.TrameJeu"/> y accède
    /// au moment de jouer le tour (modèle dyshay/SynFus).
    /// </summary>
    public BotDofus.Divers.Combats.IA.ConfigCombat? ConfigCombat { get; set; }

    /// <summary>
    /// Config dépôt banque automatique (banque/&lt;perso&gt;.json).
    /// Si <c>Active</c> et poids ≥ <c>SeuilPoidsPct</c>, le bot interrompt le
    /// farm pour déposer ses items à la banque (cf. <see cref="Banque.PiloteBanque"/>).
    /// </summary>
    public BotDofus.Divers.Banque.ConfigBanque? ConfigBanque { get; set; }

    /// <summary>
    /// Groupe héros actuellement attaché à ce compte (null si pas en mode héros).
    /// Instancié par <see cref="BotDofus.Divers.MultiAccount.DetecteurModeHeros"/>
    /// à la réception du 1er <c>GTSX</c> d'un combat. Dissous à la déconnexion.
    /// Cf. <c>docs/SYNTHESE-MODE-HEROS-PHASE1.md</c> (Phase 3).
    /// </summary>
    public BotDofus.Divers.MultiAccount.GroupeHeros? GroupeHeros { get; set; }

    /// <summary>
    /// URL webhook Discord pour notifications événements importants (mort,
    /// level up, banque pleine, déconnexion). Vide = pas de notif.
    /// Format : <c>https://discord.com/api/webhooks/&lt;id&gt;/&lt;token&gt;</c>.
    /// </summary>
    public string WebhookDiscordUrl { get; set; } = string.Empty;

    /// <summary>Déclenché à chaque changement d'état, utilisé par l'UI pour se rafraîchir.</summary>
    public event EventHandler<EtatsCompte>? EtatChange;

    public void ChangerEtat(EtatsCompte nouvelEtat)
    {
        if (Etat == nouvelEtat) return;
        Etat = nouvelEtat;
        EtatChange?.Invoke(this, nouvelEtat);
    }

    public void Effacer()
    {
        // TODO : réinitialiser les futurs sous-systèmes (Personnage, Carte, Combat, Scripts)
        ChangerEtat(EtatsCompte.Deconnecte);
    }

    public void Dispose()
    {
        Effacer();
        GC.SuppressFinalize(this);
    }
}
