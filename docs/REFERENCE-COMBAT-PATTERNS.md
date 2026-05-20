# REFERENCE — Patterns IA Combat (SynFus / dyshay / Cadernis)

Source synthétique pour le décideur IA combat de Luffy-bot (cf. roadmap CLAUDE.md).
Référence : dyshay `Otros/Peleas/SpellsManager.cs` + `FightExtensions.cs` + `Fight.cs`
(commit Alvaro Prendes 2019, basé sur thread cadernis phylonia #1771 « Bonne IA en combat »).

> **Note SynFus Aqua v1.1.0** : binaire .NET single-file `Synfus.exe` + DLLs ressources framework
> seulement. Pas de fichier config combat exporté visible. Le modèle se déduit de l'UI
> screenshots déjà connue + du fork dyshay (même auteur, salesprendes.com).

---

## ★ 1. Les 4 modes de combat (= dyshay `Tactica` + extension SynFus)

dyshay n'expose que **3 tactiques** (`AGRESIVA / PASIVA / FUGITIVA`). SynFus en ajoute
1 quatrième (`Equilibre/Tactique`) qui maintient une distance préférée. Notre `StrategieCombat`
les couvre toutes (Agressif, Tactique, Defensif, Soutien, Passif, Fugitif).

Algo de **fin de tour** dyshay (`FightExtensions.get_Fin_Turno`, lignes 178-200) :

```
si tactica == AGRESIVA et PAS en CAC → s'approcher (cercano=true)
si tactica == FUGITIVA et en CAC     → fuir (cercano=false)
si tactica == FUGITIVA et ennemi < 8 → fuir tous les PM
si tactica == FUGITIVA et ennemi > 12 → s'avancer (sinon out of range turn after turn)
sinon (PASIVA)                       → ne pas bouger
puis : Gt (pass turn)
```

### Algo de choix de cellule cible (par mode)

`get_Mover` (FightExtensions ligne 203) : itère **toutes** les cellules atteignables via
`PeleasPathfinder.get_Celdas_Accesibles` (= A* en combat, évite combattants vivants),
puis sélectionne :

| Mode | Critère de sélection (parmi cells atteignables) |
|------|--------------------------------------------------|
| **Agressif (CERCA)** | minimise `Σ distance(cell, chaque ennemi) - 1` |
| **Eloigne / Fugitif (LEJOS)** | maximise la même Σ, ET parmi les ex-aequo prend le chemin le **plus long** (utilise tous les PM) |
| **Equilibre/Tactique** (extension) | Pour notre `DistancePreferee=5` : minimise `\|dist(cell, ennemi_le_plus_proche) - 5\|` |
| **Passif (INMOVIL)** | Pas de mouvement — utile en `BloquerLeCombat` |

→ Cf. `Fight.get_Celda_Mas_Cercana_O_Lejana` lignes 279-296 et `Get_Total_Distancia_Enemigo`
ligne 238. Critère unique : somme Chebyshev (« diag = 1 case »).

---

## ★ 2. Algo LOS Bresenham (grille iso 14×40)

dyshay implémente une version modifiée Bresenham qui marche sur les coords **iso** des
cellules (déjà fournies par `Cellule.CalculerCoordonnees(cellId, 14)` chez nous).
Source : `Fight.cs` ligne 417 `get_Linea_Obstruida`. Pseudo-code condensé C# :

```csharp
// Retourne true si la ligne (a → b) est bloquée par un obstacle ou un combattant.
public static bool LigneObstruee(Carte carte, Cellule a, Cellule b, HashSet<int> occupees)
{
    double x = a.X + 0.5, y = a.Y + 0.5;
    double tx = b.X + 0.5, ty = b.Y + 0.5;
    double prevX = a.X, prevY = a.Y;

    double dx = tx - x, dy = ty - y;
    double padX, padY;
    int pasos, type;

    if (Math.Abs(dx) == Math.Abs(dy))         // diagonale parfaite
        { pasos = (int)Math.Abs(dx); padX = Math.Sign(dx); padY = Math.Sign(dy); type = 1; }
    else if (Math.Abs(dx) > Math.Abs(dy))     // x-major
        { pasos = (int)Math.Abs(dx); padX = Math.Sign(dx);
          padY = Math.Ceiling(dy / pasos * 100) / 100; type = 2; }
    else                                       // y-major
        { pasos = (int)Math.Abs(dy); padY = Math.Sign(dy);
          padX = Math.Ceiling(dx / pasos * 100) / 100; type = 3; }

    int errSup = (int)Math.Floor(3 + pasos / 2.0);
    int errInf = (int)Math.Floor(97 - pasos / 2.0);

    for (int i = 0; i < pasos; i++)
    {
        // selon `type` (1=diag, 2=x-major, 3=y-major), avancer (x,y) d'un pas,
        // calculer cell (cellX, cellY) intermédiaire(s) et tester :
        if (CelluleObstruee(cellX, cellY, carte, occupees, destId, prevX, prevY))
            return true;
        prevX = cellX; prevY = cellY;
        x += padX; y += padY;
    }
    return false;
}
```

Particularités importantes (dyshay) :
- Le test **n'inclut PAS la cellule de départ ni d'arrivée** (gérées par `CelluleObstruee` via destId).
- Une cell bloque si `!es_linea_vision` (bit MapData), ou si elle est dans `occupees` (combattants vivants).
- Le décalage `+0.5` centre la trajectoire sur la cellule.

**Bit LOS dans MapData** (cf. `Map.DecompressCell` ligne 172) :
```csharp
bool es_linea_vision = (cellInformations[0] & 1) != 1;
```
→ Stocké pour chaque cellule lors du parsing initial de la map.

---

## ★ 3. Pipeline cast (Lanzar_Hechizo / Mover_Lanzar / Reculer_Et_Lancer)

`SpellsManager.manejador_Hechizos` (lignes 37-56) :

```
si focus == CELDA_VACIA          → lanzar_Hechizo_Celda_Vacia(sort)
si méthode == AMBOS              → get_Lanzar_Hechizo_Simple(sort)
si méthode == ALEJADO et !CAC    → get_Lanzar_Hechizo_Simple(sort)
si méthode == CAC et CAC         → get_Lanzar_Hechizo_Simple(sort)
si méthode == CAC et !CAC        → get_Mover_Lanzar(sort, ennemi_le_plus_proche)
```

`get_Lanzar_Hechizo_Simple` (lignes 58-147) — **cast direct si possible, sinon move+cast** :
1. Check PA / cooldown / max par tour via `get_Puede_Lanzar_hechizo(id)`.
2. Cible = `get_Obtener_Enemigo_Mas_Cercano(range)` (ou allié si focus heal).
3. Si `get_Puede_Lanzar_hechizo(id, ma_cell, cible.cell, mapa) == NINGUNO` → **cast GA300**.
4. Si le seul échec est `NO_ESTA_EN_RANGO` → `get_Mover_Lanzar_hechizo_Simple`.

`get_Mover_Lanzar_hechizo_Simple` (lignes 149-179) — algo critique :
```
pour chaque cell atteignable C (PeleasPathfinder) :
  si méthode == CAC et C n'est pas adjacente à un allié → skip
  si get_Puede_Lanzar_hechizo(sort, C, cible.cell, mapa) != NINGUNO → skip
  → candidat valide : C minimise les PM utilisés
choisit cell avec le MOINS de PM (économise pour cast suivant)
move vers cette cell, puis le pipeline cast est réessayé via l'évènement movimiento
```

→ **Pattern kiting (cast + reculer)** : pas un module séparé chez dyshay. C'est l'algo
de **fin de tour** (`get_Fin_Turno`) qui s'occupe de reculer APRÈS que tous les sorts
ont été tentés. Pour notre IA c'est plus malin de :
- Cast d'abord (boucle sur règles, drain PA)
- Puis si tactica == FUGITIVE/Defensif et il reste des PM → move LEJOS
- Sinon si tactica == AGRESIVO et pas en CAC → move CERCA

---

## ★ 4. Format config SynFus (champs à reproduire dans `ConfigCombat`)

Format **dyshay binaire** (`PeleaConf.guardar`, ligne 29) — champs par compte :

```
Byte    tactica                     // AGRESIVA=0, PASIVA=1, FUGITIVA=2
Byte    posicionamiento             // LEJOS=0, CERCA=1, INMOVIL=2
Bool    desactivar_espectador
Bool    utilizar_dragopavo          // monture
Byte    iniciar_regeneracion        // % PV pour soin
Byte    detener_regeneracion        // % PV jusqu'auquel soigner
Byte    nombre_hechizos
Pour chaque hechizo :
    Int16   id
    String  nombre
    Byte    focus                   // ENEMIGO/ALIADO/ENCIMA/CELDA_VACIA
    Byte    metodo_lanzamiento      // CAC/ALEJADO/AMBOS
    Byte    lanzamientos_x_turno
```

✓ **Déjà couvert** dans notre `ConfigCombat.cs` (JSON camelCase, sérialisation Text.Json).
Mapping :
- `tactica`           → `Strategie` (enum élargi)
- `posicionamiento`   → `Positionnement`
- `desactivar_espectador` → `DesactiverModeSpectateur`
- `utilizar_dragopavo` → `UtiliserMonture`
- `iniciar_regeneracion` → `ConsommableUtiliserSiPvInfPct`
- `detener_regeneracion` → `ConsommableJusquaPvSupPct`

`HechizoPelea` → `RegleSort` :
- `id/nombre`             → `IdSort/Nom`
- `focus`                 → `Focus` (FocusSort)
- `metodo_lanzamiento`    → `MethodeLancement`
- `lanzamientos_x_turno`  → `NombreParTour`

**Extension SynFus** (UI screenshots, non sérialisée chez dyshay mais présente dans SynFus) :
toutes les `Conditions Distance / Cible / Joueur / Situation / Avancées` de `RegleSort.cs`
(DistanceMin/Max, IgnorerCAC, CiblePvInfPourcent, EnnemisMin/Max, TousLesNTours, etc.).
→ ✓ déjà présents dans notre modèle.

---

## ★ 5. Anti-patterns à éviter

1. **Cast trop rapide après GTS** — Cadernis V1 §535-540 et capture user 16:21 : un humain met
   **1.5-1.7 s** entre voir le tour et cliquer. Tout cast < 500 ms après GTS = signal
   anti-bot. → toujours `await Task.Delay(rng.Next(1400, 2100))` avant le 1er GA300.

2. **Pattern réaction immédiate après packet (< 100 ms)** — V3 §547 : marque #1 de bot.
   Mettre une variance ≥ 30 % sur tous les délais : `rng.Next(min, min + min*0.3)`.

3. **Cibler une cellule occupée sans le savoir** — Fight.cs ligne 378 :
   `es_celda_vacia && !es_Celda_Libre(celda_objetivo)` → kick serveur si on lance une invoc
   sur cell occupée. **Toujours filtrer** via `get_Celdas_Ocupadas` (= ennemis + alliés vivants).

4. **LOS oublié → cast échoué → boucle infinie** — Cadernis V1 §533 (m4x0ubot) : la LDV
   est l'item à plus haute priorité de la todolist. Si on caste un sort `NecessiteLOS`
   sans test, le serveur répond `GAF<code>` (échec) et la même règle est retentée à
   l'infini si `RecastSiEchec=true`. → impérativement appeler `LigneObstruee` AVANT le cast
   et incrémenter `hechizo_lanzado_index` même sur échec LOS.

5. **GE/Gt confondus → timeout 45 s/tour** — V1 §502-505 : `Gt` minuscule = pass-turn,
   `GT` majuscule = turn-ready-ack. Confondre les deux = timeout serveur. Déjà fixé
   (commit d4ec747) mais à ne pas re-régresser.

6. **Bouger une seule fois puis cast = monotone** — bots boucle exacte sur 5 maps =
   marque #6 (V3 §552). Pour le combat ça donne : toujours même séquence move→cast→Gt
   sans variance. → injecter occasionnellement (1-2 % via rng) un mouvement « parasite »
   (1 case dans une direction qui ne change rien à la portée).

---

## Annexes utiles (déjà capitalisées chez nous)

- `Cellule.CalculerCoordonnees(cellId, 14)` → `(x, y)` iso (formule dyshay).
- `Distance Chebyshev` = `max(|dx|, |dy|)` (cf. `GetDistanceBetweenCells`).
- `Pathfinder.Trouver(carte, depart, arrivee, interdites)` A*, cell occupées en `interdites`.
- `BaseSorts.Instance.Trouver(id).Stats(niveau)` → stats par niveau (XML dyshay 281 sorts).
- Délai déplacement combat ≈ `400 + 100 × nbCases` ms (V1 §541-544).

Fichiers de référence (lecture en cas de doute) :
- `.claude/worktrees/vigorous-murdock/dyshay-source/Otros/Peleas/SpellsManager.cs`
- `.claude/worktrees/vigorous-murdock/dyshay-source/Otros/Peleas/FightExtensions.cs`
- `.claude/worktrees/vigorous-murdock/dyshay-source/Otros/Peleas/Fight.cs` (ll. 417-580)
- `.claude/worktrees/vigorous-murdock/dyshay-source/Otros/Peleas/Configuracion/PeleaConf.cs`
- `Divers/Combats/IA/ConfigCombat.cs` (modèle JSON actuel — aligné SynFus)
- `Divers/Combats/IA/RegleSort.cs` (règles + conditions)
- `BIBLE-CADERNIS.md` §10-11 (protocole combat + sorts)
- `BIBLE-CADERNIS-V3.md` §14 (anti-bot indicators)
