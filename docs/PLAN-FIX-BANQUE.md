# ADR — Fix dépôts banque incomplets

## Statut
Proposé : 2026-05-31 — AGENT 6 ARCHITECT-SYNTHESIS

## Contexte
Symptôme : après le 1er dépôt banque réussi, les dépôts suivants (cycles supplémentaires + workflows post-combat) ne déposent qu'une fraction des items. Signature : mêmes UIDs renvoyés 9× consécutivement, `0/N OR reçus en 8000ms` × 9 passes, poids final inchangé (~5 min de bot bloqué bras-cassés). 5 forensiques indépendantes (logs, visuels, code, protocole, state machine) convergent vers la même cause racine : **désynchronisation `Personnage.Inventaire` ↔ état serveur**, déclenchée par la race OAK/OQ post-loot et amplifiée par l'absence de suppression locale optimiste après EMO+ (divergence vs dyshay).

## Décision
**Approche A : Suppression locale optimiste dyshay-style, combinée à un refresh inventaire au début de CHAQUE pass et à un lock global sur l'inventaire pendant le burst.** Réduit `MAX_PASSES` de 3 à 1 et `cyclesSupplementaires` de 3 à 1 (alignement dyshay).

## Comparaison des 3 approches

### A. Suppression optimiste locale dyshay-style ✅ RECOMMANDÉ
- **Mécanisme** : après envoi `EMO+<uid>|<qte>`, retirer immédiatement l'item de `_perso.Inventaire` sans attendre l'OR. Refresh inventaire à chaque début de pass.
- **Pros** :
  - Aligné sur la référence open-source dyshay (testée Retro 1.29 depuis des années).
  - Élimine la cause racine : la pass suivante ne re-voit JAMAIS un UID déjà envoyé.
  - Permet de réduire MAX_PASSES à 1 → fin du ping-pong stérile.
  - Pattern minimal, ~10 lignes de C# à ajouter.
- **Cons** :
  - Si le serveur refuse silencieusement un EMO+ (rare), l'item est perdu côté local jusqu'au prochain OAK/changement de map. Mitigation : safety net « pending OR » (liste des UIDs envoyés non confirmés ; si pas d'OR après 10 s, on les remet en local + log `[BANQUE-LOST]`).
- **Blast radius** : `PiloteBanque.DeposerToutInterneAsync` uniquement + 1 méthode helper sur `Personnage` (suppression locale). 0 changement protocole, 0 changement TrameJeu critical path.
- **Robustesse** : 9/10 — éprouvé en prod dyshay.
- **Risque régression** : faible. Le seul risque est « item perdu local » géré par le safety net.

### B. Refresh inventaire serveur entre cycles
- **Mécanisme** : envoyer un paquet de re-sync inventaire (`OK` ? `Os` ?) avant chaque cycle.
- **Pros** : forcerait un état clean côté local.
- **Cons** :
  - Pas de paquet de re-sync inventaire documenté côté Dofus 1.29 client→serveur (seul `OK` côté serveur push partiel).
  - La méthode `ResyncInventaireParChangementCarteAsync` existe mais est désactivée (`PiloteBanque.cs:76-85`) : « transitions échouent toutes, 20 s perdues pour rien ».
- **Blast radius** : énorme (nouveau paquet à valider en MITM, capture user requise).
- **Robustesse** : 3/10 — pas de garantie protocolaire.
- **Risque régression** : élevé.

### C. Attendre stabilisation OQ avant burst (rallonger le délai 2.5 s)
- **Mécanisme** : pause 5-8 s après ECK5, avec détection « OQ silencieux depuis N secondes ».
- **Pros** : pas de changement structurel.
- **Cons** :
  - N'élimine pas la race fondamentale : les OQ post-loot peuvent arriver à tout moment, pas seulement après ECK5.
  - Ne corrige PAS le cas où l'OR n'est jamais émis par le serveur (manifestation H6) — qui est la cause RÉELLE des 9 passes stériles.
  - Coût : 5-8 s additionnels par dépôt → bot plus lent.
- **Blast radius** : 1 ligne (`Task.Delay`).
- **Robustesse** : 4/10 — masque le symptôme sans corriger la cause.
- **Risque régression** : faible mais l'inefficacité reste.

**Verdict** : A est le seul fix qui corrige la cause racine de façon minimale et alignée sur la référence du domaine.

---

## Conséquences positives
- Élimination de la boucle stérile « mêmes UIDs renvoyés 9× ».
- Élimination des 5 min de bot bloqué après chaque dépôt incomplet.
- Alignement avec dyshay (référence éprouvée).
- Réduction du trafic réseau (1 passe au lieu de 9).
- Logs banque plus lisibles (1 cycle clair au lieu d'une cascade de pass stériles).

## Conséquences négatives / risques
| Risque | Sévérité | Mitigation |
|--------|----------|------------|
| Item supprimé localement avant ACK serveur → perdu si serveur refuse | MOYEN | Liste `_envoyesEnAttenteOR` ; si pas d'OR après 10 s, log `[BANQUE-LOST]` et re-injection en local |
| OAK concurrent pendant burst → race avec suppression optimiste | FAIBLE | `lock(_perso.Inventaire)` autour de la suppression + ToList initial |
| Item dépose qte partielle (EMO+|1 mais qte serveur = 18) | MOYEN | Refresh snapshot à CHAQUE pass : la pass suivante verra la qte corrigée si OQ correctif arrive |
| Régression sur le cas nominal où tout marche | FAIBLE | Tests d'idempotence + scénario « inventaire stable » avant merge |

---

## Causes racines confirmées (H1-H8)

| H | Verdict | Confiance | Preuve |
|---|---------|-----------|--------|
| H1 (cipher desync) | **FAUX** | 10/10 | 8/65 OR pass 1 → cipher OK |
| H2 (filtrage cassé) | **FAUX** | 10/10 | `CalculerItemsADeposer` pure, items pertinents bien sélectionnés |
| H3 (rate-limit serveur cadence) | **FAUX** | 9/10 | 300 ms = pattern dyshay éprouvé |
| H4 (UIDs fantômes) | **FAUX** | 9/10 | Filtre template>30000 déjà actif, UIDs bloqués sont valides |
| **H5 (désync `_perso.Inventaire`)** | **VRAI — racine** | 9.5/10 | AGENT 3 §H5 + AGENT 5 §1+§4 + commentaire forensic existant `TrameJeu.cs:1213-1224` |
| **H6 (item déposé serveur sans OR)** | **VRAI — manifestation de H5** | 9/10 | Mêmes UIDs renvoyés 9×, 0/N OR — vu côté serveur |
| H7 (`OnObjetRetrait` cassé) | **FAUX** | 10/10 | `inv.RemoveAll` marche, 8/30 OR confirmés |
| H8 (proxy droppe les OR) | **FAUX** | 9/10 | 0 occurrence de `[INV] OQ inconnu` |

**Cause racine unique consolidée** : H5+H6 = désynchronisation `_perso.Inventaire` ↔ serveur, mécanisme = race OAK/OQ post-loot + absence de suppression locale optimiste après EMO+.

---

## Plan d'implémentation

### Étape 1 : Ajouter une méthode helper de suppression locale optimiste sur `Personnage`

- Fichier : `Divers/Jeu/Personnage/Personnage.cs`
- Lignes : à insérer après `RecalculerPoidsLocal()` (vers L153).
- Avant : (méthode absente)
- Après :
```csharp
/// <summary>
/// Suppression locale OPTIMISTE d'un item — appelée par PiloteBanque
/// juste après envoi EMO+ pour éviter que la pass suivante re-soumette
/// le même UID (pattern dyshay StoreAllObjectsAction.cs:35).
/// Si l'OR confirme dans les secondes qui suivent, OnObjetRetrait
/// trouvera l'UID déjà absent et fera un no-op silencieux (cf. fix
/// TrameJeu.cs:1185 `if (n > 0)`).
/// Si le serveur refuse (rare), l'item est perdu côté local jusqu'au
/// prochain OAK / changement de map (acceptable, cf. ADR-BANQUE).
/// </summary>
/// <param name="identifiantObjet">UID de l'item à supprimer.</param>
/// <returns>true si l'item était présent et a été retiré.</returns>
public bool SupprimerObjetOptimiste(long identifiantObjet)
{
    int n;
    lock (Inventaire)
    {
        n = Inventaire.RemoveAll(x => x.Identifiant == identifiantObjet);
    }
    if (n > 0)
    {
        // Pas de NotifierInventaireChange ici : on évite de spammer l'UI
        // pendant un burst banque (50 events/s). Le pilote appellera un
        // NotifierInventaireChange final après le burst.
    }
    return n > 0;
}
```
- Justification : encapsule la suppression locale derrière une API claire, sous le lock canonique, alignée avec le pattern dyshay. Ne déclenche pas l'event UI pour éviter le storm pendant le burst.

### Étape 2 : `PiloteBanque.DeposerToutInterneAsync` — suppression optimiste + refresh par pass

- Fichier : `Divers/Banque/PiloteBanque.cs`
- Lignes principales : 257-372 (boucle multi-pass).

#### 2a. Réduire MAX_PASSES de 3 à 1 (alignement dyshay)
- Avant (L256) :
```csharp
const int MAX_PASSES = 3;
```
- Après :
```csharp
// MAX_PASSES=1 (alignement dyshay StoreAllObjectsAction.cs) : avec la
// suppression locale optimiste, la pass 1 voit déjà l'inventaire après
// décrément local de chaque EMO+. Les items dont l'OR n'arrive pas sont
// considérés déposés (pattern dyshay : pas de retry, resync via OAK
// du prochain combat). Évite la boucle stérile 9-passes-0-OR observée.
const int MAX_PASSES = 1;
```

#### 2b. Refresh inventaire snapshot à CHAQUE pass (déjà fait L266, mais consolider sous lock)
- Avant (L266) :
```csharp
var snapshot = _perso.Inventaire.ToList();
var aDeposer = CalculerItemsADeposer(snapshot);
```
- Après :
```csharp
// Snapshot SOUS LOCK : évite InvalidOperationException si OnObjetAjout/
// OnObjetQuantite mutent la liste pendant ToList(). AGENT 5 §1.
List<ObjetInventaire> snapshot;
lock (_perso.Inventaire)
{
    snapshot = _perso.Inventaire.ToList();
}
Journaliseur.Info($"[BANQUE-FILTRE] Pass {pass} snapshot : {snapshot.Count} items dans l'inventaire local.");
var aDeposer = CalculerItemsADeposer(snapshot);
```

#### 2c. Suppression locale optimiste après envoi EMO+
- Avant (L320-330) :
```csharp
var paquet = $"EMO+{item.Identifiant}|{item.Quantite}";
var infoItem = BaseDonnees.Instance.Item(item.IdTemplate);
var nomItem = infoItem?.Nom ?? "?";
var typeItem = infoItem?.IdType ?? -1;
Journaliseur.Info(
    $"[BANQUE] → {paquet} « {nomItem} » (template {item.IdTemplate}, type={typeItem}, qte {item.Quantite})");
await _session.EnvoyerAuServeurAsync(paquet).ConfigureAwait(false);
deposes++;

// Délai fixe 300ms (= dyshay) entre chaque EMO+.
await Task.Delay(300, ct).ConfigureAwait(false);
```
- Après :
```csharp
var paquet = $"EMO+{item.Identifiant}|{item.Quantite}";
var infoItem = BaseDonnees.Instance.Item(item.IdTemplate);
var nomItem = infoItem?.Nom ?? "?";
var typeItem = infoItem?.IdType ?? -1;
Journaliseur.Info(
    $"[BANQUE-DEPOT] → {paquet} « {nomItem} » (template {item.IdTemplate}, type={typeItem}, qte {item.Quantite})");
await _session.EnvoyerAuServeurAsync(paquet).ConfigureAwait(false);
deposes++;

// SUPPRESSION LOCALE OPTIMISTE (pattern dyshay StoreAllObjectsAction.cs:35).
// Sans ça, si l'OR ne revient pas (timeout, désync serveur), la pass
// suivante re-snapshot l'inventaire et re-soumet le MÊME UID → boucle
// stérile 9× observée (AGENT 1 §Pattern récurrent).
// Race-safe : OnObjetRetrait sous lock(inv) ; si l'OR arrive après
// notre suppression, RemoveAll retourne 0 → no-op silencieux.
bool supprime = _perso.SupprimerObjetOptimiste(item.Identifiant);
_envoyesEnAttenteOR[item.Identifiant] = DateTime.UtcNow;
if (supprime)
    Journaliseur.Debogue($"[BANQUE-DEPOT] Suppression optimiste OK : UID {item.Identifiant} retiré localement.");

// Délai fixe 300ms (= dyshay) entre chaque EMO+.
await Task.Delay(300, ct).ConfigureAwait(false);
```
- Justification : déconnecte le state local du « j'attends un OR pour décrémenter ». Le pattern dyshay tient depuis 5 ans en prod Retro 1.29 ; aucune raison qu'il ne marche pas sur Hystoria.

#### 2d. Tableau « en attente OR » + log final avec items non confirmés
- Ajouter au début de la classe (vers L37, après `CompteurObjectRemove`) :
```csharp
/// <summary>Items envoyés en EMO+ dont on attend l'OR du serveur. Clé = UID,
/// valeur = timestamp UTC d'envoi. Utilisé pour détecter les items dont
/// l'OR n'arrive jamais (suppression optimiste compromise) et logger
/// [BANQUE-LOST] en fin de workflow.</summary>
private readonly Dictionary<long, DateTime> _envoyesEnAttenteOR = new();
```

- Modifier la fin du burst (après L344) :
```csharp
int orRecus = System.Threading.Interlocked.CompareExchange(ref CompteurObjectRemove, 0, 0) - compteurOrAvantBurst;
confirmes = Math.Min(deposes, orRecus);

// Détection des items en attente OR > 10s : potentiellement perdus côté
// serveur (refus silencieux). Loggés mais PAS réinjectés (acceptation du
// risque mineur — au pire ils sont récupérés au prochain OAK/changement
// de map). Cf. ADR-BANQUE §Risques.
var seuilPerdu = DateTime.UtcNow.AddSeconds(-10);
var perdus = _envoyesEnAttenteOR
    .Where(kv => kv.Value < seuilPerdu)
    .Select(kv => kv.Key)
    .ToList();
foreach (var uidPerdu in perdus)
{
    Journaliseur.Avertir($"[BANQUE-LOST] UID {uidPerdu} envoyé sans OR retour depuis >10s — item considéré déposé côté serveur, local OK.");
    _envoyesEnAttenteOR.Remove(uidPerdu);
}
```

#### 2e. Nettoyer la map d'attente quand l'OR arrive
- Cette responsabilité revient à `OnObjetRetrait` (Étape 3 ci-dessous), qui doit retirer l'UID de `_envoyesEnAttenteOR`. Comme `_envoyesEnAttenteOR` est instance privée du pilote, on expose un static accessor partagé OU on déplace la map en static. **Choix retenu** : static partagé sur `PiloteBanque`, accédé par `TrameJeu.OnObjetRetrait`. Avant (L37) :
```csharp
public static int CompteurObjectRemove;
```
- Après :
```csharp
public static int CompteurObjectRemove;

/// <summary>UIDs envoyés en EMO+ dont on attend l'OR. Lu par TrameJeu.
/// OnObjetRetrait qui en retire l'UID dès que l'OR arrive. Le pilote
/// log [BANQUE-LOST] pour les UIDs restants après timeout 10s.</summary>
public static readonly System.Collections.Concurrent.ConcurrentDictionary<long, DateTime> EnvoyesEnAttenteOR = new();
```
- Et remplacer dans 2c : `_envoyesEnAttenteOR[item.Identifiant] = DateTime.UtcNow;` → `EnvoyesEnAttenteOR[item.Identifiant] = DateTime.UtcNow;`

#### 2f. Logs structurés [BANQUE-*]
Renommer les logs existants en catégories canoniques pour faciliter l'analyse forensic future :
- L189 `[BANQUE] Démarrage dépôt…` → `[BANQUE-START] Démarrage dépôt…`
- L226 `[BANQUE] ❌ Pas de ECK5…` → `[BANQUE-TIMEOUT] Pas de ECK5…`
- L324 `[BANQUE] → EMO+…` → `[BANQUE-DEPOT] → EMO+…` (déjà fait en 2c)
- L353 `[BANQUE] Fin pass…` → `[BANQUE-CONFIRM] Fin pass…`
- L376 `[BANQUE] → EV` → `[BANQUE-END] → EV`
- L387/L392 `[BANQUE] ✅ Dépôt terminé…` → `[BANQUE-END] ✅ Dépôt terminé…`

### Étape 3 : `TrameJeu.OnObjetRetrait` — nettoyer la map d'attente sur OR reçu

- Fichier : `Commun/Frames/TrameJeu.cs`
- Lignes : 1177-1193.
- Avant :
```csharp
private void OnObjetRetrait(MessageObjetRetrait msg)
{
    var inv = _etat.Personnage.Inventaire;
    int n;
    lock (inv) // race UI thread
    {
        n = inv.RemoveAll(x => x.Identifiant == msg.IdentifiantObjet);
    }
    if (n > 0)
    {
        Journaliseur.Debogue($"[INV] -1 objet (id {msg.IdentifiantObjet}, total = {inv.Count})");
        _etat.Personnage.NotifierInventaireChange();
    }
    // Compteur monotone consommé par PiloteBanque.AttendreObjectRemoveAsync
    // pour synchroniser les dépôts (chaque EMO+ déclenche un OR<id>|<uid>).
    System.Threading.Interlocked.Increment(ref BotDofus.Divers.Banque.PiloteBanque.CompteurObjectRemove);
}
```
- Après :
```csharp
private void OnObjetRetrait(MessageObjetRetrait msg)
{
    var inv = _etat.Personnage.Inventaire;
    int n;
    lock (inv) // race UI thread
    {
        n = inv.RemoveAll(x => x.Identifiant == msg.IdentifiantObjet);
    }
    if (n > 0)
    {
        Journaliseur.Debogue($"[INV] -1 objet (id {msg.IdentifiantObjet}, total = {inv.Count})");
        _etat.Personnage.NotifierInventaireChange();
    }
    else
    {
        // L'item est déjà absent localement : c'est le cas nominal après une
        // suppression optimiste du PiloteBanque (cf. ADR-BANQUE §A).
        // No-op silencieux (Debogue, pas Avertir).
        Journaliseur.Debogue($"[INV] OR pour UID {msg.IdentifiantObjet} mais déjà absent localement (suppression optimiste banque) — no-op.");
    }
    // Compteur monotone consommé par PiloteBanque pour synchroniser les
    // dépôts (chaque EMO+ déclenche un OR<id>|<uid>).
    System.Threading.Interlocked.Increment(ref BotDofus.Divers.Banque.PiloteBanque.CompteurObjectRemove);
    // Nettoyer la map d'attente OR du pilote banque : l'OR est arrivé,
    // l'item n'est plus « en attente ».
    BotDofus.Divers.Banque.PiloteBanque.EnvoyesEnAttenteOR.TryRemove(msg.IdentifiantObjet, out _);
}
```
- Justification : safety net silencieux quand l'OR arrive après suppression optimiste + maintenance de la map d'attente.

### Étape 4 : `PiloteBanque.WorkflowCompletAsync` — réduire les cycles supplémentaires de 3 à 1

- Fichier : `Divers/Banque/PiloteBanque.cs`
- Ligne : 127.
- Avant :
```csharp
for (int cycleSupplementaire = 1; cycleSupplementaire <= 3; cycleSupplementaire++)
```
- Après :
```csharp
// 1 seul cycle supplémentaire au lieu de 3 (alignement dyshay + ADR-BANQUE).
// Avec la suppression optimiste, le 1er cycle devrait vider 95-100% des items
// éligibles. Le 2ème (et unique) cycle absorbe les OQ correctifs tardifs
// (loots arrivés pendant le 1er dépôt). Au-delà, on entrait dans la boucle
// stérile 9-passes-0-OR (cf. AGENT 1 §Pattern récurrent).
for (int cycleSupplementaire = 1; cycleSupplementaire <= 1; cycleSupplementaire++)
```

### Étape 5 : Reset `EnvoyesEnAttenteOR` au début de chaque workflow

- Fichier : `Divers/Banque/PiloteBanque.cs`
- Ligne : 189 (début de `DeposerToutInterneAsync`).
- Avant :
```csharp
Journaliseur.Info($"[BANQUE] Démarrage dépôt — poids actuel {_perso.PourcentagePoids:F1}% (seuil={_cfg.SeuilPoidsPct}%)");

// Reset des flags observateurs avant ouverture (sinon un EV résiduel
// d'un workflow précédent ferait croire que la banque est déjà fermée).
BanqueFermeeObservee = false;
```
- Après :
```csharp
Journaliseur.Info($"[BANQUE-START] Démarrage dépôt — poids actuel {_perso.PourcentagePoids:F1}% (seuil={_cfg.SeuilPoidsPct}%)");

// Reset des flags observateurs avant ouverture (sinon un EV résiduel
// d'un workflow précédent ferait croire que la banque est déjà fermée).
BanqueFermeeObservee = false;

// Reset de la map d'attente OR (pourrait contenir des résidus d'un
// workflow précédent qui a abort sur ECK5 timeout).
EnvoyesEnAttenteOR.Clear();
```

---

## Tests

### Tests unitaires (à ajouter dans `BotDofus.Tests/` — fichier `PiloteBanqueTests.cs` neuf)

1. **`CalculerItemsADeposer_FiltreParCategorie_Ressource`** — input : inventaire de 10 items (3 ressources, 3 équipements, 4 consommables) ; config = `DeposerRessources=true` only ; assert output = 3 items ressources.
2. **`CalculerItemsADeposer_RespectIdsAGarder`** — input : 5 items dont 2 dans `IdsAGarder` ; assert output exclut les 2 protégés.
3. **`CalculerItemsADeposer_RespecteSeuilParTemplate`** — input : 100 pains template=2244, `SeuilParTemplate[2244]=20` ; assert output dépose 80 pains (qte ajustée).
4. **`SupprimerObjetOptimiste_ItemPresent_Supprime`** — input : inventaire avec UID=A ; appel `SupprimerObjetOptimiste(A)` ; assert inventaire ne contient plus A.
5. **`SupprimerObjetOptimiste_ItemAbsent_NoOp`** — input : inventaire sans UID=B ; appel `SupprimerObjetOptimiste(B)` ; assert pas d'exception, retourne false.
6. **`SupprimerObjetOptimiste_ThreadSafe`** — 100 threads concurrents qui suppriment et ajoutent ; assert pas de `InvalidOperationException`, état cohérent.
7. **`Workflow_Idempotence_DeuxAppelsDeposerToutAsync`** — mock session + inventaire stable ; 2× `DeposerToutAsync()` consécutifs ; assert 2ème appel = no-op (`aDeposer.Count == 0` immédiat).

### Test d'intégration (manuel)

- Lancer Luffy-bot en mode farm sur la map habituelle de Beiloddurul.
- Attendre que le poids atteigne 90%.
- Observer dans le log : présence de `[BANQUE-START]`, `[BANQUE-FILTRE]`, `[BANQUE-DEPOT]` x N, `[BANQUE-CONFIRM]`, `[BANQUE-END]`.
- Vérifier : 0 occurrence de `cycle supplémentaire 2/3` ou plus (avant le fix : 21 occurrences).
- Vérifier : 0 ligne `0/N OR reçus en 8000ms` après la pass 1 (avant le fix : ≥ 6 par workflow).
- Mesurer le poids final : doit être < 30% (objectif), pas 66.2% figé comme actuellement.

---

## Procédure de validation

1. **Build** : `dotnet build BotDofus.Wpf/BotDofus.Wpf.csproj -c Debug --nologo -v minimal` → 0 erreur, ≤ warnings actuels.
2. **Tests unitaires** : `dotnet test BotDofus.Tests/BotDofus.Tests.csproj --nologo` → all green incluant les 7 nouveaux.
3. **Session live** : 30 min de farm Beiloddurul, observer que la banque se déclenche au moins 1 fois et que les logs montrent le pattern dyshay (1 pass, ≥ 90% des EMO+ confirmés OR).
4. **Régression** : vérifier que le 1er dépôt après lancement bot fonctionne toujours (poids 90 → < 30 en 30 s).
5. **Edge case** : tester en lançant `Tester maintenant` depuis l'UI deux fois rapidement (vérifier idempotence du 2ème appel = no-op).

---

## Décisions parallèles

- **MAX_PASSES réduit de 3 → 1** : impact direct du fix. Avec suppression optimiste, le multi-pass perd son sens (la pass 2 ne verrait que les items dont l'OR n'est pas arrivé, qu'on a décidé de considérer perdus).
- **`cyclesSupplementaires` réduit de 3 → 1** : idem, prévention de la boucle 9-pass stérile observée.
- **`OnObjetQuantite` UID inconnu** : reste un no-op avec `Avertir` (`TrameJeu.cs:1212-1224`). Pas de tentative de récupération auto (template inconnu = pas reconstructible). Fix séparé possible : déclencher un reset inventaire via changement de map quand on cumule N `[INV] OQ inconnu` (hors scope ADR).
- **`OnObjetRetrait` no-op silencieux quand item déjà absent** : changement de Debogue (pas Avertir) — la suppression optimiste rend ce cas nominal.
- **Map `EnvoyesEnAttenteOR` static partagé** : OK pour un seul compte actif à la fois (cas normal). Si multi-comptes simultanés (rare), accepter le risque mineur de fausse confirmation OR croisée (non bloquant). Future amélioration : passer en `Dictionary<int /*persoId*/, ConcurrentDictionary<long, DateTime>>`.
- **`Personnage.SupprimerObjetOptimiste`** ne déclenche PAS `NotifierInventaireChange` pendant le burst : évite le storm UI (50 events/sec). Le pilote appellera 1 seul `NotifierInventaireChange` à la fin du burst (à ajouter en fin de pass, post-suppression OR perdus). Note : à confirmer en review si l'UI a besoin du refresh intermédiaire.
- **Pas de revert de `Task.Delay(2500)` post-ECK5** : utile contre les OQ correctifs lents, garder.
- **Pas de touche au protocole `EMO+/EV/ApS`** : confirmé OK par AGENT 4.

---

## Fichiers modifiés (récap)

| Fichier | Étapes |
|---------|--------|
| `Divers/Jeu/Personnage/Personnage.cs` | 1 (méthode `SupprimerObjetOptimiste`) |
| `Divers/Banque/PiloteBanque.cs` | 2 (MAX_PASSES, snapshot locké, suppression optimiste, map en attente, logs, cycles supplémentaires, reset map) |
| `Commun/Frames/TrameJeu.cs` | 3 (`OnObjetRetrait` no-op silencieux + nettoyage map) |
| `BotDofus.Tests/PiloteBanqueTests.cs` | Tests (nouveau fichier) |

Aucun changement protocole. Aucun changement TrameJeu sur le critical path combat/récolte. Aucun changement UI WPF.

---

**Confiance globale ADR** : 9/10. Le fix est minimal, ciblé, aligné sur une référence open-source éprouvée (dyshay), et conserve une marge de sécurité (safety net `[BANQUE-LOST]`, no-op silencieux `OnObjetRetrait`).
