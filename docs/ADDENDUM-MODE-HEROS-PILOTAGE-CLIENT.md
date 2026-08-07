# Addendum — pilotage des héros liés par le client (Abrak)

**Statut** : correction critique au forensic du 2026-05-22 matin
(`docs/PROTOCOLE-MODE-HEROS-ABRAK.md` et synthèse Phase 1).

## Le forensic précédent était faux

Conclusion erronée : « le serveur joue les héros liés tout seul, le bot
pilote uniquement le master ».

**En réalité** : le client envoie `GA300<sortId>;<cell>`, `GA001<chemin>`,
`Gt` **pour chaque héros dont c'est le tour**, sans préfixe d'id. Le
serveur attribue l'action au combattant courant (identifié par le dernier
`GTS<id>` émis).

## Preuve — log `botdofus-20260522-095820.log`

Combat de 09:59 où le user a piloté manuellement ses 8 persos :

| Temps | Paquet | Sens | Sens du paquet |
|-------|--------|------|----------------|
| 09:59:47 | `GTS401770\|45000\|1` | S→C | Tour master (Beiloddurul) |
| 09:59:49 | `GA001feCheo` | C→S | Master se déplace |
| 09:59:49 | `GA300183;125` | C→S | Master cast Ronce (sort 183) cell 125 |
| 09:59:53 | `GTS401775\|45000\|1` | S→C | Tour Dranariel (Enutrof lié) |
| 10:00:00 | `GA001feqhec` | C→S | Dranariel se déplace → `[GA0] acteur #401775 cell 258` |
| 10:00:02 | `GA30051;125` | C→S | Dranariel cast Lancer de Pièce (sort 51) cell 125 |
| 10:00:05 | `Gt` | C→S | Fin de tour Dranariel |
| 10:00:07 | `GTS401774\|45000\|1` | S→C | Tour Aerawiol |
| 10:00:12 | `GA001feR` | C→S | Aerawiol se déplace → `[GA0] acteur #401774 cell 299` |

Les paquets `GA300`/`GA001` envoyés par le client **n'ont pas de
préfixe d'id**. Le serveur sait à qui les attribuer parce qu'il a
annoncé `GTS<id>` juste avant.

## Pourquoi le forensic du log `081349` se trompait

Dans ce log de capture initiale, le user avait passé les tours des
liés directement avec `Gt` sans envoyer d'action — d'où la conclusion
hâtive « le serveur joue les liés tout seul ». En réalité, le user
n'avait simplement **rien fait** pendant ces tours, et le `Gt` final
relayait juste l'absence d'action.

## Conséquences pour le bot

1. **Le bot peut piloter chaque héros lié** s'il sait quoi faire.
2. **Le serveur attribue automatiquement** les actions au perso dont
   c'est le tour : pas besoin de spécifier d'id, juste envoyer
   `GA300<sortId>;<cell>` et le serveur fait le routage.
3. **Mais** : le bot doit savoir QUELS sorts utilisables par chaque
   héros lié, et avec QUELLE stratégie. Sur le canal réseau, le bot
   ne reçoit que le `SL` du master (`SL192~1~-1;368~1~-1;...`) à la
   connexion. **Les sorts des liés ne sont pas transmis au bot**.

## Plan d'implémentation (à exécuter dans une prochaine session)

### Étape 1 — Sorts par perso lié (configuration manuelle)

Sans signal réseau pour les sorts des liés, l'user doit les renseigner
manuellement. Proposition :

- Fichier `peleas/heros/<idJeu>.json` par perso lié, format identique
  à `peleas/<idMaster>.json` mais en ajoutant `"SortsAppris": {"51": 5, "52": 1, ...}`.
- Pré-rempli par défauts de classe (Enutrof niv 8 → sorts 41, 51, 52, ...).
- Édition dans une UI dédiée (`VueGroupeHeros` étendue).

### Étape 2 — IA combat contextualisée par perso

`TrameJeu.JouerTourCombatAsync` actuellement utilise `_etat.Personnage`
(le master). Refactor :

- Nouvelle signature `JouerTourCombatAsync(int idPersoActif)`.
- Si `idPersoActif == _etat.Personnage.Identifiant` → use config + sorts master.
- Sinon → résoudre `MembreHeros` correspondant, charger sa config + sorts,
  appliquer l'IA avec ce contexte.

### Étape 3 — Hook GTS<idLié>

Actuellement le handler skip si `IdentifiantCombattant != Personnage.Identifiant`
(ligne 173 TrameJeu.cs). Modifier :

```csharp
if (msg.IdentifiantCombattant != _etat.Personnage.Identifiant)
{
    // Si membre du groupe pilotable → jouer son tour.
    var membre = _compte.GroupeHeros?.TrouverParIdJeu(msg.IdentifiantCombattant);
    if (membre != null && membre.ConfigCombat != null)
    {
        await JouerTourCombatPourMembreAsync(membre);
    }
    return;
}
```

### Étape 4 — UI de config par perso

Étendre `VueGroupeHeros` avec un bouton « Configurer ce perso » sur
chaque ligne → ouvre un éditeur de `ConfigCombat` + sorts pour ce
membre. Sauvegarde dans `peleas/heros/<idJeu>.json`.

### Étape 5 — Mode passif par perso

Idée : checkbox « Bot pilote ce perso » par membre du groupe. Si
décochée, le bot envoie juste `Gt` à son tour. Permet à l'user de
choisir quels persos sont bot-pilotés vs main-pilotés.

## Vue actuelle

`VueGroupeHeros` (commit `b173146`) affiche le groupe en LECTURE SEULE
avec les vraies identités (Beiloddurul + 7 Enutrof nommés), badge
état, stats live PV/PA/PM, ordre des tours. Le bandeau bleu en haut
sera mis à jour pour refléter le nouveau verdict (« bot peut piloter
les liés, config requise »).

## Action immédiate (cette session)

Cette doc + mise à jour du bandeau d'info. L'implémentation complète
(étapes 1-5) demande plusieurs heures, ouvre des questions design
(charger les sorts comment, UI plus complexe), et bénéficie d'être
faite en session focused.
