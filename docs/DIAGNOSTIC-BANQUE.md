# DIAGNOSTIC-BANQUE — Synthèse des 5 forensiques

> AGENT 6 ARCHITECT-SYNTHESIS — 2026-05-31
> Consolide les findings des AGENT 1 (logs), 2 (visuels), 3 (code), 4 (protocole), 5 (state machine).

---

## 1. Cause racine confirmée

**Désynchronisation `Personnage.Inventaire` ↔ état serveur, pendant et après le burst `EMO+`, à cause d'une race OAK/OQ post-loot, aggravée par l'absence de suppression locale optimiste après envoi `EMO+` (divergence dyshay).**

Niveau de confiance : **9.5/10** (convergence parfaite AGENT 1 + 3 + 4 + 5 ; AGENT 2 confirme le symptôme visuel).

---

## 2. Mécanisme step-by-step

Le bug se manifeste après n'importe quel combat qui précède l'ouverture du coffre. La séquence détaillée :

```
1. Combat termine → boucle TrameJeu pousse OAK(uid=A, qte=1, position=63)
                                       ↓
2. Le serveur enchaîne OQ(uid=A, qte=18) en correctif quelques ms plus tard
   (drop stack avec items déjà présents — pattern OAK→OQ documenté Hystoria).
                                       ↓
3. ContexteCompte détecte poids >= seuil → spawn WorkflowCompletAsync.
                                       ↓
4. PiloteBanque.DeposerToutInterneAsync :
     - ApS → ECK5 → BanqueOuvertureObservee=true
     - Task.Delay(2500) « stabilisation OQ »
       ⚠ pas assez sous charge réelle : les OQ correctifs continuent
       d'arriver plus de 2.5 s après ECK5 sur Hystoria.
                                       ↓
5. Pass 1 :
     - snapshot = _perso.Inventaire.ToList()
       ⚠ ToList() SANS lock(inv) → race avec OnObjetAjout / OnObjetQuantite /
         OnObjetRetrait qui mutent la liste sous lock(inv).
     - aDeposer = CalculerItemsADeposer(snapshot)
       → contient uid=A avec qte=1 (l'OQ correctif n'est pas encore arrivé)
     - foreach item → EMO+A|1 envoyé via canal chiffré '-'.
                                       ↓
6. Le serveur reçoit EMO+A|1 :
     - Côté serveur, l'objet A est à qte=18 (état réel).
     - Le serveur retire 1 unité de A, A reste à qte=17.
     - PAS d'OR émis (l'objet n'est pas supprimé, juste décrémenté).
     - Hystoria n'envoie PAS toujours d'OQ de confirmation pour ce delta.
                                       ↓
7. Côté bot :
     - Pas d'OR reçu pour A → CompteurObjectRemove pas incrémenté.
     - L'item A reste visible dans _perso.Inventaire à qte=1.
     - Si un OQ correctif arrive pour A (qte=17 désormais) APRÈS le RemoveAll
       du cycle 1, OnObjetQuantite trouve l'UID inconnu (déjà supprimé en
       cas où c'est un autre UID) ou met à jour qte=17 (cas où A est encore là).
                                       ↓
8. Pass 2 / 3 / cycles supplémentaires :
     - snapshot ré-itère : uid=A est encore là avec qte=1 (ou 17 selon timing).
     - EMO+A|1 (ou |17) re-envoyé → MÊME UID, le serveur l'ignore ou ne
       répond rien (item déjà partiellement déposé, état incohérent).
     - 0/N OR reçus en 8000ms × 9 passes consécutives.
                                       ↓
9. Bilan : 27 items « bloqués » côté local, 0 dépôt effectif, 243 EMO+ inutiles.
   Sac visuel toujours plein (cf. AGENT 2 screenshot 175310.png).
```

Le pattern signature dans les logs (AGENT 1 §Pattern récurrent) est strictement reproductible : **mêmes UIDs renvoyés 9 fois consécutives, toujours `0/N OR reçus en 8000ms`, poids final inchangé**.

---

## 3. Sub-causes secondaires

| # | Sub-cause | Impact | Source |
|---|-----------|--------|--------|
| S1 | **Pas de suppression optimiste locale après EMO+** (dyshay le fait) | Items déjà déposés re-soumis aux passes suivantes | AGENT 4 §A6 |
| S2 | **`ToList()` non locké** sur `_perso.Inventaire` | Snapshot incohérent ou `InvalidOperationException` swallow | AGENT 5 §1 |
| S3 | **OQ avec UID inconnu = no-op** (cf. `TrameJeu.cs:1212-1224`) | Item invisible côté bot quand l'OQ correctif arrive après suppression locale partielle | AGENT 3 §H5 + AGENT 5 §4 |
| S4 | **OQ avec qte=0 ne supprime PAS l'objet** (cf. `TrameJeu.cs:1200-1210`) | Entrées zombies dans l'inventaire | AGENT 3 §A1 |
| S5 | **Multi-pass × 3 cycles supplémentaires × 3 passes = 9 boucles stériles** | 5 min de bot bloqué bras-cassés | AGENT 1 §Bilan + AGENT 4 §A1 |
| S6 | **Stabilisation OQ 2.5 s post-ECK5 insuffisante** sous charge réelle | Snapshot pass 1 prend des qte transitoires | AGENT 3 §H5 + AGENT 5 §2 |
| S7 | **Timeout OR 8 s trop court quand burst lui-même prend 15 s** (50 items × 300 ms) | Confirmes < déposés, déclenche pass 2/3 inutiles | AGENT 3 §H4 |
| S8 | **CompteurObjectRemove static partagé** | Faux positifs si plusieurs comptes simultanés (futur) | AGENT 3 §A2 |

---

## 4. Tableau verdict H1-H8 (consolidation AGENT 1 + 3)

| H | Hypothèse | Verdict | Confiance | Preuves clés |
|---|-----------|---------|-----------|-------------|
| H1 | Cipher desync canal '-' | **FAUX** | 10/10 | AGENT 1 §H1 : 8/65 OR reçus pass 1 → cipher OK ; le reste 0/N est sémantique pas cipher |
| H2 | Filtrage catégorie cassé | **FAUX** | 10/10 | AGENT 1 §H2 + AGENT 3 §H2 : `CalculerItemsADeposer` pure, items pertinents (Slip Vampire, Os Chafer…) bien sélectionnés |
| H3 | Rate-limit serveur (cadence EMO+) | **FAUX** | 9/10 | AGENT 1 §H3 : 300 ms = 3.3/s, sous toute limite plausible ; capture user dyshay confirme 300 ms safe |
| H4 | UIDs fantômes / corrupted | **FAUX** | 9/10 | AGENT 1 §H4 : filtre template>30000 déjà actif, les 27 « bloqués » sont des templates valides (756, 310, 2285) |
| H5 | **Désync inventaire `_perso.Inventaire` ↔ serveur** (OQ-after-snapshot, OQ UID inconnu, qte transitoire) | **VRAI — cause racine** | **9.5/10** | AGENT 3 §H5 + AGENT 5 §1+§4 + commentaire forensic existant `TrameJeu.cs:1213-1224` (le code lui-même admet le bug !) |
| H6 | **Item physiquement déposé côté serveur sans OR retour** (burst partiellement consommé) | **VRAI — manifestation du même bug que H5** | 9/10 | AGENT 1 §H6 : mêmes UIDs 9× envoyés, jamais d'OR. C'est H5 vu côté serveur — le serveur a accepté `EMO+A|1` mais n'a pas émis d'OR car A n'est pas entièrement consommé |
| H7 | `OnObjetRetrait` ne décrémente pas | **FAUX** | 10/10 | AGENT 1 §H7 + AGENT 3 §H3 : le code `inv.RemoveAll` marche, 8/30 OR sur pass 1/2 sont la preuve |
| H8 | Proxy droppe les OR | **FAUX** | 9/10 | AGENT 1 §H8 : 0 occurrence de `[INV] OQ inconnu` dans les logs problématiques → tout ce qui sort du proxy est traité |

**Conclusion** : H5 et H6 sont **deux faces du même bug**, vu respectivement côté bot (snapshot désynchronisé) et côté serveur (réception sans OR retour). La cause racine unique est la **désync** ; le mécanisme est la **race OAK/OQ vs snapshot vs absence de suppression optimiste**.

---

## 5. Visuel (AGENT 2) — Smoking gun confirmé

Paire de screenshots 28/05 17:52:48 → 17:53:10 :
- **17:52:48** : banque ouverte côte à côte avec inventaire **plein de ressources**.
- **17:53:10** : banque fermée, inventaire **toujours plein des mêmes catégories ressources**, journal combat actif visible.

→ Confirme visuellement le symptôme « banque ouverte+fermée mais sac toujours plein ». Renforce H6 (interférence combat→banque sans isolation propre des OQ post-loot).

---

## 6. Sources orthogonales convergentes

| Source | Verdict cause racine |
|--------|----------------------|
| Logs production (AGENT 1) | H6 désync serveur — 9/10 |
| Captures écran (AGENT 2) | Confirme symptôme, suggère H6 interférence combat-banque |
| Audit code (AGENT 3) | H5 désync `_perso.Inventaire` — 9/10 |
| Référence protocole (AGENT 4) | Divergence majeure dyshay : pas de suppression optimiste — 10/10 |
| State machine (AGENT 5) | Race snapshot/OQ/lock — 9/10 |

Toutes les sources convergent vers le même bug : **l'inventaire local du bot n'est pas synchronisé avec ce que le serveur sait, et le bot rejoue inlassablement les mêmes UIDs sur des items déjà partis serveur-side**.

---

**Fix prescrit** : voir `docs/PLAN-FIX-BANQUE.md` (ADR formel).
