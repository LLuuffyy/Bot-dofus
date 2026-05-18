using System;
using System.Collections.Generic;
using BotDofus.Commun.Reseau;

namespace BotDofus.Commun.Messages.VersClient.Objet;

// =====================================================================
// VersClient / Objet
// Mouvements dans l'inventaire, mise à jour de poids, ajout/retrait
// d'objets, changement d'équipement.
// =====================================================================

/// <summary>OAK : un ou plusieurs objets sont ajoutés à l'inventaire.</summary>
public sealed class MessageObjetAjout : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "OAK";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public IReadOnlyList<string> BlocsObjets { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<ObjetParse> ObjetsParse { get; private set; } = Array.Empty<ObjetParse>();

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        // Hystoria : blocs séparés par ';' (parfois '|'). Chaque bloc :
        //   <id_hex>~<template_hex>~<qty_hex>~<pos_hex>~<effets...>
        BlocsObjets = charge.Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries);

        var liste = new List<ObjetParse>(BlocsObjets.Count);
        foreach (var bloc in BlocsObjets)
        {
            var parts = bloc.Split('~');
            if (parts.Length < 4) continue;

            // IDs en hexa, qty/pos parfois aussi.
            var id = ParseHex(parts[0]);
            var template = ParseHex(parts[1]);
            var qty = ParseHex(parts[2]);
            var pos = parts[3].Length > 0 ? ParseHex(parts[3]) : 63;

            if (id <= 0 || template <= 0) continue;
            liste.Add(new ObjetParse(id, template, qty == 0 ? 1 : qty, pos));
        }
        ObjetsParse = liste;
    }

    private static int ParseHex(string s)
        => int.TryParse(s, System.Globalization.NumberStyles.HexNumber,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;
}

/// <summary>Représentation parsée d'un objet d'inventaire (info de base).</summary>
public readonly record struct ObjetParse(int Identifiant, int IdTemplate, int Quantite, int Position);

/// <summary>OR : retrait d'un objet de l'inventaire (id).</summary>
public sealed class MessageObjetRetrait : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "OR";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public int IdentifiantObjet { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        int.TryParse(charge, out var id);
        IdentifiantObjet = id;
    }
}

/// <summary>Oq : changement de quantité d'un objet (id,nouvelleQte).</summary>
public sealed class MessageObjetQuantite : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "Oq";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public int IdentifiantObjet { get; private set; }
    public int NouvelleQuantite { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var parts = charge.Split('|');
        if (parts.Length > 0 && int.TryParse(parts[0], out var id)) IdentifiantObjet = id;
        if (parts.Length > 1 && int.TryParse(parts[1], out var q)) NouvelleQuantite = q;
    }
}

/// <summary>Ow : poids actuel et max du personnage. Format Hystoria : "Ow<actuel>|<max>".</summary>
public sealed class MessageObjetPoids : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "Ow";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public int PoidsActuel { get; private set; }
    public int PoidsMax { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        // Format Hystoria : "Ow<charId>;<actuel>|<max>" (ex. Ow401770;812|25545).
        // On retire d'abord l'éventuel "<id>;" de tête (avant : actuel restait 0).
        var pv = charge.IndexOf(';');
        if (pv >= 0) charge = charge[(pv + 1)..];
        var parts = charge.Split('|');
        if (parts.Length > 0 && int.TryParse(parts[0], out var a)) PoidsActuel = a;
        if (parts.Length > 1 && int.TryParse(parts[1], out var m)) PoidsMax = m;
    }
}

/// <summary>OM : déplacement d'un objet (id,nouvelleCase).</summary>
public sealed class MessageObjetDeplacement : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "OM";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public int IdentifiantObjet { get; private set; }
    public int CasePosition { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var parts = charge.Split('|');
        if (parts.Length > 0 && int.TryParse(parts[0], out var id)) IdentifiantObjet = id;
        if (parts.Length > 1 && int.TryParse(parts[1], out var p)) CasePosition = p;
    }
}
