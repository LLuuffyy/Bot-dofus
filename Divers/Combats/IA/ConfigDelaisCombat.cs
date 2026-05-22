namespace BotDofus.Divers.Combats.IA;

/// <summary>
/// Plage de délai aléatoire min-max (ms). Le bot tire un nombre dans
/// l'intervalle pour humaniser. Si Min==Max, délai constant.
/// </summary>
public sealed class PlageDelai
{
    public int Min { get; set; }
    public int Max { get; set; }

    public PlageDelai() { }
    public PlageDelai(int min, int max) { Min = min; Max = max; }

    /// <summary>Tire un délai dans [Min, Max].</summary>
    public int Tirer() => Min >= Max ? Min : System.Random.Shared.Next(Min, Max);
}

/// <summary>
/// Profil de vitesse prédéfini. Quand changé, recharge tous les délais
/// avec les valeurs par défaut du profil. <c>Custom</c> = l'utilisateur
/// a modifié les valeurs manuellement, on ne touche plus à rien.
/// </summary>
public enum ProfilVitesseCombat
{
    /// <summary>Délais humains (~100-400ms par action). Indétectable mais lent.</summary>
    HumainNormal = 0,
    /// <summary>Délais rapides (~30-100ms). Toujours crédible, gain ~3x.</summary>
    Rapide = 1,
    /// <summary>Limite physique TCP (~5-15ms). Détectable mais combats ~2-3s.</summary>
    UltraRapide = 2,
    /// <summary>L'utilisateur a tweaké manuellement. Pas de reset auto.</summary>
    Custom = 3,
}

/// <summary>
/// Toutes les plages de délais utilisées en combat ET hors combat.
/// Inspiré du panneau « Délais » de SynFus (capture user 2026-05-22).
/// L'utilisateur peut soit choisir un profil (HumainNormal/Rapide/UltraRapide),
/// soit éditer chaque valeur (passe en mode Custom).
/// </summary>
public sealed class ConfigDelaisCombat
{
    public ProfilVitesseCombat Profil { get; set; } = ProfilVitesseCombat.Rapide;

    // === EN COMBAT ===
    /// <summary>Délai générique entre 2 actions combat (fallback).</summary>
    public PlageDelai ActionCombatGeneral { get; set; } = new(50, 100);
    /// <summary>Avant le clic sur un PNB / interactif en combat.</summary>
    public PlageDelai CliquerPnb { get; set; } = new(50, 100);
    /// <summary>Délai humanisé avant d'envoyer GA300 (cast d'un sort).</summary>
    public PlageDelai LancerSort { get; set; } = new(50, 100);
    /// <summary>Entre 2 casts dans la même boucle multi-cast.</summary>
    public PlageDelai EntreDeuxSorts { get; set; } = new(50, 150);
    /// <summary>Avant d'envoyer Gt (passer le tour).</summary>
    public PlageDelai PasserTour { get; set; } = new(50, 100);
    /// <summary>Après confirmation d'un déplacement, avant l'action suivante.</summary>
    public PlageDelai ApresDeplacement { get; set; } = new(20, 50);
    /// <summary>Timeout d'attente du broadcast GAS/GAF après un cast.</summary>
    public PlageDelai TimeoutCast { get; set; } = new(600, 600);
    /// <summary>Timeout d'attente du broadcast GA après un GA001 déplacement.</summary>
    public PlageDelai TimeoutMouvement { get; set; } = new(1500, 1500);
    /// <summary>Avant placement (Gp) dans la phase Placement combat.</summary>
    public PlageDelai PlacementCombat { get; set; } = new(150, 300);
    /// <summary>Durée moyenne par case du déplacement (×nbCases pour le wait).</summary>
    public PlageDelai DureeParCaseMs { get; set; } = new(200, 300);

    // === HORS COMBAT ===
    /// <summary>Déplacement sur la map (clic sur cellule).</summary>
    public PlageDelai DeplacementMap { get; set; } = new(50, 150);
    /// <summary>Changement de map (transition).</summary>
    public PlageDelai ChangementMap { get; set; } = new(100, 200);
    /// <summary>Délai avant d'envoyer GA907 pour engager un combat.</summary>
    public PlageDelai EngagerCombat { get; set; } = new(100, 200);
    /// <summary>Réponse aux PNJ (DC, choix dialogue).</summary>
    public PlageDelai ReponsePnj { get; set; } = new(100, 200);

    /// <summary>
    /// Recharge toutes les valeurs avec les défauts du profil donné.
    /// Si <see cref="ProfilVitesseCombat.Custom"/>, ne touche à rien.
    /// </summary>
    public void AppliquerProfil(ProfilVitesseCombat profil)
    {
        Profil = profil;
        switch (profil)
        {
            case ProfilVitesseCombat.HumainNormal:
                ActionCombatGeneral = new(100, 400);
                CliquerPnb = new(100, 300);
                LancerSort = new(100, 300);
                EntreDeuxSorts = new(100, 400);
                PasserTour = new(100, 250);
                ApresDeplacement = new(50, 100);
                TimeoutCast = new(800, 800);
                TimeoutMouvement = new(1500, 1500);
                PlacementCombat = new(200, 500);
                DureeParCaseMs = new(300, 400);
                DeplacementMap = new(100, 350);
                ChangementMap = new(150, 300);
                EngagerCombat = new(200, 1000);
                ReponsePnj = new(100, 250);
                break;

            case ProfilVitesseCombat.Rapide:
                ActionCombatGeneral = new(50, 100);
                CliquerPnb = new(50, 100);
                LancerSort = new(50, 100);
                EntreDeuxSorts = new(50, 150);
                PasserTour = new(50, 100);
                ApresDeplacement = new(20, 50);
                TimeoutCast = new(600, 600);
                TimeoutMouvement = new(1500, 1500);
                PlacementCombat = new(150, 300);
                DureeParCaseMs = new(200, 300);
                DeplacementMap = new(50, 150);
                ChangementMap = new(100, 200);
                EngagerCombat = new(100, 200);
                ReponsePnj = new(100, 200);
                break;

            case ProfilVitesseCombat.UltraRapide:
                ActionCombatGeneral = new(5, 15);
                CliquerPnb = new(5, 15);
                LancerSort = new(5, 15);
                EntreDeuxSorts = new(5, 15);
                PasserTour = new(5, 15);
                ApresDeplacement = new(5, 15);
                TimeoutCast = new(400, 400);
                TimeoutMouvement = new(1000, 1000);
                PlacementCombat = new(50, 100);
                DureeParCaseMs = new(150, 200);
                DeplacementMap = new(20, 50);
                ChangementMap = new(50, 100);
                EngagerCombat = new(50, 100);
                ReponsePnj = new(50, 100);
                break;

            case ProfilVitesseCombat.Custom:
                // Pas de changement — l'utilisateur a la main.
                break;
        }
    }

    /// <summary>Factory avec profil par défaut Rapide.</summary>
    public static ConfigDelaisCombat Defauts()
    {
        var c = new ConfigDelaisCombat();
        c.AppliquerProfil(ProfilVitesseCombat.Rapide);
        return c;
    }
}
