using System;
using System.Collections.Generic;
using System.Linq;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.MultiAccount;

/// <summary>
/// Agrégat racine du mode héros multi-perso : tient la liste ordonnée des
/// membres, l'état d'activation, et l'ordre des tours capturé depuis GTL/GTS.
///
/// Cf. <c>docs/ARCHITECTURE-MODE-HEROS.md</c> (ADR-006) et
/// <c>docs/SYNTHESE-MODE-HEROS-PHASE1.md</c>.
///
/// Cycle de vie typique sur Abrak (mono-client) :
///   <list type="bullet">
///   <item>Création vide à la connexion</item>
///   <item><c>Activer()</c> au 1er <c>GTSX</c> du combat (détection mode héros)</item>
///   <item><c>NotifierTourServeur(id, num)</c> à chaque <c>GTS</c> reçu</item>
///   <item><c>PasserAuSuivant()</c> après <c>Gt</c> envoyé / <c>GTR</c> confirmé</item>
///   <item><c>Dissoudre()</c> à la fin du combat (<c>GE</c>) ou à la déconnexion</item>
///   </list>
/// </summary>
public sealed class GroupeHeros : IDisposable
{
    // ----- Identité -----
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public string Nom { get; set; } = string.Empty;
    public DateTime CreeLe { get; init; } = DateTime.UtcNow;

    // ----- Composition -----
    private readonly List<MembreHeros> _membres = new();
    public IReadOnlyList<MembreHeros> Membres => _membres;
    public MembreHeros? Leader => _membres.FirstOrDefault(m => m.Role == RoleDansGroupe.Leader);

    // ----- État runtime -----
    public bool EstActif { get; private set; }

    /// <summary>
    /// <c>true</c> si toute la session passe par un SEUL client Dofus (cas Abrak
    /// — confirmé par forensic). <c>false</c> si N clients distincts synchronisés
    /// serveur (cas futur Aqua/Atoria).
    /// </summary>
    public bool EstMonoClient { get; set; } = true;

    private readonly List<int> _ordreToursCourant = new();
    public IReadOnlyList<int> OrdreToursCourant => _ordreToursCourant;

    /// <summary>Index du combattant actuellement actif dans <see cref="OrdreToursCourant"/> (-1 = aucun).</summary>
    public int IndexMembreActuel { get; private set; } = -1;

    private readonly object _verrouEtat = new();

    // ----- Events -----
    public event EventHandler? MembresChanges;
    public event EventHandler<MembreHeros>? TourDeMembre;
    public event EventHandler? GroupeActive;
    public event EventHandler? GroupeDissous;
    /// <summary>Déclenché quand PV/PA/PM/vivant d'un membre changent (live via GTM).</summary>
    public event EventHandler<MembreHeros>? StatsMembreChange;

    // ----- Composition : mutations -----

    /// <summary>
    /// Ajoute un membre. Anti-doublon par <see cref="MembreHeros.IdJeu"/> (si > 0)
    /// puis par <see cref="MembreHeros.Identifiant"/> (si non vide).
    /// Si <paramref name="membre"/> est <see cref="RoleDansGroupe.Leader"/> et qu'un
    /// leader existe déjà, l'ancien est rétrogradé en <see cref="RoleDansGroupe.Suiveur"/>.
    /// </summary>
    public void AjouterMembre(MembreHeros membre)
    {
        if (membre is null) throw new ArgumentNullException(nameof(membre));
        bool ajoute = false;
        lock (_verrouEtat)
        {
            if (membre.IdJeu != 0 && _membres.Any(m => m.IdJeu == membre.IdJeu)) return;
            if (!string.IsNullOrEmpty(membre.Identifiant)
                && _membres.Any(m => m.Identifiant == membre.Identifiant)) return;

            if (membre.Role == RoleDansGroupe.Leader)
            {
                foreach (var ancien in _membres.Where(m => m.Role == RoleDansGroupe.Leader))
                    ancien.Role = RoleDansGroupe.Suiveur;
            }
            _membres.Add(membre);
            ajoute = true;
        }
        if (!ajoute) return;
        Journaliseur.Info($"[GH:{Id}] +{membre.Nom} (id {membre.IdJeu}, role {membre.Role})");
        MembresChanges?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Retire un membre par son <see cref="MembreHeros.IdJeu"/>.</summary>
    public bool RetirerMembre(int idJeu)
    {
        bool ok;
        lock (_verrouEtat)
        {
            var trouve = _membres.FirstOrDefault(m => m.IdJeu == idJeu);
            if (trouve is null) return false;
            ok = _membres.Remove(trouve);
        }
        if (ok)
        {
            Journaliseur.Info($"[GH:{Id}] -membre id {idJeu}");
            MembresChanges?.Invoke(this, EventArgs.Empty);
        }
        return ok;
    }

    public MembreHeros? TrouverParIdJeu(int idJeu)
    {
        lock (_verrouEtat) return _membres.FirstOrDefault(m => m.IdJeu == idJeu);
    }

    // ----- Lifecycle -----

    public void Activer()
    {
        if (EstActif) return;
        lock (_verrouEtat)
        {
            EstActif = true;
            IndexMembreActuel = -1;
            _ordreToursCourant.Clear();
        }
        Journaliseur.Info($"[GH:{Id}] ACTIVÉ — {_membres.Count} membres, leader={Leader?.Nom ?? "?"}");
        GroupeActive?.Invoke(this, EventArgs.Empty);
    }

    public void Dissoudre()
    {
        if (!EstActif) return;
        lock (_verrouEtat)
        {
            EstActif = false;
            _ordreToursCourant.Clear();
            IndexMembreActuel = -1;
        }
        Journaliseur.Info($"[GH:{Id}] DISSOUS");
        GroupeDissous?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Appelé à chaque <c>GTS</c> reçu (par <c>TrameJeu</c> ou un futur dispatcher).
    /// Met à jour <see cref="OrdreToursCourant"/> et <see cref="IndexMembreActuel"/>.
    /// Émet <see cref="TourDeMembre"/> SI le combattant fait partie du groupe ;
    /// sinon, le combattant est juste tracké dans l'ordre (pour stats UI).
    ///
    /// Le reset entre combats DOIT être déclenché explicitement par l'appelant
    /// via <see cref="ReinitialiserOrdreTours"/> à la fin du combat
    /// (<c>MessageFinCombat</c> côté <c>TrameJeu</c>) : on ne peut pas inférer
    /// la transition « nouveau combat » depuis <paramref name="numeroTour"/> car
    /// tous les combattants jouent leur tour 1 séquentiellement dans un même
    /// combat (1 GTS par combattant).
    ///
    /// Le paramètre <paramref name="numeroTour"/> est conservé pour stats /
    /// diagnostic future, mais n'influence pas la logique d'ordre.
    /// </summary>
    public void NotifierTourServeur(int idCombattant, int numeroTour)
    {
        _ = numeroTour;
        MembreHeros? membre;
        lock (_verrouEtat)
        {
            if (!_ordreToursCourant.Contains(idCombattant))
                _ordreToursCourant.Add(idCombattant);
            IndexMembreActuel = _ordreToursCourant.IndexOf(idCombattant);
            membre = _membres.FirstOrDefault(m => m.IdJeu == idCombattant);
        }
        if (membre is null) return;
        membre.DerniereActivite = DateTime.UtcNow;
        TourDeMembre?.Invoke(this, membre);
    }

    /// <summary>Avance le curseur au combattant suivant de l'ordre courant (modulo).</summary>
    public MembreHeros? PasserAuSuivant()
    {
        lock (_verrouEtat)
        {
            if (_ordreToursCourant.Count == 0 || IndexMembreActuel < 0) return null;
            IndexMembreActuel = (IndexMembreActuel + 1) % _ordreToursCourant.Count;
            int idNext = _ordreToursCourant[IndexMembreActuel];
            return _membres.FirstOrDefault(m => m.IdJeu == idNext);
        }
    }

    /// <summary>Membre actuellement en train de jouer (selon le curseur).</summary>
    public MembreHeros? JoueurActuel
    {
        get
        {
            lock (_verrouEtat)
            {
                if (IndexMembreActuel < 0 || IndexMembreActuel >= _ordreToursCourant.Count) return null;
                int id = _ordreToursCourant[IndexMembreActuel];
                return _membres.FirstOrDefault(m => m.IdJeu == id);
            }
        }
    }

    /// <summary>
    /// Met à jour les stats live d'un membre (PV/PA/PM/cell/vivant) — typiquement
    /// appelé par <c>TrameJeu.OnCombattantsAbrak</c> à chaque <c>GTM</c> reçu.
    /// Émet <see cref="StatsMembreChange"/> uniquement si une valeur a effectivement
    /// changé, pour éviter de spammer l'UI à chaque refresh.
    /// </summary>
    public bool NotifierStatsCombattant(int idJeu, int pv, int pvMax, int pa, int pm, int cellule, bool vivant)
    {
        MembreHeros? membre;
        bool change = false;
        lock (_verrouEtat)
        {
            membre = _membres.FirstOrDefault(m => m.IdJeu == idJeu);
            if (membre is null) return false;
            if (membre.Pv != pv) { membre.Pv = pv; change = true; }
            if (membre.Pa != pa) { membre.Pa = pa; change = true; }
            if (membre.Pm != pm) { membre.Pm = pm; change = true; }
            if (membre.EstVivant != vivant) { membre.EstVivant = vivant; change = true; }
            // PvMax exposé sur MembreHeros via une propriété — ajouté plus bas.
            if (membre.PvMax != pvMax) { membre.PvMax = pvMax; change = true; }
            if (membre.Cellule != cellule) { membre.Cellule = cellule; change = true; }
        }
        if (change)
        {
            membre.DerniereActivite = DateTime.UtcNow;
            StatsMembreChange?.Invoke(this, membre);
        }
        return change;
    }

    /// <summary>Reset l'ordre des tours (à appeler entre 2 combats si désactivation conservée).</summary>
    public void ReinitialiserOrdreTours()
    {
        lock (_verrouEtat)
        {
            _ordreToursCourant.Clear();
            IndexMembreActuel = -1;
        }
    }

    public void Dispose()
    {
        try { Dissoudre(); } catch { /* swallow on dispose */ }
    }
}
