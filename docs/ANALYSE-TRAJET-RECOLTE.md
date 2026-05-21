# Analyse forensique — Session farm autonome 2026-05-21 12:56 → 16:06

**Fichier log :** `BotDofus.Wpf/bin/Debug/net8.0-windows/logs/botdofus-20260521-125616.log` (4.5 MB, 28 574 lignes)
**Durée session :** ~3h10 (12:56:17 boot → 16:06:00 proxy stop)
**Perso :** Beiloddurul (id 401770) — Sadida niv 13
**Config combat :** `peleas/test.json` (mode Agressif, placement PasDeDeplacement, distance préférée 4)
**Sorts appris (paquet SL #156 et #2201) :** `#192 niv1, #193 niv1, #195 niv1, #197 niv1, #198 niv1, #182 niv1, #183 niv5, #200 niv1`

---

## 1) Stats combats globales

| Métrique | Valeur |
|---|---|
| Combats déclenchés (`[COMBAT] N allié(s) vs M ennemi(s)`) | **35** |
| Combats terminés (`[COMBAT] Combat terminé`) | **35** |
| Défaites / morts (`[MORT] Beiloddurul est mort`) | **1** (à 13:02:15, combat #1) |
| Victoires | **34** |
| GAF échec serveur (`code=2 hors portée`) | 70 |
| Durée combat moyenne | **20.5 s** (min 4.2 s — max 74.6 s) |
| Tours joués (mes `TOUR-START`) | **88** |
| Tours pass turn (`ACTION Passe le tour`) | **88** (= tous correctement passés) |
| Timeouts ACTION-MV (pré-mouvement) | 2 → fallback secours OK |
| Timeout ZAAP | 1 (13:17:39, map 10317 GDM jamais reçu) |
| Désync cipher / kicks | **0** (zéro `kick`/`déconnecté`, 1 seul `ÉCHEC 'Gv'` à la fermeture session) |

**Composition combats :** 24 sur 35 = `1 allié vs 5 ennemis` (protecteurs de ressources Hystoria — agros multi-mob standards).

**Distribution monstres engagés (extrait `[FARM] cible`) :** Petit Tournesol Sauvage, Épouvantail d'Incarnam, Arakne Immature, Jeune Boufton Blanc, Petit Bouftou.

---

## 2) ⚠️ La Folle (#182) — PRIORITÉ ABSOLUE — cause racine

### 2.1 Verdict : sort 182 n'est JAMAIS cast — pas un bug de moteur, un bug de **config**

| Vérification | Résultat |
|---|---|
| Sort 182 appris par le perso | ✅ OUI (`#182 niv1` dans `[SORTS] 8 sort(s) scanné(s)` à 12:56:46 et 13:09:05) |
| Sort 182 envoyé au serveur (`GA300182;...`) | ❌ **0 occurrence** sur toute la session |
| Sort 182 dans `[DECIDEUR] Règle SynFus retenue` | ❌ **0 occurrence** |
| Sort 182 dans `[DECIDEUR] rejet règle #182` | ❌ **0 occurrence** |
| Règle id=182 dans `peleas/test.json` (état final) | ❌ **ABSENTE** |

**La règle 182 n'existe nulle part dans le json `peleas/test.json`.** Le moteur `MoteurReglesCombat.Evaluer` itère `cfg.Regles.OrderByDescending(...)` — s'il n'y a pas de règle 182, le sort ne sera jamais ni proposé, ni rejeté, ni cast. Pas de log.

### 2.2 Cause racine — l'user a édité manuellement la config

Au boot 12:56:17 : `[CFG-COMBAT] Chargé : ... 7 règles`. Le preset Sadida (`VueCombat.xaml.cs:578`) crée bien 7 règles dont :
```csharp
Ajout(182, "La Folle", FocusSort.CelluleAdjacenteMoi, 100, 3, MethodeLancement.LesDeux, premierTour: false);
Ajout(193, "La Bloqueuse", FocusSort.CelluleAdjacenteEnnemi, 95, 1, ...);
```

Entre **12:57:29 et 13:00:34** : **23 sauvegardes `[CFG-COMBAT] Sauvegardé : peleas\test.json`** consécutives — l'user a manuellement édité la rotation dans l'onglet Combat WPF (chaque modif déclenche save debouncé). État final du json : **5 règles** seulement → 183 Ronce, 195 Larme, 200 Poison Paralysant, 192 Ronce Apaisante, 197 Puissance Sylvestre. Les règles **182 La Folle** et **193 La Bloqueuse** ont été supprimées.

Confirmation par horodatage `peleas/test.json` : modifié à 13:08 (puis stable). Les 5 règles restantes correspondent exactement aux 5 sorts qui apparaissent dans les logs `[DECIDEUR]`.

### 2.3 Lignes de log clés

Aucune ligne ne mentionne le sort 182. Voici la dernière confirmation que 182 est appris mais ignoré :

```
156: [12:56:46.196] [SORTS] 8 sort(s) scanné(s) : #192 niv1, #193 niv1, #195 niv1, #197 niv1, #198 niv1, #182 niv1, #183 niv5, #200 niv1
2201:[13:09:05.458] [SORTS] 8 sort(s) scanné(s) : #192 niv1, #193 niv1, #195 niv1, #197 niv1, #198 niv1, #182 niv1, #183 niv5, #200 niv1
```

Et la signature du chargement de la config (qui DEVRAIT contenir 7 règles mais en a 5) :

```
2:   [12:56:17.257] [CFG-COMBAT] Chargé : style=Tactique, placement=PasDeDeplacement, distance=4, 7 règles, soin=aucun
225..245: [12:57:29..13:00:34] [CFG-COMBAT] Sauvegardé : peleas\test.json   (23 saves)
```

### 2.4 Fausse piste — les logs « cell vide » du début

Entre **13:01:04 et 13:02:11** (1er combat, qui s'est soldé par MORT), le moteur logue :
```
331: [13:01:04.746] [DECIDEUR] Règle SynFus retenue : « Ronce » (#183 niv5) focus=CelluleVide, cible « (cell vide) » cell 192
332: [13:01:04.746] [INVOC] Cell 192 ajoutée aux invocations attendues
333: [13:01:04.746] [ACTION] Sort « Ronce » niv5 sur cell 192 (cible « (cell vide) », 4 PA, portée 1-8, dist réelle=1)
```

→ C'est la règle **183 Ronce** que l'user avait à ce moment éditée avec `Focus=CelluleVide` (mauvaise config saisie temporairement). Pas un bug du moteur — l'user a corrigé en cours de combat (save 13:02:12), et à partir du combat #2 (13:03), la règle 183 a focus `EnnemiLePlusFaible` (état final du json).

Le moteur a fait exactement ce qui était demandé : cast un sort Ronce sur une cellule vide. Le serveur l'a accepté (ack `GKK0`), mais ce n'était pas pertinent — d'où la mort du perso.

### 2.5 Vérification compteur NombreParTour — pas de bug

Le compteur `Combat.CompteursRegleParTour` est correctement reset à chaque `NouveauTour()` (`Combat.cs:144`). Tous les `[DECIDEUR] rejet règle #X : NombreParTour atteint` observés sont légitimes (la règle a déjà été lancée le nombre max autorisé ce tour-là).

---

## 3) Autres rejets de sorts — analyse complète

Distribution des **277 rejets `[DECIDEUR]`** (extrait via regex `rejet règle .* :`) :

| Raison | Count | Pertinent ? |
|---|---|---|
| NombreParTour atteint (2/2) — Ronce | 74 | ✅ légitime |
| `aucune cible valide` — Puissance Sylvestre focus=InvocationLaPlusBlessee | **59** | ⚠️ **voir 3.1** |
| NombreParTour atteint (1/1) | 52 | ✅ légitime |
| `dist 0 < porteeMin 1` — Ronce Apaisante focus=AllieLePlusBlesse | **30** | ⚠️ **voir 3.2** |
| `dist X > porteeMax 2/3` — Poison Paralysant | 53 | ✅ légitime (sort CAC, ennemi loin) |
| `dist X > porteeMax 8` — Ronce | 4 | ✅ légitime (ennemi > 8 cases) |
| PA insuffisants (5<6) | 1 | ✅ légitime |

### 3.1 Bug latent — Puissance Sylvestre (#197) toujours rejeté

**59 rejets** sur 35 combats, soit ~1.7 par combat. Le sort 197 (buff invocs) n'a aucune cible valide parce qu'**aucune invocation n'a JAMAIS été lancée** (cf. §2 — La Folle/La Bloqueuse pas dans la config). C'est cohérent, mais ça pollue les logs et ralentit la décision (un tour itère 5 règles puis pass turn).

**Pas de bug logique du moteur**, mais c'est un effet de bord du bug config §2.

### 3.2 Bug latent — Ronce Apaisante (#192) jamais cast

**30 rejets** `dist 0 < porteeMin 1` à 100% du temps. Le sort 192 (heal) a `RANGO_MINIMO=1` (xml dyshay), focus configuré `AllieLePlusBlesse`. Or le seul allié vivant est **MOI** → cible=moi → dist=0 → rejet portée min. Sort `RANGO_MODIFICABLE="TRUE"` mais la portée 1-4 stricte exclut le self-cast.

Le moteur fait son job (le serveur rejetterait aussi le cast). **Mais l'user pense que ce sort devrait se cast quand son PV baisse** → en réalité Ronce Apaisante ne peut PAS soigner le caster, elle soigne un allié adjacent. Le bot solo n'en bénéficiera jamais.

**Recommandation soft :** soit le retirer du preset Sadida pour les configs solo, soit ajouter un commentaire UI « heal allié uniquement, inutile en solo ».

### 3.3 Aucun bug structurel du moteur

- Compteur NombreParTour reset correctement à chaque tour (`Combat.NouveauTour:144`)
- Cooldown multi-tours testé via `DernierTourLanceParSort` — pas de cas observé (CooldownTours=0 dans la config courante)
- Multi-cast par tour fonctionne (vu au tour 13:10:01-13:10:05 : Ronce + Ronce + Larme + Poison = 4 casts/tour)
- LOS / ANTI-BAN ne se déclenchent jamais (logique car portées 1-8, en CAC souvent)

---

## 4) Comportement récolte

Distribution `GA500<cell>;<skill>` (1 824 occurrences, mais doublons d'observation+injection) :

| Skill | Count brut | Sens | Note |
|---|---|---|---|
| 45 (Faucher) | 940 | Bûcheron / paysan | Skill principal de la session |
| 6 (Couper bois) | 425 | Bûcheron | |
| 68 (Cueillir) | 318 | Paysan plante / fleur | |
| 53 (Pêcher) | 40 | Pêche | |
| 39 | 14 | Couper bois (variant) | |
| **114 (Zaap)** | **5** | Téléport zaap | OK fonctionnel (sauf 1 timeout) |

**Métiers réels du perso** (extrait `[MÉTIERS]` 16:04:42) : `job#2=Bois niv0`, `job#28=Céréale niv34` (skills 45/47/53/57/46/122). Le niveau 34 en Paysan explique le volume de `;45` (Faucher).

Patterns récolte :
- Boucle stable : `GA001<path>` → `GA500<cell>;<skill>` → `GKK0` × N → next.
- Aucun blocage de récolte (`OQ` 1 occurrence dans une chat-line, pas de log technique d'échec OQ).
- 5 zaaps réussis (skill 114), 1 zaap timeout à 13:17:39 (`[ZAAP] timeout (6 s) — pas de GDM #10317`) → récupéré naturellement.

---

## 5) Bugs et anomalies divers

| Anomalie | Sévérité | Détail |
|---|---|---|
| **Désync cipher** | ✅ aucune | `≠proxy(décalé)` est NORMAL (40 occurrences = comportement attendu post-fix `resetReel/_akRenegocie`) ; aucun kick |
| **Mort perso** | ⚠️ 1 fois | Combat #1 (13:01-13:02), tour 9 ennemis → PV 0. Cause = config combat corrompue (Ronce avec focus CelluleVide → bot a cast 9× sort hors-cible) |
| **70 GAF code=2 hors portée** | 🟢 acceptable | Pattern : bot fait `GA001` (déplacement) puis `GA300` aussitôt, parfois le serveur n'a pas encore traité le déplacement → GAF refusé. Le bot continue avec `[DECIDEUR] retenue` valide derrière. À optimiser. |
| **2 timeouts ACTION-MV** | 🟢 acceptable | Mode SECOURS optimiste activé → cast aveugle réussi. |
| **Boucles infinies** | ✅ aucune | |
| **GKK2 / GKKNaN** | 🔧 cosmétique | `[OBS C→S '-'] clair='GKK2'` et `GKKNaN` apparaissent — paquets envoyés par le **client réel** (pas le bot). Probablement variantes anti-bot Hystoria. Aucun impact, le proxy relaye. |
| **Map 10314 logué 4400 fois** | 🟢 spam | `[CARTE] #10314 : 0 cellule(s) interactive(s)...` répété à chaque event. Cosmétique. |

---

## 6) 🎯 RECOMMANDATIONS FIX (pour l'agent principal)

### 6.1 BUG « La Folle » — c'est UN BUG D'UX, PAS DE MOTEUR

**Cause racine** : `peleas/test.json` ne contient pas la règle 182. L'user a supprimé La Folle/La Bloqueuse manuellement via l'onglet Combat (23 saves consécutives au démarrage). Le moteur n'a rien à corriger.

**FIX recommandé (UX + protection)** — fichier `BotDofus.Wpf/Vues/VueCombat.xaml.cs`, autour de `BtnPresetSadida_Click` (l. 552-589) :

1. **Affichage avertissement quand un sort APPRIS n'est pas dans la rotation.**
   - Ajouter dans `Rafraichir()` (méthode existante de `VueCombat.xaml.cs`) un check :
     ```csharp
     var idsConfig = _contexte.ConfigCombat.Regles.Select(r => r.IdSort).ToHashSet();
     var sortsAppris = _contexte.EtatJeu.Personnage.SortsAppris;
     var manquants = sortsAppris.Keys.Where(id => !idsConfig.Contains(id)).ToList();
     if (manquants.Count > 0)
         TxtEtatSauvegarde.Text = $"⚠ {manquants.Count} sort(s) appris non géré(s) : {string.Join(",", manquants)}";
     ```
   - L'user verrait immédiatement « ⚠ 3 sorts appris non gérés : 182,193,198 » et saurait quoi rajouter.

2. **Bouton « Ajouter sort manquant »** dans `VueCombat.xaml` — qui propose les sorts appris pas encore configurés, avec preset focus intelligent (CelluleAdjacenteMoi pour invocs Sadida 182/198, EnnemiLePlusFaible sinon).

3. **Empêcher l'utilisateur de supprimer un sort flagué « critique »** OU au moins demander confirmation (`"Supprimer La Folle ? Le bot ne pourra plus invoquer."`).

### 6.2 BUG latent : Ronce Apaisante (#192) toujours rejetée en solo

Fichier `Divers/Combats/IA/MoteurReglesCombat.cs` ligne 137 — quand `focus=AllieLePlusBlesse` et **moi seul allié vivant**, le moteur retourne moi en cible (cf. `ChoisirCible` l. 238-241 : `AllieLePlusBlesse` filtre `a.PV < a.PVMax` puis ordonne, donc retourne moi si je suis blessé). Puis cast échoue car dist 0 < porteeMin 1.

**FIX** dans `MoteurReglesCombat.cs` ligne 238 — exclure self quand portéeMin > 0 :
```csharp
FocusSort.AllieLePlusBlesse => alliesVivants
    .Where(a => a.PV < a.PVMax)
    .Where(a => a.Identifiant != moi.Identifiant)   // ← AJOUTER : pas moi si porteeMin>0
    .OrderBy(a => a.PVMax > 0 ? 100 * a.PV / a.PVMax : 100)
    .FirstOrDefault(),
```
**OU** plus propre : ajouter un nouveau focus `MoiOuAllieBlesse` qui retourne self uniquement si porteeMin=0 ; puis migrer le json. Ou alors traiter le case dans `Evaluer` : si focus=AllieLePlusBlesse retourne moi ET porteeMin>0 ET je suis pas blessé/seul, fallback sur autre règle.

### 6.3 BUG latent : Puissance Sylvestre (#197) pollue les logs

59 rejets `aucune cible valide` (focus=InvocationLaPlusBlessee). Ce sort buff les invocs ; or quand la rotation n'a pas d'invocation (config user actuelle), il ne servira jamais.

**FIX** soft (UI) — `BtnPresetSadida_Click` (l. 552) : ne pas ajouter #197 si #182/193/198 absents. Ou flagger la règle « inactive » dans le DataGrid.

**FIX** dur (moteur) — `MoteurReglesCombat.cs` ligne 137 : downgrade le log de `Debogue` à `Trace` (ou skip silencieusement) quand `focus=InvocationLa...` et `combat.Allies.None(a => a.EstInvocation)` — pas besoin de logger 59 fois la même évidence.

### 6.4 Optimisation : `GAF code=2 hors portée` après pré-mouvement

Pattern observé : `[PRE-MOVE] Envoi GA001` → `[GA0] Position confirmée` → **GAF-ECHEC code=2 immédiat**. Le bot fait `GA300` trop vite après le `GA001` ; le serveur n'a parfois pas fini de traiter le déplacement et refuse le cast comme hors-portée.

**FIX** dans `Commun/Frames/TrameJeu.cs` ligne ~1613 — augmenter le délai après confirmation déplacement :
```csharp
await Task.Delay(System.Random.Shared.Next(300, 500)).ConfigureAwait(false);
// → passer à Random(500, 800) pour éviter le GAF
```

### 6.5 Note finale — pas de bug bloquant côté IA

L'IA a fait **34 victoires / 35 combats** sur 3h, avec 1 mort imputable à la config user corrompue temporairement (Ronce focus=CelluleVide). Le moteur de règles fonctionne correctement, le cipher MITM est stable (0 kick), la récolte tourne bien (5 zaaps, ~1700 GA500 envoyés).

**Priorité fix** :
1. **🔴 §6.1** (UX règles manquantes) — c'est CE bug que l'user signale comme « La Folle ne se cast pas »
2. **🟡 §6.2** (Ronce Apaisante self-cast)
3. **🟢 §6.3** (Puissance Sylvestre spam logs)
4. **🟢 §6.4** (délai post pré-mouvement)
