# 📖 BIBLE CADERNIS V2 — Extension : crawl massif (50+ threads)

> **Suite/extension** de `BIBLE-CADERNIS.md`. Compilé le 20/05/2026 après un second passage approfondi (45+ threads lus en détail).
>
> Ce V2 **complète** le V1 — ne le remplace pas. Les sujets déjà couverts en V1 ne sont pas répétés ici, seulement les nouveautés/approfondissements.

---

## 📑 SOMMAIRE V2

1. [Décodage MapData — algorithme complet bit-à-bit](#-1-décodage-mapdata--algorithme-complet)
2. [États GDF des ressources — table complète](#-2-états-gdf-des-ressources-)
3. [Hook DLL Dofus Retro — code C++ MinHook](#-3-hook-dll-dofus-retro)
4. [Architecture serveur émulateur 1.29](#-4-architecture-serveur-émulateur-129)
5. [Approches MITM comparées (proxy, frida, config.xml)](#-5-approches-mitm-comparées)
6. [Splitting des paquets ≤1024 bytes (anti-bot)](#-6-splitting-des-paquets-1024-bytes)
7. [Bots open-source de référence](#-7-bots-open-source-de-référence)
8. [Bot récolte MoundirEsport — approche moderne 2026](#-8-bot-récolte-moderne-2026)
9. [Anatomie d'un paquet binaire D2.0 (pour culture)](#-9-anatomie-paquet-d20)
10. [Sources désobfusquées clients Retro (Dofedex)](#-10-sources-désobfusquées-dofedex)
11. [Récupération IP serveur via packet sniffing](#-11-récupération-ip-serveur)
12. [Index complet des 50+ threads lus](#-12-index-complet)

---

## 🗺 1. Décodage MapData — algorithme complet

Source : thread `interpretation-mapdata-dechiffree-1-29.2034` (Aichan, C# code)

### Format
Après déchiffrement (cf V1 §4), la mapData est une chaîne où **chaque cellule = 10 caractères** encodés dans l'alphabet base64 :
```
ZKARRAY2 = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_"
```

### Code de décodage cellule (C#)
```csharp
private static readonly string ZKARRAY2 =
    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";

public static int HashCodeIndex(char c) => ZKARRAY2.IndexOf(c);

private Cell UncompressCell(string data)  // data = 10 chars
{
    Cell cell = default;
    int[] d = new int[10];
    for (int i = data.Length - 1; i >= 0; i--)
        d[i] = HashCodeIndex(data[i]);

    cell.Movement = (d[2] & 56) >> 3;                          // bits mouvement
    cell.LayerObject2Num = ((d[0] & 2) << 12)
                         + ((d[7] & 1) << 12)
                         + (d[8] << 6)
                         + d[9];                                // ID interactif
    cell.LayerObject2Interactive = ((d[7] & 2) >> 1) != 0;     // est-elle interactive ?

    return cell;
}
```

### Valeurs `Movement` connues
- `0` : non marchable
- `1` : marchable normal
- `2` : **cellule de transition** (changement de map)
- `4` : autre (rare)

### Équations bordure cellule transition (m4x0ubot)
Pour identifier la DIRECTION de transition d'une cellule de type 2 :
```vb
If ((x - 1) = y) Then        changeurGauche = i
ElseIf ((x - 27) = y) Then   changeurDroite = i
ElseIf ((x + y) = 31) Then   changeurBas = i
ElseIf (y < 0) Then
    If (x - |y| = 1) Then    changeurHaut = i
End If
```

⚠ Constantes `27, 31` valides pour maps **14×17** standard Retro. Sur maps custom (Hystoria a parfois du 15×17), ces équations sont **fausses** — fallback sur déduction relative (min/max sur les transitions de la map).

---

## 🌾 2. États GDF des ressources

Source : thread `comprehension-disponibilite-des-ressources-au-chargement-dune-map.2705` (Wizen, 1.34.11 mais valable Retro)

### Au chargement de map
Si toutes les ressources sont récoltables → **PAS de GDF reçu**.
Si une/plusieurs ne le sont pas → un seul GDF groupé :
```
S→C : GDF|380;4;0
S→C : GDF|380;4;0|425;4;0|416;2;0|127;4;0|97;4;0
```

### Format GDF
```
GDF|<cell1>;<état1>;<qté1>|<cell2>;<état2>;<qté2>|...
```

### États (table complète Wizen + nos observations)
| État | Sens | Récoltable ? |
|------|------|---|
| 2 | Récolte EN COURS | non |
| 3 | Fin de récolte / depleted | non (jusqu'au respawn) |
| 4 | Indisponible (mid-respawn) | **non** (piège classique) |
| 5 | Disponible | **oui** |
| -1 | Aucun GDF reçu = défaut stable | oui (assumer 5) |
| 0,1 | Quasi jamais | non |

### Logique runtime
```
au chargement de map :
    pour chaque cellule où layerObject2Interactive == true :
        ressource.disponible = true   # par défaut

en cours de jeu, à chaque GDF reçu :
    pour chaque (cell, état, qté) dans GDF :
        ressource[cell].disponible = (état == 5)
        ressource[cell].quantite = qté  # rarement utilisé
```

---

## 💉 3. Hook DLL Dofus Retro

Source : thread `hook-dofus-retro.3129` (cremi532, code C++ MinHook complet)

### Principe
Injecter une DLL dans le process Dofus.exe, hooker `recv()` et `WSASend()` de Ws2_32.dll pour dumper tous les paquets sans MITM réseau.

### Code C++ minimal (MinHook)
```cpp
#include <winsock2.h>
#include <windows.h>
#include "MinHook.h"
#pragma comment(lib, "Ws2_32.lib")

typedef int (WINAPI *recv_t)(SOCKET, char*, int, int);
typedef int (WSAAPI *WSASend_t)(SOCKET, LPWSABUF, DWORD, LPDWORD, DWORD,
                                LPWSAOVERLAPPED, LPWSAOVERLAPPED_COMPLETION_ROUTINE);

static recv_t p_recv = nullptr;
static WSASend_t p_WSASend = nullptr;

void dump_ascii(const char* tag, const char* buf, int len) {
    if (!buf || len <= 0) return;
    std::cout << tag << " n=" << len << "\n";
    for (int i = 0; i < len; i++)
        if (buf[i]) std::cout << (std::isprint((unsigned char)buf[i]) ? buf[i] : '.');
    std::cout << "\n";
}

int WINAPI hook_recv(SOCKET s, char* buf, int len, int flags) {
    int r = p_recv(s, buf, len, flags);
    if (r > 0) dump_ascii("[recv]", buf, r);
    return r;
}

int WSAAPI hook_WSASend(SOCKET s, LPWSABUF bufs, DWORD cnt, LPDWORD bs, DWORD f,
                       LPWSAOVERLAPPED ov, LPWSAOVERLAPPED_COMPLETION_ROUTINE c) {
    for (DWORD i = 0; i < cnt; i++) dump_ascii("[send]", bufs[i].buf, bufs[i].len);
    return p_WSASend(s, bufs, cnt, bs, f, ov, c);
}

DWORD WINAPI HookThread(LPVOID) {
    AllocConsole();
    freopen("CONOUT$", "w", stdout);
    MH_Initialize();
    MH_CreateHookApi(L"Ws2_32", "recv",    (LPVOID)hook_recv,    (LPVOID*)&p_recv);
    MH_CreateHookApi(L"Ws2_32", "WSASend", (LPVOID)hook_WSASend, (LPVOID*)&p_WSASend);
    MH_EnableHook(MH_ALL_HOOKS);
    return 0;
}

BOOL APIENTRY DllMain(HMODULE h, DWORD reason, LPVOID) {
    if (reason == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(h);
        CreateThread(nullptr, 0, HookThread, nullptr, 0, nullptr);
    }
    return TRUE;
}
```

### Injection
- Outils : Process Hacker, script Python (`ctypes.windll.kernel32.CreateRemoteThread`), n'importe quel injector standard
- Pas de modif disque (rien n'est patché statiquement)
- Pas d'envoi réseau côté hook → presque indétectable

### Quand l'utiliser
- Alternative au MITM proxy quand on a besoin de **lire** les paquets en clair côté client (déjà déchiffrés par le client)
- Plus simple que MITM pour Hystoria (pas besoin de reverse cipher '-')
- Limite : pas d'injection facile, juste observation

---

## 🖥 4. Architecture serveur émulateur 1.29

Source : thread `emulateur-1-29.1268` (Fygorn, C#)

### Ports
| Port | Rôle |
|------|------|
| **444** | Realm server ↔ client (auth, liste serveurs) |
| **436** | Realm server ↔ game servers (communication interne) |
| **5555** | Game server ↔ client (gameplay) |

### Topologie
```
[ Client Dofus ]
       │
       ├─ port 444 ─→ [ Realm Server ]
       │                    │
       │              port 436 (interne)
       │                    │
       │                    ▼
       └─ port 5555 ─→ [ Game Server ]
```

### Format paquet émulateur (= ce que ton bot doit envoyer/recevoir)
```csharp
byte[] bytePacket = Encoding.UTF8.GetBytes(packet + "\x00");  // ⚠ null terminé
socket.Send(bytePacket);
```

Tous les paquets Retro sont :
- Encodés en **UTF-8**
- Terminés par **byte zéro `\x00`**
- Aucun header binaire ni longueur explicite

### Lecture côté serveur
```csharp
while (connected) {
    if (socket.Available > 0) {
        byte[] buffer = new byte[socket.Available];
        socket.Receive(buffer);
        // Split sur \x00 si plusieurs paquets reçus en un buffer
        var packets = Encoding.UTF8.GetString(buffer).Split('\x00');
        foreach (var p in packets) if (!string.IsNullOrEmpty(p)) ProcessPacket(p);
    }
}
```

### GameServerInformations (format paquet AH)
```
AH<id>;<status>;<completion>;<saison>|<id2>;...
```
- `status` : 3 = en ligne, 0 = offline, autre = maintenance
- `completion` : 0 = pas plein, 1 = peu rempli, etc.

---

## 🔀 5. Approches MITM comparées

Source : thread `mitm-sur-dofus-retro.2480` (Eldeiss, retours d'expérience)

### Approche A — Modif `config.xml`
**Le plus simple, c'est ce qu'on fait dans notre projet.**

Dans `Dofus 2/app/config.xml` (ou équivalent Retro) :
```xml
<connserver name="prox" ip="127.0.0.1" port="8080" />
```
Le client se connecte à `127.0.0.1:8080` au lieu du serveur officiel.

Avantages :
- Pas de hook, pas d'injection
- Fonctionne avec n'importe quel proxy TCP

Inconvénients :
- Modif fichier de config visible
- Le client check parfois le hash de config.xml

### Approche B — Frida hook `connect()`
Hook la function `connect()` du process pour rediriger toute connexion vers le proxy local. Voir [LaBot](https://github.com/Labo-de-Dofus/LaBot) (Python).

Avantages :
- Pas de modif fichier
- Plus discret

Inconvénients :
- Frida = injection process (peut être détecté)
- `connect()` appelé plusieurs fois (auth + game)

### Approche C — Hosts file + redirection DNS
Modifier `C:\Windows\System32\drivers\etc\hosts` :
```
127.0.0.1   dofus-co-retro-xxx.amazonaws.com
127.0.0.1   213.248.126.180
```
+ proxy local qui résout les vrais IPs et relaie.

Avantages :
- Transparent pour le client
- Pas de modif process

Inconvénients :
- Nécessite admin
- Visible globalement (autres apps affectées)

### Notre approche (Luffy-bot)
On utilise une variante : redirection DNS au niveau réseau (Tailscale ou hosts) + proxy MITM C# qui :
1. Écoute sur 1303 et 1304 (auth + game)
2. Résout DNS vers vrai serveur Hystoria
3. Relaye en clair + déchiffre canal '-' pour logger
4. Re-chiffre injections

---

## 📦 6. Splitting des paquets ≤1024 bytes

Source : thread `d1proxy-sniffer-et-samuser.2208` (Aquazus, Java)

### Constat
Le serveur officiel Dofus **split toujours** ses gros paquets en blocs de **≤1024 bytes**. Si ton émulateur/proxy envoie un paquet plus gros d'un coup, l'anti-bot peut le détecter (signature taille).

### Code splitting (Java, adapté en C#)
```csharp
public static void EnvoyerSplit(NetworkStream destination, string paquet)
{
    const int TAILLE_BLOC = 1024;
    var data = Encoding.UTF8.GetBytes(paquet);
    int offset = 0;
    while (offset < data.Length) {
        int taille = Math.Min(TAILLE_BLOC, data.Length - offset);
        destination.Write(data, offset, taille);
        offset += taille;
    }
    // null terminator à la fin
    destination.WriteByte(0);
}
```

### Quand c'est critique
- Liste perso (ALK) sur compte avec 50+ persos → ~5 KB
- Liste banque sur compte rempli → 20+ KB
- Liste sorts/métiers complète → ~3 KB

### Notre code
On utilise probablement déjà le buffer TCP natif qui split automatiquement, mais à vérifier sur les très gros paquets.

---

## 🤖 7. Bots open-source de référence

Récapitulatif des bots Retro/Hystoria avec code consultable :

### Code C# (NOTRE BASE)
- **`dyshay/Bot-Dofus-Retro`** ⭐⭐⭐ — code propre, modèle Spell par niveau, IA combat (SpellsManager)
  - https://github.com/dyshay/Bot-Dofus-Retro
- **`salesprendes/Bot_Dofus_Retro`** — souche dyshay (Alvaro Prendes, salesprendes.com)
- **`Azzary/NebulaR-Bot`** — Nebular Bot, hook user32.dll (clics + clavier), pas d'injection paquets, Lua scripting
  - https://github.com/Azzary/NebulaR-Bot
  - Approche fenêtre Dofus visible, scripts Lua avec `WindowManager:Click()`, `WindowManager:Press()`

### Code Python
- **`Labo-de-Dofus/LaBot`** — bot Python avec Frida pour hook connect()
- Multi-projets de bot récolte / multi-account (cf threads `Ankam4te.2186`, `bot-recolte-retro-retour-dxp.3263`)

### Code Java
- **D1Proxy / `Aquazus/D1Proxy`** — proxy MITM Java + outils (sniff, .all command auto pass turn)

### Code VB.Net
- m4x0ubot (legacy) — base de nombreux forks, dont les modifs de phylonia (multi-sort, anti-allié pathfinder)

### Émulateurs serveur (utile pour comprendre)
- **KryoneV2** — émulateur 1.29
- **Ancestra** — référence souvent citée
- **StarLoco** — supporte clients 1.29-1.32
- **`Aquazus/?`** — émulateur Java

### Décompilateurs clients
- **`dofera/dofedex`** ⭐ — sources Retro DÉSOBFUSQUÉES (versions 1.29 à 1.32)
  - https://github.com/dofera/dofedex
  - **Le must-have pour comprendre comment le vrai client traite les paquets**

---

## 🌾 8. Bot récolte moderne 2026

Source : thread `bot-recolte-retro-retour-dxp.3263` (MoundirEsport, Python + Claude)

### Architecture
1. **Phase 1** : 100% pixel (capture d'écran, OpenCV template matching, OCR coords, pyautogui)
   - Limites : OCR fragile, combat impossible, pods via 1 pixel
2. **Phase 2** : Proxy MITM (DNS redirect + proxy relay)
   - Temps réel : coords map (`Gc`), pods (`Ow`), combat complet (`GJK`, `GTS`, `GTM`, `GA*`)
   - Récolte gardée en pixel (template matching + clic) car ça marche bien

### Navigation : graphe auto-apprenant
```
when bot change_map detected (via GDM/Gc) :
    edge = (map_avant, direction, map_apres)
    graph.add_edge(edge)
    save_graph_to_disk()

when bot needs_to_go(target_map) :
    path = A_star(current_map, target_map, graph)
    for each (map, direction) in path :
        walk_to_exit(direction)
        wait_for_map_change()
```
Difficulté : chaque map a ses propres positions de clic de sortie → besoin d'overrides manuels pour cas spécifiques (portes, escaliers).

### Combat
- Grille combat 560 cells reconstruite depuis le réseau
- Évaluation de **TOUTES les combinaisons** (position × sort × cible) chaque tour
- Problème : protecteurs bougent + walkability SWF ne matche pas toujours le serveur

### Leçons pour notre bot
- ✅ MITM pour combat = approche validée
- ✅ Graphe navigation auto-apprenant > chemins hardcodés
- ⚠ Walkability SWF → cas limites, fallback raw GA001 enregistré (= ce qu'on fait)
- ⚠ Combat = évaluation exhaustive coûteuse mais nécessaire pour les cas où le bot doit choisir position+sort+cible

---

## 🔢 9. Anatomie paquet D2.0

> *(Pour culture — notre projet est Retro 1.29 string-based, donc PAS ce format. À garder en tête au cas où on aurait à interagir avec D2.0)*

Source : threads `de-lanalyse-des-paquets.1056` (Labo) + `initiation-a-la-communication-avec-d.436`

### Structure binaire
```
[ HEADER : 2 bytes ]
   ├─ id : 14 bits   (id du paquet)
   └─ lenType : 2 bits  (taille de la longueur : 0/1/2/3)
[ LENGTH : 0, 1, 2 ou 3 bytes selon lenType ]
[ DATA : <length> bytes ]
```

### Lecture
```cpp
short header = (recv_buf[0] << 8) | recv_buf[1];
int id      = header >> 2;     // 14 bits hauts
int lenType = header & 3;       // 2 bits bas
int length;
switch (lenType) {
    case 0: length = 0; break;
    case 1: length = recv_buf[2] & 0xff; break;
    case 2: length = ((recv_buf[2] << 8) | recv_buf[3]) & 0xff; break;
    case 3: length = ((recv_buf[2] << 16) | (recv_buf[3] << 8) | recv_buf[4]) & 0xff; break;
}
// puis data = recv_buf[2 + lenType .. 2 + lenType + length]
```

### Types primitifs
- `readShort` : 2 bytes big-endian
- `readUnsignedShort` : same, cast unsigned
- `readInt` : 4 bytes
- `readUTF` : `unsigned short length` + `length bytes UTF-8`
- `readBoolean` : 1 byte (0 ou 1)
- `readDouble` : 8 bytes IEEE 754

### Sérialisation/désérialisation
- Décompiler `DofusInvoker.swf` (JPEXS / Sothink) → fichiers `.as` (ActionScript)
- Chercher `protocolId:uint = <id>` pour trouver le fichier du paquet
- Méthodes `serializeAs_*` (envoi) et `deserializeAs_*` (réception) montrent l'ordre des champs

---

## 🔓 10. Sources désobfusquées Dofedex

Source : thread `dofedex-sources-retro-dsobfusqus-1-29.2482` (ERA + Fred6729)

### Repo
https://github.com/dofera/dofedex

### Contenu
- **Versions Retro recensées** (1.29 à 1.32) avec liens de téléchargement
- **Sources désobfusquées** des dernières versions
- Mis à jour avec les nouvelles releases Retro / Hystoria-compatibles

### Comment l'utiliser
1. Cloner le repo (ou télécharger version voulue)
2. Ouvrir `DofusInvoker.swf` (le SWF principal du client) avec JPEXS Free Flash Decompiler
3. Naviguer les classes ActionScript (`com.ankamagames.dofus.*`)
4. Lire les méthodes `serialize` / `deserialize` des paquets pour comprendre exactement leur format
5. Comparer avec ce que ton sniff/MITM observe

### Cas d'usage typique
Tu vois un paquet `Hp` chez ton sniff sans savoir ce que c'est ?
→ Dans le SWF désobfusqué, chercher la classe handling `Hp` → tu trouves la struct exacte (champs, types).

---

## 🌐 11. Récupération IP serveur

Source : thread `extraction-adresse-ip-bot-dofus-retro.3084` (Kurosaki, Python)

### Méthode 1 — Sniff Wireshark
```python
from scapy.all import sniff, IP, TCP

def packet_callback(packet):
    if packet.haslayer(TCP) and packet[TCP].dport == 5555:
        print(f"Serveur Dofus : {packet[IP].dst}")
        return True  # stop after first

sniff(filter="tcp port 5555", prn=packet_callback, count=1)
```

### Méthode 2 — Parse les paquets `AXK`
Le paquet `AXK<ip:port chiffrés>` reçu après `AX<serverId>` contient l'adresse du serveur de jeu en chiffré simple :
```
AXK<8 chars ID compte><ip>:<port>;...
```
L'IP est chiffrée mais le format reste lisible (chars hexadécimaux + séparateurs).

### Méthode 3 — DNS lookup direct
Pour Hystoria : `nslookup` sur le hostname AYK reçu → IP serveur.

---

## 📚 12. Index complet des 50+ threads lus

### Format : ID — titre — résumé info clé

#### Protocole 1.29 général
- `protocol-dofus-1-29.1822` — Diagramme paquets (image)
- `protocole-dofus-1-29.1552` — Liens vers tutos Labo
- `d1-29-questions-protocole.2099` ⭐ — Flux complet auth→jeu
- `comprendre-le-protocole-de-dofus.115` — Bouh2 D2.0 (générique)
- `de-lanalyse-des-paquets.1056` — Labo tuto WireShark D2.0
- `tuto-identifier-les-packets.143` — WPE Pro + SWF decompiler
- `analyse-des-paquets-tcp-recus-dofus-retro.2476` — Retro spécifique
- `journal-de-bord-analyser-ses-premiers-paquets.2424` — Débutant

#### MITM / Sniffer / Hook
- `tutoriel-faire-un-bot-mitm-a-partir-de-zero.773` — C# D2.0
- `tuto-bot-socket-les-fondamentaux.491` — C# socket basics
- `mitm-sur-dofus-retro.2480` ⭐ — Eldeiss, comparatif approches
- `sockets-1-29-probleme-de-deplacement-apres-un-combat.1585` ⭐ — wukzu VB.Net complet
- `creation-de-mon-bot-mitm-dofus-retro.3039` — Akihiko Python
- `creation-bot-mitm-dofus-retro.3300` — projet récent
- `mitm-1-29-déplacements.1650` — BlueDream MITM
- `sniffer-grace-au-raw-sockets-sur-windows.1870` — sniffer raw
- `d1proxy-sniffer-et-samuser.2208` ⭐ — Java + splitting paquets
- `hook-dofus-retro.3129` ⭐⭐ — DLL C++ MinHook complet

#### Cartes / MapData
- `systeme-de-decryptage-mapdata-dofus-1-29x.1790` — algo déchiffrement
- `interpretation-mapdata-dechiffree-1-29.2034` ⭐⭐ — Aichan, code C# bit-à-bit
- `decryptage-des-maps-1-29.2225` — variante
- `gestion-des-paquets-...-ressources-recoltable.3268` ⭐ — états GDF
- `comprehension-disponibilite-des-ressources-au-chargement-dune-map.2705` ⭐ — Wizen
- `analyse-de-map-avec-limage.2931` — overlay image
- `ou-trouve-t-on-les-mapdata.2908` — où sont stockées

#### Crypto / Auth
- `dofus1-29-systeme-de-cryptage-decryptage-mdp-c.2282` — cryptPassword
- `authentification-1-35-5-retro-monocompte.2744` — variante 1.35.5

#### Combat / IA
- `bonne-ia-en-combat.1771` ⭐ — phylonia, algo pseudo-code
- `ma-petite-modification-du-m4x0ubot-d-1-29-1.1766` ⭐ — m4x0ubot mods (PA/PM live)
- `dofus-bot-1-29-2-sorts.1774` — multi-cast
- `probleme-paquet-ga-envoie-de-sort.2392` ⭐ — confirme `GA300<id>;<cell>`
- `up-lancer-un-sort.411` — D2.0 cast
- `autoit-v3-deplacement-en-combat-pour-d-v2.146` — AutoIt déplacement combat
- `cree-un-bot-d-1-29-avec-actionaz-3.1015` — protecteurs ressources
- `debutant-creer-son-propre-bot-combat.3001` — anti-bot tips
- `dofus-recuperer-la-logique-dun-combat.3137` — IA monstres (Turquoise/Furye)
- `ia-datas-de-combat.2024` — format JSON logs combat
- `environnement-rl-retro-ia-des-mobs.3304` — IA monstres reinforcement learning

#### Récolte / Métiers
- `bot-recolte-retro-retour-dxp.3263` ⭐ — MoundirEsport approche moderne
- `gestion-des-paquets-...-ressources-recoltable.3268` ⭐ — états GDF

#### Bots open-source
- `bon-voici-le-code-source-entier-de-snowbot.3223` — Snowbot full source download
- `tous-les-scripts-snowbot-sans-chiffrement.3222` — scripts seuls
- `nebular-bot-dofus-retro.2990` ⭐ — Nebular C# GitHub
- `azur_tools-1-29.2186` — outils BDD émulateur
- `emulateur-1-29.1268` — émulateur 1.29 architecture
- `dofedex-sources-retro-dsobfusqus-1-29.2482` ⭐⭐ — sources Retro désob
- `parseur-de-protocole.2465` — parseur SWF D2.0
- `extraction-adresse-ip-bot-dofus-retro.3084` — récup IP serveur
- `ankam4te.2186` — multi-compte Python

#### Anti-bot / Bans
- `autoban-changement-de-map.3285` ⭐ — Touch, GKK0 timing
- `ban-detection-dun-client-retro-sur-lequel-on-lit-des-clefs.3297` — Dofus 3.0 primitive swap
- `simulation-clics-injection-dll-detectable-retro.3136` — détection injection
- `la-pave-de-ma-vision-sur-lanti-bot-et-son-fonctionnement.2419` — vision anti-bot Ankama

#### Dofus Touch / Unity 3.0 (hors scope mais info parfois utile)
- `comprenre-dofus-et-le-botting.3128` — Unity protobuf
- `interpretation-mapdata-dechiffree-1-29.2034` — partiellement applicable

---

## 🎯 RÈGLES PRATIQUES SUPPLÉMENTAIRES (V2)

### Pour bot MITM Hystoria
1. **Décodage MapData** : utiliser le code C# d'Aichan (#2034) — bit-à-bit pour Movement et LayerObject2
2. **États GDF** : ne pas faire confiance à `RessourceDisponible=true` par défaut ; **toujours** lire les états reçus
3. **Splitting paquets** : si tu envoies des paquets >1KB, split en blocs de 1024 bytes (comme le serveur officiel)
4. **Auto-learn graph** : enregistrer toutes les transitions map (départ, direction, arrivée) → A* pour navigation
5. **Évaluation combat exhaustive** : pour chaque tour, scorer toutes les combinaisons (position × sort × cible) — plus précis qu'un cascade if/else

### Pour comprendre le client
- **Dofedex désobfusqué** est ton meilleur ami pour lire le code .as du client
- **JPEXS Free Flash Decompiler** pour ouvrir le SWF
- **Wireshark** + filtre `tcp port 5555` pour sniffer brut

### Pour les tests
- **Hook DLL recv/WSASend** = alternative au MITM, plus simple pour Hystoria
- Process Hacker pour injecter
- AllocConsole + freopen pour avoir une fenêtre de log

---

## 🔗 LIENS GITHUB CLÉS (TOUT EN UN)

| Repo | Description | Langue |
|------|-------------|--------|
| https://github.com/dyshay/Bot-Dofus-Retro | Notre base | C# |
| https://github.com/Azzary/NebulaR-Bot | Bot hook+Lua | C# |
| https://github.com/dofera/dofedex | Sources Retro désob | (clients) |
| https://github.com/Manghao/DofusMapDataDecypher | Déchiffrement mapData | Java |
| https://github.com/hussein-aitlahcen/dofus-map-key | Key cracker | (varie) |
| https://github.com/Labo-de-Dofus/LaBot | Bot Python Frida | Python |
| https://github.com/Aquazus/D1Proxy | Proxy MITM | Java |

---

🌿 **Bon développement !** Avec V1 + V2 tu as ~60 threads couverts en profondeur.
