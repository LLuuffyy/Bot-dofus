using System.Collections.Generic;

namespace BotDofus.Divers.Scripts;

/// <summary>
/// Représentation C# d'un script Lua chargé : configuration, étapes de mouvement,
/// étapes de banque. Le fichier source reste accessible via <see cref="CheminFichier"/>.
/// </summary>
public sealed class ScriptCharge
{
    public string CheminFichier { get; init; } = string.Empty;
    public string Nom { get; init; } = string.Empty;

    public ConfigurationScript Configuration { get; init; } = new();
    public IReadOnlyList<EtapeScript> EtapesMouvement { get; init; } = new List<EtapeScript>();
    public IReadOnlyList<EtapeScript> EtapesBanque { get; init; } = new List<EtapeScript>();

    public override string ToString() => $"Script « {Nom} » ({EtapesMouvement.Count} étapes, {EtapesBanque.Count} banque)";
}
