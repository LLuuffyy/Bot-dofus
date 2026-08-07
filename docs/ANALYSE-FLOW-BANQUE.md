# ANALYSE-FLOW-BANQUE — Protocole banque Hystoria (Dofus Retro 1.29)

> **Source** : `BotDofus.Wpf/bin/Debug/net8.0-windows/logs/botdofus-20260521-160621.log`
> **Date capture** : 2026-05-21 16:08:07 → 16:08:26 (~19s)
> **Compte** : Beiloddurul (Sadida niv 19, id 401770), Zeliox83
> **Mode** : sniffer (MITM passif, l'user a manipulé manuellement le client Dofus)
> **Map** : #10303 (Astrub ?) — coffre banque cell ~395/403/424/433/437 (GDF)

---

## Préambule

Cette analyse documente le **protocole exact** d'utilisation de la banque (coffre) sur le serveur privé **Hystoria** capturé via le proxy MITM en mode passif. Le code actuel dans `Divers/Banque/PiloteBanque.cs` utilise des paquets `EBM` / `EM<uid>;<qte>;1` / `EV` qui sont des **hypothèses non vérifiées** issues de la doc dyshay/cadernis. La capture montre une réalité **différente** : Hystoria utilise un coffre interactif (`ApS`), pas le dialogue banquier (`DC`/`DR`).

### Vue d'ensemble de la séquence (manipulation user)

| t | Event | Direction | Paquet |
|---|-------|-----------|--------|
| 16:08:07.612 | Ouverture banque | C→S **CLAIR** | `ApS` |
| 16:08:07.660 | Confirmation ouverture | S→C | `ECK5` puis `EL` |
| 16:08:09.755 | Dépôt item #1 (1 ex.) | C→S **CHIFFRÉ '-'** | `EMO+8007499756\|1` |
| 16:08:09.793 | Confirmation dépôt | S→C | `EsKO+8016316507\|1\|728\|` + `OR401770\|8007499756` |
| 16:08:11.090 | Dépôt item #2 (1 ex.) | C→S **CHIFFRÉ '-'** | `EMO+7964523660\|1` |
| 16:08:15.001 | Dépôt item #3 (42 ex.) | C→S **CHIFFRÉ '-'** | `EMO+7999301324\|42` |
| 16:08:17.274 | Dépôt item #4 (1775 ex.) | C→S **CHIFFRÉ '-'** | `EMO+7994252552\|1775` |
| 16:08:20.048 | Dépôt item #5 (2864 ex.) | C→S **CHIFFRÉ '-'** | `EMO+7972913998\|2864` |
| 16:08:26.323 | Fermeture banque | C→S **CHIFFRÉ '-'** | `EV` |
| 16:08:26.361 | Confirmation fermeture | S→C | `EV` |

---

## Phase 1 — Ouverture banque

### Pas de dialogue NPC banquier !

L'user n'a **PAS** parlé au PNJ banquier. Aucun `DC<id>` (Dialog Create), aucun `DR<reponse>` (Dialog Reply). Le doc cadernis (`BIBLE-CADERNIS-V3.md` ligne 453) est **OBSOLÈTE pour Hystoria** sur ce point — sur Hystoria il existe un **coffre interactif** posé sur la map, et l'user clique dessus → ouverture directe d'un échange en mode 5 (banque).

### Paquet client : `ApS` (CLAIR, ASCII, 4 octets)

```
[16:08:07.612] [OBS-BRUT C→S] #15 len=4 b0=0x41'A' ascii=non Shield0xF9=non aperçu='ApS·'
[16:08:07.612] [VOCAB C→S] ApS
```

**Interprétation** : `Ap` = « Action proximity » / « Activer interactif », `S` = mode « Storage » (banque). Pas de paramètre (le serveur retrouve le coffre via la position du perso, et c'est probablement la map qui contient un GDF qui marque la cell coffre). C'est un paquet en **clair**, comme `GI`, `Af`, `GC1` — pas le canal `-` chiffré.

### Réponse serveur

```
[16:08:07.658] Info #0 :                                       ← Im (info trad vide)
[16:08:07.660] [PKT S→C] As153374,142000,171000|...            ← stats perso (poids/kamas refresh)
[16:08:07.660] [VOCAB S→C] ECK5                                ← Échange Créé Kind=5 (banque)
[16:08:07.662] [VOCAB S→C] EL                                  ← Échange Liste (vide → banque vide)
```

- **`As<stats>`** : refresh complet stats (PV, PA, PM, kamas, **poids/pods**) — montrer le pods libre au joueur dans l'UI échange.
- **`ECK<kind>`** : Échange Créé, `kind=5` = banque. Codes connus : `4` = échange joueur, `1` = HDV, `5` = banque, etc.
- **`EL`** : Échange Liste — vide ici car la banque ne contient rien à afficher (l'user était venu déposer, pas retirer). Sinon, le format serait `EL<liste objets>` similaire au format `O+` de l'inventaire (`uid|template|qte|effets`).

### Coût en kamas

**ZÉRO** dans cette session. Pas de paquet « kamas–X » côté serveur. Sur Hystoria, ouvrir un coffre banque (`ApS`) **n'a pas de coût** — contrairement au dialogue banquier classique (50/100k/500k kamas). C'est la différence majeure avec la doc cadernis V3 ligne 456.

---

## Phase 2 — Liste des items en banque

### Banque vide → `EL` seul (sans contenu)

```
[16:08:07.662] [VOCAB S→C] EL
```

Ici, **pas d'items dans la banque** au moment de l'ouverture. Le serveur envoie juste `EL` (préfixe + vide). Si la banque contenait des items, le format attendu (par analogie avec `ASK|<inventaire>` et l'inventaire perso) serait :

```
EL<uid>|<template>|<qte>|<effets>|...    (un objet par bloc, séparateur `|`)
```

**À reconfirmer** sur une session où la banque a des items. Pour le bot, on peut s'autoriser à ignorer ce contenu (on est en mode dépôt unilatéral) — mais il faut **parser le préfixe `EL`** pour détecter que l'échange est bien synchronisé.

### Refresh stats persage : `As<...>`

```
[PKT S→C] As153374,142000,171000|9037|35|8|0~0,0,0,0,0,0|160,160|9740,10000|131|101|...
```

Le bloc 0 = `xp_actuel,xp_min_niveau,xp_max_niveau`, bloc 1 = `kamas`, bloc 2 = `points_caract`, bloc 3 = `points_sort`, bloc 4 = `vie_actuelle~vie_max`, bloc 5 = `energie`, **bloc 6 = `pods_actuel,pods_max`** (`9740,10000`). C'est crucial : avant et après chaque dépôt, le serveur **renvoie `As`** pour mettre à jour les pods.

---

## Phase 3 — Dépôt d'items

### 5 dépôts successifs observés

Chaque dépôt suit le **même pattern** :

```
C→S CHIFFRÉ '-' : EMO+<uid>|<quantite>
S→C            : EsKO+<uid serveur>|<qte>|<id banque>|     ← confirmation côté banque
S→C            : OR<persoId>|<uid>                          ← Object Remove inventaire
S→C            : As<stats>                                  ← refresh pods (parfois)
```

#### Dépôt #1 — uid 8007499756, qte 1

```
[16:08:09.755] [OBS-BRUT C→S] #17 len=40 b0=0x2D'-'
[16:08:09.755] [OBS C→S '-'] #4 idxClé=5 cks='0' déchiffrable=OUI → clair='EMO+8007499756|1'
[16:08:09.755] [VOCAB C→S] EMO+8007499756|1
[16:08:09.755] [REENC C→S] #4 idxProxy=5 relayé idxClient=5 clair='EMO+8007499756|1'

[16:08:09.793] [VOCAB S→C] EsKO+8016316507|1|728|
[16:08:09.793] [VOCAB S→C] OR401770|8007499756
[INV] -1 objet (id 8007499756, total = 101)
```

**Interprétation paquet `EMO+<uid>|<qte>` :**
- `E` = Échange
- `M` = Move
- `O` = Object
- `+` = sens **depot** (perso → banque). Le `-` serait retrait (banque → perso).
- `<uid>` = uid de l'objet dans l'inventaire perso (10-digit Hystoria, `long`)
- `<qte>` = quantité à déposer

**Interprétation paquet `EsKO+<uidBanque>|<qte>|<template>|` (S→C) :**
- `EsK` = Échange success K (ack).
- `O+` = objet ajouté côté **banque** (sens +).
- `<uidBanque>` = nouvel UID que la banque attribue à l'objet (ici `8016316507` ≠ uid inventaire `8007499756` !). À noter : **l'UID change** quand l'objet passe de l'inventaire à la banque.
- `<qte>` = 1
- `<template>` = `728` ← **id template item** (table items, à mapper sur `Resources/data/items_*.json`)

**Interprétation paquet `OR<persoId>|<uid>` (S→C) :**
- `OR` = Object Remove (déjà géré par `TrameJeu` → `_inventaire.SupprimerObjet`).
- L'inventaire local diminue de 1 objet (`total = 101` → 100 etc).

#### Dépôts suivants (résumé)

| # | t | Paquet client | template | qte |
|---|---|---------------|----------|-----|
| 1 | 16:08:09.755 | `EMO+8007499756\|1` | 728 | 1 |
| 2 | 16:08:11.090 | `EMO+7964523660\|1` | ? | 1 |
| 3 | 16:08:15.001 | `EMO+7999301324\|42` | ? (probable consommable) | 42 |
| 4 | 16:08:17.274 | `EMO+7994252552\|1775` | ? (probable **blé** ou **bois**) | **1775** |
| 5 | 16:08:20.048 | `EMO+7972913998\|2864` | ? (probable **blé** ou **bois**) | **2864** |

Les templates `728` (item #1) sont à mapper sur la base d'items. Les grandes quantités (1775, 2864) sont cohérentes avec **du blé** et **du bois** (les deux récoltes du compte). Le `42` est probablement une ressource alchimiste (lin, ortie, sauge…).

### ⚠ Canal CHIFFRÉ obligatoire

Le paquet `EMO+...` part **toujours sur le canal chiffré '-'** (incrémentation `idxClé`). Le code actuel du `PiloteBanque` envoie en clair via `EnvoyerAuServeurAsync` → **ÇA NE PASSERA PAS** sur Hystoria et **risque un kick anti-cheat**. Il faut router via `_canalAbrak.EnvoyerCsVersServeur` (route cipher) — comme `GA300`, `GA001`, `GA500`.

### Délais entre dépôts (humanisation observée)

```
#1 → #2 : 1.335 s
#2 → #3 : 3.910 s
#3 → #4 : 2.274 s
#4 → #5 : 2.774 s
```

Plage **1.3-3.9s** entre dépôts manuels. Pour humaniser, viser **1500-3000ms** randomisé.

### Pas de paquet « confirmation OK » global

Chaque dépôt est acquitté individuellement par `EsKO+...` + `OR...`. **Pas de `EK` global** comme dans certaines docs Retro. Le bot doit attendre `OR<persoId>|<uidEnvoyé>` ou `EsKO+...` pour passer au suivant.

### Aucun `GKK0` requis

Contrairement aux actions GA001/GA300/GA500, **le dépôt banque ne nécessite PAS de `GKK0` ack**. La séquence est purement Échange (E*) et non Game-Action (GA*).

---

## Phase 4 — Fermeture banque

### Paquet client : `EV` (CHIFFRÉ '-')

```
[16:08:26.323] [OBS-BRUT C→S] #22 len=8 b0=0x2D'-' aperçu='-ABD710·'
[16:08:26.323] [OBS C→S '-'] #9 idxClé=10 cks='B' déchiffrable=OUI → clair='EV'
[16:08:26.323] [VOCAB C→S] EV
[16:08:26.324] [REENC C→S] #9 idxProxy=10 relayé idxClient=10 clair='EV'
```

**Interprétation** : `EV` = **Exchange leaVe** (quitter l'échange). Envoi via canal chiffré `-` (action sensible).

### Réponse serveur : `EV` (S→C)

```
[16:08:26.361] [VOCAB S→C] EV
```

Le serveur renvoie symétriquement `EV` pour confirmer que l'échange est clos. Pas de paramètre. Pas de refresh stats explicite (les pods ont déjà été mis à jour à chaque dépôt).

**Latence ouverture → fermeture** : 19s total (5 dépôts + délais humains).

---

## Phase 5 — Reprise (post-fermeture)

```
[16:08:28.765] GA001agRbhj                          ← user marche
[16:08:28.903] GA0;1;401770;agNagRbhj               ← serveur confirme
[16:08:30.560] GKK0                                  ← ack
[16:08:30.601] GA;2;401770;
[16:08:30.601] GM|-401770                            ← perso quitte la map
[16:08:30.603] GDM|10306|...                         ← change map vers #10306
```

Après `EV`, le client est **immédiatement libre** : l'user peut marcher (`GA001`), changer de map (`GDM`), recevoir des messages chat (`cMK`). **Aucun reset d'inventaire** côté serveur post-fermeture : les `OR` reçus pendant les dépôts ont déjà nettoyé l'inventaire local. L'inventaire est cohérent.

À noter : `As` est renvoyé à chaque transition pour synchro pods.

---

## Tableau récap — Action → Paquet client → Paquet serveur

| Action | Paquet client | Canal | Réponse serveur | Notes |
|--------|---------------|-------|-----------------|-------|
| **Ouvrir banque** | `ApS` | CLAIR | `As<stats>` + `ECK5` + `EL` | Pas de dialogue NPC, coffre interactif |
| **Lister contenu** | (implicite, dans `EL`) | — | `EL<items>` (vide si pas d'items) | Format à confirmer |
| **Déposer item** | `EMO+<uidInv>\|<qte>` | **CHIFFRÉ '-'** | `EsKO+<uidBanque>\|<qte>\|<template>\|` + `OR<persoId>\|<uidInv>` | UID change inventaire↔banque |
| **Retirer item** (non observé) | `EMO-<uidBanque>\|<qte>` (déduit) | **CHIFFRÉ '-'** | `EsKO-...` + `OA<...>` (probable) | À capturer |
| **Fermer banque** | `EV` | **CHIFFRÉ '-'** | `EV` | Symétrique |

---

## Recommandations pour automatiser

### Séquence exacte à implémenter dans `PiloteBanque.cs`

```csharp
// 1. Arriver sur la map banque + se positionner près du coffre
//    (déjà géré par le pilote actuel via zaap + pathfinder)

// 2. Ouvrir la banque
await _session.EnvoyerAuServeurAsync("ApS").ConfigureAwait(false);   // CLAIR — pas le canal '-'
// Attendre la réponse serveur : ECK5 + EL
await AttendreReponseAsync(prefixe: "ECK5", timeout: 3000).ConfigureAwait(false);

// 3. Pour chaque item à déposer :
foreach (var item in itemsADeposer)
{
    // ⚠ CANAL CHIFFRÉ '-' obligatoire (action sensible)
    var paquet = $"EMO+{item.Uid}|{item.Quantite}";
    await _session.EnvoyerCipherAsync(paquet).ConfigureAwait(false);

    // Attendre confirmation OR<perso>|<uid> (= objet retiré inventaire)
    await AttendreObjectRemoveAsync(item.Uid, timeout: 3000).ConfigureAwait(false);

    // Délai humanisé 1500-3000ms
    await DelaiHumaniseAsync(1500, 3000).ConfigureAwait(false);
}

// 4. Fermer la banque (canal '-')
await _session.EnvoyerCipherAsync("EV").ConfigureAwait(false);
await AttendreReponseAsync(prefixe: "EV", timeout: 2000).ConfigureAwait(false);
```

### Modifications minimales à faire dans `PiloteBanque.cs`

1. Ligne 90 : remplacer `"EBM"` par `"ApS"` (envoi clair).
2. Ligne 108 : remplacer `$"EM{item.Identifiant};{item.Quantite};1"` par `$"EMO+{item.Identifiant}|{item.Quantite}"` (UID + qte, séparateur **`|`** pas `;`, et **prefixe `EMO+`** pas `EM`).
3. Ligne 108 : **router via le canal chiffré `-`** (`_canalAbrak.EnvoyerCsVersServeur` sous `_verrouCs`) — `EnvoyerAuServeurAsync` en clair ferait kick.
4. Ligne 114 : `"EV"` est correct **MAIS** doit aussi passer par le canal chiffré `-`.
5. Ajouter une attente `ECK5` après `ApS` avant de commencer les dépôts.
6. Ajouter une attente `OR<persoId>|<uid>` (ou `EsKO+...|<uid>|...`) entre chaque dépôt.

### Filet de sécurité côté `ApiBot.EnvoyerHumaniseAsync`

Comme pour le combat (whitelist `GA300, GA001, Gt, GT, GTR, Gp, GP, GR, GKK0`), prévoir une whitelist d'échange quand `EtatJeu.Banque.EstOuverte == true` :
```csharp
ApS, EMO, EV, GA001, GKK0
```
Bloquer les autres paquets (récolte GA500, zaap WU…) pendant que la banque est ouverte → évite les kicks.

### Identification des items à déposer

Le pilote a besoin de **l'UID** de chaque item (pas le template). L'UID est dans `Personnage.Inventaire.Objets[i].Identifiant` (déjà stocké en `long`). Le filtrage (« déposer si Type ∈ {Ressources, Récolte}, garder Consommables ») se fait via le **template** mappé sur `Resources/data/items_*.json`.

---

## 🎯 PROTOCOLE BANQUE HYSTORIA CONFIRMÉ

À utiliser dans `Divers/Banque/PiloteBanque.cs` :

| Étape | Paquet | Canal | Attente serveur avant suite |
|-------|--------|-------|------------------------------|
| 1. Ouvrir | `ApS` | **CLAIR** | `ECK5` (+ optionnel `EL`) |
| 2. Déposer | `EMO+<uidInv>\|<qte>` | **CHIFFRÉ '-'** | `OR<persoId>\|<uidInv>` (objet sorti de l'inv) ou `EsKO+...` |
| 3. Fermer | `EV` | **CHIFFRÉ '-'** | `EV` (S→C) |

**Tout est confirmé par capture user réelle 2026-05-21 16:08:07-26.** Plus d'hypothèse `EBM` / `EM<uid>;<qte>;1` — ces deux paquets sont **faux pour Hystoria**.

### Pièges à éviter
- `EBM` → **FAUX**, le vrai paquet d'ouverture est `ApS` (clair).
- `EM<uid>;<qte>;1` → **FAUX**, le vrai paquet de dépôt est `EMO+<uid>|<qte>` (séparateur `|` pas `;`, préfixe `EMO+` pas `EM`, **canal chiffré**).
- Envoyer `EMO+...` en clair → kick anti-cheat probable (action sensible doit être chiffrée).
- Oublier d'attendre `ECK5` → le serveur n'a pas fini d'ouvrir l'échange, les dépôts seront rejetés.
- Oublier d'attendre `OR<persoId>|<uid>` entre dépôts → spam paquets → kick anti-flood.
- Confondre `EV` avec `EV0` ou autre — sur Hystoria c'est juste `EV` (2 caractères) dans les deux sens.
