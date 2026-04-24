using BotDofus.Commun.Reseau;

namespace BotDofus.Commun.Messages.VersServeur.Jeu;

// =====================================================================
// VersServeur / Jeu
// Actions que le bot envoie au serveur durant le gameplay.
// =====================================================================

/// <summary>GR : signale que le client est prêt (chargement carte terminé, prêt à jouer).</summary>
public sealed class MessageJeuPret : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "GR";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public bool Pret { get; set; } = true;

    public override void Desserialiser(string charge) { Charge = charge; }
    public override string Serialiser() => Prefixe + (Pret ? "K" : "F");
}

/// <summary>GA : action de jeu (déplacement, lancer sort, utiliser objet...). Le sous-code qualifie.</summary>
public sealed class MessageJeuAction : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "GA";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public string SousCode { get; set; } = string.Empty;
    public string Parametres { get; set; } = string.Empty;

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        if (charge.Length >= 3) SousCode = charge[..3];
        if (charge.Length > SousCode.Length) Parametres = charge[SousCode.Length..];
    }

    public override string Serialiser() => $"{Prefixe}{SousCode}{Parametres}";
}

/// <summary>GC : créer/démarrer un combat (défi à un joueur ou attaque d'un monstre).</summary>
public sealed class MessageJeuCreer : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "GC";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public int TypeCombat { get; set; }
    public int? IdentifiantCible { get; set; }
    public override void Desserialiser(string charge) { Charge = charge; }
    public override string Serialiser()
    {
        var cible = IdentifiantCible.HasValue ? "|" + IdentifiantCible.Value : string.Empty;
        return $"{Prefixe}{TypeCombat}{cible}";
    }
}

/// <summary>GE : finir son tour en combat.</summary>
public sealed class MessageJeuFinirTour : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "GE";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public override void Desserialiser(string charge) { Charge = charge; }
}

/// <summary>GP : se placer sur une case de départ avant combat.</summary>
public sealed class MessageJeuPosition : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "GP";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public int CaseDepart { get; set; }

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        int.TryParse(charge, out var v);
        CaseDepart = v;
    }

    public override string Serialiser() => $"{Prefixe}{CaseDepart}";
}

/// <summary>GQ : quitter la carte courante / la partie.</summary>
public sealed class MessageJeuQuitter : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "GQ";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public override void Desserialiser(string charge) { Charge = charge; }
}
