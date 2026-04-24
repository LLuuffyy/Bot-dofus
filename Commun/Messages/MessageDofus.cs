using BotDofus.Commun.Reseau;

namespace BotDofus.Commun.Messages;

/// <summary>
/// Classe de base pour tous les messages typés du protocole Dofus Retro 1.29.
/// Un message est défini par un préfixe de 2 caractères identifiant son type,
/// suivi d'une charge utile textuelle variable.
/// Le découpage de la charge utile dépend du type : séparateurs '|', '~', ';',
/// ou parsing positionnel.
/// </summary>
public abstract class MessageDofus
{
    /// <summary>Préfixe 2 caractères du message (ex. "HG", "HC", "BM").</summary>
    public abstract string Prefixe { get; }

    /// <summary>Sens du message (VersClient ou VersServeur).</summary>
    public abstract DirectionPaquet Direction { get; }

    /// <summary>Charge utile brute reçue (tout sauf le préfixe).</summary>
    public string Charge { get; internal set; } = string.Empty;

    /// <summary>Paquet brut d'origine.</summary>
    public PaquetBrut? Source { get; internal set; }

    /// <summary>
    /// Décompose la charge utile et initialise les propriétés typées du message.
    /// </summary>
    public abstract void Desserialiser(string charge);

    /// <summary>
    /// Reconstruit le contenu texte complet (préfixe + charge) prêt à être émis sur le réseau.
    /// Utilisé pour les messages VersServeur qu'on veut injecter.
    /// </summary>
    public virtual string Serialiser() => Prefixe + Charge;

    public override string ToString() => $"{Prefixe} {Charge}";
}
