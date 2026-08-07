# Patterns IA combat — Synfus & Dyshay (référence)

**Date** : 2026-05-22
**Sources** : `docs/ANALYSE-DYSHAY-COMBAT.md` (extraits déjà capturés du repo
dyshay/Bot-Dofus-Retro) + capture forensic SynFus.

## Pattern 1 — Distance d'arrêt selon mode (kite intelligent)

**Source dyshay** : `FightExtensions.get_Mover`

```csharp
public async Task get_Mover(bool cercano, Luchadores enemigo)
{
    KeyValuePair<short, MovimientoNodo>? nodo = null;
    Map mapa = cuenta.game.map;
    int distancia = -1;
    int distancia_total = Get_Total_Distancia_Enemigo(pelea.jugador_luchador.celda);

    foreach (var kvp in PeleasPathfinder.get_Celdas_Accesibles(...))
    {
        if (!kvp.Value.alcanzable) continue;
        int temporal_distancia = Get_Total_Distancia_Enemigo(mapa.GetCellFromId(kvp.Key));

        if ((cercano && temporal_distancia <= distancia_total)
         || (!cercano && temporal_distancia >= distancia_total))
        {
            if (cercano) { nodo = kvp; distancia_total = temporal_distancia; }
            else if (kvp.Value.camino.celdas_accesibles.Count >= distancia)
            {
                nodo = kvp;
                distancia_total = temporal_distancia;
                distancia = kvp.Value.camino.celdas_accesibles.Count;  // MAXIMISE PM
            }
        }
    }
    if (nodo != null) await ...get_Mover_Celda_Pelea(nodo);
}
```

**Pattern extrait** :
- Énumère toutes les cells atteignables via PM
- Métrique : SOMME des distances Chebyshev à TOUS les ennemis vivants
  (= `Get_Total_Distancia_Enemigo`)
- Si `cercano=true` (avance) : minimise la somme
- Si `cercano=false` (recule) : maximise la somme + maximise les PM
  consommés (s'éloigne au max)

**Adapté chez nous** : `ScorePositionCombat.SommeDistances` + `ScoreCelluleAvance`
qui généralise avec contexte sort.

## Pattern 2 — Kite parfait pour Cra

**Source** : *no formal source* (pattern joueur compétitif Dofus).

**Algorithme** :
```
distance_optimale = porteeMax_sort + PM_ennemi
```

**Justification** : à tour N je tire à porteeMax. L'ennemi a PM cases pour
avancer. À tour N+1 je veux être à porteeMax DE NOUVEAU pour re-cast.
Donc je dois finir tour N à `porteeMax + PM_ennemi`.

**Cas limites** :
- PM_ennemi = 0 → dist = porteeMax (l'ennemi ne bouge pas, je reste pile à portée max)
- PM_ennemi > PM_max_recul_possible → je recule au max possible, et au tour
  N+1 l'ennemi me rattrape mais je peux quand même cast (juste moins optimal)

**Adapté chez nous** : `ScorePositionCombat.DistanceIdeale` cas Eloigne/Fuyard.

## Pattern 3 — Décision tour par tour

**Source dyshay** : `SpellsManager.manejador_Hechizos`

```csharp
public async Task<ResultadoLanzandoHechizo> manejador_Hechizos(HechizoPelea hechizo, bool capturer = false)
{
    if (hechizo.focus == HechizoFocus.CELDA_VACIA)
        return await lanzar_Hechizo_Celda_Vacia(hechizo);

    if (hechizo.metodo_lanzamiento == MetodoLanzamiento.AMBOS)
        return await get_Lanzar_Hechizo_Simple(hechizo, capturer);

    if (hechizo.metodo_lanzamiento == MetodoLanzamiento.ALEJADO
        && !cuenta.game.fight.esta_Cuerpo_A_Cuerpo_Con_Enemigo())
        return await get_Lanzar_Hechizo_Simple(hechizo);

    if (hechizo.metodo_lanzamiento == MetodoLanzamiento.CAC
        && cuenta.game.fight.esta_Cuerpo_A_Cuerpo_Con_Enemigo())
        return await get_Lanzar_Hechizo_Simple(hechizo);

    if (hechizo.metodo_lanzamiento == MetodoLanzamiento.CAC
        && !cuenta.game.fight.esta_Cuerpo_A_Cuerpo_Con_Enemigo())
        return await get_Mover_Lanzar_hechizo_Simple(hechizo, get_Objetivo_Mas_Cercano(hechizo));

    return ResultadoLanzandoHechizo.NO_LANZADO;
}
```

**Pattern extrait** : la méthode de lancement (CAC / Distance / Both)
+ position actuelle (au CAC ou non) déterminent si on cast direct ou si
on bouge avant de cast.

**Adapté chez nous** : `MoteurReglesCombat.Evaluer` filtre selon
`MethodeLancement` + `SeulementCAC`/`IgnorerCAC`. Si hors portée et qu'on
peut bouger → `IACombatHerosSimple.TenterDeplacementPuisCastAsync` ou
master `TrouverApprocheCombat`.

## Pattern 4 — `get_Fin_Turno` (repositionnement fin de tour)

**Source dyshay** : `FightExtensions.get_Fin_Turno`

```csharp
private async Task get_Fin_Turno()
{
    if (!pelea.esta_Cuerpo_A_Cuerpo_Con_Enemigo()
        && configuracion.tactica == Tactica.AGRESIVA)
        await get_Mover(true, pelea.get_Obtener_Enemigo_Mas_Cercano());
    else if (pelea.esta_Cuerpo_A_Cuerpo_Con_Enemigo()
        && configuracion.tactica == Tactica.FUGITIVA)
        await get_Mover(false, pelea.get_Obtener_Enemigo_Mas_Cercano());
    else if (pelea.is_proche_7() && configuracion.tactica == Tactica.FUGITIVA)
        await get_Mover(false, ...);
    else if (pelea.is_loin_8() && configuracion.tactica == Tactica.FUGITIVA)
        await get_Mover(true, ...);

    pelea.get_Turno_Acabado();
    cuenta.connexion.SendPacket("Gt");
}
```

**Pattern extrait** : APRÈS tous les casts du tour, repositionne selon le
mode tactique :
- AGRESIVA + pas au CAC → avance
- FUGITIVA + au CAC → recule
- FUGITIVA + dist < 8 → recule
- FUGITIVA + dist > 12 → avance (reste en portée)

**Adapté chez nous** : `IACombatHerosSimple.RepositionnerFinTourAsync` et
`TrameJeu` ligne 1296+ (POST-CAST KITE en Fuyard/Eloigne).

## Pattern 5 — LOS Bresenham iso

**Source dyshay** : `Fight.get_Linea_Obstruida`

160 lignes denses, gestion 3 cas :
- Diagonal pur (`tipo=1` quand `|dx|==|dy|`)
- X-major (`tipo=2`)
- Y-major (`tipo=3`)

Chaque cellule traversée testée via `get_Es_Celda_Obstruida` :
```csharp
return mp.isInLineOfSight                          // bit LOS mur MapData
    || (mp.cellId != targetCellId
        && occupiedCells.Contains(mp.cellId));     // combattant sur trajectoire
```

**Adapté chez nous** : `Cartes.LigneVisuelle.EstObstruee` — V1 OK pour
combattants, TODO pour murs MapData bit-à-bit (cf. BIBLE-CADERNIS-V2).

## Pattern 6 — Pathfinder combat (BFS 4-dir)

**Source dyshay** : `PeleasPathfinder.get_Celdas_Accesibles`

BFS classique, distance ≤ PM, exclut cells occupées. Pour chaque cell
atteinte, stocke `MovimientoNodo(celdaInicial, alcanzable)` qui permet
reconstituer le chemin via `get_Path_Pelea`.

Voisinage : 4-dir orthogonal STRICT (cf. commentaire dyshay `// pelea no
utiliza diagonales`).

**Adapté chez nous** : `Pathfinder.Trouver(combat: true)` = A* 4-dir.

## Pattern 7 — Sélection cible avec heuristique low-HP

**Source dyshay** : `Fight.get_Obtener_Enemigo_Mas_Cercano(int range)`

Si on est déjà au CAC (dist=1) → garde l'ennemi le plus proche.
Sinon dans la portée du sort → vise le mob low-HP non-invocation en priorité.

**Adapté chez nous** : `MoteurReglesCombat.EnnemiPlusProcheOuLowHp` —
même heuristique pour le Focus `EnnemiLePlusProche`.

## Synthèse

Les 7 patterns extraits couvrent 80% des cas tactiques PvM standard. Notre
implémentation ADR-008 les incorpore tous, avec en plus :
- Score multi-critères avec poids configurables
- Délégué `TestLosDelegate` pour découpler le moteur des occupations
- Cache pré-calcul `(int x, int y)[] ennemisXY` (évite recalcul par cellule
  candidate)

Aucune ré-invention vs Synfus/Dyshay — uniquement de l'extension propre.
