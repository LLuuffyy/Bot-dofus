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

    /// <summary>
    /// Seuil pods (%) déclenchant le trajet <c>marchand()</c> (vente items
    /// équipements au PNJ). Si ≤ 0, désactivé. Lu depuis le global Lua
    /// <c>MARCHAND_SEUIL_PODS = 80</c>.
    /// </summary>
    public int MarchandSeuilPods { get; set; } = 0;

    /// <summary>
    /// Si <c>true</c>, après chaque combat le bot reste sur la map et farme
    /// les autres groupes de mobs avant de passer à l'étape suivante du
    /// mouvement. Lu depuis le global Lua <c>FORCE_FIGHT = true</c>.
    /// </summary>
    public bool ForceFight { get; set; } = false;

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
