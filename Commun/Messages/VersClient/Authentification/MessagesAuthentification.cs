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

/// <summary>
/// AH : liste des serveurs présentée au login (Dofus Retro 1.39 / Aqua).
/// Format observé : <c>AH&lt;id&gt;;&lt;etat&gt;;&lt;completion&gt;;&lt;selectionnable&gt;|...</c>
/// (ex. <c>AH2;1;10;1|4;0;10;0|100;0;10;0</c> — serveur 2 (Aqua) en ligne et sélectionnable).
/// </summary>
public sealed class MessageServeursDisponibles : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "AH";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public IReadOnlyList<InfoServeurAccueil> Serveurs { get; private set; } = Array.Empty<InfoServeurAccueil>();

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var liste = new List<InfoServeurAccueil>();
        foreach (var bloc in charge.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var p = bloc.Split(';');
            if (p.Length < 2) continue;
            int.TryParse(p[0], out var id);
            int.TryParse(p[1], out var etat);
            int completion = p.Length > 2 && int.TryParse(p[2], out var c) ? c : 0;
            bool selectionnable = p.Length > 3 && p[3] == "1";
            liste.Add(new InfoServeurAccueil(id, etat, completion, selectionnable));
        }
        Serveurs = liste;
    }

    public readonly record struct InfoServeurAccueil(
        int Identifiant, int EtatBrut, int Completion, bool Selectionnable)
    {
        public bool EnLigne => EtatBrut == 1;
    }
}

/// <summary>
/// AYK : redirect vers le serveur de jeu après login validé.
/// Format observé sur Hystoria : <c>AYK&lt;ip&gt;:&lt;port&gt;;&lt;ticket&gt;</c>
/// (ex. <c>AYK162.19.127.155:5555;7504</c>) — IP en clair, pas de cryptedIp/Port.
/// Le ticket est numérique et sera renvoyé via <c>AT&lt;ticket&gt;</c> sur le serveur de jeu.
/// </summary>
public sealed class MessageHoteChiffre : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "AYK";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;

    public string Hote { get; private set; } = string.Empty;
    public int Port { get; private set; }
    public string Ticket { get; private set; } = string.Empty;

    /// <summary>Compat avec l'ancienne API : retourne "&lt;ip&gt;:&lt;port&gt;".</summary>
    public string IpPortChiffre => Port > 0 ? $"{Hote}:{Port}" : Hote;

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var parts = charge.Split(';', 2);
        var ipPort = parts[0];
        if (parts.Length > 1) Ticket = parts[1];

        var sep = ipPort.LastIndexOf(':');
        if (sep > 0)
        {
            Hote = ipPort[..sep];
            int.TryParse(ipPort[(sep + 1)..], out var p);
            Port = p;
        }
        else
        {
            Hote = ipPort;
        }
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

/// <summary>ASK : confirmation de sélection de personnage avec infos de base + inventaire initial.</summary>
public sealed class MessageSelectionPersonnage : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "ASK";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public int Identifiant { get; private set; }
    public string Nom { get; private set; } = string.Empty;
    public int Niveau { get; private set; }
    public int IdClasse { get; private set; }
    public IReadOnlyList<VersClient.Objet.ObjetParse> ObjetsInitiaux { get; private set; }
        = System.Array.Empty<VersClient.Objet.ObjetParse>();

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var parts = charge.Split('|');
        if (parts.Length < 4) return;

        if (int.TryParse(parts[0], out var id)) Identifiant = id;
        Nom = parts[1];
        if (int.TryParse(parts[2], out var niv)) Niveau = niv;
        if (int.TryParse(parts[3], out var cl)) IdClasse = cl;

        // Format Hystoria : <id>|<nom>|<niv>|<classe>|<sexe>|<align>|<coul1>|<coul2>|<coul3>|<items>
        // L'index exact des items varie. On scanne tous les parts à la recherche de la section
        // items (reconnaissable par le motif `~` qui sépare les champs internes de chaque item,
        // contrairement aux couleurs qui sont juste hex sans `~`).
        var liste = new List<VersClient.Objet.ObjetParse>();
        foreach (var part in parts.Skip(4)) // skip id/nom/niv/classe
        {
            if (!part.Contains('~')) continue; // section pas-items (sexe, couleurs, etc.)

            foreach (var item in part.Split(';', System.StringSplitOptions.RemoveEmptyEntries))
            {
                var ip = item.Split('~');
                if (ip.Length < 4) continue;
                // Hystoria peut préfixer l'UID d'un 'O' littéral (cf. OAK).
                var uidBrut = ip[0];
                if (uidBrut.Length > 1 && !EstHex(uidBrut[0])) uidBrut = uidBrut[1..];
                // UID = long (dépasse Int32 : ex. 0x1daa081ab). Ancien int
                // → overflow → idObj=0 → inventaire initial JAMAIS chargé.
                var idObj = ParseHex(uidBrut);
                var tpl = (int)ParseHex(ip[1]);
                var qty = (int)ParseHex(ip[2]);
                var pos = ip[3].Length > 0 ? (int)ParseHex(ip[3]) : 63;
                if (idObj > 0 && tpl > 0)
                    liste.Add(new VersClient.Objet.ObjetParse(idObj, tpl, qty == 0 ? 1 : qty, pos));
            }
            break; // une seule section items dans le paquet
        }
        ObjetsInitiaux = liste;
    }

    private static bool EstHex(char c)
        => c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');

    private static long ParseHex(string s)
        => long.TryParse(s, System.Globalization.NumberStyles.HexNumber,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;
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
    /// <summary>Caracs (total) par statId AB : 10=Vita 11=Sag 12=For 13=Int 14=Cha 15=Agi.</summary>
    public System.Collections.Generic.Dictionary<int, int> Caracteristiques { get; } = new();
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var blocs = charge.Split('|');

        // Diagnostic : log les 12 premiers blocs pour pouvoir corriger la position des champs
        // (Vie/Énergie/PA/PM diffèrent entre Dofus officiel et Hystoria).
        var apercu = string.Join(" | ", blocs.Take(12).Select((b, i) =>
            $"[{i}]={(b.Length > 30 ? b[..30] + "…" : b)}"));
        Utilitaires.Journaux.Journaliseur.Debogue($"[As] blocs : {apercu}");

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

        // Hystoria : le bloc Kamas peut avoir un suffixe "#<bonusKolizeum>" — on ne garde que la partie numérique.
        Kamas = ParserLongAvant(blocs, 1, '#');
        PointsCaracteristiques = ParserInt(blocs, 2);
        PointsSorts = ParserInt(blocs, 3);

        // Format Hystoria observé en debug live (paquet As réel) :
        //   blocs[0]=XP, blocs[1]=kamas#bonus, blocs[2]=pCaracs, blocs[3]=pSorts,
        //   blocs[4]=alignement (ignoré), blocs[5]=vie/vieMax, blocs[6]=energie/energieMax,
        //   blocs[7]=initiative, blocs[8]=prospection,
        //   blocs[9]=PA,PM,?,? (ex: "7,5,0,0")
        (Vie, VieMax) = ParserPaire(blocs, 5);
        (Energie, EnergieMax) = ParserPaire(blocs, 6);
        // Stats Dofus 1.29 : chaque stat a 4 composants "base,equipement,potion,boost".
        // Le total affiché = somme des 4. Hystoria garde ce format.
        //   blocs[9] = PA (ex: "7,5,0,0" → 12 PA total)
        //   blocs[10] = PM (ex: "3,1,0,0" → 4 PM total)
        PA = SommeStat(blocs, 9);
        PM = SommeStat(blocs, 10);

        // Caracs Dofus 1.29 : après PA(9)/PM(10) viennent les 6 stats, chacune
        // "base,equip,don,boost" (total = somme). Ordre observé live (Abrak) :
        //   [11]=Vitalité [12]=Sagesse [13]=Force [14]=Intelligence
        //   [15]=Chance   [16]=Agilité  → statId AB 10..15.
        for (int s = 0; s < 6; s++)
        {
            int total = SommeStat(blocs, 11 + s);
            Caracteristiques[10 + s] = total;
        }
    }

    private static int SommeStat(string[] blocs, int index)
    {
        if (blocs.Length <= index) return 0;
        var total = 0;
        foreach (var p in blocs[index].Split(','))
        {
            if (int.TryParse(p, out var v)) total += v;
        }
        return total;
    }

    private static long ParserLongAvant(string[] blocs, int index, char separateur)
    {
        if (blocs.Length <= index) return 0;
        var brut = blocs[index];
        var idx = brut.IndexOf(separateur);
        var partie = idx >= 0 ? brut[..idx] : brut;
        return long.TryParse(partie, out var v) ? v : 0;
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

/// <summary>
/// SL : liste des sorts du personnage. Format :
/// <c>SL&lt;idSort&gt;~&lt;niveau&gt;~&lt;position&gt;;…</c> (position -1 = pas
/// sur la barre). Permet de scanner les sorts appris + leur niveau.
/// </summary>
public sealed class MessageListeSorts : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "SL";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;

    /// <summary>idSort → niveau.</summary>
    public Dictionary<int, int> Sorts { get; } = new();

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        // Certains serveurs préfixent d'un 'K' (compression Abrak) : on saute
        // tout caractère non-chiffre/'-' de tête avant la 1re entrée.
        foreach (var entree in charge.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var p = entree.Split('~');
            if (p.Length < 2) continue;
            var sid = new string(p[0].Where(c => char.IsDigit(c) || c == '-').ToArray());
            if (int.TryParse(sid, out var idSort)
                && int.TryParse(p[1], out var niveau) && idSort > 0)
            {
                Sorts[idSort] = niveau;
            }
        }
    }
}
