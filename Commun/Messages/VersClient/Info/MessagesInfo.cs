using BotDofus.Commun.Reseau;

namespace BotDofus.Commun.Messages.VersClient.Info;

// =====================================================================
// VersClient / Info
// Messages d'information affichés dans la fenêtre du client (pop-ups, HUD).
// =====================================================================

/// <summary>Im : message d'information typé (succès, erreur, avertissement) avec code et arguments.</summary>
public sealed class MessageInfoMessage : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "Im";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public int Code { get; private set; }
    public string Arguments { get; private set; } = string.Empty;
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var parts = charge.Split('~', 2);
        int.TryParse(parts[0], out var c);
        Code = c;
        if (parts.Length > 1) Arguments = parts[1];
    }
}

/// <summary>IO : info de carte (accessoires, météo, fond, etc.).</summary>
public sealed class MessageInfoCarte : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "IO";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public override void Desserialiser(string charge) { Charge = charge; }
}

/// <summary>IL : actualisation des PV du personnage hors combat.</summary>
public sealed class MessageInfoVie : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "IL";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public int Vie { get; private set; }
    public int VieMax { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var parts = charge.Split(',');
        if (parts.Length > 0 && int.TryParse(parts[0], out var v)) Vie = v;
        if (parts.Length > 1 && int.TryParse(parts[1], out var m)) VieMax = m;
    }
}
