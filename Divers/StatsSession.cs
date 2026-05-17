using System;
using BotDofus.Commun.Reseau;
using BotDofus.Divers.Combats.Enums;
using BotDofus.Divers.Jeu.Personnage;

namespace BotDofus.Divers;

/// <summary>
/// Compteurs de session pour un compte bot : temps online, volume de paquets,
/// combats joués, kamas/XP gagnés depuis l'attache de la session jeu.
/// Alimenté par les événements de <see cref="ContexteCompte"/> et exposé à l'UI.
/// </summary>
public sealed class StatsSession
{
    private bool _baselineKamasCapture;
    private bool _baselineXpCapture;

    public DateTime? DemarreA { get; private set; }

    public long PaquetsRecus { get; private set; }
    public long PaquetsEnvoyes { get; private set; }
    public long OctetsRecus { get; private set; }
    public long OctetsEnvoyes { get; private set; }

    public int CombatsTotaux { get; private set; }
    public int CombatsEnCours { get; private set; }

    /// <summary>Nb de paquets modifiés/supprimés par le moteur d'interception (métrique furtivité).</summary>
    public long PaquetsModifies { get; private set; }

    public long KamasInitiaux { get; private set; }
    public long KamasCourants { get; private set; }
    public long XpInitiale { get; private set; }
    public long XpCourante { get; private set; }

    public TimeSpan TempsEcoule => DemarreA.HasValue ? DateTime.UtcNow - DemarreA.Value : TimeSpan.Zero;
    public long KamasGagnes => KamasCourants - KamasInitiaux;
    public long XpGagnee => XpCourante - XpInitiale;

    public event EventHandler? Change;

    public void DemarrerSession(Personnage perso)
    {
        DemarreA = DateTime.UtcNow;
        KamasInitiaux = perso.Kamas;
        KamasCourants = perso.Kamas;
        XpInitiale = perso.XpActuelle;
        XpCourante = perso.XpActuelle;
        _baselineKamasCapture = perso.Kamas != 0;
        _baselineXpCapture = perso.XpActuelle != 0;
        Change?.Invoke(this, EventArgs.Empty);
    }

    public void NotifierPaquet(EvenementPaquetRecu e)
    {
        var taille = e.Paquet.Contenu.Length;
        if (e.Paquet.Direction == DirectionPaquet.VersClient)
        {
            PaquetsRecus++;
            OctetsRecus += taille;
        }
        else
        {
            PaquetsEnvoyes++;
            OctetsEnvoyes += taille;
        }
        Change?.Invoke(this, EventArgs.Empty);
    }

    public void NotifierInterception()
    {
        PaquetsModifies++;
        Change?.Invoke(this, EventArgs.Empty);
    }

    public void NotifierEtatCombat(EtatCombat etat)
    {
        if (etat == EtatCombat.Placement || etat == EtatCombat.EnCours)
        {
            if (CombatsEnCours == 0) CombatsTotaux++;
            CombatsEnCours = 1;
        }
        else if (etat == EtatCombat.Inactif)
        {
            CombatsEnCours = 0;
        }
        Change?.Invoke(this, EventArgs.Empty);
    }

    public void NotifierKamas(long valeur)
    {
        if (!_baselineKamasCapture)
        {
            KamasInitiaux = valeur;
            _baselineKamasCapture = true;
        }

        if (KamasCourants == valeur) return;
        KamasCourants = valeur;
        Change?.Invoke(this, EventArgs.Empty);
    }

    public void NotifierXp(long valeur)
    {
        if (!_baselineXpCapture)
        {
            XpInitiale = valeur;
            _baselineXpCapture = true;
        }

        if (XpCourante == valeur) return;
        XpCourante = valeur;
        Change?.Invoke(this, EventArgs.Empty);
    }

    public void Reinitialiser()
    {
        DemarreA = null;
        PaquetsRecus = PaquetsEnvoyes = 0;
        OctetsRecus = OctetsEnvoyes = 0;
        CombatsTotaux = CombatsEnCours = 0;
        KamasInitiaux = KamasCourants = 0;
        XpInitiale = XpCourante = 0;
        _baselineKamasCapture = _baselineXpCapture = false;
        Change?.Invoke(this, EventArgs.Empty);
    }
}
