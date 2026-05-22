# AUDIT — Support multi-compte existant et pré-requis pour le mode héros

> Cible : Luffy-bot (BotDofus.Wpf). Auditeur : revue statique, sans modification de code.
> Objet : cartographier ce qui est déjà câblé multi-compte, identifier les manques et
> proposer des points d'extension pour ajouter le « mode héros » (8 personnages liés
> côté serveur, contrôlés en parallèle par un seul utilisateur).

---

## 1. État du support multi-compte

### 1.1 Modèle objet

Chaque compte est représenté par un duo `Compte` + `ContexteCompte` :

- `Compte` (`Divers/Compte.cs:10`) = simple modèle : identifiant, mot de passe,
  pseudo affiché, `ServeurPrefere`, `PersonnagePrefere`, `ModePassif`,
  `ConfigCombat`, `ConfigBanque`, `WebhookDiscordUrl`. Aucune référence à un autre
  compte, aucune notion de groupe.
- `ContexteCompte` (`Divers/ContexteCompte.cs:22-743`) = « glue » d'UN compte :
  il instancie un `ProxyReseau` (auth) (`ContexteCompte.cs:93`), un `ProxyJeu`
  (créé à l'interception AYK ou en eager via `DemarrerProxyJeuEager()`,
  `ContexteCompte.cs:459`), son `Repartiteur`, sa `GestionnaireTrames`, son
  `EtatJeu`, son `ApiBot`, son `ApiLua`, son `MoteurLuaInteractif`, sa
  `ConfigCombat` chargée depuis `peleas/<identifiant>.json` (`ContexteCompte.cs:98`),
  sa `ConfigBanque` chargée depuis `banque/<identifiant>.json`
  (`ContexteCompte.cs:105`), son `DetecteurStaff` et son `StatsSession`.

  Tout cet état est **par instance** → un `ContexteCompte` par compte = isolation
  logique correcte au niveau données « monde » (carte, perso, combat).

### 1.2 Stockage des comptes

- Liste UI : `ObservableCollection<CompteVm> Comptes`
  (`BotDofus.Wpf/MainWindow.xaml.cs:23`) avec quota affiché `0/200`
  (`MainWindow.xaml.cs:518`, `MainWindow.xaml:109`).
- Persistance : `FichierComptes` → `EntreeCompte` JSON (login, mdp, serveur,
  perso, commentaire) (`MainWindow.xaml.cs:738-750`).
- Import legacy : `accounts.bot` (`MainWindow.xaml.cs:752`).

### 1.3 Cap technique réel : 1 SEUL compte démarrable à la fois

C'est le verrou central. `ConfigReseau` (`Commun/Reseau/ConfigReseau.cs`) code en
DUR :

```csharp
public int PortEcouteLocal      { get; set; } = 1303;   // auth local listener
public int PortEcouteJeuLocal   { get; set; } = 1304;   // jeu  local listener
public int PortSourceMarqueur   { get; set; } = 50303;  // socket sortante auth
public int PortSourceMarqueurJeu { get; set; } = 50304; // socket sortante jeu
```

Quand `MainWindow` instancie un `ContexteCompte` par compte
(`MainWindow.xaml.cs:670`), CHAQUE contexte ouvre son `ProxyReseau` sur le MÊME
`127.0.0.1:1303` et lance ensuite un `ProxyJeu` sur `0.0.0.0:1304` — voir
`ProxyReseau.cs:39` (`new TcpListener(IPAddress.Parse(_config.AdresseEcouteLocale),
_config.PortEcouteLocal)`) et `ContexteCompte.cs:477-485` (`ProxyJeu` configuré
à `PortEcouteJeuLocal`, soit 1304 par défaut).

Conséquence : le second démarrage lève `SocketException` (port déjà utilisé) dans
`DemarrerAsync` (`ProxyReseau.cs:50-58`). Le code UI le sait :

> « On NE démarre PLUS tous les comptes (collision garantie sur port 450). On
> démarre seulement le compte actuellement sélectionné. »  
> — `MainWindow.xaml.cs:120-122` (`BtnDemarrerTous_Click`)

Et au démarrage de l'app, seul `Comptes[0]` est démarré
(`MainWindow.xaml.cs:642-654`).

### 1.4 Modèle d'auth : « MITM-avec-vrai-client »

Le bot **n'authentifie jamais lui-même** côté MITM : c'est le `Dofus.exe` lancé
par l'utilisateur qui fait l'auth, et le proxy se contente d'observer + injecter
en parallèle (`ContexteCompte.cs:203-209`, comment « le bot ne DOIT JAMAIS
installer TrameAuthentification »). Le seul mode où le bot devient client à part
entière, c'est `ClientAutonomeAbrak` (`Commun/Reseau/ClientAutonomeAbrak.cs:26`),
branché à la demande (bouton `Client Auto`, `MainWindow.xaml.cs:179`). Ce client
ouvre SA PROPRE socket vers `51.89.153.20:1303/1304` SANS passer par le proxy
local — donc il échappe au verrou port-collision et permettrait en théorie
plusieurs sessions parallèles (chaque client autonome = socket distinct ;
limitation = serveur Dofus 1.29 limite typiquement à 1 connexion par IP).

### 1.5 Sélection UI

`MainWindow.LstComptes_SelectionChanged` (`MainWindow.xaml.cs:294-313`) bascule
toutes les vues sur le contexte courant via un appel `Lier(ContexteCompte)` :

```
VueDash.Lier(_contexteSelectionne);
VuePersoTab.Lier(_contexteSelectionne);
VueSnif.Lier(_contexteSelectionne);
VueMap.Lier(_contexteSelectionne);
…
VueConfigTab.Lier(_contexteSelectionne);
```

Toutes les vues (`BotDofus.Wpf/Vues/Vue*.xaml.cs`, 11 fichiers) suivent le pattern
« 1 contexte courant » — voir `VueCombat.xaml.cs:50-72` (`Lier(ContexteCompte ctx)`
fait `Detacher()` puis `_contexte = ctx`). Aucune vue n'affiche plusieurs
contextes en même temps.

### 1.6 État partagé entre comptes (global statiques)

Plusieurs singletons / champs statiques traversent **tous les comptes** :

| Statique | Fichier | Multi-compte safe ? |
|----------|---------|---------------------|
| `BaseDonnees.Instance` | `Divers/Donnees/BaseDonnees.cs:18` | Oui (data read-only sauf BDD interactifs, protégée par `_verrouInteractifs`, `:49`). |
| `Journaliseur` (static class) | `Utilitaires/Journaux/Journaliseur.cs:25` | **Non** — tous les logs vont dans le MÊME fichier `botdofus-YYYYMMDD-HHmmss.log`, aucun tag de compte. À 2+ comptes, lignes interleavées. |
| `FabriqueMessages` (registre statique) | `Commun/Messages/FabriqueMessages.cs:17` | Read-only après init, safe. |
| `PiloteBanque.CompteurObjectRemove` | `Divers/Banque/PiloteBanque.cs:37` | **Non** — incrémenté par CHAQUE `TrameJeu.OnObjetRetrait` (`TrameJeu.cs:846`). Un compte qui retire un objet incrémente le compteur attendu par un autre → race condition certaine en multi-banque parallèle. |
| `PiloteBanque.BanqueFermeeObservee` | `Divers/Banque/PiloteBanque.cs:42` | **Non** — `volatile bool` global, mis à true par CHAQUE `EV` reçu (`TrameJeu.cs:70`). Si compte A ferme sa banque pendant que compte B la garde ouverte, B arrête ses dépôts. |
| `RedirecteurWinDivert` | `MainWindow.xaml.cs:38`, `Utilitaires/Reseau/RedirecteurWinDivert.cs` | **Limite forte** : un seul driver actif par process, filtre IP+port codé en dur (`51.89.153.20:1303/1304`). Pas de filtre par compte. |
| `RegexAyk` | `ContexteCompte.cs:83` | Read-only, safe. |

### 1.7 Fichiers data par perso (déjà séparés)

Bonne nouvelle : la config IA combat et la config banque sont **DÉJÀ par
identifiant** (`peleas/<id>.json` et `banque/<id>.json`). On constate la
présence physique :

```
peleas/Zeliox - [III].config
peleas/Zeliox - [IV].config
peleas/Zeliox - [V].config
peleas/Zeliox - [VI].config
peleas/Zeliox - [VII].config
peleas/Toutan-kamou.config
peleas/Pda-one.config … Pda-four.config
banque/Zeliox - [III].json … (5 fichiers + Toutan-kamou)
```

→ L'utilisateur joue **déjà** un mode multi-perso (7 persos Zeliox + Toutan-kamou
= 8, soit la taille canonique d'un groupe héros Dofus). La structure de données
par perso est en place.

---

## 2. Ce qui marche déjà

| Capacité | Statut | Référence |
|----------|--------|-----------|
| Liste de N comptes en UI + persistance JSON | OK | `MainWindow.xaml.cs:23,738`, `Utilitaires/Config/FichierComptes.cs` |
| Isolation `Compte`/`ContexteCompte` (état perso, carte, combat) | OK | `Divers/ContexteCompte.cs:22` |
| Config combat + config banque par perso | OK | `ContexteCompte.cs:98-107`, fichiers `peleas/*.config`, `banque/*.json` |
| IA combat indépendante par contexte | OK | `TrameJeu` instanciée 1× par contexte (`ContexteCompte.cs:224`) |
| Inventaire / personnage / sorts indépendants | OK | `EtatJeu.Personnage` par contexte (`EtatJeu.cs:17`) |
| Switch UI rapide entre comptes | OK | `MainWindow.xaml.cs:294-313` (le `Lier()` debounce ré-attache les events) |
| Mode passif global propagé à tous comptes | OK | `MainWindow.xaml.cs:435-445` (`ChkModePassif_Toggle`) |
| **Sessions MITM réellement parallèles** | **NON** | Conflit port 1303/1304 (`ConfigReseau.cs:47,53`) — voir 1.3 |
| Logs séparés par compte | NON | Journaliseur statique global (`Journaliseur.cs:25`) |
| Inventaire/combat indépendants côté runtime | OK localement, MAIS un seul peut tourner en même temps en MITM | id. |
| Client autonome multi-instance | Théoriquement OK | Chaque `ClientAutonomeAbrak` a sa propre socket (`ClientAutonomeAbrak.cs:46`), AUCUN partage de port local |

**Verdict 2** : l'architecture est *prête à devenir* multi-compte. Le seul vrai
blocage runtime est la couche réseau (ports fixes + driver WinDivert mono-filtre).
Tout le reste (état, IA, UI, config) est déjà par-compte.

---

## 3. Ce qui manque pour le mode héros

Le mode héros Dofus = 8 personnages liés au niveau du COMPTE serveur. Tous
combattent automatiquement ensemble (1 seul placement, 1 seul `GR1`, 1 seul tour
serveur côté joueur, tous les 8 jouent à la suite côté `GTS`). Le serveur envoie
des paquets « Party » (`P*`) pour annoncer membres / leader / position carte.

### 3.1 Sessions parallèles (bloquant)

Voir 1.3. **Sans** modification : impossible de lancer 2 clients MITM
simultanément (port 1303 déjà bind). Le mode héros côté serveur Hystoria
nécessite 1 socket par perso → il FAUT un port par compte.

Solutions envisageables (à arbitrer hors audit) :
- **Ports dynamiques** : `ConfigReseau.PortEcouteLocal = 1303 + idx` (et porter
  le patch jusqu'à `BtnLancerJeu` qui patche `config.xml` pour chaque client).
  Plus le filtre WinDivert qui doit accepter plusieurs ports.
- **Voie « client autonome »** : forcer tous les comptes héros en
  `ClientAutonomeAbrak` (`Commun/Reseau/ClientAutonomeAbrak.cs`) → pas de port
  local à se disputer, juste 8 sockets sortantes 51.89.153.20:1304.

### 3.2 Détection « groupe héros » dans les paquets — manquante

`FabriqueMessages.cs:97-198` enregistre TOUS les parsers connus. **Aucun** parser
pour les paquets de groupe Dofus Retro :

| Paquet manquant | Sens | Sémantique mode héros |
|-----------------|------|------------------------|
| `PR` (Party Request) | S→C | Invitation à rejoindre |
| `PI` (Party Invite) | S→C / C→S | Demande d'invitation |
| `PG` (Party General) | S→C | État membres (PV/PM/PA/cell/map) |
| `PM` (Party Member) | S→C | Ajout/MAJ membre |
| `PL` (Party Leader) | S→C | Désignation du leader |
| `PV` (Party leaVe) | S→C | Membre quitte |
| `PF` (Party Follow) | C→S | Lier le suivi à un membre |

Validation par recherche : `Grep "Prefixe => \"P` retourne 0 résultat dans
`Commun/Messages/VersClient/`. Le `Grep "^\s*\"P[A-Z]\""` dans
`FabriqueMessages.cs` aussi : **aucun parser P*** existant.

Conséquence : le bot ne sait actuellement PAS qu'il est dans un groupe.

### 3.3 Coordination tour-par-tour entre comptes — inexistante

L'IA combat dans `TrameJeu.JouerTourCombatAsync` (`TrameJeu.cs:980+`) ne raisonne
que sur SON `EtatJeu.Combat`. Il y a déjà l'idée d'« allié » dans
`Combat.Allies` (`Divers/Combats/Combat.cs:19`) mais ces alliés sont peuplés à
partir du paquet **`GTM`** (serveur) — donc côté Beiloddurul, je verrais
Toutan-kamou comme un `CombattantAllie` mais sans pouvoir le piloter (juste
l'observer). Aucune méthode `SignalerAuAllie(...)`, aucun bus.

### 3.4 Bus d'événements / mémoire partagée inter-comptes — inexistant

- Pas de singleton `BusInterCompte`, pas d'`event` global d'envergure groupe.
- `MainWindow` ne fait que `foreach (var c in Comptes) { c.Contexte.ModePassif = … }`
  (`MainWindow.xaml.cs:435-444`) — pure broadcast UI → contextes.
- Pas de fichier d'état partagé, pas de pipe nommé.

### 3.5 UI groupée — inexistante

- Pas de `VueGroupe.xaml`.
- Header de `MainWindow` affiche les stats du SEUL contexte sélectionné
  (`MainWindow.xaml.cs:489-519`, `RafraichirStatsHeader()`).
- Liste des comptes (`MainWindow.xaml:243-291`) montre nom + status + perso mais
  pas de regroupement « équipe héros ».
- Pas d'indicateur « qui joue maintenant ».

### 3.6 ApiLua mono-perso

`MoteurLuaInteractif` (`Divers/Scripts/MoteurLuaInteractif.cs`) lié à 1
`ContexteCompte` (`ContexteCompte.cs:116`). Auto-script `scripts/<id>.lua`
(`ContexteCompte.cs:247`). Pas de fonction Lua `bot.attendre_groupe()`,
`bot.dire_groupe(msg)`, `bot.tour_de(perso)`.

---

## 4. Points d'extension recommandés

### 4.1 Détection mode héros — hook précis

Dans `FabriqueMessages.EnregistrerMessagesStandards()`
(`Commun/Messages/FabriqueMessages.cs:97-198`), ajouter les enregistrements `P*`.

Dans `TrameJeu.EnregistrerGestionnaires()` (`Commun/Frames/TrameJeu.cs:43-193`) —
zone juste après les `Ecouter<MessageActeurAbrak>` (`TrameJeu.cs:142-149`),
ajouter :

```csharp
Ecouter<MessagePartyGeneral>(OnPartyGeneral);  // PG → membres + leader + maps
Ecouter<MessagePartyMember>(OnPartyMember);    // PM → MAJ ponctuelle
Ecouter<MessagePartyLeave>(OnPartyLeave);      // PV
```

L'`OnPartyGeneral` doit pousser dans `Compte.GroupeHerosId` (champ à créer sur
`Compte.cs`) la liste des persos co-héros, et déclencher l'enrôlement dans le
`GroupeHeros` global (cf. 4.2).

Point d'attention : **`ContexteCompte.OnPaquetRecu`** (`ContexteCompte.cs:501`)
est l'autoroute par où tout paquet passe AVANT le `Repartiteur` →
implémentations alternatives possibles (parser P* inline ici).

### 4.2 Où poser `GroupeHeros` — proposition concrète

Créer un nouveau dossier `Divers/MultiCompte/` avec :

```
Divers/MultiCompte/
├── GroupeHeros.cs            // agrégat 1..8 ContexteCompte + état partagé
├── RegistreGroupes.cs        // singleton qui mappe groupeId → GroupeHeros
└── BusInterCompte.cs         // event bus typé (in-process, thread-safe)
```

`GroupeHeros` exposerait :

```csharp
public sealed class GroupeHeros : IDisposable
{
    public string Identifiant { get; }                       // ex: "Zeliox-Team"
    public List<ContexteCompte> Membres { get; } = new();
    public ContexteCompte? Leader { get; set; }              // celui qui parle au serveur pour les déplacements groupés
    public Combat? CombatPartage { get; set; }               // référence vers EtatJeu.Combat[leader] OU agrégat dédié
    public BusInterCompte Bus { get; }                       // évents : TourChange, MembreAjoute, Cast, …

    public void AjouterMembre(ContexteCompte ctx);
    public void RetirerMembre(string identifiant);
    public Task SynchroniserPlacementAsync(int cellPourLeader, IReadOnlyDictionary<string,int> cellsParMembre);
    public Task NotifierTourTermineAsync(int idActeur);      // → réveille le prochain membre
}
```

`RegistreGroupes` : singleton statique (similaire à `BaseDonnees.Instance`) avec
`Obtenir(string id)` / `Inscrire(GroupeHeros g)`.

### 4.3 Quel objet partager entre les `ContexteCompte` d'un groupe

Deux options viables :

- **Option A (recommandée) — Référence directe + bus** : chaque `ContexteCompte`
  garde une référence (nullable) `public GroupeHeros? Groupe { get; set; }`. Les
  événements transitent par `Groupe.Bus` (typé, in-process). Pattern publish/
  subscribe asynchrone, pas de polling. Coût : nouveau champ + nouvelle propriété
  sur `ContexteCompte` (~743 lignes → ~750).

- **Option B — Singleton scoped par identifiant de groupe** : `GroupeHeros` 
  observable depuis n'importe quel `ContexteCompte` via `RegistreGroupes`. Plus
  découplé mais cache la dépendance et complique l'arrêt propre (`Dispose`).

L'option A se marie mieux au style existant (`ContexteCompte` est déjà la « glue »
explicite ; cf. `Compte.ConfigCombat` qui est exposé via la même mécanique
`ContexteCompte.cs:99-102`).

### 4.4 IA combat groupée — hook dans le pipeline existant

`JouerTourCombatAsync` (`TrameJeu.cs:980`) prend déjà en compte
`Combat.Allies` (`TrameJeu.cs:1002`). Pour la coordination héros :

- Hook 1 : **avant le moteur de règles** (juste avant `MoteurReglesCombat.Evaluer`
  à `TrameJeu.cs:1079`), poser un check `Groupe?.Bus.PublierMonTourCommence()` →
  permet aux autres contextes de pause leurs propres IA si elles voyaient déjà
  la fin du tour précédent.
- Hook 2 : **après le pass turn** (`TrameJeu.cs:1105` après `await
  _session.EnvoyerAuServeurAsync("Gt")`) → `Groupe?.Bus.PublierMonTourFini()`
  pour réveiller le suivant.

Le serveur Hystoria fait déjà le séquencement (`GTS<id>`), donc en pratique le
bus sert surtout à PARTAGER L'INFO (qui vise quoi, qui buff qui) plutôt qu'à
gating les actions.

### 4.5 UI groupée — où la glisser

- **Liste latérale** (`MainWindow.xaml:243-291`) : ajouter un `Expander` /
  un groupement visuel (un cadre coloré autour des comptes d'un même groupe).
- **Nouvel onglet `VueGroupe.xaml`** : 8 cellules en grille, chacune montrant
  PV/PA/PM + drapeau « tour en cours » + sort actuellement préparé.
- **Header** (`MainWindow.xaml:302-472`) : ajouter une jauge « tour groupe N/8 ».

---

## 5. Risques techniques

### 5.1 Threading / race conditions

- `ContexteCompte.OnPaquetRecu` (`ContexteCompte.cs:501`) tourne sur le thread
  IO du `SessionProxy` (boucles `ReadAsync` issues du proxy). Avec N contextes,
  N threads IO parallèles parlent au Journaliseur statique. Pas de souci pour
  le log (le `lock (_verrouFichier)` couvre l'écriture, `Journaliseur.cs:27`)
  mais les notifications UI doivent passer par `Dispatcher.Invoke` côté Vues.
- `PiloteBanque.CompteurObjectRemove` (`PiloteBanque.cs:37`) `static int`
  partagé : race garantie en multi-compte simultané. À promouvoir en instance
  (déjà rangé par `TrameJeu.cs:846` qui fait `Interlocked.Increment`, mais le
  cible est statique → mauvais accès).
- `Combat.MesInvocationsIds` (`Combat.cs:73`) : HashSet non synchronisé. Modifié
  par `TrameJeu.OnCombattantsAbrak` (thread IO) ET lu par moteur IA (thread
  Task) — déjà actuellement un risque mono-compte, exacerbé en groupe.
- `BusInterCompte` à créer : doit utiliser `ConcurrentDictionary` /
  `Channel<T>` pour ne pas réintroduire de races.

### 5.2 Lifetime

- `ContexteCompte` : créé par `AjouterCompteDepuisEntree` (`MainWindow.xaml.cs:670`),
  détruit par `BtnSupprimerCompte_Click` (`MainWindow.xaml.cs:686`) ou `OnClosed`
  (`MainWindow.xaml.cs:447`). Lifetime aligné UI.
- `GroupeHeros` (à créer) doit être OWNED par `MainWindow` (jamais par un
  contexte — sinon la suppression du leader fait fuir le groupe).
- Hot-add / hot-remove : si un compte rejoint un groupe en cours de combat, le
  `Combat.Allies` du leader est déjà peuplé par `GTM` serveur — bon ; mais il
  faut purger `MesInvocationsIds` (partagé entre tours) à la réintégration.

### 5.3 Persistance

- `ConfigCombat` est déjà par perso : `peleas/<Compte.Identifiant>.json`
  (`ContexteCompte.cs:98`). Confirmé par la présence physique des 8 fichiers
  `peleas/Zeliox - [III..VII].config` + `Toutan-kamou.config` + `Pda-one..four.config`.
  → **Conforme** au modèle multi-perso, RIEN à changer côté config combat.
- `ConfigBanque` idem : `banque/<Compte.Identifiant>.json` (`ContexteCompte.cs:106`)
  + 6 fichiers physiques présents. Conforme.
- À créer : `groupes/<groupeId>.json` pour persister la composition du groupe
  héros (liste des identifiants membres, désignation du leader, stratégie de
  placement collective). Ne pas tenter de réutiliser `peleas/` (un groupe ≠ un
  perso).

### 5.4 Couplage WinDivert / proxy mono-instance

`RedirecteurWinDivert` (`Utilitaires/Reseau/RedirecteurWinDivert.cs:30`) est un
**driver kernel mono-instance**. Le filtre actuel est codé en dur pour
`51.89.153.20:1303/1304`. Multi-compte exigerait soit :

1. un seul driver actif qui redirige toutes les sessions vers leurs ports
   locaux respectifs (filtre par port source client),
2. soit l'abandon de WinDivert au profit de `ClientAutonomeAbrak` pour le mode
   héros (recommandé pour le scope examen).

---

## 6. Observations bonus (bugs / faiblesses repérés en passant)

1. **`PiloteBanque.CompteurObjectRemove` statique** (`Divers/Banque/PiloteBanque.cs:37`)
   et `BanqueFermeeObservee` (`:42`) : doivent devenir instance pour survivre au
   mode héros — mais MÊME en mono-compte c'est un risque dormant si un jour 2
   workflows banque tournent en parallèle. À déplacer dans `ContexteCompte` ou
   `PiloteBanque` (champs instance).
2. **Journaliseur sans tag de compte** (`Journaliseur.cs:25`) : à 2+ comptes,
   les logs sont illisibles. Ajouter un `[contexte]` préfixé via un
   `AsyncLocal<string>` set par chaque `ContexteCompte` au moment de relayer
   les paquets.
3. **`AjouterCompteDepuisEntree` ne vérifie pas la disponibilité du port**
   (`MainWindow.xaml.cs:657-674`) — le `ContexteCompte` est créé même si
   `DemarrerProxy()` va planter. Aujourd'hui c'est gérable (UI résiliente)
   mais en multi-compte il faudrait pré-allouer les ports (1303+idx, 1304+idx)
   et fail-fast si conflit.
4. **`BtnDemarrerTous_Click` mal nommé** (`MainWindow.xaml.cs:118`) — démarre 1
   seul compte malgré le nom. Renommer ou évoluer en vrai démarrage groupé
   quand la couche réseau le supportera.
5. **`OnEtatCompteChange` ne re-câble la TrameJeu QUE si la session jeu est
   présente** (`ContexteCompte.cs:374-378`) — OK pour un compte, mais pour le
   héros il faudra aussi propager le changement d'état au `GroupeHeros`
   (avancement collectif EnJeu→EnCombat).
6. **`Combat.IdentifiantAllie` au singulier** (`Combat.cs:16`) — modèle 1 perso.
   À étendre en `IList<int>` ou à dupliquer par-perso quand un même `Combat`
   représente plusieurs persos co-héros côté UI.

---

## 7. Synthèse

| Axe | Statut | Action nécessaire |
|-----|--------|-------------------|
| Modèle compte / contexte | OK | Aucune |
| Persistance par perso (peleas/banque) | OK | Aucune |
| UI mono-contexte | OK pour le quotidien | Étendre (`VueGroupe`) pour le mode héros |
| Sessions parallèles | BLOQUÉ | Ports dynamiques OU client autonome |
| Parser paquets Party (P*) | ABSENT | Ajouter messages + enregistrement |
| Coordination tour-par-tour | ABSENT | `GroupeHeros` + `BusInterCompte` |
| Logs séparés par compte | ABSENT | Préfixer + (optionnel) fichier par compte |
| WinDivert mono-filtre | LIMITANT | Passage `ClientAutonomeAbrak` recommandé |
| État partagé (statiques) | RISQUÉ | Promouvoir en champs instance (`PiloteBanque`) |
