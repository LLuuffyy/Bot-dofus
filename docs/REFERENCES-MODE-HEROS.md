# Références — Mode Héros (Dofus Retro 1.29)

> **Objet :** recensement des sources connues sur le « mode héros » côté serveur
> (mécanisme intégré qui lie plusieurs personnages d'un même compte web, fait
> jouer toute l'équipe au même combat, et expose à un client unique
> les inputs/outputs réseau de chacun). Le mode héros est différent du
> « multi-client » (plusieurs Dofus.exe + un overlay tipo AHK).
>
> **Date du rapport :** 2026-05-22
> **Cible serveur de notre bot :** Hystoria (1.29.x semi-like, **monocompte**, **PAS de mode héros**)
> **Cibles potentielles futures :** Abrak, Aqua (les 2 serveurs FR qui exposent un Mode Héros « 8 persos / 1 fenêtre »)

---

## TL;DR

- Le **protocole Dofus 1.29 stock** n'a **AUCUN** support natif du mode héros (cf. `kralamoure/retroproto`, `Araknemu`).
- Le mode héros est une **extension propriétaire** des serveurs privés FR modernes : **Abrak**, **Aqua**, **Atoria** ; commercialement vendu comme « 8 persos en 1 seule fenêtre ».
- **Une seule implémentation client publique** trouvée — pas open-source mais analysable : **SynFus Aqua v1.1.0** (DLL .NET obfusquée, présente dans `C:\Users\touki\Desktop\SynFus_Aqua_v1.1.0\`). Elle expose entièrement la **structure C#** d'un client mode-héros (classes, méthodes, propriétés, events) bien que les bodies de méthodes soient ofusqués.
- **Aucune doc publique du protocole sur le wire.** Pas de capture, pas de tableau de paquets. Il faudra **capturer nous-mêmes en mode passif** si on cible Abrak/Aqua un jour.
- **Notre serveur (Hystoria) n'a pas de mode héros** → sujet hors-roadmap actuel ; rapport conservé comme référence si on porte le bot sur Abrak/Aqua.

---

## A. Paquets identifiés

### A.1 Côté SynFus (client Aqua) — analyse statique du DLL obfusqué

L'analyse par lecture des metadata .NET (PEReader → MetadataReader) du fichier
`C:\Users\touki\Desktop\SynFus_Aqua_v1.1.0\Synfus.dll` (2.79 Mo, .NET 8 WPF)
révèle les **handlers** suivants. Les noms de propriétés/méthodes ne sont **pas**
obfusqués (seuls les corps de méthode et les string literals le sont), donc on
peut reconstruire la matrice paquet → handler.

**Source brute :** types via `System.Reflection.Metadata.MetadataReader` sur
`Bot_Dofus_1._29._1.Comun.Frames.Juego.CharacterFrame` et `FightFrame`.

#### `Bot_Dofus_1._29._1.Comun.Frames.Juego.CharacterFrame` (out-of-combat)

| Property (= handler paquet serveur) | Interprétation |
|---|---|
| `get_Ping_Or_HeroList` | Le serveur Aqua **détourne le paquet ping** (`Bp`/`Bn`) pour, en mode héros, encoder la **liste des héros liés** au lieu d'un simple pong. Le client teste si la trame est un pong stock ou une hero-list. |
| `ParseHeroList` | Parse la trame hero-list (probablement format `Bp<hero1>|<hero2>|...` ou similaire). Préfixe du paquet inconnu sans capture wire. |
| `get_HeroEntry_Standalone` | Décrit une **entrée d'héros standalone** (1 héros isolé, hors-équipe). |
| `get_MainHeroEntry_Standalone` | Le « main hero » = le perso actuellement piloté (= ce que le serveur considère comme « actif »). |
| `get_HeroMode_Active` | Bool : le serveur a-t-il activé le hero mode pour cette session ? |
| `get_HeroMode_Init` | Paquet d'**init** du hero mode (probablement émis après l'auth et la sélection compte, avant le `GC` map). |
| `get_HeroMode_AccountBasic` | Infos **compte web** (compte parent qui possède les N héros). |
| `get_HeroMode_AccountEntityHero` | Infos détaillées d'**une entité héros sur un compte** (id, nom, classe, niv, gfx, sex, colors, stuff, PV, énergie). |
| `get_HeroMode_Stats` | Snapshot des stats du héros actif (PA/PM/PV/etc). |
| `get_PartyMember_Add` / `_Remove` / `_Update` | Gestion **du groupe (party) `Rp...`** classique, mais probablement étendu pour les héros liés. |

#### `Bot_Dofus_1._29._1.Comun.Frames.Juego.FightFrame` (combat)

| Property | Interprétation |
|---|---|
| `get_Hero_Turn_Start` | Le serveur signale **« début de tour d'un héros lié »** — variante du `GTS` ou `GTM` standard. |
| `get_Hero_Turn_Update` | Mise à jour mi-tour (PV/PA/PM partagés à l'UI multi-portrait). |
| `get_Hero_Switch_Message` | **Le serveur annonce un switch d'héros** (= « ton hero #2 a maintenant la main pour cliquer »). |
| `get_Hero_Handoff` | Probablement le **handover de contrôle réseau** (le client doit envoyer les paquets « Gp/GA/Gt » comme s'il était l'autre hero). |
| `get_Hero_Character_Data` | Snapshot des données perso d'un hero qui prend la main. |
| `get_Hero_Map_Info` | Infos map (cellules, entités) re-broadcastée au switch (chaque hero voit la map depuis sa cell). |

#### `Bot_Dofus_1._29._1.Otros.Game.GameClass` (state machine côté client)

Champs/props centraux :

- `IsHeroMode` (bool) — flag global activé par `HeroMode_Init`.
- `Heroes` (collection de `HeroCharacter`) — les héros liés au compte web.
- `MainHero`, `SelectedHeroId`, `ActiveMapCharacterId`, `ActiveMapCell` — état actif.
- `BeginHeroSwitch` / `EndHeroSwitch`, `PushHeroContext` / `PopHeroContext` — workflow de switch côté client (sauvegarder le contexte du hero A, restaurer celui du hero B).
- `RefreshAllHeroesSpellsAsync` — re-fetch les sorts de chaque hero (probablement en faisant `WC<characterId>` ou équivalent paquet d'init perso, plusieurs fois).
- `HeroRefreshMinDelayMs` / `HeroRefreshMaxDelayMs` — **humanise** le re-fetch (anti-ban) → suggère que le serveur **n'envoie pas** automatiquement tous les sorts ; il faut faire un round-trip par hero.

#### `Bot_Dofus_1._29._1.Otros.Game.Character.HeroCharacter` (DTO d'un hero)

Champs : `Id`, `Name`, `Level`, `GfxId`, `Sex`, `Color1/2/3`, `Stuff` (string),
`IsActive`, `MaxHp`, `CurrentHp`, `MaxEnergy`, `ClassId`, `ClassName`.

Méthodes de parsing :

| Méthode | Trame source probable (1.29 stock) |
|---|---|
| `ParseFromBp` | `Bp` (ping/heartbeat) **étendu** pour inclure les heroes — base de la hero-list. |
| `ParseAEAStats` | `AEA<...>` = stats compte (`AccountExtraAttributes`). |
| `ParseFromPM` | `PM<...>` — non identifié dans le 1.29 stock. Peut-être « PartyMember » (paquet de party étendu). |

#### `Bot_Dofus_1._29._1.Comun.Network.TcpClient` — **CRUCIAL : tagging par préfixe**

| Méthode | Interprétation |
|---|---|
| `ExtractHeroId` | Côté **entrée** (paquet du serveur) : extrait l'ID du héros destinataire **depuis le paquet**. |
| `StripHeroPrefix` | Retire ce préfixe **avant** de router le paquet vers le handler standard du frame. |

→ **Théorie de protocole** : sur Aqua, **chaque paquet de jeu est préfixé** d'un
identifiant héros (ex : `H<id>|GTS<turnEntity>|<timer>|<num>`), et le client SynFus
strip ce préfixe avant de traiter avec son `FightFrame`/`CharacterFrame`. Le proxy
SynFus a aussi `RewriteBpPingBytes` dans `Bot_Dofus_1._29._1.Comun.Proxy.ProxyServer`
(à côté des handlers `HandleAYKRedirect`/`HandleAXKRedirect`/`HandleAKPacket`/
`InjectToServer`/`InjectToClient`) → preuve que le PING `Bp` est **modifié à la
volée** par le proxy SynFus pour faire passer la hero-list.

#### `Bot_Dofus_1._29._1.Interfaces.UI_Principal` — UX héros

Méthodes UI confirmant la sémantique :
- `HeroIcon_Click` → click sur un portrait dans la barre d'icônes héros.
- `SelectHero` / `AutoSelectHeroIcon` / `SelectHeroForUI`.
- `RefreshTabsAfterHeroSwitch` → après switch, on rafraîchit les onglets (Spells/Inventory/Char/Map) avec les data du hero actif.
- `UpdateHeroOwnership` → mise à jour de l'ownership des entités sur la map (qui appartient à quel hero).

### A.2 Côté serveur — **rien de public**

Aucun émulateur OSS connu n'implémente le mode héros :

- `Arakne/Araknemu` (Dofus 1.29 Java) — **non**, fonctionnalité absente (cf. WebFetch 2026-05-22).
- `Aerafaal/dofus-retro` (1.29 C#/.NET POC) — pas de hero mode (cf. WebFetch).
- `dyshay/OblivionRetro` — pas mentionné.
- `Emudofus/Shivas` (1.29.1 Java) — pas mentionné.
- `kralamoure/retroproto` (Go, protocole 1.29 stock) — confirme via WebFetch que **le protocole standard ne définit aucun paquet « hero »**.

---

## B. Flow de combat mode héros (reconstruit)

> Tout ce qui suit est **déduit** de la structure SynFus Aqua. À **vérifier par
> capture wire** sur un serveur Abrak ou Aqua si on porte le bot dessus.

### B.1 Init de session

```
Auth login standard (AT, ASK, etc.)
   ↓
Sélection compte (le compte web Aqua qui possède N persos)
   ↓
Le serveur envoie HeroMode_Init (paquet inconnu, op-code à déterminer)
   ↓
GameClass.IsHeroMode := true
   ↓
Le serveur envoie HeroMode_AccountBasic (infos compte)
   ↓
Pour chaque hero lié : HeroMode_AccountEntityHero (Id, Name, Level, GfxId, Stuff, …)
   ↓
Le client ouvre une seule connexion TCP — TOUS les heroes partagent ce socket
   ↓
GameClass.MainHero := heros sélectionné par le user dans l'UI
```

### B.2 Hors-combat : un seul hero actif sur la map

Le serveur ne broadcaste qu'**un** perso « sur la map » (cell, sprite). Les autres
heroes sont en « stand-by » (probablement assis dans un slot interne du compte
web, comme une mule dans un coffre). Le client SynFus en garde l'état complet
(`Heroes` collection + `_heroCharacters` dictionnary).

**Switch hors-combat** :
```
User click HeroIcon_2
   ↓
UI_Principal.SelectHero(id)
   ↓
GameClass.BeginHeroSwitch + PushHeroContext (save hero #1 state)
   ↓
[Paquet client→serveur inconnu — probablement un GA<sub-op-code> ou Hs<id>]
   ↓
Le serveur teleport hero #1 hors-map, place hero #2 sur la cell sauvegardée
   ↓
Le serveur renvoie : HeroSwitchMessage + HeroMapInfo + nouveau GDM/GC
   ↓
EndHeroSwitch + PopHeroContext (load hero #2 state)
   ↓
RefreshTabsAfterHeroSwitch
```

### B.3 Entrée combat mode héros

**Hypothèse forte** (à confirmer en capture) : quand un combat démarre, **les
N heroes liés rejoignent automatiquement le combat** côté serveur (ils
n'apparaissent qu'en combat, pas hors-combat sur la map). Ce serait la valeur
ajoutée du système.

```
User déclenche combat (GA907<cell>;<grpId>) avec hero actif
   ↓
GC<type> echo
   ↓
[Le serveur enrolle automatiquement les N heroes liés dans le combat]
   ↓
Phase placement : pour chaque hero, le client doit envoyer Gp<cell>
   (mais comment le client choisit pour les N persos ? Probablement
   en switchant via Hero_Switch_Message reçu du serveur,
   ou en routant Gp avec un préfixe H<id> à l'envoi)
   ↓
GR1 ack (probablement 1 GR1 global suffit pour toute l'équipe, ou
   1 par hero — capture requise)
   ↓
GTM autoritatif : liste de TOUS les combattants (les N heroes + ennemis)
   ↓
Phase combat
```

### B.4 Pendant le combat : système de tours

**Hypothèse principale** : le serveur orchestre l'ordre des tours **par initiative**
(`GTS` standard), et **chaque tour de hero arrive sur le même socket TCP**. Le
client utilise `ExtractHeroId` / `StripHeroPrefix` pour savoir lequel des N heroes
joue, et bascule l'UI dessus.

```
Serveur → Client : GTS<id_hero_X>|<timer>|<num>
   le client TcpClient strip le préfixe H<id_X>, route vers FightFrame
   FightFrame.Hero_Turn_Start déclenche BeginHeroSwitch automatique
   ↓
Client → Serveur : <préfixe-hero-X>GA300<sortId>;<cell>
   (le serveur sait que c'est hero X qui lance le sort grâce au préfixe)
   ↓
Serveur → Client : GAS<id_X> / GAF<code>|<id_X>
   ↓
Client → Serveur : GKK0 (+ préfixe ?)
   ↓
Client → Serveur : Gt (pass turn)
   ↓
Serveur → Client : GTF<id_X> + GTR<id_X> + Hero_Switch_Message(next hero)
   ↓
Serveur → Client : GTS<id_hero_Y> ...
```

→ Très probable d'avoir un **op-code H<id>...|** ou similaire en préfixe sur
**chaque paquet** dans les deux sens. Sans capture wire on n'a pas la forme
exacte du préfixe.

### B.5 Fin de combat

```
Serveur → Client : End_FightAsync (Fight_End paquet GE équivalent)
   ↓
Le serveur "ramasse" les N heroes — un seul revient sur la map (le MainHero)
   ↓
HeroMode_Stats refresh par hero (loot/xp répartis)
```

---

## C. Implémentations existantes

### C.1 SynFus Aqua v1.1.0 (analyseable localement)

**Localisation :** `C:\Users\touki\Desktop\SynFus_Aqua_v1.1.0\Synfus.dll`
(2 887 168 octets, .NET 8 WPF, obfusqué — strings chiffrées, méthodes IL valides
mais avec headers volontairement cassés sur certaines pour casser ILSpy).

**Statut :** **implémentation cliente complète et fonctionnelle** d'un client
Dofus Retro + bot pour le serveur Aqua, **incluant le mode héros**. Toutes les
classes/propriétés/méthodes énumérées en section A sont des **noms réels** lisibles
via `System.Reflection.Metadata` :

```
Bot_Dofus_1._29._1.Otros.Game.GameClass               (state machine héros)
Bot_Dofus_1._29._1.Otros.Game.Character.HeroCharacter (DTO hero)
Bot_Dofus_1._29._1.Comun.Frames.Juego.CharacterFrame  (parsing init/list)
Bot_Dofus_1._29._1.Comun.Frames.Juego.FightFrame      (parsing combat)
Bot_Dofus_1._29._1.Comun.Network.TcpClient            (StripHeroPrefix, ExtractHeroId)
Bot_Dofus_1._29._1.Comun.Proxy.ProxyServer            (RewriteBpPingBytes)
Bot_Dofus_1._29._1.Interfaces.UI_Principal            (UI portraits + switch)
Bot_Dofus_1._29._1.Controles.HeroIcon                 (control WPF)
Bot_Dofus_1._29._1.Controles.HeroRefreshButton        (control WPF)
```

Notable : SynFus **Hystoria v1.1.7** (`C:\Users\touki\Desktop\SynFus_Hystoria_v1.1.7\`)
**ne contient AUCUNE** classe Hero* — confirmation que **Hystoria n'a pas le mode
héros**. Strings scan vide pour `Hero|hero` dans `Synfus.dll` Hystoria.

**Outils utilisés :**
- `ilspycmd` (9.0.0.7889) — décompilation échoue (`BadImageFormatException: Invalid method header: 0x38`, anti-décompile volontaire).
- `dotnet` + `System.Reflection.Metadata.PEReader` — énumération metadata réussie, suffisante pour reconstruire la matrice des handlers.
- Programme custom à `C:\Users\touki\AppData\Local\Temp\synfus-typelist\TypeList\Program.cs` (utilisable pour ré-énumérer si besoin).

### C.2 dyshay / Bot-Dofus-Retro

**Statut :** PAS de mode héros. Source à `https://github.com/dyshay/Bot-Dofus-Retro`,
mono-perso strict. Sa structure de sorts (XML `hechizos_dyshay.xml`) et sa logique
combat n'ont aucune notion de hero/team interne.

### C.3 Autres bots / OSS Dofus 1.29

| Projet | Hero mode ? |
|--------|-------------|
| `Romain-P/Guinness-Bot` (Kotlin MITM) | **Non**, single-character only. |
| `louisabraham/LaBot` (Python Dofus 2) | Non (cible Dofus 2.0, pas 1.29). |
| `Mrpotatosse/AivyCore` (C# proxy archived 2021) | Non, framework générique. |
| `kralamoure/retroproto` (Go protocole) | Non, protocole **stock** sans extension hero. |

### C.4 krm35/dofus-retro-heros (faux ami)

`https://github.com/krm35/dofus-retro-heros` — **ATTENTION** : le nom est trompeur.
C'est **un launcher Electron + electron-tabs** qui ouvre N webviews du client Dofus
dans une seule fenêtre OS, **côté client uniquement**. **N'a aucun rapport** avec
le mode héros serveur des Abrak/Aqua.

### C.5 Yokani/DofusHeroes

Pareil : script AHK pour multi-fenêtres + bind raccourcis. **Pas un vrai mode héros**.

---

## D. Notes / forums

### D.1 Serveurs FR qui implémentent le mode héros

| Serveur | URL | Version | Notes |
|---------|-----|---------|-------|
| **Abrak** | https://abrak.fr/actualites/2-heroes | 1.38 / 1.40 retro | « 8 perso en 1 fenêtre », mode héros marketé comme exclusif |
| **Aqua** | https://wiki.play-aqua.net/hero-mode.html | semi-like retro | « contrôler plusieurs persos simultanément », switch via onglets portraits |
| **Atoria** | https://serveur-prive.net/dofus/atoria | semi-like | mentionne « mode héros » |
| **Hystoria** | https://serveur-prive.net/dofus/hystoriamono | 1.40-1.43 | **MONOCOMPTE, pas de mode héros** ⚠️ |

### D.2 Documentation publique

- **abrak.fr/actualites/2-heroes** : descriptif marketing (« 8 persos », « pas de latences »). **Aucun détail technique**.
- **wiki.play-aqua.net/hero-mode.html** (analysé par WebFetch 2026-05-22) :
  > « Le joueur peut contrôler plusieurs personnages simultanément »
  > « Création de groupe via une interface dédiée et intuitive »
  > « Changement instantané d'un personnage à un autre grâce aux onglets de portraits »
  > « Une seule interface pour plusieurs personnages »
  > « Aucune automatisation, aucun avantage déloyalement acquis »

  → **Aucune mention de paquet, opcode, ou détail réseau.** Le wiki présente le système
  comme une simple amélioration ergonomique. La réalité technique (préfixe par paquet,
  enrollment auto en combat) reste opaque.

- **cadernis.fr** :
  - Lien direct vers `cadernis.fr/d/2937-dofus-retro-hros` (URL listée par les search results) → **HTTP 404** en WebFetch direct. Le thread existe mais n'est pas accessible sans auth ou avec une URL légèrement différente.
  - URL pivot `cadernis.fr/index.php?threads/dofus-retro-heros.2937/` → 403 (auth requise).
  - Le forum cadernis francophone a effectivement un thread « Dofus Retro Héros » mais le contenu n'a **pas pu être extrait** lors de cette recherche (auth ou cookies requis).

### D.3 Threads forum non-techniques

- `dofus.com/fr/forum/1787-dofus-retro/2363856-mode-heros-dofus-retro-dofus-retail` — discussion joueurs sur l'arrivée du mode héros sur Dofus Retail officiel (sans rapport avec serveurs privés).
- `dofus.com/fr/forum/1978-serveurs-historiques/2436749-mode-heros` — idem, débat joueur, **403** sur WebFetch (cloudflare).
- `dofus.com/fr/forum/1829-suggestions-idees/2402875-mode-heros-dofus-retro` — suggestion joueur, pas technique.

### D.4 Compte Toukiki sur cadernis

Le user a un compte cadernis dédié (`toukiki83@gmail.com`). Une **session
authentifiée Chrome** sur ce compte permettrait d'extraire le contenu du
thread `2937-dofus-retro-hros` qui contient probablement la seule discussion
technique francophone publique du sujet (workflow déjà documenté dans
`memory/tech_cadernis_savoir.md` : crawl Chrome authentifié).

---

## E. Notre projet (Bot-dofus / BotDofus.Wpf)

### E.1 État actuel — aucun support hero mode

- **Cible serveur : Hystoria** — hystoria.net est **monocompte**. Pas de mode héros côté serveur.
- Scan grep du repo : **aucune** occurrence de `hero|héros|HeroMode|multi-perso` significative.
  - 1 seule mention dans `docs\AUDIT-CODE-IA-COMBAT.md:567` : « (multi-perso swap) » dans le contexte d'un bug théorique de re-ID quand on enchaîne plusieurs combats — **n'a rien à voir** avec un vrai mode héros.
- **Roadmap** (`docs/FEATURES-ROADMAP.md`) : mentionne `Multi-compte synchro: Bot leader + suiveurs` et `Mode multi-bot orchestré (raid donjon): Synchroniser 8 comptes` (= multi-bot, pas mode héros) — confirme l'absence de toute roadmap hero mode.
- **CLAUDE.md** : aucune mention.

### E.2 Implications

- **Court terme** : aucun travail à faire. Hystoria n'a pas de mode héros, donc pas d'enjeu.
- **Si on porte le bot vers Abrak/Aqua** un jour :
  1. **Capturer le protocole** en mode passif (Luffy-bot a déjà `Compte.ModePassif` qui désactive tout envoi → idéal pour sniffer pur).
  2. Mapper le **préfixe par hero** (op-code et format à déterminer).
  3. Étendre `SessionProxy` pour gérer `RewriteBpPingBytes` et `Strip/InjectHeroPrefix` à la SynFus.
  4. Refactor `Combat` / `Personnage` pour supporter N persos simultanés (actuellement `Compte` = 1 perso).
  5. Brancher l'IA combat pour orchestrer N personnages dans un même combat (extension du `MoteurReglesCombat`).
- **Effort estimé** : **3-4 semaines** si le protocole se laisse capturer proprement. Probablement plus si Abrak/Aqua chiffrent leurs préfixes.

---

## F. Reste à découvrir via capture réseau

Si on se lance un jour, capturer en priorité :

| Phase | Paquet à comprendre |
|---|---|
| Init | Format exact de `HeroMode_Init` côté serveur (op-code, structure de la hero-list) |
| Init | Comment le ping `Bp` est étendu pour transporter la hero-list (`RewriteBpPingBytes` SynFus) |
| Switch hors-combat | Op-code client→serveur pour demander un switch d'héros |
| Switch | Format du `HeroSwitchMessage` serveur→client |
| Préfixe global | **Le plus critique** : forme exacte du préfixe « ce paquet appartient au hero X » dans les 2 sens |
| Combat entrée | Comment les N heroes s'enrôlent (auto ou via paquet client) |
| Combat tour | Format de `Hero_Turn_Start` (variante de `GTS` ?) |
| Combat tour | Format de `Hero_Handoff` (le serveur passe-t-il la main automatiquement ?) |

---

## Sources

- **Locales** :
  - `C:\Users\touki\Desktop\SynFus_Aqua_v1.1.0\Synfus.dll` — DLL cliente Aqua (mode héros complet, source primaire d'analyse).
  - `C:\Users\touki\Desktop\SynFus_Hystoria_v1.1.7\Synfus.dll` — contre-exemple (Hystoria sans mode héros).
  - `C:\Users\touki\Desktop\Mélange\synfus-decompile\` — décompilation partielle (utilities/crypto uniquement, classes Hero* non décompilables).
  - `C:\Users\touki\AppData\Local\Temp\synfus-typelist\TypeList\Program.cs` — outil d'énumération de types/méthodes (ré-utilisable).

- **Web** :
  - [Abrak — Les héros](https://abrak.fr/actualites/2-heroes)
  - [Aqua Wiki — Mode Héros](https://wiki.play-aqua.net/hero-mode.html)
  - [Aqua Wiki — Guides](https://wiki.play-aqua.net/guides.html)
  - [kralamoure/retroproto — protocole 1.29 stock](https://github.com/kralamoure/retroproto) (confirme absence du mode héros dans le stock)
  - [krm35/dofus-retro-heros](https://github.com/krm35/dofus-retro-heros) (faux ami, electron-tabs)
  - [Yokani/DofusHeroes](https://github.com/Yokani/DofusHeroes) (faux ami, AHK)
  - [Arakne/Araknemu](https://github.com/Arakne/Araknemu) (émulateur 1.29 Java, pas de hero mode)
  - [Aerafaal/dofus-retro](https://github.com/Aerafaal/dofus-retro) (POC reverse, pas de hero mode)
  - [Romain-P/Guinness-Bot](https://github.com/Romain-P/Guinness-Bot) (MITM Kotlin, pas de hero mode)
  - [HydreIO/dofus-protocol-1.29](https://github.com/HydreIO/dofus-protocol-1.29) (archivé 2020, pas de hero mode)
  - [serveur-prive.net — Hystoria](https://serveur-prive.net/dofus/hystoriamono) (confirme : monocompte, pas de hero mode)
  - [RPG-Paradize — Abrak](https://www.rpg-paradize.com/site-ABRAK+-+RETRO+1.40+-+SEMILIKE+-+HROS+-+8+Perso+en+1-114503)
  - [synfus.net](https://synfus.net/) (site officiel SynFus — 403 sur WebFetch, accessible navigateur)

- **Forum cadernis** (référence indirecte, contenu non extrait) :
  - `cadernis.fr/d/2937-dofus-retro-hros` (404 sur WebFetch direct, exists per Google index)
  - `cadernis.fr/index.php?threads/dofus-retro-heros.2937/` (403 sur WebFetch, auth requise)

---

## Conclusion

**Aucune référence externe substantielle** ne documente le protocole wire du mode
héros (Abrak / Aqua). Le seul **artefact technique** exploitable est la **DLL
SynFus Aqua** présente localement, qui révèle la **structure C# côté client**
(classes, handlers, events) mais **pas le format des paquets sur le fil**.

**Si on cible un jour Abrak ou Aqua**, il faudra :
1. **Capturer le protocole nous-mêmes** (Luffy-bot a déjà tout le proxy MITM et le mode passif — idéal pour sniffing).
2. S'inspirer de la **structure SynFus** pour le découpage logique côté client (CharacterFrame/FightFrame + StripHeroPrefix sur TcpClient).
3. Faire attention au **cipher Aqua** (la classe `VeloraCipher` existe dans le dump partiel — différent du cipher Hystoria '-').

**Pour Hystoria (notre cible actuelle)** : non concerné, sujet à archiver comme
référence future.
