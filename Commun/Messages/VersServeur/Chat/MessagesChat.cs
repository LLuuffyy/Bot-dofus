using BotDofus.Commun.Reseau;

namespace BotDofus.Commun.Messages.VersServeur.Chat;

// =====================================================================
// VersServeur / Chat
// Envoi de messages et smileys par le bot.
// =====================================================================

/// <summary>BM : envoyer un message sur un canal (général, guilde, groupe, privé).</summary>
public sealed class MessageChatEnvoyer : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "BM";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public string Canal { get; set; } = "*";   // * = général, % = guilde, $ = groupe, ^ = alliance
    public string? DestinataireMP { get; set; } // rempli uniquement pour les privés ("/w")
    public string Texte { get; set; } = string.Empty;

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        Texte = charge;
    }

    public override string Serialiser()
    {
        if (!string.IsNullOrEmpty(DestinataireMP))
        {
            return $"{Prefixe}|{DestinataireMP}|{Texte}";
        }
        return $"{Prefixe}{Canal}{Texte}";
    }
}

/// <summary>BS : envoyer un smiley sur la carte.</summary>
public sealed class MessageSmileyEnvoyer : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "BS";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public int IdentifiantSmiley { get; set; }

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        int.TryParse(charge, out var v);
        IdentifiantSmiley = v;
    }

    public override string Serialiser() => $"{Prefixe}{IdentifiantSmiley}";
}
