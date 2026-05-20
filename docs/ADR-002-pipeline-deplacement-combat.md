# ADR-002 V2 — Pipeline de déplacement combat (cipher desync MITM + confirmation event-based)

- Statut : Proposé (remplace ADR-002 V1 — recentré sur la racine cipher desync identifiée dans log `botdofus-20260520-203358`)
- Date : 2026-05-20
- Auteurs : System Architecture Designer
- Bug : "Le bot envoie GA001 mais le serveur ne broadcast jamais le mouvement" — perso figé entre les tours
- Liens :
  - `Commun/Reseau/SessionProxy.cs` ll. 121-257 (`EnvoyerAuServeurAsync`, `EnvoyerCsVersServeur`, gestion idxProxy/idxClient)
  - `Commun/Reseau/ClientAutonomeAbrak.cs` ll. 379-469 (modèle "socket seul → idx local monotone")
  - `Commun/Frames/TrameJeu.cs` ll. 467-545 (`OnActionJeu` parse `GA;0/1;<id>;<chemin>` broadcast), ll. 1012-1066 (`JouerTourCombatAsync` segment déplacement optimistic)
  - Log `BotDofus.Wpf/bin/Debug/net8.0-windows/logs/botdofus-20260520-203358.log` (combat live, 5 tours figés cell 193)
  - Commit antérieur `67ac8bd` : optimistic update `perso.CellulePosition = cellArrivée` immédiat après envoi
  - BIBLE-CADERNIS.md §7 (GA001 + GKK0 timing)

---

## 1. Analyse de la racine — cipher desync sur canal '-'

### 1.1 Observation directe dans le log (20:35:25 → 20:38:00)

Phase pré-combat (overworld, idxProxy ↔ idxClient strictement alignés) :
```
20:34:16  [REENC C→S] #1 idxProxy=2 relayé idxClient=2 clair='BD'
20:34:17  [REENC C→S] #2 idxProxy=3 relayé idxClient=3 clair='BD'
20:35:25  [REENC C→S] #3 idxProxy=4 relayé idxClient=4 clair='GA001he6geD'   ← client
20:35:25  [GA0] acteur #401770 (MOI) chemin='afihe6ge6' → cell 314           ← confirmé serveur
```

Premier point d'injection bot (cast direct, sans déplacement) :
```
20:35:31.780  [REENC C→S] #8  idxProxy=9   INJECTÉ clair='GA300183;431'
20:35:31.854  [VOCAB S→C] GAS401770                                          ← serveur OK
20:35:31.858  [VOCAB S→C] GAF0|401770                                        ← code 0 = succès
20:35:31.993  [REENC C→S] #9  idxProxy=10 relayé idxClient=9  ≠proxy(décalé) ← décalage +1
20:35:32.229  [REENC C→S] #10 idxProxy=11 INJECTÉ clair='GKK0'
20:35:32.632  [REENC C→S] #11 idxProxy=12 relayé idxClient=10 ≠proxy(décalé) ← +2
```
Le décalage entre `idxProxy` (compteur monotone du re-chiffrement) et `idxClient` (index brut lu dans l'en-tête du paquet client) s'installe d'**exactement le nombre d'injections bot effectuées** depuis le dernier reset. C'est **structurellement attendu** (le proxy avance le compteur à chaque émission, client réel ou bot, donc le client tourne en retard) et **c'est ce que SynFus fait**.

Combat suivant — tour 1 du bot (20:36:50.649) — le bot tente un déplacement à 1 case :
```
20:36:50.649  [ACTION] Hors portée → déplacement 1 case(s) vers cell 194 (sort « Ronce » niv5 portée 1-8)
20:36:50.649  [INJ ->SRV '-' réenc] GA001adc
20:36:51.179  [INJ ->SRV '-' réenc] GKK0
20:36:51.588  [INJ ->SRV '-' réenc] GA300183;230
20:36:51.633  Info #0 : 8~16                                                  ← annonce dégâts → cast OK ?
20:36:51.847  [CARTE] ...
20:36:51.993  [INJ ->SRV '-' réenc] GKK0
20:36:53.094  [INJ ->SRV] Gt
20:36:53.408  [PKT S→C] GTM|401770;0;130;6;3;193;;130|...                    ← BOT TOUJOURS CELL 193 !
```

**Aucun `GA;0;401770;adc` ni `GA;1;401770;adc` broadcast** entre 20:36:50.649 et le GTM final. Le serveur a **silent-dropped le GA001**. Mais il a accepté le GA300 (Info "8~16" = dégâts), donc le cipher fonctionne sur ce paquet précis. **Asymétrie cruciale.**

### 1.2 Pourquoi GA300 passe et GA001 ne passe pas

Hypothèse principale écartée — pure cipher desync : si idxProxy/idxClient étaient incohérents, GA300 serait aussi rejeté. **Or il ne l'est pas.** Donc le serveur déchiffre correctement les deux. **La désynchronisation `≠proxy(décalé)` n'est PAS la cause directe du rejet GA001** — c'est un side-effect cosmétique du modèle SynFus (le proxy avance d'autant qu'il injecte, le client tourne mécaniquement en retard).

Hypothèses survivantes pour le rejet silencieux GA001 :

a. **Conflit avec une action déjà ouverte serveur-side**. Lors d'un tour combat, après le GTS401770 le bot dans le **même tour** envoie GA001 puis GA300. Le serveur 1.29 traite les actions de manière séquentielle : il peut exiger GAS/GAF complet du GA001 avant d'accepter GA300. Or le bot envoie GA001 puis 530 ms plus tard GKK0 puis 410 ms plus tard GA300 — sans vérifier que la séquence GAS/GAF du GA001 est arrivée. Le `Task.Delay(nbPas * 330 + 200)` aveugle ne suffit pas. Le serveur peut être en train de rejeter GA001 (cellule invalide, PM, cible occupée), donc il n'a JAMAIS émis GAS/GAF pour celui-ci, mais le bot enchaîne quand même GA300 — qui lui passe parce que cell 193 reste valide.

b. **Cell d'arrivée invalide en combat 1.29**. `GA001adc` = mouvement 1 case (`a` = direction, `dc` = hash cellule 194). Cell 194 voisine de cell 193 sur grille iso 14×40. Vérifier dans `[CARTE] #10287` si cell 194 est marchable ET non occupée. Le log ne le dit pas explicitement.

c. **Encodage GA001 différent en combat 1.29**. Le client réel à 20:35:25 envoie `GA001he6geD` (3 segments) pour un mouvement overworld. Le bot envoie `GA001adc` (1 segment) en combat. dyshay `PathFinderUtil.get_Pathfinding_Limpio` confirme que pour 1 case le format est `<direction><cell-2char>` = `adc`. Format identique. Reste à vérifier : le `direction` doit-il être recalculé en combat (mouvements bloqués par combattants) ? `a` = SE, ok depuis cell 193 vers 194 (sur 14-grid, dx=0, dy=+1 → SE).

d. **Pas de GAS/GAF émis pour les déplacements combat** — au contraire de GA300. Hypothèse plausible : en 1.29 combat, le serveur émet uniquement `GA;0;<id>;<chemin>` (et c'est l'event décisif), pas GAS/GAF. La preuve dans le log : à 20:36:53.409 quand l'ennemi #-1 se déplace, on a `GA1;1;-1;agtfgeff1` mais **aucun GAS-1 ni GAF0|-1 associé**. Donc GAS/GAF est pour les actions de cast/PA-consumming, pas pour les move.

**Conclusion :** la cause n'est **pas** le cipher (GA300 passe), c'est très probablement (c) ou (b). Mais **on ne peut pas trancher sans confirmation event-based** : tant que le bot ne distingue pas "GA001 accepté" de "GA001 rejeté", on est aveugle.

### 1.3 Risque d'interleaving (client réel + bot en parallèle)

`SessionProxy.EnvoyerCsVersServeur` (ll. 173-256) sérialise toutes les écritures C→S sous `_verrouCs`. Donc l'**ordre d'écriture wire** est garanti monotone : un GA001 client réel et un GA001 bot ne peuvent pas s'entrelacer côté byte stream. **Bon point.**

Mais l'**ordre logique métier** n'est pas garanti : le bot peut envoyer son GA001 alors que le client réel a déjà initié une action (par exemple un `GAS<myId>` est en cours côté serveur pour une action client). Le serveur 1.29 peut refuser une 2e action tant que la 1re n'est pas fermée par `GAF<code>|<myId>`. C'est un risque **uniquement en mode actif avec client Dofus.exe ouvert en parallèle** — qui est le mode standard du projet aujourd'hui.

---

## 2. Test reproductible pour confirmer/infirmer

### 2.1 Test cipher (déjà fait par les logs, mais formalisable)

Test A — capture Wireshark loopback port 1304 :
```
wireshark -k -i "Adapter for loopback traffic capture" -f "tcp port 1304" -Y "tcp.payload contains '-'"
```

Critères :
1. **Index de clé dans l'en-tête** (`brut[1]` = char hex 1-9a-f) : doit être strictement monotone modulo `N-1` côté wire (= idxProxy). Le log `[REENC C→S] #N idxProxy=X` permet de corréler 1:1.
2. **Le serveur a-t-il répondu par un AK renégocié ?** Si oui (`[REENC C→S] AK renégocié S→C après amorce`), il y a eu reset rotation côté serveur. Si non, le cipher est nominal.

Sur le log actuel : **aucun message** `AK renégocié S→C après amorce` n'apparaît entre l'amorce et la fin du combat. Le cipher tourne sur la rotation initiale → pas de reset serveur → cipher PROBABLEMENT cohérent.

### 2.2 Test ciblé "le serveur accepte-t-il le GA001 bot ?"

Test B — instrumentation minimale (5 lignes), à ajouter temporairement dans `OnActionJeu` et dans le pipeline cast :

1. Logger toutes les actions GA0/GA1 reçues pour `acteurId == perso.Identifiant` avec timestamp ms (déjà fait L. 522-524 `[GA0] acteur #401770 (MOI)`).
2. Logger l'instant t0 d'envoi du GA001 bot dans `JouerTourCombatAsync` (déjà fait L. 1027 `[ACTION-MV] Envoi GA001`).
3. **Mesurer Δt = t(GA;0;<myId> reçu) − t0**. Sur un combat humain sain : 150-400 ms. Sur un combat où le bot reste figé : `null` (jamais reçu).
4. **Comparer la cellule de destination annoncée par le broadcast avec la cellule envoyée**. Si différentes → le serveur a tronqué le chemin (cellule traversée occupée, PM insuffisants en transit).

Le test B confirme directement si c'est un rejet métier (cas a/b/c) ou un rejet cipher (rare).

### 2.3 Test C — mode passif comparatif

1. Activer `ChkModePassif`, démarrer un combat manuel.
2. Quand c'est le tour user, déplacer manuellement 1 case adjacente.
3. Vérifier que le proxy logge :
   - `[REENC C→S] #N idxProxy=X relayé idxClient=X clair='GA001<encodé>'`
   - `[GA0] acteur #401770 (MOI) chemin='<encodé>' → cell <X>`
4. **Comparer caractère-pour-caractère** l'encodé client réel vs l'encodé bot pour le même mouvement (cell origine + cell destination identiques). Toute divergence = bug Pathfinder.

---

## 3. Pipeline correct — 3 options évaluées

### 3.1 Option A — Attendre broadcast `GA;0/1;<myId>` event-based avec timeout 2.5s

Le serveur broadcast `GA;0;<myId>;<chemin>` (ou `GA;1;...` en combat, cf. log L. 757 `GA1;1;-1;agtfgeff1`) quand il **accepte** un déplacement. `OnActionJeu` (TrameJeu.cs L. 474) le parse déjà et met à jour `_etat.Personnage.CellulePosition`.

- **Pour** : signal métier fiable, déjà câblé jusqu'à L. 535 (`[ACTION-MV] Position confirmée par serveur`). Il manque juste un event explicite déclenchable depuis l'IA.
- **Pour** : timeout 2.5s suffisant (broadcast typique 150-400 ms, marge ×5).
- **Contre** : pas de feedback "rejet explicite" → on infère rejet par timeout.
- **Contre** : pas de feedback "fin d'animation" → on doit toujours attendre `nbPas * 330ms` avant GKK0 sécurisé.

### 3.2 Option B — Tracker GAS/GAF

Le serveur émet `GAS<id>` (début action) puis `GAF<code>|<id>` (fin, code=0 OK) pour les actions PA-consumming (cast, sort…). Le log L. 305-307 le confirme pour un GA300, **mais** L. 757-758 montre qu'un déplacement adverse n'émet PAS GAS/GAF (uniquement GA1). Donc :

- **GA001 bot ne génère probablement PAS de GAS/GAF** : option B inapplicable pour les déplacements.
- GAS/GAF reste utile pour le cast (GA300) post-déplacement.

### 3.3 Option C — Hybride recommandée

Pour le **déplacement** : Option A (broadcast `GA;0/1;<myId>` avec timeout 2.5s, fallback Gt sur timeout).

Pour le **cast post-déplacement** : Option B (GAS401770 + GAF0|401770 avec timeout 3s, validation code).

**Justification** : asymétrie protocolaire 1.29 reconnue, chaque action utilise le signal serveur disponible. Pas de timing aveugle.

---

## 4. Modifications de code prêtes à appliquer

### 4.1 Nouveau event `Combat.MouvementBotConfirme` — fichier `Divers/Combats/Combat.cs`

Ajouter à côté de `EtatChange`, `TourChange` :
```csharp
public event EventHandler<MouvementBotArgs>? MouvementBotConfirme;

public sealed class MouvementBotArgs : EventArgs
{
    public int IdActeur;
    public int CellArrivee;
    public string CheminEncode;
}

internal void DeclencherMouvementBot(int idActeur, int cellArrivee, string chemin)
    => MouvementBotConfirme?.Invoke(this, new MouvementBotArgs
        { IdActeur = idActeur, CellArrivee = cellArrivee, CheminEncode = chemin });
```

### 4.2 Émettre l'event depuis `OnActionJeu` — `Commun/Frames/TrameJeu.cs` L. 527-536

Remplacer :
```csharp
if (moi)
{
    int? avant = _etat.Personnage.CellulePosition;
    _etat.Personnage.CellulePosition = cell;
    _etat.CarteCourante?.SignalerRechargee();
    if (_etat.Combat.Etat != Divers.Combats.Enums.EtatCombat.Inactif)
        Journaliseur.Info($"[ACTION-MV] Position confirmée par serveur : cell {avant} → {cell} (broadcast GA;1;)");
}
```
par :
```csharp
if (moi)
{
    int? avant = _etat.Personnage.CellulePosition;
    _etat.Personnage.CellulePosition = cell;
    _etat.CarteCourante?.SignalerRechargee();
    if (_etat.Combat.Etat != Divers.Combats.Enums.EtatCombat.Inactif)
    {
        Journaliseur.Info($"[ACTION-MV] Position confirmée par serveur : cell {avant} → {cell} (broadcast GA;{p[0]};)");
        _etat.Combat.DeclencherMouvementBot(acteurId, cell, chemin);
    }
}
```

### 4.3 Helper `AttendreMouvementOuTimeoutAsync` — nouveau fichier `Divers/Combats/PipelineDeplacementCombat.cs` (~80 lignes, sous la convention 500-line cap)

```csharp
namespace BotDofus.Divers.Combats;

public enum ResultatDeplacementCombat
{
    Confirme,           // broadcast GA;0/1;<myId> reçu avec cell attendue
    ConfirmePartiel,    // broadcast reçu mais cell d'arrivée ≠ celle envoyée (serveur a tronqué)
    TimeoutSilencieux,  // rien reçu en 2.5s → rejet métier serveur
}

public static class PipelineDeplacementCombat
{
    public static async Task<ResultatDeplacementCombat> AttendreMouvementOuTimeoutAsync(
        Combat combat,
        int idMoi,
        int cellAttendue,
        int timeoutMs,
        CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<(int cellAtteinte, bool exact)>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        void OnMv(object? s, MouvementBotArgs e)
        {
            if (e.IdActeur != idMoi) return;
            tcs.TrySetResult((e.CellArrivee, e.CellArrivee == cellAttendue));
        }
        combat.MouvementBotConfirme += OnMv;
        try
        {
            var tEvent = tcs.Task;
            var tTimeout = Task.Delay(timeoutMs, ct);
            var won = await Task.WhenAny(tEvent, tTimeout).ConfigureAwait(false);
            if (won == tTimeout)
                return ResultatDeplacementCombat.TimeoutSilencieux;
            var (atteint, exact) = tEvent.Result;
            return exact
                ? ResultatDeplacementCombat.Confirme
                : ResultatDeplacementCombat.ConfirmePartiel;
        }
        finally { combat.MouvementBotConfirme -= OnMv; }
    }
}
```

### 4.4 Refactor `JouerTourCombatAsync` — `Commun/Frames/TrameJeu.cs` L. 1017-1060

Remplacer le bloc actuel :
```csharp
var resApproche = TrouverApprocheCombat(perso, combat, ennemi, sortVise, _compte.ConfigCombat);
if (resApproche.HasValue)
{
    var (chemin, distApres) = resApproche.Value;
    int nbPasMove = chemin.Count - 1;
    int cellArrivee = chemin[^1].Identifiant;
    // ... [logs PATHFINDING + ACTION-MV envoi GA001] ...

    var paquetDep = BotDofus.Divers.Cartes.Deplacement.Pathfinder.PaquetDeplacement(chemin);
    Journaliseur.Info($"[ACTION-MV] Envoi GA001 → '{paquetDep}' (cells {chemin[0].Identifiant}→{cellArrivee})");

    // 1) Arm event AVANT envoi (anti-race)
    int idMoi = _etat.Personnage.Identifiant;
    int cellAvantMv = maCell;

    // 2) Envoi
    await _session.EnvoyerAuServeurAsync(paquetDep).ConfigureAwait(false);

    // 3) Attente broadcast confirmation (timeout 2500 ms)
    int timeoutMs = Math.Max(2500, nbPasMove * 450 + 1000);
    var resultat = await BotDofus.Divers.Combats.PipelineDeplacementCombat
        .AttendreMouvementOuTimeoutAsync(_etat.Combat, idMoi, cellArrivee, timeoutMs, default)
        .ConfigureAwait(false);

    switch (resultat)
    {
        case ResultatDeplacementCombat.Confirme:
            // perso.CellulePosition déjà mis à jour par OnActionJeu (L. 530)
            maCell = _etat.Personnage.CellulePosition ?? cellArrivee;
            Journaliseur.Info($"[ACTION-MV] Mouvement CONFIRMÉ serveur : cell {cellAvantMv}→{maCell}");

            // GKK0 d'ack action (sûr car serveur a accepté)
            await Task.Delay(System.Random.Shared.Next(150, 300)).ConfigureAwait(false);
            await _session.EnvoyerAuServeurAsync("GKK0").ConfigureAwait(false);
            await Task.Delay(System.Random.Shared.Next(300, 500)).ConfigureAwait(false);

            // Continue → cast
            sort = sortVise; sortCoutPA = paVL;
            sortPorteeMin = pminVL; sortPorteeMax = pmaxVL;
            distEnnemi = distApres;
            break;

        case ResultatDeplacementCombat.ConfirmePartiel:
            // Serveur a tronqué le chemin (cell occupée mid-path) → recalculer dist
            maCell = _etat.Personnage.CellulePosition ?? cellAvantMv;
            int distReelle = BotDofus.Divers.Cartes.Cellule.Distance(maCell, ennemi.CellulePosition, 14);
            Journaliseur.Avertir($"[ACTION-MV] Mouvement PARTIEL : visé cell {cellArrivee}, atteint {maCell} (dist réelle {distReelle})");

            await Task.Delay(System.Random.Shared.Next(150, 300)).ConfigureAwait(false);
            await _session.EnvoyerAuServeurAsync("GKK0").ConfigureAwait(false);
            await Task.Delay(System.Random.Shared.Next(300, 500)).ConfigureAwait(false);

            if (distReelle <= pmaxVL && distReelle >= pminVL)
            {
                sort = sortVise; sortCoutPA = paVL;
                sortPorteeMin = pminVL; sortPorteeMax = pmaxVL;
                distEnnemi = distReelle;
            }
            else
            {
                Journaliseur.Info($"[ACTION] Sort hors portée après move partiel (dist {distReelle}) → Gt");
                await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false);
                return;
            }
            break;

        case ResultatDeplacementCombat.TimeoutSilencieux:
            // ROLLBACK : pas de confirmation = serveur a refusé en silence.
            // NE PAS envoyer GKK0 (ack d'action inexistante = signature anti-bot).
            // NE PAS continuer vers le cast (le perso est resté à cellAvantMv).
            Journaliseur.Erreur($"[ACTION-MV] Mouvement REFUSÉ silencieux (timeout {timeoutMs}ms) : "
                + $"perso reste cell {cellAvantMv}, abandon tour");
            // L'optimistic update L. 1043 ne doit JAMAIS avoir touché perso.CellulePosition
            // dans le nouveau pipeline (cf. §5 ci-dessous).
            await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false);
            return;
    }
}
```

### 4.5 Suppression critique — `Commun/Frames/TrameJeu.cs` L. 1035-1045

**Retirer** les lignes 1035-1045 :
```csharp
// OPTIMISTIC UPDATE ... (SUPPRIMER)
int cellAvantMv = maCell;
_etat.Personnage.CellulePosition = cellArrivee;
maCell = cellArrivee;
Journaliseur.Info($"[ACTION-MV] Position optimiste mise à jour : ...");
```
Désormais c'est **uniquement** `OnActionJeu` (L. 530) qui peut écrire `_etat.Personnage.CellulePosition` en combat — single writer, source de vérité serveur.

---

## 5. Compatibilité avec le fix optimistic actuel — "degraded mode"

Pour permettre une mise en service graduelle (canary), on garde l'**optimistic update derrière un feature flag** :

Dans `ConfigCombat` :
```csharp
public bool ModeDeplacementOptimisteSecours { get; set; } = false;
```

Dans le case `TimeoutSilencieux` :
```csharp
case ResultatDeplacementCombat.TimeoutSilencieux:
    if (_compte.ConfigCombat?.ModeDeplacementOptimisteSecours == true)
    {
        // Mode dégradé : on accepte l'ancien comportement (cast aveugle)
        Journaliseur.Avertir($"[ACTION-MV] Timeout mais ModeOptimisteSecours=ON → on continue le cast (legacy)");
        _etat.Personnage.CellulePosition = cellArrivee;
        maCell = cellArrivee;
        // ... GKK0 + cast comme aujourd'hui
        break;
    }
    // Sinon : Gt immédiat (chemin par défaut)
    Journaliseur.Erreur(...);
    await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false);
    return;
```

Workflow recommandé :
1. **Phase 1** : déployer event-based avec flag `false` par défaut → mesurer taux de timeouts sur 10 combats.
2. **Phase 2** : si taux > 30 %, c'est qu'il y a un bug Pathfinder/encodage (cf. test 2.3) — activer le flag temporairement pour ne pas paralyser le bot pendant le diag.
3. **Phase 3** : flag définitivement `false`, supprimer le fallback dans la PR suivante.

---

## 6. Risque interleaving client réel + bot

### 6.1 Le problème

En mode actif (proxy MITM), le client Dofus.exe est lancé en parallèle. L'utilisateur peut :
- Cliquer sur la map → client envoie GA001 cell distante.
- L'IA bot envoie son propre GA001 dans le même tour.

Le proxy sérialise wire (`_verrouCs`), donc pas d'entrelacement byte, mais le serveur 1.29 traite séquentiellement. Si le client réel a une action GAS en cours, le bot risque un rejet.

### 6.2 Mitigation — état "client a une action ouverte"

Tracker dans `Combat` un flag `ActionClientEnCours` :

```csharp
// Dans Combat.cs
public bool ActionClientEnCours { get; private set; }
internal void DemarrerActionClient(int idActeur)
{
    if (idActeur == _idPersoMoi) ActionClientEnCours = true;
}
internal void TerminerActionClient(int idActeur)
{
    if (idActeur == _idPersoMoi) ActionClientEnCours = false;
}
```

Brancher sur GAS/GAF dans `OnActionJeu` :
```csharp
case "GAS":  if (acteurId == idMoi) _etat.Combat.DemarrerActionClient(acteurId); break;
case "GAF":  if (acteurId == idMoi) _etat.Combat.TerminerActionClient(acteurId); break;
```

Dans `JouerTourCombatAsync`, **avant** d'envoyer GA001 :
```csharp
if (_etat.Combat.ActionClientEnCours)
{
    // Attendre GAF avec timeout court
    var sw = Stopwatch.StartNew();
    while (_etat.Combat.ActionClientEnCours && sw.ElapsedMilliseconds < 2000)
        await Task.Delay(100).ConfigureAwait(false);
    if (_etat.Combat.ActionClientEnCours)
    {
        Journaliseur.Avertir("[COMBAT] Action client toujours en cours après 2s → Gt pour éviter conflit");
        await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false);
        return;
    }
}
```

Effet : **le bot ne tirera jamais en parallèle d'une action client encore ouverte serveur-side**. Cas typique : user en mode passif test → 0 conflit. Cas mode actif user paniqué qui clique → bot patient → 0 conflit.

### 6.3 Mode passif global

Quand `Compte.ModePassif == true`, `JouerTourCombatAsync` skip déjà (CLAUDE.md "Mode Passif"). La mitigation 6.2 n'est utile qu'en mode actif.

---

## 7. Plan de rollout

1. **Commit 1** — Event `MouvementBotConfirme` + déclenchement dans `OnActionJeu` (§ 4.1, 4.2). Pas de changement comportemental.
2. **Commit 2** — Helper `PipelineDeplacementCombat` (§ 4.3). Pas de changement comportemental.
3. **Commit 3** — Refactor `JouerTourCombatAsync` avec event-based + flag `ModeDeplacementOptimisteSecours` activé par défaut (§ 4.4, 4.5, 5). Comportement identique à aujourd'hui par défaut.
4. **Commit 4** — Activer le mode strict par défaut (flag à `false`) après vérif live 5+ combats sans timeout.
5. **Commit 5** — Mitigation interleaving GAS/GAF client (§ 6.2). Optionnel.

Chaque commit testable indépendamment, rollback unitaire.

---

## 8. Décisions à valider

1. **Timeout 2500 ms** : OK ou élargir à 3500 ms pour marge lag ?
2. **`ConfirmePartiel`** : tenter cast sur dist réelle (proposé) ou Gt direct (plus safe) ?
3. **Mitigation interleaving (§ 6)** : faire en même PR que le pipeline ou plus tard ?
4. **Suppression définitive de l'optimistic update** : décider après combien de combats sans timeout (proposé : 10) ?

---

## 9. Références internes

- `Commun/Reseau/SessionProxy.cs` L. 171-257 (`EnvoyerCsVersServeur` — émetteur unique sérialisé canal '-')
- `Commun/Reseau/SessionProxy.cs` L. 192-220 (logique `resetReel` + `_akRenegocie` — réinit rotation)
- `Commun/Reseau/ClientAutonomeAbrak.cs` L. 446-469 (modèle alternatif — socket seul, idx local monotone, **pas de désync car pas de client parallèle**)
- `Commun/Frames/TrameJeu.cs` L. 467-545 (`OnActionJeu` — parsing broadcast GA0/GA1, mise à jour position)
- `Commun/Frames/TrameJeu.cs` L. 1017-1066 (segment déplacement combat à refactorer)
- `Divers/Cartes/Deplacement/Pathfinder.cs` L. 135-170 (encodage `PaquetDeplacement`)
- `BotDofus.Wpf/bin/Debug/net8.0-windows/logs/botdofus-20260520-203358.log` :
  - L. 250-251 (broadcast client réel : OK)
  - L. 303-307 (cast bot : GAS+GAF reçus → cipher fonctionne)
  - L. 730-738 (bot tour 1 : GA001 envoyé, aucun broadcast retour, perso reste cell 193)
  - L. 754, 770, 785 (GTM successifs confirment cell 193 figée)
