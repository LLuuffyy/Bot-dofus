namespace BotDofus.Divers.Combats.IA;

/// <summary>
/// Profil de comportement de l'IA en combat. Choisi par l'utilisateur via
/// la config du compte ou via un setter du script Lua.
/// </summary>
public enum StrategieCombat
{
    /// <summary>Maximise les dégâts, ignore le positionnement défensif.</summary>
    Agressif,

    /// <summary>Cherche à rester à distance, utilise la portée maximum.</summary>
    Tactique,

    /// <summary>Privilégie la survie : kite, soigne, fuit en zone safe.</summary>
    Defensif,

    /// <summary>Soutien : soigne et boost les alliés en priorité.</summary>
    Soutien,

    /// <summary>Aucune décision automatique — passe le tour à chaque fois.</summary>
    Passif,

    /// <summary>Reste loin, fuit si engagé (= dyshay FUGITIVA).</summary>
    Fugitif
}
