using System;
using BotDofus.Commun.Reseau;

namespace BotDofus.Divers.Interception;

/// <summary>
/// Action décidée par une règle d'interception pour un paquet donné.
/// </summary>
public enum ActionInterception
{
    /// <summary>Laisser passer le paquet inchangé.</summary>
    Laisser,

    /// <summary>Remplacer le contenu du paquet par <see cref="ResultatInterception.NouveauContenu"/>.</summary>
    Remplacer,

    /// <summary>Supprimer le paquet (ne jamais le transmettre au destinataire).</summary>
    Supprimer,
}

/// <summary>Résultat de l'évaluation d'une règle.</summary>
public readonly record struct ResultatInterception(ActionInterception Action, string? NouveauContenu)
{
    public static readonly ResultatInterception Laisser = new(ActionInterception.Laisser, null);
    public static ResultatInterception Remplacer(string contenu) => new(ActionInterception.Remplacer, contenu);
    public static readonly ResultatInterception Supprimer = new(ActionInterception.Supprimer, null);
}

/// <summary>
/// Une règle d'interception : si le paquet (et sa direction) matche, le transformateur
/// produit l'action à appliquer.
///
/// Conçue pour le mode furtif : la transformation se fait INLINE dans le flux du vrai
/// client Dofus, donc le timing, les keep-alives et les checksums restent ceux du client
/// officiel. Le serveur ne voit aucune anomalie de séquence.
/// </summary>
public sealed class RegleInterception
{
    /// <summary>Nom lisible (affiché dans l'UI / les logs).</summary>
    public string Nom { get; set; } = "règle";

    /// <summary>true = règle prise en compte ; false = ignorée (toggle UI).</summary>
    public bool Active { get; set; } = true;

    /// <summary>Ne matcher que cette direction. null = les deux sens.</summary>
    public DirectionPaquet? Direction { get; set; }

    /// <summary>Préfixe requis du paquet (ex. "GA", "GKK"). Vide = pas de filtre préfixe.</summary>
    public string Prefixe { get; set; } = string.Empty;

    /// <summary>
    /// Nombre maximum d'applications (0 = illimité). Utile pour un one-shot
    /// (ex. modifier le PREMIER déplacement uniquement).
    /// </summary>
    public int MaxApplications { get; set; }

    /// <summary>Compteur d'applications effectives (lecture seule pour l'UI).</summary>
    public int NombreApplications { get; private set; }

    /// <summary>
    /// Transformateur : reçoit le contenu clair + la direction, renvoie l'action.
    /// Doit être pur et rapide (exécuté dans la boucle relai).
    /// </summary>
    public Func<string, DirectionPaquet, ResultatInterception> Transformateur { get; set; }
        = (_, _) => ResultatInterception.Laisser;

    public bool Correspond(string contenu, DirectionPaquet direction)
    {
        if (!Active) return false;
        if (Direction.HasValue && Direction.Value != direction) return false;
        if (!string.IsNullOrEmpty(Prefixe) && !contenu.StartsWith(Prefixe, StringComparison.Ordinal)) return false;
        if (MaxApplications > 0 && NombreApplications >= MaxApplications) return false;
        return true;
    }

    public ResultatInterception Appliquer(string contenu, DirectionPaquet direction)
    {
        var res = Transformateur(contenu, direction);
        if (res.Action != ActionInterception.Laisser)
        {
            NombreApplications++;
        }
        return res;
    }

    public void ReinitialiserCompteur() => NombreApplications = 0;
}
