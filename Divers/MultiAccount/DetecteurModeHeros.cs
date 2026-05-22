using System;
using System.Linq;
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

    /// <summary>
    /// Appelé par <see cref="BotDofus.Commun.Frames.TrameJeu"/> à chaque
    /// <see cref="MessagePartyMembres"/> reçu. Crée/met à jour le GroupeHeros
    /// dès qu'on observe un groupe formé HORS combat (l'invitation Party
    /// Dofus régulière → PM avec &gt;1 membre). Bien plus tôt que GTSX qui
    /// n'arrive qu'au 1er combat.
    /// </summary>
    public void OnPartyMembres(MessagePartyMembres msg)
    {
        if (msg is null) return;
        if (msg.Membres.Count == 0) return;

        var monId = _etat.Personnage.Identifiant;
        // On ne fait quelque chose que si NOUS faisons partie du groupe envoyé.
        if (monId != 0 && msg.Membres.All(m => m.Id != monId))
        {
            // Pas notre groupe — peut être un PM d'observation.
            return;
        }

        var groupe = _compte.GroupeHeros;
        if (groupe is null)
        {
            groupe = new GroupeHeros { Nom = $"groupe-{monId}" };
            _compte.GroupeHeros = groupe;
            Journaliseur.Info($"[MODE-HEROS] Détection via PM — groupe créé pour master {monId}");
        }

        foreach (var m in msg.Membres)
        {
            if (m.Operation == '-')
            {
                groupe.RetirerMembre(m.Id);
                continue;
            }
            var existant = groupe.TrouverParIdJeu(m.Id);
            if (existant is null)
            {
                groupe.AjouterMembre(new MembreHeros
                {
                    IdJeu = m.Id,
                    Nom = m.Nom,
                    Niveau = m.Niveau,
                    Role = m.Id == monId ? RoleDansGroupe.Leader : RoleDansGroupe.Suiveur,
                    Pv = m.Pv,
                    PvMax = m.PvMax,
                });
                Journaliseur.Info($"[MODE-HEROS] +Membre via PM : {m.Nom} (id {m.Id}, niv {m.Niveau})");
            }
            else
            {
                // Update : on enrichit seulement les champs jusqu'ici vides.
                if (string.IsNullOrWhiteSpace(existant.Nom) && !string.IsNullOrWhiteSpace(m.Nom))
                    existant.Nom = m.Nom;
                if (existant.Niveau == 0 && m.Niveau > 0)
                    existant.Niveau = m.Niveau;
                if (existant.PvMax == 0 && m.PvMax > 0)
                {
                    existant.PvMax = m.PvMax;
                    existant.Pv = m.Pv;
                }
            }
        }

        if (!groupe.EstActif) groupe.Activer();
    }

    /// <summary>Hook PL — promotion du leader.</summary>
    public void OnPartyLeader(MessagePartyLeader msg)
    {
        if (msg is null || msg.IdLeader == 0) return;
        var groupe = _compte.GroupeHeros;
        if (groupe is null) return;
        var nouveau = groupe.TrouverParIdJeu(msg.IdLeader);
        if (nouveau is null || nouveau.Role == RoleDansGroupe.Leader) return;
        // Rétrograde l'ancien leader.
        foreach (var m in groupe.Membres)
            if (m.Role == RoleDansGroupe.Leader && m.IdJeu != msg.IdLeader)
                m.Role = RoleDansGroupe.Suiveur;
        nouveau.Role = RoleDansGroupe.Leader;
        Journaliseur.Info($"[MODE-HEROS] Leader → {nouveau.Nom} (id {msg.IdLeader})");
    }
}
