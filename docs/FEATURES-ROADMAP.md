# Features roadmap — Luffy-bot vs concurrents

## ✅ Déjà implémenté (parité ou supérieur à SynFus/dyshay/BotOFus)

| Feature | Détail |
|---------|--------|
| Pathfinder A* PriorityQueue | 5× plus rapide que List linéaire (audit C8) |
| Pathfinder combat 4-dir strict | Matche dyshay `pelea no utiliza diagonales` |
| 12 Focus (vs 6 dyshay) | Invocations / Allié variantes / EnnemiLePlusLoin / CelluleAdjacenteEnnemi |
| 18 conditions par règle SynFus | Distance/Cible/Joueur/Situation/Avancé (cooldown, max/cible) |
| Multi-cast par tour drain PA | Boucle évaluation jusqu'à plus de PA/règles |
| Pré-mouvement Mode-aware | Agressif rapproche / Eloigne recule / Equilibre ajuste |
| Post-cast kiting | dyshay `get_Fin_Turno` Eloigne/Fuyard recule après cast |
| GTM resync position autoritative | Plus de désync optimist |
| Garde-fou anti-ban (portée + LOS) | Refus catégorique d'envoyer GA300 hors range/LOS |
| Tracking invocations Sadida par cell | Plus de "bot tape ses propres invocs" |
| Soin auto consommable | OU<uid> si PV < seuil |
| Codes échec GAF mappés | Log explicite (hors portée, LOS, PA, cooldown) |
| MapViewer mode combat + highlights | Combattants vivants + ma cell bleue + cells portée orange |
| Animation flash cast | Polygon rouge fade-out 400ms |
| UI Combat complète + presets Sadida/Cra/Auto | Mode SynFus, 7+ règles d'un clic |
| Auto-save 800ms + Charger config JSON | Indicateur visuel ✓/⚠ |
| Tests unitaires xUnit | 13 tests, non-régression Pathfinder 4-dir |

## 🚧 En cours / partiellement fait

| Feature | Détail | Effort restant |
|---------|--------|----------------|
| Dépôt banque auto | `ConfigBanque` créée, MOTEUR à implémenter | ~2-3h |
| Notifications Discord | `NotificateurDiscord` créé, INTÉGRATION events à faire | ~1h |
| Stats live (XP/h, kamas/h, combats/h) | StatsSession enrichi, **UI à créer** | ~30min |

## 🎯 À ajouter — Priorité haute

| Feature | Pourquoi | Effort |
|---------|----------|--------|
| **Anti-stuck** (timeout 60s sans changement de carte) | Critique pour farm de nuit | 1h |
| **Mode pause si joueur visible** | SAFETY ban (autre joueur ≠ moi sur la map) | 2h |
| **Boost potions auto** (XP, Sagesse, Pods) | Cap d'XP/kamas/h | 1h |
| **Auto-relance après mort** | Reprendre farm direct | 1h |
| **Sauvegarde stats CSV/JSON exportable** | Reporting | 30min |

## 🌟 À ajouter — Priorité moyenne (différenciateur)

| Feature | Pourquoi |
|---------|----------|
| Système macros/combos sorts | Chaîne précise pour PvP (ex. Tacle → Sort → Pousser) |
| Quêtes auto (chaîne PNJ) | Dialogue auto + objectifs |
| Auto-équipement (set switch) | Set farm / combat / drop |
| Multi-compte synchro | Bot leader + suiveurs |
| Détection événements (Imps spawn) | Trigger script alternatif |
| Mode farm spécifique (Bworker, Donjon) | Templates dédiés |
| Anti-tacle intelligent (cooldown Map Frappe) | Skip mob tacleur |
| Compteur loot (drop rate) | Stats par mob, rare drops |

## 🔭 À ajouter — Long terme (innovation)

| Feature | Pourquoi |
|---------|----------|
| Machine Learning prédiction loot | Estimer XP/kamas/h selon route |
| Mode multi-bot orchestré (raid donjon) | Synchroniser 8 comptes |
| Plugin marketplace | UI custom plugins user |
| Replay combat (rejouer ligne par ligne) | Debug avancé |
| Discord bot commandes (start/stop/status) | Contrôle à distance |
| Web dashboard temps réel | Stats multi-comptes |
| Détection automatique de classe | Adapter rotation auto |
| Apprentissage rotation depuis logs | Améliorer DPS auto |

---

## 📐 Plan d'implémentation Dépôt banque (Phase A)

### Protocole Dofus 1.29 banque (à valider en capture)

| Action | Paquet client | Réponse serveur |
|--------|--------------|-----------------|
| Ouvrir banque | `EBM` ou interactif PNJ `GA500<cell>;<skill>` | `ELS<liste-items>` (storage list) |
| Déposer item | `EM<uid>;<quantite>;1` | `OS<uid>` (object stored) |
| Retirer item | `EM<uid>;<quantite>;0` | `OA<uid>;<qte>` (object added) |
| Fermer banque | `EV` (exchange validate / leave) | `EV` echo |

### Pipeline pseudo-code

```
À chaque OS reçu (poids change) :
  if (perso.PourcentagePoids >= cfg.SeuilPoidsPct) {
    if (notif Discord activé) → NotifierBanquePleineAsync
    sauvegarder map courante (pour retour farm)
    Pause script Lua actif
    SeRendreAuxBanqueAsync(cfg.MapBanqueId)  // pathfinding inter-maps
    OuvrirBanqueAsync(cfg.GfxNpcBanquier)
    foreach item in inventaire:
      if (item.IdTemplate in cfg.ItemsAGarder) continue
      if (cfg.ItemsADeposer.Count > 0 && item.IdTemplate not in cfg.ItemsADeposer) continue
      DeposerItemAsync(item.Identifiant, item.Quantite)
      attendre 200-500ms (humanisation)
    FermerBanqueAsync()
    if (cfg.RetourFarmApresDepot) {
      RetournerCarteAsync(carteFarmAvant)
      Reprendre script Lua
    }
  }
```

### Fichiers à créer (futures sessions)

- `Divers/Banque/PiloteBanque.cs` : pilote async (ouverture, dépôt, fermeture)
- `Divers/Banque/Trajets/BanquePathFinder.cs` : pathfinding overworld vers Astrub bank
- `BotDofus.Wpf/Vues/VueBanque.xaml(.cs)` : UI config (seuil/cible/items à déposer/garder)
- `Resources/data/banques.json` : positions banques par serveur (Astrub, Bonta, Brakmar, etc.)

### Risques

- Le serveur Hystoria peut avoir un format propriétaire différent de Retro standard.
- Le pathfinding inter-maps nécessite la `worldmap` complète (déjà chargée dans `Resources/worldmap/`).
- Tacle pendant le trajet bank → combat → reprendre après.

### Tests prévus

1. Unit : `ConfigBanque.Charger/Sauvegarder` round-trip JSON.
2. Intégration : ouverture banque + dépôt 1 item factice (mock serveur).
3. E2E : full trajet farm → bank → retour avec sauvegarde position.

---

## 📐 Plan Notifications Discord (Phase B — facile)

Déjà créé : `NotificateurDiscord.NotifierAsync(url, msg)`.

À intégrer côté TrameJeu :
- Sur message `As` (paquet stats) → si niveau > précédent → `NotifierLevelUpAsync`
- Sur message `GE` (fin combat) → si PV = 0 → `NotifierMortAsync`
- Sur trigger banque (Phase A) → `NotifierBanquePleineAsync`
- Sur exception réseau → `NotifierDeconnexionAsync`

UI : ajouter un input « Discord Webhook URL » dans VueConfig.

---

## 📐 Plan Anti-stuck (Phase C — rapide)

```csharp
private DateTime _dernierChangementMap;
private int _dernierMapId;

// Dans OnCarteRecue / OnMouvementCarte handler :
if (carte.Identifiant != _dernierMapId)
{
    _dernierMapId = carte.Identifiant;
    _dernierChangementMap = DateTime.UtcNow;
}

// Timer 30s tick :
if ((DateTime.UtcNow - _dernierChangementMap).TotalMinutes > 5
    && _etat.Combat.Etat == EtatCombat.Inactif)
{
    Journaliseur.Avertir("[ANTI-STUCK] 5 minutes sans changement de map hors combat → reset script");
    NotificateurDiscord.NotifierErreurCritiqueAsync(...);
    Scripts.Stop();
    Scripts.Restart();
}
```

---

## 📐 Plan Boost potions auto (Phase D — simple)

`ConfigBoosts` :
```csharp
public class ConfigBoosts {
    public bool Actif;
    public int ItemBoostXpId;       // ex. Pot. d'XP
    public int IntervalleMinutes;   // re-boost tous les X min
    public bool BoostSagesse;
    public bool BoostPods;
}
```

Timer : toutes les `IntervalleMinutes`, si item dans inventaire → `OU<uid>`.
