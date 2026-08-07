using System.Collections.Generic;
using System.IO;
using BotDofus.Divers.Combats.IA;
using Xunit;

namespace BotDofus.Tests;

/// <summary>
/// Phase 4 (mode héros) — assert que <see cref="ConfigCombat"/> est isolée
/// par perso (chargement depuis <c>peleas/&lt;perso&gt;.json</c>, aucune
/// instance partagée, aucun champ statique mutable).
///
/// Sur Abrak (mono-client), seul le master a une ConfigCombat utilisée par
/// le bot (les héros liés sont joués serveur-side). L'isolation reste
/// néanmoins critique pour les serveurs N-clients et pour des
/// switches entre comptes dans la même session.
/// </summary>
public class IsolationConfigCombatTests
{
    [Fact]
    public void Charger_DeuxComptes_DonneInstancesIndependantes()
    {
        var tmp1 = Path.GetTempFileName();
        var tmp2 = Path.GetTempFileName();
        try
        {
            var orig1 = new ConfigCombat
            {
                Mode = ModeCombat.Agressif,
                DistancePreferee = 5,
                SeuilFuitePv = 30,
            };
            orig1.Sauvegarder(tmp1);

            var orig2 = new ConfigCombat
            {
                Mode = ModeCombat.Eloigne,
                DistancePreferee = 8,
                SeuilFuitePv = 50,
            };
            orig2.Sauvegarder(tmp2);

            var c1 = ConfigCombat.Charger(tmp1);
            var c2 = ConfigCombat.Charger(tmp2);

            // 2 instances bien distinctes
            Assert.NotSame(c1, c2);

            // Champs indépendants
            Assert.Equal(ModeCombat.Agressif, c1.Mode);
            Assert.Equal(ModeCombat.Eloigne, c2.Mode);
            Assert.Equal(5, c1.DistancePreferee);
            Assert.Equal(8, c2.DistancePreferee);

            // Mutation de l'un ne touche pas l'autre
            c1.Mode = ModeCombat.Fuyard;
            Assert.Equal(ModeCombat.Eloigne, c2.Mode);

            c2.DistancePreferee = 99;
            Assert.Equal(5, c1.DistancePreferee);
        }
        finally
        {
            if (File.Exists(tmp1)) File.Delete(tmp1);
            if (File.Exists(tmp2)) File.Delete(tmp2);
        }
    }

    [Fact]
    public void Regles_IndependantesEntreInstances()
    {
        var c1 = new ConfigCombat();
        var c2 = new ConfigCombat();

        c1.Regles.Add(new RegleSort { IdSort = 183, Priorite = 1 });
        Assert.Empty(c2.Regles);
        c2.Regles.Add(new RegleSort { IdSort = 192, Priorite = 2 });
        Assert.Single(c1.Regles);
        Assert.Single(c2.Regles);
        Assert.Equal(183, c1.Regles[0].IdSort);
        Assert.Equal(192, c2.Regles[0].IdSort);
    }

    [Fact]
    public void GenererParDefaut_PluSieursAppels_NePartagentPasEtat()
    {
        var c1 = ConfigCombat.GenererParDefaut(new List<int> { 183 });
        var c2 = ConfigCombat.GenererParDefaut(new List<int> { 192 });

        Assert.NotSame(c1, c2);
        Assert.NotSame(c1.Regles, c2.Regles);
    }
}
