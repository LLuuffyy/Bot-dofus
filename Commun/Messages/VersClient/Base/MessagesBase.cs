using BotDofus.Commun.Reseau;

namespace BotDofus.Commun.Messages.VersClient.Base;

// =====================================================================
// VersClient / Base
// Messages "système" bas niveau du protocole (ping, date, confirmations).
// =====================================================================

/// <summary>BC : confirmation générique d'une action précédente.</summary>
public sealed class MessageConfirmation : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "BC";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public override void Desserialiser(string charge) { Charge = charge; }
}

/// <summary>BP : latence moyenne mesurée par le serveur.</summary>
public sealed class MessagePingMoyen : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "BP";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public int PingMs { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        int.TryParse(charge, out var v);
        PingMs = v;
    }
}

/// <summary>BD : date en jeu (année,mois,jour).</summary>
public sealed class MessageDate : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "BD";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public int Annee { get; private set; }
    public int Mois { get; private set; }
    public int Jour { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var parts = charge.Split('|');
        if (parts.Length > 0 && int.TryParse(parts[0], out var a)) Annee = a;
        if (parts.Length > 1 && int.TryParse(parts[1], out var m)) Mois = m;
        if (parts.Length > 2 && int.TryParse(parts[2], out var j)) Jour = j;
    }
}

/// <summary>BT : temps de référence (horodatage serveur, synchronisation).</summary>
public sealed class MessageTempsReference : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "BT";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public long HorodatageMs { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        long.TryParse(charge, out var v);
        HorodatageMs = v;
    }
}
