# Migration Hystoria → Rafale

> 2026-06-02 — Phase 1 (config) + Phase 2 (crypto/protocole/level cap)

## Contexte

- **Ancien** : Abrak (51.89.153.20:1303/1304), serveur sur lequel le code gameplay
  (combat, banque, récolte, mode héros) a été développé. Nom commun en interne
  du temps de Hystoria, mais c'était bien Abrak depuis longtemps.
- **Nouveau** : Rafale (`playrafal.com`, port 26118), serveur Dofus Retro 1.29
  privé. Économie / contenu / level cap DIFFÉRENTS de l'ancien — on ne présume
  pas iso-gameplay.

Le code bas-niveau (protocole, parsing) reste 1.29 standard.

## Phase 1 — Connexion & adresse serveur

### Audit emplacements (avant centralisation)

| Fichier | Ligne | Hardcode | Statut |
|---|---|---|---|
| `Commun/Reseau/ConfigReseau.cs` | 21,24,30,33,47,53,61,70 | IP `51.89.153.20`, ports 1303/1304/50303/50304 | ✅ Centralisé Rafale |
| `App.config` | 6,7,10,11,16,17,20 | IP+ports auth/jeu/proxy | ✅ Déprécié (vers JSON) |
| `BotDofus.Wpf/MainWindow.xaml.cs` | 444 | `"51.89.153.20", 1303, 1304, 50303, 50304` | ✅ Via ConfigReseau |
| `BotDofus.Wpf/MainWindow.xaml.cs` | 690,691,695 | Libellés UI `51.89.153.20:1303/1304` | ✅ Dynamiques |
| `BotDofus.Wpf/MainWindow.xaml.cs` | 985,995 | Chemins client `Hystoria` | ✅ Via ConfigReseau |
| `Utilitaires/Hystoria/LanceurDofus.cs` | 21 | Chemin `AppData\Hystoria\Dofus\...` | ✅ Via ConfigReseau |
| `Utilitaires/Aqua/PatcheurConfigXml.cs` | 54 | Port `1303` par défaut | ⏳ Inchangé (constructeur param) |
| `Commun/Reseau/ClientAutonomeAbrak.cs` | 249 | Paquet `Ap1303` | ✅ Via ConfigReseau |
| `Divers/Scripts/Api/ApiAnka.cs` | 105 | `serverName() => "Hystoria"` | ✅ Via ConfigReseau |
| `Formulaires/FormulairePrincipal.cs` | 29,59 | Libellés UI "Hystoria" | ✅ Dynamiques |
| `Formulaires/FormulaireOptions.cs` | 29 | Libellé "Hôte serveur Hystoria" | ✅ Générique |

### Centralisation : `ConfigReseau`

`Commun/Reseau/ConfigReseau.cs` est désormais l'unique source de vérité.
Persisté sur disque dans `config-reseau.json` à côté de `Luffy-bot.exe`.

```jsonc
{
  "NomServeur": "Rafale",
  "HoteDistant": "",                          // ← à capturer
  "PortDistant": 26118,
  "HoteJeuDistant": "",                       // ← à capturer (souvent =HoteDistant)
  "PortJeuDistant": 26118,
  "AdresseEcouteLocale": "127.0.0.1",
  "PortEcouteLocal": 26118,
  "PortEcouteJeuLocal": 26119,
  "PortSourceMarqueur": 50118,
  "PortSourceMarqueurJeu": 50119,
  "CheminClientDofus": "C:\\Games\\Rafal\\Dofus.exe",
  "CheminConfigXmlClient": "C:\\Games\\Rafal\\resources\\app\\retroclient\\config.xml"
}
```

### Capture IP — sniffer de discovery

L'IP du serveur Rafale n'est PAS exposée dans le `config.xml` du client
(résolue côté SWF obfusqué). Sous-domaines `connect/game/play/auth.playrafal.com`
n'existent pas. → on capture au runtime.

`Utilitaires/Reseau/SniffeurIpServeur.cs` :
- poll `IPGlobalProperties.GetActiveTcpConnections()` toutes les 250 ms
- filtre les IP privées (loopback, 10/8, 172.16/12, 192.168/16, 169.254/16)
- retourne la 1re IP publique sortante observée sur `:PortCible`

**Workflow utilisateur** au prochain "Lancer Jeu" :
1. Bot détecte `EstIpServeurInconnue` → message + démarre le sniffer (60 s)
2. User lance manuellement `C:\Games\Rafal\Dofus.exe` (ou via le launcher Zaap)
3. Sniffer capture l'IP → persiste dans `config-reseau.json`
4. Re-cliquer "Lancer Jeu" → WinDivert + proxy MITM actifs

## Phase 2 — Vérification protocole

### Crypto

**`Utilitaires/Crypto/HystoriaCipher.cs`** (mal nommé : c'est juste l'enveloppe
`CRYPTS` standard, pas spécifique Hystoria) :
- PSK : `0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef`
- Algo : XOR avec keystream dérivé `(PSK[i+seq mod 32] XOR seq_low XOR seq_high XOR (i & 0xFF))`
- Enveloppe : `"CRYPTS" + seq_hex(8) + base64(ciphertext)`
- Active après réception `HG` ou `CRYPTOK` côté client

**`Utilitaires/Crypto/CanalAbrak.cs`** (canal `-`) :
- 16 clés rotatives reçues via paquet serveur `AK<id>k0|k1|...`
- Cipher custom Abrak v1.48 reconstitué depuis core.swf
- Auto-calibré via checksum oracle (PrétAuDechiffrement)

### À VALIDER sur Rafale (1er run)

| Test | Indicateur OK | Indicateur KO | Action si KO |
|---|---|---|---|
| Handshake auth | log `[CIPHER] HG reçu` | `[CIPHER] CRYPTFAIL` | nouvelle PSK à extraire |
| Cipher CRYPTS | log `CRYPTOK reçu` | mêmes logs avec déchiffrement KO | idem |
| Canal `-` | paquets entités/positions overworld lisibles | parsers `Gp/GR1/GA001` muets | re-extraire clés AK + ordre |
| AYK | log `[CIPHER] HG reçu` post-AT | timeout AT | port jeu peut différer |

### Level cap > 255 — audit

**Aucun risque trouvé** : tous les parsings de niveau utilisent `int.TryParse`
(safe jusqu'à 2,147,483,647).

Emplacements vérifiés :
- `MessagePersonnage.Desserialiser` (Authentification:247) : `int`
- `MessageNouveauNiveau.Desserialiser` (ANK, Authentification:442) : `int`
- `MessageListeSorts.Desserialiser` (SL, Authentification:471) : `int`
- `MessageActeurAbrakAjout.Desserialiser` (NL, JeuExtras:58) : `int`
- `MessageStats.Desserialiser` (As, Authentification:316) : tous `int`/`long`
- `TrameJeu.ParserNiveau` (1289) : `int`
- `BaseSorts.Charger` (XML, 142) : `int`

Les seuls `byte` autour du mot "niveau" sont :
- `Cellule.LayerNiveau` (byte) : superposition Z d'une cellule de carte
  (object1/object2/zone), **pas un niveau de perso/monstre**
- `DecompresseurMapData.cs:73` `byte niveau = (byte)(bits[1] & 0b0000_1111)` :
  idem (champ 4-bit du MapData, range 0-15)

**→ Aucun patch nécessaire pour le level cap.**

### Protocole — handshake observé

Le handshake auth+jeu reste 1.29 standard tant que Rafale n'a pas re-tooled
le client (peu probable, c'est un serveur d'émulation) :

```
[1] Client → auth   : (TCP SYN vers IP:port_auth)
[2] Serveur → Client : "HC<clé hex>"       handshake challenge
[3] Client → Serveur : "1.48.0"            version client
[4] Client → Serveur : "<login>\n#1<mdpChiffré>"  (auth)
[5] Serveur → Client : "Af" + AH<liste serveurs>  (succès)
[6] Client → Serveur : "Ap<port_auth>" + "Ai<identity>" + "Ax"
[7] Serveur → Client : "AYK<ip_jeu>:<port_jeu>;<ticket>"
[8] Client → jeu    : (TCP SYN vers ip_jeu:port_jeu)
[9] Client → Serveur : "AT<ticket>"
[10] Serveur → Client : "AK<id>k0|k1|...|k15"     (clés canal '-')
[11] Serveur → Client : "HG" puis "CRYPTOK" (chiffrement actif)
```

L'IP/port du jeu vient du paquet `AYK` (parsé par `ContexteCompte:573`,
`HoteJeu peut être hostname ou IP`). Sur Rafale, si AYK pointe une autre IP
que l'auth, c'est elle qui prime — le sniffer capture la 1re mais le
`AYK` est la vérité finale.

## Récap Phase 1+2 — modifié vs reste à valider

**MODIFIÉ :**
- Centralisation `ConfigReseau` (nouvelle source unique)
- Persistance `config-reseau.json`
- `SniffeurIpServeur` (discovery IP runtime)
- `MainWindow.BtnLancerJeu` async + lecture ConfigReseau + sniffer auto
- `MainWindow.BtnDocs` affiche valeurs dynamiques
- `MainWindow.ObtenirCheminClientDofus` consulte ConfigReseau d'abord
- `LanceurDofus.CheminDefaut` lit ConfigReseau (au lieu de hardcode Hystoria)
- `ApiAnka.serverName()` lit ConfigReseau.NomServeur
- `ClientAutonomeAbrak.OnAH` envoie `Ap<port>` dynamique (au lieu de `Ap1303`)
- `App.config` épuré (juste journalisation)
- Labels UI : `FormulairePrincipal.AProps`, `FormulaireOptions`
- `SessionProxy` logs cipher améliorés (mention CRYPTS générique + indication PSK à vérifier si CRYPTFAIL)

**RESTE À VALIDER (Phase 2 live) :**
- 🔴 IP serveur Rafale (capture via sniffer au 1er run)
- 🟠 PSK CRYPTS identique sur Rafale (log `CRYPTOK` vs `CRYPTFAIL` au 1er run)
- 🟠 Clés AK canal `-` (auto-calibrées via CanalAbrak — devrait fonctionner)
- 🟠 Port jeu (peut différer du port auth — AYK source de vérité)

**POINTS BLOQUANTS :**
- L'IP serveur Rafale n'a pas pu être trouvée dans le repo, le launcher, ou
  les DNS publics. Doit être capturée par le sniffer au prochain run.

## État des lieux gameplay (préparation suite — combats / mode héros)

> *Pas modifié ici, juste recensement pour la suite.*

### Combats
- `Divers/Combats/IA/` : `ConfigCombat`, `RegleSort`, `DecideurCombat`,
  `MoteurReglesCombat`, `MoteurTactique`, `ConfigDelaisCombat`
- `Commun/Frames/TrameJeu.JouerTourCombatAsync` : pipeline GTS → décision →
  GA300 → GKK0 → Gt
- `Commun/Frames/TrameCombat.cs` : phase placement + tour
- État runtime : `Divers/Combats/Combat.cs` + `Combattants`
- Stats sorts par niveau : `Resources/data/hechizos_dyshay.xml` (281 sorts,
  niveaux 1-6) — sera probablement différent sur Rafale (level cap > 6)
- BDD monstres : `Resources/data/monsters_*.json` — à valider sur Rafale
  (l'économie/contenu peuvent différer)

### Mode héros (multi-perso jusqu'à 4)
- `Divers/MultiAccount/DetecteurModeHeros.cs` : NO/NOL parse
- `Divers/MultiAccount/ActivateurHerosAbrak.cs` : séquence NOL→NS→PV→NA
- `Divers/MultiAccount/IACombatHerosSimple.cs` : IA tour héros lié
- `Divers/MultiAccount/AutoInviteurHeros.cs`
- Toutes ces classes assument le protocole Abrak/Hystoria. À tester en live
  sur Rafale.

### Banque
- 3 fixes successifs déjà appliqués (V1/V2/V3 — voir `docs/PLAN-FIX-BANQUE-V3.md`).
- Comportement OQ partiel + sérialisation workflow OK sur Abrak.
- À re-tester sur Rafale (économie différente → quantités différentes →
  potentiellement comportement OQ partiel différent).
