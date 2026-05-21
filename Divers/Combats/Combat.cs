using System;
using System.Collections.Generic;
using BotDofus.Divers.Combats.Combattants;
using BotDofus.Divers.Combats.Enums;

namespace BotDofus.Divers.Combats;

/// <summary>
/// État d'un combat en cours. Regroupe les combattants alliés/ennemis,
/// le tour courant, le timer, et les statistiques.
/// </summary>
public sealed class Combat
{
    public EtatCombat Etat { get; private set; } = EtatCombat.Inactif;
    public int NumeroTour { get; private set; }
    public int IdentifiantAllie { get; set; }
    public int IdentifiantCombattantActuel { get; private set; }

    public List<Combattant> Allies { get; } = new();
    public List<Combattant> Ennemis { get; } = new();
    public List<int> PositionsEquipe1 { get; } = new();
    public List<int> PositionsEquipe2 { get; } = new();
    public int EquipePlacement { get; private set; } = -1;

    /// <summary>
    /// Compteur de lancements par sort pour le TOUR courant. Reset à chaque
    /// <see cref="NouveauTour"/>. Utilisé par <see cref="IA.MoteurReglesCombat"/>
    /// pour appliquer <see cref="IA.RegleSort.NombreParTour"/> (limite SynFus :
    /// « lancer ce sort au max N fois par tour »).
    /// </summary>
    public Dictionary<int, int> CompteursRegleParTour { get; } = new();

    /// <summary>
    /// Compteur de lancements par (sort, cible) pour le TOUR courant. Reset à
    /// chaque <see cref="NouveauTour"/>. Implémente <see cref="IA.RegleSort.NombreParCible"/>
    /// (« max N fois sur la même cible »).
    /// </summary>
    public Dictionary<(int idSort, int idCible), int> CompteursRegleParCible { get; } = new();

    /// <summary>
    /// Dernier numéro de tour où chaque sort a été lancé. Persiste à travers
    /// les tours (pas reset par NouveauTour). Implémente
    /// <see cref="IA.RegleSort.CooldownTours"/> (« relançable tous les N tours »).
    /// Reset par <see cref="Reinitialiser"/> en fin de combat.
    /// </summary>
    public Dictionary<int, int> DernierTourLanceParSort { get; } = new();

    public event EventHandler<EtatCombat>? EtatChange;
    public event EventHandler<int>? TourChange;
    public event EventHandler? PositionsChangees;

    /// <summary>
    /// Émis quand le serveur BROADCAST un déplacement de combat pour MON perso
    /// (broadcast <c>GA;0/1;&lt;monId&gt;;&lt;chemin&gt;</c> reçu). Signal
    /// décisif pour confirmer qu'un GA001 envoyé par l'IA a été accepté
    /// (cf. ADR-002 §4.1). <see cref="IA.PipelineDeplacementCombat"/> écoute
    /// cet event pour résoudre le <see cref="System.Threading.Tasks.TaskCompletionSource{TResult}"/>
    /// de l'attente bloquante avec timeout.
    /// </summary>
    public event EventHandler<MouvementBotArgs>? MouvementBotConfirme;

    /// <summary>
    /// Cells ciblées par nos casts d'invocation (Focus=CelluleVide ou
    /// CelluleAdjacenteEnnemi). Quand le GTM suivant retourne un nouveau
    /// combattant à une de ces cells, il est classé COMME MON INVOCATION
    /// (Allies + EstInvocation=true) au lieu d'ennemi (heuristique id&lt;0).
    /// Vidée par <see cref="OnPet"/> (déprécation : géré dans TrameJeu).
    /// </summary>
    public System.Collections.Generic.HashSet<int> CellsInvocationsAttendues { get; } = new();

    /// <summary>IDs des combattants identifiés comme MES invocations.
    /// Sert au GTM handler à les mettre dans Allies (pas Ennemis) à chaque MAJ.</summary>
    public System.Collections.Generic.HashSet<int> MesInvocationsIds { get; } = new();

    /// <summary>
    /// Sort sélectionné dans l'UI Combat (via clic ⓘ). Permet à MapViewer
    /// de surligner les cellules dans la portée du sort. -1 = pas de sélection.
    /// </summary>
    public int SortSelectionneId { get; private set; } = -1;
    public int SortPorteeMin { get; private set; }
    public int SortPorteeMax { get; private set; }
    public string SortSelectionneNom { get; private set; } = string.Empty;

    /// <summary>Émis quand le sort sélectionné dans VueCombat change.</summary>
    public event EventHandler? SortSelectionneChange;

    public void DefinirSortSelectionne(int idSort, int porteeMin, int porteeMax, string nom)
    {
        SortSelectionneId = idSort;
        SortPorteeMin = porteeMin;
        SortPorteeMax = porteeMax;
        SortSelectionneNom = nom;
        SortSelectionneChange?.Invoke(this, EventArgs.Empty);
    }

    public void EffacerSortSelectionne()
    {
        SortSelectionneId = -1;
        SortPorteeMin = 0;
        SortPorteeMax = 0;
        SortSelectionneNom = string.Empty;
        SortSelectionneChange?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Déclenche l'event <see cref="MouvementBotConfirme"/>. Appelé par
    /// <see cref="Commun.Frames.TrameJeu.OnActionJeu"/> quand un broadcast
    /// <c>GA;0/1</c> est reçu et que l'acteur correspond à <see cref="IdentifiantAllie"/>.
    /// </summary>
    public void DeclencherMouvementBot(int idActeur, int cellArrivee, string cheminEncode)
        => MouvementBotConfirme?.Invoke(this, new MouvementBotArgs
        {
            IdActeur = idActeur,
            CellArrivee = cellArrivee,
            CheminEncode = cheminEncode
        });

    public void Demarrer()
    {
        ChangerEtat(EtatCombat.Placement);
    }

    public void PassageEnCombat()
    {
        ChangerEtat(EtatCombat.EnCours);
    }

    public void NouveauTour(int identifiantCombattant)
    {
        NumeroTour++;
        IdentifiantCombattantActuel = identifiantCombattant;
        CompteursRegleParTour.Clear();
        CompteursRegleParCible.Clear();
        TourChange?.Invoke(this, identifiantCombattant);
    }

    public void DefinirPositionsPlacement(IEnumerable<int> equipe1, IEnumerable<int> equipe2, int equipeCourante)
    {
        PositionsEquipe1.Clear();
        PositionsEquipe1.AddRange(equipe1);
        PositionsEquipe2.Clear();
        PositionsEquipe2.AddRange(equipe2);
        EquipePlacement = equipeCourante;
        ChangerEtat(EtatCombat.Placement);
        PositionsChangees?.Invoke(this, EventArgs.Empty);
    }

    public void Reinitialiser()
    {
        Allies.Clear();
        Ennemis.Clear();
        PositionsEquipe1.Clear();
        PositionsEquipe2.Clear();
        EquipePlacement = -1;
        NumeroTour = 0;
        IdentifiantCombattantActuel = 0;
        CompteursRegleParTour.Clear();
        CompteursRegleParCible.Clear();
        DernierTourLanceParSort.Clear();
        CellsInvocationsAttendues.Clear();
        MesInvocationsIds.Clear();
        ChangerEtat(EtatCombat.Inactif);
        PositionsChangees?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// À appeler après mise à jour des combattants (parseur GTM Abrak) pour
    /// rafraîchir les vues (onglet Combat, grille Carte) sans changement d'état.
    /// </summary>
    public void SignalerCombattantsMaj() => PositionsChangees?.Invoke(this, EventArgs.Empty);

    public bool EstMonTour => IdentifiantCombattantActuel == IdentifiantAllie
                            && Etat == EtatCombat.EnCours;

    private void ChangerEtat(EtatCombat nouveau)
    {
        if (Etat == nouveau) return;
        Etat = nouveau;
        EtatChange?.Invoke(this, nouveau);
    }
}

/// <summary>
/// Args portés par <see cref="Combat.MouvementBotConfirme"/> : id du combattant,
/// cellule d'arrivée annoncée par le broadcast serveur, chemin encodé (utile
/// pour comparer à ce que le bot a envoyé et détecter une troncature serveur
/// = <see cref="IA.ResultatDeplacementCombat.ConfirmePartiel"/>).
/// </summary>
public sealed class MouvementBotArgs : EventArgs
{
    public int IdActeur { get; init; }
    public int CellArrivee { get; init; }
    public string CheminEncode { get; init; } = string.Empty;
}
