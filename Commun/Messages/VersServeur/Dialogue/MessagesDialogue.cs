using BotDofus.Commun.Reseau;

namespace BotDofus.Commun.Messages.VersServeur.Dialogue;

// =====================================================================
// VersServeur / Dialogue
// Interactions avec les PNJ (ouvrir dialogue, choisir réponse, quitter).
// =====================================================================

/// <summary>DB : commencer un dialogue avec un PNJ (identifiant sur la carte).</summary>
public sealed class MessageDialogueDebuter : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "DB";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public int IdentifiantPNJ { get; set; }

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        int.TryParse(charge, out var v);
        IdentifiantPNJ = v;
    }

    public override string Serialiser() => $"{Prefixe}{IdentifiantPNJ}";
}

/// <summary>DR : choisir une réponse dans un dialogue en cours.</summary>
public sealed class MessageDialogueReponse : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "DR";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public int IdentifiantReponse { get; set; }

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        int.TryParse(charge, out var v);
        IdentifiantReponse = v;
    }

    public override string Serialiser() => $"{Prefixe}{IdentifiantReponse}";
}

/// <summary>DV : quitter le dialogue courant.</summary>
public sealed class MessageDialogueQuitter : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "DV";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public override void Desserialiser(string charge) { Charge = charge; }
}
