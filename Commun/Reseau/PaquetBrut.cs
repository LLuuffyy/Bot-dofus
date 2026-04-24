using System;

namespace BotDofus.Commun.Reseau;

/// <summary>
/// Représente un paquet Dofus Retro brut, après extraction par le proxy mais avant
/// tout décodage typé. Le protocole 1.29 est textuel : chaque paquet est une chaîne
/// UTF-8 se terminant par un octet null (0x00), avec un préfixe de 2 à 3 caractères
/// identifiant le type de message.
/// </summary>
public sealed class PaquetBrut
{
    public PaquetBrut(DirectionPaquet direction, string contenu)
    {
        Direction = direction;
        Contenu = contenu ?? string.Empty;
        Horodatage = DateTime.UtcNow;
    }

    public DirectionPaquet Direction { get; }

    /// <summary>Contenu textuel complet du paquet, sans le null terminal.</summary>
    public string Contenu { get; }

    public DateTime Horodatage { get; }

    /// <summary>
    /// Préfixe 2 caractères du paquet (identifiant de type).
    /// Dofus Retro 1.29 utilise quasi-exclusivement des préfixes de 2 caractères
    /// (ex. "HG", "HC", "Af", "GC", "BM"). Certains paquets ont ensuite un
    /// sous-code 1 caractère (ex. "BM K" → le 3e caractère qualifie le sous-type).
    /// </summary>
    public string Prefixe => Contenu.Length >= 2 ? Contenu[..2] : Contenu;

    /// <summary>Sous-code éventuel après le préfixe (1 caractère ou vide).</summary>
    public string SousCode => Contenu.Length >= 3 ? Contenu.Substring(2, 1) : string.Empty;

    /// <summary>Charge utile du paquet : tout ce qui suit le préfixe.</summary>
    public string Charge => Contenu.Length > 2 ? Contenu[2..] : string.Empty;

    public override string ToString()
    {
        var fleche = Direction == DirectionPaquet.VersClient ? "<--" : "-->";
        return $"[{Horodatage:HH:mm:ss.fff}] {fleche} {Contenu}";
    }
}
