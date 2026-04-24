using System;
using BotDofus.Divers.Cartes;
using BotDofus.Divers.Combats;
using BotDofus.Divers.Jeu.Personnage;

namespace BotDofus.Divers.Jeu;

/// <summary>
/// Agrégat des données "monde" d'un compte bot : carte courante, personnage,
/// combat en cours. Un <see cref="Compte"/> expose un <see cref="EtatJeu"/>
/// unique que les trames et l'UI peuvent consulter.
/// </summary>
public sealed class EtatJeu
{
    public Personnage.Personnage Personnage { get; } = new();
    public Carte? CarteCourante { get; private set; }
    public Combat Combat { get; } = new();

    public event EventHandler<Carte>? CarteChangee;

    public void ChangerCarte(int identifiant, string? clefDecryption = null)
    {
        CarteCourante = new Carte(identifiant);
        Personnage.CarteCourante = identifiant;
        CarteChangee?.Invoke(this, CarteCourante);
        // TODO : décoder la clef pour peupler les types de cellules via AppliquerMouvements.
        _ = clefDecryption; // placeholder
    }
}
