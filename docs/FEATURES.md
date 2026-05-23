# Luffy-bot — Features inventory

Inventaire des features actuellement implémentées et de leur statut.

Dernière maj : 2026-05-23

## ✅ Implémentées et stables

### Combat IA tactique
- **Moteur règles SynFus/dyshay** (`Divers/Combats/IA/MoteurReglesCombat.cs`) — priorités, conditions (Focus/PV%/tour/ennemis), multi-cast ≤8 par tour
- **Moteur tactique** (`MoteurTactique.cs`) — score multi-critères, kite intelligent (porteeMax + PM_ennemi), LOS combatants
- **12 presets de classe** (`ServiceConfigsHeros.PresetParClasse`)
- **Ordre cast → move** : préserve le tacle CAC, repositionne après les casts
- **GKK0 systématique avant Gt** : débloque tour après déplacement post-cast
- **Cooldown mort 30s** : anti-spam Sacrifice Poupesque
- **Relax LOS** : repositionne même si dist OK quand LOS bloquée

### Panneau délais SynFus
- **ConfigDelaisCombat** : 14 plages min/max éditables
- **Profils** : HumainNormal / Rapide / UltraRapide / Custom
- **UI Expander** dans VueCombat avec auto-save

### Farming
- **`EngagerCombatAsync` / `EngagerGroupeAsync`** : engage le groupe le plus proche
- **GA907 timing adaptatif** : `(cases-1) × 350 + 500 ms`, clamp 250-10000ms
- **Blacklist temporaire** : 3 timeouts → blacklist 45s (anti-retry-infini)
- **MonstreLePlusProche** : filtre `EstGroupeAttaquable` (id < 0 uniquement)

### Banque auto
- **PiloteBanque** : workflow complet (trajet → dépôt → retour)
- **Trigger automatique** : déclenche quand poids ≥ seuil configuré
- **Configurable** : ConfigBanque par perso (MapBanqueId, seuil, cible)

### Caractéristiques auto (NOUVEAU)
- **Distribution auto au level-up** : selon répartition % configurée
- **Paliers Dofus 1.29** : Vita 1pt, Sagesse 3pt, F/I/C/A 1-2-3-4pt
- **Modes** : Manuel / Preview / Automatique
- **12 presets classe** : Feca tank, Iop force, Cra agi, etc.
- **Sécurité** : Mode Manuel par défaut, mode Preview pour test sans envoi

### Stats session
- **Compteurs live** : kamas/h, xp/h, combats/h, ratio victoire
- **Affichés dans Dashboard** avec ratios par heure

### Mode héros Abrak
- **`IACombatHerosSimple`** : IA dédiée pour héros liés (multi-perso combat)
- **Compteurs cloisonnés par caster** `(idCaster, idSort)` — pas de blocage cross-perso
- **Auto-invite** : `AutoInviteurHeros` pour les vrais PJs externes

### Sécurité / robustesse
- **Thread-safety Inventaire** : lock UI/réseau (fix crash)
- **Détection déconnexion** → alerte Discord
- **Détection level-up** → notification Discord
- **Détection banque pleine** → notification Discord

### Network / Protocole
- **Proxy MITM** : `SessionProxy` (cipher Hystoria, canal '-' réenc)
- **Pipeline déplacement combat** : event-driven `MouvementBotConfirme`
- **AttendreDebutCombatAsync** : timeout 4s + diag enrichi

### UI WPF
- **VueDashboard** : stats live, console couleur (chat / technique)
- **VueCombat** : config règles SynFus + panneau délais avancé
- **VueInventaire** : grille avec snapshot atomique
- **VueMapViewer** : overlay combat (portée sort jaune, cells atteignables vert)
- **VueBanque** : config dépôt auto
- **VueGroupeHeros** : config invitation héros
- **VueScripts** : Lua editor + runner

### Scripts Lua
- **`ApiLua`** : 60+ helpers (combat, farm, zaap, banque)
- **`ApiAnka`** : compat scripts Anka-Bot
- **EnregistreurTrajet** : record/replay trajectoires GA001

## 🟡 Partiellement implémentées

| Feature | Statut | Gap |
|---------|--------|-----|
| LOS Bresenham | Partiel | Murs/decor MapData non décodés (TODO 4-6h) |
| Preset Sadida (classe 10) | Partiel | Manque 193 (Bloqueuse), 195 (Larme), 198 (Sacrifice Poupesque) |
| Encodage pathfinder | Suspect | Caractères majuscules dans GA001 non confirmés |
| UI caracs auto | Backend OK | Pas de sliders UI — éditer `caracs/<perso>.json` à la main pour l'instant |

## ❌ Non implémentées (roadmap)

### P1 — Critique
- **Auto-reconnexion** sur déconnexion (doc TODO `docs/TODO-AUTO-RECONNEXION.md`)
- **Détection CAPTCHA Hystoria**
- **Mode burst combat** (skip attentes broadcasts en ULTRA TURBO) — gain ~50%
- **Système HDV** vente auto (paquets EHe/EHm/EHM/EHs à confirmer)

### P2 — Utile
- **Mode HEADLESS pur** (= synfus/dyshay) — combats 1-2s
- **Multi-perso combos** (Sadida pose Folle → tous tirent dessus)
- **Web dashboard distant**
- **Replay visuel combat .gif**
- **Auto-loot drag-and-drop UI**

### P3 — Nice-to-have
- **Auto-craft métiers**
- **Auto-bourse HDV**
- **Détection automatique classe** au démarrage
- **Coach mode** (suggère sort mais user clique)

## 📁 Configurations par perso

| Fichier | Description |
|---------|-------------|
| `peleas/<perso>.json` | Config combat IA (règles, mode, délais) |
| `banque/<perso>.json` | Config dépôt banque auto |
| `caracs/<perso>.json` | Config répartition caracs (NOUVEAU) |
| `multi-account/<perso>.json` | Config invitation héros |
| `interactions/<perso>.json` | Config interactifs récolte |

## 🎯 Build & tests

- **Build** : `dotnet build BotDofus.sln -c Debug --nologo -v minimal`
- **Tests** : `dotnet test BotDofus.Tests/BotDofus.Tests.csproj --nologo`
- **Tests count** : 107/107 verts (au 2026-05-23)
- **Warnings** : 1 résiduel (CS1998 SessionProxy:121, méthode async sans await — design intentionnel)
