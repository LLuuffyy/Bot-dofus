# Modèle de données — Support Mode Héros

- Statut : Proposé (compagnon de `docs/ARCHITECTURE-MODE-HEROS.md` / ADR-006)
- Date : 2026-05-22
- Auteurs : System Architecture Designer (assist. Claude Opus 4.7)
- Portée : signatures C# détaillées des 3 nouvelles classes, des extensions de classes existantes, du schéma JSON `multiaccount/groupe.json`, et des séquences runtime types.

> ⚠️ Toutes les zones marquées **« À VALIDER »** sont des hypothèses à confirmer par les logs forensic d'un combat mode héros réel. Voir ADR-006 §1.4 pour les 3 points d'incertitude principaux.

---

## 1. Diagramme de classes (texte ASCII)

```
                                                                              ┌──────────────────────────────────────┐
                                                                              │       multiaccount/groupe.json       │
                                                                              │                                      │
                                                                              │  {                                   │
                                                                              │   "groupes": [                       │
                                                                              │     { "id": "grp-...",               │
                                                                              │       "leader": "<Identifiant>",     │
                                                                              │       "membres": [{...}],            │
                                                                              │       "creeLe": "2026-05-22T11:30Z"} │
                                                                              │   ]                                  │
                                                                              │  }                                   │
                                                                              └────────────────┬─────────────────────┘
                                                                                               │ load/save
                                                                                               ▼
┌─────────────────────────────┐ 1   1 ┌───────────────────────────────────┐ 1   N ┌─────────────────────────────┐
│        Compte (POCO)        │ ◄────►│         GroupeHeros               │ ◄────►│        PersonnageRef        │
│                             │       │                                   │       │                             │
│ + Identifiant : string      │       │ + Id : string                     │       │ + Identifiant : string      │
│ + ConfigCombat : ConfigC.   │       │ + EtatActif : bool                │       │ + Role : RoleDansGroupe     │
│ + ConfigBanque : ConfigB.   │       │ + Leader : PersonnageRef          │       │ + ContexteCompte : Cmpt.   │
│ + EstDansGroupeHeros : bool │       │ + Membres : IReadOnlyList<P.Ref>  │       │   (runtime, non sérialisé) │
│ + IdGroupeHeros : string?   │       │ + Combat : Combat (shared)        │       │ + DerniereActivite : DateT. │
│ + RoleDansGroupe : enum     │       │ + OrdreToursCourant : List<int>   │       └─────────────────────────────┘
└─────────────────────────────┘       │ + EstMonoClient : bool            │
                                      │ + EvenementsGroupe : events       │
              ▲                       └─────────────┬─────────────────────┘
              │ 1                                   │ 1
              │                                     │ uses
              │                                     ▼
┌─────────────────────────────┐ 1    ┌───────────────────────────────────┐
│      ContexteCompte         │      │       DispatcheurTours            │
│   (existant, étendu)        │      │                                   │
│                             │      │ - groupe : GroupeHeros            │
│ + Compte : Compte           │      │ - _verrouTour : object            │
│ + EtatJeu : EtatJeu         │      │                                   │
│ + Trames : Gestionnaire     │      │ + Activer() : void                │
│ + Api : ApiBot              │      │ + Desactiver() : void             │
│ + Groupe : GroupeHeros?     │      │ + OnTourChange(idComb) : Task     │
│ + AttacherAuGroupe(...)     │      └───────────────────────────────────┘
│ + DetacherDuGroupe()        │                            ▲
│ + EstContextePartage : bool │                            │ écoute event TourChange du
└─────────────────────────────┘                            │ Combat partagé du GroupeHeros
              ▲                                            │
              │ ref                                        │
              │                                            │
              │                  ┌───────────────────────────────────────┐
              │                  │       DetecteurModeHeros              │
              │                  │                                       │
              │                  │ - _signalA_Im : HashSet<string>       │
              │                  │ - _signalB_GtmSeuil : int (= 2)       │
              │                  │ - _registreComptes : Func<...>        │
              │                  │                                       │
              │                  │ + AnalyserInfo(msg : MessageInfo)     │
              │                  │ + AnalyserCombattants(msg, etat,      │
              │                  │      compte)                          │
              │                  │ + AnalyserPaquetBrut(p : string)      │
              │                  │   (signal C — paquet d'annonce)       │
              │                  │ + event GroupeForme                   │
              │                  │ + event GroupeDissous                 │
              │                  └───────────────────────────────────────┘
              │                                            │ produit
              │                                            │
              └────────────────────────────────────────────┘
                            consommé par MainWindow / GroupeHeros
```

---

## 2. Nouvelles classes — signatures détaillées

### 2.1 `Divers/MultiAccount/PersonnageRef.cs`

DTO simple, sert de pointeur stable vers un perso du groupe. Doit rester sérialisable (utilisé dans `multiaccount/groupe.json`).

```csharp
using System.Text.Json.Serialization;

namespace BotDofus.Divers.MultiAccount;

/// <summary>
/// Référence à un perso membre d'un <see cref="GroupeHeros"/>.
/// Sérialisable JSON (le runtime ContexteCompte est résolu à chaud).
/// </summary>
public sealed class PersonnageRef
{
    /// <summary>Identifiant compte (= <see cref="Compte.Identifiant"/>).</summary>
    public string Identifiant { get; init; } = string.Empty;

    /// <summary>Rôle dans le groupe (Leader = 1 et 1 seul par groupe).</summary>
    public RoleDansGroupe Role { get; set; } = RoleDansGroupe.Suiveur;

    /// <summary>
    /// Référence vers le <see cref="ContexteCompte"/> résolu au runtime.
    /// Null tant que le compte n'est pas chargé en mémoire.
    /// </summary>
    [JsonIgnore]
    public ContexteCompte? Contexte { get; set; }

    /// <summary>Dernier timestamp d'activité (paquet reçu pour ce perso) — diag UI.</summary>
    [JsonIgnore]
    public System.DateTime DerniereActivite { get; set; } = System.DateTime.MinValue;

    /// <summary>
    /// <c>Identifiant</c> jeu (= <see cref="Personnage.Identifiant"/>, int signé).
    /// Sert au matching avec <c>MessageCombattantsAbrak.Combattants[i].Id</c>.
    /// Mis à jour quand le contexte reçoit son <c>ASK</c>.
    /// </summary>
    [JsonIgnore]
    public int IdentifiantJeu => Contexte?.EtatJeu.Personnage.Identifiant ?? 0;
}

public enum RoleDansGroupe
{
    Aucun = 0,
    Leader = 1,
    Suiveur = 2
}
```

### 2.2 `Divers/MultiAccount/GroupeHeros.cs`

Cœur de l'agrégat. Lifecycle complet, état partagé.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using BotDofus.Divers.Combats;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.MultiAccount;

/// <summary>
/// Agrégat racine du mode héros multi-perso : tient la liste ordonnée des membres,
/// l'état combat partagé, l'ordre des tours. Voir ADR-006 pour le contexte.
/// </summary>
public sealed class GroupeHeros : IDisposable
{
    // ----- Identité -----
    public string Id { get; init; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public DateTime CreeLe { get; init; } = DateTime.UtcNow;

    // ----- Composition -----
    private readonly List<PersonnageRef> _membres = new();
    public IReadOnlyList<PersonnageRef> Membres => _membres;
    public PersonnageRef? Leader => _membres.FirstOrDefault(m => m.Role == RoleDansGroupe.Leader);

    // ----- État runtime -----
    /// <summary><c>true</c> dès qu'on a détecté/forcé l'activation du mode héros côté serveur.</summary>
    public bool EstActif { get; private set; }

    /// <summary>
    /// <c>true</c> si toute la session passe par un SEUL client Dofus (1 SessionProxy
    /// pour les 8 persos). <c>false</c> si N clients distincts synchronisés serveur.
    /// Cf. ADR-006 §2.1/§2.2. À VALIDER par capture.
    /// </summary>
    public bool EstMonoClient { get; set; } = true;

    /// <summary>État combat partagé (réplique du <see cref="Combats.Combat"/> du leader).</summary>
    public Combat Combat { get; } = new();

    /// <summary>
    /// Ordre des tours du tour courant, à mesure que les GTS arrivent. Liste
    /// d'<c>IdentifiantCombattant</c> dans l'ordre d'initiative annoncé par le serveur.
    /// Reset à chaque tour 1 d'un nouveau combat.
    /// </summary>
    private readonly List<int> _ordreToursCourant = new();
    public IReadOnlyList<int> OrdreToursCourant => _ordreToursCourant;

    /// <summary>Index du membre actuellement en train de jouer (-1 = aucun).</summary>
    public int IndexMembreActuel { get; private set; } = -1;

    /// <summary>Verrou de mutation (cf. ADR-006 §8.3).</summary>
    private readonly object _verrouEtat = new();

    // ----- Events -----
    public event EventHandler? MembresChanges;
    public event EventHandler<PersonnageRef>? TourDeMembre;
    public event EventHandler? GroupeActive;
    public event EventHandler? GroupeDissous;

    // ----- Composition (modifications) -----
    public void AjouterMembre(PersonnageRef membre)
    {
        lock (_verrouEtat)
        {
            if (_membres.Any(m => m.Identifiant == membre.Identifiant)) return;
            _membres.Add(membre);
        }
        Journaliseur.Info($"[GH:{Id}] Membre ajouté : {membre.Identifiant} ({membre.Role})");
        MembresChanges?.Invoke(this, EventArgs.Empty);
    }

    public bool RetirerMembre(string identifiant)
    {
        bool ok;
        lock (_verrouEtat)
        {
            var trouve = _membres.FirstOrDefault(m => m.Identifiant == identifiant);
            if (trouve == null) return false;
            ok = _membres.Remove(trouve);
        }
        if (ok)
        {
            Journaliseur.Info($"[GH:{Id}] Membre retiré : {identifiant}");
            MembresChanges?.Invoke(this, EventArgs.Empty);
        }
        return ok;
    }

    public PersonnageRef? TrouverParIdJeu(int idJeu)
    {
        lock (_verrouEtat)
        {
            return _membres.FirstOrDefault(m => m.IdentifiantJeu == idJeu);
        }
    }

    public PersonnageRef? TrouverParIdentifiant(string identifiant)
    {
        lock (_verrouEtat)
        {
            return _membres.FirstOrDefault(m => m.Identifiant == identifiant);
        }
    }

    // ----- Lifecycle -----
    public void Activer()
    {
        if (EstActif) return;
        EstActif = true;
        IndexMembreActuel = -1;
        _ordreToursCourant.Clear();
        Journaliseur.Info($"[GH:{Id}] Groupe ACTIVÉ — {_membres.Count} membres, leader={Leader?.Identifiant ?? "?"}");
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
        // Détache les contextes fantômes.
        foreach (var m in _membres) m.Contexte?.DetacherDuGroupe();
        Journaliseur.Info($"[GH:{Id}] Groupe DISSOUS");
        GroupeDissous?.Invoke(this, EventArgs.Empty);
    }

    // ----- Ordre des tours -----
    /// <summary>
    /// Appelé par <c>TrameJeu</c> dès qu'un <c>MessageTourCombatAbrak</c> (GTS) est reçu,
    /// AVANT le filtre legacy. Met à jour le curseur, émet <see cref="TourDeMembre"/>
    /// si le membre courant appartient au groupe.
    /// </summary>
    public void NotifierTourSeServeur(int idCombattant, int numeroTour)
    {
        PersonnageRef? membre;
        lock (_verrouEtat)
        {
            // Reset à chaque nouveau tour 1.
            if (numeroTour == 1 && _ordreToursCourant.Count > 0
                && _ordreToursCourant[0] != idCombattant)
                _ordreToursCourant.Clear();

            if (!_ordreToursCourant.Contains(idCombattant))
                _ordreToursCourant.Add(idCombattant);
            IndexMembreActuel = _ordreToursCourant.IndexOf(idCombattant);
            membre = _membres.FirstOrDefault(m => m.IdentifiantJeu == idCombattant);
        }
        if (membre == null) return; // ce n'est pas un membre du groupe (ennemi/allié externe)
        membre.DerniereActivite = DateTime.UtcNow;
        TourDeMembre?.Invoke(this, membre);
    }

    /// <summary>Passe au membre suivant après <c>Gt</c> confirmé (appelé par dispatcher).</summary>
    public PersonnageRef? PasserAuSuivant()
    {
        lock (_verrouEtat)
        {
            if (_ordreToursCourant.Count == 0 || IndexMembreActuel < 0) return null;
            IndexMembreActuel = (IndexMembreActuel + 1) % _ordreToursCourant.Count;
            int idNext = _ordreToursCourant[IndexMembreActuel];
            return _membres.FirstOrDefault(m => m.IdentifiantJeu == idNext);
        }
    }

    public PersonnageRef? JoueurActuel
    {
        get
        {
            lock (_verrouEtat)
            {
                if (IndexMembreActuel < 0 || IndexMembreActuel >= _ordreToursCourant.Count) return null;
                int id = _ordreToursCourant[IndexMembreActuel];
                return _membres.FirstOrDefault(m => m.IdentifiantJeu == id);
            }
        }
    }

    public void Dispose()
    {
        try { Dissoudre(); } catch { }
    }
}
```

### 2.3 `Divers/MultiAccount/DetecteurModeHeros.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using BotDofus.Commun.Messages.VersClient.Info;
using BotDofus.Commun.Messages.VersClient.Jeu;
using BotDofus.Divers.Jeu;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.MultiAccount;

/// <summary>
/// Observe les paquets pour détecter activation / dissolution du mode héros.
/// Stratégie multi-signaux (cf. ADR-006 §3.2). Tant qu'aucun signal n'est confirmé
/// par capture forensic, reste en NO-OP (la détection s'enclenche via UI manuelle).
/// </summary>
public sealed class DetecteurModeHeros
{
    /// <summary>
    /// Codes Im potentiels qui annoncent l'entrée mode héros (= signal A).
    /// À VALIDER : remplir avec les codes capturés (ex. "Im0155", "Im0xxx").
    /// </summary>
    private static readonly HashSet<string> CodesImEntree = new()
    {
        // "0155",   // hypothèse — à valider
        // "0xxx",
    };

    /// <summary>Nombre min de membres simultanément alliés (id>0) dans un GTM pour suspecter un groupe (= signal B).</summary>
    private const int SeuilGtmGroupe = 2;

    /// <summary>Préfixes paquets candidats pour signal C (annonce dédiée). À VALIDER.</summary>
    private static readonly string[] PrefixesAnnonceCandidats = { "GH", "HG", "Pg", "Pl" };

    /// <summary>Fonction injectée par MainWindow pour résoudre "id jeu → Compte chargé".</summary>
    private readonly Func<int, Compte?> _resolveurCompteParId;

    public DetecteurModeHeros(Func<int, Compte?> resolveurCompteParId)
    {
        _resolveurCompteParId = resolveurCompteParId;
    }

    public event EventHandler<EvenementGroupeForme>? GroupeForme;
    public event EventHandler? GroupeDissous;

    // === Signal A — Info serveur ===
    public void AnalyserInfo(MessageInfoMessage msg)
    {
        // À VALIDER : format exact du paquet Im du mode héros.
        // Hypothèse — un Im du type "Im0xxx;<liste persos>".
        if (!CodesImEntree.Contains(msg.Code)) return;
        Journaliseur.Info($"[GH-DETECT] Signal A : Im{msg.Code} reçu, args={msg.Arguments}");
        // TODO post-capture : parser msg.Arguments pour extraire la composition.
        // GroupeForme?.Invoke(this, new EvenementGroupeForme { ... });
    }

    // === Signal B — Compo GTM ===
    public void AnalyserCombattants(MessageCombattantsAbrak msg, EtatJeu etat, Compte compte)
    {
        // Filtre id>0 = persos joueurs/alliés. Si on en a 2+ qui matchent des Comptes
        // chargés en mémoire avec EstDansGroupeHeros=true, c'est un signal fort.
        var candidats = msg.Combattants
            .Where(c => c.Id > 0)
            .Select(c => (c.Id, Compte: _resolveurCompteParId(c.Id)))
            .Where(t => t.Compte != null && t.Compte.EstDansGroupeHeros)
            .ToList();

        if (candidats.Count < SeuilGtmGroupe) return;
        if (candidats.All(t => t.Compte == compte)) return; // un seul perso = solo

        Journaliseur.Info(
            $"[GH-DETECT] Signal B : {candidats.Count} comptes du groupe détectés dans le même GTM " +
            $"({string.Join(",", candidats.Select(t => t.Compte!.Identifiant))})");

        GroupeForme?.Invoke(this, new EvenementGroupeForme
        {
            IdsJeu = candidats.Select(t => t.Id).ToList(),
            Comptes = candidats.Select(t => t.Compte!).ToList(),
            ContexteHote = compte
        });
    }

    // === Signal C — Paquet d'annonce dédié (BRUT, pré-parsing) ===
    /// <summary>
    /// Hook bas-niveau pour reconnaître un paquet d'annonce serveur spécifique au mode
    /// héros, avant que le parsing message-level ne le jette. À VALIDER.
    /// </summary>
    public void AnalyserPaquetBrut(string contenu)
    {
        foreach (var prefixe in PrefixesAnnonceCandidats)
        {
            if (contenu.StartsWith(prefixe, StringComparison.Ordinal))
            {
                Journaliseur.Debogue($"[GH-DETECT] Signal C candidat : préfixe '{prefixe}' contenu='{(contenu.Length > 80 ? contenu[..80] + "…" : contenu)}'");
                // TODO post-capture : parser réellement le paquet.
            }
        }
    }
}

public sealed class EvenementGroupeForme : EventArgs
{
    public List<int> IdsJeu { get; init; } = new();
    public List<Compte> Comptes { get; init; } = new();
    public Compte? ContexteHote { get; init; }
}
```

### 2.4 `Divers/MultiAccount/DispatcheurTours.cs`

```csharp
using System;
using System.Threading.Tasks;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.MultiAccount;

/// <summary>
/// Sur "tour du membre X" reçu par <see cref="GroupeHeros"/>, route l'event vers le bon
/// <c>ContexteCompte</c> pour exécuter son <c>JouerTourCombatAsync</c> avec sa propre
/// <c>ConfigCombat</c>. Cf. ADR-006 §3.3.
/// </summary>
public sealed class DispatcheurTours
{
    private readonly GroupeHeros _groupe;
    private readonly object _verrouTour = new();
    private bool _attache;

    public DispatcheurTours(GroupeHeros groupe)
    {
        _groupe = groupe;
    }

    public void Activer()
    {
        if (_attache) return;
        _groupe.TourDeMembre += OnTourDeMembre;
        _attache = true;
        Journaliseur.Info("[GH-DISP] Dispatcher activé");
    }

    public void Desactiver()
    {
        if (!_attache) return;
        _groupe.TourDeMembre -= OnTourDeMembre;
        _attache = false;
        Journaliseur.Info("[GH-DISP] Dispatcher désactivé");
    }

    private async void OnTourDeMembre(object? sender, PersonnageRef membre)
    {
        // Sérialisation 1-à-la-fois — un seul tour de groupe en cours.
        Task tache;
        lock (_verrouTour)
        {
            tache = ExecuterTourAsync(membre);
        }
        try { await tache.ConfigureAwait(false); }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[GH-DISP] Erreur tour {membre.Identifiant} : {ex.Message}");
        }
    }

    private async Task ExecuterTourAsync(PersonnageRef membre)
    {
        var ctx = membre.Contexte;
        if (ctx == null)
        {
            Journaliseur.Avertir($"[GH-DISP] Membre {membre.Identifiant} sans ContexteCompte résolu — skip");
            return;
        }
        if (ctx.Compte.ModePassif)
        {
            Journaliseur.Info($"[GH-DISP] {membre.Identifiant} en passif → skip");
            return;
        }
        Journaliseur.Info($"[GH-DISP] >>> Tour de {membre.Identifiant} (rôle {membre.Role})");

        // En mono-client : le ContextCompte fantôme partage la session du leader.
        // Son TrameJeu pousse via Api → SessionProxy du leader. Rien à faire de spécial ici.
        // En N-clients : chaque ContexteCompte a déjà reçu son propre GTS et son
        // handler legacy a déjà appelé JouerTourCombatAsync — le dispatcher ne fait
        // que tracer/synchroniser.
        if (_groupe.EstMonoClient)
        {
            // Déclenchement manuel via une méthode publique exposée par TrameJeu
            // (à ajouter à l'implémentation — pas dans cet ADR : `public Task JouerTourCombatPubAsync()`).
            var trame = ctx.Trames.TrameActive as BotDofus.Commun.Frames.TrameJeu;
            if (trame != null)
            {
                await trame.JouerTourCombatPubAsync().ConfigureAwait(false);
            }
        }
        // Modèle N-clients : pas d'action — le handler TrameJeu legacy a déjà fait le job.
    }
}
```

---

## 3. Extensions de classes existantes

### 3.1 `Divers/Compte.cs` — propriétés annexes

```csharp
public sealed class Compte : IEffacable
{
    // ... existant ...

    /// <summary>Membre actif d'un GroupeHeros (cf. ADR-006). Sérialisé dans comptes.json.</summary>
    public bool EstDansGroupeHeros { get; set; }

    /// <summary>Clé du GroupeHeros auquel ce compte appartient (null = aucun). Sérialisé.</summary>
    public string? IdGroupeHeros { get; set; }

    /// <summary>Rôle du compte dans son groupe. Sérialisé.</summary>
    public BotDofus.Divers.MultiAccount.RoleDansGroupe RoleDansGroupe { get; set; }
        = BotDofus.Divers.MultiAccount.RoleDansGroupe.Aucun;
}
```

### 3.2 `Divers/Combats/Combat.cs` — event annexe

```csharp
public sealed class Combat
{
    // ... existant ...

    /// <summary>
    /// Émis par <see cref="DispatcheurTours"/> après <c>Gt</c> confirmé pour un membre.
    /// Sert au curseur d'ordre du groupe. Non utilisé en mode solo (legacy).
    /// </summary>
    public event EventHandler<int>? FinTourMembreGroupe;

    /// <summary>Déclencheur interne.</summary>
    public void DeclencherFinTourMembre(int idCombattant)
        => FinTourMembreGroupe?.Invoke(this, idCombattant);
}
```

### 3.3 `Divers/ContexteCompte.cs` — attache groupe

```csharp
public sealed class ContexteCompte : IDisposable
{
    // ... existant ...

    /// <summary>Groupe auquel ce contexte appartient (null si solo). Backref runtime.</summary>
    public BotDofus.Divers.MultiAccount.GroupeHeros? Groupe { get; private set; }

    /// <summary>
    /// <c>true</c> si ce contexte est un "fantôme" qui partage le SessionProxy du leader
    /// (mode mono-client). Permet à <see cref="Dispose"/> de skip l'arrêt du proxy.
    /// </summary>
    public bool EstContextePartage { get; private set; }

    public void AttacherAuGroupe(BotDofus.Divers.MultiAccount.GroupeHeros groupe, bool estLeader)
    {
        Groupe = groupe;
        if (!estLeader && groupe.EstMonoClient)
        {
            EstContextePartage = true;
            // Le contexte n'a pas/plus de SessionProxy propre — on lui injecte celle
            // du leader pour que ses envois (GA300/Gt) partent au bon endroit.
            var sessionLeader = groupe.Leader?.Contexte?.SessionJeuActive;
            if (sessionLeader != null)
                Api.LierSession(sessionLeader);
        }
        Compte.IdGroupeHeros = groupe.Id;
        Compte.EstDansGroupeHeros = true;
        Compte.RoleDansGroupe = estLeader
            ? BotDofus.Divers.MultiAccount.RoleDansGroupe.Leader
            : BotDofus.Divers.MultiAccount.RoleDansGroupe.Suiveur;
    }

    public void DetacherDuGroupe()
    {
        Groupe = null;
        EstContextePartage = false;
        // Compte.EstDansGroupeHeros / IdGroupeHeros conservés pour reformation auto.
    }
}
```

### 3.4 `Commun/Frames/TrameJeu.cs` — expose `JouerTourCombatPubAsync`

Méthode publique nouvelle (signature uniquement, l'implémentation appelle juste l'existant) :

```csharp
public Task JouerTourCombatPubAsync() => JouerTourCombatAsync();
```

Plus les **3 hooks** décrits dans ADR-006 §5 (signaux A/B + notification ordre tours).

---

## 4. Schéma JSON `multiaccount/groupe.json`

```json
{
  "$schema": "internal/multiaccount-groupe-v1",
  "version": 1,
  "groupes": [
    {
      "id": "grp-beiloddurul-2026-05-22",
      "nom": "Sadida + 7 Enutrof",
      "leader": "Beiloddurul",
      "estMonoClient": true,
      "membres": [
        { "identifiant": "Beiloddurul",  "role": "Leader" },
        { "identifiant": "Aerawiol",     "role": "Suiveur" },
        { "identifiant": "Vrottigrat",   "role": "Suiveur" },
        { "identifiant": "Enutrof01",    "role": "Suiveur" },
        { "identifiant": "Enutrof02",    "role": "Suiveur" },
        { "identifiant": "Enutrof03",    "role": "Suiveur" },
        { "identifiant": "Enutrof04",    "role": "Suiveur" },
        { "identifiant": "Enutrof05",    "role": "Suiveur" }
      ],
      "creeLe": "2026-05-22T11:30:00Z",
      "modifieLe": "2026-05-22T11:30:00Z"
    }
  ]
}
```

**Règles d'invariants** :

- Exactement 1 membre avec `role = "Leader"` par groupe.
- `identifiant` unique dans `membres` ET unique global (un compte ne peut pas être dans 2 groupes).
- `leader` = `identifiant` du membre marqué `Leader` (redondance volontaire pour lookup O(1)).
- `estMonoClient` : initialement `true` (modèle Hystoria probable), peut être patché à `false` après validation forensic si N-clients confirmé.

**Helper de chargement/sauvegarde** : à mettre dans un nouveau `Divers/MultiAccount/StockageGroupes.cs` (statique, miroir de `ConfigCombat.Charger`/`Sauvegarder`).

---

## 5. Séquences runtime — exemples

### 5.1 Démarrage + activation par GTM (signal B, modèle mono-client)

```
User démarre l'app
    │
    ▼
MainWindow.ChargerComptesSauvegardes()
    │ — charge 8 Comptes, dont 8 avec EstDansGroupeHeros=true, IdGroupeHeros="grp-beilo..."
    ▼
MainWindow charge multiaccount/groupe.json
    │ — instancie 1 GroupeHeros provisoire (EstActif=false)
    │ — peuple Membres[] avec PersonnageRef vides (Contexte=null)
    ▼
User clique "Démarrer" sur le compte Beiloddurul (leader)
    │
    ▼
new ContexteCompte("Beiloddurul") + Proxy démarré
    │
    ▼
Client Dofus se connecte → AYK → session jeu attachée → ASK reçu
    │ — Personnage.Identifiant = 123456 (id jeu du Sadida)
    ▼
MainWindow.OnSessionJeuAttachee:
    │ — resolveur(Compte.Identifiant) → GroupeHeros provisoire
    │ — GroupeHeros.Membres[0].Contexte = ctxLeader  (Beiloddurul)
    │ — Compte.RoleDansGroupe = Leader (déjà en JSON)
    │
    │ — Pour chaque autre membre du groupe (7 Enutrof) :
    │   - new ContexteCompte("Enutrof01", EstContextePartage=true)
    │   - Ne démarre PAS son proxy
    │   - PersonnageRef.Contexte = ctxFantôme
    │
    ▼
User entre en combat (agression d'un groupe de mobs)
    │
    ▼
Serveur envoie GTM → TrameJeu.OnCombattantsAbrak (sur ctxLeader)
    │ — parse 8 alliés + N ennemis
    │ — HOOK H2 : DetecteurModeHeros.AnalyserCombattants(msg, etat, leader.Compte)
    │     → détecte 8 ids id>0 qui matchent des Comptes EstDansGroupeHeros=true
    │     → émet EvenementGroupeForme
    ▼
MainWindow.OnGroupeForme:
    │ — Pour chaque (id jeu, Compte) :
    │     - Trouve le PersonnageRef correspondant
    │     - Force ctxFantôme.AttacherAuGroupe(groupe, estLeader=false)
    │       → Api.LierSession(leader.SessionJeuActive)
    │ — groupe.Activer()
    │ — new DispatcheurTours(groupe).Activer()
    ▼
[GROUPE ACTIF]
```

### 5.2 Tour d'un perso fantôme (Enutrof01)

```
Serveur envoie GTS<id_Enutrof01> sur le client Dofus du leader
    │
    ▼
SessionProxy du leader décrypte
    ▼
Repartiteur → TrameJeu (du leader)
    ▼
Handler MessageTourCombatAbrak (TrameJeu.cs:157):
    │ — combat.NouveauTour(id_Enutrof01)
    │ — HOOK H3 : _compte.Groupe?.NotifierTourSeServeur(id_Enutrof01, num)
    │     → groupe.IndexMembreActuel mis à jour
    │     → groupe.TourDeMembre émis (membre = Enutrof01)
    │ — Check legacy : id_Enutrof01 != _etat.Personnage.Identifiant (leader=Sadida)
    │   → return (handler legacy skip)
    ▼
DispatcheurTours.OnTourDeMembre(membre=Enutrof01):
    │ — membre.Contexte = ctxFantôme(Enutrof01)
    │ — ctxFantôme.Compte.ConfigCombat = peleas/Enutrof01.json (chargée à création)
    │ — trame = ctxFantôme.Trames.TrameActive (TrameJeu du fantôme)
    │ — await trame.JouerTourCombatPubAsync()
    │     → MoteurReglesCombat.Evaluer(combat=GROUPE.Combat, cfg=peleas/Enutrof01.json, ...)
    │     → règle gagnante : Pelle Fantomatique focus EnnemiLePlusFort
    │     → GA300<idPelle>;<cellEnnemi>
    │     → Api.EnvoyerHumaniseAsync → SessionProxy du leader (partagée)
    │     → serveur reçoit le cast comme provenant du client unique
    │ — Gt envoyé
    ▼
Serveur envoie GTF<id_Enutrof01>, puis GTS<id_Enutrof02>
    │
    ▼ (boucle séquence ci-dessus pour Enutrof02, 03, ..., Sadida)
```

### 5.3 Tour du leader (Sadida) — mode "presque legacy"

```
Serveur envoie GTS<id_Sadida>
    │
    ▼
TrameJeu (leader).MessageTourCombatAbrak:
    │ — combat.NouveauTour(id_Sadida)
    │ — HOOK H3 : groupe.NotifierTourSeServeur(id_Sadida, num)
    │     → TourDeMembre émis (membre = leader)
    │ — Check legacy : id_Sadida == _etat.Personnage.Identifiant → entre dans le bloc
    │ — ModePassif ? non → await JouerTourCombatAsync()
    │     → ConfigCombat = peleas/Beiloddurul.json (Sadida — Ronce/Larme/invoc)
    │     → cast normal, Gt
    ▼
DispatcheurTours.OnTourDeMembre(membre=leader):
    │ — Détecte que le tour leader est en cours OU déjà fini (race possible)
    │ — Si EstMonoClient ET trame déjà lancée par handler legacy → skip
    │ — Sinon (cas N-clients) : pas applicable ici
```

> **Race condition à anticiper** : si le handler legacy ET le dispatcher se déclenchent tous les deux pour le leader, on lance 2× `JouerTourCombatAsync` → double cast. **Mitigation** :
> - le dispatcher détecte que `membre == groupe.Leader && groupe.EstMonoClient` et **skip** (le legacy a déjà fait le job),
> - alternativement, on désactive le check legacy `:173` (`msg.IdentifiantCombattant != _etat.Personnage.Identifiant`) quand `_compte.Groupe != null` ET on laisse **uniquement** le dispatcher gérer.
>
> La 2e solution est plus propre architecturalement (un seul chemin de décision) — à valider à l'implémentation.

### 5.4 Fin de combat + persistance

```
Serveur envoie GE (fin combat) → TrameJeu.MessageFinCombat
    │
    ▼
combat.Reinitialiser()
    │ — Combat partagé du groupe se reset
    │ — GroupeHeros._ordreToursCourant.Clear()
    │ — GroupeHeros.IndexMembreActuel = -1
    │ — GroupeHeros.EstActif RESTE true (persos toujours liés)
    ▼
Stats / UI mis à jour
    ▼
... farm continue, prochain combat reutilise le même GroupeHeros
```

### 5.5 Déconnexion du leader → dissolution

```
Client Dofus du leader se ferme
    │
    ▼
SessionProxy(leader) détecte disconnect → ProxyJeu.Arreter
    │
    ▼
ctxLeader.OnEtatCompteChange(Deconnecte)
    │
    ▼
MainWindow.OnContexteDeconnecte:
    │ — if (ctxLeader.Groupe != null) ctxLeader.Groupe.Dissoudre()
    ▼
GroupeHeros.Dissoudre():
    │ — EstActif = false
    │ — pour chaque membre fantôme : membre.Contexte.DetacherDuGroupe() + Dispose()
    │ — GroupeDissous émis
    ▼
DispatcheurTours.Desactiver()
    ▼
multiaccount/groupe.json conservé tel quel (reformable au prochain démarrage)
```

---

## 6. Tests recommandés à l'implémentation

| Test | Objectif |
|---|---|
| Chargement `multiaccount/groupe.json` malformé | Doit fallback sur groupe vide, **pas crash**. |
| 1 seul Compte chargé sur 8 attendus | Le `GroupeHeros` reste à `EstActif=false`, `MembresChanges` émis avec membres incomplets, badge UI "incomplet". |
| GTM avec 2 alliés id>0 qui ne sont **pas** dans le groupe | Pas d'activation (`Compte` correspondants pas trouvés ou `EstDansGroupeHeros=false`). |
| GTM avec un seul allié du groupe (autres en partyfollow non-héros) | Pas d'activation (seuil signal B = 2). |
| Tour d'un perso fantôme alors que `EstMonoClient=true` mais `Leader.SessionJeuActive==null` | `DispatcheurTours.ExecuterTourAsync` détecte session manquante, log warn, ne crash pas. |
| `Dissoudre()` pendant un tour en cours | Attente fin tour (CompletionSource) avant Dispose des fantômes. |
| Race GTS quasi-simultanés sur 2 clients (modèle N) | `NotifierTourSeServeur` sérialise via `_verrouEtat`, premier wins par `NumeroTour`. |

---

## 7. Glossaire

- **Mode héros** : feature serveur Hystoria/Abrak qui lie N persos à un même joueur, tous engagés dans le même combat.
- **Leader** : perso désigné comme "principal" (le client Dofus actif est celui du leader en modèle mono-client).
- **Suiveur** : autre perso du groupe, sans client Dofus dédié (mono-client) ou avec son propre client synchro (N-clients).
- **Contexte fantôme** : `ContexteCompte` créé pour un suiveur en mono-client — pas de `SessionProxy` propre, partage celle du leader.
- **Signal A/B/C** : 3 sources de détection mode héros (cf. §2.3) — A = `Im` info-serveur, B = compo GTM, C = paquet d'annonce dédié.
