using BotDofus.Divers.Caracteristiques;
using Xunit;

namespace BotDofus.Tests;

/// <summary>
/// Tests des paliers de coût en points pour les caractéristiques Dofus 1.29.
/// Validation des règles : Vita 1pt/+1, Sagesse 3pt/+1, Force/Int/Cha/Agi
/// paliers progressifs 1-2-3-4 par tranche de 100.
/// </summary>
public class PaliersCaracsTests
{
    [Theory]
    [InlineData(IdsCaracs.Vitalite, 0, 1)]
    [InlineData(IdsCaracs.Vitalite, 500, 1)]
    [InlineData(IdsCaracs.Sagesse, 0, 3)]
    [InlineData(IdsCaracs.Sagesse, 100, 3)]
    [InlineData(IdsCaracs.Force, 0, 1)]
    [InlineData(IdsCaracs.Force, 99, 1)]
    [InlineData(IdsCaracs.Force, 100, 2)]
    [InlineData(IdsCaracs.Force, 199, 2)]
    [InlineData(IdsCaracs.Force, 200, 3)]
    [InlineData(IdsCaracs.Force, 299, 3)]
    [InlineData(IdsCaracs.Force, 300, 4)]
    [InlineData(IdsCaracs.Force, 500, 4)]
    [InlineData(IdsCaracs.Intelligence, 150, 2)]
    [InlineData(IdsCaracs.Chance, 250, 3)]
    [InlineData(IdsCaracs.Agilite, 99, 1)]
    public void CoutPourUnPoint_Retourne_Palier_Attendu(int statId, int valeurActuelle, int coutAttendu)
    {
        var cout = PaliersCaracs.CoutPourUnPoint(statId, valeurActuelle);
        Assert.Equal(coutAttendu, cout);
    }

    [Fact]
    public void CombienAvecBudget_Vita_5_Points_Donne_5_Stats()
    {
        var (stats, conso) = PaliersCaracs.CombienAvecBudget(IdsCaracs.Vitalite, 0, 5);
        Assert.Equal(5, stats);
        Assert.Equal(5, conso);
    }

    [Fact]
    public void CombienAvecBudget_Sagesse_9_Points_Donne_3_Stats()
    {
        var (stats, conso) = PaliersCaracs.CombienAvecBudget(IdsCaracs.Sagesse, 0, 9);
        Assert.Equal(3, stats);  // 9pt / 3pt/stat
        Assert.Equal(9, conso);
    }

    [Fact]
    public void CombienAvecBudget_Force_100_Stats_Restantes_Coute_100()
    {
        // De 0 à 99 : 100 stats × 1pt = 100pt
        var (stats, conso) = PaliersCaracs.CombienAvecBudget(IdsCaracs.Force, 0, 100);
        Assert.Equal(100, stats);
        Assert.Equal(100, conso);
    }

    [Fact]
    public void CombienAvecBudget_Force_Traverse_Palier_100_200()
    {
        // À 99 : 1 pt pour +1. Puis à 100 : 2 pt pour +1.
        // Budget 5 pt depuis 99 → 1 pt (99→100) puis 4 pt (100→101, 101→102) = 3 stats, 5 conso
        var (stats, conso) = PaliersCaracs.CombienAvecBudget(IdsCaracs.Force, 99, 5);
        Assert.Equal(3, stats);  // 99→100 (1pt) + 100→101 (2pt) + 101→102 (2pt) = 5pt total
        Assert.Equal(5, conso);
    }

    [Fact]
    public void CombienAvecBudget_Budget_Zero_Retourne_Zero()
    {
        var (stats, conso) = PaliersCaracs.CombienAvecBudget(IdsCaracs.Force, 0, 0);
        Assert.Equal(0, stats);
        Assert.Equal(0, conso);
    }

    [Fact]
    public void CombienAvecBudget_Budget_Insuffisant_Pour_Palier_Suivant()
    {
        // À 300 : 4pt pour +1. Budget 3pt → 0 stat.
        var (stats, conso) = PaliersCaracs.CombienAvecBudget(IdsCaracs.Force, 300, 3);
        Assert.Equal(0, stats);
        Assert.Equal(0, conso);
    }

    [Fact]
    public void ConfigRepartition_Total_100_Valide()
    {
        var cfg = new ConfigRepartitionCaracs
        {
            Mode = ModeDistribCaracs.Automatique,
            PctForce = 100
        };
        Assert.True(cfg.EstValide);
        Assert.Equal(100, cfg.Total);
    }

    [Fact]
    public void ConfigRepartition_Manuel_Invalide()
    {
        var cfg = new ConfigRepartitionCaracs { Mode = ModeDistribCaracs.Manuel, PctForce = 100 };
        Assert.False(cfg.EstValide);
    }

    [Fact]
    public void ConfigRepartition_Total_Pas_100_Invalide()
    {
        var cfg = new ConfigRepartitionCaracs
        {
            Mode = ModeDistribCaracs.Automatique,
            PctForce = 50,
            PctVitalite = 30  // total = 80
        };
        Assert.False(cfg.EstValide);
    }

    [Theory]
    [InlineData(1, 60, 40)]  // Feca: Vita 60 + Sagesse 40
    [InlineData(8, 100, 0)]  // Iop: Force 100
    [InlineData(10, 0, 30)]  // Sadida: Int 70 + Sagesse 30
    public void PresetParClasse_Retourne_Repartition_Connue(int idClasse, int forceAttendue, int sagesseAttendue)
    {
        var preset = ConfigRepartitionCaracs.PresetParClasse(idClasse);
        Assert.NotNull(preset);
        if (idClasse == 1) { Assert.Equal(60, preset.PctVitalite); Assert.Equal(40, preset.PctSagesse); }
        if (idClasse == 8) Assert.Equal(100, preset.PctForce);
        if (idClasse == 10) { Assert.Equal(70, preset.PctIntelligence); Assert.Equal(30, preset.PctSagesse); }
    }

    [Fact]
    public void PresetParClasse_Classe_Inconnue_Retourne_Null()
    {
        var preset = ConfigRepartitionCaracs.PresetParClasse(999);
        Assert.Null(preset);
    }
}
