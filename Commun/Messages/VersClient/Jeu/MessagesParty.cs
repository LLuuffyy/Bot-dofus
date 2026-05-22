using System;
using System.Collections.Generic;
using System.Linq;
using BotDofus.Commun.Reseau;

namespace BotDofus.Commun.Messages.VersClient.Jeu;

/// <summary>
/// PM : Party Members — liste / mise à jour des membres du groupe.
/// Format observé sur Abrak (mode héros, log 095820) :
///   <c>PM+401770;Beiloddurul;101;-1;-1;-1;,9aa,9a9,,;195,195;21;295;105;1|+401774;Aerawiol;31;-1;-1;-1;,,,,;90,90;8;30;123;1</c>
/// Soit pour chaque bloc séparé par <c>|</c> :
/// <c>&lt;op&gt;&lt;id&gt;;&lt;nom&gt;;&lt;skin&gt;;&lt;c1&gt;;&lt;c2&gt;;&lt;c3&gt;;&lt;couleurs&gt;;&lt;pv&gt;,&lt;pvMax&gt;;&lt;niveau&gt;;&lt;init&gt;;&lt;prospec&gt;;&lt;sexe&gt;</c>
/// où <c>op</c> = <c>+</c> (ajout), <c>-</c> (retrait), absent (update).
/// </summary>
public sealed class MessagePartyMembres : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "PM";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;

    public readonly record struct Membre(
        char Operation, int Id, string Nom, int Skin, int Pv, int PvMax, int Niveau)
    {
        /// <summary>
        /// Identifiant de classe Dofus Retro déduit du skin (skin = idClasse*10 + sexe).
        /// Ex. skin 101 → classe 10 (Sadida), skin 31 → classe 3 (Enutrof).
        /// </summary>
        public int IdClasse => Skin / 10;
    }

    public List<Membre> Membres { get; } = new();

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        foreach (var bloc in charge.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            if (bloc.Length < 2) continue;
            char op;
            string corps;
            if (bloc[0] == '+' || bloc[0] == '-')
            {
                op = bloc[0];
                corps = bloc[1..];
            }
            else
            {
                op = '~'; // update
                corps = bloc;
            }
            var champs = corps.Split(';');
            if (!int.TryParse(champs.ElementAtOrDefault(0), out var id)) continue;
            var nom = champs.ElementAtOrDefault(1) ?? string.Empty;
            int.TryParse(champs.ElementAtOrDefault(2), out var skin);
            int pv = 0, pvMax = 0;
            var pvBloc = champs.ElementAtOrDefault(7) ?? string.Empty;
            var pvParts = pvBloc.Split(',');
            if (pvParts.Length >= 2)
            {
                int.TryParse(pvParts[0], out pv);
                int.TryParse(pvParts[1], out pvMax);
            }
            int.TryParse(champs.ElementAtOrDefault(8), out var niveau);
            Membres.Add(new Membre(op, id, nom, skin, pv, pvMax, niveau));
        }
    }
}

/// <summary>
/// PL : Party Leader — id du leader courant. Format simple : <c>PL&lt;idLeader&gt;</c>.
/// </summary>
public sealed class MessagePartyLeader : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "PL";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public int IdLeader { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        int.TryParse(charge, out var id);
        IdLeader = id;
    }
}

/// <summary>
/// PI : Party Invite — confirmations / erreurs d'invitation côté serveur.
/// Forme observée : <c>PIEa</c> (erreur "absent" ?), <c>PIE&lt;code&gt;</c>.
/// </summary>
public sealed class MessagePartyInvitation : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "PI";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public string Code { get; private set; } = string.Empty;
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        Code = charge;
    }
}

/// <summary>
/// PCK : Party Check — accusé de réception d'invitation (S→C).
/// Format : <c>PCK&lt;nomMaster&gt;</c>.
/// </summary>
public sealed class MessagePartyCheck : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "PCK";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public string NomMaster { get; private set; } = string.Empty;
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        NomMaster = charge;
    }
}

/// <summary>
/// Nh (S→C) : liste des sorts d'un héros lié (réponse à <c>Nh&lt;id&gt;</c> +
/// <c>Ns&lt;id&gt;</c> envoyés par le client en mode héros, log 101039 sec 10:11:58).
/// Format observé : <c>Nh401774|49~1~-1;51~4~3;41~1~1;42~1~-1;43~1~2;</c>
/// soit <c>Nh&lt;idHeros&gt;|&lt;sortId&gt;~&lt;niveau&gt;~&lt;posBarre&gt;;...</c>
/// (posBarre = -1 si pas dans la barre de sorts).
/// </summary>
public sealed class MessageHerosSorts : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "Nh";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;

    public int IdHeros { get; private set; }
    /// <summary>Sorts appris : (id → niveau).</summary>
    public Dictionary<int, int> Sorts { get; } = new();
    /// <summary>Position dans la barre de sorts (id → pos, -1 = hors barre).</summary>
    public Dictionary<int, int> PositionsBarre { get; } = new();

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        // Format : "<id>|<sortId>~<niv>~<pos>;<sortId>~<niv>~<pos>;..."
        var sep = charge.IndexOf('|');
        if (sep < 0)
        {
            // Pas de payload : c'est juste "<id>" (= Nh client sans corps,
            // observé en C→S ; côté S→C on doit avoir le pipe).
            int.TryParse(charge, out var idSeul);
            IdHeros = idSeul;
            return;
        }
        if (!int.TryParse(charge[..sep], out var id)) return;
        IdHeros = id;
        var corps = charge[(sep + 1)..];
        foreach (var bloc in corps.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var champs = bloc.Split('~');
            if (champs.Length < 2) continue;
            if (!int.TryParse(champs[0], out var sortId)) continue;
            int.TryParse(champs[1], out var niveau);
            int pos = -1;
            if (champs.Length >= 3) int.TryParse(champs[2], out pos);
            Sorts[sortId] = niveau;
            PositionsBarre[sortId] = pos;
        }
    }
}
