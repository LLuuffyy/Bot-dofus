using BotDofus.Commun.Messages.VersClient.Jeu;
using BotDofus.Divers;
using BotDofus.Divers.Jeu;
using BotDofus.Divers.MultiAccount;
using Xunit;

namespace BotDofus.Tests;

/// <summary>
/// Tests Phase 3 — parser GTSX + DetecteurModeHeros.
/// Pas de réseau, pas de TrameJeu : on instancie directement le parser et
/// le détecteur, on simule la réception des GTSX, on vérifie le GroupeHeros
/// résultant.
/// </summary>
public class ModeHerosTests
{
    [Fact]
    public void GTSX_Desserialiser_ParseIdsMasterEtLie()
    {
        var msg = new MessageTourCombatAbrak();
        msg.Desserialiser("X401770;401775;0;1;0;100;100;6;3;0");

        Assert.True(msg.EstGTSX);
        Assert.False(msg.EstTour);
        Assert.Equal(401770, msg.IdMaster);
        Assert.Equal(401775, msg.IdPersoLie);
        Assert.Equal("401770;401775;0;1;0;100;100;6;3;0", msg.DonneesGTSX);
    }

    [Fact]
    public void GTS_Standard_DesserialiserInchange()
    {
        var msg = new MessageTourCombatAbrak();
        msg.Desserialiser("401770|45000|1");

        Assert.True(msg.EstTour);
        Assert.False(msg.EstGTSX);
        Assert.Equal(401770, msg.IdentifiantCombattant);
        Assert.Equal(1, msg.NumeroTour);
    }

    [Fact]
    public void GTSX_Malformee_NeCasseRien()
    {
        var msg = new MessageTourCombatAbrak();
        msg.Desserialiser("Xpasunint;401775");

        Assert.True(msg.EstGTSX);
        Assert.Equal(0, msg.IdMaster);
        Assert.Equal(0, msg.IdPersoLie);
    }

    [Fact]
    public void DetecteurModeHeros_PremierGTSX_CreeGroupeAvecMaster()
    {
        var compte = new Compte("Beilo", "pwd");
        var etat = new EtatJeu();
        etat.Personnage.Identifiant = 401770;
        etat.Personnage.Nom = "Beiloddurul";
        etat.Personnage.IdClasse = 10;
        etat.Personnage.Niveau = 17;

        var detecteur = new DetecteurModeHeros(compte, etat);
        var msg = new MessageTourCombatAbrak();
        msg.Desserialiser("X401770;401771;0;1;0;100;100;6;3;0");

        detecteur.OnGTSX(msg);

        Assert.NotNull(compte.GroupeHeros);
        Assert.True(compte.GroupeHeros!.EstActif);
        Assert.Equal(2, compte.GroupeHeros.Membres.Count);
        Assert.Equal(401770, compte.GroupeHeros.Leader!.IdJeu);
        Assert.Equal("Beiloddurul", compte.GroupeHeros.Leader.Nom);
        Assert.Equal(RoleDansGroupe.Leader, compte.GroupeHeros.Leader.Role);
        Assert.NotNull(compte.GroupeHeros.TrouverParIdJeu(401771));
        Assert.Equal(RoleDansGroupe.Suiveur, compte.GroupeHeros.TrouverParIdJeu(401771)!.Role);
    }

    [Fact]
    public void DetecteurModeHeros_PluSieursGTSX_EnroleTousLesLies()
    {
        var compte = new Compte("Beilo", "pwd");
        var etat = new EtatJeu();
        etat.Personnage.Identifiant = 401770;

        var detecteur = new DetecteurModeHeros(compte, etat);
        // Simule les 7 GTSX d'entrée combat
        for (int i = 1; i <= 7; i++)
        {
            var msg = new MessageTourCombatAbrak();
            msg.Desserialiser($"X401770;{401770 + i};0;1;0;100;100;6;3;0");
            detecteur.OnGTSX(msg);
        }

        Assert.NotNull(compte.GroupeHeros);
        // 1 master + 7 liés
        Assert.Equal(8, compte.GroupeHeros!.Membres.Count);
        for (int i = 1; i <= 7; i++)
        {
            Assert.NotNull(compte.GroupeHeros.TrouverParIdJeu(401770 + i));
        }
    }

    [Fact]
    public void DetecteurModeHeros_GTSXAvecMasterEtranger_Ignore()
    {
        var compte = new Compte("Beilo", "pwd");
        var etat = new EtatJeu();
        etat.Personnage.Identifiant = 401770;

        var detecteur = new DetecteurModeHeros(compte, etat);
        var msg = new MessageTourCombatAbrak();
        // Master = 999999 ≠ 401770 (= moi) → doit être ignoré
        msg.Desserialiser("X999999;888888;0;1;0;100;100;6;3;0");

        detecteur.OnGTSX(msg);

        Assert.Null(compte.GroupeHeros);
    }

    [Fact]
    public void DetecteurModeHeros_NonGTSX_NoOp()
    {
        var compte = new Compte("Beilo", "pwd");
        var etat = new EtatJeu();
        etat.Personnage.Identifiant = 401770;

        var detecteur = new DetecteurModeHeros(compte, etat);
        var msg = new MessageTourCombatAbrak();
        // GTS classique (pas GTSX) → no-op
        msg.Desserialiser("401770|45000|1");

        detecteur.OnGTSX(msg);

        Assert.Null(compte.GroupeHeros);
    }

    [Fact]
    public void DetecteurModeHeros_GTSXMalforme_NoOp()
    {
        var compte = new Compte("Beilo", "pwd");
        var etat = new EtatJeu();
        etat.Personnage.Identifiant = 401770;

        var detecteur = new DetecteurModeHeros(compte, etat);
        var msg = new MessageTourCombatAbrak();
        msg.Desserialiser("Xpasunint;rien");

        detecteur.OnGTSX(msg);

        Assert.Null(compte.GroupeHeros);
    }
}
