using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Utilitaires.Config;

/// <summary>
/// Importateur du format binaire <c>accounts.bot</c> hérité des bots Hystoria.
///
/// Format :
///   - int32 LE = nombre de comptes
///   - pour chaque compte, 5 chaînes length-prefixed (1 octet + contenu UTF-8) :
///       1. identifiant (login)
///       2. mot de passe
///       3. nom du serveur (« Hystoria »)
///       4. pseudo de personnage (parfois vide)
///       5. GUID de la machine (préfixé par '$')
///   - puis un blob de métadonnées (IP machine, chemin client, signature Base64)
///     qu'on ignore pour l'import.
/// </summary>
public static class ImportateurAccountsBot
{
    public static List<EntreeCompte> Importer(string cheminFichier)
    {
        var resultat = new List<EntreeCompte>();
        if (!File.Exists(cheminFichier))
        {
            Journaliseur.Debogue($"accounts.bot introuvable ({cheminFichier})");
            return resultat;
        }

        try
        {
            using var fs = File.OpenRead(cheminFichier);
            using var lecteur = new BinaryReader(fs, Encoding.UTF8, leaveOpen: false);

            int nombre = lecteur.ReadInt32();
            for (int i = 0; i < nombre; i++)
            {
                var login = LireChaine(lecteur);
                var mdp = LireChaine(lecteur);
                var serveur = LireChaine(lecteur);
                var personnage = LireChaine(lecteur);
                var guid = LireChaine(lecteur);

                resultat.Add(new EntreeCompte
                {
                    Identifiant = login,
                    MotDePasse = mdp,
                    Commentaire = $"Importé depuis accounts.bot ({serveur} — {personnage}) GUID {guid}"
                });
            }

            Journaliseur.Info($"{resultat.Count} comptes importés depuis {cheminFichier}");
        }
        catch (Exception ex)
        {
            Journaliseur.Erreur($"Échec d'import de {cheminFichier}", ex);
        }

        return resultat;
    }

    private static string LireChaine(BinaryReader lecteur)
    {
        byte longueur = lecteur.ReadByte();
        if (longueur == 0) return string.Empty;
        var octets = lecteur.ReadBytes(longueur);
        return Encoding.UTF8.GetString(octets);
    }
}
