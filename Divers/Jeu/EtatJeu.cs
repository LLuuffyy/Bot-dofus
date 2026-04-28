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
        var carte = new Carte(identifiant);
        CarteCourante = carte;
        Personnage.CarteCourante = identifiant;

        // Décodage du terrain réel : déchiffre la data GDM puis applique les types cellules.
        if (!string.IsNullOrEmpty(dateVersion) && !string.IsNullOrEmpty(clefCarte))
        {
            try
            {
                var mapDataChiffree = ChargeurMapLocale.ChargerMapData(identifiant, dateVersion);
                var clair = !string.IsNullOrEmpty(mapDataChiffree)
                    ? DechiffreurCarte.DechiffrerDonneesMap(mapDataChiffree, clefCarte)
                    : DechiffreurCarte.Dechiffrer(clefCarte, dateVersion);
                if (!string.IsNullOrEmpty(clair))
                {
                    var n = DecompresseurMapData.Appliquer(carte, clair);
                    Journaliseur.Info($"[CARTE] {identifiant} : terrain décodé ({n}/{carte.Cellules.Length} cellules)");
                }
                else
                {
                    Journaliseur.Avertir($"[CARTE] {identifiant} : dechiffrement vide (date={dateVersion})");
                }
            }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[CARTE] {identifiant} : échec décodage terrain ({ex.Message})");
            }
        }

        CarteChangee?.Invoke(this, carte);
    }
}
