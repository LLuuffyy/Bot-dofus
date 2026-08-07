# DYSHAY-RELOOK (V2)

> Objectif : trancher définitivement le débat AGENT V1 sur le pattern banque
> dyshay (envoi EMO+ / suppression locale / timing / retry). Source primaire
> relue méthode-par-méthode, citations brutes.

## Sources analysées

Repo `dyshay/Bot-Dofus-Retro` cloné localement dans le worktree :

| Fichier | Chemin | Rôle |
|---|---|---|
| `StoreAllObjectsAction.cs` | `.claude/worktrees/vigorous-murdock/dyshay-source/Otros/Scripts/Acciones/Almacenamiento/StoreAllObjectsAction.cs` | Action « dépose tout » (entry-point banque) |
| `InventoryClass.cs` | `.claude/worktrees/vigorous-murdock/dyshay-source/Otros/Game/Character/Inventory/InventoryClass.cs` | Modèle inventaire client (`ConcurrentDictionary<uint, InventoryObject>`) |
| `CharacterFrame.cs` | `.claude/worktrees/vigorous-murdock/dyshay-source/Comun/Frames/Juego/CharacterFrame.cs` | Handler des paquets serveur incl. `OR` |
| `ScriptManager.cs` | `.claude/worktrees/vigorous-murdock/dyshay-source/Otros/Scripts/ScriptManager.cs` | Enqueue `StoreAllObjectsAction` après `NpcBankAction` |

Aucun fichier `Banco.cs`/`Almacen.cs`/`Bank.cs` distinct — l'état banque
n'est pas modélisé côté client (dyshay traite juste l'inventaire perso, le
coffre est implicite côté serveur).

Version : repo standalone non versionné (csproj `Bot_Dofus_1.29.1`). Pas de
date dans les fichiers, mais structure et namespaces collent à la release
publique de référence.

---

## Q1 — Pattern envoi EMO+

**Verdict : FIRE-AND-FORGET (burst avec délai 300 ms fixe entre envois, AUCUNE attente OR)**

**Preuve** :

```csharp
// StoreAllObjectsAction.cs:25-40
private async Task cleanInventory(Account account,int idcapture = 0, int idCAC = 0)
{
    InventoryClass inventario = account.game.character.inventario;

    foreach (InventoryObject objeto in inventario.objetos)
    {
        if (!objeto.objeto_esta_equipado() && idcapture != objeto.id_modelo && idCAC != objeto.id_modelo)
        {
            account.connexion.SendPacket($"EMO+{objeto.id_inventario}|{objeto.cantidad}");
            inventario.eliminar_Objeto(objeto, 0, false);
            await Task.Delay(300);
        }
    }
}
```

**Analyse** :
- Le `foreach` itère sur `inventario.objetos` (= `_objetos.Values` du
  `ConcurrentDictionary`).
- Pour chaque objet non-équipé/non-capture/non-CAC : `SendPacket` puis
  suppression locale puis `await Task.Delay(300)`.
- **Aucun `await` sur une `TaskCompletionSource` liée à `OR`** — c'est un
  burst pur cadencé à 300 ms. Le serveur reçoit donc 36 EMO+ à 300 ms
  d'intervalle (~10,8 s pour 36 items) sans aucune barrière de
  synchronisation côté client.

---

## Q2 — Suppression locale

**Verdict : OPTIMISTE — juste après envoi EMO+, AVANT l'OR serveur. Le handler OR est utilisé pour d'AUTRES flows (`Od`, drop, casse) mais PAS pour la banque.**

**Preuve 1 — appel dans le burst** :

```csharp
// StoreAllObjectsAction.cs:34-35
account.connexion.SendPacket($"EMO+{objeto.id_inventario}|{objeto.cantidad}");
inventario.eliminar_Objeto(objeto, 0, false);
//                              ^      ^
//                              |      paquete_eliminar = false → ne renvoie PAS Od au serveur
//                              cantidad = 0 → "tout le stack"
```

**Preuve 2 — implémentation `eliminar_Objeto`** :

```csharp
// InventoryClass.cs:80-103
public void eliminar_Objeto(InventoryObject obj, int cantidad, bool paquete_eliminar)
{
    if (obj == null) return;
    cantidad = cantidad == 0 ? obj.cantidad : cantidad > obj.cantidad ? obj.cantidad : cantidad;

    if (obj.cantidad > cantidad)
    {
        InventoryObject nuevo_objeto = obj;
        nuevo_objeto.cantidad -= cantidad;
        _objetos.TryUpdate(obj.id_inventario, nuevo_objeto, obj);
    }
    else
        _objetos.TryRemove(obj.id_inventario, out InventoryObject objeto);    // ← retrait LOCAL IMMÉDIAT

    if (paquete_eliminar)   // ← false dans le flow banque, donc skip
    {
        cuenta.connexion.SendPacket($"Od{obj.id_inventario}|{cantidad}");
        cuenta.Logger.LogInfo("INVENTAIRE", $"{cantidad} {obj.nombre} éliminée(s).");
    }

    inventario_actualizado?.Invoke(true);
}
```

**Preuve 3 — handler OR (côté `CharacterFrame`)** :

```csharp
// CharacterFrame.cs:196-197
[PaqueteAtributo("OR")]
public void get_Eliminar_Objeto(TcpClient cliente, string paquete) =>
    cliente.account.game.character.inventario.eliminar_Objeto(uint.Parse(paquete.Substring(2)), 1, false);
```

**Analyse** :
- Le handler `OR` rappelle `eliminar_Objeto` avec **`cantidad=1`**, ce qui
  pose un problème conceptuel : si le burst a déjà décrémenté localement
  toute la stack, l'OR tente de retirer 1 d'un objet déjà absent →
  `_objetos.TryGetValue` retourne false → silencieux no-op (cf.
  `eliminar_Objeto(uint, int, bool)` ligne 105-111).
- Donc le pattern dyshay est bel et bien : **suppression locale optimiste,
  puis l'OR sert juste de no-op cleanup pour les flows non-banque** (drop
  manuel, casse, etc.).
- L'**AGENT V1 a lu correctement** : décrément local immédiat post-EMO+,
  PAS d'attente OR.

---

## Q3 — Timing

**Verdict : `Task.Delay(300)` fixe entre 2 EMO+, AUCUN timeout OR.**

**Preuve** :

```csharp
// StoreAllObjectsAction.cs:34-36
account.connexion.SendPacket($"EMO+{objeto.id_inventario}|{objeto.cantidad}");
inventario.eliminar_Objeto(objeto, 0, false);
await Task.Delay(300);
```

**Analyse** :
- 300 ms entre chaque envoi, sans variation, sans random, sans backoff.
- Aucun `Task.Delay` post-burst ni avant `EV` (le `EV` n'apparaît même pas
  dans `StoreAllObjectsAction`).
- Délai total prévisible : `nbItems × 300 ms`.

---

## Q4 — Retry / timeout

**Verdict : RIEN. Pas de retry, pas de timeout, pas de log d'erreur, pas de queue de ré-essai.**

**Preuve** : voir Q1 — le foreach n'a aucune branche `if (orPasReçu) ...`.
La méthode retourne `ResultadosAcciones.HECHO` inconditionnellement après
la boucle :

```csharp
// StoreAllObjectsAction.cs:21-22
Task.WaitAll(tasks);
return ResultadosAcciones.HECHO;
```

**Analyse** :
- Si un EMO+ est ignoré par le serveur (item retiré entre-temps, item
  équipé, état non-banque…), dyshay ne s'en rend jamais compte côté client
  — l'item reste en réalité dans l'inventaire serveur, et un prochain
  `agregar_Objetos` ou `modificar_Objetos` le ressuscitera localement (ce
  qui en pratique se produit au prochain check-in).
- En clair : dyshay accepte la perte silencieuse comme normale. Le bot est
  conçu pour des sessions longues où le prochain workflow remet tout
  d'équerre.

---

## Q5 — Vérification post-burst

**Verdict : NON. Aucune re-vérification.**

**Preuve** : le `cleanInventory` retourne dès la fin du `foreach`. Le
callsite (`process`) wait tous les `cleanInventory` (perso + followers)
avec `Task.WaitAll(tasks)`, puis retourne `HECHO` :

```csharp
// StoreAllObjectsAction.cs:8-23
internal override async Task<ResultadosAcciones> process(Account account)
{
    Task[] tasks = new Task[account.hasGroup ? account.group.members.Count + 1 : 1];
    int idCapture = account.script.getCaptureIdAndQuantity().Key;
    int idCAC = account.script.getCACId();
    tasks[0] = cleanInventory(account, idCapture, idCAC);
    if (account.hasGroup && account.isGroupLeader)
    {
        foreach (var follower in account.group.members)
        {
            tasks[account.group.members.IndexOf(follower) + 1] = cleanInventory(follower, idCapture, idCAC);
        }
    }
    Task.WaitAll(tasks);
    return ResultadosAcciones.HECHO;
}
```

**Analyse** :
- Pas de second `foreach` sur les items restants.
- Pas de comparaison snapshot avant/après.
- Pas même de log « X items déposés ».
- C'est cohérent avec le « fire-and-forget » de Q1 et le « pas de retry »
  de Q4 — dyshay est extrêmement minimaliste sur cette action.

---

## Q6 — Serveurs Retro vs privés (Hystoria/Abrak)

**Verdict : aucun commentaire dyshay sur les différences de serveur. Le code cible Dofus Retro 1.29 « générique » — ce qui historiquement = serveur officiel Ankama / Pandala / DofusOnLove.**

Indices indirects :
- Le projet s'appelle `Bot_Dofus_1.29.1` (csproj) — pas de mention
  Hystoria, Abrak, ni serveur privé.
- Aucun branchement conditionnel sur le nom du serveur dans le handler
  `OR` ou le burst.
- Le `SendPacket("EMO+...")` est envoyé en clair via `account.connexion`.
  Dans dyshay, la `Connection` ne semble PAS gérer un canal chiffré '-'
  comme Hystoria. **Différence majeure : Hystoria oblige certains paquets
  à passer par le canal '-' (cipher AK rotatif), tandis que dyshay parle
  en clair.**

**Implications pour Luffy-bot** :
- Sur Hystoria, le cipher '-' introduit une latence asymétrique côté
  serveur : un EMO+ chiffré envoyé tous les 300 ms peut être traité dans
  l'ordre, mais si l'idxProxy se désynchronise, le serveur ignore le
  paquet sans erreur visible côté client. C'est probablement la cause
  réelle des « items perdus » V1 (et pas un défaut du pattern dyshay).
- Sur Hystoria, l'OR sert aussi de **réponse différée** au canal chiffré.
  Les OR arrivent **groupés** une fois que le serveur a tout digéré, pas
  un par un — ce qui peut faire que le compteur global `CompteurObjectRemove`
  Luffy-bot retombe pile à `compteurOrAttendu` à la fin du burst, mais avec
  un décalage cible/source non-fiable.

---

## Correction au rapport V1

> L'AGENT V1 a affirmé : « dyshay supprime optimistement après EMO+ ».

**VRAI.** Lecture confirmée à 100 %.

**Citation exacte** : `StoreAllObjectsAction.cs:34-35` :

```csharp
account.connexion.SendPacket($"EMO+{objeto.id_inventario}|{objeto.cantidad}");
inventario.eliminar_Objeto(objeto, 0, false);
```

L'argument `paquete_eliminar = false` désactive le renvoi d'un `Od` au
serveur (`InventoryClass.cs:96-100`), mais le `TryRemove` côté
`_objetos` (`InventoryClass.cs:94`) se produit bel et bien
**synchronement**, avant le `await Task.Delay(300)`.

L'AGENT V1 a donc lu le code dyshay correctement. Si V1 a échoué (24/36
items « perdus »), la cause n'est PAS une mauvaise interprétation de
dyshay — c'est un problème spécifique à Hystoria (cipher '-' / OR
groupés / état banque côté serveur différent de Retro officiel).

---

## Conclusion

### Pattern dyshay réel — résumé

| Aspect | Comportement |
|---|---|
| Pattern envoi | **Burst fire-and-forget** |
| Délai inter-EMO+ | **300 ms fixe** |
| Suppression locale | **Optimiste, immédiate post-EMO+** |
| Attente OR | **Aucune** |
| Retry / timeout | **Aucun** |
| Re-vérification post-burst | **Aucune** |
| Logging items perdus | **Aucun** |
| Délai total | **~`nbItems × 300 ms`** |
| Robustesse perte | **Reposée sur le prochain workflow** |

### DELTA Luffy-bot V1 vs dyshay réel

Le code actuel `PiloteBanque.cs` ressemble déjà beaucoup à dyshay :
- ✅ Burst à 300 ms (`PiloteBanque.cs:344-366`)
- ✅ Suppression locale optimiste (`PiloteBanque.cs:363`)
- ✅ Pas de retry par item

**Différences (légitimes pour Hystoria)** :
1. Luffy-bot attend `BanqueOuvertureObservee` (ECK5) avant de commencer
   (`PiloteBanque.cs:237-243`) — dyshay ne le fait pas, mais sur Hystoria
   c'est essentiel (ApS injecté = risque de no-op si pas près d'un coffre).
2. Luffy-bot attend 2,5 s post-ECK5 pour stabiliser l'inventaire
   (`PiloteBanque.cs:264`) — dyshay n'a pas ce besoin (pas de loots tardifs
   sur sa boucle).
3. Luffy-bot fait un compteur global OR post-burst
   (`PiloteBanque.cs:374-377`) — dyshay ne fait rien. **Cette feature est
   un FILET, pas une dépendance fonctionnelle.**
4. Luffy-bot a un flag `BloquerEvClient` pour empêcher le vrai client
   Dofus de fermer la banque pendant qu'on dépose — dyshay n'a pas ce
   problème car il est un client autonome.

### Recommandations V2 (sans dévier de dyshay)

| Reco | Détail | Source dyshay ? |
|---|---|---|
| **Garder 300 ms entre EMO+** | C'est le timing dyshay officiel | ✅ confirmé `StoreAllObjectsAction.cs:36` |
| **Garder suppression locale optimiste** | C'est le pattern dyshay | ✅ confirmé `StoreAllObjectsAction.cs:35` |
| **Ne PAS introduire de retry par item** | dyshay accepte la perte | ✅ confirmé absence de retry |
| **Ne PAS attendre OR par item** | C'est ce qui plombe (latence × N) | ✅ confirmé fire-and-forget |
| **Augmenter le délai inter-EMO+ à 400-500 ms si cipher '-' ralentit** | Compense la latence cipher Hystoria absente côté dyshay | ⚠️ extension Hystoria, pas dyshay |
| **Pré-snapshot d'inventaire + re-snapshot post-burst** | Identifier les « vrais perdus » côté serveur, sans re-tenter | ❌ ajout Luffy-bot (dyshay = aucun) |
| **Si re-snapshot révèle items restants, ne PAS les re-envoyer** | Risque de double-dépôt (item déposé serveur, ressuscité par OQ) — préférer un log `[BANQUE-LOST-CONFIRMED]` et continuer | Conservatisme V2 |
| **Vérifier que le canal '-' n'a pas désync entre EMO+ #1 et #36** | C'est la vraie cause probable des 24/36 perdus. Logger `idxProxy` avant/après burst | Spécifique Hystoria |

### Recommandation finale pour le bug 24/36

Le bug N'EST PAS dans le pattern dyshay (V1 l'a copié correctement). C'est
soit :
1. **Désynchronisation cipher '-'** au milieu du burst → serveur ignore
   silencieusement les EMO+ avec idx invalide.
2. **Backpressure TCP** : 36 paquets chiffrés en 11 s saturent le buffer
   serveur Hystoria qui drop sans erreur visible.
3. **État banque côté serveur** : si ApS est envoyé deux fois (client réel +
   bot injecté), le serveur ouvre puis ferme rapidement.

→ Investigation prioritaire : tracer `idxProxy` autour de chaque EMO+ et
comparer aux OR reçus. Si écart, c'est le cipher. Sinon, augmenter le
délai inter-EMO+ à 600 ms et re-tester.
