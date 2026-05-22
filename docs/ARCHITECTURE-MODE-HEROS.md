# ADR-006 — Architecture support « Mode Héros » Hystoria

- Statut : Proposé
- Date : 2026-05-22
- Auteurs : System Architecture Designer (assist. Claude Opus 4.7)
- Liés : ADR-001 (modes combat), ADR-002 (pipeline déplacement combat), ADR-003 (MapViewer combat), CLAUDE.md §IA combat
- Fichiers existants impactés (HOOKS uniquement, pas de modif dans cet ADR) :
  - `Divers/ContexteCompte.cs` (constructeur + handlers combat)
  - `Divers/Compte.cs` (nouveaux champs annexes)
  - `Divers/Combats/Combat.cs` (event d'ordre des tours partagé)
  - `Commun/Frames/TrameJeu.cs:43-193` (`EnregistrerGestionnaires`) + `:980` (`JouerTourCombatAsync`)
  - `BotDofus.Wpf/MainWindow.xaml.cs` (création/lifecycle des contextes)
- Fichiers NOUVEAUX proposés (à créer dans un commit ultérieur de réalisation, **pas** dans cet ADR) :
  - `Divers/MultiAccount/GroupeHeros.cs`
  - `Divers/MultiAccount/DetecteurModeHeros.cs`
  - `Divers/MultiAccount/DispatcheurTours.cs`
  - `Divers/MultiAccount/PersonnageRef.cs` (DTO compagnon, voir `docs/MODELE-DONNEES-MODE-HEROS.md`)
  - `multiaccount/groupe.json` (état persistant, à la racine projet à côté de `peleas/`)

---

## 1. Contexte

### 1.1 Le besoin

Hystoria propose, comme certains private servers Retro modernes, un **« mode héros » intégré au serveur** : un même joueur lie N personnages (jusqu'à 8 sur Hystoria) à son compte. **Tous ces persos sont présents simultanément dans le MÊME combat**, et le serveur orchestre l'ordre des tours (initiative globale).

User cible : **1 Sadida (leader) + 7 Enutrof (suiveurs)**, configuration farm/PP. Chaque perso garde ses propres :

- sorts appris,
- équipement,
- caractéristiques,
- **et surtout sa propre `ConfigCombat`** (peleas/`<perso>`.json — Sadida = règles invocation/heal, Enutrof = règles Pelle/Maladresse/FlècheChance).

### 1.2 Ce qui existe déjà (acquis)

- 1 `ContexteCompte` par compte/client Dofus, qui agrège :
  - `Compte` (identité),
  - `SessionProxy` (MITM avec un client Dofus),
  - `Personnage` (état runtime du **seul** personnage actif),
  - `Combat` (état combat **par perso**),
  - `ConfigCombat` chargée depuis `peleas/<Identifiant>.json` (`ContexteCompte.cs:98`),
  - `TrameJeu` (handler paquets gameplay),
  - `MoteurReglesCombat` (évaluation règles par tour).
- Plusieurs `ContexteCompte` parallèles supportés (`MainWindow.Comptes` : `ObservableCollection<CompteVm>`, chacun avec son propre proxy port). Sert aujourd'hui au **multi-client** (lancer 2 clients Dofus côte à côte, **combats indépendants**).
- `TrameJeu.JouerTourCombatAsync` (`TrameJeu.cs:980`) est déclenché par le handler `MessageTourCombatAbrak` (`:157`) uniquement quand `msg.IdentifiantCombattant == _etat.Personnage.Identifiant` (`:173`) — un perso ne joue **que son tour**.
- `Combat` (`Combats/Combat.cs`) maintient `Allies`/`Ennemis`/`IdentifiantAllie` mais part du principe qu'**il existe 1 seul « moi »** par combat.

### 1.3 Ce qui manque

| Manque | Conséquence si non résolu |
|---|---|
| Détection « entrée mode héros » | Le bot croit être en combat solo, ignore les 7 autres persos pop dans le GTM. |
| Lien explicite entre les N `ContexteCompte` qui sont en réalité **le même joueur** | Les 8 contextes considèrent les 7 autres persos comme des « alliés joueurs externes » (id > 0 = allié neutre). L'IA d'un perso ne sait pas qu'elle peut planifier autour de l'IA d'un autre perso du groupe. |
| Routing des tours `GTS<perso X>` vers le bon décideur | Si on a 8 `ContexteCompte`, le serveur n'envoie le `GTS` qu'**au client réel actif** (1 seul client = 1 seul `SessionProxy`). Les autres `ContexteCompte` ne reçoivent **rien**. Sans dispatcher, 7 persos sur 8 restent muets et timeout (45 s par tour) → kick. **C'est le piège principal.** |
| Partage de l'état combat (1 instance `Combat` partagée vs 8 instances divergentes) | 8 `OnCombattantsAbrak` qui parsent le même GTM, chacun classe les 7 autres persos en `Ennemis`/`Allies` selon l'heuristique id>0/id<0 (`TrameJeu.cs:235`) → désync potentielle. |
| Persistance composition du groupe | Reformer le groupe à chaque démarrage = friction. |

### 1.4 Pré-requis incertain (À VALIDER par capture)

> ⚠️ **L'archi ci-dessous suppose** que, côté serveur Hystoria, le mode héros se manifeste par **UN SEUL client Dofus** auquel le serveur pousse les 8 GTS à tour de rôle. C'est le modèle « observed-héros » des private servers Retro modernes.
>
> Si la capture forensic montre au contraire **N clients distincts synchronisés serveur-side** (modèle « multi-instances liées »), la couche dispatcher est inutile (chaque client reçoit naturellement son propre GTS) ; il reste alors juste à ajouter le partage d'ordre des tours + la composition du groupe pour l'UI. La couche `GroupeHeros` et la persistance restent valides dans les deux cas.
>
> **3 points à confirmer par les logs à venir** :
> 1. Nombre de connexions TCP simultanées du client Dofus quand le mode héros est actif ?
> 2. Format exact du paquet qui annonce la composition du groupe (probablement une variante de `Im` info-serveur, ou un nouveau préfixe genre `GH`/`HG`/`Pg` — à identifier).
> 3. Format des GTS quand 8 persos sont engagés : tous portent l'`IdentifiantCombattant` du perso à jouer, ou y a-t-il un sous-paquet de scope (« cluster héros X » vs « cluster héros Y ») ?

---

## 2. Décision

On introduit **une couche d'agrégation `GroupeHeros`** au-dessus de N `ContexteCompte`, avec :

1. **Un `DetecteurModeHeros`** branché dans `TrameJeu.OnCombattantsAbrak` (GTM) + un nouveau handler générique sur les paquets `Im` (info serveur). Il analyse la composition et **promeut** un set de `ContexteCompte` en `GroupeHeros` quand il détecte des marqueurs (à valider — cf. §1.4).
2. **Un `GroupeHeros`** qui mutualise l'état combat (1 instance `Combat` réplicée — un perso = un acteur dans le même combat), l'ordre des tours, et la composition (`PersonnageRef[]`).
3. **Un `DispatcheurTours`** qui reçoit « tour du perso X » depuis le ou les `TrameJeu` du groupe et **route l'event vers le `ContexteCompte` correspondant** — qui à son tour exécute son `JouerTourCombatAsync()` avec **sa propre `ConfigCombat`**.

### 2.1 Topologie modèle « 1 client → N persos » (probable Hystoria)

```
            [VRAI CLIENT DOFUS (1 instance)]
                       │
                       │ canal MITM '-'/clair
                       ▼
            [SessionProxy unique]
                       │
                       │ paquets parsés
                       ▼
    ┌─────────────────────────────────────────┐
    │   ContexteCompte « hôte » (= leader)    │
    │   - SessionProxy attaché                │
    │   - TrameJeu installée                  │
    │   - Personnage = leader (Sadida)        │
    │   - Combat = ÉTAT PARTAGÉ               │
    └────────────────┬────────────────────────┘
                     │ ref
                     ▼
            [GroupeHeros] ◄── [DetecteurModeHeros]
            │  Membres = [
            │     PersonnageRef(Sadida)  → ContexteCompte hôte
            │     PersonnageRef(Enu1)    → ContexteCompte « fantôme »
            │     PersonnageRef(Enu2)    → ContexteCompte « fantôme »
            │     ...
            │  ]
            │  Combat (shared)
            │  OrdreTours = [idA, idB, idC, …]
            └─────────┬───────────────────────────┐
                      │                           │
                      ▼                           ▼
           [DispatcheurTours]            UI / persistance
              │ (sur GTS<persoX>)        multiaccount/groupe.json
              ▼
       resolve(persoX) → ContexteCompte du perso → ConfigCombat → JouerTourCombatAsync()
```

`ContexteCompte « fantôme »` = un `ContexteCompte` créé pour le perso suiveur **sans `SessionProxy` propre**. Il n'a pas de client réel à piloter : ses envois (GA300, Gt) passent par **le `SessionProxy` du leader** (= le seul client Dofus actif). L'API `Api.LierSession` (`ContexteCompte.cs:198`) accepte déjà la promotion de session a posteriori ; on lui injecte la session leader au moment où le groupe est composé.

### 2.2 Topologie modèle alternatif « N clients liés » (à confirmer)

Si la capture montre N clients Dofus distincts, chacun avec son `SessionProxy` :

```
   [Client 1]──[SessionProxy 1]──[ContexteCompte 1] ◄┐
   [Client 2]──[SessionProxy 2]──[ContexteCompte 2] ◄┤
   …                                                 │  ← GroupeHeros aggrège
   [Client 8]──[SessionProxy 8]──[ContexteCompte 8] ◄┤
                                                     │
                            [GroupeHeros] ◄──────────┘
                            (1 Combat partagé entre tous)
```

Dans ce modèle, le `DispatcheurTours` est **passif** : chaque `TrameJeu` reçoit naturellement son propre GTS et déclenche son `JouerTourCombatAsync`. Le dispatcher sert alors uniquement à **bloquer les tours qui ne sont pas dans ce client** et à propager l'état combat aux autres (UI synchro).

**L'archi prévoit les deux modèles** : `GroupeHeros` reste agnostique sur la topologie réseau, le `DispatcheurTours` a deux modes d'opération (route-via-leader vs broadcast-passif) gouvernés par un flag `EstMonoClient` détecté par `DetecteurModeHeros`.

---

## 3. Nouvelles classes (à créer dans `Divers/MultiAccount/`)

### 3.1 `GroupeHeros.cs`

**Rôle** : agrégat racine de la session multi-héros. Tenu en vie aussi longtemps que les persos sont liés (entre la première détection et la déconnexion totale). Une seule instance vivante à la fois (singleton soft géré par `MainWindow`).

**Responsabilités** :

- détient la liste ordonnée des membres (`PersonnageRef[]`),
- expose un `Combat` partagé (le serveur n'envoie qu'un GTM, on n'en parse qu'un — voir §6 threading),
- maintient l'`OrdreToursCourant` (liste des `IdentifiantCombattant` du tour courant, ordonnée par initiative annoncée par le serveur),
- expose `EstActif`, `MembreActuel`, `PasserAuSuivant()`,
- émet `EvenementTourDePerso` (consommé par `DispatcheurTours`).

Signatures détaillées : voir `docs/MODELE-DONNEES-MODE-HEROS.md` §2.

### 3.2 `DetecteurModeHeros.cs`

**Rôle** : observer le flux paquets pour détecter l'entrée/sortie du mode héros, peupler la composition.

**Stratégie de détection (multi-signaux, défensive en attendant les logs)** :

1. **Signal A — paquet `Im` info-serveur** : sur Hystoria, l'entrée en mode héros déclenche probablement un message texte côté chat système (`Im<code>;<args>`) du type « Vous contrôlez maintenant 8 personnages » ou « Mode héros activé ». **À VALIDER : code Im exact**.
2. **Signal B — `MessageCombattantsAbrak` (GTM)** : si parmi les `Allies` (id > 0), 2+ ont un `Identifiant` qui matche les `Identifiant`s d'autres `Compte`s **chargés en mémoire et en mode héros**, alors le groupe est confirmé. C'est le signal le plus fiable (la composition se lit directement dans le GTM).
3. **Signal C — paquet d'annonce dédié** : si le serveur Hystoria envoie un préfixe spécifique (hypothèses : `GH`, `HG`, `Pg`, `Pl<liste>`), le détecteur en sniffe la première occurrence et la classifie. **À VALIDER**.

**Sortie** : émet `EvenementGroupeForme(membres : PersonnageRef[], leader : PersonnageRef)` ET `EvenementGroupeDissous()`.

**Implémentation graceful** : tant qu'aucun signal n'est confirmé par capture, le détecteur ne fait **rien** (no-op). Le `GroupeHeros` peut être créé **manuellement** depuis l'UI WPF (futur onglet Multi-comptes) pour permettre un mode héros piloté à la main avant validation des signaux serveur.

### 3.3 `DispatcheurTours.cs`

**Rôle** : sur réception de `Combat.TourChange` (event existant `Combats/Combat.cs:48`), résoudre quel perso du groupe joue et déclencher l'IA correspondante.

**Logique** :

```pseudo
OnTourChange(idCombattant):
    if (groupe.EstActif == false) → comportement legacy, rien à faire (le handler legacy de TrameJeu fait déjà le travail)
    membre = groupe.TrouverParId(idCombattant)
    if (membre == null) → ennemi ou allié externe, rien à faire
    if (groupe.EstMonoClient && membre != groupe.Leader):
        // perso fantôme — il faut lui faire jouer son tour VIA le SessionProxy du leader
        await membre.Contexte.Trames.TrameActive.JouerTourCombatAsync(viaSession: groupe.Leader.Contexte.SessionJeuActive)
    else:
        // chaque ContexteCompte a déjà sa propre session — le handler TrameJeu legacy va se déclencher tout seul
        // on se contente d'enregistrer le tour pour la trace + UI
        groupe.NotifierTourJoue(membre)
```

**Le `DispatcheurTours` est attaché au `Combat` partagé du `GroupeHeros`**, pas à un `Combat` individuel. Il s'abonne dans `GroupeHeros.Activer()` et se désabonne dans `Dissoudre()`.

**Bypass du handler legacy** : quand le groupe est actif et qu'un `ContexteCompte` reçoit un `GTS` dont l'`IdentifiantCombattant` **n'est pas le sien**, le handler `TrameJeu.cs:173` continue de filtrer correctement (`return`). Le dispatcher ne remplace donc pas le handler, il s'ajoute en parallèle pour gérer les persos fantômes (modèle 1-client) ou pour la synchronisation UI/traces (modèle N-clients).

---

## 4. Extensions de classes existantes (champs annexes, pas de logique)

### 4.1 `Compte.cs`

Ajout de **3 propriétés annexes** (sérialisables JSON pour `comptes.json` existant) :

- `bool EstDansGroupeHeros { get; set; }` — flag UI (badge dans la liste comptes).
- `string? IdGroupeHeros { get; set; }` — clé du groupe, nullable. Permet à plusieurs groupes coexistants (rare mais possible : 2 comptes Hystoria distincts, chacun avec son groupe héros).
- `RoleDansGroupe RoleDansGroupe { get; set; }` — enum `{ Aucun = 0, Leader = 1, Suiveur = 2 }`.

**Pas de référence directe `GroupeHeros` dans `Compte`** : `Compte` est un POCO sérialisable (cf. `Compte.cs:10` `IEffacable`), il ne doit pas porter d'objet runtime. La résolution `Compte → GroupeHeros` passe par le registre global tenu par `MainWindow` (ou un `GestionnaireGroupes` static future).

### 4.2 `Combat.cs`

Ajout d'un event optionnel pour signaler la fin de tour d'un membre du groupe :

- `event EventHandler<int>? FinTourMembreGroupe;` — émis par `DispatcheurTours` après `Gt` confirmé du membre courant. Sert à `GroupeHeros.PasserAuSuivant()` qui maintient son curseur d'ordre.

Pas d'ajout de champ d'état (l'`OrdreTours` reste sur `GroupeHeros` pour ne pas polluer `Combat`).

### 4.3 `ContexteCompte.cs`

Ajout :

- méthode `void AttacherAuGroupe(GroupeHeros groupe, bool estLeader)` — promeut ce contexte comme membre du groupe et, si **suiveur en mono-client**, réoriente `Api.LierSession` vers la `SessionProxy` du leader (au lieu de la sienne, qui n'existera pas).
- méthode `void DetacherDuGroupe()` — symétrique pour la dissolution.
- propriété `GroupeHeros? Groupe { get; private set; }` — back-ref runtime, **non sérialisée**.

---

## 5. Hooks dans `TrameJeu.cs`

Trois insertions, **toutes en mode observateur (pas de break du flow legacy)** :

| # | Endroit (approx) | Hook | Rôle |
|---|---|---|---|
| H1 | `:43` — `EnregistrerGestionnaires` | `Ecouter<MessageInfoMessage>(msg => DetecteurModeHeros.AnalyserInfo(msg));` | Détection signal A (codes `Im` du mode héros). |
| H2 | `:195` — `OnCombattantsAbrak` (juste après la résync GTM, avant le log de fin) | `DetecteurModeHeros.AnalyserCombattants(msg, _etat, _compte);` | Détection signal B (compo équipe). |
| H3 | `:157` — handler `MessageTourCombatAbrak` (juste après `_etat.Combat.NouveauTour`, avant le check `msg.IdentifiantCombattant != _etat.Personnage.Identifiant`) | `_compte.Groupe?.NotifierTourSeServeur(msg.IdentifiantCombattant, msg.NumeroTour);` | Met à jour l'ordre des tours partagé du groupe. |

> ⚠️ Les positions sont **approximatives** (le fichier va évoluer entre cette ADR et l'implémentation). À l'implémentation, repérer par les **commentaires alentours** plutôt que par numéro de ligne.

**Aucun hook ne modifie le comportement legacy** : si `_compte.Groupe == null` (pas de groupe actif), tous les hooks sont des no-op grâce au `?.`. Le mode mono-perso reste inchangé.

### 5.1 Hook supplémentaire dans `SessionProxy.cs`

Aucun. Le proxy reste **agnostique du mode héros** : il continue de transporter octets et cipher. Toute la logique groupe vit au-dessus, côté trames/contextes.

---

## 6. Cycle de vie

### 6.1 Création

1. Démarrage de l'app, `MainWindow` charge `comptes.json`.
2. Pour chaque compte avec `EstDansGroupeHeros = true` ET `IdGroupeHeros` partagé, **un `GroupeHeros` provisoire** est instancié, encore inactif (état `Inactif`, pas de combat). Sa composition est lue depuis `multiaccount/groupe.json`.
3. L'utilisateur démarre le client Dofus (leader). `ContexteCompte` du leader est créé, `SessionProxy` lié.
4. Quand `OnSelectionPersonnage` (`TrameJeu.cs:316`) est reçu pour le leader, `DetecteurModeHeros` se met en veille active.

### 6.2 Activation

- Premier combat où le serveur engage tous les héros : `OnCombattantsAbrak` (`TrameJeu.cs:195`) reçoit un GTM avec 8 alliés. `DetecteurModeHeros.AnalyserCombattants` reconnaît la compo (signal B) → `EvenementGroupeForme` → le `GroupeHeros` provisoire passe en état `Actif`.
- `ContexteCompte` fantômes (1 par perso non-leader) sont instanciés à la volée, **sans démarrer leur propre `Proxy`**. Leur `Api` est branchée sur la `SessionProxy` du leader via `Api.LierSession(leader.SessionJeuActive)`.
- `Combat` du leader devient le `Combat` partagé du groupe.

### 6.3 Désactivation (fin combat)

- À `MessageFinCombat` (`TrameJeu.cs:89`), `Combat.Reinitialiser()` est appelé. **Le `GroupeHeros` ne se dissout PAS** : il reste actif (les persos sont toujours liés serveur-side), juste son `Combat` reset.

### 6.4 Dissolution

Cas qui dissolvent le groupe :

- Déconnexion du leader (paquet `dx`/perte session) → `OnEtatCompteChange` (`ContexteCompte.cs:368`) avec état `Deconnecte` → `GroupeHeros.Dissoudre()`.
- Action utilisateur explicite (bouton « Dissoudre » dans la future `VueMultiComptes`).
- Détection paquet « mode héros désactivé » (signal serveur — à valider).

À la dissolution : les `ContexteCompte` fantômes sont `Dispose()`-és, leur `Api` débranchée. Le `Compte.EstDansGroupeHeros` reste à `true` pour permettre la reformation auto au prochain démarrage.

---

## 7. Persistance

**Fichier proposé** : `multiaccount/groupe.json` (à la racine projet, à côté de `peleas/`, `banque/`, `config/`).

**Pourquoi pas dans `comptes.json`** : la composition est un agrégat orthogonal à la liste des comptes (un compte peut appartenir à 0 ou 1 groupe), et les groupes peuvent être multiples. Garder le fichier comptes simple.

**Format minimal** (détail dans `docs/MODELE-DONNEES-MODE-HEROS.md` §4) :

```json
{
  "groupes": [
    {
      "id": "grp-beiloddurul",
      "leader": "Beiloddurul",
      "membres": [
        { "identifiant": "Beiloddurul", "role": "Leader" },
        { "identifiant": "Enutrof01",   "role": "Suiveur" },
        ...
      ],
      "creeLe": "2026-05-22T11:30:00Z"
    }
  ]
}
```

**Chargement** : `MainWindow` au démarrage, après `ChargerComptesSauvegardes()`.
**Sauvegarde** : à toute modification (ajout/retrait membre, dissolution explicite).

---

## 8. Threading

### 8.1 État existant (rappel)

- `SessionProxy` parse les paquets sur un **thread réseau dédié** (pool de tasks).
- `TrameJeu` est appelée par `Repartiteur.TraiterPaquet` (`ContexteCompte.cs:584`) qui s'exécute sur ce **même thread réseau**.
- Les events `Combat.*` et `Personnage.Mis_A_Jour` sont donc émis **hors UI thread**. Les vues WPF marshallent via `Dispatcher.BeginInvoke` (cf. `VueMapViewer` ADR-003).
- Chaque `ContexteCompte` a **son propre thread réseau** (proxy distinct).

### 8.2 Modèle multi-thread du `GroupeHeros`

| Composant | Thread d'exécution | Section critique |
|---|---|---|
| `GroupeHeros` (mutation `Membres`, `OrdreTours`) | Thread réseau du leader (modèle mono-client) OU multi-threads (modèle N-clients) | `lock (_verrouEtat)` autour de toute mutation de la liste membres et de l'ordre des tours. |
| `DetecteurModeHeros.AnalyserCombattants` | Thread réseau du `TrameJeu` appelant | Pas de mutation directe : poste vers `GroupeHeros` via méthode synchronisée. |
| `DispatcheurTours.OnTourChange` | Thread réseau qui a émis l'event `Combat.TourChange` | Délègue à `await contexte.Trames.TrameActive.JouerTourCombatAsync()` qui suit le pattern async existant (cf. `TrameJeu.cs:125`). |
| UI (`VueMultiComptes` future, badges sur `LstComptes`) | UI thread, abonnement aux events `GroupeHeros.MembresChanges` via `Dispatcher.BeginInvoke`. |

### 8.3 Races à éviter

1. **GTM concurrent multi-clients** (modèle N-clients) : 8 `OnCombattantsAbrak` parsent le même GTM en parallèle, chacun voulant écrire dans le `Combat` partagé. **Solution** : le `Combat` partagé n'est **écrit que par le leader** ; les fantômes lisent en read-only et signalent leurs events locaux (`PositionsChangees`) via marshalling vers le `Combat` partagé du groupe.
2. **Ordre des tours désync** : 2 `GTS` arrivent presque en même temps sur 2 clients différents (modèle N-clients). **Solution** : sérialisation FIFO dans `GroupeHeros.NotifierTourSeServeur` (numéro de tour + timestamp serveur croissant — premier arrivé wins, les arrivées tardives sont ignorées si même `NumeroTour`).
3. **Dissolution pendant un combat** : `Dissoudre()` appelé alors qu'un membre est en train de jouer son tour. **Solution** : `Dissoudre()` attend la fin du tour courant via `CompletionSource` ; le `Dispose()` des fantômes est différé de 2 s après fin combat.

### 8.4 Mode passif global

`Compte.ModePassif` est respecté **au niveau du `DispatcheurTours`** :
- si **n'importe quel membre** du groupe est en passif → le dispatcher skip toute IA (modèle « si l'un observe, tous observent »),
- alternative configurable : passif uniquement sur les persos individuels (par perso). À trancher à l'implémentation, par défaut **passif global** (plus safe pour les captures protocole).

---

## 9. Conséquences

### 9.1 Ce qui devient possible

- **Le bot pilote 8 persos en combat synchronisé**, chacun avec sa propre `ConfigCombat` SynFus.
- **Stratégies coopératives** : règles SynFus avec focus `AllieLePlusBlesse` qui ciblent réellement un autre membre du groupe (et pas un allié random) — déjà supporté par `MoteurReglesCombat.ChoisirCible` (`MoteurReglesCombat.cs:255`).
- **UI multi-perso unifiée** : un seul `VueCombat` qui affiche les 8 ConfigCombat en onglets, plus un `VueGroupeHeros` qui montre l'ordre des tours et l'état global.
- **Farm groupé** : 1 Sadida (CC/AOE) + 7 Enutrof (Pelle/Maladresse) en synchro → potentiel kamas/PP démultiplié, ciblé par le user.

### 9.2 Ce qui devient compliqué

- **Debug** : 8 décideurs IA s'enchaînent → trace combat × 8. Mitigation : préfixer tous les logs combat par `[GH:<idMembre>]` (ex. `[GH:Enu01]`, `[GH:leader]`).
- **Lifecycle proxy** : on a maintenant des `ContexteCompte` sans `Proxy`. Le code qui assume `Contexte.Proxy != null` (cf. `ActiverEnregistrement`, `Dispose`) doit garder `Proxy` non-null mais marquer un nouveau flag `EstContextePartage` pour skip le démarrage.
- **Tests** : impossible de simuler un mode héros sans logs forensic réels (cf. §1.4 — points à valider). Premier sprint d'implémentation = pur **observation** (signal A/B/C captés, état tracé, aucune action) avant de brancher le dispatcher.

### 9.3 Ce qu'on perd

- **Indépendance combat multi-comptes** : aujourd'hui 2 comptes peuvent être en 2 combats indépendants en parallèle (peu probable mais possible). En mode héros, c'est un seul combat partagé → comportement légèrement différent à expliciter dans l'UI (badge « groupé »).
- **Simplicité** : un niveau d'indirection en plus pour résoudre « quelle config pour ce tour ». Mitigé par le fait que `MoteurReglesCombat.Evaluer` est déjà stateless (reçoit `ConfigCombat` en paramètre) — il suffit de lui passer celle du membre courant.

### 9.4 Risques résiduels

| Risque | Sévérité | Mitigation |
|---|---|---|
| Modèle de connexion mode héros mal deviné (1 vs N clients) | Élevé | Détecteur graceful (no-op si signal pas confirmé) + 2 topologies prêtes. |
| Désync ordre des tours en multi-thread | Moyen | Sérialisation FIFO + tests stress logs forensic. |
| ConfigCombat non chargée pour un fantôme (fichier `peleas/<perso>.json` absent) | Faible | Fallback `ConfigCombat.GenererParDefaut` déjà existant (`ConfigCombat.cs:151`). |
| User active mode héros sans avoir 8 comptes loadés dans `comptes.json` | Faible | UI doit lister les comptes manquants et inviter à les ajouter, sans planter. |

---

## 10. Plan d'implémentation (post-ADR)

Pas dans cette ADR (livrable = docs uniquement). Pour rappel quand le ticket d'implémentation sera ouvert :

1. **Sprint observation** : capturer logs forensic d'un combat mode héros réel sur Hystoria (8 persos engagés, ModePassif=true sur le leader). Lever les 3 incertitudes du §1.4.
2. **Sprint « scaffolding »** : créer les 3 fichiers `Divers/MultiAccount/*.cs` + persistance `multiaccount/groupe.json`. Détecteur en no-op. UI badge dans `LstComptes`.
3. **Sprint « détection »** : implémenter signaux A/B/C en fonction des logs forensic. Activation auto du groupe.
4. **Sprint « dispatcher mono-client »** : si modèle 1-client confirmé, brancher `DispatcheurTours` route-via-leader.
5. **Sprint « UI »** : `VueGroupeHeros` (vue d'ensemble) + intégration onglet `VueCombat` (multi-config).
6. **Sprint « coopératif »** : règles SynFus avancées exploitant les autres membres (heal cross-perso, focus invocation alliée du leader, etc.).
