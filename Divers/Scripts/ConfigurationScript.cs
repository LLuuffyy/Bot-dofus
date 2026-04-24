using System.Collections.Generic;

namespace BotDofus.Divers.Scripts;

/// <summary>
/// Options globales déclarées au sommet d'un script Lua
/// (SHOW_FIGHT_COUNTER, MAX_PODS, AUTO_REGEN, DUNGEON_MAPS, SOUL_CAPTURE...).
/// </summary>
public sealed class ConfigurationScript
{
    public bool AfficherCompteurCombats { get; set; } = true;
    public int PodsMax { get; set; } = 90;

    public RegenerationAuto? RegenerationAutomatique { get; set; }
    public List<int> CartesDonjon { get; set; } = new();
    public CaptureAme? CaptureAme { get; set; }
}

/// <summary>Paramètres de régénération automatique hors combat.</summary>
public sealed class RegenerationAuto
{
    public int PvMinimumPourcent { get; set; } = 50;
    public int PvMaximumPourcent { get; set; } = 90;
    public List<int> IdentifiantsObjetsSoins { get; set; } = new();
}

/// <summary>Paramètres de capture d'âme sur un boss.</summary>
public sealed class CaptureAme
{
    public int IdentifiantSort { get; set; }
    public int CarteCible { get; set; }
}
