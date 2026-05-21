# AUDIT CODE IA COMBAT — Luffy-bot

> Audit ligne par ligne réalisé le 2026-05-21 sur la branche `claude/dofus-bot-continue-IUIN3`.
> Périmètre : pipeline IA combat (handler tour → moteur règles → fallback legacy →
> pathfinder approche → pipeline déplacement → cast → end-turn).
>
> Fichiers audités :
> - `Commun/Frames/TrameJeu.cs` (1341 l)
> - `Divers/Combats/IA/MoteurReglesCombat.cs` (213 l)
> - `Divers/Combats/IA/RegleSort.cs` (163 l)
> - `Divers/Combats/IA/ConfigCombat.cs` (185 l)
> - `Divers/Combats/IA/StrategieCombat.cs` + `ModeCombat.cs`
> - `Divers/Combats/Combat.cs` (132 l)
> - `Divers/Combats/PipelineDeplacementCombat.cs` (90 l)
> - `Divers/Cartes/Deplacement/Pathfinder.cs` (224 l)
> - `Divers/Cartes/LigneVisuelle.cs` (169 l)
> - `Divers/ContexteCompte.cs` (700 l)

---

## §1 — Issues CRITIQUES (priorité 1)

### C1. Exceptions silencieuses dans le fail-safe end-turn
**Fichier** : `Commun/Frames/TrameJeu.cs`
**Lignes** : 113-120, 172-177

Le `catch` qui suit `JouerTourCombatAsync` envoie un `Gt` de secours, mais si CE Gt
échoue lui-même (socket morte, session disposée), l'erreur est **avalée
silencieusement** par un `catch { /* swallow */ }`. Résultat : le combat se
fige en `[COMBAT] erreur IA tour` sans log de cause sur le second échec,
impossible à diagnostiquer (visible à 2 endroits, copier-collé).

```diff
-                try { await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false); }
-                catch { /* swallow */ }
+                try { await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false); }
+                catch (Exception ex2)
+                {
+                    Journaliseur.Erreur($"[COMBAT] fail-safe Gt aussi en erreur : {ex2.Message}", ex2);
+                }
```

À répliquer dans les deux handlers (`MessageTourCombat` et `MessageTourCombatAbrak`).

---

### C2. `_session` nullable utilisé sans garde au fail-safe
**Fichier** : `Commun/Frames/TrameJeu.cs`
**Lignes** : 118, 175

Le mode `BrancherClientAutonome` (cf. `ContexteCompte.cs:558`) construit
`TrameJeu(..., session: null!)`. Dans ce mode, `_session is null` est testé
ligne 102/158 → return propre. Mais dans le bloc `catch`, on appelle
inconditionnellement `_session.EnvoyerAuServeurAsync("Gt")` → **NullReferenceException
masquée par le `catch { swallow }`** vu en C1.

```diff
                 catch (Exception ex)
                 {
                     Journaliseur.Avertir($"[COMBAT] erreur IA tour : {ex.Message}");
-                    try { await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false); }
-                    catch { /* swallow */ }
+                    if (_session is not null)
+                    {
+                        try { await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false); }
+                        catch (Exception ex2)
+                        {
+                            Journaliseur.Erreur($"[COMBAT] fail-safe Gt en erreur : {ex2.Message}", ex2);
+                        }
+                    }
                 }
```

---

### C3. Multi-cast inopérant — fallback legacy fait 1 cast PUIS `Gt` quoi qu'il arrive
**Fichier** : `Commun/Frames/TrameJeu.cs`
**Lignes** : 1127-1144 (fallback legacy) + 1313-1339 (`ExecuterRegleAsync`)

Les deux chemins de cast (fallback legacy ET règle SynFus) envoient
**toujours** la séquence `GA300 → GKK0 → Gt` après UN seul cast. Résultat :

- Beiloddurul niv 13 a PA=6, Ronce niv 5 = 4 PA → reste **2 PA libres**, jamais
  utilisés.
- `RegleSort.NombreParTour` est lu en filtre (l. 69-73 `MoteurReglesCombat`),
  incrémenté l. 1330, mais comme on `Gt` après le 1er cast, le compteur ne
  sert à rien.
- La doc CLAUDE.md §Roadmap mentionne « Multi-cast par tour (drain PA, respecte
  maxParTour) » comme TODO.

**Aucune limite max configurable** : aujourd'hui le code force `1 cast/tour`.

```diff
-    private async Task ExecuterRegleAsync(Divers.Combats.IA.MoteurReglesCombat.ResultatRegle r)
+    private async Task ExecuterRegleAsync(Divers.Combats.IA.MoteurReglesCombat.ResultatRegle r)
     {
         /* … log et envoi GA300 … */
         await _session.EnvoyerAuServeurAsync(paquet).ConfigureAwait(false);
         combat.CompteursRegleParTour[r.Sort.Identifiant] =
             (combat.CompteursRegleParTour.TryGetValue(r.Sort.Identifiant, out var cnt) ? cnt : 0) + 1;
         await Task.Delay(System.Random.Shared.Next(300, 500)).ConfigureAwait(false);
         await _session.EnvoyerAuServeurAsync("GKK0").ConfigureAwait(false);
-        await Task.Delay(System.Random.Shared.Next(1000, 1500)).ConfigureAwait(false);
-        Journaliseur.Info("[ACTION] Passe le tour (Gt)");
-        await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false);
+        // Tentative d'enchainer un second cast tant qu'il reste des PA et que les
+        // règles le permettent. Le passage du tour est délégué à JouerTourCombatAsync
+        // après la boucle.
     }
```

Refacto suggérée : faire de `ExecuterRegleAsync` une méthode qui **ne ferme pas
le tour**. Boucler dans `JouerTourCombatAsync` tant que `MoteurReglesCombat.Evaluer`
renvoie une règle ; clôturer avec `Gt` UNE fois (cf. §4).

---

### C4. Limite cast tour codée en dur — pas de plafond global
**Fichier** : `Divers/Combats/IA/ConfigCombat.cs`

Aucune propriété `MaxCastsParTour` globale. Si on corrige C3, le bot pourrait
spammer 4-5 casts en 1 tour → suspect anti-bot. Il faut un plafond config :

```diff
     public int DelaiEntreActionsMs { get; set; } = 800;
+    /// <summary>
+    /// Nombre max de sorts lancés par tour (toutes règles confondues). 0 = illimité,
+    /// défaut 4 (drain PA Ronce x4 = 16 PA → couvre Sadida niv 50). Anti-spam
+    /// anti-détection : un humain ne cast pas plus de 3-4 sorts/tour en moyenne.
+    /// </summary>
+    public int MaxCastsParTour { get; set; } = 4;
```

---

### C5. Détection LOS non appliquée en fallback legacy
**Fichier** : `Commun/Frames/TrameJeu.cs`
**Lignes** : 944-993 (filtre offensifs fallback)

`MoteurReglesCombat.Evaluer` teste `stats?.NecessiteLOS == true && dist > 1`
(l. 147-162) — bien. Mais le fallback legacy (`offensifs.Sort` + boucle
`foreach (var s in offensifs)` l. 952-993) **n'applique aucun test LOS**. Si
aucune règle SynFus n'est en portée, on retombe sur le legacy qui peut
proposer un sort LOS-required avec un combattant entre nous et la cible →
serveur répond `GAF` (échec) → tour perdu.

Symétriquement, **un sort qui ne nécessite PAS de LOS doit caster sans
vérification** (e.g. Ronce traverse les murs). Le moteur règles s'en sort
correctement par le guard `stats?.NecessiteLOS == true`, mais c'est subtil :
si `stats == null` (XML manquant ou variante inconnue), le test est skip
par défaut → cast aveugle. Acceptable (legacy comportement), mais à logguer
en `Debogue` au moins une fois.

**Fix** : extraire un helper `CheminLosLibre(carte, depart, cible, combattants)` et
l'appeler dans les deux chemins.

```diff
         foreach (var s in offensifs)
         {
             /* … coutPA / portee check … */
+            if (statsNiv?.NecessiteLOS == true && distEnnemi > 1
+                && _etat.CarteCourante is { } carteLos
+                && carteLos.Obtenir(maCell) is { } cMoi
+                && carteLos.Obtenir(ennemi.CellulePosition) is { } cEnn)
+            {
+                var occ = new HashSet<int>();
+                foreach (var a in combat.Allies)
+                    if (!a.EstMort && a.Identifiant != perso.Identifiant) occ.Add(a.CellulePosition);
+                foreach (var en in combat.Ennemis)
+                    if (!en.EstMort && en.Identifiant != ennemi.Identifiant) occ.Add(en.CellulePosition);
+                if (Divers.Cartes.LigneVisuelle.EstObstruee(carteLos, cMoi, cEnn, occ))
+                {
+                    Journaliseur.Debogue($"[COMBAT] rejet « {s.Nom} » : LOS obstruée");
+                    continue;
+                }
+            }
             sort = s;
```

---

### C6. `OnMv` handler reste attaché en cas d'exception dans `Task.WhenAny`
**Fichier** : `Divers/Combats/PipelineDeplacementCombat.cs`
**Lignes** : 69-87

Le `try/finally` détache bien `OnMv` en cas de fin normale. Mais si la TCS
n'est jamais résolue (timeout passé, et l'event arrive APRÈS le finally), le
`tcs.TrySetResult` est silencieusement no-op → **OK**.

En revanche, si `Task.Delay(timeoutMs, ct)` lève (token annulé), `OnMv` est
détaché (finally). Mais avant le détachement, l'event peut être tiré et
appeler `tcs.TrySetResult` qui marche encore. **Pas de fuite**, mais il y a
une **race subtile** : si deux mouvements arrivent en rafale (broadcast double),
le 2ᵉ déclenchera `OnMv` après le détachement → ignoré. OK.

**Risque réel** : l'handler `OnActionJeu` (l. 477 `TrameJeu`) peut être réentré
côté UI thread car appelé par la pipeline réseau. Si on ajoute un futur autre
handler à `MouvementBotConfirme`, il faut documenter que `OnMv` doit être
idempotent.

Pas de fix urgent — à documenter dans le XmlDoc.

---

### C7. Race : `Combat.NumeroTour++` lu pendant que `MoteurReglesCombat.Evaluer` itère
**Fichier** : `Divers/Combats/Combat.cs` l. 70-76 + `MoteurReglesCombat.cs` l. 91-94

`NouveauTour` est appelé depuis le repartiteur réseau (`MessageTourCombatAbrak`),
PUIS l'IA est lancée en `Task.Run`-équivalent (`await JouerTourCombatAsync`). Si
un nouveau `GTS` arrive AVANT que `Evaluer` ait fini de regarder
`combat.NumeroTour` (peu probable mais possible si le serveur double-tour
suite à un fail), on lit `NumeroTour` désynchronisé du `idCombattant` initial.

Pas un crash, mais des règles `PremierTour` ou `APartirDuTour` peuvent se
déclencher au mauvais moment.

```diff
     public void NouveauTour(int identifiantCombattant)
     {
         NumeroTour++;
         IdentifiantCombattantActuel = identifiantCombattant;
         CompteursRegleParTour.Clear();
         TourChange?.Invoke(this, identifiantCombattant);
     }
+    /// <summary>Snapshot atomique du tour courant pour évaluation IA (immuable
+    /// pendant l'évaluation, immune aux GTS rafale). À passer à MoteurReglesCombat.</summary>
+    public (int numeroTour, int idCombattant) Snapshot()
+        => (NumeroTour, IdentifiantCombattantActuel);
```

Et passer le snapshot à `Evaluer(...)` au lieu de lire `combat.NumeroTour`
directement.

---

### C8. Pathfinder A* : `ouvertes` est un `List<>` linéaire → O(N²) sur grandes cartes
**Fichier** : `Divers/Cartes/Deplacement/Pathfinder.cs`
**Lignes** : 47-64

```csharp
var ouvertes = new List<Cellule> { depart };
// …
for (int i = 1; i < ouvertes.Count; i++)
{
    if (ouvertes[i].CouF < ouvertes[idx].CouF || …) idx = i;
}
ouvertes.RemoveAt(idx);
```

Sur les 560 cellules d'une map Dofus, recherche linéaire à chaque itération.
Pour combats courts (PM=3) c'est anodin. Pour overworld sur longs trajets
(zaap → village 30+ cells), 560×30 = 16K compares. Une `PriorityQueue<>` .NET 6+
règle le problème en O(log N) :

```diff
-            var ouvertes = new List<Cellule> { depart };
+            var ouvertes = new PriorityQueue<Cellule, (int, int)>();
+            ouvertes.Enqueue(depart, (0, 0));
+            var dansOuvertes = new HashSet<Cellule> { depart };
             /* … */
-            while (ouvertes.Count > 0)
+            while (ouvertes.TryDequeue(out var courante, out _))
             {
-                int idx = 0;
-                for (int i = 1; i < ouvertes.Count; i++) /* … */
-                var courante = ouvertes[idx];
-                ouvertes.RemoveAt(idx);
+                dansOuvertes.Remove(courante);
                 /* … */
                 /* enqueue voisin avec priority (CouF, -CouG) */
             }
```

Pas critique fonctionnellement, mais **CombatApproche** lance `Pathfinder.Trouver`
**N fois** (une par cellule candidate dans `TrouverApprocheCombat` l. 1256) →
sur une map 560 cells, ça pète vite quand PM=6.

---

## §2 — Issues MOYENNES

### M1. `TrouverApprocheCombat` recalcule N pathfinders pour N cellules candidates
**Fichier** : `Commun/Frames/TrameJeu.cs`
**Lignes** : 1236-1273

Pour chaque cellule de la carte où `dEstimee ≤ pmMax` et dist Chebyshev dans
[porteeMin, porteeMax], on appelle `Pathfinder.Trouver(...)`. Sur Beiloddurul
(PM=3) c'est ~10-30 cells candidates × A* 560 nœuds = ~10K opérations. Acceptable
mais lent (passage 100ms+ par tour).

**Optimisation** : faire UN seul A* multi-cibles depuis `depart` (Dijkstra avec
plusieurs goals), récupérer les `Distance(depart → cell)` en une passe, puis
filtrer les cells dans la portée du sort et choisir la meilleure par score.

**Workaround minimal** : trier les candidates par score puis tester en commençant
par la meilleure ; arrêter dès qu'on en trouve une atteignable. Sortie anticipée :

```diff
-        BotDofus.Divers.Cartes.Cellule? meilleureCible = null;
-        List<BotDofus.Divers.Cartes.Cellule>? meilleurChemin = null;
-        double meilleurScore = double.MaxValue;
-        int meilleurNbPasTie = int.MaxValue;
-        int meilleureDist = -1;
-        foreach (var c in carte.Cellules) { … gros foreach lourd … }
+        // 1) Pré-sélection lazy : on candide d'abord par score (sans pathfinder),
+        //    puis on path-find dans l'ordre du meilleur score.
+        var candidates = new List<(BotDofus.Divers.Cartes.Cellule c, int dist, double score)>();
+        foreach (var c in carte.Cellules)
+        {
+            if (c == null || !c.EstMarchable || c.IdInteractif >= 0 || interdites.Contains(c)) continue;
+            int d = System.Math.Max(System.Math.Abs(c.X - xE), System.Math.Abs(c.Y - yE));
+            if (d < porteeMin || d > porteeMax) continue;
+            int dEstimee = System.Math.Abs(c.X - depart.X) + System.Math.Abs(c.Y - depart.Y);
+            if (dEstimee > pmMax || dEstimee == 0) continue;
+            candidates.Add((c, d, ScoreCelluleMode(d, mode, porteeMax, distPref)));
+        }
+        // 2) Tri par score asc, puis dEstimee asc (économie PM).
+        candidates.Sort((a, b) =>
+        {
+            int s = a.score.CompareTo(b.score);
+            return s != 0 ? s : System.Math.Abs(a.c.X - depart.X) + System.Math.Abs(a.c.Y - depart.Y)
+                                  - (System.Math.Abs(b.c.X - depart.X) + System.Math.Abs(b.c.Y - depart.Y));
+        });
+        // 3) Path-find seulement les top-K meilleures (10 max).
+        foreach (var (c, d, _) in candidates.Take(10))
+        {
+            var chemin = Pathfinder.Trouver(carte, depart, c, interdites, combat: true);
+            if (chemin == null || chemin.Count - 1 > pmMax || chemin.Count == 1) continue;
+            return (chemin, d);
+        }
+        return null;
```

---

### M2. `DistanceDofus` instance method appelle `_etat.CarteCourante` à chaque appel
**Fichier** : `Commun/Frames/TrameJeu.cs`
**Lignes** : 1159-1165 + 939, 941, 1062

```csharp
private int DistanceDofus(int idA, int idB)
{
    int mw = _etat.CarteCourante?.Largeur ?? Carte.LargeurParDefaut;
    /* … */
}
```

Appelé 3-5 fois par tour → 3-5 lectures de `_etat.CarteCourante.Largeur`.
Pas critique en performance, mais surtout **incohérent** : `MoteurReglesCombat`
prend `mapWidth` en paramètre (l. 50 : `mapWidth = carte.Largeur > 0 ? carte.Largeur : Carte.LargeurParDefaut`).
Les 2 chemins doivent partager le même mapWidth, sinon distance ≠ entre cast et filtre.

**Risque concret** : si `CarteCourante` change entre 2 appels (changement de
map mid-tour ?), Largeur peut différer → portée mal calculée → cast rejeté.

```diff
-    private int DistanceDofus(int idA, int idB)
+    private int DistanceDofus(int idA, int idB)
     {
-        int mw = _etat.CarteCourante?.Largeur ?? Carte.LargeurParDefaut;
+        int mw = _largeurCarteSnapshot > 0 ? _largeurCarteSnapshot : Carte.LargeurParDefaut;
         /* … */
     }
```

Et `_largeurCarteSnapshot` snapshotté au début de `JouerTourCombatAsync`.

---

### M3. `LigneVisuelle.EstObstruee(a, b, ISet<int>)` (V1 simple) renvoie **toujours false**
**Fichier** : `Divers/Cartes/LigneVisuelle.cs`
**Lignes** : 30-106

```csharp
public static bool EstObstruee(Cellule a, Cellule b, ISet<int> cellulesOccupees)
{
    /* … 70 lignes de Bresenham … */
    _ = cellulesOccupees;
    return false; // PATH MORT
}
```

Cette surcharge fait 70 lignes de Bresenham puis **return false inconditionnel**.
**Code mort confirmé** : `cellulesOccupees` n'est jamais consulté. Le seul appel
réel est l'overload `EstObstruee(carte, a, b, ISet<int>)` (l. 113). À supprimer
ou à émettre un `Obsolete` :

```diff
+    [System.Obsolete("Utiliser la surcharge avec Carte. Cette surcharge V1 ne teste rien.", error: false)]
     public static bool EstObstruee(Cellule a, Cellule b, ISet<int> cellulesOccupees)
     {
+        // V1 inopérante : le calcul Bresenham qui suit n'a aucun effet.
+        // Conservé pour compatibilité API ; à supprimer en v0.5.
         /* … */
     }
```

---

### M4. Détection mouvement combat : reset du compteur `NbLootsRecus`/`_dernier*` pas symétrique
**Fichier** : `Commun/Frames/TrameJeu.cs`
**Lignes** : 80-92

Au `MessageFinCombat`, on reset `_dernierNbVivants = -1; _dernierNbCombattants = -1`.
Mais **on ne reset PAS** `_acteursVus` (l. 31) ni l'état tour côté `Combat.NumeroTour`.
Si un nouveau combat redémarre vite, certains acteurs vus avant ne seront pas
re-loggués → l'utilisateur ne sait pas qu'ils sont là.

```diff
         Ecouter<MessageFinCombat>(_ =>
         {
             _etat.Combat.Reinitialiser();
             _dernierNbVivants = -1;
             _dernierNbCombattants = -1;
+            _acteursVus.Clear();
             /* … */
         });
```

---

### M5. `OnSelectionPersonnage` modifie `Inventaire` sans lock (thread reseau vs UI WPF)
**Fichier** : `Commun/Frames/TrameJeu.cs`
**Lignes** : 282-295

Le `Inventaire.Clear()` + `Add(...)` se fait depuis le thread réseau. La vue WPF
peut être en train d'énumérer la collection au même moment → `InvalidOperationException`.

Pareil pour `_etat.Combat.Allies.Clear()` / `Add()` (`PeuplerCombatDepuisCarte`
l. 825-872, et `OnCombattantsAbrak` l. 201-202 `.RemoveAll`).

```diff
+        // CRITIQUE : muter la collection depuis le thread réseau pendant que
+        // la UI itère lève InvalidOperationException. Idéalement bascule sur
+        // Application.Current.Dispatcher (mais TrameJeu est hors WPF).
+        // Workaround : verrou explicite côté Inventaire/Allies/Ennemis.
```

Refacto : exposer `ObservableCollection<T>` thread-safe (BindingOperations.EnableCollectionSynchronization)
ou queuer les updates dans une `Channel<>` consommée côté UI.

---

### M6. `_etat.Personnage.CellulePosition` modifié sans synchronisation
**Fichier** : `Commun/Frames/TrameJeu.cs`
**Lignes** : 530-543

```csharp
if (moi)
{
    int? avant = _etat.Personnage.CellulePosition;
    _etat.Personnage.CellulePosition = cell;
    _etat.CarteCourante?.SignalerRechargee();
    if (_etat.Combat.Etat != EtatCombat.Inactif)
    {
        _etat.Combat.DeclencherMouvementBot(acteurId, cell, chemin);
    }
}
```

L'event `MouvementBotConfirme` est tiré **synchroneusement** sur le thread
réseau. Les listeners (`PipelineDeplacementCombat.OnMv`) marshallent vers
TCS qui réveille l'await dans `JouerTourCombatAsync`. La continuation de
l'await s'exécute potentiellement sur ce même thread réseau (à cause de
`ConfigureAwait(false)` partout).

Conséquence : pendant la continuation, on appelle `_session.EnvoyerAuServeurAsync`
qui peut sérialiser en aval, le tout sur le thread réseau qui devrait être en
train de lire le prochain paquet. Goulot.

```diff
-        => MouvementBotConfirme?.Invoke(this, new MouvementBotArgs { … });
+    {
+        // Découple le thread réseau de la suite IA : on poste l'event en
+        // arrière-plan pour ne pas bloquer la lecture des paquets entrants.
+        var args = new MouvementBotArgs { … };
+        var handler = MouvementBotConfirme;
+        if (handler != null)
+            System.Threading.ThreadPool.QueueUserWorkItem(_ => handler(this, args));
+    }
```

---

### M7. `Pathfinder.Distance` retourne distance EUCLIDIENNE carrée → mauvaise heuristique A*
**Fichier** : `Divers/Cartes/Deplacement/Pathfinder.cs`
**Lignes** : 177-178

```csharp
private static int Distance(Cellule a, Cellule b)
    => (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
```

Pour A*, l'heuristique doit être ADMISSIBLE (≤ vrai coût). Ici, dist² peut
être >> vrai coût (Manhattan en 4-dir). Conséquence : A* devient suboptimal
voire **gloutonne biased** — choisit parfois un chemin plus long qu'un autre
exploré.

Symétriquement, `gTemporaire = courante.CouG + Distance(voisin, courante)`
(l. 104) utilise aussi dist². Comme chaque pas coûte exactement 1 (Manhattan
4-dir), CouG accumule des **1** (pas adjacent = (dx,dy)=(1,0) → 1²+0²=1) ou
des **2** (diagonale = (1,1) → 1+1=2). En 8-dir overworld, ça PÉNALISE les
diagonales (coût 2 au lieu de 1.414 ou 1). En 4-dir combat, OK puisque tous
voisins ont coût 1.

```diff
-    private static int Distance(Cellule a, Cellule b)
-        => (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
+    // Coût de transition (utilisé pour g) : 1 en 4-dir (Manhattan), 1 ou √2 en 8-dir.
+    // Heuristique (utilisée pour h) : Chebyshev = max(|dx|, |dy|) = admissible.
+    private static int CoutTransition(Cellule a, Cellule b)
+    {
+        int dx = System.Math.Abs(a.X - b.X);
+        int dy = System.Math.Abs(a.Y - b.Y);
+        // 4-dir donne toujours (dx+dy)=1 ; 8-dir diag = (1,1) → coût 2 (approx √2*1.414, mais entier-safe)
+        return dx + dy;
+    }
+    private static int Heuristique(Cellule a, Cellule b)
+        => System.Math.Max(System.Math.Abs(a.X - b.X), System.Math.Abs(a.Y - b.Y));
```

Et l. 104 : `int gTemporaire = courante.CouG + CoutTransition(voisin, courante);`
Et l. 118 : `voisin.CouH = Heuristique(voisin, arrivee);`

---

### M8. `EstMonTour` calcul : aucune utilisation
**Fichier** : `Divers/Combats/Combat.cs`
**Lignes** : 109-110

`public bool EstMonTour => …` — pas un bug, mais Grep montre qu'il n'est utilisé
**nulle part**. Soit retirer, soit câbler dans `MoteurReglesCombat.Evaluer`
comme garde précoce.

---

### M9. `DecideurCombat` legacy : code mort accessible via `#pragma warning disable CS0162`
**Fichier** : `Divers/ContexteCompte.cs`
**Lignes** : 182-252

Le handler `Combat.TourChange` retourne en première ligne (`return;` l. 184),
le `#pragma warning disable CS0162` masque le warning C# « code mort ». OK,
documenté, mais ce code legacy partage `ConfigCombat` avec la vraie IA → si
quelqu'un retire ce `return` un jour, il déclenche un **double-jeu** comme
documenté (l. 178). **Risque de régression** : extraire le code mort dans un
fichier `obsolete/` ou le supprimer pour de bon.

---

### M10. `Combat.Reinitialiser` ne reset PAS `IdentifiantAllie`
**Fichier** : `Divers/Combats/Combat.cs`
**Lignes** : 89-101

```csharp
public void Reinitialiser()
{
    Allies.Clear();
    Ennemis.Clear();
    /* … */
    NumeroTour = 0;
    IdentifiantCombattantActuel = 0;
    /* IdentifiantAllie pas reset */
}
```

Si on quitte un combat et qu'un autre démarre avec un autre `IdentifiantAllie`
(multi-perso swap), le moteur de règles utilisera l'ancien ID → `moi` introuvable
dans `combat.Allies`, return null (MoteurReglesCombat:53). Symptôme : « pas de
règle évaluée, fallback legacy ». Difficile à diag.

```diff
     public void Reinitialiser()
     {
         Allies.Clear();
         Ennemis.Clear();
         /* … */
         NumeroTour = 0;
         IdentifiantCombattantActuel = 0;
+        IdentifiantAllie = 0;
         CompteursRegleParTour.Clear();
         /* … */
     }
```

---

## §3 — Issues MINEURES

### m1. Off-by-one : `nbPas == 0` skip silencieux
**Fichier** : `Commun/Frames/TrameJeu.cs` l. 1260

```csharp
if (nbPas == 0) continue; // déjà à cette case
```

Pourrait logguer en `Debogue` pour aider la debug (« j'ai trouvé la cible mais
je suis déjà dessus »).

---

### m2. Calculs `100 * PV / PVMax` peut diviser par 0
**Fichier** : `Divers/Combats/IA/MoteurReglesCombat.cs` l. 79, 122
**Fichier** : `MoteurReglesCombat.cs` l. 193

Garde existante : `if (moi.PVMax > 0)` (l. 77). OK.
Pour `cible.PVMax`, l. 120 : `if (cible.PVMax > 0)`. OK.
Pour `AllieLePlusBlesse` l. 193 : `a.PVMax > 0 ? 100 * a.PV / a.PVMax : 100` — OK.

**Mais** : si `PV = 0` et `PVMax > 0` → `mesPvPct = 0` → règle `MesPvInfPourcent.Value = 30`
→ `0 >= 30 ? false → ne continue pas`. Le perso à 0 PV peut tenter de cast.
Devrait être filtré en amont par `if (moi.EstMort) return null;`.

```diff
         var moi = combat.Allies.FirstOrDefault(c => c.Identifiant == combat.IdentifiantAllie);
-        if (moi == null) return null;
+        if (moi == null || moi.EstMort) return null;
```

---

### m3. `mes_PvSup/Inf` logique inversée vs intention SynFus
**Fichier** : `Divers/Combats/IA/MoteurReglesCombat.cs` l. 80-81

```csharp
if (regle.MesPvInfPourcent.HasValue && mesPvPct >= regle.MesPvInfPourcent.Value) continue;
if (regle.MesPvSupPourcent.HasValue && mesPvPct <= regle.MesPvSupPourcent.Value) continue;
```

Sémantique : « lance ce sort si mes PV < X% ». Si `MesPvInfPourcent=30` et
`mesPvPct=20` → `20 >= 30 ? false → on ne continue pas → règle évaluée → OK`.
Si `mesPvPct=40` → `40 >= 30 ? true → continue` → skip règle. **Correct**.

Pour `Sup` : « lance si mes PV > Y% ». Si `Y=50` et `mesPvPct=70` → `70 <= 50 ? false → on ne continue pas → règle évaluée → OK`.
Si `mesPvPct=30` → `30 <= 50 ? true → continue`. **Correct**.

OK, juste pas évident à la lecture. Ajouter un commentaire :

```diff
+            // Sémantique SynFus :
+            //   MesPvInfPourcent=X → "lance ce sort si PV% < X" (panic/heal trigger).
+            //   MesPvSupPourcent=Y → "lance si PV% > Y" (offensifs quand en forme).
             if (regle.MesPvInfPourcent.HasValue && mesPvPct >= regle.MesPvInfPourcent.Value) continue;
             if (regle.MesPvSupPourcent.HasValue && mesPvPct <= regle.MesPvSupPourcent.Value) continue;
```

---

### m4. `Thread.Sleep` jamais utilisé — bonne nouvelle (mais ContexteCompte:151 `Task.Delay(800)` sans `ConfigureAwait(false)`)
**Fichier** : `Divers/ContexteCompte.cs` l. 151, 153, 221, 236, 241

Tous les `Task.Delay` sont sans `ConfigureAwait(false)`. Côté ContexteCompte
on est sur la pipeline réseau, pas WPF → impact faible. Mais incohérent avec
le reste du code (TrameJeu utilise `.ConfigureAwait(false)` partout).

```diff
-                    await System.Threading.Tasks.Task.Delay(800);
+                    await System.Threading.Tasks.Task.Delay(800).ConfigureAwait(false);
```

---

### m5. Listener `Compte.EtatChange` détaché dans Dispose, MAIS pas `EtatJeu.Combat.EtatChange`
**Fichier** : `Divers/ContexteCompte.cs` l. 132, 612

```csharp
EtatJeu.Combat.EtatChange += async (_, etat) => { /* … */ };
// …
public void Dispose()
{
    // pas de Combat.EtatChange -= …
}
```

Lambda anonyme = handler non-référencable → impossible à détacher. Si l'utilisateur
recrée des `ContexteCompte` (changement de compte), les anciens lambdas restent
abonnés à `Combat` qui reste l'instance par `ContexteCompte`. **Pas une fuite
mémoire pratique** (le `Combat` lui-même est jeté avec le Contexte), mais
mauvais pattern.

Refacto : nommer les handlers (`private async void OnCombatEtatChange(object? sender, EtatCombat etat) { … }`).

---

### m6. `RegleSort.Cible` (compat) : enum `CibleSort` marqué Obsolete mais utilisé en getter
**Fichier** : `Divers/Combats/IA/RegleSort.cs` l. 41-62

```csharp
[JsonIgnore] public CibleSort Cible { get => Focus switch …; set => Focus = value switch …; }
```

`CibleSort` est marqué `[Obsolete]` l. 153. Si du code legacy lit `regle.Cible`,
il s'expose à un warning. La compat est utile mais devrait être contenue
strictement dans `ApiLua` (ou retirée si plus aucun script n'y touche).

---

### m7. `RegleSort.RecastSiEchec` jamais lu
**Fichier** : `Divers/Combats/IA/RegleSort.cs` l. 71

Grep confirme : aucune lecture nulle part. Soit câbler (catch GAF échec → re-éval),
soit supprimer du JSON model.

---

### m8. `ConfigCombat.GenererParDefaut` ne tient pas compte du Mode
**Fichier** : `Divers/Combats/IA/ConfigCombat.cs` l. 139-161

Crée toutes les règles avec `Focus = EnnemiLePlusProche`. Si le user a passé
en `Mode = Eloigne`, l'IA va tourner autour de l'ennemi en s'éloignant (mode
positionnement) MAIS cibler l'ennemi le plus proche → contradictoire. Devrait
choisir `Focus` cohérent avec le `Mode` par défaut.

---

### m9. `Pathfinder.Trouver` : `cellulesInterdites ??= new List<Cellule>()` puis `new HashSet(...)`
**Fichier** : `Divers/Cartes/Deplacement/Pathfinder.cs` l. 42-48

```csharp
cellulesInterdites ??= new List<Cellule>();
// …
var fermees = new HashSet<Cellule>(cellulesInterdites);
```

Allocation List → puis copie dans HashSet. Si déjà HashSet en entrée, pas grave
(O(1) lookups). Mais l'appelant passe systématiquement un `HashSet<Cellule>`
(cf. TrameJeu:1208) → on alloue inutilement la List intermédiaire.

```diff
-            cellulesInterdites ??= new List<Cellule>();
-            // …
-            var fermees = new HashSet<Cellule>(cellulesInterdites);
+            var fermees = cellulesInterdites is HashSet<Cellule> hs
+                ? new HashSet<Cellule>(hs)  // copie pour mutation locale
+                : new HashSet<Cellule>(cellulesInterdites ?? Enumerable.Empty<Cellule>());
```

---

### m10. `LigneVisuelle` Bresenham : pad précision ×100 mais condition `dx == dy` pas float-safe
**Fichier** : `Divers/Cartes/LigneVisuelle.cs` l. 40, 132

```csharp
if (System.Math.Abs(dx) == System.Math.Abs(dy))
```

Compare des `double` → float equality dangereuse. En l'occurrence ici dx/dy
sont des `int.X - int.X = int` cast en double → l'égalité est sûre. Mais lecture
fragile. Préférer :

```diff
-        if (System.Math.Abs(dx) == System.Math.Abs(dy))
+        if (System.Math.Abs(System.Math.Abs(dx) - System.Math.Abs(dy)) < 1e-9)
```

(détail mineur, codestyle.)

---

## §4 — Améliorations suggérées (refacto / architecture)

### A1. Extraire un `MoteurCombat` orchestrant la boucle complète
**Pourquoi** : `JouerTourCombatAsync` fait 260 lignes (884-1144) → mélange IA,
réseau, log, humanisation. Difficile à tester, impossible à mocker.

**Quoi** : créer `Divers/Combats/IA/MoteurCombat.cs` :

```csharp
public sealed class MoteurCombat
{
    private readonly Combat _combat;
    private readonly Personnage _perso;
    private readonly Carte _carte;
    private readonly ConfigCombat _cfg;
    private readonly ISessionEnvoi _session;   // interface pour mock

    public async Task<ResultatTour> JouerTourAsync(CancellationToken ct);
}

public abstract record EtapeTour;
public sealed record EtapeAttendre(int Ms) : EtapeTour;
public sealed record EtapeCastSort(int IdSort, int Cible) : EtapeTour;
public sealed record EtapeDeplacer(string PaquetGA001) : EtapeTour;
public sealed record EtapePassTour : EtapeTour;
```

Les `Etape*` sont émises par le moteur, consommées par un orchestrateur qui
les traduit en paquets réseau. Tests unitaires triviaux.

---

### A2. `Combattant.CellulePosition` devrait être un `int?` pas `int`
**Pourquoi** : actuellement `int` avec 0 comme valeur sentinel « pas placé » →
risque de bug (cell 0 existe-t-elle ? non sur la grille iso, mais quand même).

```diff
-    public int CellulePosition { get; set; }
+    public int? CellulePosition { get; set; }
```

Impacte beaucoup d'appelants → refacto coûteuse mais propre.

---

### A3. INotifyPropertyChanged sur `Personnage` / `Combattant`
**Pourquoi** : aujourd'hui, les vues WPF se reabonnent à des events maison
(`Mis_A_Jour`, `SortsChanges`, `PositionsChangees`). Trop ad-hoc, raté quand
on ajoute une propriété → la vue ne se met pas à jour.

**Quoi** : faire hériter `Personnage` et `Combattant` de `ObservableObject`
(CommunityToolkit.Mvvm) → `[ObservableProperty]` sur PA/PM/PV/Cell/etc. génère
tout via source-gen. Plus de `NotifierInventaireChange()` manuel.

---

### A4. Tests unitaires `MoteurReglesCombat`
**Pourquoi** : aujourd'hui zéro test sur la pipeline IA. Toute modif a un risque
de régression invisible.

**Quoi** : un projet `BotDofus.Tests/Combats/MoteurReglesCombatTests.cs` avec
scenarios :
- 0 règle → null
- 1 règle valide en portée → return cette règle
- 1 règle hors portée → null
- 1 règle PA insuffisant → null
- Focus EnnemiLePlusProche → bonne cible
- Focus AllieLePlusBlesse → bon allié
- LOS obstrué → continue à la règle suivante
- NombreParTour atteint → continue
- Priorité décroissante respectée

Mocks : `Combat`, `Personnage`, `Carte` (factories simples).

---

### A5. ADR-004 « Multi-cast par tour & limite globale »
**Pourquoi** : la décision multi-cast (cf. C3 + C4) mérite un ADR pour figer le
contrat (max global, ordre des règles, stop sur PA insuffisant, etc.).

**Quoi** : `docs/ADR-004-multicast-tour.md` contenant :
- Sémantique : tant que `Evaluer` renvoie une règle, on cast, on incrémente
  `CompteursRegleParTour`, on attend GAS/GAF, on re-evalue.
- Plafond : `MaxCastsParTour` (proposé en C4).
- Stop conditions : PA < min cost / aucune règle / déplacement bloqué.

---

### A6. Logger structuré pour les rejets de règles
**Pourquoi** : aujourd'hui le moteur rejette des règles avec `continue` muet.
L'utilisateur ne sait pas POURQUOI sa règle « Ronce » n'a pas été lancée.

```diff
+            // Snapshot diagnostic
+            var raison = new System.Text.StringBuilder();
             /* … remplir les continue avec raison.Append("PA<cost ") puis return … */
+            Journaliseur.Debogue($"[IA-RULE] règle « {regle.Nom} » #{regle.IdSort} REJETÉE : {raison}");
```

Sinon, dans VueCombat, prévoir un panneau « dernière décision IA » avec
détail des rejets.

---

### A7. Détacher proprement `MouvementBotConfirme` quand la TCS gagne sur timeout
**Fichier** : `Divers/Combats/PipelineDeplacementCombat.cs`

Le `finally` détache mais entre le `Task.WhenAny` et le `finally`, si plusieurs
events arrivent en rafale, l'OnMv est appelé pour chacun. TrySetResult ignore
les suivants → OK. Mais on peut court-circuiter en détachant DÈS QUE le TCS
a un result :

```diff
         void OnMv(object? s, MouvementBotArgs e)
         {
             if (e.IdActeur != idMoi) return;
-            tcs.TrySetResult((e.CellArrivee, e.CellArrivee == cellAttendue));
+            if (tcs.TrySetResult((e.CellArrivee, e.CellArrivee == cellAttendue)))
+                combat.MouvementBotConfirme -= OnMv; // self-detach
         }
```

---

### A8. `IDisposable` sur `ContexteCompte` ne dispose pas `EtatJeu`
**Fichier** : `Divers/ContexteCompte.cs` l. 603-614

`EtatJeu` n'implémente pas IDisposable, mais ses sous-objets (Combat avec ses
events) ne sont jamais nettoyés. Pas une fuite mémoire critique (le contexte
entier est jeté), mais à documenter.

---

## §5 — Tableau récapitulatif issue / fichier / criticité

| ID | Fichier | Lignes | Sévérité | Catégorie |
|----|---------|--------|----------|-----------|
| C1 | TrameJeu.cs | 118-119, 175-176 | CRITIQUE | Exception silencieuse |
| C2 | TrameJeu.cs | 118, 175 | CRITIQUE | NRE potentiel |
| C3 | TrameJeu.cs | 1127-1144, 1313-1339 | CRITIQUE | Multi-cast inopérant |
| C4 | ConfigCombat.cs | (à ajouter) | CRITIQUE | Pas de plafond cast/tour |
| C5 | TrameJeu.cs | 944-993 | CRITIQUE | LOS non testée en fallback |
| C6 | PipelineDeplacementCombat.cs | 69-87 | (info) | Race events |
| C7 | Combat.cs / MoteurReglesCombat.cs | 70-76 / 91-94 | CRITIQUE | Race tour++ |
| C8 | Pathfinder.cs | 47-64 | CRITIQUE-perf | O(N²) A* |
| M1 | TrameJeu.cs | 1236-1273 | MOYENNE | N pathfinders en boucle |
| M2 | TrameJeu.cs | 1159-1165 | MOYENNE | mw lu N fois |
| M3 | LigneVisuelle.cs | 30-106 | MOYENNE | Path mort |
| M4 | TrameJeu.cs | 80-92 | MOYENNE | `_acteursVus` pas reset |
| M5 | TrameJeu.cs | 282-295 | MOYENNE | Race UI vs réseau |
| M6 | TrameJeu.cs | 530-543 | MOYENNE | Event sur thread réseau |
| M7 | Pathfinder.cs | 177-178 | MOYENNE | Heuristique A* fausse |
| M8 | Combat.cs | 109-110 | MOYENNE | Méthode jamais utilisée |
| M9 | ContexteCompte.cs | 182-252 | MOYENNE | Code mort obsolète |
| M10 | Combat.cs | 89-101 | MOYENNE | `IdentifiantAllie` pas reset |
| m1 | TrameJeu.cs | 1260 | mineure | Skip silencieux |
| m2 | MoteurReglesCombat.cs | 53 | mineure | Garde EstMort manquante |
| m3 | MoteurReglesCombat.cs | 80-81 | mineure | Commentaire manquant |
| m4 | ContexteCompte.cs | 151+ | mineure | Manque ConfigureAwait |
| m5 | ContexteCompte.cs | 132, 612 | mineure | Lambda non détachable |
| m6 | RegleSort.cs | 41-62 | mineure | API obsolète exposée |
| m7 | RegleSort.cs | 71 | mineure | RecastSiEchec mort |
| m8 | ConfigCombat.cs | 139-161 | mineure | Focus default ignore Mode |
| m9 | Pathfinder.cs | 42-48 | mineure | Alloc inutile |
| m10 | LigneVisuelle.cs | 40, 132 | mineure | Float equality |
| A1 | nouveau `MoteurCombat.cs` | — | refacto | Extraction orchestrateur |
| A2 | Combattant.cs | 9 | refacto | int → int? |
| A3 | Personnage / Combattant | — | refacto | INotifyPropertyChanged |
| A4 | (nouveau projet) | — | refacto | Tests unitaires |
| A5 | docs/ADR-004 | — | doc | ADR multi-cast |
| A6 | MoteurReglesCombat.cs | — | refacto | Logger structuré |
| A7 | PipelineDeplacementCombat.cs | 69-87 | refacto | Self-detach |
| A8 | ContexteCompte.cs | 603 | refacto | Documenter dispose |

---

## §6 — Priorisation conseillée

1. **C1, C2, C5** : 30 min — corrige les NRE silencieuses + LOS fallback.
2. **C3, C4, A5** : 2-3 h — multi-cast + ADR + impl boucle dans `JouerTourCombatAsync`.
3. **C7, M10** : 30 min — snapshot tour + reset `IdentifiantAllie`.
4. **M1, M2, M7, C8** : 2-3 h — perf pathfinder (PriorityQueue + heuristique correcte + 1 A* / candidate).
5. **M3, m7, m6** : 30 min — nettoyage code mort.
6. **M4, M5, M6** : 1 h — thread safety carte/entités.
7. **A1** + **A4** : 1 jour — extraction `MoteurCombat` testable.

Issues mineures peuvent être groupées en commit `chore(ia-combat): cleanup
warnings & code mort` (~30 min).
