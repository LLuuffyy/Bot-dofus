# Analyse dyshay/Bot-Dofus-Retro — Combat 1.29 (refonte nuit 2026-05-21)

> Doc de référence n°2 / 4. Source primaire C# pré-obfuscation, copyright
> `Alvaro Prendes 2019 — salesprendes.com` (le même qui a écrit SynFus).
> Chemin local : `.claude/worktrees/vigorous-murdock/dyshay-source/`.
> Tous les snippets ci-dessous sont **COPIÉS** verbatim du repo, puis traduits
> commentaire à commentaire en français pour notre projet.

---

## 1. `Otros/Peleas/SpellsManager.cs` — Lanzar / Mover / Reculer

### 1.1 `manejador_Hechizos` (entrée publique)

```csharp
public async Task<ResultadoLanzandoHechizo> manejador_Hechizos(HechizoPelea hechizo, bool capturer = false)
{
    if (hechizo.focus == HechizoFocus.CELDA_VACIA)
        return await lanzar_Hechizo_Celda_Vacia(hechizo);

    if (hechizo.metodo_lanzamiento == MetodoLanzamiento.AMBOS)
        return await get_Lanzar_Hechizo_Simple(hechizo, capturer);

    if (hechizo.metodo_lanzamiento == MetodoLanzamiento.ALEJADO && !cuenta.game.fight.esta_Cuerpo_A_Cuerpo_Con_Enemigo())
        return await get_Lanzar_Hechizo_Simple(hechizo);

    if (hechizo.metodo_lanzamiento == MetodoLanzamiento.CAC && cuenta.game.fight.esta_Cuerpo_A_Cuerpo_Con_Enemigo())
        return await get_Lanzar_Hechizo_Simple(hechizo);

    if (hechizo.metodo_lanzamiento == MetodoLanzamiento.CAC && !cuenta.game.fight.esta_Cuerpo_A_Cuerpo_Con_Enemigo())
        return await get_Mover_Lanzar_hechizo_Simple(hechizo, get_Objetivo_Mas_Cercano(hechizo));

    return ResultadoLanzandoHechizo.NO_LANZADO;
}
```

**Traduction française du flow** :
- Sort sur cellule vide (invocation Sadida par ex.) → branche dédiée.
- Sort polyvalent (CAC + Distance) → cast simple (le calcul de portée dans
  `get_Lanzar_Hechizo_Simple` décidera s'il faut bouger).
- Sort à distance uniquement, et je ne suis pas collé à l'ennemi → cast simple.
- Sort CAC uniquement, et je SUIS collé → cast simple.
- Sort CAC uniquement, mais je ne suis PAS collé → **force le déplacement
  d'abord**, puis cast (via `get_Mover_Lanzar_hechizo_Simple`).

### 1.2 `get_Lanzar_Hechizo_Simple` (cast direct, ou bouge si hors portée)

```csharp
private async Task<ResultadoLanzandoHechizo> get_Lanzar_Hechizo_Simple(HechizoPelea hechizo, bool capturer = false)
{
    if (pelea.get_Puede_Lanzar_hechizo(hechizo.id) != FallosLanzandoHechizo.NINGUNO)
        return ResultadoLanzandoHechizo.NO_LANZADO;

    // [bloc capture âme — sort 413 — non pertinent pour notre projet]

    Luchadores enemigo = get_Objetivo_Mas_Cercano(hechizo);
    if (enemigo != null)
    {
        FallosLanzandoHechizo resultado = pelea.get_Puede_Lanzar_hechizo(
            hechizo.id, pelea.jugador_luchador.celda, enemigo.celda, mapa);

        if (resultado == FallosLanzandoHechizo.NINGUNO)
        {
            await pelea.get_Lanzar_Hechizo(hechizo.id, enemigo.celda.cellId);
            return ResultadoLanzandoHechizo.LANZADO;
        }
        if (resultado == FallosLanzandoHechizo.NO_ESTA_EN_RANGO)
            return await get_Mover_Lanzar_hechizo_Simple(hechizo, enemigo);
    }
    else if (hechizo.focus == HechizoFocus.CELDA_VACIA)
        return await lanzar_Hechizo_Celda_Vacia(hechizo);

    return ResultadoLanzandoHechizo.NO_LANZADO;
}
```

**Trad** :
1. Pré-check global : PA dispos, cooldown, max lancers par tour, max invocations.
   Si KO → NO_LANZADO direct.
2. Trouve l'ennemi (selon Focus).
3. Check portée + LOS + occupations depuis ma case courante.
4. Si OK → `GA300<sort>;<cell>` via `Fight.get_Lanzar_Hechizo`.
5. Si **portée seulement** est KO → bascule sur `get_Mover_Lanzar_hechizo_Simple`.
6. Tout autre échec (LOS, ligne, cell vide non libre…) → NO_LANZADO (la règle
   est skipée, on passe à la suivante).

### 1.3 `get_Mover_Lanzar_hechizo_Simple` (DÉCISION CRITIQUE bouger+cast)

```csharp
private async Task<ResultadoLanzandoHechizo> get_Mover_Lanzar_hechizo_Simple(HechizoPelea hechizo_pelea, Luchadores enemigo)
{
    KeyValuePair<short, MovimientoNodo>? nodo = null;
    int pm_utilizados = 99;

    foreach (KeyValuePair<short, MovimientoNodo> movimiento in PeleasPathfinder.get_Celdas_Accesibles(pelea, mapa, pelea.jugador_luchador.celda))
    {
        if (!movimiento.Value.alcanzable) continue;

        if (hechizo_pelea.metodo_lanzamiento == MetodoLanzamiento.CAC
         && !pelea.esta_Cuerpo_A_Cuerpo_Con_Aliado(mapa.GetCellFromId(movimiento.Key)))
            continue;

        if (pelea.get_Puede_Lanzar_hechizo(hechizo_pelea.id, mapa.GetCellFromId(movimiento.Key), enemigo.celda, mapa) != FallosLanzandoHechizo.NINGUNO)
            continue;

        if (movimiento.Value.camino.celdas_accesibles.Count <= pm_utilizados)
        {
            nodo = movimiento;
            pm_utilizados = movimiento.Value.camino.celdas_accesibles.Count;
        }
    }

    if (nodo != null)
    {
        await cuenta.game.manager.movimientos.get_Mover_Celda_Pelea(nodo);
        return ResultadoLanzandoHechizo.MOVIDO;
    }
    return ResultadoLanzandoHechizo.NO_LANZADO;
}
```

**Trad** :
1. Énumère TOUTES les cellules accessibles via PM (BFS 4-dir issu de `PeleasPathfinder`).
2. Filtre celles où la cible reste atteignable (sort castable depuis la nouvelle cell).
3. **Garde celle qui consomme le MINIMUM de PM** (`<=`). Préserve les PM pour
   un éventuel cast suivant ou un repositionnement de fin de tour.
4. Envoie `GA001<chemin>` via `Movimiento.get_Mover_Celda_Pelea` (le cast suivra
   au callback `movimiento` event).

### 1.4 `get_Objetivo_Mas_Cercano` (sélection cible selon Focus)

```csharp
private Luchadores get_Objetivo_Mas_Cercano(HechizoPelea hechizo)
{
    Spell Spell = cuenta.game.character.get_Hechizo(hechizo.id);
    SpellStats SpellStats = Spell.get_Stats();
    int range = SpellStats.alcanze_maximo;

    if (hechizo.focus == HechizoFocus.ENCIMA)     return pelea.jugador_luchador;       // sort sur soi
    if (hechizo.focus == HechizoFocus.CELDA_VACIA) return null;

    return hechizo.focus == HechizoFocus.ENEMIGO
        ? pelea.get_Obtener_Enemigo_Mas_Cercano(range)
        : pelea.get_Obtener_Aliado_Mas_Cercano();
}
```

---

## 2. `Otros/Peleas/FightExtensions.cs` — boucle tour + `get_Fin_Turno` + `get_Mover`

### 2.1 `get_Fin_Turno` (positionnement avant pass turn)

```csharp
private async Task get_Fin_Turno()
{
    if (!pelea.esta_Cuerpo_A_Cuerpo_Con_Enemigo() && configuracion.tactica == Tactica.AGRESIVA)
        await get_Mover(true, pelea.get_Obtener_Enemigo_Mas_Cercano());
    else if (pelea.esta_Cuerpo_A_Cuerpo_Con_Enemigo() && configuracion.tactica == Tactica.FUGITIVA)
        await get_Mover(false, pelea.get_Obtener_Enemigo_Mas_Cercano());
    else if (pelea.is_proche_7() && configuracion.tactica == Tactica.FUGITIVA)
    {
        cuenta.Logger.LogInfo("Fight", $"Enemi prés de < 8 cases , on recule de {pelea.jugador_luchador.pm} PM");
        await get_Mover(false, pelea.get_Obtener_Enemigo_Mas_Cercano());
    }
    else if (pelea.is_loin_8() && configuracion.tactica == Tactica.FUGITIVA)
    {
        cuenta.Logger.LogInfo("Fight", $"Enemi loin de > 12 cases , on avance de {pelea.jugador_luchador.pm} PM");
        await get_Mover(true, pelea.get_Obtener_Enemigo_Mas_Cercano());
    }

    pelea.get_Turno_Acabado();
    var t = new Random().Next(200, 500);
    await Task.Delay(t);
    cuenta.connexion.SendPacket("Gt");
}
```

**Trad** :
- AGRESIVA + pas en CAC → avance (`get_Mover(cercano=true, ...)`).
- FUGITIVA + en CAC → recule (`get_Mover(cercano=false, ...)`).
- FUGITIVA + ennemi à dist < 8 → recule.
- FUGITIVA + ennemi à dist > 12 → avance (rapproche-toi pour rester en portée).
- Sinon (PASIVA, ou AGRESIVA déjà en CAC) → rien, juste pass turn.

**C'est UNE VRAIE LOGIQUE DE KITE.** Notre code actuel ne fait RIEN de ça.

### 2.2 `get_Mover` (déplacement repositionnement)

```csharp
public async Task get_Mover(bool cercano, Luchadores enemigo)
{
    KeyValuePair<short, MovimientoNodo>? nodo = null;
    Map mapa = cuenta.game.map;
    int distancia = -1;
    int distancia_total = Get_Total_Distancia_Enemigo(pelea.jugador_luchador.celda);

    foreach (KeyValuePair<short, MovimientoNodo> kvp in PeleasPathfinder.get_Celdas_Accesibles(pelea, mapa, pelea.jugador_luchador.celda))
    {
        if (!kvp.Value.alcanzable) continue;

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
                distancia = kvp.Value.camino.celdas_accesibles.Count;   // MAXIMISE PM utilisés
            }
        }
    }
    if (nodo != null)
        await cuenta.game.manager.movimientos.get_Mover_Celda_Pelea(nodo);
}

public int Get_Total_Distancia_Enemigo(Cell celda)
    => cuenta.game.fight.get_Enemigos.Sum(e => e.celda.GetDistanceBetweenCells(celda) - 1);
```

**Trad** : énumère toutes les cells atteignables, métrique = SOMME des distances
à tous les ennemis vivants. Si `cercano=true` (avance) on minimise la somme. Si
`cercano=false` (recule) on maximise la somme **et** on maximise aussi les PM
consommés (= on tire au maximum de la portée PM pour bien s'éloigner).

### 2.3 Détection « ennemi proche / loin »

```csharp
public bool is_proche_7(Cell celda = null) => get_Enemi_inferieur_7().Count() > 0;
public bool is_loin_8(Cell celda = null)   => get_Enemi_superieur_8().Count() > 0;

public IEnumerable<Luchadores> get_Enemi_inferieur_7(Cell celda = null)
    => get_Enemigos.Where(enemigo => enemigo.esta_vivo
        && (celda == null
            ? jugador_luchador.celda.GetDistanceBetweenCells(enemigo.celda)
            : enemigo.celda.GetDistanceBetweenCells(celda)) < 8);

public IEnumerable<Luchadores> get_Enemi_superieur_8(Cell celda = null)
    => get_Enemigos.Where(enemigo => enemigo.esta_vivo
        && (celda == null
            ? jugador_luchador.celda.GetDistanceBetweenCells(enemigo.celda)
            : enemigo.celda.GetDistanceBetweenCells(celda)) > 12);
```

⚠️ Bug nommage : `is_proche_7` teste `< 8`, `is_loin_8` teste `> 12`. Le code
est correct mais les noms sont décorrélés. À ne pas reproduire.

---

## 3. `Otros/Peleas/Fight.cs` — état combat + utilitaires

### 3.1 `get_Linea_Obstruida` (LOS Bresenham)

160 lignes denses. Logique : Bresenham modifié qui gère 3 cas (diagonale pure
== `tipo=1`, horizontal-dominant == `tipo=2`, vertical-dominant == `tipo=3`),
puis `get_Es_Celda_Obstruida` valide chaque case traversée.

```csharp
private static bool get_Es_Celda_Obstruida(double x, double y, Map map,
    List<short> occupiedCells, int targetCellId, double lastX, double lastY)
{
    Cell mp = map.GetCellByCoordinates((int)x, (int)y);
    return mp.isInLineOfSight                              // obstacle map (mur, décor)
        || (mp.cellId != targetCellId
            && occupiedCells.Contains(mp.cellId));         // combattant sur trajectoire
}
```

C'est exactement ce que fait notre `BotDofus.Divers.Cartes.LigneVisuelle.EstObstruee`
— validé. À garder.

### 3.2 `get_Celda_Mas_Cercana_O_Lejana` (placement initial)

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

**Trad** : parmi les cells de placement disponibles (reçues via paquet `GP`),
choisir celle qui minimise (CERCA_DE_ENEMIGOS) ou maximise (LEJOS_DE_ENEMIGOS)
la somme des distances aux ennemis vivants.

Implémentation à porter direct dans notre `ContexteCompte.ChoisirCellulePlacement`
ou équivalent.

### 3.3 Sélection cible avancée `get_Obtener_Enemigo_Mas_Cercano(int range)`

```csharp
public Luchadores get_Obtener_Enemigo_Mas_Cercano(int range = 0)
{
    int distancia = -1, distancia_temporal;
    bool mobFind = false;
    Luchadores enemigo = null;
    int vieMob = 999999;
    List<KeyValuePair<Luchadores, int>> enemiRange = new List<KeyValuePair<Luchadores, int>>();

    foreach (Luchadores luchador_enemigo in get_Enemigos)
    {
        if (!luchador_enemigo.esta_vivo) continue;

        distancia_temporal = jugador_luchador.celda.GetDistanceBetweenCells(luchador_enemigo.celda);

        if ((distancia == -1 || distancia_temporal < distancia))
        {
            distancia = distancia_temporal;
            enemigo = luchador_enemigo;
        }
        if (range != 0)
            enemiRange.Add(new KeyValuePair<Luchadores, int>(luchador_enemigo, distancia_temporal));
    }

    // 1er passage : focus low-HP non-invocation dans la range
    if (range != 0 && distancia != 1)
    {
        foreach (var item in enemiRange)
            if (item.Value <= range)
                if (vieMob > item.Key.vida_actual && item.Key.id_invocador != 10)
                {
                    vieMob = item.Key.vida_actual;
                    enemigo = item.Key;
                    mobFind = true;
                }
    }
    // 2e passage : si aucun mob trouvé sans-invoc, accepte les invoc
    if (range != 0 && distancia != 1 && mobFind == false)
    {
        foreach (var item in enemiRange)
            if (item.Value <= range)
                if (vieMob > item.Key.vida_actual)
                {
                    vieMob = item.Key.vida_actual;
                    enemigo = item.Key;
                    mobFind = true;
                }
    }
    return enemigo;
}
```

**Trad** : si on est déjà en CAC (distance=1) on garde l'ennemi le plus proche.
Sinon, on cherche dans la portée du sort un mob à **PV minimum** (focus
low-HP) et **non-invocation** en priorité, puis avec invocations en fallback.
C'est la heuristique "achève les mobs en train de mourir".

---

## 4. `Otros/Game/Manejadores/Movimientos/Movimiento.cs` — envoi GA001 combat

### 4.1 `get_Mover_Celda_Pelea` (envoi GA001 en combat)

```csharp
public async Task get_Mover_Celda_Pelea(KeyValuePair<short, MovimientoNodo>? nodo)
{
    if (!cuenta.IsFighting()) return;
    if (nodo == null || nodo.Value.Value.camino.celdas_accesibles.Count == 0) return;
    if (nodo.Value.Key == cuenta.game.fight.jugador_luchador.celda.cellId) return;

    nodo.Value.Value.camino.celdas_accesibles.Insert(0, cuenta.game.fight.jugador_luchador.celda.cellId);
    List<Cell> lista_celdas = nodo.Value.Value.camino.celdas_accesibles.Select(c => mapa.GetCellFromId(c)).ToList();

    await cuenta.connexion.SendPacketAsync("GA001" + PathFinderUtil.get_Pathfinding_Limpio(lista_celdas), false);
    personaje.evento_Personaje_Pathfinding_Minimapa(lista_celdas);
}
```

**Trad** :
1. Vérifie qu'on est bien en combat et qu'on a un nodo valide.
2. Insère la cell de départ EN PREMIÈRE position du chemin (le pathfinder
   renvoie depuis la 2e cell — il faut ajouter le départ pour que l'encodage
   marche).
3. Envoie `GA001` + chemin encodé.
4. **PAS de GKK0 proactif.** C'est le serveur qui broadcast la confirmation,
   puis MapFrame envoie GKK<n> en réaction.

C'est la raison de la modif récente dans notre `TrameJeu` (lignes 1089-1097) :
on n'envoie plus de GKK0 après GA001 combat.

### 4.2 `evento_Movimiento_Finalizado` (callback fin déplacement OVERWORLD)

```csharp
public async Task evento_Movimiento_Finalizado(Cell celda_destino, byte tipo_gkk, bool correcto)
{
    cuenta.AccountState = AccountStates.MOVING;

    if (correcto)
    {
        await Task.Delay(PathFinderUtil.get_Tiempo_Desplazamiento_Mapa(personaje.celda, actual_path, personaje.esta_utilizando_dragopavo));

        if (cuenta == null || cuenta.AccountState == AccountStates.DISCONNECTED) return;

        cuenta.connexion.SendPacket("GKK" + tipo_gkk);  // GKK réactif APRÈS délai marche
        personaje.celda = celda_destino;
    }
    actual_path = null;
    cuenta.AccountState = AccountStates.CONNECTED_INACTIVE;
    movimiento_finalizado?.Invoke(correcto);
}
```

**Trad** : pour les déplacements OVERWORLD uniquement. Le délai = temps réel
d'animation de marche calculé en fonction du nb de cases + monture + dénivelés.
Puis GKK<tipo> (tipo = code passé par le frame qui a déclenché le mouvement,
ex 4 = changement de map, 6 = arrivée case). **Ce n'est pas appelé pour les
mouvements combat** — ceux-là sont validés par le broadcast `GA;0/1` du
serveur.

---

## 5. `Otros/Mapas/Movimiento/PathFinderUtil.cs` — encoding chemin

### 5.1 `get_Pathfinding_Limpio` (compression chemin → segments par direction)

```csharp
public static string get_Pathfinding_Limpio(List<Cell> camino)
{
    Cell celda_destino = camino.Last();
    if (camino.Count <= 2)
        return celda_destino.GetCharDirection(camino.First()) + Hash.Get_Cell_Char(celda_destino.cellId);

    StringBuilder pathfinder = new StringBuilder();
    char direccion_anterior = camino[1].GetCharDirection(camino.First()), direccion_actual;

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
```

**Trad** : compresse le chemin (suite de cells) en suite de **segments** `<dir><cell_2char>`,
un par changement de direction. Exemple : 5 cases vers le SE puis 3 vers le S →
2 segments seulement, pas 8.

- `<dir>` ∈ `{a, b, c, d, e, f, g, h}` (8 directions iso, NE/E/SE/S/SO/O/NO/N).
- `<cell_2char>` = `Hash.Get_Cell_Char(cellId)` qui encode l'ID cell (0-639) sur
  2 caractères de l'alphabet base-64 dyshay (`abc...XYZ012`).

**Pour le combat**, le pathfinder ne renvoie que des cells adjacentes 4-dir
(pas de diagonale), donc les directions seront uniquement parmi `{b, d, f, h}`
(E, S, O, N). Mais l'encoding marche pareil.

### 5.2 `get_Tiempo_Desplazamiento_Mapa` (durée animation marche overworld)

```csharp
private static readonly Dictionary<TipoAnimacion, DuracionAnimacion> tiempo_tipo_animacion = new()
{
    { TipoAnimacion.MONTURA, new DuracionAnimacion(135, 200, 120) },     // horizontal, vertical, lineal
    { TipoAnimacion.CORRIENDO, new DuracionAnimacion(170, 255, 150) },   // courir : > 6 cases
    { TipoAnimacion.CAMINANDO, new DuracionAnimacion(480, 510, 425) },   // marcher : <= 6 cases
    { TipoAnimacion.FANTASMA, new DuracionAnimacion(57, 85, 50) }        // mode fantôme
};
```

Notre `PathFinderUtil` C# WPF utilise les mêmes constantes. Validé.

---

## 6. `Otros/Mapas/Movimiento/Peleas/PeleasPathfinder.cs` — BFS combat

### 6.1 `get_Celdas_Accesibles` (BFS depuis ma cell, limité PM)

```csharp
public static Dictionary<short, MovimientoNodo> get_Celdas_Accesibles(Fight pelea, Map mapa, Cell celda_actual)
{
    Dictionary<short, MovimientoNodo> celdas = new Dictionary<short, MovimientoNodo>();
    if (pelea.jugador_luchador.pm <= 0) return celdas;

    short maximos_pm = pelea.jugador_luchador.pm;
    List<NodoPelea> celdas_permitidas = new List<NodoPelea>();
    Dictionary<short, NodoPelea> celdas_prohibidas = new Dictionary<short, NodoPelea>();

    NodoPelea nodo = new NodoPelea(celda_actual, maximos_pm, pelea.jugador_luchador.pa, 1);
    celdas_permitidas.Add(nodo);
    celdas_prohibidas[celda_actual.cellId] = nodo;

    while (celdas_permitidas.Count > 0)
    {
        NodoPelea actual = celdas_permitidas.Last();
        celdas_permitidas.Remove(actual);
        Cell nodo_celda = actual.celda;
        List<Cell> adyecentes = get_Celdas_Adyecentes(nodo_celda, mapa.mapCells);

        // exclut les cells occupées par un combattant
        int i = 0;
        while (i < adyecentes.Count)
        {
            Luchadores enemigo = pelea.get_Luchadores.FirstOrDefault(f => f.celda.cellId == adyecentes[i]?.cellId);
            if (adyecentes[i] != null && enemigo == null) { i++; continue; }
            adyecentes.RemoveAt(i);
        }

        int pm_disponibles = actual.pm_disponible - 1;
        int pa_disponibles = actual.pa_disponible;
        int distancia = actual.distancia + 1;
        bool accesible = pm_disponibles >= 0;

        for (i = 0; i < adyecentes.Count; i++)
        {
            if (celdas_prohibidas.ContainsKey(adyecentes[i].cellId))
            {
                NodoPelea anterior = celdas_prohibidas[adyecentes[i].cellId];
                if (anterior.pm_disponible > pm_disponibles) continue;
                if (anterior.pm_disponible == pm_disponibles && anterior.pm_disponible >= pa_disponibles) continue;
            }
            if (!adyecentes[i].IsWalkable()) continue;

            celdas[adyecentes[i].cellId] = new MovimientoNodo(nodo_celda.cellId, accesible);
            nodo = new NodoPelea(adyecentes[i], pm_disponibles, pa_disponibles, distancia);
            celdas_prohibidas[adyecentes[i].cellId] = nodo;

            if (actual.distancia < maximos_pm) celdas_permitidas.Add(nodo);
        }
    }

    // reconstitue le chemin de chaque cell atteinte
    foreach (short celda in celdas.Keys)
        celdas[celda].camino = get_Path_Pelea(celda_actual.cellId, celda, celdas);

    return celdas;
}
```

**Trad** : BFS classique, distance ≤ PM, exclut cells occupées par combattants.
Pour chaque cell atteinte on stocke le `MovimientoNodo(celda_inicial, alcanzable)`
qui permet de reconstituer le chemin via `get_Path_Pelea`.

### 6.2 `get_Celdas_Adyecentes` (voisinage 4-dir ortho)

```csharp
public static List<Cell> get_Celdas_Adyecentes(Cell nodo, Cell[] mapa_celdas)
{
    List<Cell> celdas_adyecentes = new List<Cell>();
    Cell celda_derecha   = mapa_celdas.FirstOrDefault(c => c.x == nodo.x + 1 && c.y == nodo.y);
    Cell celda_izquierda = mapa_celdas.FirstOrDefault(c => c.x == nodo.x - 1 && c.y == nodo.y);
    Cell celda_inferior  = mapa_celdas.FirstOrDefault(c => c.x == nodo.x && c.y == nodo.y + 1);
    Cell celda_superior  = mapa_celdas.FirstOrDefault(c => c.x == nodo.x && c.y == nodo.y - 1);

    if (celda_derecha != null)   celdas_adyecentes.Add(celda_derecha);
    if (celda_izquierda != null) celdas_adyecentes.Add(celda_izquierda);
    if (celda_inferior != null)  celdas_adyecentes.Add(celda_inferior);
    if (celda_superior != null)  celdas_adyecentes.Add(celda_superior);

    return celdas_adyecentes;
}
```

**Note importante** : le commentaire dans le code dit `//pelea no utiliza diagonales`
— le pathfinder combat est strictement 4-dir orthogonal. Notre `Pathfinder.Trouver(...,
combat: true)` doit faire pareil (déjà aligné).

---

## 7. Mapping dyshay → notre code WPF

| dyshay | Notre code | Statut |
|--------|------------|--------|
| `SpellsManager.manejador_Hechizos` | `TrameJeu.JouerTourCombatAsync` (260 l. inline) | À refondre en machine d'état |
| `SpellsManager.get_Mover_Lanzar_hechizo_Simple` | `TrameJeu.TrouverApprocheCombat` | OK (avec ModeCombat extension) |
| `FightExtensions.get_Fin_Turno` | **manquant** (pass turn direct) | À ajouter |
| `FightExtensions.get_Mover` | **manquant** (pas de move multi-ennemis) | À ajouter |
| `Fight.get_Linea_Obstruida` | `LigneVisuelle.EstObstruee` | OK |
| `Fight.get_Celda_Mas_Cercana_O_Lejana` | **partiel** (placement legacy) | À porter |
| `Fight.get_Obtener_Enemigo_Mas_Cercano(range)` | filtre EnnemiPlusFaible dans rule eng. | Heuristique low-HP à porter |
| `Fight.get_Puede_Lanzar_hechizo` 12 codes | `MoteurReglesCombat.Evaluer` continue silent | Refactor en enum |
| `Movimiento.get_Mover_Celda_Pelea` | `TrameJeu` ligne 1042 envoi inline | À extraire |
| `Movimiento.evento_Movimiento_Finalizado` (GKK<n> overworld) | `TrameOverworld` partiel | OK overworld |
| `PathFinderUtil.get_Pathfinding_Limpio` | `Pathfinder.PaquetDeplacement` | OK |
| `PeleasPathfinder.get_Celdas_Accesibles` BFS | `Pathfinder.Trouver(..., combat: true)` | OK |

---

## 8. À retenir pour notre refonte

1. **Machine d'état event-driven** : `TurnStarted → ProcessRule(i) → (cast|move) → callback → ProcessRule(i) or ProcessRule(i+1) → ... → EndTurn`.
2. **`get_Fin_Turno` est CRUCIAL** : c'est le repositionnement post-cast qui rend le bot vivant (kite/engage). Notre `Gt` direct casse toute tactique.
3. **`Get_Total_Distancia_Enemigo`** = sum sur tous les ennemis. Permet le multi-mob smart-positioning. Notre approche 1-ennemi est trop naïve.
4. **`FallosLanzandoHechizo` enum** : codes d'erreur explicites permettent du log précis ET déclenchent la bascule "rejette ce sort, essaie le suivant" vs "force move puis re-essaye".
5. **`lanzamientos_x_turno` + `lanzamientos_por_objetivo` + `intervalo`** = 3 compteurs distincts gérés par `Fight.get_Turno_Acabado`. On a déjà `CompteursRegleParTour` mais pas `intervalo` (cooldown multi-tours) ni `por_objetivo`.
6. **PAS de GKK0 après GA001 combat.** Seul GKK est `MapFrame` après broadcast serveur de déplacement overworld.

Source authoritative à garder ouverte pendant la refonte :
`.claude/worktrees/vigorous-murdock/dyshay-source/Otros/Peleas/`.
