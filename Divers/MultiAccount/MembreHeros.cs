using System;
using System.Text.Json.Serialization;

namespace BotDofus.Divers.MultiAccount;

/// <summary>
/// Représente un perso membre d'un <see cref="GroupeHeros"/>.
///
/// Sur Abrak (mono-client confirmé par capture forensic), les héros liés sont
/// joués serveur-side : le bot n'a pas besoin d'un <see cref="ContexteCompte"/>
/// par lié, juste d'un POCO pour les identifier et tracker leur état
/// (PV/PA/PM affichés dans la VueGroupeHeros). Cf.
/// docs/SYNTHESE-MODE-HEROS-PHASE1.md.
///
/// Sur un éventuel serveur N-clients (Aqua/Atoria), <see cref="Contexte"/>
/// peut être renseigné pour router les actions vers la bonne session.
/// </summary>
public sealed class MembreHeros
{
    /// <summary>Identifiant compte (<see cref="Compte.Identifiant"/>). Vide pour les liés Abrak.</summary>
    public string Identifiant { get; init; } = string.Empty;

    /// <summary>Identifiant jeu (= <c>Personnage.Identifiant</c> côté serveur, int signé).</summary>
    public int IdJeu { get; set; }

    public string Nom { get; set; } = string.Empty;

    /// <summary>Identifiant classe Dofus Retro (Sadida=10, Enutrof=6, ...).</summary>
    public int IdClasse { get; set; }

    public int Niveau { get; set; }

    /// <summary>Leader = pilotable par le bot ; Suiveur = géré serveur sur Abrak.</summary>
    public RoleDansGroupe Role { get; set; } = RoleDansGroupe.Suiveur;

    /// <summary>
    /// Référence vers le <see cref="ContexteCompte"/> du membre, si applicable.
    /// Null pour les liés Abrak (le serveur joue, pas de session bot dédiée).
    /// </summary>
    [JsonIgnore]
    public ContexteCompte? Contexte { get; set; }

    [JsonIgnore]
    public DateTime DerniereActivite { get; set; } = DateTime.MinValue;

    [JsonIgnore]
    public bool EstVivant { get; set; } = true;

    [JsonIgnore]
    public int Pv { get; set; }

    [JsonIgnore]
    public int PvMax { get; set; }

    [JsonIgnore]
    public int Pa { get; set; }

    [JsonIgnore]
    public int Pm { get; set; }

    /// <summary>Dernière cellule observée (depuis <c>GTM</c>).</summary>
    [JsonIgnore]
    public int Cellule { get; set; }

    /// <summary>
    /// Config combat persistée par perso lié (<c>peleas/heros/&lt;idJeu&gt;.json</c>).
    /// Sur Abrak, n'a pas d'effet runtime (le serveur joue les liés), mais
    /// permet de préparer la base pour un éventuel serveur N-clients où la
    /// config par perso pilote son IA.
    /// </summary>
    [JsonIgnore]
    public BotDofus.Divers.Combats.IA.ConfigCombat? ConfigCombat { get; set; }
}

/// <summary>Rôle d'un <see cref="MembreHeros"/> dans son <see cref="GroupeHeros"/>.</summary>
public enum RoleDansGroupe
{
    Aucun = 0,
    /// <summary>Compte master, pilotable par le bot (un seul par groupe).</summary>
    Leader = 1,
    /// <summary>Héros lié — sur Abrak, le serveur joue son tour ; le bot l'observe.</summary>
    Suiveur = 2,
}
