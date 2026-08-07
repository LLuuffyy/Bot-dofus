using System.Text;
using BotDofus.Commun.Reseau;

namespace BotDofus.Commun.Messages.VersServeur.Authentification;

// =====================================================================
// VersServeur / Authentification
// Messages que le client envoie au serveur durant la phase de login.
// =====================================================================

/// <summary>AA : envoi login + mot de passe chiffré avec la clé reçue via HC.</summary>
public sealed class MessageAuthentification : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "AA";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public string Login { get; set; } = string.Empty;
    public string MotDePasseChiffre { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var parts = charge.Split('#', 3);
        if (parts.Length > 0) Login = parts[0];
        if (parts.Length > 1) MotDePasseChiffre = parts[1];
        if (parts.Length > 2) Version = parts[2];
    }

    public override string Serialiser() => $"{Prefixe}{Login}#{MotDePasseChiffre}#{Version}";
}

/// <summary>AX : demande de la liste des serveurs.</summary>
public sealed class MessageDemandeServeurs : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "AX";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public override void Desserialiser(string charge) { Charge = charge; }
}

/// <summary>Ax : choisir un serveur en entrant son identifiant.</summary>
public sealed class MessageChoisirServeur : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "Ax";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public int IdentifiantServeur { get; set; }

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        int.TryParse(charge, out var v);
        IdentifiantServeur = v;
    }

    public override string Serialiser() => $"{Prefixe}{IdentifiantServeur}";
}

/// <summary>AL : demande la liste des personnages.</summary>
public sealed class MessageObtenirPersonnages : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "AL";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public override void Desserialiser(string charge) { Charge = charge; }
}

/// <summary>AS : sélectionner un personnage par son identifiant.</summary>
public sealed class MessageChoisirPersonnage : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "AS";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public int IdentifiantPersonnage { get; set; }

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        int.TryParse(charge, out var v);
        IdentifiantPersonnage = v;
    }

    public override string Serialiser() => $"{Prefixe}{IdentifiantPersonnage}";
}

/// <summary>AT : transmet le ticket reçu du serveur d'auth au serveur de jeu.</summary>
public sealed class MessageEnvoiTicket : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "AT";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public string Ticket { get; set; } = string.Empty;

    public override void Desserialiser(string charge) { Charge = charge; Ticket = charge; }
    public override string Serialiser() => Prefixe + Ticket;
}

/// <summary>Ai : identité machine envoyée avec la connexion (OS, langue, adresse MAC).</summary>
public sealed class MessageIdentite : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "Ai";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public string Identite { get; set; } = string.Empty;
    public override void Desserialiser(string charge) { Charge = charge; Identite = charge; }
    public override string Serialiser() => Prefixe + Identite;
}
