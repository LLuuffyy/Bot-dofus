# PATTERN-BANQUE-AUTO — Patterns de référence pour le dépôt banque auto

> **Mission** : extraire les patterns d'implémentation du dépôt banque
> depuis dyshay/Bot-Dofus-Retro (référence open-source), SynFus
> (décompilé) et les BIBLE-CADERNIS, puis proposer une architecture
> alignée avec la capture Hystoria réelle (`docs/ANALYSE-FLOW-BANQUE.md`).
>
> **Sources étudiées** :
> - `worktrees/vigorous-murdock/dyshay-source/Otros/Scripts/**`
> - `worktrees/vigorous-murdock/dyshay-source/Comun/Frames/Juego/**`
> - `worktrees/vigorous-murdock/dyshay-source/Otros/Game/Character/Inventory/**`
> - `BIBLE-CADERNIS.md`, `BIBLE-CADERNIS-V2.md`, `BIBLE-CADERNIS-V3.md`
> - `docs/ANALYSE-FLOW-BANQUE.md` (capture user Hystoria 2026-05-21)
> - SynFus_Aqua_v1.1.0 : DLL obfusquée, namespaces récupérables mais
>   bodies vides → patterns confirmés par dyshay (SynFus en est un fork).

---

## Section 1 — Protocole banque (paquets client/serveur)

### 1.1 Deux variantes du protocole

Il existe **deux flows distincts** côté Dofus Retro 1.29 :

| Variante | Activé par | Source |
|----------|-----------|--------|
| **Dialogue banquier** (avec menu kamas) | `DC<npcId>` → menu `DM`/`DQ` → `DR<reponse>` | dyshay (BIBLE-CADERNIS V3 ligne 453) |
| **Coffre interactif** (gratuit, click direct) | `ApS` | Hystoria capture 2026-05-21 |

→ **Hystoria utilise la variante coffre** (`ApS` direct, pas de PNJ). Le code
dyshay implémente la variante PNJ. **Le bot doit prioriser `ApS`** mais peut
garder un fallback PNJ pour compatibilité Dofus Retro vanille.

### 1.2 Tableau récap C→S / S→C (consolidé)

| Action | Variante PNJ (dyshay) | Variante coffre (Hystoria) | Canal | Réponse serveur |
|--------|------------------------|----------------------------|-------|------------------|
| Repérer la map | (zaap + walk) | (zaap + walk) | — | — |
| Approche NPC/coffre | (déplacement A*) | (déplacement A*) | — | — |
| **Ouvrir** | `DC<npcId>` (puis `DR<q>|<r>`) | **`ApS`** | CLAIR | `DCK<id>` puis `DQ<q>|<reps>` (PNJ) **OU** `As<stats>`+`ECK5`+`EL` (coffre) |
| Confirmer storage opened | (handled by `DV` close-dialog → `ECK`) | (direct `ECK5`) | — | `ECK<kind>` côté serveur |
| **Lister contenu** | `EL<items>` (auto à l'ouverture) | `EL<items>` (auto, vide si aucun) | — | `EL<uid>|<template>|<qte>|<effets>|...` |
| **Déposer 1 item** | `EMO+<uidInv>|<qte>` | **`EMO+<uidInv>|<qte>`** | **CHIFFRÉ '-'** | `EsKO+<uidBanque>|<qte>|<template>|` + `OR<persoId>|<uidInv>` |
| **Retirer 1 item** | `EMO-<uidBanque>|<qte>` (déduit, non capturé) | `EMO-<uidBanque>|<qte>` (à confirmer) | **CHIFFRÉ '-'** | `EsKO-...` + `OAKO<obj>` (probable) |
| **Fermer** | `EV` | `EV` | **CHIFFRÉ '-'** (Hystoria) | `EV` |

### 1.3 Code dyshay — l'ouverture PNJ (référence)

`Otros/Scripts/Acciones/Npcs/NpcBankAction.cs:45` :

```csharp
internal override Task<ResultadosAcciones> process(Account account)
{
    if (account.Is_Busy())
        return resultado_fallado;

    Otros.Mapas.Entidades.Npcs npc = ...; // résolution npcId
    account.connexion.SendPacket("DC" + npc.id, true);
    if (account.hasGroup && account.isGroupLeader)
        foreach (var follower in account.group.members)
            follower.connexion.SendPacket("DC" + npc.id, true);
    return resultado_procesado;
}
```

### 1.4 Code dyshay — le dépôt en masse

`Otros/Scripts/Acciones/Almacenamiento/StoreAllObjectsAction.cs` (script
dyshay original, lignes 1-42) :

```csharp
class StoreAllObjectsAction : ScriptAction
{
    internal override async Task<ResultadosAcciones> process(Account account)
    {
        // ... support group (leader + followers en parallèle)
        await cleanInventory(account, idCapture, idCAC);
        return ResultadosAcciones.HECHO;
    }

    private async Task cleanInventory(Account account, int idcapture, int idCAC)
    {
        InventoryClass inventario = account.game.character.inventario;
        foreach (InventoryObject objeto in inventario.objetos)
        {
            if (!objeto.objeto_esta_equipado()
                && idcapture != objeto.id_modelo
                && idCAC    != objeto.id_modelo)
            {
                account.connexion.SendPacket($"EMO+{objeto.id_inventario}|{objeto.cantidad}");
                inventario.eliminar_Objeto(objeto, 0, false);
                await Task.Delay(300);
            }
        }
    }
}
```

→ Délai **300ms** entre dépôts dans dyshay, contre **1500-3000ms** observés
dans la capture user Hystoria (un humain). Pour passer pour humain on prend
le plus haut.

### 1.5 Code dyshay — fermeture banque

`Otros/Scripts/Acciones/CerrarVentanaAccion.cs` :

```csharp
class CerrarVentanaAccion : ScriptAction
{
    internal override Task<ResultadosAcciones> process(Account account)
    {
        if (account.Is_In_Dialog())
        {
            account.connexion.SendPacket("EV");
            // ... idem pour followers du groupe
            return resultado_procesado;
        }
        return resultado_hecho;
    }
}
```

### 1.6 Handler serveur — events d'ouverture/fermeture

`Comun/Frames/Juego/CharacterFrame.cs` (dyshay) :

```csharp
[PaqueteAtributo("ECK")]                                  // Echange Créé Kind
public void get_Intercambio_Ventana_Abierta(TcpClient cliente, string paquete)
    => cliente.account.AccountState = AccountStates.STORAGE;

[PaqueteAtributo("DV")]                                   // Dialogue closed
public void get_Cerrar_Dialogo(TcpClient cliente, string paquete)
{
    Account cuenta = cliente.account;
    switch (cuenta.AccountState)
    {
        case AccountStates.STORAGE:
            cuenta.game.character.inventario.evento_Almacenamiento_Abierto();
            break;
        // ...
    }
}

[PaqueteAtributo("EV")]                                   // Exchange leaVe
public void get_Ventana_Cerrada(TcpClient cliente, string paquete)
{
    Account cuenta = cliente.account;
    if (cuenta.AccountState == AccountStates.STORAGE)
    {
        cuenta.AccountState = AccountStates.CONNECTED_INACTIVE;
        cuenta.game.character.inventario.evento_Almacenamiento_Cerrado();
    }
}
```

---

## Section 2 — Détection des pods

### 2.1 Le paquet serveur `Ow` (pods update)

`CharacterFrame.cs:72-83` (dyshay) :

```csharp
[PaqueteAtributo("Ow")]
public void get_Actualizacion_Pods(TcpClient cliente, string paquete)
{
    string[] pods = paquete.Substring(2).Split('|');
    short pods_actuales = short.Parse(pods[0]);
    short pods_maximos  = short.Parse(pods[1]);
    CharacterClass personaje = cliente.account.game.character;
    personaje.inventario.pods_actuales = pods_actuales;
    personaje.inventario.pods_maximos  = pods_maximos;
    cliente.account.game.character.evento_Pods_Actualizados();
}
```

**Format** : `Ow<actuel>|<max>`. Ex : `Ow9740|10000` = 97.4% remplis.

### 2.2 Aussi : `As<stats>` (refresh global)

Sur Hystoria, **`As` est renvoyé à chaque action sensible** (cf.
`ANALYSE-FLOW-BANQUE.md`). Bloc 6 du `As` = `pods_actuel,pods_max`.
Sur Dofus vanille, `Ow` est utilisé en parallèle.

→ Le bot doit écouter **les deux** pour avoir un état des pods fiable.

### 2.3 Formule de seuil (dyshay)

`InventoryClass.cs:26` :

```csharp
public int porcentaje_pods => (int)((double)pods_actuales / pods_maximos * 100);
```

`ScriptManager.cs:321-332` (méthode `getMaxPods`) :

```csharp
private bool getMaxPods()
{
    int maxPods = script_manager.get_Global_Or("MAX_PODS", DataType.Number, 90);
    bool isMaxPods = account.game.character.inventario.porcentaje_pods >= maxPods;
    if (account.hasGroup && account.isGroupLeader)
    {
        foreach (var follower in account.group.members)
            isMaxPods = isMaxPods || follower.game.character.inventario.porcentaje_pods >= maxPods;
    }
    return isMaxPods;
}
```

→ **`MAX_PODS = 90`** par défaut, donc déclenchement à 90%.

### 2.4 Mapping projet actuel

Le projet Bot-dofus expose déjà `Personnage.PourcentagePoids` (`Personnage.cs:133`) :

```csharp
public double PourcentagePoids =>
    PoidsMax > 0 ? Math.Clamp(100.0 * PoidsActuel / PoidsMax, 0, 100) : 0;
```

Et un setter sécurisé (`ActualiserPoids` ne remplace pas `PoidsMax`
quand `max==0`, ce qui évite le bug du `Ow<actuel>` partiel). **Déjà OK,
aucune modif nécessaire pour les pods.**

---

## Section 3 — Filtres items (que déposer / que garder)

### 3.1 Catégorisation par byte type (modèle dyshay)

`Otros/Game/Character/Inventory/InventoryUtilities.cs:76-150` :

```csharp
public static InventoryObjectsTypes get_Objetos_Inventario(byte tipo) =>
    tipo switch {
        1 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9 or 10
            or 11 or 16 or 17 or 18 or 19 or 20 or 21 or 22 or 83
                                                  => EQUIPMENTS,
        12 or 13 or 33 or 85 or 86               => MISCELLANEOUS,
        15 or 34 or 35 or 36 or 38 or 41 or 46 or 47 or 48 or 50
            or 51 or 53 or 54 or 55 or 56 or 57 or 58 or 59 or 60
            or 63 or 65 or 68 or 84 or 96 or 98
            or 100 or 103 or 104 or 105 or 106
            or 107 or 108 or 109 or 111          => RESOURCES,
        24                                       => QUEST_ITEMS,
        _                                        => UNKNOWN,
    };
```

Cinq catégories au total :
- `EQUIPMENTS` : armes, anneaux, amulettes, capes, bottes…
- `RESOURCES` : matériaux récoltés (blé, bois, lin, minerais…)
- `MISCELLANEOUS` : potions, parchemins, divers consommables courts
- `QUEST_ITEMS` : items de quête
- `UNKNOWN` : type inconnu

### 3.2 Le champ `"t"` dans `Resources/data/items_merged.json`

Le projet a déjà les types dans `items_merged.json` :

```json
{
  "0": {"t":114, "n":"Tourmenteur de Goutte", ...},   // arme
  "1": {"t":5,   "n":"Bâton Poupinateur", ...},       // bâton
  "2": {"t":3,   "n":"Potion Kraméléhone", ...},      // potion
  "4": {"t":19,  "n":"Mimibiote", ...},               // canne
  "7": {"t":2,   "n":"Arc Plass'Tik'", ...},          // arc
  ...
}
```

`t` = byte type Dofus 1.29 (entre 1 et 250 selon version). On peut donc
**réutiliser la fonction `get_Objetos_Inventario`** côté projet pour
catégoriser chaque item de l'inventaire.

### 3.3 Politique de filtrage de dyshay

`StoreAllObjectsAction.cs` :
- **Skip si équipé** (`objeto_esta_equipado()`)
- **Skip si `id_modelo == idCapture`** (la pierre d'âme courante)
- **Skip si `id_modelo == idCAC`** (l'arme CAC du combat)
- **Tout le reste est déposé**, peu importe la catégorie.

→ Pas de liste blanche/noire d'items dans dyshay : la politique est
« tout sauf l'équipement actif + capture/CAC ». Simple.

### 3.4 Politique de filtrage de SynFus (déduite des UI screenshots)

Les UI SynFus montrent des cases à cocher par catégorie (Resources /
Equipments / Miscellaneous / Quest), plus une whitelist d'IDs à garder
+ une whitelist d'IDs forcés à déposer.

→ Modèle plus puissant que dyshay mais c'est juste de la combinatoire
sur les mêmes catégories.

### 3.5 Modèle data class recommandé

```csharp
public enum CategorieObjet { Equipement, Ressource, Consommable, Quete, Inconnu }

public sealed class FiltreBanque
{
    public bool DeposerEquipements  { get; set; } = false;  // par défaut on garde
    public bool DeposerRessources   { get; set; } = true;
    public bool DeposerConsommables { get; set; } = false;  // pains/potions à garder
    public bool DeposerQuetes       { get; set; } = false;
    public bool DeposerInconnus     { get; set; } = false;

    /// <summary>IDs template forcés à garder même si la catégorie est cochée.</summary>
    public HashSet<int> IdsAGarder { get; set; } = new();

    /// <summary>IDs template forcés à déposer même si la catégorie est décochée.</summary>
    public HashSet<int> IdsADeposerForce { get; set; } = new();

    /// <summary>Nombre min à garder en inventaire par template (ex. pain={312:50}).</summary>
    public Dictionary<int, int> SeuilParTemplate { get; set; } = new();
}
```

---

## Section 4 — Workflow de déclenchement

### 4.1 Pipeline dyshay (du high level au low level)

```
[ScriptManager.cs:306-318]
verifyMaxPods() = porcentaje_pods >= MAX_PODS (90% par défaut)
    ↓
script_state = ScriptState.BANQUE
    ↓
[ScriptManager.cs:354-358]
procesar_Entradas() : ajoute NPCBancoBandera() au lieu des bandera classiques
    ↓
[ScriptManager.cs:403-405]
procesar_Actual_Entrada() : switch → case NPCBancoBandera _: manejar_Npc_Banco_Bandera()
    ↓
[ScriptManager.cs:463-489]
manejar_Npc_Banco_Bandera() :
    actions_manager.enqueue(new NpcBankAction(-1));            // 1. parler au PNJ
    actions_manager.enqueue(new StoreAllObjectsAction());      // 2. tout déposer
    foreach (item à reprendre) actions_manager.enqueue(...);   // 3. retirer si configuré
    actions_manager.enqueue(new CerrarVentanaAccion(), true);  // 4. fermer (EV)
    ↓
[ScriptManager.cs:873]
boucle : tant que script_state == BANQUE && !getMaxPods() = false
    ↓
fin du dépôt : script_state revient à MOUVEMENT/RECOLTE
```

### 4.2 Implémentation actuelle dans Bot-dofus

Le projet a déjà l'orchestrateur dans `ContexteCompte.cs:289-342` :

```csharp
// ----- Trigger banque (poids ≥ seuil) -----
if (ConfigBanque.Active
    && !_banqueDeclenchee
    && perso.PourcentagePoids >= ConfigBanque.SeuilPoidsPct
    && EtatJeu.Combat.Etat == EtatCombat.Inactif)
{
    _banqueDeclenchee = true;
    int? carteAvant = perso.CarteCourante;
    var session = SessionJeuActive;
    _ = Task.Run(async () =>
    {
        bool scriptEnExecution = Scripts.Etat == EtatScript.EnExecution;
        if (scriptEnExecution) Scripts.MettreEnPause();
        var pilote = new PiloteBanque(Api, session, perso, ConfigBanque);
        await pilote.WorkflowCompletAsync(carteAvant);
    });
}
else if (perso.PourcentagePoids < ConfigBanque.CiblePoidsPct)
{
    _banqueDeclenchee = false;  // reset après dépôt
}
```

→ Le pattern hystérésis (`SeuilPoidsPct` ≠ `CiblePoidsPct`) est **plus
intelligent que dyshay** (qui ne déclenche que sur seuil haut). Garder
30% comme cible évite les allers-retours banque.

### 4.3 Sauvegarde de l'état avant interruption

dyshay : pas de sauvegarde explicite, juste un changement de `script_state`.

Bot-dofus actuel : `int? carteAvant = perso.CarteCourante;` capturé avant le
fire-and-forget Task, puis passé à `WorkflowCompletAsync(carteFarmAvant)`
pour le zaap retour. **Bon pattern**.

### 4.4 Reprise après dépôt

dyshay : à la fin du dépôt, `manejar_Npc_Banco_Bandera()` boucle sur le
prochain état et `script_state` redevient `MOUVEMENT`. La boucle de
récolte normale reprend.

Bot-dofus : `WorkflowCompletAsync` peut zaap retour (`RetourFarmApresDepot
&& carteFarmAvant != mapBanque`), mais le script Lua reste **en pause**
(`Scripts.MettreEnPause()` avant, jamais resume) → décision UX :
attente d'input user pour relancer. C'est plus safe que de relancer
auto en cas d'erreur. À garder.

---

## Section 5 — Catégorisation d'items (mapping concret)

### 5.1 Source de vérité côté projet

`Resources/data/items_merged.json` : 1 entrée par templateId, contenant
au moins `"t":<typeByte>`, `"n":"<nom>"`, `"l":<niveau>`, `"g":<gfx>`,
`"w":<weightPods>`.

### 5.2 Helper recommandé

```csharp
public static class CategoriseurObjet
{
    public static CategorieObjet Categoriser(byte typeByte) => typeByte switch
    {
        1 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9 or 10
            or 11 or 16 or 17 or 18 or 19 or 20 or 21 or 22 or 83
                                                 => CategorieObjet.Equipement,
        12 or 13 or 33 or 85 or 86               => CategorieObjet.Consommable,
        15 or 34 or 35 or 36 or 38 or 41 or 46 or 47 or 48 or 50
            or 51 or 53 or 54 or 55 or 56 or 57 or 58 or 59 or 60
            or 63 or 65 or 68 or 84 or 96 or 98
            or 100 or 103 or 104 or 105 or 106
            or 107 or 108 or 109 or 111          => CategorieObjet.Ressource,
        24                                       => CategorieObjet.Quete,
        _                                        => CategorieObjet.Inconnu,
    };
}
```

### 5.3 Détection « est équipé »

Côté projet, `ObjetInventaire.Position` ≠ 63 (NOT_EQUIPPED) = équipé.
Ne **jamais** déposer un objet équipé. Sinon : déséquiper d'abord
(`OM<uid>|63`), attendre l'ack, puis déposer.

Pour le bot v1 : **ignorer les items équipés**, ne déposer que ceux non
équipés. C'est ce que fait dyshay et c'est suffisant.

---

## ARCHITECTURE RECOMMANDÉE POUR LE BOT

### Modifications à `ConfigBanque.cs`

Ajouter aux propriétés existantes :

```csharp
public sealed class ConfigBanque
{
    // ----- existant : SeuilPoidsPct, CiblePoidsPct, MapBanqueId, etc. -----

    /// <summary>Filtres par catégorie d'items.</summary>
    public bool DeposerEquipements  { get; set; } = false;
    public bool DeposerRessources   { get; set; } = true;
    public bool DeposerConsommables { get; set; } = false;
    public bool DeposerQuetes       { get; set; } = false;
    public bool DeposerInconnus     { get; set; } = false;

    /// <summary>IDs template à garder coûte que coûte (ex. arme CAC, pierre d'âme).</summary>
    public HashSet<int> IdsAGarder { get; set; } = new();

    /// <summary>IDs template à déposer même hors catégorie cochée.</summary>
    public HashSet<int> IdsADeposerForce { get; set; } = new();

    /// <summary>Quantité min à garder par template (pains, potions de soin).</summary>
    public Dictionary<int, int> SeuilParTemplate { get; set; } = new();

    /// <summary>Délais inter-dépôts (humanisation, défaut 1500-3000ms).</summary>
    public int DelaiDepotMinMs { get; set; } = 1500;
    public int DelaiDepotMaxMs { get; set; } = 3000;

    /// <summary>Timeout d'attente OR<persoId>|<uid> entre dépôts.</summary>
    public int TimeoutAckDepotMs { get; set; } = 3000;

    /// <summary>Variante protocole : true = coffre interactif (Hystoria), false = PNJ banquier.</summary>
    public bool UtiliserCoffreInteractif { get; set; } = true;
}
```

### Refonte de `PiloteBanque.cs`

```csharp
public sealed class PiloteBanque
{
    private readonly ApiBot _api;
    private readonly SessionProxy _session;
    private readonly Personnage _perso;
    private readonly ConfigBanque _cfg;
    private readonly EtatJeu _etat;
    private readonly BaseDonneesItems _items;  // accès items_merged.json

    public async Task<bool> WorkflowCompletAsync(int? carteFarmAvant, CancellationToken ct = default)
    {
        // 1. zaap vers MapBanqueId
        // 2. pathfinder vers cell coffre/PNJ
        // 3. OuvrirBanqueAsync()  → ApS ou DC<npc>+DR
        // 4. AttendreOuvertureAsync()  → ECK5 reçu
        // 5. DeposerToutAsync()  → boucle EMO+ chiffré + attente OR
        // 6. FermerBanqueAsync()  → EV chiffré + attente EV serveur
        // 7. zaap retour si RetourFarmApresDepot
    }

    private async Task<bool> OuvrirBanqueAsync(CancellationToken ct)
    {
        if (_cfg.UtiliserCoffreInteractif)
        {
            await _session.EnvoyerAuServeurAsync("ApS").ConfigureAwait(false);  // CLAIR
        }
        else
        {
            // Variante PNJ : DC<id> puis attendre DQ puis DR<question>|<reponseBanque>
            // ...
        }
        return await AttendreEvenementAsync(p => p.StartsWith("ECK"), _cfg.TimeoutAckDepotMs, ct).ConfigureAwait(false);
    }

    private async Task<int> DeposerToutAsync(CancellationToken ct)
    {
        var aDeposer = FiltrerInventaire().ToList();
        int deposes = 0;
        foreach (var item in aDeposer)
        {
            if (_perso.PourcentagePoids <= _cfg.CiblePoidsPct) break;

            // ⚠ CANAL CHIFFRÉ '-' obligatoire pour EMO+
            await _session.EnvoyerCipherAsync($"EMO+{item.Identifiant}|{item.Quantite}").ConfigureAwait(false);

            var ackOk = await AttendreObjectRemoveAsync(item.Identifiant, _cfg.TimeoutAckDepotMs, ct).ConfigureAwait(false);
            if (!ackOk) { Journaliseur.Avertir($"[BANQUE] ack OR manqué pour uid={item.Identifiant}"); continue; }

            deposes++;
            await DelaiHumaniseAsync(_cfg.DelaiDepotMinMs, _cfg.DelaiDepotMaxMs, ct).ConfigureAwait(false);
        }
        return deposes;
    }

    private IEnumerable<ObjetInventaire> FiltrerInventaire()
    {
        foreach (var obj in _perso.Inventaire)
        {
            // 1. skip équipé
            if (obj.Position != 63) continue;  // 63 = NOT_EQUIPPED

            // 2. id-keep brut
            if (_cfg.IdsAGarder.Contains(obj.IdTemplate)) continue;

            // 3. force-deposit (court-circuite la catégorie)
            if (_cfg.IdsADeposerForce.Contains(obj.IdTemplate)) { yield return obj; continue; }

            // 4. catégorie
            var typeByte = _items.GetTypeByte(obj.IdTemplate);
            var cat = CategoriseurObjet.Categoriser(typeByte);
            bool catCochee = cat switch
            {
                CategorieObjet.Equipement   => _cfg.DeposerEquipements,
                CategorieObjet.Ressource    => _cfg.DeposerRessources,
                CategorieObjet.Consommable  => _cfg.DeposerConsommables,
                CategorieObjet.Quete        => _cfg.DeposerQuetes,
                CategorieObjet.Inconnu      => _cfg.DeposerInconnus,
                _                           => false,
            };
            if (!catCochee) continue;

            // 5. seuil par template (garder N exemplaires)
            if (_cfg.SeuilParTemplate.TryGetValue(obj.IdTemplate, out int seuil))
            {
                int aDeposer = obj.Quantite - seuil;
                if (aDeposer <= 0) continue;
                // → on sortira un proxy avec Quantite = aDeposer (cf. extension)
            }

            yield return obj;
        }
    }

    private async Task FermerBanqueAsync(CancellationToken ct)
    {
        // ⚠ Sur Hystoria, EV part en chiffré (canal '-')
        await _session.EnvoyerCipherAsync("EV").ConfigureAwait(false);
        await AttendreEvenementAsync(p => p == "EV", 2000, ct).ConfigureAwait(false);
    }
}
```

### Intégration dans `ApiBot` / `SessionProxy`

Ajouter une whitelist combat-like pour la banque :

```csharp
// Dans ApiBot.EnvoyerHumaniseAsync :
if (_etat.Banque.EstOuverte)
{
    // Seuls les paquets banque autorisés
    if (!EstPaquetBanqueAutorise(message)) return;
}

private static bool EstPaquetBanqueAutorise(string p)
    => p.StartsWith("ApS") || p.StartsWith("EMO") || p == "EV"
       || p.StartsWith("GA001") || p == "GKK0";  // permettre le walk
```

### Intégration dans la boucle de farm (`ContexteCompte`)

Déjà OK (`ContexteCompte.cs:289-342`), juste deux ajustements :

1. **Reset `_banqueDeclenchee` côté succès workflow** (en plus de poids < cible) :

```csharp
var pilote = new PiloteBanque(Api, session, perso, ConfigBanque, EtatJeu);
bool ok = await pilote.WorkflowCompletAsync(carteAvant);
if (ok) _banqueDeclenchee = false;  // reset explicite (cible atteinte)
```

2. **Skip le trigger si déjà à la banque** (ne pas re-zaap si l'user
y est déjà) — facultatif :

```csharp
&& perso.CarteCourante != ConfigBanque.MapBanqueId
```

### Persistance config (déjà présente)

`banque/<perso>.json` est déjà géré par `ConfigBanque.Charger`/`Sauvegarder`.
Garder ce pattern, juste ajouter les nouveaux champs (filtres catégorie,
listes garde/dépose, seuils par template).

### UI WPF (onglet Banque, à créer)

Suggéré :
- Top : `[x] Activer | Seuil [90]% → Cible [30]% | Map banque [10117]`
- Section « Catégories à déposer » : 5 checkboxes (Équipements / Ressources
  / Consommables / Quête / Inconnus)
- Section « À garder » : ListBox d'IDs templates avec bouton + / – /
  glissé depuis l'inventaire
- Section « Forcer dépôt » : ListBox idem
- Section « Seuils par template » : DataGrid (templateId, qte min)
- Bouton « Tester dépôt maintenant » (force trigger)

### Tests à faire valider en capture user

1. **Retrait d'items** : `EMO-<uidBanque>|<qte>` non capturé. Faire
   tester l'user en mode passif : ouvrir banque, retirer 1 item,
   logger le paquet observé. → Confirme/infirme le format.
2. **Liste contenu banque** : `EL<format>` quand banque non vide,
   captura `EL<uid>|<template>|<qte>|<effets>|...` ou format custom
   Hystoria.
3. **Réponse banque pleine** : si le banque a un cap (1000 items
   ou autre), capter le paquet d'erreur serveur.

---

## 🎯 RÉSUMÉ DU PATTERN LE PLUS PERTINENT

### Pour Hystoria (priorité absolue) — capture confirmée

```
1. zaap vers map banque (Astrub 10117 par défaut)
2. pathfinder vers le coffre (cell ~395/403/424/433/437, gfx coffre)
3. C→S CLAIR     : ApS                                        ← ouvrir
4. attendre S→C  : ECK5 (+ EL contenu banque)
5. boucle items à déposer :
     C→S CHIFFRÉ : EMO+<uidInv>|<qte>                         ← dépôt
     attendre    : OR<persoId>|<uidInv>                       ← inv synced
     délai       : 1500-3000ms (humanisation)
6. C→S CHIFFRÉ   : EV                                         ← fermer
7. attendre S→C  : EV (confirmation)
8. zaap retour vers map de farm si configuré
```

### Différences avec dyshay (réf code)

| Aspect | dyshay (PNJ classique) | Hystoria (coffre) |
|--------|------------------------|---------------------|
| Ouverture | `DC<npcId>` + `DR<q>|<r>` | `ApS` direct |
| Coût kamas | Oui (50/100k/500k) | **Zéro** |
| Canal dépôt | Clair | **Chiffré '-'** obligatoire |
| Délai inter-dépôt | 300ms | **1500-3000ms** (humanisation) |
| Ack dépôt | `OR<persoId>|<uid>` | `OR<persoId>|<uid>` (idem) |
| Fermeture | `EV` clair | `EV` chiffré |

→ Le canal chiffré et l'humanisation sont les deux différences à NE PAS
oublier sur Hystoria sous peine de kick anti-cheat.
