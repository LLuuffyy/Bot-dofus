using BotDofus.Divers.Banque;
using Xunit;

namespace BotDofus.Tests;

/// <summary>
/// Tests du système banque : catégorisation d'items + filtres de dépôt.
/// Le côté réseau (paquets ApS/EMO+/EV) ne peut pas être testé sans mock
/// MITM, mais la logique de filtrage est l'élément critique (sécurité :
/// jamais déposer un item dans la liste noire).
/// </summary>
public class BanqueTests
{
    [Theory]
    // Équipement (armes/anneaux/amulettes/capes/coiffes/ceintures/bottes…).
    [InlineData(1, CategorieObjet.Equipement)]    // amulette
    [InlineData(7, CategorieObjet.Equipement)]    // anneau
    [InlineData(11, CategorieObjet.Equipement)]   // bottes
    [InlineData(16, CategorieObjet.Equipement)]   // coiffe (dans le range dyshay)
    // Ressources (matériaux récoltés).
    [InlineData(41, CategorieObjet.Ressource)]    // ressources de récolte
    [InlineData(63, CategorieObjet.Ressource)]    // peaux
    [InlineData(96, CategorieObjet.Ressource)]    // céréales
    // Consommables (potions/pains/parchemins).
    [InlineData(12, CategorieObjet.Consommable)]  // potion
    [InlineData(85, CategorieObjet.Consommable)]  // pain
    [InlineData(86, CategorieObjet.Consommable)]  // boisson
    // Quête.
    [InlineData(24, CategorieObjet.Quete)]
    // Inconnu (type non répertorié).
    [InlineData(0, CategorieObjet.Inconnu)]
    [InlineData(250, CategorieObjet.Inconnu)]
    public void Categoriser_MappingByteType(int typeByte, CategorieObjet attendu)
    {
        Assert.Equal(attendu, CategoriseurObjet.Categoriser(typeByte));
    }

    [Fact]
    public void ConfigBanque_ValeursParDefaut_SecurisesParConstruction()
    {
        var cfg = new ConfigBanque();

        // Par défaut, le système est INACTIF — on n'expédie rien tant que
        // l'user n'a pas explicitement validé.
        Assert.False(cfg.Active);

        // Par défaut, on dépose UNIQUEMENT les ressources. Équipement, conso,
        // quête, inconnu : tout reste dans l'inventaire (sécurité maximale).
        Assert.False(cfg.DeposerEquipements);
        Assert.True(cfg.DeposerRessources);
        Assert.False(cfg.DeposerConsommables);
        Assert.False(cfg.DeposerQuetes);
        Assert.False(cfg.DeposerInconnus);

        // Seuil par défaut raisonnable (80% pods → on a marge avant FULL).
        Assert.InRange(cfg.SeuilPoidsPct, 50, 95);

        // Délai humanisé compatible avec la capture user (1.3-3.9s).
        Assert.InRange(cfg.DelaiActionMinMs, 1000, 5000);
        Assert.InRange(cfg.DelaiActionMaxMs, 1000, 5000);
        Assert.True(cfg.DelaiActionMaxMs >= cfg.DelaiActionMinMs);
    }

    [Fact]
    public void ConfigBanque_IdsAGarder_VideParDefaut()
    {
        // La liste noire est vide par défaut — l'user la remplit.
        var cfg = new ConfigBanque();
        Assert.Empty(cfg.IdsAGarder);
    }

    [Fact]
    public void ConfigBanque_CompatAscendante_ItemsAGarder_Synchronise()
    {
        var cfg = new ConfigBanque();
        cfg.ItemsAGarder = new System.Collections.Generic.List<int> { 312, 311, 110 };

        Assert.Contains(312, cfg.IdsAGarder);
        Assert.Contains(311, cfg.IdsAGarder);
        Assert.Contains(110, cfg.IdsAGarder);
        Assert.Equal(3, cfg.ItemsAGarder.Count);
    }

    [Fact]
    public void ConfigBanque_SauvegardeChargement_RoundTrip()
    {
        var tmp = System.IO.Path.GetTempFileName();
        try
        {
            var orig = new ConfigBanque
            {
                Active = true,
                SeuilPoidsPct = 85,
                CiblePoidsPct = 25,
                MapBanqueId = 10303,
                DeposerEquipements = false,
                DeposerRessources = true,
                DeposerConsommables = false,
                IdsAGarder = { 312, 311 },
                IdsADeposerForce = { 728 },
                SeuilParTemplate = { [312] = 50, [311] = 20 },
            };
            orig.Sauvegarder(tmp);
            var loaded = ConfigBanque.Charger(tmp);

            Assert.Equal(orig.Active, loaded.Active);
            Assert.Equal(orig.SeuilPoidsPct, loaded.SeuilPoidsPct);
            Assert.Equal(orig.CiblePoidsPct, loaded.CiblePoidsPct);
            Assert.Equal(orig.MapBanqueId, loaded.MapBanqueId);
            Assert.Equal(orig.DeposerRessources, loaded.DeposerRessources);
            Assert.Contains(312, loaded.IdsAGarder);
            Assert.Contains(311, loaded.IdsAGarder);
            Assert.Contains(728, loaded.IdsADeposerForce);
            Assert.Equal(50, loaded.SeuilParTemplate[312]);
            Assert.Equal(20, loaded.SeuilParTemplate[311]);
        }
        finally
        {
            if (System.IO.File.Exists(tmp)) System.IO.File.Delete(tmp);
        }
    }
}
