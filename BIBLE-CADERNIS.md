# 📖 BIBLE CADERNIS — Tout pour ton projet Bot Dofus Retro 1.29

> **Source** : [cadernis.fr](https://cadernis.fr) (compte Toukiki connecté) + repos GitHub dyshay/salesprendes — compilé le 20/05/2026.
>
> Ce document concentre **TOUT le savoir extrait** des threads cadernis et code source dyshay nécessaires pour comprendre et étendre ton bot Dofus Retro 1.29 sur Hystoria.

---

## 📑 TABLE DES MATIÈRES

1. [Méthode d'accès à cadernis](#-1-méthode-daccès-à-cadernis)
2. [Protocole 1.29 — fondamentaux](#-2-protocole-129--fondamentaux)
3. [Flux de connexion détaillé (auth → jeu)](#-3-flux-de-connexion-détaillé-auth--jeu)
4. [Chiffrement & sécurité (cipher, mdp, mapData)](#-4-chiffrement--sécurité)
5. [MITM & sniffer](#-5-mitm--sniffer)
6. [Cartes & cellules](#-6-cartes--cellules)
7. [Déplacement (GA001 + GKK0)](#-7-déplacement-ga001--gkk0)
8. [Pathfinding](#-8-pathfinding)
9. [Récolte (GA500 + états GDF)](#-9-récolte-ga500--états-gdf)
10. [Combat (GA300, séquence, IA)](#-10-combat-ga300-séquence-ia)
11. [Sorts (XML dyshay, stats par niveau)](#-11-sorts-xml-dyshay-stats-par-niveau)
12. [Zaap / téléport](#-12-zaap--téléport)
13. [Anti-ban & détection](#-13-anti-ban--détection)
14. [Threads cadernis par sujet](#-14-threads-cadernis-par-sujet)
15. [Repos GitHub de référence](#-15-repos-github-de-référence)

---

## 🌐 1. Méthode d'accès à cadernis

### Accès Web
- URL : https://cadernis.fr/
- Compte dédié (jetable) : `toukiki83@gmail.com` (login Toukiki)
- **Sans login** : threads en HTTP 403 → utiliser `WebSearch allowed_domains:["cadernis.fr"]` pour lire les extraits indexés
- **Avec login (Claude in Chrome)** : full content accessible

### Structure du forum
- **Général** (269 threads) — discussions ouvertes
- **Partage** (251) — code partagé, projets open-source
- **Tutoriels** (165) — tutos par version (1.29, 2.0, Touch, Unity 3.0)
- **Projets** (147) — projets bots complets avec captures
- **Questions/Réponses** (1.5K) — Q&A, beaucoup d'or
- **Mises à jour** (10) — annonces Ankama

### Prefixes utiles (pour filtrer)
- `1.29` — Dofus Retro (NOTRE CIBLE)
- `Touch` — Dofus Touch
- `2.0` / `3.0` — Dofus moderne (Unity)
- `C#` / `Python` / `VB.Net` / `Java` / `Autoit` — langage

---

## 📡 2. Protocole 1.29 — fondamentaux

Dofus Retro 1.29 utilise un protocole **texte ASCII** (pas binaire comme D2.0+).

### Structure d'un paquet
```
<préfixe><contenu>\0
```
- Pas de header binaire ni longueur, **terminateur = byte zéro (0x00)**
- Préfixe = 1 à 3 chars alphanumériques (case-sensitive : `Gt` ≠ `GT`)
- Contenu = chaîne ASCII avec séparateurs `|` (champs) et `;` (sous-champs)
- Format compact : tout est dans le préfixe + chaîne

### Conventions de préfixes (1ère lettre)
| Préfixe | Famille |
|---------|---------|
| `A*` | Authentification (login, perso list, serveurs) |
| `B*` | Basique (date, time, ping) |
| `G*` | Gameplay (Move, Action, Map, Combat...) |
| `I*` | Items / inventaire |
| `J*` | Jobs / métiers |
| `O*` | Objets (inventaire détaillé) |
| `S*` | Sorts / Stats |
| `W*` | Waypoints / Zaap |
| `c*` | Chat / commandes |

### Sens des flèches dans les logs
- `[VOCAB C→S]` = Client → Serveur (envoyé par toi)
- `[VOCAB S→C]` = Serveur → Client (reçu)
- `[REENC C→S]` = paquet client réencryté/relayé par le proxy
- `[INJ ->SRV]` = paquet injecté par le bot

---

## 🔐 3. Flux de connexion détaillé (auth → jeu)

Source : thread cadernis `d1-29-questions-protocole.2099` #7 + capture user.

### Séquence complète
```
S→C  HC<vocab>                 handshake (clé chiffrement vocabulaire)
C→S  1.29.1                    version client (peut être 1.48.0 pour Hystoria Abrak)
C→S  <login>\n<pwd># - Crypt - identifiants (mdp chiffré)
C→S  Af                        flag init
S→C  Af1|2|0||-1
S→C  Ad<perso>                 pseudo
S→C  Ac<communauté>
S→C  AH<srv>;<etat>;<pop>;<saison>|...   liste serveurs
C→S  Ax                        demande tickets serveurs
S→C  AlK0
S→C  AQ<question secrète>      (si activée)
S→C  AxK<ticket>|<srv,état>|...
C→S  AX<idServeur>             choix serveur
S→C  AXK<ip:port chiffrés>     adresse serveur de jeu (= AYK Hystoria)
S→C  HG                        handshake jeu
C→S  AT<ticket>                sur le serveur de jeu
S→C  ATK0                      ticket OK
C→S  Ak0                       auth gameserver OK
C→S  AV ; S→C BN ; S→C AV0 ; C→S Agfr ; C→S AL ; C→S Af
S→C  Aq1
S→C  ALK<...>|<id;nom;classe;niv;...>|...   liste persos
C→S  AS<idPerso>               choix perso
S→C  ASK|<id>|<nom>|<classe>|<niv>|...|<inventaire>   perso complet
C→S  GC1                       entrée en jeu (demande map)
S→C  OS<...>                   équipement/stuff
S→C  eL<xp>|<...>              niveau/xp
S→C  JS|<job>;<skill~niv~..>|...   métiers
S→C  JX / JO                   jobs xp / options
S→C  ZS0 ; cC<canaux> ; gS<guilde>
S→C  al|<cell>;<v>|<cell>;<v>|...  DONNÉES CELLULES (LOS/walkable)
S→C  SL<idObj>~<skill>~<lettre>;...  éléments interactifs/récolte
S→C  AR<...> ; Ow<pods>|<podsMax>  PODS
S→C  Im189 / Im0152;<date~ip>  infos session
S→C  GCK|1|<nom>               confirmation entrée en jeu
S→C  As<stats...>              caractéristiques
C→S  BD ; S→C ILS2000
S→C  GDM|<map>|<date>|<mapData hex>   CHANGEMENT MAP + mapData chiffrée
C→S  GI                        demande infos map (entités)
S→C  BT<timestamp>
S→C  fC0
S→C  GDO+<cell>;<idElem>;<etat>;...  OBJETS INTERACTIFS (récolte)
S→C  GA;1;<acteur>;<path>      déplacement broadcast
S→C  hP<id>|<nom>;<x>          position joueurs
S→C  Rp<...>                   groupe/party
S→C  GM|+<cell>;<dir>;...;<id>;<nom>;<classe>;...  entités spawn
S→C  GDK                       CONFIRMATION fin de changement de map
S→C  EW+<id>| ; GDE|<cell>;<etat> ; TT32
```

### Spécificité Hystoria (Abrak v1.48)
- Hostname résolu via `AYK<host>:<port>;<ticket>` (pas IP directe)
- Version reportée : `1.48.0` (au lieu de `1.29.1`)
- Canal chiffré supplémentaire `'-'` (préfixe `-<hex>`) pour les actions sensibles : `GA001`, `GA300`, `GA500`, `GA907`, `GKK0`
- 16 clés rotatives `AK<key0>|<key1>|...|<key15>` (`ATK<keyIdx>` indique la clé active)
- Compteur `idxClient` monotone (1→15 puis wrap, skips 0) dans l'en-tête des paquets chiffrés

---

## 🔐 4. Chiffrement & sécurité

### A. Crypt mot de passe (thread `dofus1-29-systeme-de-cryptage-decryptage-mdp-c.2282`)
Le `Send <login>\n<pwd># - Crypt -` :
```python
HASH = "<alphabet 64 chars>"   # = HashCarte chez nous

def cryptPassword(pwd, key):
    result = "#1"
    for index in range(len(pwd)):
        P = ord(pwd[index])
        K = ord(key[index])
        div = P // 16
        dif = P % 16
        result += HASH[(div + K) % len(HASH)]
        result += HASH[(dif + K) % len(HASH)]
    return result
```
- `key` = clé reçue dans le paquet `HC<vocab>` du serveur
- Utile pour client autonome (émulation auth) ; en MITM le vrai client chiffre déjà

### B. Déchiffrement MapData (thread `systeme-de-decryptage-mapdata-dofus-1-29x.1790`)
La mapData hex arrive dans `GDM|<map>|<date>|<mapData hex>`. Clé = la `<date>`/clé du GDM.

```python
def Checksum(s):
    num = 0
    for c in s:
        num += ord(c) % 16
    return HEX[num % 16]  # table "0".."F"

def PrepareKey(k):
    out = ""
    for i in range(0, len(k) - 1, 2):
        out += str(int(k[i:i+2], 16))[0]   # premier chiffre décimal
    return out

def DecypherData(data, key, cks):
    out = ""
    for i in range(0, len(data) - 1, 2):
        num = int(data[i:i+2], 16)
        nb = round(((i / 2) + cks) % len(key))
        out += chr(num ^ ord(key[nb]))
    return out

# Usage
key = PrepareKey(key)
checksum = int(Checksum(key), 16) * 2
mapData = DecypherData(mapData, key, checksum)
mapData = urllib.parse.unquote(mapData)   # ⚠ unescape final OBLIGATOIRE
```
- Outils GitHub de réf : `github.com/Manghao/DofusMapDataDecypher`, `github.com/hussein-aitlahcen/dofus-map-key`
- Bug classique : oublier le `urllib.unquote` final → caractères hors ASCII

### C. Cipher Hystoria '-'
- Spécifique au serveur privé Hystoria (Abrak v1.48), pas dans le 1.29 vanilla
- Format : `-<hexBytes>`
- Déchiffrement = XOR avec la clé active (16 clés AK, rotation par `idxClient`)
- En MITM : le proxy doit RE-chiffrer ses injections avec le bon `idxProxy`
- Désync = kick (le serveur détecte saut d'index)

---

## 🕸 5. MITM & sniffer

### Architecture MITM standard pour Dofus 1.29
```
[ Client Dofus ] ←→ [ Proxy local (toi) ] ←→ [ Serveur Hystoria ]
       1303              écoute 1303,1304       51.89.153.20:1303
```

1. **Redirection** : le client connecté à `127.0.0.1:1303` au lieu du vrai IP serveur (via DNS / hosts / config)
2. **Relai brut** : tout paquet client → serveur et vice versa, **inchangé** par défaut
3. **Décryption** : si canal chiffré '-', déchiffrer pour logger en clair (`[VOCAB]`)
4. **Injection** : le bot peut envoyer ses propres paquets dans le flux
5. **Re-chiffrement** : si paquet bot doit aller sur canal '-', le chiffrer avec `idxProxy` correct

### Code minimal MITM (inspiré thread `tuto-bot-socket-les-fondamentaux.491`)
```csharp
var serverSocket = new TcpListener(IPAddress.Loopback, 1303);
serverSocket.Start();
var client = await serverSocket.AcceptTcpClientAsync();
var server = new TcpClient("51.89.153.20", 1303);

// Relai bidirectionnel asynchrone
_ = Task.Run(() => Relai(client.GetStream(), server.GetStream()));   // C→S
_ = Task.Run(() => Relai(server.GetStream(), client.GetStream()));   // S→C

async Task Relai(NetworkStream src, NetworkStream dst) {
    var buf = new byte[4096];
    int n;
    while ((n = await src.ReadAsync(buf, 0, buf.Length)) > 0) {
        ProcessPaquet(buf, n);  // log, déchiffrer, parser
        await dst.WriteAsync(buf, 0, n);
    }
}
```

### Sniffer alternatif : raw sockets (thread `sniffer-grace-au-raw-sockets-sur-windows.1870`)
- Capture passive (pas de MITM, pas d'injection)
- Sous Windows : `SocketType.Raw` + `IPHeaderInclude`
- Utile pour audit, pas pour bot actif

---

## 🗺 6. Cartes & cellules

### Structure d'une carte Dofus Retro
- **560 cellules** total en grille **iso losange 14×40**
- Chaque cellule a : `(x, y)`, `type` (marchable/non), `layer1`, `layer2`, `interactif`
- Index linéaire `cellId` 0..559

### Conversion `cellId ↔ (x, y)` (formule dyshay validée)
```csharp
public static (int x, int y) CalculerCoordonnees(int id, int mapWidth = 14) {
    int loc5 = id / ((mapWidth * 2) - 1);    // 27 si mapWidth=14
    int loc6 = id - (loc5 * 27);
    int loc7 = loc6 % mapWidth;
    int y = loc5 - loc7;
    int x = (id - (mapWidth - 1) * y) / mapWidth;
    return (x, y);
}
```

### Distance dans Dofus
- **Chebyshev** : `max(|x1-x2|, |y1-y2|)` — c'est cette métrique qu'utilise le jeu pour la portée des sorts
- Manhattan = pour info, pas utilisé par le jeu

### Encodage cellule en base64 (HashCarte)
```csharp
ALPHABET = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
// 64 chars

EncoderCellule(id) = ALPHABET[id / 64] + ALPHABET[id % 64];   // 2 chars / cell
DecoderCellule(c1,c2) = ALPHABET.IndexOf(c1) * 64 + ALPHABET.IndexOf(c2);
```
Utilisé dans **tous les paquets GA001 et GA0** pour encoder les cellules de manière compacte.

### MapData (déchiffrée) — format
- Suite de cellules concaténées, chacune ~10 octets ASCII
- Champs : `flags`, `layer_ground`, `slope`, `layer_object1`, `layer_object2`, `interactif`, `LOS`
- Bot mode : layer_object2 > 0 → indique cellule interactive (récoltable)
- Bot mode : `IdInteractif >= 0` → arbre/ressource → NON traversable même si épuisée

### Cellules de transition (changement de map)
- Sprites layer1 connus : `1030`, `1029`, `1764`, `2298`, `745`
- Pour aller à la map voisine : marcher SUR la cellule transition → serveur envoie `GDM` (nouvelle map)

### Équations bordure dyshay (`Movimiento.cs`)
```csharp
LEFT   : (x - 1) == y
RIGHT  : (x - 27) == y
BOTTOM : (x + y) == 31
TOP    : y < 0 && (x - |y|) == 1
```
Permet de savoir si une cell est sur la bordure d'une certaine direction. **⚠ Constantes 27/31 calibrées pour grille 14×17 ; sur cartes 15×17 c'est faux** → utiliser plutôt déduction relative aux Transitions de la carte.

---

## 🚶 7. Déplacement (GA001 + GKK0)

### Format GA001
```
GA001<path>
```
Où `path` = suite de couples `[dirChar][cellChar2]`, **compressés par direction** :
- `dirChar` = `a..h` = direction 0..7 (NE/E/SE/S/SW/W/NW/N)
- `cellChar2` = HashCarte.EncoderCellule(cellId)
- On n'émet un nouveau couple QUE si la direction change
- Toujours terminer par direction + cellule finale

### Exemple
Chemin de cell 100 → 102 (vers l'est) → 104 (vers le SE) :
```
GA001 a [encoder(102)] c [encoder(104)]
```
(directions `a`=NE, `b`=E, `c`=SE, etc.)

### Encodage direction (dyshay `Cellule.DirectionVers`)
```csharp
(dx,dy) → dir char :
  ( 0,-1) → 'h' (7=N)
  ( 1,-1) → 'a' (0=NE)
  ( 1, 0) → 'b' (1=E)
  ( 1, 1) → 'c' (2=SE)
  ( 0, 1) → 'd' (3=S)
  (-1, 1) → 'e' (4=SW)
  (-1, 0) → 'f' (5=W)
  (-1,-1) → 'g' (6=NW)
```

### Séquence complète déplacement
1. Client envoie `GA001<path>` (canal '-' chiffré sur Hystoria)
2. Serveur répond `GA;1;<idPerso>;<path>` (= broadcast à tous)
3. Client attend l'animation
4. Client envoie `GKK0` (ack action terminée)

### GKK0 — TIMING CRITIQUE (anti-ban !)
Source : thread `deplacement-dofus-1-29.1767` + thread `autoban-changement-de-map.3285`.

> **Le serveur Dofus a une heuristique anti-bot qui détecte un `GKK0` envoyé trop tôt** (avant la fin de l'animation). Si tu envoies GKK0 trop vite, **autoban changement de map** = ban définitif.

### Calcul de la durée d'animation (`PathFinderUtil.get_Tiempo_Desplazamiento_Mapa`)
```python
moveTime = 20
anim = RUN if nbCells > 6 else WALK   # course si chemin long

for (prev, cur) in zip(path[:-1], path[1:]):
    if prev.Y == cur.Y:
        moveTime += anim.durationHorizontal   # ~250 ms
    elif prev.X == cur.X:
        moveTime += anim.durationVertical     # ~285 ms
    else:
        moveTime += anim.durationLinear       # ~330 ms (diag)
    
    # Ajustements selon élévation
    if prev.level < cur.level:
        moveTime += 100
    elif cur.level > prev.level:
        moveTime -= 100
    elif prev.slope != cur.slope:
        moveTime += 100 if prev.slope == 1 else -100

# moveTime en ms
```

### Recette éprouvée chez nous (commit `b88bd9e`)
- durée = `nbCases × 450 ms`, borné `[2000, 9000]`
- Pour notre cas (Hystoria), valeurs RUN/WALK × H/V/L = dans `DuracionAnimacion`
- ⚠ **NE PAS** baser le calcul sur `pas=(len-5)/2` du token GA001 (1 ligne droite = 1 token ≠ 1 case)
- Si déjà sur cell transition mais carte non changée → re-`GKK0`

---

## 🧭 8. Pathfinding

### Algorithme : A* sur grille 8-directions
Source : `dyshay/Otros/Mapas/Movimiento/Mapas/Pathfinder.cs` + notre `Pathfinder.cs`

```csharp
public static List<Cellule>? Trouver(
    Carte carte,
    Cellule depart,
    Cellule arrivee,
    ICollection<Cellule>? cellulesInterdites = null,
    bool arreterDevant = false,
    int distanceArret = 1)
{
    var ouvertes = new List<Cellule> { depart };
    var fermees = new HashSet<Cellule>(cellulesInterdites ?? new List<Cellule>());
    
    while (ouvertes.Count > 0) {
        // Sélectionne cellule avec F minimal (tie-break G max)
        var courante = ouvertes.OrderBy(c => c.F).ThenByDescending(c => c.G).First();
        
        if (courante == arrivee) return Reconstruire(depart, arrivee);
        
        ouvertes.Remove(courante);
        fermees.Add(courante);
        
        foreach (var voisin in Voisins(carte, courante)) {
            if (fermees.Contains(voisin)) continue;
            if (!voisin.EstMarchable && voisin != arrivee) continue;
            if (voisin.IdInteractif >= 0 && voisin != arrivee) continue;   // ⚠ arbres bloquent
            if (voisin.EstCelluleTeleport() && voisin != arrivee) continue;
            
            int gTemp = courante.G + Distance(voisin, courante);
            if (gTemp < voisin.G) {
                voisin.G = gTemp;
                voisin.H = Heuristique(voisin, arrivee);   // ex: distance Chebyshev
                voisin.F = voisin.G + voisin.H;
                voisin.Parent = courante;
                if (!ouvertes.Contains(voisin)) ouvertes.Add(voisin);
            }
        }
    }
    return null;   // pas de chemin
}
```

### Pièges connus
- **Arbres/ressources bloquent même épuisées** : filtrer `IdInteractif >= 0` (notre fix sinon "saut sans progrès" 13 fois /min)
- **Cellules de transition** : marchables seulement comme destination finale
- **Cellules combattants** : interdites en combat (= obstacles temporaires)
- **Maps 15×17 vs 14×17** : équations bordure dyshay (constantes 27/31) FAUSSES sur Hystoria → utiliser raw:GA001 enregistré, pas calcul

---

## 🌾 9. Récolte (GA500 + états GDF)

### Paquet de récolte
```
C→S : GA500<celluleRessource>;<idSkill>
```
- `idSkill` = OBLIGATOIRE (si absent, serveur **drop la requête en silence**)
- Skills observés : `45` (couper bois), `50` (faucher blé/lin), `68` (cueillir lin/chanvre), `114` (utiliser zaap), `157` (zaap doc dyshay, mais Hystoria utilise 114)

### Séquence complète récolte
```
C→S  GA001<approche>    # se rapprocher de la cellule
S→C  GA;1;<id>;<path>   # broadcast
C→S  GA500<cell>;<skill>
S→C  GAS<id>            # animation start
   ↑ 9 à 12 secondes (niv 1) à 2-3 secondes (niv 100)
S→C  GAF0|<id>          # animation end
S→C  GDF|<cell>;3;0     # état interactif passe à "épuisé"
S→C  OQ<perso>|<uidObjet>,<quantite>   # LOOT
S→C  IQ<perso>|<quantite>              # info quantity
```
⚠ **PAS de GKK0 après GA500** ! Le client relaie son GKK0 de marche, c'est tout. Sinon perd l'OQ/IQ.

### États GDF (`gestion-des-paquets...ressources-recoltable.3268`)
| State | Sens |
|-------|------|
| `5` | Ready, clickable (récoltable) |
| `-1` | Aucun GDF reçu = défaut stable, **assumer 5** |
| `4` | Mid-respawn (animation), **PAS récoltable** (piège classique) |
| `3` | **Depleted** (épuisée) |
| `2` | **Récolte EN COURS** (~3 s) |
| `0,1` | Quasi jamais (autres types d'objets) |

→ `available = state ∈ {-1, 5}`

### Détection « ma récolte a réussi »
- **Compteur monotone OQ** : chaque OQ reçu incrémente un compteur perso
- Boucle d'attente : sort dès que `compteur > avant` (loot reçu) OU `RessourceDisponible == false` + 2s (pris par un autre)
- Plafond global 17s (rare cas : récolte sans OQ)
- Marche pour 12s niv 1 ET 2-3s niv 100 sans changer de code

---

## ⚔ 10. Combat (GA300, séquence, IA)

### Phase combat
1. **Engagement** : `GA907<cell>;<idGroupe>` (agression manuelle) ou `GC<type>` (défi)
2. **Placement** : serveur envoie `GP<cellsEquipe1>|<cellsEquipe2>|0` ; client choisit avec `Gp<cell>` (G majuscule, p MINUSCULE)
3. **Prêt** : client envoie `GR1` (PAS `GRK` ! le K est pour D2.0) ; annule = `GR0`
4. **Combat démarre** : `GS` reçu, puis `GTM` (liste combattants)
5. **Tours** : `GTS<id>|<timerMs>|<numTour>` → tour de quelqu'un
6. **Action** : si mon tour, je cast/bouge/passe
7. **Fin tour** : `Gt` (minuscule !)
8. **Ack** : serveur envoie `GTF<id>` + `GTR<id>` ; client répond `GT` (majuscule)
9. **Fin combat** : `GE0`/`GE1` (victoire/défaite) ; client envoie `GEV` (exit view) puis `GC1` pour reprendre

### Cast de sort
```
C→S : GA300<idSort>;<celluleCible>
```
- Exemple : `GA300183;185` = Ronce (sort 183) sur cellule 185
- Format vu sur 2 threads cadernis (alllstars #2392, nomane #1774) + capture user manuel

### Pass turn — `Gt` (minuscule !)
- ⚠ L'ancien `GE` (majuscule) = FAUX, timeout 45s/tour
- Capture user 12:41:18 confirme `Gt` minuscule sur Hystoria
- `GT` (majuscule) = AUTRE chose : turn ready ack envoyé après GTF/GTR serveur

### Séquence type d'un tour combat (capture user passif)
```
T0       : Serveur envoie GTS401770|45000|6      (mon tour)
T0+1.7s  : Client envoie GA300183;185            (cast Ronce)  ← délai humain
T0+2.0s  : Client envoie GKK0                    (ack action)
T0+3.6s  : Serveur envoie GTF + GTR (fin tour)
T0+3.7s  : Client envoie GT (turn ready ack)
T0+3.9s  : Serveur envoie GTS-1|...|6 (tour ennemi)
```

### Algorithme IA combat (phylonia #1771 « Bonne IA en combat »)
```
Pour chaque sort (trié par priorité) :
  si PA insuffisant → sort suivant
  si dans portée :
    si pas besoin LDV → cast
    sinon : check LDV → cast si ok
  sinon (hors portée) :
    simuler chaque case adjacente (marquant la précédente bloquée)
    dès qu'une position permet de toucher → bouger + cast
```

### Modifications m4x0ubot 1.29.1 (phylonia #1766)
- Compteur PA/PM **live** au début du tour (pas hardcodé 3 PM)
- Pathfinding **évite les alliés** (liste mise à jour à chaque mouvement)
- **Multi-cast par tour** : drain PA jusqu'à épuisement
- LDV "à venir" → priorité haute, pas implémenté chez lui

### Délai humain réaction (capture user)
- Tour 6 : GTS 16:21:58.058 → GA300 16:21:59.754 = **1696 ms**
- Tour 7 : GTS 16:22:02.921 → GA300 16:22:04.494 = **1573 ms**
- Vrais humains : **1.5-1.7 s** entre voir le tour et cliquer
- ⚠ Trop rapide (< 500ms) = signal anti-bot

### Distance combat (dyshay)
- `delay déplacement = 400 + 100 × distance` ms
- En GA001 combat, format identique à overworld
- Cellules combattants vivants = obstacles temporaires

### Anti-pattern : ne jamais envoyer pendant un combat
Paquets BLOQUÉS pendant un combat (sinon kick) :
- `GA500` (récolte)
- `WU`/`WC` (zaap)
- `GA907` (agression sur un autre groupe)
- `IO` (interactions PNJ)
- Tout dialogue PNJ

Paquets AUTORISÉS pendant un combat :
- `GA300` (cast)
- `GA001` (déplacement combat)
- `Gt` (pass turn)
- `GT` (ready ack)
- `GTR<id>` (turn ready)
- `Gp<cell>` (placement)
- `GR1`/`GR0` (prêt)
- `GKK0` (ack action)
- `GQ` (quitter combat)

---

## ✨ 11. Sorts (XML dyshay, stats par niveau)

### ⭐ Source de vérité : `hechizos.xml` de dyshay
- Cloné depuis https://github.com/dyshay/Bot-Dofus-Retro → `Resources/otros/hechizos.xml`
- **281 sorts × 6 niveaux** = ~1500 entrées de stats
- Stats PAR NIVEAU (pas une moyenne)

### Format XML
```xml
<HECHIZO ID="183">
  <NOMBRE>Ronce</NOMBRE>
  <NIVEL NIVEL="1" COSTE_PA="5" RANGO_MINIMO="1" RANGO_MAXIMO="6"
         LANZ_EN_LINEA="FALSE" NECESITA_VISION="TRUE"
         NECESITA_CELDA_LIBRE="FALSE" RANGO_MODIFICABLE="TRUE"
         MAX_LANZ_POR_TURNO="0" MAX_LANZ_POR_OBJETIVO="2" COOLDOWN="0">
    <EFECTO TIPO="97" COOLDOWN="0" OBJETIVO="-1" ZONA="Pa" ES_CRITICO="FALSE" />
    <EFECTO TIPO="97" COOLDOWN="0" OBJETIVO="-1" ZONA="Pa" ES_CRITICO="TRUE" />
  </NIVEL>
  <NIVEL NIVEL="2" ... />
  ...
  <NIVEL NIVEL="5" COSTE_PA="4" RANGO_MINIMO="1" RANGO_MAXIMO="8" ... />
  <NIVEL NIVEL="6" COSTE_PA="3" RANGO_MINIMO="1" RANGO_MAXIMO="8" ... />
</HECHIZO>
```

### Stats par niveau (exemple Ronce)
| Niveau | PA | Range Min | Range Max | Max/cible |
|--------|----|-----|------|-----------|
| 1 | 5 | 1 | 6 | 2 |
| 2 | 5 | 1 | 6 | 2 |
| 3 | 5 | 1 | 7 | 2 |
| 4 | 5 | 1 | 7 | 2 |
| **5** | **4** | **1** | **8** | **2** |
| 6 (max) | 3 | 1 | 8 | 2 |

**⚠ Toujours utiliser les stats du NIVEAU appris**, pas niv 1 par défaut !

### Modèle C# (= dyshay `Spell.cs` + `SpellStats.cs`)
```csharp
public class Spell {
    public int Id;
    public string Nom;
    public Dictionary<int, SpellStats> StatsParNiveau;   // niveau → stats
    
    public SpellStats Stats(int niveau) =>
        StatsParNiveau.TryGetValue(niveau, out var s)
            ? s
            : StatsParNiveau.OrderByDescending(kv => kv.Key)
                            .FirstOrDefault(kv => kv.Key <= niveau).Value;
}

public class SpellStats {
    public int CoutPA;
    public int PorteeMin, PorteeMax;
    public bool LigneSeule, NecessiteLOS, CelluleVide, PorteeModifiable;
    public int MaxParTour, MaxParCible, Cooldown;
    public List<SpellEffect> EffetsNormaux, EffetsCritiques;
}
```

### Paquet `SL` — sorts appris du perso
```
SL<idSort>~<niveau>~<position>;<idSort>~<niveau>~<position>;...
```
- `position` = case raccourci (1-12) sur la barre de sorts, `-1` si pas placé
- Exemple : `SL192~1~-1;193~1~2;195~1~-1;198~1~-1;182~1~4;183~5~1;200~1~3;`
- À parser → `Dictionary<int, int> SortsAppris` (idSort → niveau)

### Classification par catégorie (heuristique nom + description FR)
- **Invocation** : nom/desc contient "invoque", "invocation"
- **Soin** : "rend des points de vie", "soigne", "régénère"
- **Offensif** : "dommage", "dégât", "vole des PV", "explose"
- **Debuff** : "retire des PA/PM", "réduit", "poison", "immobilise"
- **Buff** : "armure", "augmente", "protège", "résistance"
- **Déplacement** : "téléporte", "fait reculer", "saut", "bond"

---

## 🌀 12. Zaap / téléport

### Recette complète Hystoria (capture user 09:23-09:46)
```
1. Pathfinder trouve cell zaap (gfx 7000) sur carte courante
2. Se déplacer en case ADJACENTE (serveur refuse de marcher SUR la cell zaap)
3. C→S : GA500<celluleZaap>;114     ← skill 114 sur Hystoria (PAS 157 doc dyshay !)
4. S→C : WC|<mapId>;<cost>|<mapId2>;<cost2>|...   ← menu zaap (1-2s plus tard)
5. C→S : WU<mapId>                  ← MAJUSCULE, sélection destination
6. S→C : WV                         ← confirmation, perso téléporté
```

### Différence Hystoria vs Retro vanilla
- **Hystoria** : zaap = gfx 7000 + skill **114** ("Utiliser")
- **Retro vanilla / dyshay doc** : skill **157**
- Notre code : `UtiliserZaapAsync(mapDestination)` avec skill 114

### Skill métiers utiles
| Skill | Métier / Action |
|-------|------------------|
| 45 | Couper bois (Bûcheron) |
| 50 | Faucher (Paysan : blé, avoine, houblon, lin, chanvre) |
| 68 | Cueillir (Alchimiste : lin, chanvre, plantes) |
| 89 | Pêcher (Pêcheur) |
| 99 | Miner (Mineur) |
| 114 | Utiliser zaap (Hystoria) |

### Multi-skills (53 ressources)
Catalogue dans `CatalogueInteractifs.cs` :
- Lin gfx 7513 → skill 68 (Cueillir) OU 50 (Faucher) selon métier perso
- Chanvre similaire
- Le `CatalogueInteractifs.SkillCompatible(gfx, skillsPerso)` choisit le bon skill

---

## 🚨 13. Anti-ban & détection

### Heuristiques anti-bot Dofus (qu'il faut éviter)

#### A. Timing
- **Réaction trop rapide** (< 200ms après réception du GTS) = signal bot évident
- **Cadence régulière** (toujours 800ms exactement entre actions) = pattern détectable
- **Use** : random `(1400, 2100)` ms pour réaction + random `(300, 500)` pour ack

#### B. Séquence de paquets
- **GKK0 envoyé trop tôt** après GA001 = autoban changement de map (thread `autoban-changement-de-map.3285`)
- **Paquet sans GKK0 d'ack** où il en faut un = comportement non-client
- **Paquets dans le mauvais ordre** (Gt avant GKK0) = désync probable

#### C. Comportement
- **Pas de "Trop de Spam"** : si tu vois `Trop de spam` dans le chat, STOPPE TOUT
- **Donjons / arènes** : zones surveillées, modos peuvent passer
- **24h/24** : ban après ~2 semaines sans pause (témoignage `nonox #3001`)
- **Pause aléatoire** : ne PAS jouer toujours aux mêmes heures

#### D. Multi-account / VM
- Les **machines virtuelles** sont (parfois) détectées et bannies — fingerprint
- Multi-comptes simultanés sur même IP = peu risqué sur Retro mais surveillé sur 3.0
- ⚠ Hystoria a peut-être ses propres heuristiques

### Témoignages bans 1.29 Retro
- thread `nonox #3001` (24/10/2023) : « ban après 2 semaines de 24h/24 »
- thread `3297` (Touch/Unity, primitive swap) : ban 2h après hook crypto
- thread `3285` (Touch) : autoban changement de map = GKK0 timing

### Mode passif (essentiel pour capturer)
- Bot devient sniffer pur (n'envoie RIEN auto)
- Permet de capturer le protocole avec ton vrai client Dofus
- Indispensable pour reverse-engineering propre

---

## 📚 14. Threads cadernis par sujet

### Protocole 1.29 — fondamentaux
- **`protocol-dofus-1-29.1822`** ⭐ — doc protocole complet
- **`d1-29-questions-protocole.2099`** ⭐ — Q/R protocole, flux complet auth+jeu
- **`protocole-dofus-1-29.1552`** — duplicat
- **`comprendre-le-protocole-de-dofus.115`** — vue d'ensemble (D1+D2)
- **`tuto-identifier-les-packets.143`** — méthode reverse
- **`de-lanalyse-des-paquets.1056`** — théorie analyse paquets

### Déplacement / Pathfinding
- **`deplacement-dofus-1-29.1767`** ⭐ — discussion GKK0 timing, fix communauté
- **`mitm-1-29-déplacements.1650`** — code parsing GA0 (M4x0uBot)
- **`autoban-changement-de-map.3285`** — pourquoi le serveur ban sur changement map

### Cartes / MapData
- **`systeme-de-decryptage-mapdata-dofus-1-29x.1790`** ⭐ — algo déchiffrement
- **`interpretation-mapdata-dechiffree-1-29.2034`** — après décodage, comment lire
- **`decryptage-des-maps-1-29.2225`** — variante
- **`gestion-des-paquets...ressources-recoltable.3268`** ⭐ — états GDF complets

### Crypto / Auth
- **`dofus1-29-systeme-de-cryptage-decryptage-mdp-c.2282`** ⭐ — algo cryptPassword
- **`authentification-1-35-5-retro-monocompte.2744`** — variante 1.35.5

### MITM / Sniffer
- **`tutoriel-faire-un-bot-mitm-a-partir-de-zero.773`** — C# (mais D2.0)
- **`tuto-bot-socket-les-fondamentaux.491`** — C# bot socket
- **`mitm-sur-dofus-retro.2480`** — MITM spécifique Retro
- **`sniffer-grace-au-raw-sockets-sur-windows.1870`** — sniffer raw
- **`creation-bot-mitm-dofus-retro.3300`** — projet bot MITM récent
- **`sockets-1-29-probleme-de-deplacement-apres-un-combat.1585`** ⭐ — code VB.Net WUkzu complet (`GA500cell;skill + GKK0`, fauche, combat)

### Combat / IA
- **`bonne-ia-en-combat.1771`** ⭐ — algo IA combat phylonia
- **`ma-petite-modification-du-m4x0ubot-d-1-29-1.1766`** ⭐ — modifs m4x0ubot (PA/PM live, LDV)
- **`dofus-bot-1-29-2-sorts.1774`** — multi-cast par tour
- **`up-lancer-un-sort.411`** — paquet cast (D2.0)
- **`probleme-paquet-ga-envoie-de-sort.2392`** ⭐ — confirme `GA300<id>;<cell>` 1.29
- **`debutant-creer-son-propre-bot-combat.3001`** — questions débutant + anti-bot
- **`autoit-v3-deplacement-en-combat-pour-d-v2.146`** — déplacement en combat AutoIt
- **`cree-un-bot-d-1-29-avec-actionaz-3.1015`** — protecteurs ressources spécifiquement

### Projets bot open-source (à fouiller pour code)
- **`bon-voici-le-code-source-entier-de-snowbot.3223`** — Snowbot complet
- **`tous-les-scripts-snowbot-sans-chiffrement.3222`** — scripts seuls
- **`nebular-bot-dofus-retro.2990`** — Nebular C#
- **`creation-de-mon-bot-mitm-dofus-retro.3039`** — Python
- **`hook-dofus-retro.3129`** — hook DLL retro
- **`emulateur-1-29.1268`** — émulateur serveur (utile pour comprendre)
- **`d1proxy-sniffer-et-samuser.2208`** — D1Proxy MITM Java
- **`bot-recolte-retro-retour-dxp.3196`** — récolte Python+Claude

### Anti-ban / Détection
- **`autoban-changement-de-map.3285`** ⭐
- **`ban-detection-dun-client-retro-sur-lequel-on-lit-des-clefs.3297`** — Dofus 3.0 (info)
- **`simulation-clics-injection-dll-detectable-retro.3136`** — détection injection
- **`la-pave-de-ma-vision-sur-lanti-bot-et-son-fonctionnement.2419`** — anti-bot Ankama

### Outils / Tools
- **`azur_tools-1-29.2186`** — outils 1.29
- **`parseur-de-protocole.2465`** — parseur générique
- **`extraction-adresse-ip-bot-dofus-retro.3084`** — récupération IP serveur
- **`hook-dofus-retro.3129`** — hooks Retro

---

## 💻 15. Repos GitHub de référence

### `dyshay/Bot-Dofus-Retro` ⭐⭐⭐ (notre modèle principal)
- URL : https://github.com/dyshay/Bot-Dofus-Retro
- Langue : C# .NET Framework
- Auteur : **Alvaro Prendes** (alias salesprendes) — voir copyright dans les fichiers
- Contient :
  - `Otros/Game/Character/Spells/Spell.cs` + `SpellStats.cs` — modèle sorts par niveau
  - `Otros/Peleas/SpellsManager.cs` — IA combat (Lanzar_Hechizo, Mover_Lanzar, Reculer_Et_Lancer)
  - `Otros/Peleas/Fight.cs` + `FightExtensions.cs` — gestion combat
  - `Otros/Peleas/Configuracion/PeleaConf.cs` — config combat persistée
  - `Otros/Mapas/Movimiento/Mapas/Pathfinder.cs` — A* pathfinding
  - `Otros/Mapas/Movimiento/Movimiento.cs` — changement de map, équations bordure
  - `Utilities/Crypto/Hash.cs` — alphabet 64 chars, encodage cellule
  - `Resources/otros/hechizos.xml` — **table complète sorts × niveaux** (841 KB)

### Autres repos utiles
- **`salesprendes/Bot_Dofus_Retro`** — souche dyshay (fork direct)
- **`Manghao/DofusMapDataDecypher`** — déchiffrement mapData 1.29
- **`hussein-aitlahcen/dofus-map-key`** — key cracker mapData
- **`Romain-P/?`** — outils 1.29 (à explorer)
- **`Lisciowsky/?`** — autre impl Retro
- **`SwiTool/DofucksReborn`** — bot 1.29 open-source (thread cadernis #2017)

### Notre bot (Luffy-bot)
- Worktree principal : `C:\Users\touki\Desktop\Mélange\Bot-dofus`
- Doc CLAUDE : `CLAUDE.md` à la racine
- Résumé projet : `RESUME-PROJET.md` à la racine
- XML dyshay : `Resources/data/hechizos_dyshay.xml`

---

## 🎯 RÈGLES D'OR POUR HYSTORIA (synthèse)

1. **Paquets clés avec leurs casse EXACTE**
   - `Gp` (G maj, p min) pour placement — PAS `GP`
   - `Gt` (g min, t min) pour pass turn — PAS `GE` ni `Gt`
   - `GR1`/`GR0` pour prêt — PAS `GRK`/`GRF`
   - `GT` (majuscule) pour turn ready ack — DIFFÉRENT de `Gt`

2. **GKK0 systématique** après chaque action sensible
   - `GA001<chemin>` → `GKK0`
   - `GA300<sort>;<cell>` → `GKK0`
   - `GA500<cell>;<skill>` → ⚠ PAS de `GKK0` ! (le client n'en envoie pas)

3. **Timing humanisé OBLIGATOIRE**
   - Réaction GTS → cast : `Random(1400, 2100)` ms
   - Entre 2 paquets : `Random(300, 500)` ms minimum
   - Pause après cast avant Gt : `Random(1000, 1500)` ms

4. **Stats sorts TOUJOURS par niveau**
   - Lire depuis `hechizos.xml` (281 sorts × 6 niveaux)
   - **JAMAIS** se baser sur des valeurs hardcodées niv 1

5. **Mode passif quand on capture**
   - Toggle l'UI pour que le bot soit observateur pur
   - Indispensable pour analyser un nouveau paquet/comportement

6. **Filtre canal '-' pour Hystoria**
   - Tout paquet du canal chiffré doit passer par `_canalAbrak.EnvoyerCsVersServeur`
   - Compteur `idxProxy` monotone, jamais réutilisé
   - Désync = kick immédiat

7. **En combat, paquets STRICTEMENT autorisés**
   - Whitelist : `GA300, GA001, Gt, GT, GTR, Gp, GP, GR, GKK0, GQ`
   - Tout le reste (GA500 récolte, WU zaap...) bloqué silencieusement par le proxy

8. **Pathfinder évite OBLIGATOIREMENT**
   - Cellules `IdInteractif >= 0` (arbres/ressources, même épuisées)
   - Cellules de transition (sauf comme destination finale)
   - Cellules de combattants vivants (en combat)

---

## 📝 NOTES FINALES

- Ce document est un **résumé condensé** des threads cadernis pertinents. Pour les détails approfondis, lire les threads directement (URLs dans § 14).
- Le code source de référence reste `dyshay/Bot-Dofus-Retro` — c'est le bot le plus propre et le mieux documenté.
- SynFus (bot commercial Hystoria) est basé sur dyshay avec extensions privées (UI, conditions combat avancées).
- Les particularités Hystoria (Abrak v1.48, canal '-' chiffré, skill 114 zaap, sorts custom) sont gérées par notre code en dérivation propre.

**Pour aller plus loin** : consulter `CLAUDE.md` (architecture projet) et `RESUME-PROJET.md` (état/roadmap actuels).

🌿 Bon développement !
