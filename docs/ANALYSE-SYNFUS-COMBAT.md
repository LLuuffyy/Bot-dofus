# Analyse SynFus — Combat 1.29 (refonte nuit 2026-05-21)

> Doc de référence n°1 / 4. Source authoritative : dyshay/Bot-Dofus-Retro (Alvaro Prendes,
> `salesprendes.com`) — **auteur identique** à SynFus, code C# pré-obfuscation.
> Le binaire `Synfus.exe` (`C:\Users\touki\Desktop\SynFus_Aqua_v1.1.0\Synfus.exe`) est un
> deployable .NET self-contained obfusqué (toutes les DLL adjacentes sont des
> resources WPF/WinForms localisées en plusieurs langues — RIEN d'extractible).
> On utilise donc **dyshay-source** (chemin local
> `.claude/worktrees/vigorous-murdock/dyshay-source/`) comme proxy 1:1, vérifié
> par croisement avec les logs SynFus + captures réseau Hystoria.

---

## 1. Boucle principale d'un tour de combat (`FightExtensions.cs`)

Le tour est event-driven : pas de `while(monTour) { ... }`, mais une chaîne
d'événements `pelea_creada` → `turno_iniciado` → (`hechizo_lanzado` |
`movimiento`) → `get_Fin_Turno`.

### 1.1 Démarrage du tour (`get_Pelea_Turno_iniciado`)

```csharp
private async void get_Pelea_Turno_iniciado()
{
    cuenta.Logger.LogInfo("Fight", "Nombre de monstre restant :" + pelea.get_Enemigos.Count());

    hechizo_lanzado_index = 0;       // pointeur dans la liste de règles
    esperando_sequencia_fin = true;  // flag d'attente d'event GAS/GAF

    await Task.Delay(400);            // pause humanisation

    if (configuracion.hechizos.Count == 0 || !cuenta.game.fight.get_Enemigos.Any())
    {
        await get_Fin_Turno();        // rien à faire → fin tour direct
        return;
    }
    await get_Procesar_hechizo();    // entre dans la boucle de sorts
}
```

**Points clefs** :
- `hechizo_lanzado_index` = curseur dans `configuracion.hechizos` (la liste
  ORDONNÉE de règles de sorts, équivalent SynFus de notre `ConfigCombat.Regles`).
- `esperando_sequencia_fin` = booléen "j'ai envoyé une action, j'attends le
  callback `hechizo_lanzado` ou `movimiento` du serveur".
- Pas de boucle while. Le `get_Procesar_hechizo` se re-trigger lui-même via les
  events `get_Procesar_Hechizo_Lanzado` (callback GAS/GAF) ou
  `get_Procesar_Movimiento` (callback GA;0/1) après chaque action.

### 1.2 Boucle interne par règle (`get_Procesar_hechizo`)

```csharp
private async Task get_Procesar_hechizo()
{
    if (cuenta?.IsFighting() == false || configuracion == null) return;

    if (hechizo_lanzado_index >= configuracion.hechizos.Count)
    {
        await get_Fin_Turno();       // toutes les règles essayées → fin tour
        return;
    }

    HechizoPelea hechizo_actual = configuracion.hechizos[hechizo_lanzado_index];

    if (hechizo_actual.lanzamientos_restantes == 0)
    {
        await get_Procesar_Siguiente_Hechizo(hechizo_actual); // ++idx, recurse
        return;
    }

    ResultadoLanzandoHechizo resultado = await manejador_hechizos.manejador_Hechizos(...);
    switch (resultado)
    {
        case LANZADO:    hechizo_actual.lanzamientos_restantes--; break;
        case MOVIDO:     /* on a dû bouger pour atteindre la cible */ break;
        case NO_LANZADO: await get_Procesar_Siguiente_Hechizo(...); break;
    }
}
```

Concept : chaque règle a un **compteur de lancers restants** (`lanzamientos_restantes`),
initialisé au début du combat par `pelea_creada`. Quand on lance un sort
avec succès, on décrémente. Quand on arrive à 0, on passe à la règle suivante.
Quand on essaie de lancer et que la cible n'est pas atteignable (NO_LANZADO),
on passe **directement** à la règle suivante (pas de retry sur la même règle).

### 1.3 Fin de tour (`get_Fin_Turno`)

```csharp
private async Task get_Fin_Turno()
{
    // Repositionnement avant pass turn (selon tactique)
    if (!pelea.esta_Cuerpo_A_Cuerpo_Con_Enemigo() && configuracion.tactica == Tactica.AGRESIVA)
        await get_Mover(true, pelea.get_Obtener_Enemigo_Mas_Cercano());
    else if (pelea.esta_Cuerpo_A_Cuerpo_Con_Enemigo() && configuracion.tactica == Tactica.FUGITIVA)
        await get_Mover(false, pelea.get_Obtener_Enemigo_Mas_Cercano());
    else if (pelea.is_proche_7() && configuracion.tactica == Tactica.FUGITIVA)
        await get_Mover(false, pelea.get_Obtener_Enemigo_Mas_Cercano());  // recule
    else if (pelea.is_loin_8() && configuracion.tactica == Tactica.FUGITIVA)
        await get_Mover(true, pelea.get_Obtener_Enemigo_Mas_Cercano());   // avance

    pelea.get_Turno_Acabado();
    var t = new Random().Next(200, 500);
    await Task.Delay(t);
    cuenta.connexion.SendPacket("Gt");
}
```

C'est ICI que SynFus/dyshay fait un **2e déplacement à la fin du tour**, après
avoir vidé ses PA, pour se repositionner selon la tactique (engager, kiter,
fuir). Notre code actuel ne fait **PAS** ça — `JouerTourCombatAsync` envoie `Gt`
direct après le cast, on perd toute logique de positionnement post-cast.

---

## 2. Algo "Faut-il se déplacer ?" (`SpellsManager.manejador_Hechizos`)

```csharp
public async Task<ResultadoLanzandoHechizo> manejador_Hechizos(HechizoPelea hechizo, bool capturer = false)
{
    if (hechizo.focus == HechizoFocus.CELDA_VACIA)
        return await lanzar_Hechizo_Celda_Vacia(hechizo);

    if (hechizo.metodo_lanzamiento == MetodoLanzamiento.AMBOS)
        return await get_Lanzar_Hechizo_Simple(hechizo, capturer);

    if (hechizo.metodo_lanzamiento == MetodoLanzamiento.ALEJADO && !pelea.esta_Cuerpo_A_Cuerpo_Con_Enemigo())
        return await get_Lanzar_Hechizo_Simple(hechizo);

    if (hechizo.metodo_lanzamiento == MetodoLanzamiento.CAC && pelea.esta_Cuerpo_A_Cuerpo_Con_Enemigo())
        return await get_Lanzar_Hechizo_Simple(hechizo);

    if (hechizo.metodo_lanzamiento == MetodoLanzamiento.CAC && !pelea.esta_Cuerpo_A_Cuerpo_Con_Enemigo())
        return await get_Mover_Lanzar_hechizo_Simple(hechizo, get_Objetivo_Mas_Cercano(hechizo));

    return ResultadoLanzandoHechizo.NO_LANZADO;
}
```

**Décision binaire** : la méthode de lancement (CAC / ALEJADO / AMBOS) combinée
avec la situation actuelle (en CAC ou pas) détermine si on **doit bouger AVANT
de caster**.

| metodo_lanzamiento | Situation actuelle | Action |
|--------------------|--------------------|--------|
| `AMBOS`            | n'importe          | `get_Lanzar_Hechizo_Simple` (cast direct, ou bouge si hors portée) |
| `ALEJADO`          | pas en CAC         | cast direct |
| `ALEJADO`          | en CAC             | NO_LANZADO (rejette le sort, passe à la suite) |
| `CAC`              | en CAC             | cast direct |
| `CAC`              | pas en CAC         | **`get_Mover_Lanzar_hechizo_Simple`** (force le déplacement avant cast) |

Puis `get_Lanzar_Hechizo_Simple` re-check la portée et bascule sur `get_Mover_Lanzar...`
si nécessaire :

```csharp
FallosLanzandoHechizo resultado = pelea.get_Puede_Lanzar_hechizo(...);
if (resultado == FallosLanzandoHechizo.NINGUNO)
{
    await pelea.get_Lanzar_Hechizo(hechizo.id, enemigo.celda.cellId);  // cast direct
    return ResultadoLanzandoHechizo.LANZADO;
}
if (resultado == FallosLanzandoHechizo.NO_ESTA_EN_RANGO)
    return await get_Mover_Lanzar_hechizo_Simple(hechizo, enemigo);    // bouge puis cast
```

---

## 3. Algo de choix de cellule cible selon le mode

### 3.1 `get_Mover_Lanzar_hechizo_Simple` (déplacement pour cast hors portée)

```csharp
private async Task<ResultadoLanzandoHechizo> get_Mover_Lanzar_hechizo_Simple(HechizoPelea hechizo, Luchadores enemigo)
{
    KeyValuePair<short, MovimientoNodo>? nodo = null;
    int pm_utilizados = 99;

    foreach (KeyValuePair<short, MovimientoNodo> movimiento in PeleasPathfinder.get_Celdas_Accesibles(pelea, mapa, pelea.jugador_luchador.celda))
    {
        if (!movimiento.Value.alcanzable) continue;

        if (hechizo.metodo_lanzamiento == MetodoLanzamiento.CAC
         && !pelea.esta_Cuerpo_A_Cuerpo_Con_Aliado(mapa.GetCellFromId(movimiento.Key)))
            continue;  // mode CAC : la nouvelle cell doit être adjacente à un allié

        if (pelea.get_Puede_Lanzar_hechizo(hechizo.id, mapa.GetCellFromId(movimiento.Key), enemigo.celda, mapa) != FallosLanzandoHechizo.NINGUNO)
            continue;  // depuis cette nouvelle cell, le sort doit être castable

        if (movimiento.Value.camino.celdas_accesibles.Count <= pm_utilizados)
        {
            nodo = movimiento;
            pm_utilizados = movimiento.Value.camino.celdas_accesibles.Count;  // MINIMISE les PM
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

**Algo** :
1. Énumère TOUTES les cellules atteignables par le pathfinder (BFS 4-dir).
2. Filtre par contraintes : marchable + accessible + (mode CAC → adjacente allié) +
   sort castable depuis la nouvelle cell.
3. **Minimise** les PM utilisés (préserve PM pour les casts suivants).

Notre code (`TrouverApprocheCombat`) fait plus subtil — il choisit la cellule
selon `ModeCombat` (Agressif / Eloigne / Fuyard / Equilibre) — mais **rate**
le critère "minimise PM" en absolu (uniquement tie-break). Le `ScoreCelluleMode`
peut renvoyer 0 pour 10 cells différentes, et le tie-break sur nbPas A* n'arrive
qu'après. C'est OK mais SynFus fait plus simple : "minimise nbPas tout court".

### 3.2 `FightExtensions.get_Mover` (déplacement repositionnement fin de tour)

```csharp
public async Task get_Mover(bool cercano, Luchadores enemigo)
{
    KeyValuePair<short, MovimientoNodo>? nodo = null;
    int distancia = -1;
    int distancia_total = Get_Total_Distancia_Enemigo(pelea.jugador_luchador.celda);

    foreach (KeyValuePair<short, MovimientoNodo> kvp in PeleasPathfinder.get_Celdas_Accesibles(...))
    {
        if (!kvp.Value.alcanzable) continue;

        int temporal_distancia = Get_Total_Distancia_Enemigo(mapa.GetCellFromId(kvp.Key));

        if ((cercano && temporal_distancia <= distancia_total)
         || (!cercano && temporal_distancia >= distancia_total))
        {
            if (cercano) { nodo = kvp; distancia_total = temporal_distancia; }
            else if (kvp.Value.camino.celdas_accesibles.Count >= distancia)
            {
                nodo = kvp; distancia_total = temporal_distancia;
                distancia = kvp.Value.camino.celdas_accesibles.Count;  // MAXIMISE PM utilisés (loin)
            }
        }
    }
    if (nodo != null) await cuenta.game.manager.movimientos.get_Mover_Celda_Pelea(nodo);
}

public int Get_Total_Distancia_Enemigo(Cell celda)
    => cuenta.game.fight.get_Enemigos.Sum(e => e.celda.GetDistanceBetweenCells(celda) - 1);
```

**Métrique** : `sum(distance(cell, ennemi) - 1)` — somme des distances à TOUS
les ennemis vivants. Mode `cercano=true` (Agressif) minimise cette somme,
`cercano=false` (Fugitif) la maximise.

C'est une vraie tactique multi-ennemis. Notre code actuel ne regarde QUE
l'ennemi le plus proche (Ennemis.OrderBy(distance).First()) — perd l'info des
autres mobs.

---

## 4. Gestion PA / PM et multi-cast par tour

### 4.1 Compteurs persistants (`Fight.cs`)

```csharp
private Dictionary<int, int> hechizos_intervalo;                       // hechizoID → tours_cooldown_restants
private Dictionary<int, int> total_hechizos_lanzados;                  // hechizoID → nb_lancers_tour_courant
private Dictionary<int, Dictionary<int, int>> total_hechizos_lanzados_en_celda; // hechizoID → (celda → nb)
```

3 compteurs distincts :
- **Cooldown global** sur le sort (turns 1 à N entre 2 lancers).
- **Lancers par tour** sur le sort.
- **Lancers par cible et par tour**.

Reset à la fin du tour (`get_Turno_Acabado`) :

```csharp
public void get_Turno_Acabado()
{
    total_hechizos_lanzados.Clear();
    total_hechizos_lanzados_en_celda.Clear();
    for (int i = hechizos_intervalo.Count - 1; i >= 0; i--)
    {
        int key = hechizos_intervalo.ElementAt(i).Key;
        hechizos_intervalo[key]--;
        if (hechizos_intervalo[key] == 0) hechizos_intervalo.Remove(key);
    }
}
```

### 4.2 Validation lancement (`get_Puede_Lanzar_hechizo`)

```csharp
public FallosLanzandoHechizo get_Puede_Lanzar_hechizo(short hechizo_id)
{
    Spell hechizo = account.game.character.get_Hechizo(hechizo_id);
    if (hechizo == null) return DESONOCIDO;
    SpellStats datos = hechizo.get_Stats();

    if (jugador_luchador.pa < datos.coste_pa)                 return PUNTOS_ACCION;
    if (datos.lanzamientos_por_turno > 0
        && total_hechizos_lanzados.ContainsKey(hechizo_id)
        && total_hechizos_lanzados[hechizo_id] >= datos.lanzamientos_por_turno) return DEMASIADOS_LANZAMIENTOS;
    if (hechizos_intervalo.ContainsKey(hechizo_id))           return COOLDOWN;
    if (datos.efectos_normales[0].id == 181
        && contador_invocaciones >= account.game.character.caracteristicas.criaturas_invocables.total_stats)
        return DEMASIADAS_INVOCACIONES;

    return NINGUNO;
}
```

Surcharge avec contexte cellule :

```csharp
public FallosLanzandoHechizo get_Puede_Lanzar_hechizo(short hechizo_id, Cell celda_actual, Cell celda_objetivo, Map mapa)
{
    // ...
    if (datos.lanzamientos_por_objetivo > 0 && ...) return DEMASIADOS_LANZAMIENTOS_POR_OBJETIVO;
    if (datos.es_celda_vacia && !es_Celda_Libre(celda_objetivo)) return NECESITA_CELDA_LIBRE;
    if (datos.es_lanzado_linea && !jugador_luchador.celda.AreCellsOnLine(celda_objetivo)) return NO_ESTA_EN_LINEA;
    if (!get_Rango_hechizo(celda_actual, datos, mapa).Contains(celda_objetivo.cellId)) return NO_ESTA_EN_RANGO;
    return NINGUNO;
}
```

12 codes d'erreur explicites — c'est ce qui permet de logger précisément
pourquoi un sort est rejeté, et de basculer sur la règle suivante.

### 4.3 Multi-cast (`hechizo_lanzado` event re-entry)

```csharp
public async void get_Procesar_Hechizo_Lanzado(short celda_id, bool exito)
{
    if (pelea.total_enemigos_vivos == 0) return;
    if (!esperando_sequencia_fin) return;
    esperando_sequencia_fin = false;

    await Task.Delay(400);   // pause après cast

    if (!exito) { await get_Procesar_Siguiente_Hechizo(...); return; }

    pelea.actualizar_Hechizo_Exito(celda_id, configuracion.hechizos[hechizo_lanzado_index].id);
    await get_Procesar_hechizo();  // recurse → essaie même règle ou suivante selon compteur
}
```

**Critique** : à la fin du callback GAS/GAF d'un cast réussi, on **re-rentre**
dans `get_Procesar_hechizo`. Si `lanzamientos_restantes > 0`, on relance le
MÊME sort (multi-cast jusqu'au max). Sinon on passe à la règle suivante.

C'est la mécanique du multi-cast : pas de boucle, juste de la récursion event-driven.

---

## 5. Code UI Combat (form SynFus / dyshay `Forms/Opciones.cs`)

L'onglet "Peleas" (Combats) de dyshay contient :

### 5.1 Configuration globale
- **ComboBox** `Tactica` : 3 options (`AGRESIVA`, `PASIVA`, `FUGITIVA`).
- **ComboBox** `PosicionamientoInicioPelea` : (`INMOVIL`, `CERCA_DE_ENEMIGOS`, `LEJOS_DE_ENEMIGOS`).
- **CheckBox** `desactivar_espectador`, `utilizar_dragopavo`.
- **NumericUpDown** `iniciar_regeneracion` (PV%), `detener_regeneracion` (PV%).

### 5.2 Liste de sorts (DataGridView)
Colonnes :
- ID sort + Nom (lookup XML).
- **HechizoFocus** : `ENEMIGO`, `ALIADO`, `ENCIMA`, `CELDA_VACIA`.
- **MetodoLanzamiento** : `CAC`, `ALEJADO`, `AMBOS`.
- **lanzamientos_x_turno** : byte (0 = no limit).

Boutons : "Añadir sort" / "Editar" / "Eliminar" / "Subir" / "Bajar" (l'ordre =
priorité dans la rotation).

### 5.3 SynFus (extensions visuelles vs dyshay)
SynFus enrichit ce form avec :
- Panneau d'**ennemis en combat** (liste live, PV barres, sélection cible manuelle).
- **Conditions par sort** (clic sur ⓘ d'une règle) :
  - Distance (min/max, ignore CAC, seulement CAC)
  - Cible (PV<, PV>, plus faible, plus forte)
  - Joueur (mes PV<, mes PV>, pas si taclé, si invoc présente)
  - Situation (ennemis ≥, ennemis ≤, 1er tour, dernier tour)
  - Avancé (tous les N tours, à partir tour, élément requis)
- **ListBox sorts appris** (lookup depuis le perso scannée).
- **Slider distance préférée**, **slider seuil fuite**, **slider délai actions**.
- Section "Consommable de soin" (id template + seuils PV + délais ms).
- 4 **RadioButton** mode de combat (Agressif / Eloigne / Fuyard / Equilibre) —
  c'est l'extension SynFus du `Tactica` dyshay (3 valeurs → 4 modes).

Notre `VueCombat.xaml` actuel a déjà **75%** de ce UI implémenté
(toutes les sections sauf "Ennemis live" qui est juste l'`ItemsControl
ListeCombattants` sans bind sur les PV ni couleur dynamique). Cf. doc 3.

---

## 6. Synthèse pour notre projet

| Élément SynFus / dyshay | Notre code actuel | Écart |
|--------------------------|-------------------|-------|
| Tour event-driven (`pelea_creada` → `turno_iniciado` → `hechizo_lanzado` → recurse) | `JouerTourCombatAsync` linéaire en 1 méthode unique 260 l. | Refactor en machine d'état event-driven |
| Compteur `lanzamientos_restantes` par règle (réinit `pelea_creada`) | `CompteursRegleParTour[idSort]` (réinit `NouveauTour`) | OK |
| `hechizos_intervalo` (cooldown serveur multi-tours) | **absent** | À ajouter |
| `total_hechizos_lanzados_en_celda` (max par cible) | `NombreParCible` champ stocké, **non lu** | Brancher dans `MoteurReglesCombat` |
| 12 codes `FallosLanzandoHechizo` distincts | Continue / break implicite | Refactor en enum + log |
| `get_Mover_Lanzar_hechizo_Simple` MINIMISE PM | `TrouverApprocheCombat` tie-break nbPas mais score-first | Aligner |
| Repositionnement fin de tour (`get_Fin_Turno → get_Mover`) | **absent** (`Gt` direct après cast) | À implémenter |
| Bresenham LOS `get_Linea_Obstruida` | `LigneVisuelle.EstObstruee` (existant) | OK |
| Pathfinder 4-dir combat `PeleasPathfinder` | `Pathfinder.Trouver(..., combat: true)` 4-dir | OK |
| Sum distance multi-ennemis `Get_Total_Distancia_Enemigo` | Distance 1-ennemi only | À étendre |
| Capture âme (sort 413) | Hors scope projet | OK |
| Form UI 22 conditions sort | `VueCombat.xaml` 22 conditions bindées | OK (Material Design + sliders en plus) |

**Prochaines étapes** : voir docs 3 (IA-COMBAT-EXPLICATION) et 4 (PLAN-REFONTE).
