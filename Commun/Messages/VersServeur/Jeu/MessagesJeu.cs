using BotDofus.Commun.Reseau;

namespace BotDofus.Commun.Messages.VersServeur.Jeu;

// =====================================================================
// VersServeur / Jeu
// Actions que le bot envoie au serveur durant le gameplay.
// =====================================================================

/// <summary>
/// GR : signale que le client est prêt en combat (avant placement) ou
/// annule. Capture user passif 16:21:42 → « GR1 » (avec 1, pas K). L'ancien
/// « GRK »/« GRF » était une déduction des docs Dofus 2.0, FAUX sur Retro 1.29
/// Hystoria : le serveur attend « GR1 » (prêt) ou « GR0 » (annule).
/// </summary>
public sealed class MessageJeuPret : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "GR";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public bool Pret { get; set; } = true;

    public override void Desserialiser(string charge) { Charge = charge; }
    public override string Serialiser() => Prefixe + (Pret ? "1" : "0");
}

/// <summary>
/// GA : action de jeu (déplacement, lancer sort, utiliser objet...).
/// Le sous-code à 3 caractères qualifie l'action. Sous-codes observés :
///   001 : déplacement → paramètre = chemin compressé MapPoint (ex. "deQcgEdhi")
///   300 : lancer un sort → paramètre = "&lt;idSort&gt;;&lt;celluleCible&gt;" (à valider)
/// </summary>
public sealed class MessageJeuAction : MessageDofus, IMessageVersServeur
{
    public const string SousCodeDeplacement = "001";
    public const string SousCodeLancerSort = "300";

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

    /// <summary>Construit une action de déplacement à partir d'un chemin compressé.</summary>
    public static MessageJeuAction Deplacement(string cheminCompresse)
        => new() { SousCode = SousCodeDeplacement, Parametres = cheminCompresse };

    /// <summary>Construit une action de lancer de sort.</summary>
    public static MessageJeuAction LancerSort(int idSort, int celluleCible)
        => new() { SousCode = SousCodeLancerSort, Parametres = $"{idSort};{celluleCible}" };
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
    // Capture user 12:41:18 (combat manuel) : « Gt » (g minuscule, t minuscule)
    // = pass turn. L'ancien préfixe « GE » était faux (Game Event peut-être ?)
    // — n'aurait JAMAIS passé le tour côté serveur, le bot serait resté bloqué
    // à tourner en rond. Corrigé via la capture du combat manuel utilisateur.
    public override string Prefixe => "Gt";
    public override DirectionPaquet Direction => DirectionPaquet.VersServeur;
    public override void Desserialiser(string charge) { Charge = charge; }
}

/// <summary>
/// Gp : se placer sur une case de départ avant combat. Capture user passif
/// 16:22:11 → « Gp299 » (g majuscule + p MINUSCULE). L'ancien « GP » (deux
/// majuscules) venait des docs Dofus 2.0 et était FAUX sur Retro 1.29
/// Hystoria — le serveur rejette « GP299 ». Casse-tête classique du proto
/// Dofus : chaque commande a sa propre convention (Gt minuscule, GR
/// majuscule, Gp mixte).
/// </summary>
public sealed class MessageJeuPosition : MessageDofus, IMessageVersServeur
{
    public override string Prefixe => "Gp";
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
