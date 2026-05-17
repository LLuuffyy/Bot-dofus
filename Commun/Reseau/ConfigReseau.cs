namespace BotDofus.Commun.Reseau;

/// <summary>
/// Paramètres de la couche réseau : cible distante (serveur de jeu privé)
/// et port d'écoute local du proxy MITM.
/// </summary>
public sealed class ConfigReseau
{
    // =====================================================================
    // Serveur ciblé : Abrak (abrak.fr), Dofus Retro — TCP BRUT (pas de TLS)
    //   - Auth : 51.89.153.20:1303 (HC challenge confirmé en clair)
    //   - Jeu  : 51.89.153.20:1304 (post-AYK, même IP, port +1)
    // Découvert par capture netstat live du process Abrak.exe.
    // Le TLS 443/DDoS-Guard observé était le CDN+API launcher, PAS le jeu.
    //
    // Historique : Hystoria (162.19.127.156:450) puis Aqua (141.94.99.2:7781),
    // le prof change de cible régulièrement → tout est centralisé ici.
    // =====================================================================

    /// <summary>Hôte du serveur d'authentification distant (IP ou hostname).</summary>
    public string HoteDistant { get; set; } = "51.89.153.20";

    /// <summary>Port du serveur d'authentification distant.</summary>
    public int PortDistant { get; set; } = 1303;

    /// <summary>
    /// Hôte du serveur de jeu distant. Abrak : même IP que l'auth, transmis
    /// (ou non) dans le AYK. Sert de fallback / valeur observée.
    /// </summary>
    public string HoteJeuDistant { get; set; } = "51.89.153.20";

    /// <summary>Port du serveur de jeu distant (observé : auth+1).</summary>
    public int PortJeuDistant { get; set; } = 1304;

    /// <summary>
    /// Adresse d'écoute locale du proxy MITM.
    /// Volontairement <c>127.0.0.1</c> et pas <c>0.0.0.0</c> : seul Dofus.exe local
    /// peut atteindre le proxy → on ne s'expose pas sur le LAN (anti-détection / hygiène).
    /// </summary>
    public string AdresseEcouteLocale { get; set; } = "127.0.0.1";

    /// <summary>
    /// Port local du listener routant vers le serveur d'auth.
    /// Abrak : on s'aligne sur 1303 (le client lit l'IP/port depuis config.xml
    /// — voir <see cref="BotDofus.Utilitaires.Aqua.PatcheurConfigXml"/>).
    /// </summary>
    public int PortEcouteLocal { get; set; } = 1303;

    /// <summary>
    /// Port local du listener routant vers le serveur de jeu.
    /// Doit être différent de <see cref="PortEcouteLocal"/>. Abrak : 1304 (auth+1).
    /// </summary>
    public int PortEcouteJeuLocal { get; set; } = 1304;

    /// <summary>
    /// Port source local fixe pour la connexion SORTANTE du proxy vers le vrai serveur.
    /// 0 = port éphémère normal. Utilisé en mode WinDivert : le redirecteur exclut
    /// ce port source du filtre pour ne PAS réintercepter la connexion du proxy
    /// lui-même (sinon boucle infinie). Voir RedirecteurWinDivert.
    /// </summary>
    public int PortSourceMarqueur { get; set; } = 50303;

    /// <summary>
    /// Port source marqueur DÉDIÉ au proxy JEU (port 1304). Doit être différent de
    /// <see cref="PortSourceMarqueur"/> : les deux proxies (auth + jeu) ouvrent
    /// chacun une connexion sortante vers 51.89.153.20 et WinDivert doit exclure
    /// les DEUX du filtre, sinon il réintercepte la connexion du proxy jeu (boucle)
    /// OU les deux sockets se disputent le même port 50303 (bind conflict).
    /// </summary>
    public int PortSourceMarqueurJeu { get; set; } = 50304;

    /// <summary>Délai maximum d'attente d'octets avant considérer la connexion zombie (ms).</summary>
    public int DelaiLectureMs { get; set; } = 30_000;

    /// <summary>Taille du tampon de réception TCP.</summary>
    public int TailleTamponOctets { get; set; } = 8192;
}
