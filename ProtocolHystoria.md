# Protocole Hystoria — Mapping packets C→S

Extrait par grep dans `swf-decompiled/scripts/__Packages/dofus/aks/`.
Chaque code = packet envoyé par le **client** au serveur.

Format général d'un packet :
- **Préfixe** : 1 à 4 chars ASCII (le code identifiant)
- **Body** : optionnel, paramètres séparés par `|` ou `;`
- **Terminator** : `\0` (null byte) à la fin (couche TCP)
- Si chiffré : encapsulé dans `CRYPTS<seq><b64>` (cf. [HystoriaCipher.md](HystoriaCipher.md))

⚠️ Les codes sont **case-sensitive** — `AT` ≠ `At` ≠ `aT` ≠ `at`.

---

## Phase 1 — Authentification ([Account.as](swf-decompiled/scripts/__Packages/dofus/aks/Account.as))

Codes envoyés sur le serveur d'auth (`162.19.127.156:450` chez Hystoria).

| Code | Nom probable | Description |
|---|---|---|
| `Ap<v>` | AnnouncePort | Version protocole (ex: `Ap443`) |
| `Ai<id>` | AccountID | Identifiant client (ex: `Ai3247v6`) |
| `Ax` | AccountListServers | Demande liste serveurs |
| `AX<srvId>` | AccountSelectServer | Choix d'un serveur (ex: `AX601`) |
| `AT<ticket>` | AccountTicket | Présentation du ticket reçu via AYK (ex: `AT7504`) |
| `AA<login><pass>` | AccountAuth | Login + mot de passe hashé (cf. ChiffrementDofus#1) |
| `Af` | AccountQueueRequest | Position dans la file d'attente |
| `AV` | AccountVersion | Demande version |
| `AL` | AccountList (chars) | Liste des personnages |
| `ALf` | AccountListForce | Force refresh liste |
| `AS<charId>` | AccountSelectChar | Sélection personnage |
| `AD` | AccountDelete | Demande suppression compte/perso |
| `AB` | AccountBan ? | À vérifier |
| `AG<charName>` | AccountGenerate ? | Génération nouveau perso |
| `AM<...>` | AccountMod | Modification compte |
| `AP<...>` | AccountParameters | Paramètres |
| `AR<...>` | AccountReroll ? | À vérifier |
| `AF<...>` | AccountFriend ? | À vérifier |
| `Ag<lang>` | AccountLanguage | Langue (ex: `Agfr`) |
| `Ak<n>` | AccountKey ? | À vérifier |
| `Ar<...>` | AccountRandom ? | À vérifier |
| `AEc/AEn/AEi0/AEi1` | AccountExchange | Échange auth (sous-actions) |

### Réponses serveur typiques (S→C)
| Code | Description |
|---|---|
| `HC<key>` | HelloConnexion : clé publique pour hash mot de passe |
| `HG` | HelloGame : confirmation connexion serveur game |
| `AlEr<n>` | AccountLoginError : erreur d'auth (n = code) |
| `AxK<list>` | Liste serveurs (ex: `AxK0\|601,1` = serveur 601 état 1) |
| `AYK<ip>:<port>;<ticket>` | Redirect game server (ex: `AYK162.19.127.155:5555;7504`) |
| `ATK<n>` | Ticket OK / KO |
| `ALK<charlist>` | Liste personnages (format complexe `ALK<time>\|<n>\|<id>;<name>;<lvl>;<class>;<color1>;<color2>;<sex>;<looks>;<...>`) |
| `AV<n>` | Version réponse |

---

## Phase 2 — Crypto handshake ([Aks.as](swf-decompiled/scripts/__Packages/dofus/aks/Aks.as))

| Code | Description |
|---|---|
| `CRYPTS\n` | Demande activation chiffrement (envoyé après reconnexion sur game server) |
| `HS` | Heartbeat / KeepAlive |
| `ping` | Ping client → serveur |
| `qping` | ? Variant ping |

### Réponses serveur
| Code | Description |
|---|---|
| `HG...` | Confirmation handshake (active le cipher si `_cryptoState == 1`) |
| `CRYPTS...` (8+ chars) | Packet game chiffré (cf. HystoriaCipher.md) |
| `CRYPTOK` | Ack alternative |
| `CRYPTFAIL` | Refus, retour mode clair |
| `rpong` | Réponse ping |

---

## Phase 3 — Basics in-game ([Basics.as](swf-decompiled/scripts/__Packages/dofus/aks/Basics.as))

| Code | Description |
|---|---|
| `BA<...>` | Basics Action (générique) |
| `BC<emote>` | Basics ChangeEmote / Color |
| `BD<...>` | Basics Direction (orientation perso) |
| `BK<...>` | Basics Key (touche pressée) |
| `BQ<...>` | Basics Quit |
| `BW<...>` | Basics Wait |
| `BYA<...>` | Basics YA (à identifier) |
| `BYI<...>` | Basics YI (à identifier) |
| `BaK<...>` | Basics aK |
| `BaM<...>` | Basics aM |
| `Bp<...>` | Basics ping |
| `Br<...>` | Basics restart |
| `Bt<...>` | Basics talk/timer |

---

## Phase 4 — Chat ([Chat.as](swf-decompiled/scripts/__Packages/dofus/aks/Chat.as))

| Code | Description |
|---|---|
| `BM<chan>\|<msg>` | Message canal général/guilde/etc. (chan = `*`/`%`/`$`/etc.) |
| `BR<...>` | Réponse / Reply |
| `BS<...>` | Send privé |
| `cC<...>` | Chat configuration |

---

## Phase 5 — Game (déplacement, map) ([Game.as](swf-decompiled/scripts/__Packages/dofus/aks/Game.as))

| Code | Description |
|---|---|
| `GC<cellId>` | GameClick (clic sur cellule pour déplacement) |
| `GD<...>` | GameData (demande données map) |
| `GF<...>` | GameFight (entrer en combat) |
| `GP<...>` | GameParty |
| `GQ<...>` | GameQuit (quitter map) |
| `GR<...>` | GameRequest |
| `GT<...>` | GameTeleport |
| `Gdi<...>` | GameDirection ? |
| `Gf<...>` | GameFollow |
| `GhA/Ghr/Ghu` | Game house (achat/repair/use) |
| `Gp<...>` | Game prismes (?) |
| `Gt<...>` | Game time |
| `Gw<...>` | Game waypoint |

---

## Phase 6 — GameActions (le BIG one) ([GameActions.as](swf-decompiled/scripts/__Packages/dofus/aks/GameActions.as))

```
GA<actionType>(3 chars padded with 0)<param1>;<param2>;...
```

Format documenté ligne 14 :
```actionscript
this.aks.send("GA" + nActionType.padLeft("0",3) + aParams.join(";"));
```

| Sous-code (ActionType) | Description |
|---|---|
| `GA001` | (déplacement / movement path) |
| `GA300` | (cast de sort) |
| `GA500` | (interaction PNJ) |
| `GA900` | (challenge / défi) |
| `GKE<id>\|<params>` | GameKeyExchange (action paramétrée) |
| `GKK<id>` | GameKeyKill (annuler action) |

⚠️ Les valeurs exactes dépendent du serveur. Le user devra mapper chaque ActionType à un nom (movement, fight join, NPC interact, mount, etc.) en observant le trafic.

---

## Phase 7 — Combat ([Fights.as](swf-decompiled/scripts/__Packages/dofus/aks/Fights.as))

| Code | Description |
|---|---|
| `fL` | Fight List (lister combats) |
| `fD<id>` | Fight Done (terminer) |
| `fS` | Fight Start |
| `fP` | Fight Pause / Pass turn |
| `fN` | Fight Next |
| `fH` | Fight Help |

---

## Phase 8 — Sorts ([Spells.as](swf-decompiled/scripts/__Packages/dofus/aks/Spells.as))

| Code | Description |
|---|---|
| `SB<spellId>;<lvl>` | SpellBoost (monter sort de niveau) |
| `SF<spellId>` | SpellForget (oublier sort) |
| `SM<...>` | Spell Move (réorganiser barre) |
| `SR<...>` | Spell Rebuild |

---

## Phase 9 — Items / Inventaire ([Items.as](swf-decompiled/scripts/__Packages/dofus/aks/Items.as))

| Code | Description |
|---|---|
| `OD<itemId>` | Object Drop |
| `OM<itemId>;<dest>` | Object Move (équiper/déséquiper) |
| `OL<...>` | Object List |
| `OR<...>` | Object Remove |
| `OZ<...>` | Object Z (?) |
| `OF<...>` | Object Filter |
| `Od<...>` | Object delete (lowercase) |
| `Of<...>` | Object filter (lowercase) |
| `Ol<...>` | Object list (lowercase) |
| `Os<...>` | Object split |
| `O` | Object generic ? |
| `AEi0/AEi1` | Auction House item list/info |
| `OrA/OrM/OrR` | Object raccourci Add/Move/Remove (InventoryShortcuts) |

---

## Phase 10 — Échange ([Exchange.as](swf-decompiled/scripts/__Packages/dofus/aks/Exchange.as))

| Code | Description |
|---|---|
| `EA<targetId>` | Exchange Ask (proposer échange) |
| `EB` | Exchange Begin / Buy |
| `EK` | Exchange Kancel (annuler) |
| `EL` | Exchange List |
| `EP` | Exchange Put (mettre objet) |
| `EQ` | Exchange Quit |
| `ER` | Exchange Ready / Refuse |
| `ES` | Exchange Set quantité |
| `EV` | Exchange Validate |
| `EW` | Exchange Wait |
| `EHB/EHM/EHP/EHS/EHT/EHl` | Exchange Hôtel des Ventes (bid, modify, post, search, take, list) |
| `EJF<...>` | Exchange Join Fight ? |
| `EMG/EMO/EMR/EMS/EMr` | Exchange Master / Mount |
| `Eff/Efg/Efp` | Exchange flagment (?) |
| `Eq` | Exchange quit |
| `ErC/Erc/Erf/Erg/Erp` | Exchange recipe / craft |
| `Es` | Exchange split |

---

## Phase 11 — Dialogue PNJ ([Dialog.as](swf-decompiled/scripts/__Packages/dofus/aks/Dialog.as))

| Code | Description |
|---|---|
| `DR<replyId>` | Dialog Reply (choisir réponse PNJ) |
| `DV` | Dialog ValidateOk |
| `DC` | Dialog Cancel / Close |
| `DB<...>` | Dialog Begin |

---

## Phase 12 — Amis / Groupe / Guilde

### Friends ([Friends.as](swf-decompiled/scripts/__Packages/dofus/aks/Friends.as))
`FA` ajout, `FD` delete, `FL` list, `FO` ?, `FJ`/`FJC`/`FJF` Join

### Party ([Party.as](swf-decompiled/scripts/__Packages/dofus/aks/Party.as))
`PA` ask, `PF` follow, `PG` group, `PI` invite, `PP` ping/position, `PR` refuse, `PV` validate, `PW` wait

### Guild ([Guild.as](swf-decompiled/scripts/__Packages/dofus/aks/Guild.as))
~26 codes commençant par `g` : `gB/gC/gF/gH/gK/gP/gV/gW/gb/gf/gh` + sous-codes `gE*` (events), `gI*` (info/invite), `gJ*` (join), `gT*` (transfert)

---

## Phase 13 — Maisons, Métiers, Quêtes, Zaap

### Houses ([Houses.as](swf-decompiled/scripts/__Packages/dofus/aks/Houses.as))
`hB` buy, `hG` guild, `hQ` quit, `hS` sell, `hV` view

### Job ([Job.as](swf-decompiled/scripts/__Packages/dofus/aks/Job.as))
`JO` Job Order/Open

### Quests ([Quests.as](swf-decompiled/scripts/__Packages/dofus/aks/Quests.as))
`QL` Quest List, `QS` Quest Start/State

### Waypoints / Subway ([Waypoints.as](swf-decompiled/scripts/__Packages/dofus/aks/Waypoints.as), [Subway.as](swf-decompiled/scripts/__Packages/dofus/aks/Subway.as))
`WU` Waypoint Use, `WV` Waypoint View, `Wp/Wu/Wv/Ww` Subway

---

## Phase 14 — Montures ([Mount.as](swf-decompiled/scripts/__Packages/dofus/aks/Mount.as))

11 codes commençant par `R` : `Rb/Rc/Rd/Rf/Rn/Ro/Rp/Rr/Rs/Rv/Rx` (R = Riding ?)

---

## Phase 15 — Conquête ([Conquest.as](swf-decompiled/scripts/__Packages/dofus/aks/Conquest.as))

`CB` conquest begin, `CFJ/CFL/CFS` conquest fight join/list/start, `CIJ/CIV` info join/view, `CWJ/CWV` war join/view, `Cb`

---

## Phase 16 — Misc

| Code | Source | Description |
|---|---|---|
| `IM` | Infos.as | Info Message |
| `Ir` | Infos.as | Info Request |
| `eD/eU` | Emotes.as | Emote Down/Up |
| `dV` | Documents.as | Document View |
| `iA/iD/iL` | Enemies.as | Enemy Add/Delete/List |
| `cC` | Chat.as | Chat Configuration |
| `mC/mD/mS/ms` | ModReport.as | Modération |
| `tc/td/tr/ts/tu` | Ttg.as | Trésor du Temps Goultarminator |
| `xD/xE/xL/xS` | RapidStuff.as | Rapid Stuff Down/Equip/Load/Save |
| `KK<...>` | Key.as | Key Kill (?) |
| `KV` | Key.as | Key Validate |
| `TV` | Tutorial.as | Tutorial Validate |
| `YD/Yd` | Evenemential.as | Event Done |
| `YT` | Temporis.as | Temporis |
| `zi/zs` | Survey.as | Sondage in/start |

---

## Comment continuer le reverse

Si tu veux le mapping S→C complet (réponses serveur), il faut lire [DataProcessor.as](swf-decompiled/scripts/__Packages/dofus/aks/DataProcessor.as) (1346 lignes) qui a une grosse `switch(sType)` à 2 niveaux. Pour chaque `case "X"` du premier switch, lire le sous-switch.

Pour les détails du payload de chaque packet, lire les fichiers `aks/*.as` correspondants — chaque méthode `processXX` y détaille comment parser les params.

---

## Stats

- **190 codes C→S identifiés** dans 30+ fichiers
- **~50 catégories fonctionnelles**
- **Tous les packets game (post-`HG`) sont chiffrés** via HystoriaCipher

→ Quand tu loggeras des packets via ton proxy, le préfixe te dira **immédiatement** dans quel fichier `aks/*.as` chercher la sémantique des paramètres.
