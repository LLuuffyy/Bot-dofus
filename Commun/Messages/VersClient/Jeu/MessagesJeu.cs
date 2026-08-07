using System;
using System.Collections.Generic;
using BotDofus.Commun.Reseau;
using BotDofus.Utilitaires.Crypto;

namespace BotDofus.Commun.Messages.VersClient.Jeu;

// =====================================================================
// VersClient / Jeu
// Messages cœur du gameplay : carte, combat, action, tour.
// =====================================================================

/// <summary>
/// GDM : données de carte. Format observé sur Hystoria :
/// <c>GDM|&lt;mapId&gt;|&lt;dateVersion&gt;|&lt;hexDataChiffre&gt;</c>
/// La <c>dateVersion</c> sert de seed à l'algorithme de décryption Ankama
/// qui produit la liste des cellules (mouv, layer, los) en clair.
/// </summary>
public sealed class MessageDonneesCarte : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "GDM";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;

    public int IdentifiantCarte { get; private set; }
    public string DateVersion { get; private set; } = string.Empty;
    public string ClefCarte { get; private set; } = string.Empty;

    /// <summary>Alias de <see cref="ClefCarte"/> pour retro-compat.</summary>
    public string Clef => ClefCarte;
    public string DonneesChiffrees => ClefCarte;

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        // La charge commence souvent par '|' (entre préfixe et premier champ).
        var bloc = charge.StartsWith('|') ? charge[1..] : charge;
        var parts = bloc.Split('|');
        if (parts.Length > 0 && int.TryParse(parts[0], out var id)) IdentifiantCarte = id;
        if (parts.Length > 1) DateVersion = parts[1];
        if (parts.Length > 2) ClefCarte = parts[2];
    }
}

/// <summary>GJ : confirmation de rejoindre le jeu (entrée sur une carte).</summary>
public sealed class MessageRejoindreJeu : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "GJ";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public bool EstEnCombat { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        EstEnCombat = charge.StartsWith("1");
    }
}

/// <summary>GP : positions initiales avant un combat (placement des challengers).</summary>
public sealed class MessagePositionsCombat : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "GP";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public IReadOnlyList<int> PositionsDisponibles { get; private set; } = Array.Empty<int>();
    public IReadOnlyList<int> PositionsEquipe1 { get; private set; } = Array.Empty<int>();
    public IReadOnlyList<int> PositionsEquipe2 { get; private set; } = Array.Empty<int>();
    public int EquipeCourante { get; private set; } = -1;

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var bloc = charge.StartsWith('|') ? charge[1..] : charge;
        var parts = bloc.Split('|');
        if (parts.Length >= 2)
        {
            PositionsEquipe1 = DecoderCellules(parts[0]);
            PositionsEquipe2 = DecoderCellules(parts[1]);
            if (parts.Length > 2 && int.TryParse(parts[2], out var equipe)) EquipeCourante = equipe;
            PositionsDisponibles = EquipeCourante == 1 ? PositionsEquipe2 : PositionsEquipe1;
            return;
        }

        var liste = new List<int>();
        foreach (var s in bloc.Split(','))
            if (int.TryParse(s, out var c)) liste.Add(c);
        PositionsDisponibles = liste;
    }

    private static IReadOnlyList<int> DecoderCellules(string encoded)
    {
        if (string.IsNullOrEmpty(encoded)) return Array.Empty<int>();
        var liste = new List<int>(encoded.Length / 2);
        for (int i = 0; i + 1 < encoded.Length; i += 2)
        {
            var a = HashCarte.IndexCar(encoded[i]);
            var b = HashCarte.IndexCar(encoded[i + 1]);
            if (a < 0 || b < 0) continue;
            liste.Add((a << 6) + b);
        }
        return liste;
    }
}

/// <summary>GS : démarrage effectif du combat.</summary>
public sealed class MessageDebutCombat : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "GS";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public override void Desserialiser(string charge) { Charge = charge; }
}

/// <summary>GE : fin du combat (résultat, gains).</summary>
public sealed class MessageFinCombat : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "GE";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public override void Desserialiser(string charge) { Charge = charge; }
}

/// <summary>GT : tour d'un combattant (qui doit jouer maintenant).</summary>
public sealed class MessageTourCombat : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "GT";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public int IdentifiantCombattant { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        int.TryParse(charge, out var v);
        IdentifiantCombattant = v;
    }
}

/// <summary>GA : action de jeu (déplacement, sort, passer tour, etc.). Le sous-code après GA qualifie le type.</summary>
public sealed class MessageActionJeu : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "GA";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public string SousCode { get; private set; } = string.Empty;
    public string Parametres { get; private set; } = string.Empty;
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        if (charge.Length > 0) SousCode = charge[..Math.Min(3, charge.Length)];
        if (charge.Length > SousCode.Length) Parametres = charge[SousCode.Length..];
    }
}

/// <summary>GR : prêt (réponse à une synchronisation).</summary>
public sealed class MessagePret : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "GR";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public override void Desserialiser(string charge) { Charge = charge; }
}

/// <summary>GC : création de jeu (démarrage du mode combat côté serveur).</summary>
public sealed class MessageCreationJeu : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "GC";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public override void Desserialiser(string charge) { Charge = charge; }
}
