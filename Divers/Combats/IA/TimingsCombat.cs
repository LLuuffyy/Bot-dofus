namespace BotDofus.Divers.Combats.IA;

/// <summary>
/// Helper centralisé des délais utilisés par l'IA combat. Permet d'activer
/// un MODE TURBO (délais réduits au minimum) UNIQUEMENT pendant un combat,
/// sans toucher aux délais overworld (récolte, zaap, déplacement carte) qui
/// doivent rester humanisés pour ne pas trigger l'anti-bot Hystoria.
///
/// <para>
/// 2 niveaux d'accélération :
/// <list type="bullet">
///   <item><b>Turbo standard</b> (<see cref="Turbo"/>=true) : tous les délais
///   passent à <see cref="MsTurbo"/> ms (25 ms). Réduit ~75% du temps mort.</item>
///   <item><b>Ultra turbo</b> (<see cref="UltraTurbo"/>=true) : tous les délais
///   passent à <see cref="MsUltraTurbo"/> ms (5 ms). Combat 2x plus rapide
///   encore, mais risque accru de désync serveur si latence haute.</item>
/// </list>
/// </para>
///
/// <para>
/// Activation : <see cref="ConfigCombat.TurboCombat"/> = <c>true</c>.
/// L'<see cref="UltraTurbo"/> est activé en plus si la valeur de <c>TurboCombat</c>
/// est true ET que l'env var <c>ULTRA_TURBO</c> est définie (mode expert).
/// </para>
///
/// <para>
/// ⚠️ Ce flag est <b>process-wide</b> (static). En multi-perso (master + héros
/// liés) c'est intentionnel : tous partagent le même combat à un moment T,
/// donc tous doivent être en turbo ou pas. La config <see cref="ConfigCombat.TurboCombat"/>
/// du master pilote le flag global (les héros héritent).
/// </para>
/// </summary>
public static class TimingsCombat
{
    /// <summary>
    /// Si <c>true</c>, tous les <see cref="Delai"/> retournent <see cref="MsTurbo"/>
    /// au lieu d'un délai humanisé. Reset automatiquement à la fin de combat.
    /// </summary>
    public static bool Turbo { get; set; }

    /// <summary>Si <c>true</c>, encore plus rapide : délais à <see cref="MsUltraTurbo"/> ms.
    /// Activé via <see cref="ConfigCombat.UltraTurboCombat"/>.</summary>
    public static bool UltraTurbo { get; set; }

    /// <summary>Délai « turbo standard » : 25 ms. Compromis qui marche sur Hystoria
    /// sans désync TCP (test forensic 2026-05-22).</summary>
    public const int MsTurbo = 25;

    /// <summary>Délai « ultra turbo » : 5 ms. Limite physique TCP localhost.
    /// Risque de paquets chevauchés sur connexion à haute latence.</summary>
    public const int MsUltraTurbo = 5;

    /// <summary>
    /// Délai humanisé entre <c>minMs</c> et <c>maxMs</c>, ou réduit selon
    /// les flags <see cref="Turbo"/> / <see cref="UltraTurbo"/>.
    /// </summary>
    public static int Delai(int minMs, int maxMs)
    {
        if (UltraTurbo) return MsUltraTurbo;
        if (Turbo) return MsTurbo;
        return System.Random.Shared.Next(minMs, maxMs);
    }

    /// <summary>
    /// Variante : délai fixe <c>normalMs</c> réduit selon les flags actifs.
    /// </summary>
    public static int DelaiFixe(int normalMs)
    {
        if (UltraTurbo) return MsUltraTurbo;
        if (Turbo) return MsTurbo;
        return normalMs;
    }

    /// <summary>
    /// Active le mode turbo / ultra turbo selon la config. Idempotent.
    /// À appeler en début de tour IA.
    /// </summary>
    public static void AppliquerConfig(ConfigCombat? cfg)
    {
        Turbo = cfg?.TurboCombat == true;
        UltraTurbo = cfg?.UltraTurboCombat == true && Turbo;
    }

    /// <summary>Reset (fin de combat). À câbler sur <c>Combat.EtatChange == Inactif</c>.</summary>
    public static void Reset()
    {
        Turbo = false;
        UltraTurbo = false;
    }
}
