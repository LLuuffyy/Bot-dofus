using System;
using BotDofus.Divers.Enums;

namespace BotDofus.Divers;

/// <summary>
/// Représente un compte bot chargé en mémoire : identifiants, état de session,
/// et futures références vers la couche réseau, le personnage et le moteur de scripts.
/// </summary>
public sealed class Compte : IEffacable
{
    public Compte(string identifiant, string motDePasse)
    {
        Identifiant = identifiant ?? throw new ArgumentNullException(nameof(identifiant));
        MotDePasse = motDePasse ?? throw new ArgumentNullException(nameof(motDePasse));
        Etat = EtatsCompte.Deconnecte;
    }

    public string Identifiant { get; }
    public string MotDePasse { get; }
    public string? PseudoAffiche { get; set; }
    public int ServeurPrefere { get; set; }
    public int PersonnagePrefere { get; set; }

    public EtatsCompte Etat { get; set; }

    /// <summary>Déclenché à chaque changement d'état, utilisé par l'UI pour se rafraîchir.</summary>
    public event EventHandler<EtatsCompte>? EtatChange;

    public void ChangerEtat(EtatsCompte nouvelEtat)
    {
        if (Etat == nouvelEtat) return;
        Etat = nouvelEtat;
        EtatChange?.Invoke(this, nouvelEtat);
    }

    public void Effacer()
    {
        // TODO : réinitialiser les futurs sous-systèmes (Personnage, Carte, Combat, Scripts)
        ChangerEtat(EtatsCompte.Deconnecte);
    }

    public void Dispose()
    {
        Effacer();
        GC.SuppressFinalize(this);
    }
}
