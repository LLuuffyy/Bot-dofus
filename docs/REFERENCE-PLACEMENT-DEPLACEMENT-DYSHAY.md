# Référence dyshay — Placement initial & déplacement combat

Source : `.claude/worktrees/vigorous-murdock/dyshay-source/` (dyshay/Bot-Dofus-Retro,
le repo d'Alvaro Prendes — base de SynFus).

Comparaison ligne à ligne avec notre code C# .NET 8.

---

## §1 — Placement initial dyshay

### 1.1 Réception du paquet serveur `GP` (cells de placement disponibles)

Fichier : `Comun/Frames/Juego/FightFrame.cs`, lignes 24-45.

```csharp
[PaqueteAtributo("GP")]
public void get_Combate_Celdas_Posicion(TcpClient cliente, string paquete)
{
    Account cuenta = cliente.account;
    Map mapa = cuenta.game.map;
    string[] _loc3 = paquete.Substring(2).Split('|');

    for (int a = 0; a < _loc3[0].Length; a += 2)
        cuenta.game.fight.celdas_preparacion.Add(
            mapa.GetCellFromId(
                (short)((Hash.get_Hash(_loc3[0][a]) << 6) + Hash.get_Hash(_loc3[0][a + 1]))));
    // ...
}
```

**À noter :** `paquete.Substring(2)` strippe le préfixe `GP`. Le premier bloc avant `|`
contient les cellules de l'équipe locale (`PositionsEquipe1` ou `PositionsEquipe2`
selon la team du perso — le serveur met TOUJOURS la team du PJ en premier).

### 1.2 Choix de la cell + envoi `Gp<cell>` (re-positionnement)

Fichier : `Comun/Frames/Juego/MapFrame.cs`, lignes 106-126. Déclenché par le paquet
serveur `GM` (positions des entités) quand on est en phase combat (`AccountState == FIGHTING`).

```csharp
if (cuenta.game.character.id == id
    && cuenta.fightExtension.configuracion.posicionamiento != PosicionamientoInicioPelea.INMOVIL)
{
    await Task.Delay(500);

    /** la posicion es aleatoria pero el paquete GP siempre aparecera primero
        el team donde esta el pj **/
    short celda_posicion = pelea.get_Celda_Mas_Cercana_O_Lejana(
        cuenta.fightExtension.configuracion.posicionamiento == PosicionamientoInicioPelea.CERCA_DE_ENEMIGOS,
        pelea.celdas_preparacion);
    await Task.Delay(500);

    if (celda_posicion != celda.cellId)
        cuenta.connexion.SendPacket("Gp" + celda_posicion, true);
    else
    {
        if(cuenta.isGroupLeader == true)
            await Task.Delay(3800);
        else
            await Task.Delay(1800);
        cuenta.connexion.SendPacket("GR1");
    }
}
else if (cuenta.game.character.id == id)
{
    if(cuenta.isGroupLeader == true)
        await Task.Delay(3800);
    else
        await Task.Delay(1800);
    cuenta.connexion.SendPacket("GR1"); // boton listo
}
```

### 1.3 Algo `get_Celda_Mas_Cercana_O_Lejana` (Fight.cs ligne 279-296)

```csharp
public short get_Celda_Mas_Cercana_O_Lejana(bool cercana, IEnumerable<Cell> celdas_posibles)
{
    short celda_id = -1;
    int distancia_total = -1;

    foreach (Cell celda_actual in celdas_posibles)
    {
        int temporal_total_distancia = get_Distancia_Desde_Enemigo(celda_actual);

        if (celda_id == -1
            || ((cercana && temporal_total_distancia < distancia_total)
             || (!cercana && temporal_total_distancia > distancia_total)))
        {
            celda_id = celda_actual.cellId;
            distancia_total = temporal_total_distancia;
        }
    }
    return celda_id;
}

public int get_Distancia_Desde_Enemigo(Cell celda_actual)
    => get_Enemigos.Sum(e => celda_actual.GetDistanceBetweenCells(e.celda) - 1);
```

**Algo :** somme des distances Manhattan (cf. §3.1) de la cell candidate à TOUS les
ennemis. `cercana=true` → minimise la somme → on se colle. `cercana=false` →
maximise → on s'éloigne au max.

### 1.4 Enum `PosicionamientoInicioPelea`

```csharp
public enum PosicionamientoInicioPelea
{
    LEJOS_DE_ENEMIGOS = 0,
    CERCA_DE_ENEMIGOS = 1,
    INMOVIL = 2
}
```

Mapping vers `Tactica` (FightExtensions.cs) :
- `AGRESIVA` → `CERCA_DE_ENEMIGOS` (par convention config)
- `PASIVA` → `INMOVIL`
- `FUGITIVA` → `LEJOS_DE_ENEMIGOS`

### 1.5 Séquence chronologique complète dyshay

```
Serveur → GP<hash>|<hash>|...      (cells placement disponibles)
Serveur → GM<entités+positions>    (qui est où, dont moi)
   ↓ delay 500 ms
Client  → Gp<cellChoisie>          (SI cellChoisie != ma cell actuelle)
Serveur → GIC<id>;<cell>|...       (confirme nouveau placement)
   ↓ delay 1600 ms (1400 si pas leader)
Client  → GR1                      (prêt)
```

---

## §2 — Déplacement combat dyshay

### 2.1 `get_Mover_Celda_Pelea` (Movimiento.cs lignes 135-150)

```csharp
public async Task get_Mover_Celda_Pelea(KeyValuePair<short, MovimientoNodo>? nodo)
{
    if (!cuenta.IsFighting())
        return;

    if (nodo == null || nodo.Value.Value.camino.celdas_accesibles.Count == 0)
        return;

    if (nodo.Value.Key == cuenta.game.fight.jugador_luchador.celda.cellId)
        return;

    // ⚠ ON INSÈRE NOTRE CELL DE DÉPART EN TÊTE DU CHEMIN
    nodo.Value.Value.camino.celdas_accesibles.Insert(0,
        cuenta.game.fight.jugador_luchador.celda.cellId);
    List<Cell> lista_celdas = nodo.Value.Value.camino.celdas_accesibles
        .Select(c => mapa.GetCellFromId(c)).ToList();
    await cuenta.connexion.SendPacketAsync(
        "GA001" + PathFinderUtil.get_Pathfinding_Limpio(lista_celdas), false);
    personaje.evento_Personaje_Pathfinding_Minimapa(lista_celdas);
}
```

**Point capital :** dyshay envoie `GA001` SANS GKK0 derrière. Le serveur valide via
ses propres événements `GA;0/1;<id>` (que dyshay traite dans `MapFrame.GA`).
**Pas de `GKK0` après un déplacement combat côté dyshay.**

### 2.2 Sélection de la cell d'arrivée (FightExtensions.cs lignes 203-236)

```csharp
public async Task get_Mover(bool cercano, Luchadores enemigo)
{
    KeyValuePair<short, MovimientoNodo>? nodo = null;
    Map mapa = cuenta.game.map;
    int distancia = -1;

    int distancia_total = Get_Total_Distancia_Enemigo(pelea.jugador_luchador.celda);

    foreach (KeyValuePair<short, MovimientoNodo> kvp
        in PeleasPathfinder.get_Celdas_Accesibles(pelea, mapa, pelea.jugador_luchador.celda))
    {
        if (!kvp.Value.alcanzable)
            continue;

        int temporal_distancia = Get_Total_Distancia_Enemigo(mapa.GetCellFromId(kvp.Key));

        if ((cercano && temporal_distancia <= distancia_total)
         || (!cercano && temporal_distancia >= distancia_total))
        {
            if (cercano)
            {
                nodo = kvp;
                distancia_total = temporal_distancia;
            }
            else if (kvp.Value.camino.celdas_accesibles.Count >= distancia)
            {
                nodo = kvp;
                distancia_total = temporal_distancia;
                distancia = kvp.Value.camino.celdas_accesibles.Count;
            }
        }
    }

    if (nodo != null)
        await cuenta.game.manager.movimientos.get_Mover_Celda_Pelea(nodo);
}

public int Get_Total_Distancia_Enemigo(Cell celda)
    => cuenta.game.fight.get_Enemigos.Sum(e => e.celda.GetDistanceBetweenCells(celda) - 1);
```

**Algo :** dyshay énumère TOUTES les cellules atteignables dans PM (via
`PeleasPathfinder.get_Celdas_Accesibles`), calcule la somme distances Manhattan
aux ennemis, prend la plus proche (cercano) ou la plus éloignée. Le déplacement
ne sert qu'à se positionner par rapport aux ennemis, **JAMAIS lié à la portée
d'un sort**.

### 2.3 Comparaison avec notre `JouerTourCombatAsync`

| Aspect | Dyshay | Notre code (TrameJeu.cs:881-1141) |
|---|---|---|
| Trigger | `get_Pelea_Turno_iniciado` → cast d'abord, déplacement APRÈS dans `get_Fin_Turno` | `OnTourCombatActuel` → cast d'abord (règles → fallback), déplacement INTERCALÉ si sort hors portée |
| Délai humain initial | `Task.Delay(400)` (fixe) | `Random(1400, 2100)` ms |
| Ordre tour | (1) cast tous les sorts → (2) si tactique AGRESIVA && pas CAC → bouger vers ennemi → (3) `Gt` | (1) chercher sort en portée → (2) si rien : bouger ET caster → (3) `Gt` |
| Critère cell cible | somme dist Manhattan aux ennemis, min ou max | énumère cells dont **dist Chebyshev** à l'ennemi le plus proche ∈ [PorteeMin, PorteeMax] sort visé, score = `ScoreCelluleMode` |
| Pathfinding | `PeleasPathfinder.get_Celdas_Accesibles` (BFS 4-dir, PM-limited) | `Pathfinder.Trouver` (A* **8-directions**, PM-limited via filtrage post-hoc) |
| Voisinage | **4 directions ortho seulement** (PeleasPathfinder.get_Celdas_Adyecentes) | **8 directions** (Pathfinder.VoisinsAdjacents) |
| Distance entité↔entité | `Math.Abs(dx) + Math.Abs(dy)` (**Manhattan**) | `Math.Max(Math.Abs(dx), Math.Abs(dy))` (**Chebyshev**) |
| Ack GA001 post-envoi | aucun (serveur émet GA;0/1, traité passivement) | `GKK0` envoyé après confirmation/timeout |
| `Gt` final | `cuenta.connexion.SendPacket("Gt")` | `EnvoyerAuServeurAsync("Gt")` |

---

## §3 — Encodage GA001 (déplacement)

### 3.1 Distance & coordonnées Cell.cs

```csharp
public int GetDistanceBetweenCells(Cell prmDestinationCell)
    => Math.Abs(x - prmDestinationCell.x) + Math.Abs(y - prmDestinationCell.y);

public bool AreCellsOnLine(Cell prmDestinationCell)
    => x == prmDestinationCell.x || y == prmDestinationCell.y;

public char GetCharDirection(Cell prmCell)
{
    if (x == prmCell.x)
        return prmCell.y < y ? (char)(3 + 'a') : (char)(7 + 'a');
    else if (y == prmCell.y)
        return prmCell.x < x ? (char)(1 + 'a') : (char)(5 + 'a');
    else if (x > prmCell.x)
        return y > prmCell.y ? (char)(2 + 'a') : (char)(0 + 'a');
    else if (x < prmCell.x)
        return y < prmCell.y ? (char)(6 + 'a') : (char)(4 + 'a');
    throw new Exception("Error direct non trouvée");
}
```

Coordonnées x/y (Cell ctor lignes 56-61) :
```csharp
byte mapWidth = prmMap.mapWidth;
int loc5 = cellId / ((mapWidth * 2) - 1);
int loc6 = cellId - (loc5 * ((mapWidth * 2) - 1));
int loc7 = loc6 % mapWidth;
y = loc5 - loc7;
x = (cellId - ((mapWidth - 1) * y)) / mapWidth;
```

**Identique à notre `Cellule.CalculerCoordonnees`** (Cellule.cs:140-148).

### 3.2 `get_Pathfinding_Limpio` (PathFinderUtil.cs lignes 67-95)

```csharp
public static string get_Pathfinding_Limpio(List<Cell> camino)
{
    Cell celda_destino = camino.Last();

    if (camino.Count <= 2)
        return celda_destino.GetCharDirection(camino.First())
             + Hash.Get_Cell_Char(celda_destino.cellId);

    StringBuilder pathfinder = new StringBuilder();
    char direccion_anterior = camino[1].GetCharDirection(camino.First());
    char direccion_actual;

    for (int i = 2; i < camino.Count; i++)
    {
        Cell celda_actual = camino[i];
        Cell celda_anterior = camino[i - 1];
        direccion_actual = celda_actual.GetCharDirection(celda_anterior);

        if (direccion_anterior != direccion_actual)
        {
            pathfinder.Append(direccion_anterior);
            pathfinder.Append(Hash.Get_Cell_Char(celda_anterior.cellId));
            direccion_anterior = direccion_actual;
        }
    }

    pathfinder.Append(direccion_anterior);
    pathfinder.Append(Hash.Get_Cell_Char(celda_destino.cellId));
    return pathfinder.ToString();
}

// Hash.cs ligne 71
public static string Get_Cell_Char(short cellID)
    => caracteres_array[cellID / 64] + "" + caracteres_array[cellID % 64];
```

Alphabet (Hash.cs lignes 8-14) :
```csharp
char[] caracteres_array = {
    'a','b',...,'z',
    'A','B',...,'Z',
    '0','1',...,'9',
    '-','_'
};
```

### 3.3 Diff avec notre `Pathfinder.EncoderChemin`

Notre code (Pathfinder.cs:135-164) :
```csharp
public static string EncoderChemin(IReadOnlyList<Cellule> chemin)
{
    if (chemin.Count <= 1) return string.Empty;
    var destination = chemin[chemin.Count - 1];

    if (chemin.Count == 2)
    {
        char dir = chemin[0].DirectionVers(chemin[1]);    // ← (dyshay: chemin[1].GetCharDirection(chemin[0]))
        return dir + HashCarte.EncoderCellule(destination.Identifiant);
    }

    var sb = new StringBuilder();
    char directionPrecedente = chemin[0].DirectionVers(chemin[1]); // ← (dyshay: chemin[1].GetCharDirection(chemin[0]))

    for (int i = 2; i < chemin.Count; i++)
    {
        char direction = chemin[i - 1].DirectionVers(chemin[i]);   // ← (dyshay: chemin[i].GetCharDirection(chemin[i-1]))
        if (direction != directionPrecedente)
        {
            sb.Append(directionPrecedente);
            sb.Append(HashCarte.EncoderCellule(chemin[i - 1].Identifiant));
            directionPrecedente = direction;
        }
    }
    sb.Append(directionPrecedente);
    sb.Append(HashCarte.EncoderCellule(destination.Identifiant));
    return sb.ToString();
}
```

**Vérification équivalence dyshay → nous** :

dyshay : `next.GetCharDirection(prev)` → `this=next, prmCell=prev`
→ `dx = next.x - prev.x`, branche sur signe de `(next.x ?= prev.x)`

nous : `prev.DirectionVers(next)` → `this=prev, voisine=next`
→ `dx = Math.Sign(next.X - prev.X)` (idem)

Mapping bit-à-bit reproduit dans Cellule.cs:114-125 — **identique au niveau code
machine**, vérifié table par table sur les 8 directions. Alphabet HashCarte
identique caractère par caractère.

**Conclusion §3 :** l'encodage GA001 produit la même chaîne. Pas de différence
combat vs overworld. **L'encodage n'est pas la cause du bug.**

---

## §4 — Paquet placement initial : `Gp<cell>` confirmé

### 4.1 Format exact

```
Gp<cellId>     (minuscule p, cellId en décimal entier, PAS de hash 2-char)
```

Référence : `MapFrame.cs:115` — `cuenta.connexion.SendPacket("Gp" + celda_posicion, true);`
où `celda_posicion` est un `short` (donc concaténé en décimal).

**Différence cruciale avec GA001 :**
- `GA001` : hash 2-char base64 par cell (alphabet `a-zA-Z0-9-_`)
- `Gp<cell>` : décimal brut (`Gp299`, `Gp303`, etc.)
- `GA300<sort>;<cell>` : décimal brut aussi (cast sort)

### 4.2 Confirmation par notre CLAUDE.md (capture user 16:21-16:24)

```
Phase Placement | Gp<cell> | Gp299
Phase Prêt      | GR<1/0>  | GR1 / GR0
```

→ **Le paquet est CONFIRMÉ** `Gp<cellule décimale>`.

### 4.3 Validation par le serveur

Si la cell choisie n'est pas dans la liste reçue via `GP|<hashes>|<team>`, le
serveur peut soit ignorer (le client reste sur son ancienne cell), soit envoyer
`GICE` (erreur changement position). dyshay traite GICE en envoyant `GR1` quand
même après délai (FightFrame.cs:48-62).

---

## §5 — Différences identifiées entre notre code et dyshay

### 5.1 BUG MAJEUR : pas de re-positionnement initial

**Notre code** (`Divers/ContexteCompte.cs:130-162`) — fonction `OnEtatCombatChange` :
```csharp
if (etat == EtatCombat.Placement)
{
    if (ModePassif) return;
    await Task.Delay(1400);
    if (EtatJeu.Combat.Etat != EtatCombat.Placement) return;
    await Api.EnvoyerPaquetBrutAsync("GR1");
    Journaliseur.Info("[AUTO-COMBAT] Prêt envoyé (GR1).");
}
```

→ On envoie `GR1` **DIRECTEMENT** sur la cell par défaut allouée par le serveur,
sans regarder ni les cells disponibles (`Combat.PositionsEquipe1/2`) ni la
tactique du `ConfigCombat`.

**Dyshay** (`MapFrame.cs:106-126`) : calcule la meilleure cell avec
`get_Celda_Mas_Cercana_O_Lejana(cercana?, celdas_preparacion)` selon
`PosicionamientoInicioPelea`, envoie `Gp<cell>` si différente, PUIS `GR1` après
le `GIC` de confirmation (ou direct si déjà bien placé).

### 5.2 BUG MAJEUR : voisinage 8 vs 4 directions en combat

**Notre Pathfinder.cs:198-211 (VoisinsAdjacents)** :
```csharp
var deltas = new (int dx, int dy)[] {
    (1,0), (-1,0), (0,1), (0,-1),
    (1,1), (1,-1), (-1,1), (-1,-1),    // ← 4 diagonales aussi
};
```

→ Notre A* utilise **8 directions** en combat comme en overworld.

**Dyshay PeleasPathfinder.cs:129-148 (get_Celdas_Adyecentes)** :
```csharp
// pelea no utiliza diagonales
Cell celda_derecha   = mapa_celdas.FirstOrDefault(n => n.x == nodo.x + 1 && n.y == nodo.y);
Cell celda_izquierda = mapa_celdas.FirstOrDefault(n => n.x == nodo.x - 1 && n.y == nodo.y);
Cell celda_inferior  = mapa_celdas.FirstOrDefault(n => n.x == nodo.x     && n.y == nodo.y + 1);
Cell celda_superior  = mapa_celdas.FirstOrDefault(n => n.x == nodo.x     && n.y == nodo.y - 1);
```

→ Le pathfinder COMBAT de dyshay est **4-directions strictes** (commentaire
`// pelea no utiliza diagonales`). En combat Dofus 1.29, on ne peut PAS bouger
en diagonale. Notre code produit des chemins illégaux → le serveur rejette
silencieusement → timeout 2.5s + mode secours optimiste → cast aveugle sur la
mauvaise cell → désync potentielle.

### 5.3 BUG : Distance Manhattan vs Chebyshev pour la portée

**Dyshay `Cell.GetDistanceBetweenCells`** = `|dx| + |dy|` (**Manhattan**).
C'est la métrique utilisée pour TOUT (portée sorts, distance ennemi-cell, etc.).

**Notre `DistanceDofus`** (TrameJeu.cs:1158-1164) = `Math.Max(|dx|, |dy|)`
(**Chebyshev**). On a même mis en commentaire : « grille iso Dofus = Chebyshev »
— c'est **FAUX** selon dyshay. Dofus Retro 1.29 mesure les portées en Manhattan,
pas en Chebyshev (cohérent avec « pas de diagonales en combat »).

Conséquence : on rejette des sorts qu'on pourrait lancer (notre Chebyshev = 5
quand dyshay calcule Manhattan = 7 → on croit en portée 1-6 alors qu'on est
en portée 1-8 réelle), et inversement.

### 5.4 BUG : GKK0 inutile (et potentiellement bloquant) après GA001 combat

**Notre code** (TrameJeu.cs:1091-1094) :
```csharp
Journaliseur.Info("[ACTION-MV] Envoi GKK0 (ack déplacement)");
await Task.Delay(System.Random.Shared.Next(150, 300));
await _session.EnvoyerAuServeurAsync("GKK0");
```

**Dyshay** : aucun `GKK0` après `GA001` combat (cf. `Movimiento.get_Mover_Celda_Pelea`).
Le `GKK<n>` n'est envoyé que par `MapFrame.GAF` quand le serveur émet `GAF<id>`
(fin d'action) — donc en RÉACTION au serveur, pas systématiquement.

→ Notre `GKK0` proactif après le déplacement n'est PAS faux en soi (capture
montre que le client réel le fait après GA300 cast), mais c'est superflu après
GA001. Plus inquiétant : dans le mode secours timeout, on envoie GKK0 alors que
le serveur a refusé silencieusement → désync compteur idx.

### 5.5 Insertion de la cell de départ dans le path GA001

**Dyshay** (Movimiento.cs:146) :
```csharp
nodo.Value.Value.camino.celdas_accesibles.Insert(0,
    cuenta.game.fight.jugador_luchador.celda.cellId);
List<Cell> lista_celdas = nodo.Value.Value.camino.celdas_accesibles
    .Select(c => mapa.GetCellFromId(c)).ToList();
```

→ dyshay insère explicitement la cell de DÉPART en tête. `get_Pathfinding_Limpio`
en a BESOIN car `camino[1].GetCharDirection(camino.First())` calcule la direction
du 1er pas depuis le départ.

**Notre Pathfinder.Reconstruire** (Pathfinder.cs:179-192) : inclut déjà `depart`
dans la liste retournée (`chemin.Add(depart); chemin.Reverse();`). Donc
`chemin[0]` est bien notre cell actuelle, c'est cohérent.

→ Pas un bug, vérifié OK.

---

## §6 — Recommandations actionables

### Fix #1 : Implémenter le re-positionnement initial avant GR1

Dans `ContexteCompte.OnEtatCombatChange(Placement)` ou via un nouveau handler
sur `Combat.PositionsChangees` :

1. Lire `combat.PositionsEquipe1` et `combat.PositionsEquipe2`
2. Déterminer ma team via `EquipePlacement` (ou via le 1er bloc — c'est TOUJOURS
   la team locale)
3. Calculer la meilleure cell selon `ConfigCombat.Positionnement` :
   - `PresEnnemis` → minimiser somme Manhattan vers ennemis (`combat.Ennemis`)
   - `LoinEnnemis` → maximiser
   - `PasDeDeplacement` → garder ma cell actuelle, envoyer GR1 direct
4. Si meilleureCell ≠ maCell : envoyer `Gp<meilleureCell>`, attendre `GIC` (handler
   serveur), PUIS envoyer `GR1`. Si égale : `GR1` direct après délai 1.5-1.8s.

Astuce : on n'a pas encore le snapshot des ennemis au moment du `GP` placement,
mais on a la liste des `MessageMouvementCarte` (entités sur la map AVANT le
combat) — utilisable comme proxy. Sinon attendre le 1er `GTM` (combattants).

### Fix #2 : Restreindre le pathfinding combat à 4 directions

Créer une variante `Pathfinder.TrouverCombat(...)` qui utilise un voisinage
4-orthogonal au lieu de 8. Ou ajouter un paramètre `bool combat = false` à
`Trouver(...)`. Dans `TrameJeu.TrouverApprocheCombat`, appeler la variante combat.

```csharp
private static readonly (int dx, int dy)[] DeltasOverworld = {
    (1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)
};
private static readonly (int dx, int dy)[] DeltasCombat = {
    (1,0),(-1,0),(0,1),(0,-1)
};
```

### Fix #3 : Distance Manhattan pour portée sorts en combat

Refondre `DistanceDofus` (et le calcul dans `TrouverApprocheCombat` ligne 1241)
pour utiliser `|dx| + |dy|` (Manhattan). Garder Chebyshev/euclidien comme
heuristique A* (rapidité), mais la métrique de PORTÉE est Manhattan strict —
cohérent avec voisinage 4-dir. À vérifier avec `LigneVisuelle` qui assume
peut-être Chebyshev.

### Fix #4 : Ne PAS envoyer GKK0 après GA001 combat (laisser le pipeline GAF)

Dans `JouerTourCombatAsync` ligne 1091-1093, supprimer l'envoi explicite de
GKK0 post-déplacement. Le `MapFrame.GAF` handler côté serveur enverra
implicitement le GKK quand le serveur acknowledge la fin de l'action (voir
dyshay MapFrame.cs:151-157).

### Fix #5 : Algo de cell-cible déplacement aligné dyshay

Dans `TrouverApprocheCombat`, plutôt qu'énumérer toutes les cells en portée du
sort et appliquer `ScoreCelluleMode`, faire comme dyshay :

1. Énumérer les cells atteignables en `PM` (via pathfinder combat 4-dir, BFS)
2. Pour chaque, calculer `Get_Total_Distancia_Enemigo` (somme Manhattan to all
   ennemis)
3. Selon tactique : prendre min (Agressif/PresEnnemis) ou max (Fugitif/LoinEnnemis)
4. **APRÈS** s'être déplacé, vérifier si un sort est utilisable depuis la cell
   d'arrivée — si non, ne pas casser ; juste passer le tour

→ Cela découple « se positionner » de « lancer un sort », comme dyshay
(`get_Fin_Turno` lignes 178-200 : on bouge à la fin du tour si on n'a pas pu
caster en CAC).

---

## Annexes

### A. Tactica enum dyshay
```csharp
public enum Tactica { AGRESIVA = 0, PASIVA = 1, FUGITIVA = 2 }
```

### B. Mapping Tactica → comportement (FightExtensions.get_Fin_Turno)
- **AGRESIVA** : si pas CAC ennemi → bouger vers ennemi le plus proche
- **FUGITIVA** : si CAC ennemi OU enemi dist < 8 → s'éloigner ; si ennemi
  dist > 12 → s'avancer (PM)
- **PASIVA** : aucun déplacement en fin de tour

### C. Distance encore vérifiée
- `Cell.GetDistanceBetweenCells` = Manhattan
- `Cell.AreCellsOnLine` = aligne x OU y égaux (Manhattan natif)
- `PeleasPathfinder.get_Celdas_Adyecentes` = 4 voisins orthogonaux uniquement

### D. Liste fichiers source dyshay analysés
- `Otros/Peleas/Fight.cs` (704 lignes)
- `Otros/Peleas/FightExtensions.cs` (259 lignes)
- `Otros/Peleas/Enums/PosicionamientoInicioPelea.cs`
- `Otros/Peleas/Enums/Tactica.cs`
- `Otros/Game/Manejadores/Movimientos/Movimiento.cs` (238 lignes)
- `Otros/Mapas/Movimiento/PathFinderUtil.cs` (98 lignes)
- `Otros/Mapas/Movimiento/Peleas/PeleasPathfinder.cs` (151 lignes)
- `Otros/Mapas/Cell.cs` (88 lignes)
- `Utilities/Crypto/Hash.cs` (92 lignes)
- `Comun/Frames/Juego/FightFrame.cs` (96 lignes — handlers GP/GIC/GICE/GTM)
- `Comun/Frames/Juego/MapFrame.cs` lignes 80-150 (handler GM + placement)
