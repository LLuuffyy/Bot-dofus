namespace BotDofus.Commun.Reseau;

/// <summary>
/// Paramètres de la couche réseau : cible distante (serveur de jeu privé)
/// et port d'écoute local du proxy MITM.
/// </summary>
public sealed class ConfigReseau
{
    // =====================================================================
    // Le protocole Hystoria sépare l'authentification et le jeu en deux
    // serveurs distincts (observé via Synfus Bot) :
    //   - Auth : 162.19.127.156:450
    //   - Jeu  : 162.19.127.155:5555 (transmis chiffré dans AYK)
    // Le proxy expose deux listeners locaux pour MITM les deux étapes.
    // =====================================================================

    /// <summary>Hôte du serveur d'authentification distant.</summary>
    public string HoteDistant { get; set; } = "162.19.127.156";

    /// <summary>Port du serveur d'authentification distant.</summary>
    public int PortDistant { get; set; } = 450;

    /// <summary>Hôte du serveur de jeu distant (utilisé après redirect AYK).</summary>
    public string HoteJeuDistant { get; set; } = "162.19.127.155";

    /// <summary>Port du serveur de jeu distant.</summary>
    public int PortJeuDistant { get; set; } = 5555;

    /// <summary>Adresse d'écoute locale du proxy MITM (les deux listeners).</summary>
    public string AdresseEcouteLocale { get; set; } = "127.0.0.1";

    /// <summary>Port local du listener routant vers le serveur d'auth.</summary>
    public int PortEcouteLocal { get; set; } = 5555;

    /// <summary>Port local du listener routant vers le serveur de jeu.</summary>
    public int PortEcouteJeuLocal { get; set; } = 5556;

    /// <summary>Délai maximum d'attente d'octets avant considérer la connexion zombie (ms).</summary>
    public int DelaiLectureMs { get; set; } = 30_000;

    /// <summary>Taille du tampon de réception TCP.</summary>
    public int TailleTamponOctets { get; set; } = 8192;
}
