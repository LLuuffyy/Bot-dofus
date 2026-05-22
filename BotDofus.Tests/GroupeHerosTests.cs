using System.Linq;
using BotDofus.Divers.MultiAccount;
using Xunit;

namespace BotDofus.Tests;

/// <summary>
/// Tests du modèle de données <see cref="GroupeHeros"/> (Phase 2).
/// Pas de réseau, pas de TrameJeu, juste l'agrégat pur : composition,
/// lifecycle (Activer/Dissoudre), tracking de l'ordre des tours via GTS.
/// </summary>
public class GroupeHerosTests
{
    [Fact]
    public void AjouterMembre_LeaderUnique_AncienLeaderRetrograde()
    {
        var g = new GroupeHeros();
        g.AjouterMembre(new MembreHeros { IdJeu = 1, Role = RoleDansGroupe.Leader, Nom = "Master" });
        g.AjouterMembre(new MembreHeros { IdJeu = 2, Role = RoleDansGroupe.Leader, Nom = "AutreLeader" });

        Assert.Equal(2, g.Membres.Count);
        Assert.Single(g.Membres, m => m.Role == RoleDansGroupe.Leader);
        Assert.Equal(2, g.Leader!.IdJeu);
        Assert.Equal(RoleDansGroupe.Suiveur, g.TrouverParIdJeu(1)!.Role);
    }

    [Fact]
    public void AjouterMembre_AntiDoublon_ParIdJeu()
    {
        var g = new GroupeHeros();
        g.AjouterMembre(new MembreHeros { IdJeu = 1, Nom = "A" });
        g.AjouterMembre(new MembreHeros { IdJeu = 1, Nom = "Doublon" });
        Assert.Single(g.Membres);
        Assert.Equal("A", g.Membres[0].Nom);
    }

    [Fact]
    public void AjouterMembre_AntiDoublon_ParIdentifiantCompte()
    {
        var g = new GroupeHeros();
        g.AjouterMembre(new MembreHeros { Identifiant = "Beilo", IdJeu = 0, Nom = "A" });
        g.AjouterMembre(new MembreHeros { Identifiant = "Beilo", IdJeu = 0, Nom = "Doublon" });
        Assert.Single(g.Membres);
    }

    [Fact]
    public void Activer_PuisDissoudre_EventsIdempotents()
    {
        var g = new GroupeHeros();
        int active = 0, dissous = 0;
        g.GroupeActive += (_, _) => active++;
        g.GroupeDissous += (_, _) => dissous++;

        g.Activer();
        g.Activer(); // idempotent
        Assert.True(g.EstActif);
        Assert.Equal(1, active);

        g.Dissoudre();
        g.Dissoudre(); // idempotent
        Assert.False(g.EstActif);
        Assert.Equal(1, dissous);
    }

    [Fact]
    public void NotifierTourServeur_DeclencheTourDeMembre_PourMembre()
    {
        var g = new GroupeHeros();
        var leader = new MembreHeros { IdJeu = 100, Role = RoleDansGroupe.Leader, Nom = "L" };
        var lie = new MembreHeros { IdJeu = 200, Role = RoleDansGroupe.Suiveur, Nom = "S" };
        g.AjouterMembre(leader);
        g.AjouterMembre(lie);
        g.Activer();

        MembreHeros? capture = null;
        g.TourDeMembre += (_, m) => capture = m;

        g.NotifierTourServeur(100, 1);
        Assert.Same(leader, capture);
        Assert.Equal(0, g.IndexMembreActuel);
        Assert.Same(leader, g.JoueurActuel);

        g.NotifierTourServeur(200, 1);
        Assert.Same(lie, capture);
        Assert.Equal(1, g.IndexMembreActuel);
    }

    [Fact]
    public void NotifierTourServeur_NeDeclenchePasPourNonMembre_MaisTrackeOrdre()
    {
        var g = new GroupeHeros();
        g.AjouterMembre(new MembreHeros { IdJeu = 100, Role = RoleDansGroupe.Leader });
        bool declenche = false;
        g.TourDeMembre += (_, _) => declenche = true;

        g.NotifierTourServeur(999, 1); // ennemi
        Assert.False(declenche);
        Assert.Contains(999, g.OrdreToursCourant);
    }

    [Fact]
    public void NotifierTourServeur_ResetOrdreUniquementSiExplicite()
    {
        var g = new GroupeHeros();
        g.AjouterMembre(new MembreHeros { IdJeu = 100, Role = RoleDansGroupe.Leader });

        // Combat 1 : tous les combattants jouent leur tour 1 dans le même combat
        g.NotifierTourServeur(100, 1);
        g.NotifierTourServeur(999, 1);
        Assert.Equal(2, g.OrdreToursCourant.Count);

        // Pas de reset auto sur numeroTour=1 (sinon on perd l'ordre entre les GTS du même tour 1)
        g.NotifierTourServeur(200, 1);
        Assert.Equal(3, g.OrdreToursCourant.Count);

        // Transition combat → l'appelant doit reset explicitement (TrameJeu sur MessageFinCombat)
        g.ReinitialiserOrdreTours();
        g.NotifierTourServeur(500, 1);
        Assert.Single(g.OrdreToursCourant);
        Assert.Equal(500, g.OrdreToursCourant[0]);
    }

    [Fact]
    public void PasserAuSuivant_BoucleModuloOrdreCourant()
    {
        var g = new GroupeHeros();
        g.AjouterMembre(new MembreHeros { IdJeu = 1, Role = RoleDansGroupe.Leader, Nom = "A" });
        g.AjouterMembre(new MembreHeros { IdJeu = 2, Role = RoleDansGroupe.Suiveur, Nom = "B" });
        g.AjouterMembre(new MembreHeros { IdJeu = 3, Role = RoleDansGroupe.Suiveur, Nom = "C" });

        // Construit l'ordre via les GTS reçus
        g.NotifierTourServeur(1, 1);
        g.NotifierTourServeur(2, 1);
        g.NotifierTourServeur(3, 1);

        Assert.Equal(3, g.JoueurActuel!.IdJeu); // dernier reçu
        Assert.Equal(1, g.PasserAuSuivant()!.IdJeu); // boucle vers le début
        Assert.Equal(2, g.PasserAuSuivant()!.IdJeu);
        Assert.Equal(3, g.PasserAuSuivant()!.IdJeu);
    }

    [Fact]
    public void PasserAuSuivant_RetourneNullSiOrdreVide()
    {
        var g = new GroupeHeros();
        g.AjouterMembre(new MembreHeros { IdJeu = 1, Role = RoleDansGroupe.Leader });
        Assert.Null(g.PasserAuSuivant());
    }

    [Fact]
    public void RetirerMembre_ExistantEtAbsent()
    {
        var g = new GroupeHeros();
        g.AjouterMembre(new MembreHeros { IdJeu = 1 });
        Assert.True(g.RetirerMembre(1));
        Assert.False(g.RetirerMembre(1));
        Assert.False(g.RetirerMembre(99));
        Assert.Empty(g.Membres);
    }

    [Fact]
    public void Dispose_DissoudGroupeActif()
    {
        var g = new GroupeHeros();
        g.AjouterMembre(new MembreHeros { IdJeu = 1, Role = RoleDansGroupe.Leader });
        g.Activer();
        Assert.True(g.EstActif);
        g.Dispose();
        Assert.False(g.EstActif);
    }

    [Fact]
    public void ReinitialiserOrdreTours_ConserveActivationMaisResetCurseur()
    {
        var g = new GroupeHeros();
        g.AjouterMembre(new MembreHeros { IdJeu = 1, Role = RoleDansGroupe.Leader });
        g.Activer();
        g.NotifierTourServeur(1, 1);
        Assert.NotEmpty(g.OrdreToursCourant);

        g.ReinitialiserOrdreTours();
        Assert.Empty(g.OrdreToursCourant);
        Assert.Equal(-1, g.IndexMembreActuel);
        Assert.True(g.EstActif); // toujours actif, juste l'ordre reset
    }

    [Fact]
    public void EstMonoClient_TrueParDefaut()
    {
        var g = new GroupeHeros();
        Assert.True(g.EstMonoClient);
    }
}
