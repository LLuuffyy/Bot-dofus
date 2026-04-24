using BotDofus.Commun.Reseau;

namespace BotDofus.Commun.Messages.VersClient.Chat;

// =====================================================================
// VersClient / Chat
// Messages de communication : canaux, privé, annonces serveur.
// =====================================================================

/// <summary>cMK : message de chat d'un joueur (canal|idEmetteur|pseudoEmetteur|message).</summary>
public sealed class MessageChatMessage : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "cMK";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;

    public string Canal { get; private set; } = string.Empty;
    public int IdentifiantEmetteur { get; private set; }
    public string PseudoEmetteur { get; private set; } = string.Empty;
    public string Texte { get; private set; } = string.Empty;

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var parts = charge.Split('|', 4);
        if (parts.Length > 0) Canal = parts[0];
        if (parts.Length > 1 && int.TryParse(parts[1], out var id)) IdentifiantEmetteur = id;
        if (parts.Length > 2) PseudoEmetteur = parts[2];
        if (parts.Length > 3) Texte = parts[3];
    }
}

/// <summary>cMS : message serveur (annonce officielle).</summary>
public sealed class MessageChatServeur : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "cMS";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public string Texte { get; private set; } = string.Empty;
    public override void Desserialiser(string charge) { Charge = charge; Texte = charge; }
}
