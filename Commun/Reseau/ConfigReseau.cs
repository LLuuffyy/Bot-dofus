namespace BotDofus.Commun.Reseau;

/// <summary>
/// Paramètres de la couche réseau : cible distante (serveur de jeu privé)
/// et port d'écoute local du proxy MITM.
/// </summary>
public sealed class ConfigReseau
{
    /// <summary>Hôte (IP ou DNS) du serveur de jeu distant. Placeholder tant que l'IP du serveur privé n'est pas connue.</summary>
    public string HoteDistant { get; set; } = "127.0.0.1";

    /// <summary>Port du serveur de jeu distant. Dofus Retro historique : 443 (auth) ou 5555 (jeu).</summary>
    public int PortDistant { get; set; } = 443;

    /// <summary>Adresse d'écoute locale du proxy MITM.</summary>
    public string AdresseEcouteLocale { get; set; } = "127.0.0.1";

    /// <summary>Port d'écoute local. Le client Dofus sera redirigé vers cette adresse.</summary>
    public int PortEcouteLocal { get; set; } = 5555;

    /// <summary>Délai maximum d'attente d'octets avant considérer la connexion zombie (ms).</summary>
    public int DelaiLectureMs { get; set; } = 30_000;

    /// <summary>Taille du tampon de réception TCP.</summary>
    public int TailleTamponOctets { get; set; } = 8192;
}
