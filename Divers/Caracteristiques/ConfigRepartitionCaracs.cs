namespace BotDofus.Divers.Caracteristiques;

/// <summary>
/// Mode de distribution automatique des points de caractéristique au level-up.
/// </summary>
public enum ModeDistribCaracs
{
    /// <summary>Aucune distribution auto. Le joueur dépense manuellement.</summary>
    Manuel = 0,
    /// <summary>Aperçu uniquement — log ce qui SERAIT distribué, sans envoi de paquet AB.</summary>
    Preview = 1,
    /// <summary>Distribution auto à chaque level-up détecté.</summary>
    Automatique = 2,
}

/// <summary>
/// Préset de répartition par classe (% par carac). Total normalisé à 100.
/// </summary>
public sealed class PresetClasseCaracs
{
    public string NomClasse { get; set; } = "";
    public int IdClasse { get; set; }
    public int PctVitalite { get; set; }
    public int PctSagesse { get; set; }
    public int PctForce { get; set; }
    public int PctIntelligence { get; set; }
    public int PctChance { get; set; }
    public int PctAgilite { get; set; }

    public int Total => PctVitalite + PctSagesse + PctForce + PctIntelligence + PctChance + PctAgilite;
}

/// <summary>
/// Config de la répartition automatique des caracs pour un perso, sérialisée
/// dans <c>peleas/&lt;perso&gt;.json</c> sous la clé <c>caracs</c>.
/// </summary>
public sealed class ConfigRepartitionCaracs
{
    public ModeDistribCaracs Mode { get; set; } = ModeDistribCaracs.Manuel;

    /// <summary>Pourcentages — somme doit valoir 100 (validation côté UI).</summary>
    public int PctVitalite { get; set; }
    public int PctSagesse { get; set; }
    public int PctForce { get; set; }
    public int PctIntelligence { get; set; }
    public int PctChance { get; set; }
    public int PctAgilite { get; set; }

    /// <summary>Délai humanisé entre 2 envois AB (ms). Évite signature anti-bot.</summary>
    public int DelaiEntreAbMs { get; set; } = 150;

    public int Total => PctVitalite + PctSagesse + PctForce + PctIntelligence + PctChance + PctAgilite;

    public bool EstValide => Total == 100 && Mode != ModeDistribCaracs.Manuel;

    /// <summary>
    /// Charge le préset standard pour une classe Dofus 1.29. Retourne null si
    /// aucun préset défini pour cette classe (l'utilisateur configurera manuellement).
    /// </summary>
    public static ConfigRepartitionCaracs? PresetParClasse(int idClasse)
    {
        // Source : meta-builds Dofus Retro 1.29 communauté + dyshay/SynFus.
        return idClasse switch
        {
            1 => new() { Mode = ModeDistribCaracs.Automatique, PctVitalite = 60, PctSagesse = 40 },                          // Feca tank
            2 => new() { Mode = ModeDistribCaracs.Automatique, PctIntelligence = 70, PctSagesse = 30 },                      // Osa
            3 => new() { Mode = ModeDistribCaracs.Automatique, PctChance = 80, PctSagesse = 20 },                            // Enutrof prospec
            4 => new() { Mode = ModeDistribCaracs.Automatique, PctAgilite = 100 },                                            // Sram
            5 => new() { Mode = ModeDistribCaracs.Automatique, PctChance = 80, PctSagesse = 20 },                            // Xelor
            6 => new() { Mode = ModeDistribCaracs.Automatique, PctForce = 100 },                                              // Ecaflip force
            7 => new() { Mode = ModeDistribCaracs.Automatique, PctIntelligence = 80, PctSagesse = 20 },                      // Eni
            8 => new() { Mode = ModeDistribCaracs.Automatique, PctForce = 100 },                                              // Iop
            9 => new() { Mode = ModeDistribCaracs.Automatique, PctAgilite = 100 },                                            // Cra agi
            10 => new() { Mode = ModeDistribCaracs.Automatique, PctIntelligence = 70, PctSagesse = 30 },                     // Sadida invoc/soin
            11 => new() { Mode = ModeDistribCaracs.Automatique, PctForce = 70, PctVitalite = 30 },                           // Sacri
            12 => new() { Mode = ModeDistribCaracs.Automatique, PctForce = 100 },                                             // Panda
            _ => null
        };
    }
}
