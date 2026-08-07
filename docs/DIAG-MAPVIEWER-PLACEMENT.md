# DIAG — MapViewer : overlays Placement persistants & late-spawn des mobs

**Contexte** : 2 bugs UX signalés par le user sur `VueMapViewer` en combat.
**Statut** : DIAGNOSTIC SEULEMENT — aucune modif appliquée.

---

## § 1. Cause du persiste bleu/rouge en combat

Les cellules bleues (équipe 1) et rouges (équipe 2) dessinées pendant la phase Placement **continuent à s'afficher** pendant `EnCours`.

**Source** : `BotDofus.Wpf/Vues/VueMapViewer.xaml.cs` l.599-618 — méthode `DessinerCellulesPlacement(Carte carte)`.

```csharp
private void DessinerCellulesPlacement(Carte carte)
{
    var combat = _contexte?.EtatJeu.Combat;
    if (combat == null) return;
    if (combat.PositionsEquipe1.Count == 0 && combat.PositionsEquipe2.Count == 0) return;
    // ... boucle dessin ...
}
```

La méthode **ne consulte jamais `combat.Etat`**. Elle se contente de vérifier que les listes `PositionsEquipe1/2` sont non vides.

Or dans `Divers/Combats/Combat.cs` l.78-87 (`DefinirPositionsPlacement`) et l.89-101 (`Reinitialiser`) :
- Les listes sont peuplées sur réception du paquet **`GP`** (`MessagePositionsCombat` → `TrameJeu.OnPositionsCombat` l.323-328).
- Elles ne sont **JAMAIS vidées** entre `Placement` et `EnCours`. Le seul clear se fait dans `Reinitialiser()` à la fin du combat (paquet `GE` `MessageFinCombat`, `TrameJeu.cs` l.80-92).

Côté pipeline `Rafraichir()` (l.258-325), l'appel `DessinerCellulesPlacement(carte)` se fait **inconditionnellement** ligne 302, AVANT le test `enCombat` ligne 309 qui gate `DessinerEntites` / `DessinerCombattants`. Conséquence : pendant `EnCours`, on dessine à la fois les overlays Placement persistants ET les rectangles des combattants → empilement visuel.

---

## § 2. Cause du late-spawn des mobs (apparaissent seulement après GR1)

**Source** : `Commun/Frames/TrameJeu.cs` l.181-267 — `OnCombattantsAbrak` est l'unique endroit où `_etat.Combat.Ennemis` et les `EntiteMonstre` de combat (id < 0) sont peuplés depuis le paquet **`GTM`**.

Or sur Hystoria/Abrak v1.48, le `GTM` autoritatif n'arrive **qu'après que les deux équipes aient envoyé `GR1`** (start de la phase `EnCours`). Pendant la phase Placement, le serveur n'a pas encore broadcast la liste des combattants. Le user dit « les mobs n'apparaissent qu'après mon GR1 » → comportement strictement conforme au protocole.

**Cependant** les `EntiteMonstre` overworld (préfixe GM `~`) sont déjà sur la carte AVANT le combat : `OnMouvementCarte` l.372-399 peuple `carte.Entites` avec les `EntiteMonstre` du groupe agressé. Ces entités survivent au passage en combat — elles ne sont purgées qu'à la fin du combat par le handler `MessageFinCombat` l.88 (`_etat.CarteCourante?.Entites.Clear()`).

Mais aujourd'hui, dans `Rafraichir()` l.309-322, dès que `Combat.Etat != Inactif`, **on saute `DessinerEntites`** (commentaire ADR-003 G.5 : éviter double-marqueur). Du coup pendant Placement, les `EntiteMonstre` du groupe agressé sont silencieusement masqués alors qu'on a leurs positions overworld en mémoire.

→ **Bug confirmé côté client** : on a l'info, on refuse juste de l'afficher.

---

## § 3. Fix proposé (pas appliqué)

### 3.1 — Conditionner `DessinerCellulesPlacement` à `Etat == Placement`

Les overlays bleu/rouge n'ont de sens que pendant la phase de placement. Skipper le rendu dès qu'on passe en `EnCours`.

### 3.2 — Afficher les `EntiteMonstre` de `carte.Entites` pendant Placement

Pendant la phase Placement, `Combat.Ennemis` est vide (GTM pas encore reçu). Mais les `EntiteMonstre` du groupe agressé sont dans `carte.Entites` depuis le GM overworld. On peut donc les dessiner avec `DessinerEntites` (filtré aux monstres pour éviter pollution joueurs/PNJ) tant qu'on est en Placement.

Découpe logique dans `Rafraichir()` :
- `Etat == Inactif` → comportement actuel (DessinerEntites + DessinerJoueur)
- `Etat == Placement` → `DessinerCellulesPlacement` + `DessinerEntites` (mobs only) + `DessinerCombattants` (utile dès que GTM partiel arrive)
- `Etat == EnCours` ou `Termine` → `DessinerCombattants` seul

---

## § 4. Modifications de code précises (à appliquer dans un commit dédié)

### 4.1 — `BotDofus.Wpf/Vues/VueMapViewer.xaml.cs` l.599-603

Gate sur `Etat == Placement` :

```diff
 private void DessinerCellulesPlacement(Carte carte)
 {
     var combat = _contexte?.EtatJeu.Combat;
     if (combat == null) return;
+    // Les overlays bleu/rouge n'ont de sens que pendant la phase Placement.
+    // Dès EnCours, GTM peuple Combat.Allies/Ennemis et DessinerCombattants
+    // prend le relais — les listes PositionsEquipe1/2 ne sont jamais vidées
+    // avant Reinitialiser() (fin de combat) donc on filtre ici.
+    if (combat.Etat != BotDofus.Divers.Combats.Enums.EtatCombat.Placement) return;
     if (combat.PositionsEquipe1.Count == 0 && combat.PositionsEquipe2.Count == 0) return;
```

### 4.2 — `BotDofus.Wpf/Vues/VueMapViewer.xaml.cs` l.309-322 (bloc `Rafraichir`)

Décliner les 3 cas Inactif / Placement / EnCours :

```diff
-    bool enCombat = _contexte.EtatJeu.Combat.Etat != BotDofus.Divers.Combats.Enums.EtatCombat.Inactif;
-    if (!enCombat)
+    var etatCombat = _contexte.EtatJeu.Combat.Etat;
+    if (etatCombat == BotDofus.Divers.Combats.Enums.EtatCombat.Inactif)
     {
         if (ChkAfficherTransitions.IsChecked == true) DessinerTransitions(carte);
         if (ChkAfficherEntites.IsChecked == true) DessinerEntites(carte);
         DessinerJoueur();
     }
+    else if (etatCombat == BotDofus.Divers.Combats.Enums.EtatCombat.Placement)
+    {
+        // Pendant Placement, GTM n'est pas encore reçu → Combat.Ennemis vide.
+        // Mais les EntiteMonstre du groupe agressé sont dans carte.Entites
+        // depuis le GM overworld : on les affiche pour que l'user voie les
+        // mobs DÈS le démarrage du combat (pas après GR1).
+        DessinerEntitesMonstresUniquement(carte);
+        DessinerCombattants(carte); // au cas où un GTM partiel arriverait
+    }
     else
     {
-        // En combat, seuls les combattants vivants sont affichés (moi en
-        // bleu, alliés vert, ennemis rouge). Pas de marqueur overworld
-        // figé. Cf. F.5 commit d9ea637 + ADR-003.
+        // EnCours / Termine : Combat.Allies/Ennemis autoritatifs via GTM.
         DessinerCombattants(carte);
     }
```

### 4.3 — Nouvelle méthode `DessinerEntitesMonstresUniquement` (à ajouter après `DessinerEntites` l.717)

Variante filtrée de `DessinerEntites` qui ne dessine QUE les `EntiteMonstre` (laisse de côté joueurs/PNJ qui pourraient parasiter la phase Placement) :

```csharp
private void DessinerEntitesMonstresUniquement(Carte carte)
{
    foreach (var ent in System.Linq.Enumerable.ToList(carte.Entites.Values))
    {
        if (ent is not EntiteMonstre) continue;
        var cell = carte.Obtenir(ent.CellulePosition);
        if (cell == null) continue;
        var (cx, cy) = ProjeterIso(cell.X, cell.Y);
        var ellipse = new Ellipse
        {
            Width = 16, Height = 16,
            Fill = new SolidColorBrush(Color.FromRgb(0x9E, 0x2B, 0x2B)),
            Stroke = Brushes.White,
            StrokeThickness = 1.5,
            Tag = ent,
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(ellipse, cx - 8);
        Canvas.SetTop(ellipse, cy + _hauteurCellule - 8);
        Canvas.SetZIndex(ellipse, 340);
        CanvasMap.Children.Add(ellipse);
    }
}
```

### 4.4 — Refresh sur transition Placement → EnCours

`Rafraichir` est déjà branché sur `Combat.EtatChange` (l.71 unsubscribe → événement existant). Vérifier que `OnCombatEtatChange` déclenche bien `Dispatcher.BeginInvoke(Rafraichir)` (l.89/91/93 utilisent ce pattern) — si oui, aucune modif supplémentaire requise, les overlays disparaîtront automatiquement au switch d'état.

---

## § 5. Notes d'impact

- **Aucune régression réseau** : ces 3 modifs sont purement rendering WPF, aucun paquet envoyé ne change.
- **Combat.Etat saute parfois directement Inactif → EnCours** dans `TrameJeu.cs` l.73-79 (handler `MessageDebutCombat` appelle `Demarrer()` puis immédiatement `PassageEnCombat()`). Donc dans les combats Abrak où GS arrive avant tout GP, on n'aura jamais d'overlays Placement visibles — comportement OK (mobs visibles via `EntiteMonstre` overworld pendant ce laps).
- **Compteur `_dernierNbVivants` reset à -1 dans MessageFinCombat** (l.83-84) — pas impacté.
- Garder `DessinerEntites` intact (utilisé hors combat avec checkbox `ChkAfficherEntites`).
