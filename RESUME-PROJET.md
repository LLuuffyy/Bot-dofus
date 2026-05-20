# 📊 Récapitulatif global Luffy-bot — état au 20/05/2026

## ✅ CE QUI MARCHE (validé en combat ou en run)

### Réseau / infrastructure
- ✅ **Proxy MITM** (`SessionProxy`) : interception des canaux clair + chiffré `'-'` (16 clés AK)
- ✅ **Cipher Hystoria** : injection bot indétectable, `idxProxy` monotone, plus de désync
- ✅ **AYK auth→jeu** : redirection automatique vers le serveur de jeu
- ✅ **Mode passif global** (toggle UI) : bot devient sniffer pur, n'envoie RIEN auto
- ✅ **Reconnexion** : auto-reattach client réel à la sortie/rentrée

### Récolte (le pilier qui marche bien)
- ✅ **Multi-métier** : Bois (Frêne/Châtaignier/...), Blé (Paysan), génériques
- ✅ **Catalogue gfx → skill** : 53 ressources mappées, fallback automatique
- ✅ **Détection succès OQ** : robuste à toute durée (Paysan niv 1 = 12s, niv 100 = 2-3s)
- ✅ **Évite combattants/transitions** : pathfinder filtre `IdInteractif >= 0` et zaaps
- ✅ **Logs `[ACTION]`** propres et lisibles

### Zaap
- ✅ **Skill 114** (PAS 157 doc dyshay) : `GA500<cell>;114 → WC menu → WU<map>`
- ✅ **API Lua** `bot.zaap(mapId)` fonctionnelle

### IA combat (le morceau récent)
- ✅ **Détection tour** : handler Abrak `GTS<id>|<timer>|<num>` branché
- ✅ **Cast simple** : `GA300<id>;<cell>` avec stats par niveau XML dyshay
- ✅ **Pass turn** : `Gt` (minuscule, capture user)
- ✅ **Stats par niveau** : XML dyshay 281 sorts × 6 niveaux → Ronce niv 5 = PA 4, portée 1-8
- ✅ **Déplacement combat** : si ennemi hors portée → A* combat → `GA001` → `GKK0`
- ✅ **Filet whitelist combat** : bloque GA500/WU/etc. pendant combat (anti-kick)
- ✅ **Placement + Prêt auto** en MITM : `Gp<cell>` + `GR1` envoyés par bot
- ✅ **Délai réaction humanisé** : 1.4-2.1s (vrai humain = 1.5-1.7s observé)

### Modèles SynFus/dyshay portés
- ✅ **`ConfigCombat`** : Positionnement, Stratégie, DistancePreferee, Consommable soin
- ✅ **`RegleSort`** : Focus, NombreParTour, MethodeLancement, 18 conditions
- ✅ **JSON persistance** `peleas/<perso>.json`

### UI WPF
- ✅ **TabControl Chat/Console** : séparation propre des logs
- ✅ **17 catégories de logs colorées** : Action, Combat, Trajet, Script, Réseau, etc.
- ✅ **Inventaire par catégorie**
- ✅ **Console virtualisée** (performance bonne sur gros logs)
- ✅ **Checkbox Mode Passif** dans le header

---

## 🟡 EN COURS / À TESTER

### Validation pratique du dernier build
- 🟡 **Combat complet avec déplacement** : pas encore testé sur un combat où le bot doit bouger ET cast (théoriquement OK)
- 🟡 **Stats par niveau correctes** : Ronce niv 5 → 1-8 devrait passer maintenant, à vérifier sur agression réelle
- 🟡 **Pas de double IA** : ContexteCompte legacy désactivé, mais à vérifier qu'il n'y a pas d'effet de bord

---

## 🔴 PROBLÈMES OUVERTS

### 🔴 Bug #1 — Déconnexions récurrentes en combat
**Symptôme** : session terminée 1-3s après `Gt` ou pendant le combat
**Hypothèses non confirmées** :
- Le `Gt` injecté par le bot ne sort pas du proxy MITM (jamais vu en VOCAB C→S dans certains logs)
- Possible interférence relai/inject sur le canal clair (lock `_verrouCs`)
- Possible signature anti-bot Hystoria sur le timing/séquence

**À investiguer** :
- Logger l'exact moment où l'écriture socket part vs quand le serveur répond
- Vérifier le buffer socket réel quand bot injecte clear en parallèle du client
- Tester en client autonome (sans MITM) pour isoler le problème

### 🔴 Bug #2 — Conditions de règles SynFus pas appliquées
**État** : les modèles sont en place (`RegleSort` avec ses 18 conditions) MAIS le décideur actuel (`TrameJeu.JouerTourCombatAsync`) ne les utilise pas. Il fait juste "ennemi le plus proche + sort le plus haut niveau".
**Conséquence** : la config SynFus que je peux écrire à la main est ignorée

---

## 📋 ROADMAP — CE QU'IL RESTE À FAIRE

### Priorité HAUTE (bloquant ou très utile)

1. **🎨 UI WPF Combat** (équivalent screenshots SynFus)
   - Onglet "General" : combobox Positionnement, Style, Distance, Soin
   - Onglet "Sorts" : ListView + form Ajouter/Modifier règle + drag reorder
   - Onglet "Délais" : sliders min/max réaction et entre actions
   - **~4-6h XAML + code-behind**

2. **🧠 Décideur IA refondu** pour appliquer les conditions
   - Pour chaque tour : itérer règles par ordre, vérifier toutes les conditions, choisir première qui passe
   - Compteur `NombreParTour` par règle (reset chaque tour)
   - Compteur `TousLesNTours`, `APartirDuTour` (état global combat)
   - Filtres `DistanceMin/Max`, `IgnorerCAC`, `SeulementCAC`
   - Cibles `CiblePlusFaible`/`CiblePlusForte`, `MesPvInfPourcent`, `PasSiTacle`
   - **~3-4h refonte décideur**

3. **🔍 Investiguer le bug déco combat** (avant tout ajout)
   - Capture réseau Wireshark sur loopback pour voir QUI envoie quoi
   - Comparer manuel vs bot byte-pour-byte
   - Tester sans MITM (client autonome) pour isoler proxy vs comportement bot

### Priorité MOYENNE (qualité de vie)

4. **Multi-cast par tour** : drain PA si sort `maxParTour > 1` (ex: Ronce maxObj 2 = 2 casts/tour si 10 PA)
5. **LDV (LOS) Bresenham** : check obstacles entre caster/cible avant cast si `NecessiteLOS=true`
6. **Consommable de soin** : auto-use Rougely/Pain quand PV < seuil pendant combat
7. **Mode Tactique réel** : maintenir DistancePreferee (kite si trop proche d'un CAC)

### Priorité BASSE (specificities Sadida ou avancé)

8. **Invocations Sadida** : cast La Folle/La Bloqueuse sur case vide en début combat (focus `CelluleVide`)
9. **Kiting** (`Reculer_Et_Lancer` SynFus) : cast puis reculer pour éviter le CAC
10. **Capture âme** : sort spécial pour les âmes mob (objets craft)
11. **Détection protecteur ressource** : priorité haute, fuite ou full DPS selon config

---

## 🔧 LÀ OÙ C'EST COMPLIQUÉ

### Le canal clair vs chiffré en MITM
Hystoria utilise **2 canaux simultanés**. Quand on injecte sur le clair (`Gt`, `GT`, `GR1`), il faut être synchro avec le relai du client réel. C'est probablement la source du bug #1. Le `lock (_verrouCs)` protège l'écriture mais peut-être pas suffisamment.

### Le timing humain
On a observé 1.5-1.7s humain réel entre GTS et action. Trop rapide (3ms) = signal anti-bot. Trop lent = timeout serveur (45s par tour). Notre fenêtre `Random(1400, 2100)` est confortable mais à valider.

### Les stats par niveau changent par classe
On a porté l'XML dyshay (281 sorts) mais Hystoria a 2145 entrées custom. Pour ton perso Sadida niv 13 c'est ok (sorts dans dyshay). Pour des sorts custom Hystoria, on retombera sur les valeurs niv 1 du JSON. À surveiller.

### Le décideur SynFus est complexe
~20 conditions × 7 sorts × N ennemis × N tours = beaucoup d'état à gérer. Faut un système de scoring pas une simple cascade `if/else`. Probablement 200-300 lignes de code propre.

---

## 📂 OÙ EST QUOI

| Quoi | Où |
|------|-----|
| Doc projet générale | `Bot-dofus/CLAUDE.md` |
| Mémoire persistante par sujet | `~/.claude/projects/.../memory/*.md` |
| Code IA combat | `Commun/Frames/TrameJeu.cs` (méthode `JouerTourCombatAsync`) |
| Modèles config combat | `Divers/Combats/IA/{ConfigCombat,RegleSort}.cs` |
| Stats sorts par niveau | `Resources/data/hechizos_dyshay.xml` + `BaseSorts.cs` |
| Logs runtime | `BotDofus.Wpf/bin/Debug/net8.0-windows/logs/` |
| Référence dyshay | `/tmp/dyshay-bot/` (cloné le 20/05) |
| Référence SynFus | `~/Desktop/SynFus_Hystoria_v1.1.7/` (DLL obfusquée) |

---

## 🎯 CE QUE JE PROPOSE COMME PROCHAINE SESSION

1. **Tester le build actuel** sur un combat (avec Ronce niv 5 = 1-8 fix) → valider que ça marche
2. Si déco persiste → **investiguer le canal clair `Gt`** avec Wireshark
3. Sinon → **implémenter UI Combat WPF** (priorité user clair)
4. **Décideur IA refondu** en parallèle (peut se faire sans UI, le JSON suffit pour tester)

---

## 📜 HISTORIQUE DES 22 TÂCHES (cette session)

Pour mémoire, les tâches travaillées le 20/05/2026 :

| # | État | Sujet |
|---|------|-------|
| 1 | ✅ | Recompiler fix resetReel (cipher) |
| 2 | ✅ | Synchro inventaire (UID long) |
| 3 | ✅ | Recherche IA combat + zaaps + ressources |
| 4 | ✅ | Logs/console colorés par catégorie |
| 5 | ✅ | Inventaire WPF en catégories |
| 6 | ⏳ | Analyse sorts + montée de stats propre |
| 7 | ✅ | Brancher IA combat sur event Abrak |
| 8 | ✅ | Conversion cell↔(x,y) + portée sort réelle |
| 9 | ✅ | Crawl cadernis IA combat |
| 10 | ✅ | Déplacement combat (move + cast) |
| 11 | ⏳ | IA multi-cast par tour |
| 12 | ⏳ | LDV (LOS) avant cast |
| 13 | ✅ | Resync Combat.Ennemis depuis GTM |
| 14 | ✅ | Pause script Lua pendant combat |
| 15 | ✅ | Whitelist combat exacte + délai humanisé |
| 16 | ✅ | Mode Passif global |
| 17 | ✅ | Protocole Hystoria : Gp/GR1/GKK0 |
| 18 | ✅ | Bot auto-place+ready en MITM |
| 19 | ✅ | Audit sorts + stats par niveau (SynFus) |
| 20 | ✅ | Whitelist GT + désactiver IA legacy + Ronce niv 5 |
| 21 | ✅ | Modèle dyshay Spell+SpellStats (stats par niveau) |
| 22 | ✅ | Port modèle config combat dyshay/SynFus complet |
