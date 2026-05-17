using System;
using BotDofus.Divers.Cartes;
using BotDofus.Divers.Combats;
using BotDofus.Divers.Jeu.Personnage;
using BotDofus.Utilitaires.Crypto;
using BotDofus.Utilitaires.Journaux;

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

    public void ChangerCarte(int identifiant, string? dateVersion = null, string? clefCarte = null)
    {
        Carte carte;
        Personnage.CarteCourante = identifiant;

        // Décodage du terrain réel : on lit d'ABORD les dimensions (width/height)
        // dans le SWF, on déchiffre, puis on alloue la carte à la BONNE taille
        // avec la BONNE largeur (sinon id→(x,y) est faux → carte « rien à voir »).
        if (!string.IsNullOrEmpty(dateVersion) && !string.IsNullOrEmpty(clefCarte))
        {
            try
            {
                var infos = ChargeurMapLocale.ChargerInfos(identifiant, dateVersion);
                var clair = !string.IsNullOrEmpty(infos.MapData)
                    ? DechiffreurCarte.DechiffrerDonneesMap(infos.MapData, clefCarte)
                    : DechiffreurCarte.Dechiffrer(clefCarte, dateVersion);

                int nbCells = !string.IsNullOrEmpty(clair)
                    ? clair.Length / 10
                    : Carte.NombreCellules(infos.Largeur, infos.Hauteur);

                carte = new Carte(identifiant, infos.Largeur, infos.Hauteur, nbCells);
                CarteCourante = carte;

                if (!string.IsNullOrEmpty(clair))
                {
                    var n = DecompresseurMapData.Appliquer(carte, clair);
                    Journaliseur.Info(
                        $"[CARTE] {identifiant} : terrain décodé ({n}/{carte.Cellules.Length} cellules, " +
                        $"{carte.Largeur}×{carte.Hauteur})");
                }
                else
                {
                    Journaliseur.Avertir($"[CARTE] {identifiant} : dechiffrement vide (date={dateVersion})");
                }
            }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[CARTE] {identifiant} : échec décodage terrain ({ex.Message})");
                carte = new Carte(identifiant);
                CarteCourante = carte;
            }
        }
        else
        {
            carte = new Carte(identifiant);
            CarteCourante = carte;
        }

        CarteChangee?.Invoke(this, carte);
    }
}
