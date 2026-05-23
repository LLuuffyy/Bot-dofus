using System.Linq;
using BotDofus.Divers.Combats.IA;
using BotDofus.Divers.MultiAccount;
using Xunit;

namespace BotDofus.Tests;

/// <summary>
/// Tests des améliorations IA Phase 4 :
/// - Preset Sadida complet
/// - PmEnnemiCible fallback adaptatif
/// - Focus EnnemiAdjacentInvocAllie
/// - PasSiTacle heuristique
/// </summary>
public class IACombatAmeliorationTests
{
    [Fact]
    public void PresetSadida_Contient_7_Sorts_Configures()
    {
        var cfg = ServiceConfigsHeros.PresetParClasse(10);  // 10 = Sadida
        Assert.NotNull(cfg);
        // Sorts attendus : Folle (182), Bloqueuse (193), Ronce (183),
        // Larme (195), Poison (200), Apaisante (192), Sacrifice (198)
        var ids = cfg.Regles.Select(r => r.IdSort).ToHashSet();
        Assert.Contains(182, ids);
        Assert.Contains(193, ids);
        Assert.Contains(183, ids);
        Assert.Contains(195, ids);
        Assert.Contains(200, ids);
        Assert.Contains(192, ids);
        Assert.Contains(198, ids);
    }

    [Fact]
    public void PresetSadida_FolleEt_Bloqueuse_T1_Seulement()
    {
        var cfg = ServiceConfigsHeros.PresetParClasse(10);
        Assert.NotNull(cfg);
        var folle = cfg.Regles.First(r => r.IdSort == 182);
        var bloqueuse = cfg.Regles.First(r => r.IdSort == 193);
        Assert.True(folle.PremierTour);
        Assert.True(bloqueuse.PremierTour);
    }

    [Fact]
    public void PresetSadida_Sacrifice_Conditions_Strictes()
    {
        var cfg = ServiceConfigsHeros.PresetParClasse(10);
        Assert.NotNull(cfg);
        var sacrifice = cfg.Regles.First(r => r.IdSort == 198);
        Assert.Equal(30, sacrifice.MesPvInfPourcent);
        Assert.True(sacrifice.SiInvocPresente);
    }

    [Fact]
    public void PresetCra_LancerCombo_Cible_AdjacentInvoc()
    {
        var cfg = ServiceConfigsHeros.PresetParClasse(9);  // 9 = Cra
        Assert.NotNull(cfg);
        var flecheMagique = cfg.Regles.First(r => r.IdSort == 161);
        Assert.Equal(FocusSort.EnnemiAdjacentInvocAllie, flecheMagique.Focus);
    }

    [Fact]
    public void PresetEnutrof_LancerPieces_Cible_AdjacentInvoc()
    {
        var cfg = ServiceConfigsHeros.PresetParClasse(3);  // 3 = Enutrof
        Assert.NotNull(cfg);
        var lancerPieces = cfg.Regles.First(r => r.IdSort == 51);
        Assert.Equal(FocusSort.EnnemiAdjacentInvocAllie, lancerPieces.Focus);
    }

    [Fact]
    public void DistanceIdeale_Equilibre_Inchange_Par_FallbackPm()
    {
        // Le fallback PM adaptatif ne doit toucher QUE Eloigne/Fuyard.
        // Mode Equilibre = clamp distPreferee dans [pmin, pmax].
        var ctx = new ScorePositionCombat.ContexteTactique(
            Mode: ModeCombat.Equilibre, PorteeMinSort: 1, PorteeMaxSort: 8,
            SortNecessiteLOS: false, PmEnnemiCible: 0,
            DistancePreferee: 5, DistanceMinEloigne: 5);
        Assert.Equal(5, ScorePositionCombat.DistanceIdeale(ctx));
    }

    [Fact]
    public void DistanceIdeale_Agressif_Toujours_CAC()
    {
        // Mode Agressif : CAC=1 si pMax≤1, sinon dist=1 (min).
        var ctxCAC = new ScorePositionCombat.ContexteTactique(
            Mode: ModeCombat.Agressif, PorteeMinSort: 1, PorteeMaxSort: 1,
            SortNecessiteLOS: false, PmEnnemiCible: 5,
            DistancePreferee: 1, DistanceMinEloigne: 1);
        Assert.Equal(1, ScorePositionCombat.DistanceIdeale(ctxCAC));

        var ctxRange = new ScorePositionCombat.ContexteTactique(
            Mode: ModeCombat.Agressif, PorteeMinSort: 2, PorteeMaxSort: 6,
            SortNecessiteLOS: false, PmEnnemiCible: 5,
            DistancePreferee: 1, DistanceMinEloigne: 1);
        Assert.Equal(2, ScorePositionCombat.DistanceIdeale(ctxRange));
    }
}
