# ADR V3 — Fix dépôts banque (course concurrente WorkflowCompletAsync)

## Statut
Proposé : 2026-06-02 — Synthèse AGENT V3-A (forensic log) + V3-B (cadernis)
Remplace : ADR V2 (pattern synchrone par UID) — pattern conservé, bug d'orchestration corrigé.

## Contexte

Le V2 (commit `789381e`) reste perçu comme cassé par le user (« ça fait pareil »).
La forensic V3-A du log `botdofus-20260601-211839.log` montre que **V2 marche
techniquement à 96 %** (27/28 items déposés sur le 1er dépôt + 6/6 sur le cycle
supplémentaire), mais souffre de **deux bugs d'orchestration** qui produisent un
torrent de fausses alertes :

1. **`WorkflowCompletAsync` peut s'exécuter en concurrence** (déclencheur manuel
   + `OnInventaireChange` qui re-trigger après le 1er OR à cause d'un poids
   recalculé buggé 0.4 % → 77 %). Les deux instances partagent le champ
   **STATIC** `AttenteResultat` (`ConcurrentDictionary<long, TCS>`). Le 2e
   workflow appelle `AttenteResultat.Clear()` au début → **draine en `Timeout`
   tous les TCS du 1er workflow encore en attente** → ce dernier voit des
   timeouts à **32 ms** (impossible techniquement, `WaitAsync(500ms)`).

2. **Le 2e workflow réenvoie un `ApS`** alors que le coffre est déjà ouvert.
   Le serveur n'émet **PAS** d'ECK5 sur le 2e ApS (banque déjà ouverte) → faux
   `[BANQUE-TIMEOUT] ❌ Pas de ECK5 reçu 2s après ApS — Abort workflow, RIEN
   n'a été déposé`. En réalité le 1er workflow continue à déposer en parallèle.

Cause racine V3 = **race condition sur l'état statique partagé entre instances
concurrentes de `WorkflowCompletAsync`**, pas un problème de protocole.

### Confirmation cadernis (AGENT V3-B)

Aucun paquet client `OK`/`Os`/`OL` de resync inventaire n'existe en Dofus Retro
1.29 (sources : `efwff/Codebreak` côté serveur, `dyshay/Bot-Dofus-Retro` côté
client). Le seul push complet est `AS` à la sélection du perso (avant `GC1`).
La technique universelle = optimistic remove (V1 dyshay) OU resync via
reconnexion complète. Le V2 (synchrone par UID) est aussi viable que dyshay
dès lors que la concurrence est empêchée.

## Décision

**Garder le pattern synchrone V2 par UID** (il marche techniquement à 96 %) et
**sérialiser les workflows** via `SemaphoreSlim(1, 1)`. Ajouter une garde
`skip ApS si banque déjà ouverte`. **Ne PAS** revenir à V1 (optimistic remove)
— V2 est plus précis pour détecter les vrais OQ partiels Hystoria.

## Plan d'implémentation

### Étape 1 — `SemaphoreSlim` global sur `WorkflowCompletAsync`

**Fichier** : `Divers/Banque/PiloteBanque.cs`

Ajouter en début de classe (après les flags statiques) :
```csharp
/// <summary>V3 — Verrou global sur WorkflowCompletAsync.
/// Empêche 2 workflows concurrents de partager AttenteResultat (qui est static)
/// et de se draîner mutuellement les TCS via Clear().
/// Le 2e déclencheur reçoit false immédiatement (TryAcquire 0 ms).</summary>
private static readonly SemaphoreSlim _verrouWorkflow = new(1, 1);
```

Wrapper le début de `WorkflowCompletAsync` :
```csharp
public async Task<bool> WorkflowCompletAsync(int? carteFarmAvant, CancellationToken ct = default)
{
    // V3 — sérialisation des workflows (forensic AGENT V3-A).
    if (!await _verrouWorkflow.WaitAsync(0, ct).ConfigureAwait(false))
    {
        Journaliseur.Avertir("[BANQUE-START] Workflow déjà en cours — skip (anti-double-trigger).");
        return false;
    }
    try
    {
        // ... corps actuel inchangé ...
    }
    finally
    {
        _verrouWorkflow.Release();
    }
}
```

### Étape 2 — Skip ApS si banque déjà ouverte

**Fichier** : `Divers/Banque/PiloteBanque.cs:212` (dans `DeposerToutInterneAsync`,
juste avant `_session.EnvoyerAuServeurAsync("ApS")`).

Ajouter check :
```csharp
// V3 — si la banque est déjà ouverte (workflow précédent qui n'a pas fait EV,
// ou cycle supplémentaire dans le même workflow), ne pas re-envoyer ApS.
// Le serveur Hystoria ne renvoie pas d'ECK5 sur un ApS redondant → faux timeout
// (forensic AGENT V3-A §Q10.4).
bool dejaOuverte = BanqueOuvertureObservee && !BanqueFermeeObservee;
if (dejaOuverte)
{
    Journaliseur.Info("[BANQUE-START] Banque déjà ouverte — skip ApS (réutilisation session).");
}
else
{
    Journaliseur.Info("[BANQUE] → ApS (ouverture coffre)");
    await _session.EnvoyerAuServeurAsync("ApS").ConfigureAwait(false);
    // Attente ECK5 2s comme aujourd'hui...
}
```

### Étape 3 — Documentation (ce fichier)

Aucune autre modification de code requise.

## Conséquences positives

- Élimination du **faux `[BANQUE-FAIL]`** sur le Carreau d'Arbalète et items
  similaires (l'item est déposé, le timeout est artificiel).
- Élimination des **faux `[BANQUE-TIMEOUT] ❌ Pas de ECK5`** (4 occurrences dans
  le log analysé).
- Les `[BANQUE-VERIFY]` final montrera des chiffres réels (pas de double
  comptage entre workflows concurrents).
- Le user verra le vrai comportement V2 sans bruit.

## Risques

- **Si le user a vraiment un trigger manuel suivi d'un trigger auto**, le 2e
  sera skippé. Acceptable (le 1er est encore en cours et fera le boulot).
- **Si `OnInventaireChange` re-trigger pendant un workflow long**, le check
  `_banqueDeclenchee` côté `ContexteCompte` aurait dû le bloquer mais ne l'a
  pas fait dans le log (le 1er workflow est lancé manuellement, pas via
  `OnInventaireChange`, donc `_banqueDeclenchee` reste à false). Le semaphore
  attrape ce cas peu importe l'origine.

## Hors-scope V3 (pour un éventuel V4)

- **`AttenteResultat` per-instance** (pas statique) — plus propre mais
  nécessite de propager une référence depuis `TrameJeu` vers le pilote actif.
  Le semaphore est plus simple et suffisant pour fermer le bug.
- **Optimistic remove (V1 dyshay)** — recommandé par AGENT V3-B mais inutile
  si V2 marche à 96 %+. Le pattern V2 est plus précis sur les OQ partiels
  Hystoria.
- **Reconnexion forcée si EL_post inattendu** — filet ultime suggéré par
  V3-B. Pas nécessaire si V3 corrige les faux timeouts.
- **Fix du poids local 0.4 % → 77 % sur le 1er OR** — bug séparé dans
  `Personnage.RecalculerPoidsLocal`. À investiguer dans une session dédiée.

## Confiance ADR V3 : 9/10

V3-A a directement identifié le bug par lecture des timestamps (32 ms timeout
techniquement impossible). V3-B confirme que le pattern V2 est viable, juste
mal orchestré. Le fix est ~10 lignes, blast radius minimal. Risque résiduel
principal = bug du poids local (hors scope).
