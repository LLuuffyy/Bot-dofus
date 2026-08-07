using BotDofus.Commun.Reseau;

namespace BotDofus.Commun.Messages.VersServeur.Jeu;

// =====================================================================
// Messages VersServeur additionnels observés sur Hystoria 1.29.
// =====================================================================

/// <summary>BD : demande la date courante au serveur.</summary>
public sealed class MessageDemandeDate : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "BD";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public override void Desserialiser(string charge) { Charge = charge; }
}

/// <summary>
/// GKK0 : signal "fin de mouvement" envoyé par le client une fois son
/// déplacement local terminé. Toujours suivi d'un BN du serveur.
/// </summary>
public sealed class MessageFinMouvement : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "GKK";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public string Code { get; set; } = "0";
    public override void Desserialiser(string charge) { Charge = charge; Code = charge; }
    public override string Serialiser() => Prefixe + Code;
}

/// <summary>GI : demande d'informations de jeu après chargement de carte.</summary>
public sealed class MessageDemandeInfosJeu : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "GI";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public override void Desserialiser(string charge) { Charge = charge; }
}

/// <summary>cC+&lt;canal&gt; ou cC-&lt;canal&gt; : abonnement / désabonnement à un canal de chat.</summary>
public sealed class MessageCanalChat : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "cC";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public bool Abonner { get; set; } = true;
    public string Canal { get; set; } = "i";
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        if (charge.Length == 0) return;
        Abonner = charge[0] == '+';
        Canal = charge.Length > 1 ? charge[1..] : string.Empty;
    }
    public override string Serialiser() => $"{Prefixe}{(Abonner ? '+' : '-')}{Canal}";
}
