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

    private BotDofus.Divers.MultiAccount.GroupeHeros? _groupeHeros;

    /// <summary>
    /// Groupe héros actuellement attaché à ce compte (null si pas en mode héros).
    /// Instancié par <see cref="BotDofus.Divers.MultiAccount.DetecteurModeHeros"/>
    /// à la réception du 1er <c>GTSX</c> d'un combat. Dissous à la déconnexion.
    /// Le set émet <see cref="GroupeHerosChange"/> pour permettre à l'UI de
    /// s'attacher dès la création (cas typique : la VueGroupeHeros est liée au
    /// Compte AVANT que le 1er GTSX arrive — sans cet event elle resterait
    /// attachée à null).
    /// </summary>
    public BotDofus.Divers.MultiAccount.GroupeHeros? GroupeHeros
    {
        get => _groupeHeros;
        set
        {
            if (ReferenceEquals(_groupeHeros, value)) return;
            _groupeHeros = value;
            GroupeHerosChange?.Invoke(this, value);
        }
    }

    /// <summary>Déclenché quand le <see cref="GroupeHeros"/> est (dé)affecté.</summary>
    public event EventHandler<BotDofus.Divers.MultiAccount.GroupeHeros?>? GroupeHerosChange;

    /// <summary>
    /// Déclenché quand le serveur envoie une erreur d'invitation
    /// (<c>PI&lt;code&gt;</c>, ex <c>PIEa</c>). Permet à
    /// <see cref="BotDofus.Divers.MultiAccount.AutoInviteurHeros"/> de passer
    /// immédiatement à l'invitation suivante au lieu d'attendre 4s de timeout.
    /// L'argument est le code brut serveur (ex <c>"Ea"</c>).
    /// </summary>
    public event EventHandler<string>? InvitationRefusee;

    /// <summary>Déclenche l'event <see cref="InvitationRefusee"/>.</summary>
    public void DeclencherInvitationRefusee(string code)
        => InvitationRefusee?.Invoke(this, code);

    /// <summary>
    /// Activateur mode héros Abrak (séquence <c>NOL → NA&lt;ids&gt;</c>).
    /// Affecté par <see cref="ContexteCompte"/> à l'init. <c>null</c> tant que
    /// le contexte n'est pas créé.
    /// </summary>
    public BotDofus.Divers.MultiAccount.ActivateurHerosAbrak? ActivateurHerosAbrak { get; set; }

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
