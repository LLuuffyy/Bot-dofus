# HYSTORIA-RATE-LIMITS (V2)

> Investigation des limites de cadence du serveur Hystoria/Abrak pour le burst `EMO+` banque.
> Hypothèse de départ : serveur ignore silencieusement 20-70 % des `EMO+` à 300 ms.

## Sources consultées

| Source | Emplacement | Trouvé |
|--------|-------------|--------|
| BIBLE-CADERNIS V1 | `BIBLE-CADERNIS.md` | Heuristiques anti-bot timing (lignes 680-708), aucun rate-limit `EMO+` explicite. Mention « Trop de Spam » côté chat (L695). |
| BIBLE-CADERNIS V2 | `BIBLE-CADERNIS-V2.md` | Aucun match `EMO`, `rate`, `flood`, `cadence`. Mention taille liste banque ~20 KB (L334). |
| BIBLE-CADERNIS V3 | `BIBLE-CADERNIS-V3.md` | Anti-bot Ankama (L533-571) : « Action immédiate après packet < 100 ms », « Cadence régulière variance < 50 ms » = scoring anti-bot. Pas de cap server-side EMO+. Protocole banque PNJ Retro vanille (L455-461) = `Ed`/`Eg` **OBSOLÈTE Hystoria**. |
| Code PiloteBanque | `Divers/Banque/PiloteBanque.cs:340-366` | Délai fixe **300 ms** entre `EMO+`, sans humanisation, avec suppression locale optimiste (commit récent). |
| Code PiloteMarchand | `Divers/Marchand/PiloteMarchand.cs:97-102` + `ConfigMarchand.cs:54-58` | Délai humanisé **80-200 ms** entre `EMO+`. Aucune perte rapportée dans les docs/forensic. |
| Code ApiBot (humaniseur) | `Divers/Scripts/Api/ApiBot.cs:106-131` + `Divers/Securite/HumaniseurActions.cs:28-31` | `EnvoyerHumaniseAsync` impose **plancher 60 ms, plage 120-380 ms** entre paquets globaux. Mais `PiloteBanque` **bypasse** l'humaniseur (utilise `_session.EnvoyerAuServeurAsync` direct). |
| Code SessionProxy | `Commun/Reseau/SessionProxy.cs` | Aucun rate-limit côté proxy. Mentionne kicks sur désync cipher (L139, L329, L403), pas sur cadence. |
| Code TrameJeu | `Commun/Frames/TrameJeu.cs:58, 469, 1735` | Mentions kicks : 45 s silence tour combat, ObjectDisposed, désync placement. Pas de kick cadence/burst documenté. |
| Memory perso | `tech_findings_cadernis_dyshay.md` + `tech_marchand_protocole.md` + `MEMORY.md` | Confirme dyshay 300 ms burst + suppression optimiste = pattern référence Retro 1.29. Marchand utilise `EMO+` aussi sans pertes. |
| docs/PROTOCOLE-BANQUE-REFERENCE.md | racine `docs/` | Tableau rate-limit (L201-208) : « 300 ms = safe par expérience dyshay, non documenté officiellement ». |
| docs/AUDIT-CODE-BANQUE.md | racine `docs/` | Confirme 300 ms = dyshay « connu OK » (L113). Identifie cause racine non-rate-limit (H5 = race OQ vs snapshot, confiance 9/10). |
| docs/FORENSIC-DEPOTS-BANQUE.md | racine `docs/` | **Preuve dure** : ratio 8/65 → 30/57 → 0/27 OR reçus. Hypothèse H6 (L182-191) = serveur consomme EMO+ sans émettre OR (buffer/rate-limit interne hypothétique). |
| docs/ANALYSE-FLOW-BANQUE.md | racine `docs/` | Capture user réelle 2026-05-21 16:08 = cadence native humaine. |
| docs/STATE-MACHINE-BANQUE.md | racine `docs/` | Race condition snapshot vs OQ tardif comme cause alternative (L226, L278). |
| docs/PLAN-FIX-BANQUE.md | racine `docs/` | Marque l'hypothèse H3 rate-limit serveur **FAUX** (confiance 9/10, L75) : « 300 ms = pattern dyshay éprouvé ». |
| WebSearch | — | NON utilisé : sources internes amplement suffisantes. |

---

## Q1 — Rate-limit documenté

**Verdict : NON, aucun rate-limit `EMO+`/seconde explicitement documenté côté Hystoria.**

- BIBLE-CADERNIS V1/V2/V3 ne mentionnent **aucun cap server-side** sur le débit de paquets. Les seules limites évoquées sont des heuristiques anti-bot **scoring** (BIBLE-CADERNIS.md:686 « Cadence régulière 800 ms exact = pattern détectable », BIBLE-CADERNIS-V3.md:539 « Cadence trop régulière variance < 50 ms → +X points »).
- Le code projet (`docs/PROTOCOLE-BANQUE-REFERENCE.md:208`) acte : *« Rate-limit anti-flood Hystoria : non documenté officiellement ; 300 ms = safe par expérience dyshay »*.
- Le seul indicateur indirect d'un cap est le message chat *« Trop de spam »* (BIBLE-CADERNIS.md:695), qui concerne le **chat**, pas les actions banque.

**Conclusion** : rien dans les sources textuelles ne prouve un rate-limit `EMO+`. L'hypothèse est née empiriquement du forensic du bot, pas d'une source primaire.

---

## Q2 — Délais bot existants par feature

| Feature | Paquet | Délai inter | Cipher ? | Réussite observée | File:line |
|---------|--------|-------------|----------|-------------------|-----------|
| Banque (V1) | `EMO+` | **300 ms fixe** | Oui '-' | **12-100 %** OR reçus (12/65 puis 30/57 puis 0/27, pattern dégradant) | `Divers/Banque/PiloteBanque.cs:344-366` |
| Marchand | `EMO+` | **80-200 ms humanisé** (`Random.Shared.Next(min,max)`) | Oui '-' | Aucune perte rapportée (cf. Q4) | `Divers/Marchand/PiloteMarchand.cs:97-102` + `Divers/Marchand/ConfigMarchand.cs:57-58` |
| Récolte | `GA500` | 1 GA500 par récolte, **GA500 collé au GA001** sans délai inter | Oui '-' | Stable (loop OQ-driven) | `Divers/Scripts/Api/ApiBot.cs:933, 946, 967` |
| Déplacement combat | `GA001` | Variable selon longueur chemin : `Task.Delay(duree)` où `duree = nbCases × 330 ms` (`ApiBot.cs:320-327`) | Oui '-' | Stable | `Divers/Scripts/Api/ApiBot.cs:320-329` |
| Cast sort | `GA300` + `GKK0` + `Gt` | 300-500 ms post-GA300, 1000-1500 ms post-GKK0 (cf. CLAUDE.md:151-153) | Oui '-' | Stable | `Commun/Frames/TrameJeu.cs` pipeline |
| Paquets API globaux | tout via `EnvoyerHumaniseAsync` | **Plancher 60 ms, plage 120-380 ms** humanisé | Selon contenu | Stable | `Divers/Securite/HumaniseurActions.cs:28-31` |
| Zaap | `WU` | 1500 ms post-GA500 zaap, puis 250 ms avant WU | Mixte | Stable | `Divers/Scripts/Api/ApiBot.cs:1045-1054` |

**Observation clé** : la banque est **la seule feature** qui :
1. **Bypasse** l'humaniseur (`PiloteBanque` appelle `_session.EnvoyerAuServeurAsync` direct, pas `_api.EnvoyerHumaniseAsync`) — la cadence est donc *métronomique parfaite 300 ms* sans jitter.
2. Burste sans aucune attente d'acquittement (`OR`).
3. Subit des pertes massives.

Le **marchand** utilise le même opcode `EMO+`, sur le même canal chiffré, à cadence **plus rapide** (80-200 ms = potentiellement 12 paquets/s en pic) et fonctionne sans pertes connues. Cela contredit fortement l'hypothèse « rate-limit serveur sur opcode `EMO+` ».

---

## Q3 — Kicks après burst

**Aucun kick documenté après burst rapide.**

Recherche dans le code pour `kick` :
- `Commun/Reseau/SessionProxy.cs:139, 232, 329, 403` : kicks documentés uniquement sur **désync cipher** (idx `-` non monotone, trame entrelacée, idx en retard).
- `Commun/Frames/TrameJeu.cs:58, 469, 1735` : kicks documentés sur 45 s de silence tour combat, ObjectDisposed (socket fermée), placement incorrect.
- `docs/FORENSIC-DEPOTS-BANQUE.md` : 243 EMO+ envoyés à 300 ms en 5 min — **aucun kick**, juste silence serveur. Le client reste connecté, la banque reste ouverte, EV final fonctionne, le perso n'est pas déco.

**Pattern serveur observé en cas de saturation** : pas de kick, pas de message d'erreur, juste **drop silencieux** de l'OR. C'est plus cohérent avec un état de session échange qui se désynchronise qu'avec un anti-flood actif.

---

## Q4 — Marchand : pattern comparé

`Divers/Marchand/PiloteMarchand.cs` :
- Ligne 97 : `var paquet = $"EMO+{item.Identifiant}|{item.Quantite}";` — **opcode identique à la banque**.
- Ligne 99 : `await _session.EnvoyerAuServeurAsync(paquet)` — **même chemin réseau** que banque (canal chiffré '-' auto).
- Ligne 102 : `await Delai(ct)` → `Random.Shared.Next(80, 200)` ms (`PiloteMarchand.cs:150-156` + `ConfigMarchand.cs:57-58`).

**Aucun pattern d'échec rapporté** dans les memory ou les docs/. Le commentaire de la config (`ConfigMarchand.cs:54-56`) précise même : *« Réduit à 80-200 ms pour accélérer la vente (~60 s pour 430 items au lieu de 3 min) »*. 430 items × 140 ms moyen = ~60 s sans pertes.

**Implication forte** : si le serveur acceptait 430 EMO+ à 140 ms moyen sans pertes côté marchand, il devrait accepter le même pattern côté banque. **La différence n'est pas dans la cadence d'envoi**.

Différences notables entre marchand et banque qui pourraient expliquer la divergence de réussite :
- Marchand : ouverture `ER2|<idCtx>` (état échange `MARKET`), banque : `ApS` (état échange `STORAGE`).
- Marchand : pas d'OQ post-loot à attendre (le délai de stabilisation 2.5 s `PiloteBanque.cs:264` n'existe pas côté marchand).
- Marchand : pas de cycle multi-pass (commit récent `MAX_PASSES=1` pour banque aussi maintenant).
- Marchand : pas de combat antérieur générant des OAK/OQ tardifs perturbant le snapshot inventaire.

→ **Cause racine probable du bug banque ≠ rate-limit.** Hypothèse H5 de `AUDIT-CODE-BANQUE.md:154` (race OQ vs snapshot inventaire après combat) est confirmée comme cause principale, avec confiance 9/10.

---

## Q5 — Cadence native client humain

Capture user `docs/ANALYSE-FLOW-BANQUE.md:19-29` (2026-05-21 16:08:07 → 16:08:26, 5 dépôts) :

| # | Timestamp | EMO+ | Δ vs précédent |
|---|-----------|------|----------------|
| 1 | 16:08:09.755 | `EMO+8007499756\|1` | (ECK5 → EMO+ : **2.1 s**) |
| 2 | 16:08:11.090 | `EMO+7964523660\|1` | **1.335 s** |
| 3 | 16:08:15.001 | `EMO+7999301324\|42` | **3.911 s** |
| 4 | 16:08:17.274 | `EMO+7994252552\|1775` | **2.273 s** |
| 5 | 16:08:20.048 | `EMO+7972913998\|2864` | **2.774 s** |

**Médiane humaine : ~2.5 s entre 2 EMO+.** Minimum observé : 1.3 s. Maximum : 3.9 s. Variance forte = pattern humain naturel (drag&drop manuel).

Le serveur accepte évidemment 100 % de ces dépôts (ack `EsKO+` + `OR` chaque fois, capture L113-114).

Le client réel **ne fait jamais de burst <1 s** en mode manuel. Le pattern dyshay (300 ms metronomique) est **10× plus rapide que ce que le serveur voit en condition humaine normale**.

---

## Q6 — Buffer banque saturé ?

**Indices indirects, pas de preuve formelle.**

`docs/FORENSIC-DEPOTS-BANQUE.md:186-191` propose explicitement :
> *« Le serveur consomme physiquement plus d'EMO+ que le nombre d'OR émis (probablement à cause d'un buffer interne ou de la déchiffre rate-limited côté serveur) »*

Indices observés :
- **Pattern dégradant pass après pass** : 12 % → 53 % → 0 % OR reçus sur le même workflow. Comportement compatible avec un buffer qui se remplit puis ne reset jamais sur la session échange courante. Mais aussi compatible avec une race OQ.
- **0 OR sur 27 EMO+ en 9 passes consécutives** (243 EMO+ identiques ignorés) : **fortement** compatible avec un état serveur cassé (session échange a perdu sa cohérence inventaire avec le bot), pas un buffer rate-limit (qui se viderait après 8 s d'attente).
- **EV puis ApS re-fonctionne** (commits ultérieurs) : fermer/rouvrir « débloque » le buffer. Cohérent avec un état interne corrompu plutôt qu'un cap throughput.
- **Aucun rejet explicite** (`EVErr`, `EvErr`, `EsKO-`, etc.). Le serveur ne dit jamais « trop vite » ni « buffer plein ».

**Conclusion buffer** : il existe probablement une saturation au niveau **état de session échange** (snapshot inventaire serveur ↔ bot désynchronisé) plutôt qu'un buffer paquet bête. Le « 0/27 répété » est plus compatible avec des **UIDs déjà consommés que le serveur ignore** qu'avec un buffer qui drop.

---

## Estimation rate-limit Hystoria pour EMO+

- **Cadence safe estimée** : **400-600 ms** entre EMO+ (avec jitter).
- **Confiance** : **5/10** — c'est une estimation conservatrice basée sur l'absence de preuve d'un vrai rate-limit, mais sur la prudence vis-à-vis du scoring anti-bot.
- **Justification** :
  - Marchand prouve que 80-200 ms passe sans pertes → un rate-limit pur opcode est **improbable**.
  - Le scoring anti-bot Ankama (BIBLE-CADERNIS-V3.md:548) sanctionne « variance < 50 ms » et « < 100 ms entre événements ». 300 ms metronomique sans jitter est donc à la limite.
  - Une cadence **400-600 ms avec jitter ≥ 30 %** (équivalente à l'humaniseur global 120-380 ms) :
    - Sort du seuil anti-bot scoring (variance OK).
    - Reste 5× plus rapide que la cadence humaine native (2.5 s médiane).
    - Reproduit le comportement du marchand qui fonctionne (140 ms avec jitter ±60 ms).
  - **MAIS** : le bug banque n'est probablement **pas** un rate-limit. Augmenter la cadence ne corrigera pas le problème racine (H5 race OQ/snapshot). Voir Q4.

---

## Recommandation V2

- **Pattern** : **synchrone** (attendre OR avant suivant) — **mais voir alternative ci-dessous**.
- **Délai inter-items** (en plus de l'attente OR) : **200 ms minimum + jitter humanisé ±30 %** (soit 200-400 ms aléatoire).
- **Timeout OR** : **1500 ms** par item (capture user montre OR ~40 ms après EMO+ en charge normale).
- **Retries** : **1 retry max** par item après timeout, puis log `[BANQUE-LOST]` et continue avec suppression optimiste.

### Alternative recommandée (probablement meilleure)

Garder le pattern **burst dyshay** (300 ms, asynchrone, suppression optimiste — commit actuel `MAX_PASSES=1` post-fix), mais résoudre la **vraie cause racine** identifiée par AGENT 1/AUDIT (H5 race OQ vs snapshot) :

1. **Augmenter la stabilisation post-ECK5** de 2.5 s → **5 s** (`PiloteBanque.cs:264`) pour laisser passer 100 % des OQ correctifs après combat.
2. **Ajouter jitter humanisé** au 300 ms : `Random.Shared.Next(250, 400)` au lieu de `Task.Delay(300)` fixe (`PiloteBanque.cs:366`) — supprime le pattern metronomique anti-bot scoring.
3. **Fermer/réouvrir** la banque (EV + ApS) si pass 1 termine avec ratio OR/EMO+ < 50 %, plutôt que de rebourrer à blanc (cf. suggestion 2 de `FORENSIC-DEPOTS-BANQUE.md:199`).
4. **Multi-cast détectable** : si > 5 EMO+ consécutifs sans OR retour pendant le burst, **stopper net** le burst (le serveur a perdu la sync), envoyer EV, attendre 2 s, re-ApS, recalculer le snapshot frais.

Confiance : ces fixes adressent la cause racine réelle (race OQ snapshot + état échange désynchronisé), pas un cap fictif côté serveur.
