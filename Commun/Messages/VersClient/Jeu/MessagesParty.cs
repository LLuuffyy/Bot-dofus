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
        char Operation, int Id, string Nom, int Skin, int Pv, int PvMax, int Niveau);

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
