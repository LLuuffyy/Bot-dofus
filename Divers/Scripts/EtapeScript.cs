using System.Collections.Generic;

namespace BotDofus.Divers.Scripts;

/// <summary>
/// Une étape d'un script de trajectoire Lua : ce que le bot doit faire
/// sur une carte donnée (changer de carte, parler à un PNJ, combattre...).
/// Compatible avec le format généré par SynFus Script Recorder.
/// </summary>
public sealed class EtapeScript
{
    /// <summary>Identifiant de carte (stocké en string car les scripts le passent ainsi).</summary>
    public string IdentifiantCarte { get; init; } = string.Empty;

    /// <summary>Direction de sortie si transition de carte ("TOP", "BOTTOM", "LEFT", "RIGHT").</summary>
    public string? Direction { get; init; }

    /// <summary>Identifiant du modèle de PNJ à interpeller.</summary>
    public int? IdentifiantPNJ { get; init; }

    /// <summary>Séquence de réponses dans le dialogue du PNJ (-1 = première option).</summary>
    public List<int>? ReponsesDialogue { get; init; }

    /// <summary>Numéro de cellule cible sur la carte (déplacement précis).</summary>
    public int? CelluleCible { get; init; }

    /// <summary>Indique que la carte comporte un combat à engager (1 seul combat puis passe à l'étape suivante).</summary>
    public bool EngagerCombat { get; init; }

    /// <summary>
    /// Boucle de combats sur la même map : tant qu'il reste un groupe
    /// attaquable, on engage en cascade. Pratique pour les zones de farm
    /// répétitif. Activé via <c>forcefight = true</c> dans l'étape Lua.
    /// </summary>
    public bool ForcerFightBoucle { get; init; }

    /// <summary>Indique qu'on doit passer à la banque (PNJ banquier ou phénix).</summary>
    public bool UtiliserBanque { get; init; }

    /// <summary>
    /// Indique qu'on doit vendre au PNJ marchand sur cette étape.
    /// Si <see cref="IdentifiantPNJ"/> est aussi set, c'est l'id du marchand.
    /// </summary>
    public bool UtiliserMarchand { get; init; }

    public override string ToString()
    {
        var verbe = EngagerCombat ? "combat"
                 : UtiliserBanque ? "banque"
                 : UtiliserMarchand ? "marchand"
                 : IdentifiantPNJ.HasValue ? $"PNJ #{IdentifiantPNJ}"
                 : CelluleCible.HasValue ? $"cellule {CelluleCible}"
                 : Direction is not null ? $"sortie {Direction}"
                 : "?";
        return $"Carte {IdentifiantCarte} → {verbe}";
    }
}
