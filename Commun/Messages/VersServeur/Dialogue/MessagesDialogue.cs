using BotDofus.Commun.Reseau;

namespace BotDofus.Commun.Messages.VersServeur.Dialogue;

// =====================================================================
// VersServeur / Dialogue
// Interactions avec les PNJ (ouvrir dialogue, choisir réponse, quitter).
// =====================================================================

/// <summary>
/// DC : commencer un dialogue avec un PNJ (identifiant du sprite sur la carte).
/// Format exact Abrak (core.swf dofus.aks.Dialog.create) : <c>DC&lt;spriteId&gt;</c>.
/// ATTENTION : la famille « D » est dans la whitelist chiffrée
/// (Aks.prepareSendPacket → true) : ce paquet DOIT passer par le canal « - »
/// chiffré pour être pris en compte côté serveur ; injecté en clair il est
/// silencieusement ignoré.
/// </summary>
public sealed class MessageDialogueDebuter : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "DC";
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
