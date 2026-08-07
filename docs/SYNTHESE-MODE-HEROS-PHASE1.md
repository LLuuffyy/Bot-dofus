# Synthèse Phase 1 — Mode héros Abrak

Synthèse des 5 rapports d'analyse de la session du 2026-05-22 (audit code, références externes, architecture, design UI, forensic logs). Doit être lu **avant** Phase 2 (implémentation).

## Documents de référence

| Sujet | Fichier | Auteur |
|------|---------|--------|
| Audit code multi-account actuel | [`AUDIT-MULTI-ACCOUNT-EXISTANT.md`](AUDIT-MULTI-ACCOUNT-EXISTANT.md) | code-analyzer |
| Recherche externe (Synfus / dyshay / web) | [`REFERENCES-MODE-HEROS.md`](REFERENCES-MODE-HEROS.md) | researcher |
| Architecture proposée (ADR-006) | [`ARCHITECTURE-MODE-HEROS.md`](ARCHITECTURE-MODE-HEROS.md) | system-architect |
| Modèle de données | [`MODELE-DONNEES-MODE-HEROS.md`](MODELE-DONNEES-MODE-HEROS.md) | system-architect |
| UI design (XAML squelette) | [`UI-GROUPE-HEROS-DESIGN.md`](UI-GROUPE-HEROS-DESIGN.md) | system-architect |
| Protocole capturé sur Abrak (log 081349) | [`PROTOCOLE-MODE-HEROS-ABRAK.md`](PROTOCOLE-MODE-HEROS-ABRAK.md) | researcher (forensic) |

## Faits confirmés par les logs

### Le serveur cible est Abrak, pas Hystoria

`REFERENCES-MODE-HEROS.md` confirme : Hystoria est **monocompte**, il n'expose pas de mode héros. Le user joue actuellement sur **Abrak** (`abrak.fr/actualites/2-heroes`). Cette session pose les bases multi-perso pour le serveur Abrak — pas pour Hystoria. À mettre à jour dans `CLAUDE.md` si la cible bascule durablement.

### Mode héros = MONO-CLIENT (un seul socket TCP)

`PROTOCOLE-MODE-HEROS-ABRAK.md` capture **13 combats consécutifs** : Beiloddurul (Sadida master, id 401770) + 7 Enutrof liés (ids 401771-401777). Tout transite par **une seule** session proxy (1303/1304). Pas de N sessions parallèles à orchestrer.

### Le serveur joue les héros liés côté serveur

Aucun `GAS`/`GAF`/`Gt` émis par le client pour les liés. Le client (et donc le bot) **n'a à piloter QUE le master** :

- À chaque `GTS<idMaster>|...` → bot joue le tour de Beiloddurul (logique combat solo existante).
- À chaque `GTS<idLié>|...` → bot **skip** (le serveur fait tout). Émet juste un `GT` après le `GTR<idLié>`.

Conséquence majeure : la roadmap finale annoncée (« 8 persos avec rotation/focus chacun ») **n'a pas de sens sur Abrak** — le serveur impose les actions des liés. La seule chose configurable côté bot reste la stratégie du **master**.

### Paquets identifiés (résumé)

| Préfixe | Sens | Sémantique mode héros |
|---------|------|------------------------|
| `GTSX<idMaster>;<idPerso>;0;1;0;<5stats>` | S→C | **Nouveau** — init buffs d'un héros lié, 1 paquet par lié post-`GTL` |
| `GTL\|<id1>\|<id2>\|...` | S→C | Ordre des tours, 9-11 IDs (master + liés + mobs) |
| `GTS<id>\|<timer>\|<num>` | S→C | Tour de quelqu'un (format inchangé vs solo, mais peut être un lié) |
| `GTM\|<entrée>\|...` | S→C | Refresh combattants, 9+ entrées au lieu de 2-4 en solo |
| `GE<xp>;<n>\|<idMaster>\|0\|<bloc1>\|<bloc2>\|...` | S→C | Fin combat **multi-blocs** (1 bloc XP+drops par lié) |

`GTSX` est le préfixe **exclusif au mode héros** (145 occurrences dans le log de capture, 0 dans les autres). C'est le signal de détection le plus fiable.

## Ajustements vs l'architecture proposée

L'ADR-006 (`ARCHITECTURE-MODE-HEROS.md`) anticipait deux topologies (mono-client ET N-clients). Avec les findings forensic, **N-clients est mort sur Abrak** :

| Élément ARCHI initial | Décision après forensic |
|------------------------|-------------------------|
| `GroupeHeros.EstMonoClient` (bool, basculable) | **Toujours `true`** sur Abrak. Garder le champ pour les autres serveurs futurs, mais le code ne crée pas de fantômes. |
| `DispatcheurTours` (route via leader / broadcast passif) | **Simplifié** — un seul mode : « joue le tour si `GTS.IdCombattant == GroupeHeros.IdMaster`, sinon skip + `GT` ». |
| `ContexteCompte` fantômes partageant `SessionProxy` | **Abandonné**. Un seul `ContexteCompte` (celui du master). Les liés sont représentés par de simples `MembreHeros` (POCO, pas un Compte). |
| `peleas/<perso>.json` par lié | **Inutile sur Abrak** (serveur joue les liés). À garder pour les serveurs N-clients. |
| 3 hooks observateurs no-op-safe dans `TrameJeu` | **Confirmés**, mais hooks à compléter pour `GTSX` (init buffs) et nouveau format `GE` multi-blocs. |

L'audit (`AUDIT-MULTI-ACCOUNT-EXISTANT.md`) recommandait d'ajouter les paquets `P*` (PG/PR/PI). Les logs forensic montrent que **le signal de mode héros n'est pas dans `P*`** mais dans `GTSX` (en combat) et probablement dans un `ALK` étendu (hors combat — à confirmer, voir questions ouvertes).

## Plan ajusté Phase 2 → Phase 7

### Phase 2 — Modèle de données (simplifié)

**Nouveaux fichiers** :
- `Divers/MultiAccount/MembreHeros.cs` — POCO `{ IdJeu, Nom, Classe, Niveau, EstMaster, EstVivant, PV/PA/PM }`
- `Divers/MultiAccount/GroupeHeros.cs` — agrégat `{ IdMaster, Membres: List<MembreHeros>, OrdreToursCourant: List<int>, IdActif }`. Events `MembreAjoute`, `MembreRetire`, `TourChange`.
- `Divers/MultiAccount/DetecteurModeHeros.cs` — observe le premier `GTSX` reçu, peuple `GroupeHeros.Membres` à partir des `GTM` qui suivent.
- `Divers/MultiAccount/DispatcheurTours.cs` — `OnGTS(id)` → si `id == IdMaster` : laisse `TrameJeu.JouerTourCombatAsync` faire son boulot. Sinon : `await EnvoyerGTApresGTR(id)`.

**Extension** : `ContexteCompte.GroupeHeros` (nullable, null si pas en mode héros).

**Tests** : modèle pur (pas de réseau), construction/dissolution, ajout/retrait membre, transition de tour.

### Phase 3 — Détection + dispatch

- Parser `GTSX` côté `FabriqueMessages` + `MessageGTSXAbrak`.
- Hook dans `TrameJeu` : à la réception du 1er `GTSX` d'un combat, instancier `GroupeHeros` sur `ContexteCompte`.
- Branch `OnTourCombatAbrak` : si `GroupeHeros != null && IdCombattant != GroupeHeros.IdMaster` → skip + GT après GTR.
- Parser `GE` multi-blocs pour collecter XP/drops par lié (visible côté UI).
- Logs `[MODE-HEROS]` + `[TOUR-DISPATCH]`.

### Phase 4 — Isolation ConfigCombat

Les héros liés étant joués serveur-side, il n'y a **rien à isoler** sur Abrak — seul le master a une `ConfigCombat`. Phase 4 ramenée à un audit + tests qui confirment l'isolation déjà OK (par `peleas/<id>.json`). Pas de nouveau code, juste un assert et un commit léger.

### Phase 5 — UI VueGroupeHeros

Design `UI-GROUPE-HEROS-DESIGN.md` valide. Ajustements :
- Section « Configuration du groupe » : **désactivée** sur Abrak (le groupe est instantié serveur-side, pas configurable côté bot). Garder l'UI mais en mode lecture seule.
- Section « Vue d'ensemble combat » : telle quelle, alimentée par `GroupeHeros.Membres` + `GTM` parsés.
- Section « Ordre des tours » : telle quelle, alimentée par `GTL`.
- Bouton « Configurer combat » par perso : pour Abrak, ouvre la config du master uniquement (les liés sont serveur-side).

### Phase 6 — Intégration onglet

Onglet « Groupe » dans `MainWindow.TabsContent`, liaison `Lier(ContexteCompte)`. Pas de changement par rapport au design.

### Phase 7 — Validation

Build clean, tests passent, push.

## Questions ouvertes (à capturer plus tard)

1. **Détection hors combat** — Le bot peut-il savoir qu'il est en mode héros AVANT le premier combat ? (Sélecteur `ALK0|1|...` est trop succinct.) Hypothèse : un paquet d'enrôlement existe à la connexion, à capturer avec un log au login.
2. **Mort d'un héros lié** — Format pas observé dans le log. `GTF<idLié>` immédiat ? `GTM` avec PV=0 ? Préfixe spécial ? À capturer en jouant un combat où un Enutrof meurt.
3. **Sémantique `NO<n>~<id>;<val>|...`** — 4 occurrences avec valeurs 3/4/0. Initiative dynamique ? compteur de buffs ? À élucider.
4. **`Gd+20;41;0;;10;5;10;5;0;0` + `GdOK41`/`GdKO19`** systématiques — challenge bonus auto Abrak ? Important pour l'XP, à ignorer côté bot mais à logger pour stats.

Ces 4 points sont **non-bloquants pour les phases 2-7**, à traiter dans une session forensic dédiée plus tard.

## Notes annexes

- L'agent ref-externe a créé un outil temporaire de décompilation `.NET` à `C:\Users\touki\AppData\Local\Temp\synfus-typelist\` (hors projet, hors git). À considérer pour de futures recherches mais hors scope projet.
- Le repo `Mélange\synfus-decompile\` (mentionné par ref-externe) n'est pas dans le projet git. Si utile, à indexer plus tard.
- Aucun parser `P*` (PG/PR/PI/PM) n'est nécessaire pour le mode héros Abrak. L'audit avait raison de noter leur absence mais ce n'est **pas** le vecteur de détection — c'est `GTSX`.
