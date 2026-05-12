namespace BotDofus.Commun.Reseau;

/// <summary>
/// Paramètres de la couche réseau : cible distante (serveur de jeu privé)
/// et port d'écoute local du proxy MITM.
/// </summary>
public sealed class ConfigReseau
{
    // =====================================================================
    // Serveur ciblé : Aqua (play-astra.net), Dofus Retro 1.39.6
    //   - Auth : 141.94.99.2:7781 (IP statique observée — pas de hostname côté loader)
    //   - Jeu  : aqua.play-astra.net:5562 (hostname transmis dans AYK,
    //            résolu à l'ouverture de la connexion sortante)
    // Le proxy expose deux listeners locaux pour MITM les deux étapes.
    //
    // Historique : on supportait avant Hystoria (162.19.127.156:450 / 5555)
    // mais le serveur a fermé / le prof a changé de cible.
    // =====================================================================

    /// <summary>Hôte du serveur d'authentification distant (IP ou hostname).</summary>
    public string HoteDistant { get; set; } = "141.94.99.2";

    /// <summary>Port du serveur d'authentification distant.</summary>
    public int PortDistant { get; set; } = 7781;

    /// <summary>
    /// Hôte du serveur de jeu distant. Sur Aqua c'est dynamique (transmis dans le AYK
    /// après sélection du serveur), ce champ sert de fallback / valeur observée.
    /// </summary>
    public string HoteJeuDistant { get; set; } = "aqua.play-astra.net";

    /// <summary>Port du serveur de jeu distant.</summary>
    public int PortJeuDistant { get; set; } = 5562;

    /// <summary>Adresse d'écoute locale du proxy MITM (les deux listeners).</summary>
    public string AdresseEcouteLocale { get; set; } = "0.0.0.0";

    /// <summary>
    /// Port local du listener routant vers le serveur d'auth.
    /// Aqua : on s'aligne sur 7781 (le client lit l'IP/port depuis config.xml
    /// — voir <see cref="BotDofus.Utilitaires.Aqua.PatcheurConfigXml"/>).
    /// </summary>
    public int PortEcouteLocal { get; set; } = 7781;

    /// <summary>
    /// Port local du listener routant vers le serveur de jeu.
    /// Doit être différent de <see cref="PortEcouteLocal"/>. Synfus utilise 30200,
    /// nous prenons 5562 par cohérence (port jeu observé).
    /// </summary>
    public int PortEcouteJeuLocal { get; set; } = 5562;

    /// <summary>Délai maximum d'attente d'octets avant considérer la connexion zombie (ms).</summary>
    public int DelaiLectureMs { get; set; } = 30_000;

    /// <summary>Taille du tampon de réception TCP.</summary>
    public int TailleTamponOctets { get; set; } = 8192;
}
