using System;
using System.Collections.Generic;
using System.Linq;
using BotDofus.Commun.Reseau;

namespace BotDofus.Commun.Messages.VersClient.Authentification;

// =====================================================================
// VersClient / Authentification
// Ensemble des messages envoyés par le serveur durant la phase de login,
// de sélection de serveur et de sélection de personnage.
// =====================================================================

/// <summary>HC : challenge de connexion contenant la clé publique/hash.</summary>
public sealed class MessageHelloConnexion : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "HC";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public string Cle { get; private set; } = string.Empty;
    public override void Desserialiser(string charge) { Charge = charge; Cle = charge; }
}

/// <summary>HG : le serveur de jeu est prêt, le client peut démarrer le GameFrame.</summary>
public sealed class MessageHelloJeu : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "HG";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public override void Desserialiser(string charge) { Charge = charge; }
}

/// <summary>AlK : login accepté, optionnellement avec la liste des cadeaux disponibles.</summary>
public sealed class MessageConnexionSucces : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "AlK";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public bool EstAbonne { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        EstAbonne = charge.StartsWith('1');
    }
}

/// <summary>AlE : login refusé avec code d'erreur (f=bad credentials, b=banned, s=subscription, etc.).</summary>
public sealed class MessageConnexionEchec : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "AlE";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public char Code { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        Code = charge.Length > 0 ? charge[0] : '\0';
    }
}

/// <summary>Ad : pseudo du compte.</summary>
public sealed class MessagePseudo : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "Ad";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public string Pseudo { get; private set; } = string.Empty;
    public override void Desserialiser(string charge) { Charge = charge; Pseudo = charge; }
}

/// <summary>AV : communauté / région par défaut du compte.</summary>
public sealed class MessageCommunaute : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "AV";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public int IdentifiantCommunaute { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        int.TryParse(charge, out var v);
        IdentifiantCommunaute = v;
    }
}

/// <summary>AQ : question secrète du compte (création de personnage).</summary>
public sealed class MessageQuestion : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "AQ";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public string Question { get; private set; } = string.Empty;
    public override void Desserialiser(string charge) { Charge = charge; Question = charge; }
}

/// <summary>AxK : liste des serveurs avec leur état, au format id;etat|id;etat|...</summary>
public sealed class MessageListeServeurs : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "AxK";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public IReadOnlyList<InfoServeur> Serveurs { get; private set; } = Array.Empty<InfoServeur>();
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var liste = new List<InfoServeur>();
        foreach (var bloc in charge.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = bloc.Split(';');
            if (parts.Length < 2) continue;
            int.TryParse(parts[0], out var id);
            int.TryParse(parts[1], out var etat);
            int joueurs = parts.Length > 2 && int.TryParse(parts[2], out var jp) ? jp : 0;
            liste.Add(new InfoServeur(id, etat, joueurs));
        }
        Serveurs = liste;
    }

    public readonly record struct InfoServeur(int Identifiant, int EtatBrut, int NombreJoueurs);
}

/// <summary>AYK : hôte chiffré du serveur de jeu sélectionné, avec ticket d'authentification.</summary>
public sealed class MessageHoteChiffre : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "AYK";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public string IpPortChiffre { get; private set; } = string.Empty;
    public string Ticket { get; private set; } = string.Empty;
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var parts = charge.Split(';', 2);
        IpPortChiffre = parts[0];
        if (parts.Length > 1) Ticket = parts[1];
    }
}

/// <summary>ATK : envoi du ticket par le serveur (accès validé).</summary>
public sealed class MessageTicket : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "ATK";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public override void Desserialiser(string charge) { Charge = charge; }
}

/// <summary>ALK : liste des personnages du compte.</summary>
public sealed class MessageListePersonnages : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "ALK";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public int NombreMax { get; private set; }
    public IReadOnlyList<InfoPersonnage> Personnages { get; private set; } = Array.Empty<InfoPersonnage>();
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var parts = charge.Split('|');
        if (parts.Length == 0) return;
        int.TryParse(parts[0], out var nb);
        NombreMax = nb;
        var liste = new List<InfoPersonnage>();
        for (int i = 1; i < parts.Length; i++)
        {
            var champs = parts[i].Split(';');
            if (champs.Length < 5) continue;
            int.TryParse(champs[0], out var id);
            int.TryParse(champs[2], out var niveau);
            int.TryParse(champs[3], out var classe);
            int.TryParse(champs[4], out var sexe);
            liste.Add(new InfoPersonnage(id, champs[1], niveau, classe, sexe));
        }
        Personnages = liste;
    }

    public readonly record struct InfoPersonnage(int Identifiant, string Nom, int Niveau, int IdClasse, int Sexe);
}

/// <summary>ASK : confirmation de sélection de personnage avec infos de base.</summary>
public sealed class MessageSelectionPersonnage : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "ASK";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public int Identifiant { get; private set; }
    public string Nom { get; private set; } = string.Empty;
    public int Niveau { get; private set; }
    public int IdClasse { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var parts = charge.Split('|');
        if (parts.Length < 4) return;

        if (int.TryParse(parts[0], out var id)) Identifiant = id;
        Nom = parts[1];
        if (int.TryParse(parts[2], out var niv)) Niveau = niv;
        if (int.TryParse(parts[3], out var cl)) IdClasse = cl;
    }
}

/// <summary>AR : restrictions du compte (actions interdites selon le statut d'abonnement).</summary>
public sealed class MessageRestrictions : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "AR";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public string Restrictions { get; private set; } = string.Empty;
    public override void Desserialiser(string charge) { Charge = charge; Restrictions = charge; }
}

/// <summary>As : statistiques du personnage après sélection/regen (PV, PA, PM, XP, kamas, etc.).</summary>
public sealed class MessageStats : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "As";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public long XpActuelle { get; private set; }
    public long XpPalier { get; private set; }
    public long XpProchainPalier { get; private set; }
    public long Kamas { get; private set; }
    public int PointsCaracteristiques { get; private set; }
    public int PointsSorts { get; private set; }
    public int Vie { get; private set; }
    public int VieMax { get; private set; }
    public int Energie { get; private set; }
    public int EnergieMax { get; private set; }
    public int PA { get; private set; }
    public int PM { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var blocs = charge.Split('|');

        // Bloc 0 : XP (actuelle,palier,prochain)
        if (blocs.Length > 0)
        {
            var xp = blocs[0].Split(',');
            if (xp.Length >= 3
                && long.TryParse(xp[0], out var a)
                && long.TryParse(xp[1], out var b)
                && long.TryParse(xp[2], out var c))
            {
                XpActuelle = a; XpPalier = b; XpProchainPalier = c;
            }
        }

        Kamas = ParserLong(blocs, 1);
        PointsCaracteristiques = ParserInt(blocs, 2);
        PointsSorts = ParserInt(blocs, 3);

        (Vie, VieMax) = ParserPaire(blocs, 4);
        (Energie, EnergieMax) = ParserPaire(blocs, 5);
        PA = ParserPremier(blocs, 6);
        PM = ParserPremier(blocs, 7);
    }

    private static long ParserLong(string[] blocs, int index)
        => blocs.Length > index && long.TryParse(blocs[index], out var v) ? v : 0;
    private static int ParserInt(string[] blocs, int index)
        => blocs.Length > index && int.TryParse(blocs[index], out var v) ? v : 0;
    private static (int, int) ParserPaire(string[] blocs, int index)
    {
        if (blocs.Length <= index) return (0, 0);
        var parts = blocs[index].Split(',');
        int.TryParse(parts.ElementAtOrDefault(0) ?? "0", out var a);
        int.TryParse(parts.ElementAtOrDefault(1) ?? "0", out var b);
        return (a, b);
    }
    private static int ParserPremier(string[] blocs, int index)
    {
        if (blocs.Length <= index) return 0;
        var premier = blocs[index].Split(',')[0];
        int.TryParse(premier, out var v);
        return v;
    }
}

/// <summary>Af : position dans la file d'attente de connexion (Dofus a souvent une queue).</summary>
public sealed class MessageQueuePosition : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "Af";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public int Position { get; private set; }
    public int TotalAttente { get; private set; }
    public int TempsMoyen { get; private set; }
    public int IdentifiantQueue { get; private set; }
    public bool EstAbonne { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var parts = charge.Split('|');
        if (parts.Length > 0 && int.TryParse(parts[0], out var p)) Position = p;
        if (parts.Length > 1 && int.TryParse(parts[1], out var t)) TotalAttente = t;
        if (parts.Length > 2 && int.TryParse(parts[2], out var a)) TempsMoyen = a;
        if (parts.Length > 3 && int.TryParse(parts[3], out var q)) IdentifiantQueue = q;
        EstAbonne = parts.Length > 4 && parts[4] == "1";
    }
}

/// <summary>ANK : passage de niveau du personnage.</summary>
public sealed class MessageNouveauNiveau : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "ANK";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public int NouveauNiveau { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        int.TryParse(charge, out var n);
        NouveauNiveau = n;
    }
}
