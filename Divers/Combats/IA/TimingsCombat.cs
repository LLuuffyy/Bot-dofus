namespace BotDofus.Divers.Combats.IA;

/// <summary>
/// Helper centralisé des délais utilisés par l'IA combat.
///
/// <para>
/// 2 sources de délais possibles :
/// <list type="number">
///   <item>NOUVEAU : <see cref="DelaisActifs"/> = <see cref="ConfigDelaisCombat"/>
///   complète (par catégorie : LancerSort, EntreDeuxSorts, PasserTour, etc.)
///   inspirée du panneau « Délais » SynFus. Permet à l'utilisateur de
///   tweaker finement chaque catégorie ou choisir un profil prédéfini.</item>
///   <item>LEGACY : <see cref="Turbo"/> / <see cref="UltraTurbo"/> flags
///   booléens qui mettent tous les délais à <see cref="MsTurbo"/> /
///   <see cref="MsUltraTurbo"/> ms. Toujours dispo en fallback si
///   <see cref="DelaisActifs"/>==null (anciennes configs).</item>
/// </list>
/// </para>
///
/// <para>
/// ⚠️ Ce contexte est <b>process-wide</b> (static). En multi-perso (master +
/// héros liés) c'est intentionnel : tous partagent le même combat à un
/// moment T, donc tous doivent utiliser les mêmes délais. La config du
/// master pilote le flag global (les héros héritent).
/// </para>
/// </summary>
public static class TimingsCombat
{
    /// <summary>Config détaillée des délais (si null → fallback Turbo/UltraTurbo legacy).</summary>
    public static ConfigDelaisCombat? DelaisActifs { get; set; }

    // ============ FALLBACK LEGACY (avant ConfigDelaisCombat) ============
    public static bool Turbo { get; set; }
    public static bool UltraTurbo { get; set; }
    public const int MsTurbo = 25;
    public const int MsUltraTurbo = 5;

    /// <summary>
    /// Délai humanisé entre <c>minMs</c> et <c>maxMs</c>, ou réduit selon
    /// les flags <see cref="Turbo"/> / <see cref="UltraTurbo"/> (fallback
    /// si <see cref="DelaisActifs"/> n'est pas défini).
    /// </summary>
    public static int Delai(int minMs, int maxMs)
    {
        if (UltraTurbo) return MsUltraTurbo;
        if (Turbo) return MsTurbo;
        return System.Random.Shared.Next(minMs, maxMs);
    }

    /// <summary>Délai fixe normalMs réduit selon les flags actifs.</summary>
    public static int DelaiFixe(int normalMs)
    {
        if (UltraTurbo) return MsUltraTurbo;
        if (Turbo) return MsTurbo;
        return normalMs;
    }

    /// <summary>
    /// Active le profil de vitesse selon la config. Idempotent.
    /// À appeler en début de tour IA et au démarrage du combat.
    /// </summary>
    public static void AppliquerConfig(ConfigCombat? cfg)
    {
        DelaisActifs = cfg?.Delais;
        // Legacy flags pour fallback (au cas où des chemins de code ne
        // sont pas encore migrés vers DelaisActifs).
        Turbo = cfg?.TurboCombat == true;
        UltraTurbo = cfg?.UltraTurboCombat == true && Turbo;
    }

    /// <summary>Reset (fin de combat). À câbler sur Combat.EtatChange == Inactif.</summary>
    public static void Reset()
    {
        DelaisActifs = null;
        Turbo = false;
        UltraTurbo = false;
    }

    // ============ NOUVELLES MÉTHODES PAR CATÉGORIE ============
    // Si DelaisActifs non défini → tombe sur le legacy Delai(fallbackMin, fallbackMax).

    public static int DelaiActionCombat(int fbMin = 100, int fbMax = 400)
        => DelaisActifs?.ActionCombatGeneral.Tirer() ?? Delai(fbMin, fbMax);
    public static int DelaiLancerSort(int fbMin = 100, int fbMax = 300)
        => DelaisActifs?.LancerSort.Tirer() ?? Delai(fbMin, fbMax);
    public static int DelaiEntreDeuxSorts(int fbMin = 100, int fbMax = 400)
        => DelaisActifs?.EntreDeuxSorts.Tirer() ?? Delai(fbMin, fbMax);
    public static int DelaiPasserTour(int fbMin = 100, int fbMax = 250)
        => DelaisActifs?.PasserTour.Tirer() ?? Delai(fbMin, fbMax);
    public static int DelaiApresDeplacement(int fbMin = 50, int fbMax = 100)
        => DelaisActifs?.ApresDeplacement.Tirer() ?? Delai(fbMin, fbMax);
    public static int DelaiPlacementCombat(int fbMin = 200, int fbMax = 500)
        => DelaisActifs?.PlacementCombat.Tirer() ?? Delai(fbMin, fbMax);
    public static int DelaiDureeParCase(int fbMin = 300, int fbMax = 400)
        => DelaisActifs?.DureeParCaseMs.Tirer() ?? Delai(fbMin, fbMax);
    public static int TimeoutCastMs() => DelaisActifs?.TimeoutCast.Tirer() ?? 600;
    public static int TimeoutMouvementMs() => DelaisActifs?.TimeoutMouvement.Tirer() ?? 1500;
    public static int DelaiEngagerCombat(int fbMin = 200, int fbMax = 1000)
        => DelaisActifs?.EngagerCombat.Tirer() ?? Delai(fbMin, fbMax);
    public static int DelaiDeplacementMap(int fbMin = 100, int fbMax = 350)
        => DelaisActifs?.DeplacementMap.Tirer() ?? Delai(fbMin, fbMax);
    public static int DelaiChangementMap(int fbMin = 150, int fbMax = 300)
        => DelaisActifs?.ChangementMap.Tirer() ?? Delai(fbMin, fbMax);
    public static int DelaiReponsePnj(int fbMin = 100, int fbMax = 250)
        => DelaisActifs?.ReponsePnj.Tirer() ?? Delai(fbMin, fbMax);
}
