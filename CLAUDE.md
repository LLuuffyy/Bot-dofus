# Luffy-bot — Bot Dofus Retro 1.29 (Hystoria)

Projet C# .NET 8 / WPF d'un bot de farm pour le serveur privé **Hystoria** (Dofus Retro 1.29.x), version Abrak v1.48. Projet d'examen scolaire — toukikigames@gmail.com.

## 🏗 Architecture

```
BotDofus/                                          (lib BotDofus.dll)
├── Commun/
│   ├── Reseau/SessionProxy.cs        ⭐ Proxy MITM, gère cipher Hystoria '-'
│   ├── Reseau/ClientAutonomeAbrak.cs    Client headless (mode auto-farm)
│   ├── Frames/TrameJeu.cs            ⭐ Handler paquets gameplay + IA combat
│   ├── Frames/TrameCombat.cs         Phase combat legacy (placement, tour)
│   └── Messages/{VersServeur,VersClient}/   Modèles paquets Dofus
├── Divers/
│   ├── Combats/{Combat,Combattants}/    État combat runtime
│   ├── Combats/IA/{ConfigCombat,RegleSort,DecideurCombat}.cs
│   ├── Cartes/{Carte,Cellule}.cs        Grille de map (560 cells iso 14×40)
│   ├── Cartes/Deplacement/Pathfinder.cs  A* + GA001 encoding
│   ├── Jeu/Personnage/{Personnage,Spells/BaseSorts}.cs
│   ├── Donnees/{BaseDonnees,CatalogueInteractifs}.cs   Lookups JSON
│   ├── Scripts/Api/{ApiBot,ApiLua}.cs   API Lua exposée aux scripts
│   └── ContexteCompte.cs              Glue : compte ↔ session ↔ api ↔ trames
└── Resources/data/                    JSONs (items, sorts, maps, monsters)

BotDofus.Wpf/                          (WinExe Luffy-bot.exe)
├── MainWindow.xaml(.cs)               Fenêtre principale + checkbox ModePassif
├── Vues/Vue*.xaml(.cs)                Onglets : Dashboard, Combat, Inventaire, etc.
```

## 🔌 Réseau / chiffrement

Le client Dofus Hystoria parle 2 canaux au serveur :
- **Canal CLAIR** : la plupart des paquets de gameplay haut-niveau (`Gp`, `GR1`, `Gt`, `GT`...)
- **Canal CHIFFRÉ '-'** : préfixe `-<hex>` pour les actions sensibles (`GA001`, `GA300`, `GA500`, `GA907`, `GKK0`)

Le proxy MITM (`SessionProxy`) :
1. Écoute sur `127.0.0.1:1303` (auth) et `0.0.0.0:1304` (jeu)
2. Relaye les paquets entre client réel ↔ serveur Hystoria (51.89.153.20)
3. Maintient un compteur `idxProxy` monotone pour le canal '-' (16 clés AK rotatives)
4. Re-chiffre les paquets injectés par le bot avec le bon index → indétectable côté serveur

⚠️ Le client Hystoria détecte la désynchronisation cipher → kick. Toute injection cipher DOIT passer par `_canalAbrak.EnvoyerCsVersServeur` sous `_verrouCs` (atomique).

### AYK — handshake auth→jeu
Format : `AYK<ip|hostname>:<port>;<ticket>` → résolution DNS + ouverture proxy sortant vers le serveur jeu.

## 🎮 Protocole 1.29 Hystoria — table définitive

Capturée via combat manuel user en mode passif (16:21-16:24). **À utiliser comme référence**.

### Client → Serveur
| Phase | Paquet | Exemple |
|-------|--------|---------|
| Agression groupe | `GA907<cell>;<idGroupe>` | `GA907303;-554` |
| Créer combat | `GC<type>` | `GC1` |
| **Placement** | **`Gp<cell>`** ⚠️ minuscule | `Gp299` |
| **Prêt** | **`GR<1/0>`** | `GR1` (prêt) / `GR0` (annule) |
| Turn ready ack | `GT` (majuscule) | `GT` après GTF/GTR serveur |
| Cast sort | `GA300<sortId>;<cell>` | `GA300183;185` |
| **Ack action** | **`GKK0`** | après chaque GA001/GA300/GA500 |
| Déplacement | `GA001<chemin>` | `GA001afybfNbf2dge` (format dyshay) |
| **Pass turn** | **`Gt`** ⚠️ minuscule | `Gt` |
| Quitter combat | `GEV` | `GEV` |
| Récolte | `GA500<cell>;<idSkill>` | `GA500297;114` (zaap) |
| Téléport zaap | `WU<mapId>` | `WU10317` |

### Serveur → Client
| Paquet | Sens |
|--------|------|
| `GTM\|<id>;<eq>;<pv>;<pa>;<pm>;<cell>;;<pvmax>\|...` | Liste combattants Abrak |
| `GTS<id>\|<timerMs>\|<numTour>` | Tour de quelqu'un |
| `GAS<id>` / `GAF<code>\|<id>` | Action début/fin |
| `GTF<id>` / `GTR<id>` | Tour fini / prêt |
| `GR<1/0><id>` | Confirmation prêt |
| `Im<niv><code>;<args>` | Info serveur (`Im0152;...` = changement map) |
| `OQ<perso>\|<uidObjet>,<quantite>` | Loot quantity update |
| `IQ<perso>\|<quantite>` | Info quantity (compteur récolte) |
| `GDF\|<cell>;<etat>;0` | État interactif (2=encours, 3=épuisé, 5=dispo) |
| `JN<jobId>\|<niveau>` | Level up métier |

### Pièges connus
- `Gp` minuscule (PAS `GP`)
- `Gt` minuscule (PAS `GE` — l'ancien code était faux, timeout 45s/tour)
- `GR1`/`GR0` (PAS `GRK`/`GRF`)
- `GT` est turn-ready-ack, `Gt` est pass-turn → JAMAIS confondre
- `GKK0` est ack action générique, **obligatoire** après chaque action sensible

## 🗺 Grille de combat / pathfinding

- 560 cellules en grille **iso losange 14×40** (pas linéaire !)
- `Cellule.CalculerCoordonnees(cellId, 14)` → `(x, y)` (formule dyshay)
- Distance **Chebyshev** (cases diag = 1) — `max(|dx|, |dy|)`
- Pathfinder A* dans `Pathfinder.Trouver(carte, depart, arrivee, interdites)`
- Encodage chemin GA001 : `<dir>(a-h)<cell-2chars>` par changement de direction
- En combat, les **cellules occupées par combattants** = obstacles

## ✨ Système de sorts

### Source de vérité : `Resources/data/hechizos_dyshay.xml` (841 KB)
Cloné depuis [dyshay/Bot-Dofus-Retro](https://github.com/dyshay/Bot-Dofus-Retro). Format :

```xml
<HECHIZO ID="183">
  <NOMBRE>Ronce</NOMBRE>
  <NIVEL NIVEL="1" COSTE_PA="5" RANGO_MINIMO="1" RANGO_MAXIMO="6" ... />
  <NIVEL NIVEL="2" ... />
  ...
  <NIVEL NIVEL="5" COSTE_PA="4" RANGO_MINIMO="1" RANGO_MAXIMO="8" ... />
  <NIVEL NIVEL="6" COSTE_PA="3" RANGO_MINIMO="1" RANGO_MAXIMO="8" ... />
</HECHIZO>
```

**281 sorts avec stats par niveau (1-6)**. Chargé par `BaseSorts.Charger()`.

### Modèle C# (= dyshay/SynFus)
```csharp
InfoSort {
    int Identifiant, string Nom, string Description, CategorieSort Categorie
    Dictionary<int, StatsNiveau> StatsParNiveau    // niv → stats
    StatsNiveau Stats(int niveau)                  // resolver avec fallback
}

StatsNiveau { CoutPA, PorteeMin, PorteeMax, LigneSeule, NecessiteLOS, ... }
```

**Toujours** lire les stats via `sort.Stats(niveauAppris)`, jamais `sort.CoutPA` directement (= valeurs niv 1, foireux).

### Aussi disponibles
- `Resources/data/spells_stats_hystoria.json` (440 KB, 2145 entrées) — variants Hystoria custom (mais 1 entrée par sort, pas par niveau)
- `Resources/data/spells_fr.json` (160 KB) — descriptions FR pour la classification offensif/soin/buff/etc.

## ⚔ IA combat

### Pipeline actuel (`TrameJeu.JouerTourCombatAsync`)
```
[GTS<moi>]                                          (serveur : mon tour)
   ↓ délai humanisé 1.4-2.1s
[COMBAT] >>> Mon tour : cell X, PA=Y, PM=Z, ennemis=N
   ↓
trouver ennemi vivant le plus proche
   ↓
filtrer sorts offensifs (par niveau, PA, portée, LOS si requis)
   ↓
si sort en portée → cast direct
sinon → TrouverApprocheCombat : pathfinder A* vers cell en portée du sort, max PM
        envoyer GA001<chemin> → attente (nbPas × 330ms) → GKK0
        puis cast
   ↓
GA300<sortId>;<cellEnnemi>
   ↓ 300-500ms
GKK0
   ↓ 1000-1500ms
Gt
   ↓
[GTF<moi>] [GTR<moi>] (serveur)
   ↓
GT (turn ready ack)
```

### Filet de sécurité — `ApiBot.EnvoyerHumaniseAsync`
Quand `EtatJeu.Combat.Etat != Inactif`, seuls les paquets de combat sont laissés passer :
```csharp
GA300, GA001, Gt, GT, GTR, Gp, GP, GR, GKK0, GQ
```
Tout le reste (GA500 récolte, WU zaap...) est bloqué silencieusement. Évite les kicks « action interdite pendant combat ».

### Config combat (par perso) — `peleas/<perso>.json`

Modèle complet SynFus/dyshay porté dans `ConfigCombat` + `RegleSort` :
- **General** : Positionnement (PasDeDeplacement/PresEnnemis/LoinEnnemis), Stratégie (Tactique/Agressif/Defensif/Soutien/Passif/Fugitif), DistancePreferee, BloquerCombat
- **Sorts** : List<RegleSort> ordonnée — chaque règle a Focus, NombreParTour, MethodeLancement (CAC/Distance/LesDeux), 18 conditions optionnelles (Distance/Cible/Joueur/Situation/Avancées)
- **Consommable de soin** : ConsommableSoinIdTemplate + seuils PV % + délais ms

### Moteur de règles SynFus (`Divers/Combats/IA/MoteurReglesCombat.cs`)

Pipeline depuis le commit `043b0c0` (Phase 3) :

```
ContexteCompte charge JSON → compte.ConfigCombat
   ↓
TrameJeu.JouerTourCombatAsync (sur GTS<moi>)
   ↓
MoteurReglesCombat.Evaluer(combat, cfg, sortsAppris)
   itère les règles par Priorite décroissante :
     1. sort connu (BaseSorts) + appris (niv > 0) ?
     2. NombreParTour : compteur Combat.CompteursRegleParTour pas atteint
     3. Joueur : MesPv%, SiInvocPresente
     4. Situation : EnnemisMin/Max, PremierTour, APartirDuTour, TousLesNTours
     5. PA disponibles (au niveau appris XML dyshay)
     6. Choix cible selon Focus (avec override CiblePlusFaible/Forte)
     7. Cible : CiblePv%
     8. Portée (au niveau appris) + Distance SynFus
     9. IgnorerCAC / SeulementCAC / MethodeLancement
   → ResultatRegle (sort, cible, dist, stats au niveau) OU null
   ↓
si ResultatRegle → ExecuterRegleAsync : GA300 → GKK0 → Gt (timing humain)
                   + Combat.CompteursRegleParTour[idSort]++
sinon            → fallback legacy (ennemi le plus proche + sort haut niv + déplacement A*)
```

**Conditions reportées** (non implémentées Phase 3) :
- `PasSiTacle` : besoin détection effet tacle GAS/GA
- `DernierTour` : pas d'info serveur a priori
- `ElementRequis` : besoin scan effets sort
- `EviterZone` : besoin grille combat

→ couvre ~80% des cas SynFus standard. UI WPF onglet Combat à venir pour éditer sans toucher `peleas/<perso>.json` à la main.

## 🌳 Récolte

### Lookup gfx → skill
`CatalogueInteractifs.SkillCompatible(gfx, skillsPerso)` mappe les gfx d'interactifs (frêne, blé, lin...) → skill métier compatible avec ce que le perso connaît. 53 ressources multi-skills (ex: Lin gfx 7513 → skill 68 Cueillir OU 50 Faucher).

### Détection « ma récolte a réussi »
`Personnage.NbLootsRecus` (compteur monotone OQ) — incrémenté à chaque OQ reçu. La boucle `RecolterTout` attend que ce compteur augmente OU que `Cellule.RessourceDisponible` flip false + 2s → robuste à la durée variable (Paysan niv 100 = 2-3s, niv 1 = 12s).

### Workflow Lua typique
```lua
recolter_tout_bois()                -- récolte Frêne/Châtaignier/...
recolter_tout_ble()                 -- Paysan
recolter_tout_metier("Paysan")      -- générique
bot.zaap(10317)                     -- téléport via zaap gfx 7000 skill 114
```

## 🌀 Zaap / téléport

`UtiliserZaapAsync(mapDestination)` :
1. Localise cell zaap (gfx 7000) sur la carte courante
2. Pathfinder vers la case adjacente (le serveur refuse de marcher SUR le zaap)
3. `GA500<celluleZaap>;114` — skill 114 (PAS 157 doc dyshay) côté Hystoria
4. Attente serveur → menu `WC|<mapId>;<cost>|...` reçu (≈1.5s)
5. `WU<mapDestination>` (MAJUSCULE) pour sélectionner
6. Confirmation `WV` reçue → bot téléporté

## 🖥 UI WPF

- **MainWindow.xaml** : header avec `ChkModePassif` (checkbox qui désactive toute action auto), liste comptes, contrôles proxy
- **VueDashboard.xaml** : console avec **2 onglets** :
  - **Chat** : messages de jeu (`[:]` `[^]` `[%]` `[$]` `[?]` `[#]` `[!]` `[*]` `[SERVEUR]`) + `[ACTION]` du bot
  - **Console** : tout le technique/debug (paquets, IA, erreurs)
- **VueCombat / VueInventaire / VuePersonnage / VueMap / VueScripts / VueTools** : un onglet par sujet

### Catégories de logs (`Utilitaires/Journaux/CategorieLog.cs`)
Tags → catégorie + couleur hex :
- `[ACTION]` → Action (vert)
- `[COMBAT]` → Combat (rouge)
- `[ZAAP]` `[ANKA]` → Trajet
- `[LUA]` `[SCRIPT]` → Script
- `[GA0]` `[CARTE]` `[MAP]` `[GDF]` → Jeu (Debug par défaut, anti-spam)
- Etc.

## 🔐 Mode Passif (toggle)

Quand `Compte.ModePassif == true` :
- `TrameJeu.JouerTourCombatAsync` : skip immédiat (le user joue à la main)
- `ApiBot.EnvoyerHumaniseAsync` : bloque tout envoi
- `ContexteCompte` placement+ready handler : skip
- `Humaniseur.Actif` : false

→ Bot devient sniffer pur. **Indispensable pour capturer le protocole** sans interférence. La checkbox `ChkModePassif_Toggle` propage à tous les comptes.

## 🛠 Build & dev

```bash
# Build (depuis racine projet)
dotnet build BotDofus.Wpf/BotDofus.Wpf.csproj -c Debug --nologo -v minimal

# Exécutable : BotDofus.Wpf/bin/Debug/net8.0-windows/Luffy-bot.exe
# ⚠️ FERMER Luffy-bot.exe avant rebuild complet (sinon fichier verrouillé)

# Logs : BotDofus.Wpf/bin/Debug/net8.0-windows/logs/botdofus-YYYYMMDD-HHMMSS.log

# Ressources data copiées automatiquement au build (csproj <CopyToOutputDirectory>):
#   Resources/data/*.json, Resources/data/*.xml, Resources/worldmap/*
```

### Lancement local Dofus
- Client réel se connecte sur `127.0.0.1:1303` (auth, redirigé) puis `0.0.0.0:1304` (jeu)
- Proxy MITM intercepte tout, déchiffre canal '-', re-chiffre les injections
- Hostname distant : Hystoria (51.89.153.20 + DNS)

## 📚 Ressources externes

- **cadernis.fr** (login Toukiki) — forum francophone, tutos protocole 1.29, source par excellence
  - Threads clés : « Bonne IA en combat » (phylonia #1771), « Modif m4x0ubot » (phylonia #1766), « Sockets 1.29 problème déplacement » (wukzu #1585), « Problème Paquet GA » (alllstars #2392)
- **dyshay/Bot-Dofus-Retro** (GitHub) — code source open-source, base de SynFus, **référence absolue** pour le protocole et la structure des sorts
- **SynFus_Hystoria_v1.1.7** (`~/Desktop/`) — bot commercial Hystoria, DLL obfusquée mais XML data identique à dyshay

## 🐛 Bugs historiques & fixes

| Bug | Cause | Fix |
|-----|-------|-----|
| Cipher desync (kick login) | idxProxy figé après injection | `resetReel + _akRenegocie` dans SessionProxy |
| Inventaire vide | UID objet > Int32, marker `O` non strippé | `long` + strip + `OQ` parser |
| « Couper bois ? » muet | BDD interactifs vide, fallback skill 45 sur arbres | `CatalogueInteractifs.SkillCompatible` 53 entrées |
| Skill 53 disparu | 2e JSK partiel après map → écrasement | UNION au lieu de Replace |
| Saut sans progrès en récolte | Pathfinder transitait par arbres (Type=Marchable) | Filter `IdInteractif >= 0` |
| Récolte min 3s figée | Niveau métier 100 = 2-3s réels | Removed min, OQ-driven exit |
| Combat passe turn 45s | `MessageJeuFinirTour.Prefixe = "GE"` | `"Gt"` (capture user 12:41) |
| IA combat muette | Branchée sur `MessageTourCombat` classique | Brancher aussi sur `MessageTourCombatAbrak` |
| Cible foireuse en combat | `Combat.Ennemis` contenait mobs map | `OnCombattantsAbrak.RemoveAll(ids absent du GTM)` |
| Kick après cast | GKK0 envoyé par boucle récolte pendant combat | Whitelist combat (`EstPaquetCombatAutorise`) |
| `GT` bloqué = kick | Whitelist trop stricte (`"GK"` matchait GKK0) | Tests exacts |
| Ronce niv 5 rejeté à dist 7-8 | JSON stocke stats niv 1 (1-6) | Chargement XML dyshay avec stats par niveau |
| Bot ne place/ready pas en MITM | `if (SessionJeuActive != null) return;` legacy | Bypassed, mode actif = full auto |
| Double IA combat | ContexteCompte legacy + TrameJeu nouveau | ContexteCompte legacy désactivé |

## 🎯 Roadmap

- [x] Cipher MITM stable
- [x] Récolte auto avec OQ + path
- [x] Zaap via skill 114
- [x] IA combat basique (cast + Gt)
- [x] Stats par niveau (XML dyshay)
- [x] Déplacement combat (move + cast)
- [x] Mode passif global
- [x] Décideur IA refondu avec conditions SynFus (Focus, NombreParTour, Distance, Méthode, PV%, tours, ennemis) — moteur règles `MoteurReglesCombat` Phase 1/2/3
- [ ] UI WPF onglet Combat (équivalent SynFus : tableau sorts + form conditions + consommable soin)
- [ ] Multi-cast par tour (drain PA, respecte maxParTour)
- [ ] LDV (Bresenham) avant cast si NecessiteLOS
- [ ] Kiting (cast + reculer après)
- [ ] Invocations Sadida (cast La Folle/La Bloqueuse case vide en début combat)
- [ ] Capture âme (sort spécial pour les âmes mob)

## 📝 Conventions de code

- **C# 12 / .NET 8** ; `Nullable` enable ; `ImplicitUsings` enable
- Namespace `BotDofus.*` (filetop, sans bloc)
- Logs : `Journaliseur.Info/Debogue/Avertir/Erreur(msg, ex?)` ; tags `[CATEGORIE]` en tête
- Tâches : `Task` + `async/await` + `ConfigureAwait(false)` partout (pas WPF UI context)
- Commits : message FR concis (style « fix: ... », « feat: ... »), HEREDOC + `Co-Authored-By: Claude Opus 4.7`
- **NE JAMAIS** : `git --no-verify`, `git push --force` sur main, modifier `.git/config`, créer doc sans demande, écrire un fichier hors arborescence (`/Divers`, `/Commun`, `Resources/`, `BotDofus.Wpf/`)
- **TOUJOURS** : Read avant Edit, créer NOUVEAU commit (pas amend), tester build avant report, garder les fichiers sous ~500 lignes, valider les entrées aux frontières (parseurs paquets, JSON, XML)

---

## 🤖 Coordination multi-agents (Ruflo / claude-flow)

Le projet est configuré avec Ruflo (`.claude-flow/`, `.mcp.json` — gitignorés). Sur les tâches multi-fichiers ou complexes, utiliser les sous-agents et outils MCP plutôt que tout faire en mono-thread.

### Quand swarmer

- **OUI** : 3+ fichiers, nouvelle feature transverse (ex. décideur IA refondu, onglet UI Combat), refactor cross-module (ex. unification TrameCombat/TrameJeu), audit sécurité protocole, recherche cadernis multi-threads
- **NON** : single-file edit, 1-2 lignes, ajustement log, fix typo, question simple

### Pattern « SendMessage-First »

Les agents nommés s'envoient des messages directement, pas de polling partagé. Spawner TOUS les agents dans **un seul** message avec `run_in_background: true`, puis kicker via `SendMessage`.

| Pattern | Flow | Cas d'usage Bot-dofus |
|---------|------|------------------------|
| **Pipeline** | A → B → C → D | Décideur IA refondu : researcher (dyshay/SynFus) → architect → coder → tester |
| **Fan-out** | Lead → A, B, C → Lead | Recherche parallèle cadernis (threads IA, déplacement, GA-cipher) |
| **Supervisor** | Lead ↔ workers | Refonte UI WPF onglet Combat (vue + binding + form conditions en // ) |

### Routing typique (projet)

| Tâche | Agents | Topologie |
|-------|--------|-----------|
| Fix protocole / paquet | `researcher`, `coder`, `tester` | hierarchical |
| Nouvelle IA / décideur | `system-architect`, `coder`, `tester`, `reviewer` | hierarchical |
| Refonte UI WPF | `system-architect`, `coder`, `reviewer` | hierarchical |
| Perf (pathfinder, parseur) | `performance-engineer`, `coder` | hierarchical |
| Audit cipher / MITM | `security-architect`, `security-auditor` | hierarchical |

### Règles agents

- TOUJOURS nommer l'agent (`name: "role"`) pour qu'il soit adressable par `SendMessage`
- TOUJOURS inclure dans le prompt : à qui envoyer le résultat, et quoi
- Après spawn : STOP, informer l'user de ce qui tourne, attendre les retours (pas de polling)
- Lire les `[INTELLIGENCE]` dans les `<system-reminder>` avant de démarrer — Ruflo propose souvent un pattern déjà vu

---

## 🧠 Mémoire & apprentissage (Ruflo MCP)

### Avant une tâche non-triviale
Utiliser `ToolSearch` pour charger les outils MCP Ruflo (préfixe `mcp__ruflo__`), puis :

```
mcp__ruflo__memory_search       — patterns déjà appris
mcp__ruflo__hooks_route         — routing auto vers le bon agent
mcp__ruflo__agentdb_pattern-search — recherche patterns vectoriels HNSW
```

### Après un succès (à appeler explicitement)
```
mcp__ruflo__memory_store        — capitaliser ce qui a marché
mcp__ruflo__hooks_post-task     — clôturer + store-results
```

### Workers en arrière-plan utiles

| Worker | Quand le déclencher |
|--------|---------------------|
| `audit` | Après changement MITM / cipher / parseur paquet |
| `optimize` | Après touche au pathfinder, à la boucle récolte, ou à l'IA combat |
| `testgaps` | Après ajout d'une feature (sorts, zaap, scripts Lua) |
| `map` | Après 5+ fichiers modifiés (sync codebase index) |
| `document` | Après changement de l'API Lua exposée |

### Outils MCP Ruflo — catégories clés

| Catégorie | Outils principaux |
|-----------|-------------------|
| **Mémoire** | `memory_store`, `memory_search`, `memory_search_unified`, `memory_import_claude` |
| **Swarm** | `swarm_init`, `swarm_status`, `swarm_health` |
| **Agents** | `agent_spawn`, `agent_list`, `agent_status` |
| **Hooks** | `hooks_route`, `hooks_post-task`, `hooks_worker-dispatch` |
| **Sécurité** | `aidefence_scan`, `aidefence_is_safe`, `aidefence_has_pii` |
| **Hive-Mind** | `hive-mind_init`, `hive-mind_consensus`, `hive-mind_spawn` |

### 3 tiers de routing modèle

| Tier | Handler | Cas d'usage |
|------|---------|-------------|
| 1 | Agent Booster (WASM) | Transformations triviales (rename, format) — skip LLM, Edit direct |
| 2 | Haiku | Petites tâches déterministes (parseur additionnel, log tag) |
| 3 | Sonnet/Opus | Archi, sécu (cipher MITM), refonte IA combat, raisonnement |

### CLI claude-flow (optionnel, hors-projet)

Les fichiers `.claude-flow/`, `.mcp.json` et `CLAUDE-flow` sont gitignorés (commit `13264e6`). Le projet **reste un projet .NET / WPF** — les CLI `npx @claude-flow/cli@latest` sont des outils dev locaux, pas des dépendances du build.

```bash
npx @claude-flow/cli@latest swarm init --topology hierarchical --max-agents 8 --strategy specialized
npx @claude-flow/cli@latest memory search --query "[mots-clés]" --namespace patterns
npx @claude-flow/cli@latest hooks route --task "[description]"
```

---

## 👤 Contexte utilisateur

- Étudiant, projet d'examen scolaire
- Personnage Hystoria : **Beiloddurul** (Sadida), niveau 13 actuel
- Sorts appris : 192 (Ronce Apaisante), 193 (La Bloqueuse), 195 (Larme), 198 (Sacrifice Poupesque), 182 (La Folle), 183 (Ronce **niv 5**), 200 (Poison Paralysant)
- Compte secondaire (mode tank/leech) : Aerawiol, Vrottigrat, etc.
- Compte cadernis dédié : `toukiki83@gmail.com`
- Préfère WPF (PAS revenir en WinForms)
- Communication FR
