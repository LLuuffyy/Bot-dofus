# PROTOCOLE-BANQUE-REFERENCE — Dofus Retro 1.29 (Hystoria/Abrak)

> Référence définitive du protocole banque, croisée entre **dyshay/Bot-Dofus-Retro**, **SynFus**, **BIBLE-CADERNIS V1/V2/V3**, **capture user Hystoria 2026-05-21** et le **code actuel du bot**.
> Date de compilation : 2026-05-31
> Auteur : AGENT 4 REFERENCE-PROTOCOLE-BANQUE (lecture seule, aucun .cs touché)

---

## Sources consultées

| Source | Emplacement | Confiance |
|--------|-------------|-----------|
| Capture user MITM passif Hystoria | `docs/ANALYSE-FLOW-BANQUE.md` (basée sur log `botdofus-20260521-160621.log`) | **★★★★★** terrain |
| Code dyshay — dépôt massif | `.claude\worktrees\vigorous-murdock\dyshay-source\Otros\Scripts\Acciones\Almacenamiento\StoreAllObjectsAction.cs` | **★★★★★** open-source |
| Code dyshay — fermeture | `.claude\worktrees\vigorous-murdock\dyshay-source\Otros\Scripts\Acciones\CerrarVentanaAccion.cs` | ★★★★★ |
| Code dyshay — handler serveur | `.claude\worktrees\vigorous-murdock\dyshay-source\Comun\Frames\Juego\CharacterFrame.cs:108-118, 202-203` | ★★★★★ |
| Code dyshay — ouverture PNJ | `.claude\worktrees\vigorous-murdock\dyshay-source\Otros\Scripts\Acciones\Npcs\NpcBankAction.cs:45` | ★★★★★ |
| Code dyshay — workflow banque | `.claude\worktrees\vigorous-murdock\dyshay-source\Otros\Scripts\ScriptManager.cs:463-489, 873` | ★★★★★ |
| Pattern documenté du projet | `docs/PATTERN-BANQUE-AUTO.md` | ★★★★ analyse synthétique |
| SynFus_Aqua_v1.1.0 | `C:\Users\touki\Desktop\SynFus_Aqua_v1.1.0\` | **★** DLLs uniquement (pas de sources, code obfusqué) |
| BIBLE-CADERNIS V1/V2/V3 | racine projet | ★★ couvre PNJ Retro classique, **PAS** Hystoria coffre |
| Code actuel du bot | `Divers/Banque/PiloteBanque.cs`, `Commun/Messages/VersClient/Objet/MessagesEchange.cs`, `Commun/Frames/TrameJeu.cs:101-128` | ★★★★★ source de vérité bot |

> **Note SynFus** : `SynFus_Aqua_v1.1.0` ne contient QUE des `.resources.dll` de localisation. Le binaire principal est obfusqué — aucune source `.cs` exploitable. SynFus étant un fork de dyshay (cf. `PATTERN-BANQUE-AUTO.md`), on extrapole les patterns dyshay côté SynFus.

---

## Flow standard banque (Hystoria coffre interactif)

```
Client réel ─── proxy MITM ─── Serveur Hystoria

   1.   ApS                  ────────►            (CLAIR)
                             ◄────  As<stats>      (refresh pods/kamas)
                             ◄────  ECK5           (Échange Créé kind=5 = banque)
                             ◄────  EL[<contenu>]  (liste banque, vide → "EL" seul)

   2a. EMO+<uidInv>|<qte>   ────────►            (CHIFFRÉ '-')
                             ◄────  EsKO+<uidBanque>|<qte>|<template>|
                             ◄────  OR<persoId>|<uidInv>           (objet sorti de l'inv)
                             ◄────  As<stats>     (refresh pods, parfois)

   2b. EMO+<uidInv2>|<qte2> ────────►            (300ms après 2a — pattern dyshay burst)
       ... etc                            (les OR/EsKO peuvent arriver en burst groupé)

   3.   EV                   ────────►            (CHIFFRÉ '-')
                             ◄────  EV             (confirmation fermeture)
```

**Latence totale capture user** : ~19s pour 5 dépôts (dont 13s de délais humains). Avec dyshay burst (300ms) : ~2s pour 5 dépôts purs + temps de réception des OR.

---

## Paquets détaillés

### `ApS` — ouverture coffre (Hystoria spécifique)

| Sens | Format | Canal | Notes |
|------|--------|-------|-------|
| C→S | `ApS` | **CLAIR** | 4 octets ASCII. Pas de paramètre — le serveur cherche le coffre via la position du perso. Aucun coût kamas (vs PNJ banquier 50/100k/500k). |
| S→C | `As<stats perso>` puis `ECK5` puis `EL[<contenu>]` | clair | Le `As` rafraîchit pods/kamas. `ECK5` confirme l'ouverture en mode 5 (banque). `EL` liste le contenu banque (vide → préfixe `EL` seul). |

Référence : `docs/ANALYSE-FLOW-BANQUE.md:41-58` (capture brute 16:08:07.612-660).

Côté projet, le parser de `ECK` est dans `Commun/Messages/VersClient/Objet/MessagesEchange.cs:32-41` (extrait des digits avant `|`).

### Variante PNJ banquier (dyshay/Retro vanille — **PAS Hystoria**)

| Sens | Format | Canal | Notes |
|------|--------|-------|-------|
| C→S | `DC<npcId>` | CLAIR | Dialogue Create — `NpcBankAction.cs:45` |
| S→C | `DCK` + `DQ<numQ>;<idQ>` + `DR<id1>\|<id2>\|...` | clair | Menu kamas |
| C→S | `DR<idReponse>` | CLAIR | Réponse choisie |
| S→C | `Ec<contenuBanque>` puis `ECK` | clair | Banque ouverte après paiement |

Référence : `BIBLE-CADERNIS-V3.md:455-461`. **Non applicable à Hystoria** (coffre interactif gratuit, pas de PNJ).

### `EMO+<uidInv>|<qte>` — dépôt

| Sens | Format | Canal | Notes |
|------|--------|-------|-------|
| C→S | `EMO+<uidInv>\|<qte>` | **CHIFFRÉ '-'** | `E`=Échange, `M`=Move, `O`=Object, `+`=dépôt (sens perso→banque). `uidInv` = `Personnage.Inventaire[i].Identifiant` (`long`, 10-digit Hystoria). |
| S→C | `EsKO+<uidBanque>\|<qte>\|<template>\|` | clair | `EsK`=Échange success K (ack), `O+`=ajout côté banque. **UID change** entre inventaire et banque. |
| S→C | `OR<persoId>\|<uidInv>` | clair | Object Remove — l'objet est retiré de l'inventaire perso. Compteur `PiloteBanque.CompteurObjectRemove` incrémenté par `TrameJeu.cs:1192`. |
| S→C | `As<stats>` parfois | clair | Refresh pods. |
| S→C | `Ow<actuel>\|<max>` (dyshay vanille) | clair | Sur Dofus vanille. Sur Hystoria, généralement remplacé par `As`. Handler dyshay : `CharacterFrame.cs:72-83`. |

Référence : capture `ANALYSE-FLOW-BANQUE.md:107-145` ; code dyshay `StoreAllObjectsAction.cs:34`.

> **PAS de GKK0 requis** : contrairement aux GA300/GA001/GA500 (game actions chiffrées), un dépôt banque n'a **pas** besoin d'ack `GKK0`. Cf. `ANALYSE-FLOW-BANQUE.md:168-170`.

### `EMO-<uidBanque>|<qte>` — retrait (déduit, NON capturé)

| Sens | Format | Canal | Notes |
|------|--------|-------|-------|
| C→S | `EMO-<uidBanque>\|<qte>` | **CHIFFRÉ '-'** | Sens `-` = retrait (banque→perso). UID est celui qu'a attribué la banque (pas l'UID inventaire d'origine). |
| S→C | `EsKO-...` + `OAKO<obj>` (probable) | clair | Object Add Kind = nouvel objet inventaire. Format non confirmé sur Hystoria. |

Référence : déduction par symétrie avec `EMO+`. `PATTERN-BANQUE-AUTO.md:44` recommande capture user pour confirmer.

### `EV` — fermeture

| Sens | Format | Canal | Notes |
|------|--------|-------|-------|
| C→S | `EV` | **CHIFFRÉ '-'** sur Hystoria | Exchange leaVe. 2 octets ASCII. Capture user : `ANALYSE-FLOW-BANQUE.md:178-183`. |
| S→C | `EV` | clair | Confirmation symétrique. Handler dyshay : `CharacterFrame.cs:108-118` (`get_Ventana_Cerrada`). Handler projet : `TrameJeu.cs:101-108` (set `BanqueFermeeObservee=true`). |

> **Note importante** : le client Dofus réel peut envoyer `EV` spontanément si une popup s'ouvre/se ferme côté UI (ex: l'user clique ailleurs). Le bot doit drop ces `EV` client pendant un dépôt automatisé via `BloquerEvClient = true` (cf. `PiloteBanque.cs:51-58, 201, 375`).

---

## Pattern de dépôt par référence

### dyshay (`StoreAllObjectsAction.cs:25-40`)

```csharp
private async Task cleanInventory(Account account, int idcapture = 0, int idCAC = 0)
{
    InventoryClass inventario = account.game.character.inventario;
    foreach (InventoryObject objeto in inventario.objetos)
    {
        if (!objeto.objeto_esta_equipado()
            && idcapture != objeto.id_modelo
            && idCAC != objeto.id_modelo)
        {
            account.connexion.SendPacket($"EMO+{objeto.id_inventario}|{objeto.cantidad}");
            inventario.eliminar_Objeto(objeto, 0, false);   // suppression OPTIMISTE locale
            await Task.Delay(300);                          // 300ms fixe entre EMO+
        }
    }
}
```

**Caractéristiques clés** :
- **Burst** : 1 EMO+ toutes les 300ms, **PAS d'attente OR par item**.
- **Suppression optimiste locale** : `eliminar_Objeto(...)` est appelé **immédiatement** après `SendPacket`, sans attendre la confirmation serveur. Si serveur refuse, l'inventaire est resynced via `OAKO` au prochain refresh.
- **Filtre minimal** : skip si équipé, skip si c'est l'item capture (pierre d'âme), skip si c'est l'arme CAC. Tout le reste est déposé.
- **Aucune politique catégorielle** côté action — la décision passe par `inventario.objetos` qui contient tout l'inventaire.

Ouverture (dyshay variante PNJ) : `NpcBankAction.cs:45` → `SendPacket("DC" + npc.id)`. Fermeture : `CerrarVentanaAccion.cs:19` → `SendPacket("EV")`.

Workflow orchestrateur (`ScriptManager.cs:463-489`) :
```
manejar_Npc_Banco_Bandera() :
    1. enqueue NpcBankAction(-1)         // DC<npcId>
    2. enqueue StoreAllObjectsAction()   // burst EMO+
    3. enqueue retraits (PREND_OBJET_BANQUE)
    4. enqueue CerrarVentanaAccion()     // EV
```

Trigger seuil : `MAX_PODS` global, défaut **90%** (`ScriptManager.cs:323`).

### SynFus (`SynFus_Aqua_v1.1.0`)

**Pas de source exploitable** — uniquement DLL `.resources.dll` localisation présentes. Le binaire principal est obfusqué.

D'après `PATTERN-BANQUE-AUTO.md:13-15` et la lecture inverse des screenshots UI documentés, SynFus :
- Est un **fork de dyshay** → utilise probablement le même pattern burst 300ms.
- Ajoute des UI cases-à-cocher par catégorie (Equipements/Resources/Miscellaneous/Quest) + listes garde/dépose forcée + seuils par template.
- Le **protocole réseau reste identique** à dyshay (`EMO+<uid>|<qte>` → `EV`).

### BIBLE-CADERNIS V1/V2/V3

Pertinent uniquement dans **V3 ligne 453-461** :
```
C→S : DC<idPnj banque>                 ouvrir dialogue banquier
S→C : DM|<lignes dialogue>             menu (50 kamas/100k/500k...)
C→S : DR<id reponse>                   choisir réponse
S→C : Ec<contenuBanque>                contenu banque
C→S : Ed<idObj>;<qte>                  déposer en banque
C→S : Eg<idObj>;<qte>                  retirer de la banque
```

> ⚠ **`Ed<idObj>;<qte>` documenté V3 est OBSOLÈTE pour Hystoria**. La capture user 2026-05-21 (`ANALYSE-FLOW-BANQUE.md`) montre `EMO+<uid>|<qte>` avec séparateur `|` et préfixe `EMO+` (pas `Ed`). Idem ouverture : Hystoria utilise `ApS` (coffre interactif gratuit), pas `DC<pnjId>` (PNJ payant). V3 décrit le **Retro classique vanille**, pas Hystoria.

V1 et V2 ne contiennent **aucune section banque** exploitable (seulement mentions accessoires : `BIBLE-CADERNIS-V2.md:334` parle de la taille de la liste banque ~20KB).

### Capture user Hystoria 2026-05-21 — source de vérité

Cf. `docs/ANALYSE-FLOW-BANQUE.md` (tableau ligne 17-29) :

| t | Paquet | Direction | Canal |
|---|--------|-----------|-------|
| 16:08:07.612 | `ApS` | C→S | CLAIR |
| 16:08:07.660 | `ECK5` + `EL` | S→C | clair |
| 16:08:09.755 | `EMO+8007499756\|1` | C→S | **CHIFFRÉ '-'** |
| 16:08:09.793 | `EsKO+8016316507\|1\|728\|` + `OR401770\|8007499756` | S→C | clair |
| 16:08:11.090 | `EMO+7964523660\|1` | C→S | CHIFFRÉ '-' |
| 16:08:15.001 | `EMO+7999301324\|42` | C→S | CHIFFRÉ '-' |
| 16:08:17.274 | `EMO+7994252552\|1775` | C→S | CHIFFRÉ '-' |
| 16:08:20.048 | `EMO+7972913998\|2864` | C→S | CHIFFRÉ '-' |
| 16:08:26.323 | `EV` | C→S | **CHIFFRÉ '-'** |
| 16:08:26.361 | `EV` | S→C | clair |

Délais inter-dépôts user : **1.3s / 3.9s / 2.3s / 2.8s** (humain naturel). dyshay burst : 300ms fixe.

---

## Timing / rate limits

| Paramètre | dyshay | Hystoria capture user | Recommandation Hystoria/Abrak |
|-----------|--------|----------------------|-------------------------------|
| Délai post-`ECK5` avant 1er `EMO+` | (immédiat) | 2.1s (user) | **2.5s** (cf. bot actuel) — laisse stabiliser les OQ post-loot |
| Délai inter-`EMO+` | 300ms fixe | 1300-3900ms (humain) | **300ms** (pattern dyshay éprouvé sur Retro 1.29) |
| Attente OR par item | **AUCUNE** (burst) | N/A | **AUCUNE** — comptabiliser globalement à la fin du burst |
| Timeout fin de burst (attente OR) | (implicite, inventaire resynced) | N/A | **5-8s** après dernier EMO+ |
| Délai avant `EV` | (immédiat post-dernier EMO+) | 6.3s après dernier EMO+ (user) | **~500-1500ms** suffit |
| Rate-limit anti-flood Hystoria | non documenté | survit à 300ms entre paquets divers (combat) | inconnu officiellement ; 300ms = safe par expérience dyshay |

---

## Comportement attendu côté serveur

| Cas | Réponse | Source |
|-----|---------|--------|
| `EMO+` envoyé **avant** ouverture banque | Ignoré silencieusement, pas de OR, pas de EsKO | déduction (le serveur n'a pas de session échange ouverte) |
| `EMO+` envoyé **sur UID inconnu** (item déjà parti / fantôme) | Ignoré silencieusement, pas de OR | observé `PiloteBanque.cs:434-441` (filtre items fantômes template>30000) |
| `EV` envoyé **pendant burst EMO+** | Ferme la banque, les EMO+ restants seront ignorés (`AccountState != STORAGE`) | dyshay `CharacterFrame.cs:113-117` (état STORAGE requis pour les actions banque) |
| `ApS` envoyé en `combat` ou hors zone coffre | Pas de `ECK5` (silence serveur) | observé `PiloteBanque.cs:226-233` (timeout 2s sur ECK5 → abort propre) |
| `EV` user-side (vrai client) pendant burst bot | Ferme la banque côté serveur, le burst restant échoue | observé `PiloteBanque.cs:51-58` (fix BloquerEvClient) |

---

## Recommandations pour le bot (Hystoria/Abrak)

| # | Recommandation | Source/Référence |
|---|---|---|
| 1 | **Ouvrir avec `ApS` (CLAIR)** — pas `DC<npc>` | capture user `ANALYSE-FLOW-BANQUE.md:38-44` |
| 2 | **Attendre `ECK5` (kind exactement = 5)** avec timeout 2s, abort sinon | bot actuel OK `PiloteBanque.cs:219-233` + `TrameJeu.cs:117-127` |
| 3 | **Dépôt en burst 300ms style dyshay**, **PAS d'attente OR par item** | `StoreAllObjectsAction.cs:34-36` (référence absolue) |
| 4 | **Pause stabilisation 2.5s post-`ECK5`** pour absorber les OQ correctifs de loot | bot actuel OK `PiloteBanque.cs:240-246` |
| 5 | **`EMO+` via canal CHIFFRÉ `-`** (action sensible) | capture user `ANALYSE-FLOW-BANQUE.md:107-117` ; bloquer en clair = kick |
| 6 | **Compter les OR globalement** après le burst (timeout 5-8s) | bot actuel OK `PiloteBanque.cs:333-345` |
| 7 | **`EV` via canal CHIFFRÉ `-`** | capture user `ANALYSE-FLOW-BANQUE.md:178-183` |
| 8 | **Bloquer les EV émis par le vrai client Dofus** pendant le workflow (`BloquerEvClient`) | bot actuel OK `PiloteBanque.cs:51-58, 201, 375` |
| 9 | **Whitelist banque côté `ApiBot.EnvoyerHumaniseAsync`** : `ApS, EMO, EV, GA001, GKK0` autorisés ; bloquer GA500, WU... | recommandé `PATTERN-BANQUE-AUTO.md:602-608` — à vérifier dans code actuel |
| 10 | **Filtre catégoriel** (Equip/Ress/Conso/Quete) via `t` byte de `items_merged.json` | bot actuel OK `PiloteBanque.cs:404-535` |
| 11 | **Multi-pass si OR manquants** (max 3 passes, 3s entre) | bot actuel implémenté `PiloteBanque.cs:256-372`, **mais voir Anomalies** |

---

## Anomalies constatées dans le code actuel du bot

### A1. Multi-pass 3 cycles supplémentaires + 3 passes internes = jusqu'à 12 tentatives

`PiloteBanque.cs:127-142` (workflow) bouclent jusqu'à **3 cycles supplémentaires**, et `PiloteBanque.cs:256-372` (`DeposerToutInterneAsync`) boucle **3 passes** par cycle.

- **dyshay** ne fait qu'**1 passe**. Si l'inventaire n'est pas vide post-EMO+, la stratégie est de **re-resync via combat suivant** (OAKO/OQ poussés naturellement), pas de re-tenter.
- Le bot actuel multi-pass parce qu'il observe « 30-50% des EMO+ se font ignorer par le serveur » (`PiloteBanque.cs:249-252`). **Hypothèse** : ces timeouts viennent peut-être de la **désynchronisation cipher** (le canal `-` a un compteur idxProxy qui DOIT être monotone — cf. mémoire `tech_cipher_desync_injection`) plutôt que d'un vrai refus serveur.
- **Divergence majeure** : multi-pass × 3 cycles est lourd et risque l'effet ping-pong (3 timeouts précédents = 45s blacklist farm dans la roadmap CLAUDE.md). dyshay ne fait jamais ça.

### A2. Délai 2.5s post-ECK5 stabilisation OQ

`PiloteBanque.cs:240-246` ajoute **2.5s** d'attente après ECK5 pour laisser les `OQ` post-loot stabiliser les quantités. Ni dyshay ni la capture user n'incluent cette pause (ils ouvrent en idle hors combat → OQ déjà absorbés).

→ Ce délai a du sens **côté bot farm-then-bank** (le combat précédent peut générer des OQ tardifs) mais ne devrait pas être nécessaire en utilisation **batch** (ex: l'user lance « tester maintenant » depuis Astrub). **Tradeoff acceptable**.

### A3. Resync inventaire désactivé

`PiloteBanque.cs:76-85` : la resync via aller-retour map est **commentée** (« forensic 2026-05-28 20:00 : transitions échouent toutes »).

→ Cohérent avec dyshay (pas de resync explicite). Pas d'anomalie, juste une observation.

### A4. Délai inter-EMO+ 300ms = aligné avec dyshay

`PiloteBanque.cs:330` : `await Task.Delay(300, ct)` entre chaque EMO+. **Strictement aligné avec dyshay**. ✅

### A5. Attente OR globale 8s post-burst

`PiloteBanque.cs:338` : `while (attente < 8000 && CompteurObjectRemove < attendu)`. 8s est généreux. dyshay n'attend rien. Pour Hystoria sous charge, 5-8s est raisonnable. ✅

### A6. Suppression optimiste locale **ABSENTE** dans le bot

dyshay `StoreAllObjectsAction.cs:35` fait `inventario.eliminar_Objeto(objeto, 0, false)` **dans la foulée** du SendPacket. Cela évite que la pass suivante voie l'item déjà déposé.

Le bot actuel attend que le `OR` arrive via `TrameJeu.cs:1192` (`MessageObjetRetrait` handler) pour décrémenter `Personnage.Inventaire`. Si le OR n'arrive jamais (timeout, désync, EV intervenu), l'item reste visible localement et **sera re-soumis à la pass suivante** → spam EMO+ déjà déposés serveur-side → confusion totale et toutes les passes après la 1ère sont du bruit.

→ **Suggestion alignement dyshay** : après chaque `EnvoyerAuServeurAsync($"EMO+{uid}|{qte}")`, supprimer **immédiatement** l'item local de `Personnage.Inventaire` (suppression optimiste). Si le OR ne vient pas, la pass suivante ne le re-soumettra pas. Le risque (item resté serveur-side) est faible et corrigé par le prochain `OAKO`/`OQ`.

### A7. Variante PNJ non implémentée

Le bot ne gère **que** l'ouverture coffre interactif (`ApS`). Si l'user se retrouve devant un PNJ banquier classique (Dofus Retro non-Hystoria, autres maps), `ApS` retournera silence serveur et le workflow abort. Pas critique pour Hystoria mais à noter.

### A8. Préfixe `EBM` / `EM<uid>;<qte>;1` historique = bug corrigé

Le bot actuel utilise bien `ApS` + `EMO+<uid>|<qte>` + `EV`. Les anciens préfixes erronés (`EBM`, `EM<uid>;<qte>;1`, dérivés doc cadernis Retro) ont été **corrigés** (cf. `ANALYSE-FLOW-BANQUE.md:262-268`). ✅

### A9. Routing canal chiffré : implicite

`PiloteBanque.cs:213, 326, 377` utilisent `_session.EnvoyerAuServeurAsync(...)`. Selon le commentaire `PiloteBanque.cs:22` (« canal chiffré auto via DoitEtreChiffre »), `SessionProxy` détecte automatiquement les paquets `EMO+`/`EV` et les route via le canal `-`. À **vérifier** dans `SessionProxy.cs` que la liste DoitEtreChiffre contient bien `EMO+` et `EV` — sinon les EMO+ partiraient en clair = kick anti-cheat probable (cf. `ANALYSE-FLOW-BANQUE.md:151`).

---

## Tableau de bord recommandé

```
État du bot vs référence dyshay :

  Ouverture coffre `ApS` (clair)         ✅ aligné
  Attente ECK5 (kind=5)                  ✅ aligné, voire mieux (timeout abort propre)
  Pause 2.5s post-ECK5                   ⚠ extra (acceptable post-combat)
  Burst EMO+ 300ms                       ✅ aligné
  Suppression optimiste locale           ❌ ABSENTE → cause probable des multi-pass
  Attente OR globale post-burst          ✅ amélioration (dyshay n'attend rien)
  Multi-pass × 3 + cycles × 3            ⚠ DIVERGENCE → tend vers le 12-tentatives
  Fermeture `EV` (chiffré)               ✅ aligné
  Whitelist anti-EV-client               ✅ AMÉLIORATION (dyshay n'a pas ce problème)
  Filtre catégories + listes garde       ✅ AMÉLIORATION (dyshay = tout-sauf-équipé)
```

---

## 🎯 Synthèse FINALE

| Étape | Protocole CONFIRMÉ Hystoria | Confiance |
|-------|----------------------------|-----------|
| 1. Ouvrir | `ApS` (CLAIR, sans paramètre) | ★★★★★ capture + dyshay (variante PNJ documentée mais inutilisable Hystoria) |
| 2. Confirmation serveur | `As<stats>` + `ECK5` + `EL[<contenu>]` (clair) | ★★★★★ capture |
| 3. Dépôt | `EMO+<uidInv>\|<qte>` (CHIFFRÉ `-`) | ★★★★★ capture + dyshay |
| 4. Délai inter-dépôt | **300ms fixe** (pattern dyshay, à confirmer comme safe sous charge Hystoria) | ★★★★ dyshay éprouvé Retro 1.29 |
| 5. Ack dépôt serveur | `EsKO+<uidBanque>\|<qte>\|<template>\|` + `OR<persoId>\|<uidInv>` | ★★★★★ capture |
| 6. Pas d'attente OR per-item | dyshay envoie en burst sans wait | ★★★★★ source open-source |
| 7. Fermer | `EV` (CHIFFRÉ `-`) | ★★★★★ capture + dyshay |
| 8. Ack fermeture | `EV` (clair) | ★★★★★ capture |

---

**Divergence majeure entre bot actuel et référence dyshay** : **le bot ne supprime PAS optimistement l'item local après envoi EMO+** → la stratégie multi-pass (3 cycles × 3 passes) tente de compenser ce manquant mais re-spam le serveur avec des EMO+ sur des UIDs déjà déposés, plombant les statistiques et risquant l'effet « anti-bot » serveur. Le fix le plus rentable serait de copier la ligne `inventario.eliminar_Objeto(objeto, 0, false)` de dyshay (`StoreAllObjectsAction.cs:35`) côté `PiloteBanque.cs:326` (juste après `await _session.EnvoyerAuServeurAsync(paquet)`), puis de **réduire MAX_PASSES à 1**.
