using System;

namespace BotDofus.Divers;

/// <summary>
/// Contrat commun aux objets dont les ressources doivent être libérées explicitement
/// au changement de carte, de combat ou de session.
/// </summary>
public interface IEffacable : IDisposable
{
    /// <summary>Libère/efface l'état interne en préservant l'objet pour réutilisation.</summary>
    void Effacer();
}
