using System;
using BotDofus.Commun.Messages.VersClient.Jeu;
using BotDofus.Divers.Jeu;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.MultiAccount;

/// <summary>
/// Observateur de paquets : à chaque <c>GTSX</c> reçu, détecte (ou enrichit) le
/// mode héros. Crée le <see cref="GroupeHeros"/> sur le <see cref="Compte"/> si
/// pas encore présent, puis ajoute le master et les héros liés au fur et à
/// mesure des paquets observés.
///
/// Stratégie sur Abrak (cf. <c>docs/PROTOCOLE-MODE-HEROS-ABRAK.md</c>) : un
/// <c>GTSX&lt;idMaster&gt;;&lt;idLié&gt;;...</c> est émis par le serveur
/// AVANT le 1er tour du combat, 1 paquet par perso lié. Le 1er crée le
/// groupe (avec master = nous), les suivants enrôlent les liés.
///
/// Sur les serveurs sans mode héros (Hystoria mono, Dofus Retro stock), aucun
/// <c>GTSX</c> n'est jamais émis → ce détecteur reste no-op. Le comportement
/// solo legacy est strictement préservé.
/// </summary>
public sealed class DetecteurModeHeros
{
    private readonly Compte _compte;
    private readonly EtatJeu _etat;

    public DetecteurModeHeros(Compte compte, EtatJeu etat)
    {
        _compte = compte ?? throw new ArgumentNullException(nameof(compte));
        _etat = etat ?? throw new ArgumentNullException(nameof(etat));
    }

    /// <summary>
    /// Appelé par <see cref="BotDofus.Commun.Frames.TrameJeu"/> à chaque
    /// <see cref="MessageTourCombatAbrak"/> dont <see cref="MessageTourCombatAbrak.EstGTSX"/> est <c>true</c>.
    /// No-op si le paquet n'est pas un GTSX ou si les ids sont invalides.
    /// </summary>
    public void OnGTSX(MessageTourCombatAbrak msg)
    {
        if (msg is null) return;
        if (!msg.EstGTSX) return;
        if (msg.IdMaster == 0 || msg.IdPersoLie == 0) return;

        // Le master DOIT être notre perso connecté — sinon ce GTSX ne nous
        // concerne pas (cas théorique : on observe un autre joueur).
        var monId = _etat.Personnage.Identifiant;
        if (monId != 0 && msg.IdMaster != monId)
        {
            Journaliseur.Debogue(
                $"[MODE-HEROS] GTSX ignoré : master {msg.IdMaster} ≠ monId {monId}");
            return;
        }

        var groupe = _compte.GroupeHeros;
        if (groupe is null)
        {
            groupe = new GroupeHeros { Nom = $"groupe-{msg.IdMaster}" };
            _compte.GroupeHeros = groupe;

            groupe.AjouterMembre(new MembreHeros
            {
                Identifiant = _compte.Identifiant,
                IdJeu = msg.IdMaster,
                Role = RoleDansGroupe.Leader,
                Nom = _etat.Personnage.Nom ?? string.Empty,
                IdClasse = _etat.Personnage.IdClasse,
                Niveau = _etat.Personnage.Niveau,
            });
            groupe.Activer();
            Journaliseur.Info(
                $"[MODE-HEROS] Détection ACTIVE — master {msg.IdMaster}, groupe créé");
        }

        // Ajout du lié (anti-doublon géré par GroupeHeros.AjouterMembre).
        if (groupe.TrouverParIdJeu(msg.IdPersoLie) is null)
        {
            groupe.AjouterMembre(new MembreHeros
            {
                IdJeu = msg.IdPersoLie,
                Role = RoleDansGroupe.Suiveur,
                // Nom/Classe/Niveau enrichis ultérieurement via GTM ou ALK
                // (hors scope Phase 3, voir Phase 5 UI).
            });
            Journaliseur.Info($"[MODE-HEROS] +Héros lié id={msg.IdPersoLie}");
        }
    }
}
