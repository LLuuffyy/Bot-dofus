# IA Combat — Doc de référence Luffy-bot (refonte nuit 2026-05-21)

> Doc n°3 / 4. Le doc CENTRAL : explique en détail comment notre IA combat
> fonctionne aujourd'hui, où sont les points bloquants, et comment elle
> se compare ligne-à-ligne à SynFus / dyshay (cf. docs 1 et 2).
>
> Lire APRÈS `ANALYSE-DYSHAY-COMBAT.md` (qui sert de référence).
> Ce doc cite explicitement les chemins absolus de tous les fichiers.

---

## 1. Architecture générale

### 1.1 Composants impliqués (chemins absolus)

```
C:\Users\touki\Desktop\Mélange\Bot-dofus\
├── Commun\Reseau\SessionProxy.cs                       # MITM, cipher '-' canal sensible
├── Commun\Frames\TrameJeu.cs                           # Handler paquets gameplay + IA combat ⭐
├── Commun\Frames\TrameCombat.cs                        # Phase placement (legacy)
├── Divers\Combats\
│   ├── Combat.cs                                       # État runtime combat (Allies/Ennemis/Tour)
│   ├── Combattants\Combattant.cs                       # PV/PA/PM/cell/équipe
│   ├── Enums\EtatCombat.cs                             # Inactif/Placement/EnCours
│   ├── PipelineDeplacementCombat.cs                    # Event-based confirmation GA;0/1
│   └── IA\
│       ├── ConfigCombat.cs                             # Profil par perso (Mode/Strategie/Regles/Soin)
│       ├── RegleSort.cs                                # Règle déclarative + 22 conditions
│       ├── MoteurReglesCombat.cs                       # Évalue les règles → ResultatRegle
│       ├── DecideurCombat.cs                           # (legacy, à supprimer post-refonte)
│       ├── StrategieCombat.cs                          # Agressif/Tactique/Defensif/Soutien/Passif/Fugitif
│       ├── ModeCombat.cs                               # Agressif/Eloigne/Fuyard/Equilibre
│       └── ActionCombat.cs                             # PasserTour/SeDeplacer/LancerSort/UtiliserObjet
├── Divers\Cartes\Deplacement\Pathfinder.cs             # A* + encodage GA001
├── Divers\Cartes\LigneVisuelle.cs                      # Bresenham LOS
├── Divers\Jeu\Personnage\Spells\BaseSorts.cs           # XML dyshay chargé (281 sorts × niveaux)
├── Divers\Compte.cs                                    # ConfigCombat persistée par compte
├── Divers\ContexteCompte.cs                            # Glue : compte ↔ session ↔ trames
└── BotDofus.Wpf\Vues\VueCombat.xaml(.cs)               # UI Material Design (rotation + conditions)
```

### 1.2 Flux de données runtime

```
[Client Dofus] ↔ [SessionProxy MITM] ↔ [Serveur Hystoria]
                          ↓
              [GestionnaireTrames]
                          ↓ dispatch
              [TrameJeu] ← écoute GTM/GTS/GA0/GAS/GAF
                          ↓
                ┌─────────┴─────────┐
                ↓                    ↓
        [Combat.cs] état       [Personnage] PV/PA/PM
                ↓
        [MoteurReglesCombat.Evaluer(combat, cfg, sortsAppris, carte)]
                ↓
        ResultatRegle? (règle/sort/cible/portée)
                ↓ si null
        [TrouverApprocheCombat] fallback legacy
                ↓
        [PipelineDeplacementCombat] attend broadcast GA;0/1
                ↓
        [SessionProxy.EnvoyerAuServeurAsync] : GA001 → GKK0 (overworld) / pas GKK0 combat / GA300 / Gt
```

---

## 2. Cycle d'un tour de combat (côté notre bot)

Source : `TrameJeu.JouerTourCombatAsync` (`Commun\Frames\TrameJeu.cs:884-1144`).

### Étape 1 — Détection du tour

```csharp
// TrameJeu.cs ligne 143-178
Ecouter<MessageTourCombatAbrak>(async msg =>
{
    if (!msg.EstTour) return;                           // GTSX (sorts) ignoré
    _etat.Combat.IdentifiantAllie = _etat.Personnage.Identifiant;
    _etat.Combat.PassageEnCombat();
    _etat.Combat.NouveauTour(msg.IdentifiantCombattant); // reset CompteursRegleParTour
    if (msg.IdentifiantCombattant != _etat.Personnage.Identifiant) return;
    if (_compte.ModePassif) { /* skip */ return; }
    await JouerTourCombatAsync();
});
```

Trigger : paquet `GTS<id>|<timerMs>|<numTour>` (format Abrak Hystoria, **PAS**
le `GTS<id>` overworld classique).

### Étape 2 — Délai de réaction humanisé

```csharp
int delaiReaction = System.Random.Shared.Next(1400, 2100);  // 1.4-2.1s
await Task.Delay(delaiReaction);
```

Reflète capture user passif 16:21-16:22 (1.5-1.7s entre voir tour et cliquer
sort). Notre ancien Random(600, 1200) était suspect.

### Étape 3 — Filtre cibles fantômes

```csharp
var ennemisVivants = combat.Ennemis
    .Where(e => !e.EstMort && e.PV > 0 && e.PVMax > 0)
    .ToList();
if (ennemisVivants.Count == 0)
{
    await _session.EnvoyerAuServeurAsync("Gt"); return;
}
```

Filet anti-cadavre : Monstre #0 PV=0/0 (jamais reçu de GTM complet) → si on
le laissait passer, le cast GA300 ciblerait une cellule vide → GAF échec
serveur → tour perdu.

### Étape 4 — Tentative moteur règles SynFus

```csharp
var cfg = _compte.ConfigCombat;
if (cfg != null && cfg.Regles.Count > 0 && _etat.CarteCourante != null)
{
    var resultat = MoteurReglesCombat.Evaluer(combat, cfg, perso.SortsAppris, _etat.CarteCourante);
    if (resultat != null)
    {
        await ExecuterRegleAsync(resultat);
        return;
    }
    Journaliseur.Info("[COMBAT] Aucune règle SynFus en portée → fallback legacy");
}
```

Si l'user a configuré des `RegleSort` dans `peleas/<perso>.json`, on évalue
chaque règle selon priorité décroissante. Cf. §3.

### Étape 5 — Fallback legacy (pas de règles configurées)

```csharp
var ennemi = ennemisVivants
    .OrderBy(e => DistanceDofus(maCell, e.CellulePosition))
    .First();                                            // ennemi le plus proche

var offensifs = BaseSorts.Instance.SortsOffensifs(idsAppris).ToList();
offensifs.Sort((a, b) =>                                 // tri niveau desc, PA asc
{
    int nivA = perso.SortsAppris.TryGetValue(a.Identifiant, ...);
    int nivB = perso.SortsAppris.TryGetValue(b.Identifiant, ...);
    if (nivA != nivB) return nivB.CompareTo(nivA);
    return a.CoutPA.CompareTo(b.CoutPA);
});

// cherche un sort lançable IMMÉDIATEMENT
foreach (var s in offensifs)
{
    int niv = perso.SortsAppris[s.Identifiant];
    var statsNiv = s.Stats(niv);                         // XML dyshay
    int coutPA = statsNiv?.CoutPA ?? s.CoutPA;
    int porteeMin = statsNiv?.PorteeMin ?? s.PorteeMin;
    int porteeMax = statsNiv?.PorteeMax ?? s.PorteeMax;
    if (coutPA > perso.PA) continue;
    if (porteeMax > 0 && distEnnemi > porteeMax) continue;
    if (distEnnemi < porteeMin) continue;
    sort = s; sortCoutPA = coutPA; ...; break;
}
```

### Étape 6 — Déplacement si aucun sort en portée

```csharp
if (sort == null && perso.PM > 0)
{
    var sortVise = offensifs.FirstOrDefault(s => CoutPA <= perso.PA);  // meilleur sort sans contrainte portée
    var resApproche = TrouverApprocheCombat(perso, combat, ennemi, sortVise, _compte.ConfigCombat);
    if (resApproche.HasValue)
    {
        var (chemin, distApres) = resApproche.Value;
        int cellArrivee = chemin[^1].Identifiant;
        var paquetDep = Pathfinder.PaquetDeplacement(chemin);          // "GA001<encoding>"
        await _session.EnvoyerAuServeurAsync(paquetDep);

        // Pipeline event-based ADR-002
        int timeoutMs = Math.Max(2500, nbPasMove * 450 + 1000);
        var resultat = await PipelineDeplacementCombat
            .AttendreMouvementOuTimeoutAsync(_etat.Combat, idMoi, cellArrivee, timeoutMs, default);

        switch (resultat)
        {
            case Confirme:        maCell = perso.CellulePosition ?? cellArrivee; break;
            case ConfirmePartiel: maCell = perso.CellulePosition; distApresMove = DistanceDofus(maCell, ennemi.Cell); break;
            case TimeoutSilencieux:
                if (cfg.ModeDeplacementOptimisteSecours) { /* cast aveugle */ }
                else { await Gt(); return; }
                break;
        }
        await Task.Delay(Random.Shared.Next(150, 300));   // PAS de GKK0 après GA001 combat
    }
}
```

### Étape 7 — Cast

```csharp
var paquetSort = $"GA300{sort.Identifiant};{ennemi.CellulePosition}";
await _session.EnvoyerAuServeurAsync(paquetSort);
await Task.Delay(Random.Shared.Next(300, 500));
await _session.EnvoyerAuServeurAsync("GKK0");              // ack action OBLIGATOIRE après GA300
await Task.Delay(Random.Shared.Next(1000, 1500));
await _session.EnvoyerAuServeurAsync("Gt");                // fin tour
```

Séquence calquée sur capture user 16:21:59-16:22:00.130 : `GA300 → 376 ms → GKK0
→ ~1.3 s → Gt`.

---

## 3. Règle de décision d'un sort (`MoteurReglesCombat.Evaluer`)

Source : `Divers\Combats\IA\MoteurReglesCombat.cs:48-167`.

### 3.1 Itération règles par priorité

```csharp
foreach (var regle in cfg.Regles.OrderByDescending(r => r.Priorite))
{
    var sort = BaseSorts.Instance.Trouver(regle.IdSort);                 // sort connu ?
    if (sort == null) continue;
    if (!sortsAppris.TryGetValue(regle.IdSort, out int niveau) || niveau <= 0) continue;
    ...
}
```

### 3.2 Compteurs et conditions filtre (ordre d'évaluation)

1. **NombreParTour** (compteur `combat.CompteursRegleParTour[idSort]`).
2. **Joueur** : MesPvInfPourcent, MesPvSupPourcent, SiInvocPresente (cherche
   allié `EstInvocation && !EstMort`).
3. **Situation** : EnnemisMin/Max, PremierTour (`combat.NumeroTour == 1`),
   APartirDuTour, TousLesNTours.
4. **Stats sort au niveau appris** (XML dyshay) : `sort.Stats(niveau)` →
   CoutPA / PorteeMin / PorteeMax / NecessiteLOS / LigneSeule.
5. **PA disponibles** : `coutPA > perso.PA → continue`.
6. **Sélection cible** :
   - Si `regle.CiblePlusFaible` → override (`OrderBy(e.PV).First()`).
   - Si `regle.CiblePlusForte` → override (`OrderByDescending(e.PV).First()`).
   - Sinon → `ChoisirCible(regle.Focus, ...)` selon `FocusSort`.
7. **Cible** : CiblePvInfPourcent, CiblePvSupPourcent.
8. **Distance Chebyshev** (Dofus iso) : porteeMin ≤ dist ≤ porteeMax.
9. **Distance SynFus** : DistanceMin/Max, IgnorerCAC (dist > 1), SeulementCAC (dist == 1).
10. **MethodeLancement** : CAC (dist=1) / Distance (dist>1) / LesDeux (toutes).
11. **LOS Bresenham** (si `stats.NecessiteLOS == true`) :
    ```csharp
    if (LigneVisuelle.EstObstruee(carte, celluleMoi, celluleCible, occupees))
        continue;
    ```

### 3.3 Choix de cible (`ChoisirCible`)

```csharp
return focus switch
{
    EnnemiLePlusProche => ennemisVivants.OrderBy(e => DistanceDofus(...)).First(),
    EnnemiLePlusFaible => ennemisVivants.OrderBy(e => e.PV).First(),
    EnnemiLePlusFort   => ennemisVivants.OrderByDescending(e => e.PV).First(),
    Moi                => moi,
    AllieLePlusBlesse  => alliesVivants.Where(a => a.PV < a.PVMax)
                                       .OrderBy(a => 100 * a.PV / a.PVMax).First(),
    _ => null  // CelluleVide non implémenté
};
```

### 3.4 Renvoi `ResultatRegle`

```csharp
return new ResultatRegle(regle, sort, cible, dist, coutPA, porteeMin, porteeMax, niveau);
```

Consommé par `TrameJeu.ExecuterRegleAsync` qui envoie `GA300<id>;<cell>` →
GKK0 → Gt et incrémente `CompteursRegleParTour[idSort]`.

---

## 4. Déplacement combat (`TrouverApprocheCombat` + envoi GA001)

Source : `Commun\Frames\TrameJeu.cs:1186-1277`.

### 4.1 Énumération cellules candidates

```csharp
foreach (var c in carte.Cellules)
{
    if (c == null || !c.EstMarchable) continue;
    if (c.IdInteractif >= 0) continue;                  // pas sur un interactif
    if (interdites.Contains(c)) continue;               // combattant vivant dessus

    int d = Math.Max(Math.Abs(c.X - xE), Math.Abs(c.Y - yE));   // Chebyshev portée
    if (d < porteeMin || d > porteeMax) continue;

    int dEstimee = Math.Abs(c.X - depart.X) + Math.Abs(c.Y - depart.Y);  // Manhattan PM
    if (dEstimee > pmMax) continue;

    var chemin = Pathfinder.Trouver(carte, depart, c, interdites, combat: true);
    if (chemin == null || chemin.Count - 1 > pmMax) continue;

    double score = ScoreCelluleMode(d, mode, porteeMax, distPref);
    if (score < meilleurScore || (score == meilleurScore && nbPas < meilleurNbPasTie))
    {
        meilleurScore = score; meilleurNbPasTie = nbPas;
        meilleureCible = c; meilleurChemin = chemin; meilleureDist = d;
    }
}
```

### 4.2 Score selon mode

```csharp
private static double ScoreCelluleMode(int distEnnemi, ModeCombat mode, int porteeMax, int distancePreferee)
    => mode switch
    {
        Agressif  => Math.Max(0, distEnnemi - 1),                 // CAC parfait → 0
        Eloigne   => Math.Max(0, porteeMax - distEnnemi),         // dist=porteeMax → 0
        Fuyard    => Math.Max(0, porteeMax - distEnnemi),         // idem Eloigne (fuite active gérée ailleurs)
        Equilibre => Math.Abs(distEnnemi - distancePreferee),     // tient DistancePreferee
        _ => 0
    };
```

### 4.3 Envoi et confirmation event-based

```csharp
var paquetDep = Pathfinder.PaquetDeplacement(chemin);              // "GA001afybfNbf2dge"
await _session.EnvoyerAuServeurAsync(paquetDep);                    // ATOMIQUE via _verrouCs

// ADR-002 : attendre le broadcast serveur GA;0/1;<monId>;<chemin>
int timeoutMs = Math.Max(2500, nbPasMove * 450 + 1000);
var resultat = await PipelineDeplacementCombat
    .AttendreMouvementOuTimeoutAsync(_etat.Combat, idMoi, cellArrivee, timeoutMs, default);
```

L'event `Combat.MouvementBotConfirme` est déclenché par
`TrameJeu.OnActionJeu` (`Commun\Frames\TrameJeu.cs:540`) quand le serveur
broadcast un `GA0|...|<monId>|<chemin>` reflétant mon déplacement.

3 résultats possibles :
- **Confirme** : cell d'arrivée = cellArrivee envoyée.
- **ConfirmePartiel** : cell d'arrivée ≠ (serveur a tronqué). Recalcule
  distance et vérifie si cast encore possible.
- **TimeoutSilencieux** : aucun broadcast en `timeoutMs`. Soit cast aveugle
  (flag `ModeDeplacementOptimisteSecours`), soit `Gt` direct.

### 4.4 PAS de GKK0 après GA001 combat

Cf. dyshay `Movimiento.get_Mover_Celda_Pelea` : envoie GA001 SEUL. C'est
`MapFrame` qui répond GKK<n> en réaction au broadcast serveur GA;0/1 — pour
les déplacements **overworld**. En combat, aucun GKK requis.

---

## 5. Points bloquants actuels et causes racines

### Bug A — Bot figé tour à tour (cipher desync apparent)
**Cause racine** (ADR-002 V2) : pas du cipher, mais le serveur 1.29 traite les
actions séquentiellement. Envoyer GA300 alors qu'un GA001 ouvert n'a pas reçu
sa séquence GAS/GAF complète → serveur silent-drop le GA001 mais accepte
GA300 (cas asymétrique observé log 20:35:31).
**Fix appliqué** : `PipelineDeplacementCombat` event-based, plus de cast
aveugle sauf si `ModeDeplacementOptimisteSecours = true`.

### Bug B — IA cible un cadavre / mob hors combat
**Cause racine** : `Combat.Ennemis` peuplé par 2 sources (GTM Abrak +
`PeuplerCombatDepuisCarte` map mobs). Au combat, des mobs map fantômes
restaient en cible.
**Fix appliqué** : à chaque GTM reçu, on purge les IDs absents du paquet.
Filtre supplémentaire dans IA : `e.PV > 0 && e.PVMax > 0`.

### Bug C — Portée niveau 1 utilisée pour Ronce niv 5
**Cause racine** : JSON `spells_stats_hystoria.json` stocke 1 entrée par sort
(niv 1). Or Ronce niv 5 = portée 1-8 (pas 1-6).
**Fix appliqué** : chargement XML `hechizos_dyshay.xml` avec `StatsParNiveau[niv]`,
toujours résolu via `sort.Stats(niveauAppris)`.

### Bug D — Bot bouge en diagonale, serveur rejette
**Cause racine** : `Pathfinder.Trouver` en mode default 8-dir → cells diagonales
encodées dans GA001 → serveur 1.29 combat n'accepte que 4-dir orthogonal.
**Fix appliqué** : `Pathfinder.Trouver(..., combat: true)` force 4-dir (cf.
dyshay `PeleasPathfinder.get_Celdas_Adyecentes`).

### Bug E — Pas de repositionnement post-cast
**Cause racine** : `JouerTourCombatAsync` envoie `Gt` direct après cast. Pas
d'équivalent à `FightExtensions.get_Fin_Turno` de dyshay.
**Statut** : **NON corrigé**. C'est le sujet de la refonte nuit (Phase 6).

### Bug F — Multi-cast manquant
**Cause racine** : après cast réussi, on envoie Gt même si PA restants ≥
coût d'un autre sort. `NombreParTour > 1` jamais exploité.
**Statut** : **NON corrigé** (Phase 4 refonte).

### Bug G — UI : CmbSort vide à l'ouverture
**Cause racine** : `Personnage.SortsAppris` peuplé via écriture directe Dict.
L'event `SortsChanges` n'était pas déclenché.
**Fix appliqué** (`TrameJeu:58`) : passer par `AjouterOuMajSort(id, niveau)`
qui invoque l'event.

### Bug H — RadioButton mode combat invisible (Material Design)
**Cause racine** : Material Design surchargeait le template par défaut.
**Fix appliqué** : `ControlTemplate` explicite dans `VueCombat.xaml:75-109`
avec cercle bordure + disque intérieur.

### Bug I — Cooldown sort multi-tours pas géré
**Cause racine** : `RegleSort.TousLesNTours` est un condition d'IDÉE mais pas
un vrai cooldown serveur (intervalo). Pas de stockage `hechizos_intervalo`.
**Statut** : **NON corrigé** (Phase 4).

### Bug J — Pas d'utilisation de potion/pain
**Cause racine** : `ActionCombat.UtiliserObjet(idObjet)` existe mais
`DecideurCombat.TrouverSoin` retourne null. Pas de scan inventaire pour
matcher `cfg.ConsommableSoinIdTemplate`.
**Statut** : **NON corrigé** (Phase 9).

---

## 6. Comparaison avec SynFus / dyshay

| Aspect | dyshay/SynFus | Luffy-bot actuel | Verdict |
|--------|---------------|------------------|---------|
| Architecture tour | Event-driven (recurse sur events `hechizo_lanzado`/`movimiento`) | Linéaire dans `JouerTourCombatAsync` 260 l. | Refonte machine d'état nécessaire |
| Source vérité sorts | `hechizos_dyshay.xml` (281×6 niveaux) | Idem (porté direct) | ✓ |
| Filtre cible vivante | `esta_vivo` flag | `!EstMort && PV>0 && PVMax>0` | ✓ (plus strict) |
| Heuristique cible | Low-HP non-invoc dans range, fallback any | OrderBy distance only | ✗ À étendre |
| LOS Bresenham | `get_Linea_Obstruida` 160 l. | `LigneVisuelle.EstObstruee` | ✓ |
| Pathfinder combat | `PeleasPathfinder.get_Celdas_Accesibles` BFS 4-dir | `Pathfinder.Trouver(..., combat:true)` A* 4-dir | ✓ |
| Encoding GA001 | `PathFinderUtil.get_Pathfinding_Limpio` | `Pathfinder.PaquetDeplacement` | ✓ |
| Mode placement init | INMOVIL/CERCA/LEJOS via `get_Celda_Mas_Cercana_O_Lejana` | `PositionnementCombat` enum + handler placement | ✓ |
| Compteur lancers par tour | `total_hechizos_lanzados` | `CompteursRegleParTour` | ✓ |
| Compteur par cible | `total_hechizos_lanzados_en_celda` | `RegleSort.NombreParCible` stocké mais **non lu** | ✗ |
| Cooldown multi-tours | `hechizos_intervalo` reset chaque turn | **manquant** | ✗ |
| Codes échec cast | enum `FallosLanzandoHechizo` 12 valeurs | `continue` silent | ✗ Refactor |
| Repositionnement fin tour | `get_Fin_Turno` → `get_Mover` | **manquant** | ✗ Phase 6 |
| Sum dist multi-ennemis | `Get_Total_Distancia_Enemigo` | Distance 1-ennemi only | ✗ Phase 5 |
| Capture âme | sort 413 spécial | Hors scope | OK |
| Multi-cast par tour | recurse sur `hechizo_lanzado` event | **Cast 1× puis Gt** | ✗ Phase 4 |
| Soin auto consommable | `iniciar_regeneracion`/`detener_regeneracion` PV% | `ConsommableSoinIdTemplate` stocké mais inutilisé | ✗ Phase 9 |
| UI form combat | WinForms DataGridView + form Opciones | WPF Material Design 22 conditions bindées | ✓ (mieux) |

---

## 7. Mapping ligne-à-ligne notre code → dyshay

### 7.1 Boucle tour combat

| Notre code | Équivalent dyshay | Fichier dyshay |
|------------|-------------------|----------------|
| `TrameJeu.JouerTourCombatAsync` ll. 884-1144 | `FightExtensions.get_Pelea_Turno_iniciado` + `get_Procesar_hechizo` | `Otros/Peleas/FightExtensions.cs:56-120` |
| `TrameJeu.ExecuterRegleAsync` ll. 1313-1339 | `SpellsManager.get_Lanzar_Hechizo_Simple` | `Otros/Peleas/SpellsManager.cs:58-147` |
| `TrameJeu.TrouverApprocheCombat` ll. 1186-1277 | `SpellsManager.get_Mover_Lanzar_hechizo_Simple` | `Otros/Peleas/SpellsManager.cs:149-179` |
| (manquant) | `FightExtensions.get_Fin_Turno` | `Otros/Peleas/FightExtensions.cs:178-201` |
| (manquant) | `FightExtensions.get_Mover` | `Otros/Peleas/FightExtensions.cs:203-236` |
| `MoteurReglesCombat.Evaluer` 167 l. | `SpellsManager.manejador_Hechizos` + `Fight.get_Puede_Lanzar_hechizo` | `Otros/Peleas/SpellsManager.cs:37-56` + `Otros/Peleas/Fight.cs:334-388` |

### 7.2 Pathfinding et état

| Notre code | Équivalent dyshay |
|------------|-------------------|
| `Pathfinder.Trouver(carte, dep, arr, interdites, combat:true)` | `PeleasPathfinder.get_Celdas_Accesibles(...)` + reconstruct chemin |
| `Pathfinder.PaquetDeplacement(chemin)` | `PathFinderUtil.get_Pathfinding_Limpio(chemin)` |
| `LigneVisuelle.EstObstruee(carte, ma, cible, occupees)` | `Fight.get_Linea_Obstruida(mapa, ci, cd, occupees)` |
| `Combat.Allies` / `Combat.Ennemis` | `Fight.get_Aliados` / `Fight.get_Enemigos` |
| `Combattant.EstMort` / `EstInvocation` | `Luchadores.esta_vivo` / `id_invocador` |
| `Combat.CompteursRegleParTour` | `Fight.total_hechizos_lanzados` |
| (manquant cooldown) | `Fight.hechizos_intervalo` |
| (manquant par cible) | `Fight.total_hechizos_lanzados_en_celda` |

### 7.3 UI WPF

| Notre code | Équivalent dyshay/SynFus |
|------------|--------------------------|
| `VueCombat.xaml` panneau ÉTAT COMBAT (ListeCombattants) | (manquant en dyshay, présent dans SynFus binary) |
| `VueCombat.xaml` SORTS CONFIGURES rotation + reorder | DataGridView dans `Forms/Opciones.cs` |
| `VueCombat.xaml` AJOUTER UN SORT (ComboBox + 4 inputs) | Form modal "Añadir hechizo" |
| `VueCombat.xaml` PanelConditions 22 conditions | Form Conditions SynFus (5 GroupBox) |
| `VueCombat.xaml` MODE DE COMBAT 4 RadioButton | `ComboBox Tactica` dyshay (3 valeurs étendu en 4) |
| `VueCombat.xaml` SldDistancePref/SldSeuilFuite/SldDelaiActions | `NumericUpDown` dyshay |
| `VueCombat.xaml` SORTS APPRIS WrapPanel | ListBox dyshay |

---

## 8. État runtime résumé

À l'ouverture d'un combat (`MessageDebutCombat`) :
1. `Combat.Demarrer()` → `Etat = Placement`.
2. `Combat.PassageEnCombat()` → `Etat = EnCours`.
3. `PeuplerCombatDepuisCarte()` peuple Ennemis avec mobs map.
4. Réception GTM Abrak → resync `Combat.Allies`/`Ennemis` (purge fantômes,
   ajoute combattants réels avec PV/PA/PM/cell).
5. Réception GTS Abrak `(EstTour=true, idCombattant == moi)` → `NouveauTour`
   reset `CompteursRegleParTour` → `JouerTourCombatAsync`.
6. Délai humanisé, filtre cibles, moteur règles, fallback legacy, déplacement
   si nécessaire, cast, GKK0, Gt.
7. Serveur réplique `GTF<id>` (turn fini) puis `GTR<idsuivant>`.
8. Tour suivant : on attend le prochain GTS<moi>.
9. Fin combat (`MessageFinCombat`) → `Combat.Reinitialiser`, retour overworld.

---

## 9. Synthèse — état actuel vs cible refonte

Notre IA combat actuelle :
- **Fonctionne** en mode mono-cast basique (1 sort par tour) + déplacement
  si hors portée + mode passif global.
- **Stable** sur le réseau (cipher OK, pipeline event-based pour déplacement).
- **22 conditions** SynFus bindées dans l'UI WPF, parsées et appliquées par
  `MoteurReglesCombat`.
- **Limites** : pas de multi-cast, pas de repositionnement post-cast, pas de
  soin auto, pas de cooldown multi-tours, pas d'invocation, pas de capture âme.

Cible refonte (10 phases, cf. doc 4) :
- Machine d'état event-driven (au lieu de méthode linéaire 260 l.).
- `EndTurnRepositioning` qui port `get_Fin_Turno` + `get_Mover`.
- Codes échec explicites (enum + log).
- Cooldown multi-tours.
- Heuristique cible low-HP non-invoc.
- Multi-cast.
- Consommable de soin auto.
- Invocations Sadida (La Folle, La Bloqueuse).

Tout est dans le plan refonte doc 4.
