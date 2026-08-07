namespace BotDofus.Commun.Reseau;

/// <summary>
/// Sens d'un paquet intercepté par le proxy MITM.
/// </summary>
public enum DirectionPaquet
{
    /// <summary>Message émis par le serveur et destiné au client Dofus.</summary>
    VersClient,

    /// <summary>Message émis par le client Dofus et destiné au serveur de jeu.</summary>
    VersServeur
}
