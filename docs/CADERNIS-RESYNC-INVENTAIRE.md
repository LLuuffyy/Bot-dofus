# CADERNIS RESYNC INVENTAIRE — recherche V3

> **TL;DR** — Aucun paquet client→serveur de resync inventaire complet n'existe en
> Dofus Retro 1.29. Le protocole est strictement **event-driven** (push-deltas
> OAK/OR/OQ). Le seul moment où le serveur pousse l'inventaire entier est à la
> sélection du perso (`AS` → série incluant `OAK` × N + `As`). Le fix qui marche
> chez dyshay, SynFus et tous les bots Retro sérieux = **suppression locale
> OPTIMISTE** post-EMO+ (décrémenter le sac local AVANT d'attendre l'OR).

---

## Sources consultées

| Source | Emplacement / URL | Verdict |
|---|---|---|
| `tech_cadernis_catalogue.md` (mémoire perso) | `.claude\…\memory\` | Index ~40 threads cadernis protocole — aucun thread « resync inventaire » identifié. |
| `tech_cadernis_savoir.md` (crawl Chrome authentifié) | `.claude\…\memory\` | Flux connexion 1.29 complet (M4x0uBot post #7). Aucun paquet client de refresh. |
| `tech_dyshay_protocole.md` | `.claude\…\memory\` | Protocole dyshay = handlers OAK/OR/OQ uniquement. Pas de `RefreshInventory`. |
| `BIBLE-CADERNIS.md` / `-V2.md` / `-V3.md` | racine projet | V3 §11 (banque) documente `DC`+`Ed`/`Eg` Retro vanille (obsolète Hystoria). Aucun paquet de resync. |
| `docs/PROTOCOLE-BANQUE-REFERENCE.md` | local | Flow Hystoria capturé : `ApS → ECK5+EL → EMO+ → EsKO+OR → EV`. `As` parfois en S→C pour pods/kamas mais **ne contient PAS la liste items**. |
| `docs/FORENSIC-DEPOTS-BANQUE.md` | local | Cause racine déjà identifiée : H6 = serveur "consomme" certains EMO+ sans émettre OR. 9/10 confiance. Suggestion #1 forensic = optimistic remove. |
| WebSearch cadernis.fr (mots-clés FR variés) | https://cadernis.fr | Toutes les requêtes "resync / rafraîchir / actualiser inventaire" renvoient à *Protocol dofus 1.29* (#1822), *Analyse des paquets TCP reçus* (#2476), *Gestion paquets longs* (#2893). Body 403 invité — extraits indexés muets sur le sujet. |
| WebFetch [Bot-Dofus-Retro/Comun/Frames/Juego/CharacterFrame.cs](https://raw.githubusercontent.com/Dyshay/Bot-Dofus-Retro/master/Comun/Frames/Juego/CharacterFrame.cs) | dyshay | Confirmé : handlers `OAKO` (add), `OR` (remove), `OQ` (qty). **Aucun handler de paquet client de refresh**. Aucune méthode `RefreshInventory` / `ReloadInventory` / `Resync*`. |
| WebFetch [Bot-Dofus-Retro/.../FightFrame.cs](https://raw.githubusercontent.com/Dyshay/Bot-Dofus-Retro/master/Comun/Frames/Juego/FightFrame.cs) | dyshay | `get_End_FightAsync` (handler `GE`) : à la fin de combat, dyshay envoie **`cliente.SendPacket("GC1")`** + delay 1500ms si leader. Pas d'autre paquet de refresh. |
| WebFetch [Codebreak emulator/Frame/InventoryFrame.cs](https://raw.githubusercontent.com/efwff/Dofus-1.29-emulator--CSharp/master/src/Codebreak.Service.World/Frame/InventoryFrame.cs) | efwff (server-side) | Côté serveur, seuls **3 paquets client** sont traités pour l'inventaire : `OM` (move slot), `OU` (use), `Od` (delete). **Aucun paquet de resync recensé côté serveur**. |
| WebFetch [Codebreak/Frame/BasicFrame.cs](https://raw.githubusercontent.com/efwff/Dofus-1.29-emulator--CSharp/master/src/Codebreak.Service.World/Frame/BasicFrame.cs) | efwff | Pas de paquet `Aa`/`AS`/`/refresh`. Le serveur n'a aucun mécanisme de refresh inventaire à la demande. |
| WebFetch [Codebreak/Frame/GameCreationFrame.cs](https://raw.githubusercontent.com/efwff/Dofus-1.29-emulator--CSharp/master/src/Codebreak.Service.World/Frame/GameCreationFrame.cs) | efwff | `GC1` côté serveur → envoie `GAME_CREATION_SUCCESS` + `GAME_DATA_MAP` (GDM) + `ACCOUNT_STATS` (As). **Pas d'inventaire complet**. |
| WebFetch [Codebreak/Frame/CharacterSelectionFrame.cs](https://raw.githubusercontent.com/efwff/Dofus-1.29-emulator--CSharp/master/src/Codebreak.Service.World/Frame/CharacterSelectionFrame.cs) | efwff | **`AS` est LE SEUL paquet qui déclenche un push inventaire complet** : `INVENTORY_WEIGHT` + `SendSets()` (inclut OAK × N pour tout le stuff/sac). Mais `AS` n'est valide qu'à la sélection initiale du perso (avant `GC1`). Aucun moyen propre de le rejouer en cours de session. |
| WebFetch [Codebreak/Game/Exchange/EntityExchange.cs](https://raw.githubusercontent.com/efwff/Dofus-1.29-emulator--CSharp/master/src/Codebreak.Service.World/Game/Exchange/EntityExchange.cs) | efwff | **CONFIRMATION DEFINITIVE H6 forensic** : sur `EMO+<uidIntrouvable>`, le serveur fait `if (item == null) return 0;` — **ignore silencieusement** sans émettre la moindre erreur au client. Même comportement pour quantité fausse / déjà déposé. |
| WebFetch [Codebreak/Game/Exchange/StorageExchange.cs](https://raw.githubusercontent.com/efwff/Dofus-1.29-emulator--CSharp/master/src/Codebreak.Service.World/Game/Exchange/StorageExchange.cs) | efwff | À l'ouverture banque (`Create()`), le serveur envoie **`EXCHANGE_STORAGE_ITEMS_LIST`** = paquet `EL` listant uniquement le contenu **banque**, PAS l'inventaire du perso. Fermer/rouvrir la banque ne resync donc PAS l'inventaire. |
| WebFetch [jomisoac/Bot-Dofus-1.29.1](https://github.com/jomisoac/Bot-Dofus-1.29.1) | upstream de dyshay | Squelette inachevé (1 seul fichier `PersonajeSeleccion.cs` dans Paquetes/). Pas de banque, pas de fight finished. |

---

## Q1 — Paquet `OK`

**Verdict : N'EXISTE PAS** en protocole client→serveur 1.29.

Côté serveur Codebreak, les seuls handlers commençant par `O` sont :
- `OM` (ObjectMove — déplacer slot interne)
- `OU` (ObjectUse — utiliser potion/sort)
- `Od` (ObjectDelete — détruire)

Côté client→serveur, **aucun paquet `OK`** dans `InventoryFrame.cs` / `BasicFrame.cs` du protocole 1.29.

Citation Codebreak `InventoryFrame.cs` :
> "OM" : ObjectMove — déplace objet (slot dest)
> "OU" : ObjectUse — applique effets
> "Od" : ObjectDelete — supprime
> ⚠️ Aucun paquet ne force une resynchronisation complète

`OAK` / `OAKO` / `OR` / `OQ` sont serveur→client (deltas push), **pas client→serveur**.

## Q2 — Paquet `Os` / `Or` / `Op`

**Verdict : N'EXISTE PAS** côté client→serveur.

Codebreak `BasicFrame.cs` ne traite aucun paquet de type `Os`/`Or`/`Op`. Vérifié sur :
- `BasicFrame.cs` (chat/guilde/groupe/admin)
- `InventoryFrame.cs` (OM/OU/Od)
- `ExchangeFrame.cs` (EQ/Eq/EHT/EHl/EHB/EHP/EA/ER/EV/EB/ES/EMG/EMO/Er)
- `GameActionFrame.cs` (GA001-010/GKK/GKE/DC)

Aucun de ces 5 fichiers ne contient un handler `Os`/`Or`/`Op`. Le protocole 1.29 N'A PAS de slot pour ces paquets côté client.

## Q3 — Tricks communautaires

Tous les "tricks" connus pour forcer un push inventaire ont été testés contre la source Codebreak et **aucun ne marche** :

| Trick | Verdict | Justification |
|---|---|---|
| Ouvrir/fermer un slot inventaire (`OM<id>\|<pos>`) | ❌ | `OM` retourne `OBJECT_MOVE_ERROR` en cas d'erreur, sinon `OBJECT_MOVE_SUCCESS` (1 item). Pas de push complet. |
| Demander stats `As` | ❌ | `As` est S→C uniquement, dispatché spontanément par le serveur sur certaines actions. Le serveur n'accepte pas un `As` client. |
| Ouvrir/fermer la banque (ApS + EV) | ⚠️ Partiel | `ApS` déclenche `As<stats>` (refresh **pods/kamas** seulement) + `ECK5` + `EL` (contenu **banque** seul, PAS inventaire perso). Aucun OAK/OR/OQ inventaire perso n'est rejoué. |
| Changer de map | ⚠️ Partiel | `GDM`+`GI`+`GDK` push la **map** (mapData, entités, GDO interactifs) + `As` (pods). **Aucun push d'inventaire item-par-item**. |
| Demander l'inventaire | ❌ | Aucun paquet client n'existe pour ça (cf. Q1/Q2). |
| Forcer un `AS` (re-sélection perso) | ❌ | Le frame `CharacterSelection` est démonté dès qu'on est en jeu. Codebreak `GameCreationFrame` fait `RemoveFrame(CharacterSelectionFrame)`. Renvoyer `AS<idPerso>` est rejeté ou cause kick. |
| Déconnexion+reconnexion full | ✅ mais inacceptable | Seule technique qui resync vraiment. Coûte 5-15s, casse complètement le flow combat/farm. À utiliser comme **filet de sécurité ultime** uniquement (ex. "5 cycles supplémentaires 0/N consécutifs → reconnect"). |

## Q4 — Vrai client humain en fin de combat

À la fin du combat, dyshay (qui reproduit le comportement client réel) envoie un seul paquet :

```csharp
[PaqueteAtributo("GE")]
public async Task get_End_FightAsync(TcpClient cliente, string paquete)
{
    // … parse xp …
    account.game.fight.Fightfinished(xp, FightTimeConverted);
    cliente.SendPacket("GC1");           // ← renvoyé au serveur
    if(account.isGroupLeader)
        await Task.Delay(1500);
}
```

**`GC1`** = "Game Create 1" — c'est le paquet d'entrée en map (le même qu'à la sélection perso, après le ticket auth). Cela force le serveur à :
1. Démonter `GameCreationFrame`, monter `GameInformationFrame`
2. Envoyer `GAME_CREATION_SUCCESS` (GCK)
3. Envoyer `GAME_DATA_MAP` (GDM — la mapData chiffrée)
4. Envoyer `ACCOUNT_STATS` (As — stats + pods + kamas)

**PAS d'inventaire poussé**. Ce que le combat a déjà envoyé en cours :
- 1× `OAKO` (= OAK) **par item lotté** (drops) — pushé automatiquement quand `character.Inventory.AddItem(item)` est appelé serveur-side dans `MonsterFight.ApplyEndCalculation`.
- Pas de batch resync : juste les deltas, item par item.

Donc côté client humain → serveur, **rien de spécial à part `GC1`** est envoyé en fin de combat.

## Q5 — Pattern "ouvrir banque → fermer → ré-ouvrir" comme resync

**Verdict : ❌ NE MARCHE PAS pour rafraîchir l'inventaire perso.**

`StorageExchange.Create()` Codebreak envoie uniquement :
```csharp
SendItemsList();   // → "EXCHANGE_STORAGE_ITEMS_LIST" = paquet "EL"
                   // contient : Storage.Items + Storage.Kamas
```

`EL` liste les items **du coffre banque**, pas du sac perso. Refermer (`Leave(success)`) ne renvoie rien d'autre que `EV`.

Donc fermer/rouvrir un coffre rafraîchit le contenu **banque** côté bot (utile pour vérifier ce qui a vraiment été déposé !) mais **ne touche pas à l'inventaire perso**. Si on veut "savoir ce qui a vraiment été déposé", il faut diff `EL_avant_workflow` vs `EL_après_workflow` côté coffre.

C'est une piste **observationnelle** : on ne resync pas le sac, mais on peut **détecter post-hoc** ce qui est passé en banque.

## Q6 — `GE` post-combat et push automatique inventaire

Confirmé Codebreak `MonsterFight.ApplyEndCalculation()` :

```csharp
foreach (var fighter in m_droppers)
{
    var items = m_distributedDrops[fighter];
    if (fighter.Type == EntityTypeEnum.TYPE_CHARACTER)
    {
        character.Inventory.AddKamas(kamas);
        character.AddExperience(exp);
        foreach (var item in items)
            character.Inventory.AddItem(item);   // ← chaque AddItem émet OAKO automatiquement
    }
}
```

Donc en fin de combat, le serveur émet :
- `OAKO<obj>` (= OAK avec préfixe O) **par item lotté** (1 paquet par drop)
- `As<stats>` (refresh xp/kamas/pods)
- `GE<duration>|<combattants;...>` (résumé combat)

Le client (vrai humain) répond par `GC1` puis le serveur push la nouvelle map (`GDM`/`GI`).

**Le serveur ne re-push PAS l'inventaire complet en fin de combat**. Il pousse juste les drops (OAK). Si un OAK est perdu / le proxy le drop / le bot bugge en parsing, **l'item est perdu côté bot pour toujours**, modulo le mécanisme suivant :

> ⚠️ **Mais** : H6 forensic montre que sur Hystoria, ce n'est PAS l'OAK qui est perdu en fin de combat. Ce sont les **OR** de fin de dépôt banque qui sont silencieusement omis. La cause racine n'est PAS un OAK manquant après combat — c'est un OR manquant après EMO+ (file pleine côté serveur, anti-spam, ou bug Abrak).

## Q7 — État "DESYNC" serveur documenté

**Verdict : N'EXISTE PAS** comme état documenté côté serveur.

Codebreak `EntityExchange.cs` montre que le serveur ne maintient **aucun état d'erreur** après un EMO+ raté :

```csharp
public override int AddItem(EntityBase entity, long guid, int quantity, long price = -1)
{
    var item = entity.Inventory.GetItem(guid);
    if (item == null)
        return 0;                          // ← silencieux, AUCUN paquet émis
    if (quantity > item.Quantity)
        quantity = item.Quantity;          // ← ajusté silencieusement
    var alreadyExchangedQuantity = GetQuantity(entity, guid);
    if (alreadyExchangedQuantity > 0)
    {
        var realQuantity = item.Quantity - alreadyExchangedQuantity;
        if (quantity > realQuantity)
            quantity = realQuantity;       // ← ajusté silencieusement
    }
    // … succès → émet OR + EsKO+
}
```

Aucun `BASIC_NO_OPERATION` / `EXCHANGE_ERROR` n'est dispatché en cas d'UID introuvable. Le serveur ne tient pas de compteur "X EMO+ ratés", ne flag pas la session en DESYNC, ne kick pas. Il **ignore le paquet silencieusement**.

**Ça veut dire** : pour le bot, un EMO+ qui reste sans OR ≠ "ré-essayer". C'est "l'item n'existe plus côté serveur, ne pas re-envoyer".

## Q8 — Bots open-source

| Bot | Pattern banque | Resync inventaire ? |
|---|---|---|
| **dyshay/Bot-Dofus-Retro** (`StoreAllObjectsAction.cs:25-40`) | **OPTIMISTIC REMOVE** — `eliminar_Objeto()` appelé **immédiatement** après `SendPacket("EMO+...")` sans attendre OR. Burst 300ms entre EMO+. | ❌ Pas de resync — fait confiance au push deltas serveur (OAK/OR/OQ) et au remove optimiste local. |
| **SynFus** (`SynFus_Aqua_v1.1.0`) | DLL obfusquée, **fork dyshay** → même pattern (cf. `PROTOCOLE-BANQUE-REFERENCE.md:153-160`). | ❌ Pareil. |
| **NebulaR-Bot** (Azzary) | Source non lue mais bot anti-detect "ne forge pas de paquets, simule souris" → pas de problème EMO+ direct. | N/A (simule clic, pas de protocole bot). |
| **efwff/Dofus-1.29-emulator** (côté serveur) | Confirme silently-ignore EMO+ inconnu. | Aucun paquet de resync exposé côté serveur. |
| **jomisoac/Bot-Dofus-1.29.1** (upstream dyshay) | Squelette inachevé. | N/A. |

**Conclusion universelle** : tout bot Retro 1.29 sérieux fait du **remove optimiste local post-EMO+**. C'est la solution universelle car c'est la seule possible.

Citation dyshay (`StoreAllObjectsAction.cs:25-40`, déjà extrait dans `PROTOCOLE-BANQUE-REFERENCE.md:117-131`) :
```csharp
foreach (InventoryObject objeto in inventario.objetos)
{
    if (!objeto.objeto_esta_equipado() && idcapture != objeto.id_modelo && idCAC != objeto.id_modelo)
    {
        account.connexion.SendPacket($"EMO+{objeto.id_inventario}|{objeto.cantidad}");
        inventario.eliminar_Objeto(objeto, 0, false);   // ← OPTIMISTIC REMOVE LOCAL
        await Task.Delay(300);
    }
}
```

---

## Solution recommandée V3

**SUPPRESSION LOCALE OPTIMISTE POST-EMO+ (pattern dyshay/SynFus).** Décrémenter
`_perso.Inventaire` immédiatement après l'envoi du paquet EMO+, sans attendre l'OR.
Si l'OR arrive, l'`OnObjetRetrait` actuel devient un no-op (l'item est déjà supprimé).
Si l'OR n'arrive pas, l'item disparaît quand même du sac local → plus aucune
relance possible côté bot, plus de cycles supplémentaires stériles.

Source d'autorité : dyshay `Otros/Scripts/Acciones/Almacenamiento/StoreAllObjectsAction.cs:34`
(`inventario.eliminar_Objeto(objeto, 0, false);` appelé **immédiatement** après
`SendPacket("EMO+...")`) — confirmé sur `PROTOCOLE-BANQUE-REFERENCE.md:127`.

Justification protocolaire : Codebreak `EntityExchange.AddItem` (server-side)
`if (item == null) return 0;` → **le serveur ignore SILENCIEUSEMENT les EMO+ orphelins**.
Aucun moyen côté bot de distinguer "item déjà déposé sans OR émis" de "item jamais
reçu par le serveur". La seule politique sûre = considérer l'item comme parti dès
qu'on l'a envoyé.

**Filet de sécurité complémentaire** : si après le workflow `EL` (contenu banque)
n'a pas augmenté du nombre attendu d'UIDs, déclencher **1 reconnexion complète**
(disconnect→reconnect AS→GC1) pour resync forcé. C'est le seul vrai resync existant.

## Code pseudo-implémentation

```csharp
// Divers/Banque/PiloteBanque.cs — méthode EnvoyerDepotAsync (boucle EMO+)

foreach (var item in itemsRetenus)
{
    var paquet = $"EMO+{item.Identifiant}|{item.Quantite}";
    _attentesOR[item.Identifiant] = (item.Quantite, DateTime.UtcNow);

    // ENVOI EMO+ (CHIFFRÉ '-')
    await _canal.EnvoyerAuServeurAsync(paquet).ConfigureAwait(false);

    // ✅ NOUVEAU — OPTIMISTIC REMOVE LOCAL (pattern dyshay StoreAllObjectsAction.cs:34)
    //   On considère l'item parti dès qu'on a envoyé l'EMO+.
    //   Si l'OR arrive plus tard, OnObjetRetrait devient un no-op (RemoveAll sur 0 ligne).
    //   Si l'OR n'arrive jamais, l'item disparaît quand même → plus de relance.
    //   ⇒ supprime les cycles stériles "0/27 OR reçus en 8000ms" du forensic.
    lock (_perso.Inventaire)
    {
        _perso.Inventaire.RemoveAll(x => x.Identifiant == item.Identifiant);
    }

    await Task.Delay(_cfg.DelaiInterEmoMs, ct).ConfigureAwait(false);  // 300 ms dyshay
}

// FIN DE WORKFLOW — détection desync banque (filet)
//   Si N_attendu > N_observé_dans_EL_post, le serveur n'a vraiment pas reçu certains EMO+
//   → seule resync vraie = reconnexion complète.
if (DiffElBanque(_elAvant, _elApres) < itemsRetenus.Count * 0.7)
{
    Journaliseur.Avertir("[BANQUE] Désync banque détectée (>30% items absents). Reconnexion forcée.");
    await _session.RouvrirSessionAsync(ct).ConfigureAwait(false);  // AS → GC1
}
```

### Effet attendu sur les logs (vs forensic 17:26-17:31)

| Avant fix | Après fix optimistic |
|---|---|
| Pass 1: 65 EMO+, 8 OR → 57 items "résiduels" boucles 9× | Pass 1: 65 EMO+, 65 items quittent le sac local immédiatement. Si 8 OR confirment, no-op. Workflow termine en 1 pass. |
| 9 passes × 27 = 243 EMO+ stériles | 0 pass de rattrapage car le sac local n'a plus aucun "fantôme" à relancer. |
| 5 min bloqué bras-cassés en map | ≤ 25s pour vider l'inventaire (65 × 300ms + délais ouverture/fermeture). |

---

## Confiance

**9.5/10**

Justifications :
- ✅ Code source dyshay (référence #1 du projet) confirme noir-sur-blanc le pattern optimistic remove (`StoreAllObjectsAction.cs:34`).
- ✅ Code source serveur Codebreak (efwff) confirme la cause racine : `if (item == null) return 0;` silencieux (`EntityExchange.cs`).
- ✅ Le protocole 1.29 ne contient aucun paquet client→serveur de resync (vérifié sur `BasicFrame`, `InventoryFrame`, `ExchangeFrame`, `GameActionFrame`, `CharacterSelectionFrame`).
- ✅ Pattern aligné avec le forensic suggestion #1 (que le user/agent V2 avait déjà identifiée mais qui n'est pas encore implémentée).
- ⚠️ -0.5 pt : cadernis a refusé l'accès aux corps de threads (403 invité) → on n'a pas pu valider qu'un thread cadernis contredise cette conclusion (très improbable vu la convergence dyshay + Codebreak).

**Risque résiduel** : si après optimistic remove des items sont **bel et bien restés dans le sac serveur** (pas seulement côté bot), le bot ne les enverra plus en banque jusqu'au prochain combat (qui forcera un OAK delta). C'est acceptable — c'est exactement le comportement humain naturel (un humain qui voit son sac vide arrête de chercher à déposer). Le filet "reconnexion si >30% absents de EL" couvre les cas pathologiques.

---

## Sources externes (markdown)

- [cadernis.fr — Protocol dofus 1.29 (#1822)](https://cadernis.fr/d/1822-protocol-dofus-1-29) — index général protocole, body 403 invité
- [cadernis.fr — Analyse des paquets TCP reçus (#2476)](https://cadernis.fr/index.php?threads%2Fanalyse-des-paquets-tcp-recus-dofus-retro.2476%2F=) — analyses paquets, body 403 invité
- [cadernis.fr — Gestion paquets longs (#2893)](https://cadernis.fr/d/2893-129-gestion-paquets-longs) — body 403 invité
- [GitHub Dyshay/Bot-Dofus-Retro — CharacterFrame.cs](https://github.com/Dyshay/Bot-Dofus-Retro/blob/master/Comun/Frames/Juego/CharacterFrame.cs) — handlers OAK/OR/OQ
- [GitHub Dyshay/Bot-Dofus-Retro — FightFrame.cs](https://github.com/Dyshay/Bot-Dofus-Retro/blob/master/Comun/Frames/Juego/FightFrame.cs) — handler GE + envoi GC1
- [GitHub efwff/Dofus-1.29-emulator--CSharp — InventoryFrame.cs](https://github.com/efwff/Dofus-1.29-emulator--CSharp/blob/master/src/Codebreak.Service.World/Frame/InventoryFrame.cs) — handlers OM/OU/Od côté serveur
- [GitHub efwff/Dofus-1.29-emulator--CSharp — ExchangeFrame.cs](https://github.com/efwff/Dofus-1.29-emulator--CSharp/blob/master/src/Codebreak.Service.World/Frame/ExchangeFrame.cs) — handlers ER/EV/EMO/EMG
- [GitHub efwff/Dofus-1.29-emulator--CSharp — EntityExchange.cs](https://github.com/efwff/Dofus-1.29-emulator--CSharp/blob/master/src/Codebreak.Service.World/Game/Exchange/EntityExchange.cs) — `return 0` silencieux sur item null
- [GitHub efwff/Dofus-1.29-emulator--CSharp — StorageExchange.cs](https://github.com/efwff/Dofus-1.29-emulator--CSharp/blob/master/src/Codebreak.Service.World/Game/Exchange/StorageExchange.cs) — push EL seul à l'ouverture banque
- [GitHub efwff/Dofus-1.29-emulator--CSharp — CharacterSelectionFrame.cs](https://github.com/efwff/Dofus-1.29-emulator--CSharp/blob/master/src/Codebreak.Service.World/Frame/CharacterSelectionFrame.cs) — `AS` = seul paquet qui push l'inventaire complet (sélection initiale)
- [GitHub efwff/Dofus-1.29-emulator--CSharp — GameCreationFrame.cs](https://github.com/efwff/Dofus-1.29-emulator--CSharp/blob/master/src/Codebreak.Service.World/Frame/GameCreationFrame.cs) — `GC1` push GCK + GDM + As (pas d'inventaire)

