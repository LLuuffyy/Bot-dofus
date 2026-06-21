using System;
using System.IO;
using System.Text.Json;

namespace BotDofus.Commun.Reseau;

/// <summary>
/// Paramètres de la couche réseau : cible distante (serveur de jeu privé)
/// et port d'écoute local du proxy MITM.
///
/// Source unique de vérité pour TOUS les composants (RedirecteurWinDivert,
/// SessionProxy, MainWindow, ClientAutonome…). Aucune valeur ne doit être
/// hardcodée ailleurs.
///
/// Chargement : <see cref="ChargerOuDefaut"/> lit
/// <c>BotDofus.Wpf/bin/Debug/.../config-reseau.json</c> au démarrage (override
/// utilisateur). Sauvegarde via <see cref="Sauvegarder"/>.
/// </summary>
public sealed class ConfigReseau
{
    // =====================================================================
    // Cible courante : Rafale (Rafal Retro v1.47.2) — Dofus Retro privé, TCP brut.
    //   - Auth : 162.19.126.176:446  (capturé 2026-06-02 via sniffer)
    //   - Jeu  : 162.19.126.176:6977 (capturé 2026-06-02 via sniffer)
    //   - Hostname OVH : ns3027085.ip-162-19-126.eu (FR)
    //
    // Le port 26118 vu dans <c>config.xml</c> du retroclient est en fait le
    // port utilisé par le launcher Zaap (cdn.playrafal.com), PAS par le
    // client de jeu lui-même.
    //
    // Historique des cibles :
    //   - Hystoria : 162.19.127.156:450  (ancien)
    //   - Aqua     : 141.94.99.2:7781    (ancien)
    //   - Abrak    : 51.89.153.20:1303   (Hystoria via launcher Abrak)
    //   - Rafale   : 162.19.126.176:446/6977 (cible courante 2026-06+)
    // =====================================================================

    /// <summary>Nom symbolique du serveur courant (affiché UI / logs).</summary>
    public string NomServeur { get; set; } = "Rafale";

    /// <summary>Hôte du serveur d'authentification distant (IP ou hostname).</summary>
    public string HoteDistant { get; set; } = "162.19.126.176";

    /// <summary>Port du serveur d'authentification distant.</summary>
    public int PortDistant { get; set; } = 446;

    /// <summary>Hôte du serveur de jeu distant. Identique à
    /// <see cref="HoteDistant"/> sur Rafale (valeur observée du AYK).</summary>
    public string HoteJeuDistant { get; set; } = "162.19.126.176";

    /// <summary>Port du serveur de jeu distant.</summary>
    public int PortJeuDistant { get; set; } = 6977;

    /// <summary>Adresse d'écoute locale du proxy MITM.
    /// <c>127.0.0.1</c> volontaire : seul Dofus.exe local peut atteindre le
    /// proxy → on ne s'expose pas sur le LAN.</summary>
    public string AdresseEcouteLocale { get; set; } = "127.0.0.1";

    /// <summary>Port local du listener routant vers le serveur d'auth.
    /// Aligné sur le port distant (le client tape direct sur l'IP/port serveur,
    /// WinDivert intercepte et redirige sur 127.0.0.1:&lt;port&gt;).</summary>
    public int PortEcouteLocal { get; set; } = 446;

    /// <summary>Port local du listener routant vers le serveur de jeu.</summary>
    public int PortEcouteJeuLocal { get; set; } = 6977;

    /// <summary>Port source local fixe pour la connexion SORTANTE du proxy
    /// vers le vrai serveur. Utilisé en mode WinDivert : le redirecteur
    /// exclut ce port source du filtre pour ne PAS réintercepter la
    /// connexion du proxy (sinon boucle infinie).</summary>
    public int PortSourceMarqueur { get; set; } = 50446;

    /// <summary>Port source marqueur DÉDIÉ au proxy JEU. Doit être différent
    /// de <see cref="PortSourceMarqueur"/>.</summary>
    public int PortSourceMarqueurJeu { get; set; } = 50697;

    /// <summary>Délai maximum d'attente d'octets avant considérer la
    /// connexion zombie (ms).</summary>
    public int DelaiLectureMs { get; set; } = 30_000;

    /// <summary>Taille du tampon de réception TCP.</summary>
    public int TailleTamponOctets { get; set; } = 8192;

    /// <summary>Chemin par défaut de Dofus.exe pour le serveur courant
    /// (override possible par config-reseau.json).</summary>
    public string CheminClientDofus { get; set; } = @"C:\Games\Rafal\Dofus.exe";

    /// <summary>Chemin par défaut du <c>config.xml</c> du client
    /// (lu par le patcheur de redirection).</summary>
    public string CheminConfigXmlClient { get; set; } =
        @"C:\Games\Rafal\resources\app\retroclient\config.xml";

    /// <summary>True si <see cref="HoteDistant"/> est vide → lancer le
    /// sniffer avant le proxy.</summary>
    public bool EstIpServeurInconnue
        => string.IsNullOrWhiteSpace(HoteDistant) || HoteDistant == "0.0.0.0";

    // =====================================================================
    // Persistance JSON
    // =====================================================================

    /// <summary>Chemin du fichier de config persisté (à côté de l'exe).</summary>
    public static string CheminFichier =>
        Path.Combine(AppContext.BaseDirectory, "config-reseau.json");

    /// <summary>Charge la config depuis disque ou retourne les valeurs par défaut.
    /// Ne logue pas (couche très basse, le Journaliseur n'est pas forcément init).</summary>
    public static ConfigReseau ChargerOuDefaut()
    {
        try
        {
            if (File.Exists(CheminFichier))
            {
                var json = File.ReadAllText(CheminFichier);
                var cfg = JsonSerializer.Deserialize<ConfigReseau>(json,
                    new JsonSerializerOptions
                    {
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true,
                    });
                if (cfg != null) return cfg;
            }
        }
        catch
        {
            // Fichier corrompu / inaccessible → on retombe sur les valeurs par défaut.
        }
        return new ConfigReseau();
    }

    /// <summary>Sérialise la config courante sur disque. Idempotent.</summary>
    public void Sauvegarder()
    {
        try
        {
            var json = JsonSerializer.Serialize(this,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(CheminFichier, json);
        }
        catch
        {
            // Ignoré : c'est juste une persistance best-effort.
        }
    }
}
