# ADR-003 — MapViewer en mode combat : superposition entités/combattants, highlights tactiques

- Statut : Proposé
- Date : 2026-05-20
- Auteurs : System Architecture Designer (assist. Claude)
- Lié à : F.5 (`d9ea637` — hooks `PositionsChangees/TourChange/EtatChange` sur la MapViewer), CLAUDE.md roadmap « UI WPF onglet Combat », ADR-001 §6 (UI VueCombat), ADR-002 §4 (pipeline déplacement combat)
- Fichiers : `BotDofus.Wpf/Vues/VueMapViewer.xaml.cs` (1400 l.), `Divers/Combats/Combat.cs`, `Commun/Frames/TrameJeu.cs:178-264` (OnCombattantsAbrak)

## 1. État actuel post-F.5 — ce qui marche, ce qui manque

### 1.1 Câblage hooks (correct post-F.5, vérifié)

`VueMapViewer.Lier(contexte)` lignes 80-84 abonne désormais :

```csharp
combat.PositionsChangees += OnCombatChange;   // GTM serveur → cellules à jour
combat.TourChange       += OnCombatTourChange; // GTS → highlight tour potentiel
combat.EtatChange       += OnCombatEtatChange; // Inactif↔Placement↔EnCours↔Termine
```

Et les handlers (l. 88-93) re-postent `Rafraichir()` via `Dispatcher.BeginInvoke` (thread-safe — events viennent du thread réseau). **OK**.

`Rafraichir()` l. 307 appelle bien `DessinerCombattants(carte)` si `Combat.Etat != Inactif`. **OK**.

### 1.2 `DessinerCombattants` est correct (l. 645-702)

- itère `combat.Allies` et `combat.Ennemis` (les bonnes listes — alimentées par `OnCombattantsAbrak` `TrameJeu.cs:201-221`)
- skip `cmb.EstMort`
- bleu épais pour moi (`combat.IdentifiantAllie`), vert pour alliés, rouge pour ennemis
- z-index 410 (au-dessus des cellules de placement 360 et des entités 340)
- affiche le `%PV` au-dessus si non plein
- **Cellule lookup correct** : `carte.Obtenir(cmb.CellulePosition)` retourne la cellule isométrique réelle

### 1.3 BUG racine — `DessinerEntites` continue à dessiner pendant le combat

`Rafraichir()` l. 303-308 :

```csharp
if (ChkAfficherEntites.IsChecked == true) DessinerEntites(carte);   // ← inchangé pendant combat
if (_contexte.EtatJeu.Combat.Etat != BotDofus.Divers.Combats.Enums.EtatCombat.Inactif)
    DessinerCombattants(carte);
```

`DessinerEntites` (l. 704-736) itère `carte.Entites.Values`. Or `OnCombattantsAbrak` (TrameJeu.cs:227-247) **REMPLIT `carte.Entites`** avec les combattants en plus de remplir `Combat.Allies/Ennemis` :

```csharp
// TrameJeu.cs l. 234-245
if (c.Id < 0)
    carte.Entites[c.Id] = new EntiteMonstre { ... };
else
    carte.Entites[c.Id] = new EntiteJoueur { ... };
```

**Conséquence visuelle (screenshots 220806/220830)** :
- pastilles vertes (`EntiteJoueur`) et rouges sombres (`EntiteMonstre`) dessinées par `DessinerEntites` à z=340
- rectangles arrondis colorés dessinés par `DessinerCombattants` à z=410 par-dessus
- **superposition** : deux marqueurs sur la même case (pastille + rectangle), z-index masque partiellement le rectangle car la pastille a un Stroke blanc 1.5px visible en débordement
- en plus, le panneau droit (`MettreAJourListeEntites` l. 773-833) liste les monstres avec leur cellule overworld figée s'il en restait avant combat → confusion totale

### 1.4 Bug secondaire — `MettreAJourListeEntites` reste en mode overworld

L. 773-833 itère `carte.Entites` sans tenir compte de `Combat.Etat`. En combat, le panneau « Monstres » affiche les `EntiteMonstre` injectées par GTM (nom = `"Monstre {c.Id}"` brut), pas les `CombattantMonstre` avec `NiveauGabarit`/PV/PA/PM/etc.

### 1.5 Bug tertiaire — overlays placement à plat sous les blocs surélevés

`DessinerCellulesPlacement` (l. 586-605) → `DessinerOverlayCellule` (l. 607-614) dessine un polygone z=360 SANS appliquer la `TranslateTransform(0, -elev)` que `DessinerGrille` (l. 490-491) applique aux cases grises surélevées. Sur les cellules bord de combat qui sont surélevées (rares mais existent), les overlays bleus/rouges « flottent » au niveau du sol et sont masqués par la falaise (faceG/faceD z=prof+100). **Cosmétique mais visible sur certaines maps**.

### 1.6 Ce qui fonctionne bien (à préserver)

- `OnPaquet` (l. 99-129) filtre les paquets `GT*` (GTM/GTS) pour déclencher `Rafraichir()` → MapViewer est bien réactive aux paquets serveur
- `DessinerCellulesPlacement` overlay bleu/rouge sur cellules de placement (équipe 1 / équipe 2) — visible sur les screenshots 220806/220830
- `Combat.IdentifiantAllie` correctement injecté par `OnCombattantsAbrak` l. 182 → `idMoi` valide dans `DessinerCombattants`

## 2. Causes possibles si les combattants n'apparaissent toujours pas

Hiérarchie des hypothèses, du plus probable au moins probable (à valider par log au prochain combat) :

| # | Hypothèse | Comment vérifier | Probabilité |
|---|-----------|-----------------|--------|
| H1 | `Combat.Allies`/`Ennemis` peuplés mais cachés par `DessinerEntites` overlay (z-fight + pastille débord blanc) | Décocher temporairement la case `ChkAfficherEntites` pendant combat → si rectangles apparaissent, c'est H1 | **TRÈS PROBABLE** |
| H2 | `carte.Obtenir(cmb.CellulePosition)` retourne null (cellule combat > Cellules.Length, ou cellule 0 par défaut sur CombattantAllie naïf) | `Journaliseur.Debogue($"[MAP] dessiner combattant #{cmb.Identifiant} cell={cmb.CellulePosition}")` à l. 654 | possible : `IdentifiantAllie=0` au début, fix l. 182 mais avant 1er GTM |
| H3 | `_contexte.EtatJeu.Combat.Etat == Inactif` au moment du Rafraichir (race condition GTM avant GS) | `TxtMapId.Text` ajout `etat={Combat.Etat}` | peu probable — `OnCombattantsAbrak` ne change pas Etat, c'est `MessageDebutCombat` qui le fait avant |
| H4 | `Rafraichir()` lève une exception silencieuse avant `DessinerCombattants` (ex: `info != null && cellPerso != null` sur cellPerso null) | wrapper try/catch + Journaliseur | possible si compte fraîchement connecté |
| H5 | Combattants placés mais cellule = 0 (avant GTM complet) → `carte.Obtenir(0)` null → skip silencieux | log dans DessinerOne | observé : capture user 16:21 montre des `Cellule=0` pour les invocations naissantes |

**Pour le screenshot 220806/220830 spécifiquement** : la grille montre clairement les cellules de placement bleues/rouges (équipes en placement, **avant que GTM n'arrive**). C'est la phase `Placement` AVANT le 1er GTM. Donc à ce moment-là `Combat.Allies` et `Combat.Ennemis` SONT VIDES — normal qu'on ne voie pas de combattants. Le bug rapporté pourrait n'être visible **qu'en phase Placement** ; en phase EnCours après le 1er GTM, les rectangles apparaîtraient — mais masqués par les pastilles overworld (H1).

**Action de diagnostic à faire AVANT toute refonte** : ajouter 2 logs dans `Rafraichir()` et `DessinerCombattants`, puis re-tester un combat. Si confirmé H1 → §3-§4. Si H2/H5 → §3 mais corriger en plus le parser GTM.

## 3. Architecture refonte mode combat — 3 modes mutuellement exclusifs

### 3.1 Énumération de mode d'affichage MapViewer

```csharp
// Privé à VueMapViewer.xaml.cs — calculé à chaque Rafraichir() depuis Combat.Etat
private enum ModeAffichageMap
{
    Overworld,       // Combat.Etat == Inactif         → entités, ressources, transitions
    CombatPlacement, // Combat.Etat == Placement       → cellules bleues/rouges, pas d'entités, perso
    CombatEnCours    // Combat.Etat == EnCours/Termine → combattants, highlights tactiques
}

private ModeAffichageMap _modeActuel = ModeAffichageMap.Overworld;
```

Mapping :

| `Combat.Etat` | `ModeAffichageMap` | Dessine entités | Dessine combattants | Dessine grille placement |
|---------------|---------------------|------------------|----------------------|----------------------------|
| Inactif | Overworld | OUI (`carte.Entites`) | NON | NON |
| Placement | CombatPlacement | NON | si liste non vide | OUI (bleu/rouge) |
| EnCours | CombatEnCours | NON | OUI | NON (déjà placés) |
| Termine | Overworld (transient) | OUI | NON | NON |

### 3.2 Pipeline `Rafraichir()` refondu

```
[GTM] [GTS] [EtatChange]
    ↓
Rafraichir() :
    1. ModeAffichageMap mode = CalculerMode(Combat.Etat)
    2. DessinerGrille(carte)                       — toujours
    3. switch (mode) :
       - Overworld         : DessinerTransitions, DessinerEntites, DessinerJoueur
       - CombatPlacement   : DessinerCellulesPlacement, DessinerCombattants (si liste non vide),
                             DessinerHighlightCellulePerso
       - CombatEnCours     : DessinerCombattants,
                             DessinerHighlightTour (qui joue maintenant),
                             DessinerHighlightCellulesAttaquables (sort sélectionné),
                             DessinerHighlightCheminPropose (si IA a calculé)
    4. MettreAJourListeEntitesSelonMode(mode)
    5. CentrerSiNecessaire(carte)
```

### 3.3 Listage panneau droit selon mode

```csharp
private void MettreAJourListeEntitesSelonMode(ModeAffichageMap mode)
{
    Monstres.Clear(); Pnjs.Clear(); Joueurs.Clear();
    Ressources.Clear(); Sorties.Clear();

    switch (mode)
    {
        case ModeAffichageMap.Overworld:
            // comportement actuel (l. 781-820)
            break;
        case ModeAffichageMap.CombatPlacement:
        case ModeAffichageMap.CombatEnCours:
            // Liste les COMBATTANTS, pas les entités carte
            foreach (var allie in Combat.Allies)
                Joueurs.Add($"{allie.Nom} [cell {allie.CellulePosition}] PV {allie.PV}/{allie.PVMax}");
            foreach (var en in Combat.Ennemis)
                Monstres.Add($"{en.Nom} [cell {en.CellulePosition}] PV {en.PV}/{en.PVMax}");
            // Ressources/Sorties restent listées (utile pour rappel contextuel)
            break;
    }
    // ... compteurs UI ...
}
```

## 4. Modifications de code prêtes à appliquer

### 4.1 Fix critique — skip `DessinerEntites` en combat (`Rafraichir`, l. 299-312)

```csharp
CanvasMap.Children.Clear();
_cellulesPolygons.Clear();
DessinerGrille(carte);

var combat = _contexte.EtatJeu.Combat;
bool enCombat = combat.Etat != BotDofus.Divers.Combats.Enums.EtatCombat.Inactif
             && combat.Etat != BotDofus.Divers.Combats.Enums.EtatCombat.Termine;

if (enCombat)
{
    DessinerCellulesPlacement(carte);              // bleus/rouges si Placement
    DessinerCombattants(carte);                    // toujours en combat
    DessinerHighlightCellulesAttaquables(carte);   // §5
    DessinerHighlightCheminPropose(carte);         // §5
    DessinerHighlightTour(carte);                  // qui joue maintenant
    // PAS DessinerEntites — exit overworld
    // PAS DessinerTransitions — pas pertinent en combat
    // PAS DessinerJoueur — DessinerCombattants gère mon perso (bleu épais)
}
else
{
    if (ChkAfficherTransitions.IsChecked == true) DessinerTransitions(carte);
    if (ChkAfficherEntites.IsChecked == true) DessinerEntites(carte);
    DessinerJoueur();
}

MettreAJourListeEntitesSelonMode(enCombat ? ... : ModeAffichageMap.Overworld);
CentrerSiNecessaire(carte);
```

### 4.2 Purge `carte.Entites` à l'entrée combat (`TrameJeu.cs:70-76` MessageDebutCombat)

**Décision** : NE PAS purger `carte.Entites` car `OnCombattantsAbrak` (l. 230-247) injecte intentionnellement les combattants dedans pour que `Carte.SignalerRechargee()` propage à d'autres vues legacy (VueCombat ancienne, ListeMonstresVue). Le fix §4.1 suffit : on n'AFFICHE plus carte.Entites en mode combat, peu importe son contenu.

**Variante prudente** : on pourrait scinder `carte.Entites` (overworld) de `carte.Combattants` (combat), mais c'est un refactor cross-module non justifié pour ce ticket. Reporté à plus tard.

### 4.3 Hook fin combat — restaurer overworld (`TrameJeu.cs:77-89` MessageFinCombat)

Déjà OK l. 85 : `_etat.CarteCourante?.Entites.Clear();` puis `SignalerRechargee()`. À l'entrée d'un nouveau `MD`/`GM` overworld, les entités seront repopulées. **Aucune modification requise**.

### 4.4 Surélévation overlays placement (`DessinerOverlayCellule` l. 607-614)

```csharp
private void DessinerOverlayCellule(Cellule cell, Color fill, Color stroke)
{
    var poly = CreerPolygoneCellule(cell, new SolidColorBrush(fill), 1.5);
    poly.Stroke = new SolidColorBrush(stroke);
    poly.IsHitTestVisible = false;
    // Si cellule surélevée (obstacle/non-marchable surélevé +20px en DessinerGrille),
    // suivre la translation pour rester sur le DESSUS du plateau, pas dans la falaise.
    bool blocCell = cell.Type != TypesCellule.Transition
                    && (!cell.EstMarchable
                        || cell.Type == TypesCellule.Obstacle
                        || cell.Type == TypesCellule.LignDeVueSeule);
    if (blocCell)
        poly.RenderTransform = new System.Windows.Media.TranslateTransform(0, -20);
    Canvas.SetZIndex(poly, 360);
    CanvasMap.Children.Add(poly);
}
```

Identique pour `DessinerCombattants` (rectangles) : surélever si cellule de placement = bloc.

## 5. Highlights cellules attaquables + chemin proposé

### 5.1 Cellules attaquables (sort sélectionné)

**Source** : `_sortSelectionne` (nouveau champ — id sort actif dans l'UI VueCombat, à propager via event Compte). Si null, pas de highlight.

**Algo** :
```
DessinerHighlightCellulesAttaquables(carte) :
    if _sortSelectionne == null OR maCell == null : return
    var sort = BaseSorts.Obtenir(_sortSelectionne)
    var stats = sort.Stats(perso.NiveauSort(sortId))   // résoudre par niveau

    foreach cellule de la carte :
        d = DistanceChebyshev(maCell, cellule)
        if d < stats.PorteeMin OR d > stats.PorteeMax : continue
        if stats.LigneSeule AND not EnLigne(maCell, cellule) : continue
        if stats.NecessiteLOS AND not LdvBresenham(carte, maCell, cellule, combat.Combattants) : continue
        DessinerOverlayCellule(cellule, Color.FromArgb(120, 255, 200, 80), Color.FromRgb(220, 160, 30))
```

**Coût** : 560 cellules × tests rapides (Chebyshev) — ~50 µs. LDV Bresenham seulement sur celles déjà dans portée → ~30-50 cellules → 200 µs/sort. Acceptable.

**Couleur** : orange semi-transparent (#FFC850 alpha 120) ; rouge plus saturé pour les cellules qui contiennent un ennemi.

### 5.2 Chemin de déplacement proposé (jaune)

**Source** : `DecideurCombat` (ou `MoteurReglesCombat` Phase 1) calcule désormais un `IList<int> CheminPropose` qu'il expose via un event `Combat.CheminProposeChange` (nouveau, équivalent à `MouvementBotConfirme` mais en pré-envoi).

**Algo** :
```
DessinerHighlightCheminPropose(carte) :
    var chemin = Combat.CheminPropose   // liste cellId
    for i in 0..chemin.Count-1 :
        var cell = carte.Obtenir(chemin[i])
        var brush = new SolidColorBrush(Color.FromArgb(180, 255, 220, 60))
        DessinerOverlayCellule(cell, brush.Color, Color.FromRgb(200, 170, 30))
    // dernière cellule = case d'arrivée, surlignée en jaune vif
```

### 5.3 Highlight « qui joue maintenant »

```
DessinerHighlightTour(carte) :
    var id = Combat.IdentifiantCombattantActuel
    var combattant = Combat.Allies.Concat(Combat.Ennemis).FirstOrDefault(c => c.Identifiant == id)
    if combattant == null : return
    var cell = carte.Obtenir(combattant.CellulePosition)
    if cell == null : return
    // halo pulsant doux autour du rectangle (Ellipse z=405, sous le rectangle z=410)
    var halo = new Ellipse { Width=34, Height=34, Stroke=Brushes.Gold, StrokeThickness=2, ... }
```

### 5.4 Highlight ma case (toujours dessiné en combat)

Déjà partiellement traité par `DessinerJoueur` (bleu pâle Fill + bleu stroke sur le polygone). En combat, `DessinerCombattants` dessine le rectangle bleu épais MOI — la cellule sous-jacente n'est PAS coloriée. **À ajouter** :

```csharp
// dans DessinerCombattants après le rectangle MOI :
if (moi && _cellulesPolygons.TryGetValue(cell.Identifiant, out var polyMoi))
{
    polyMoi.Fill = new SolidColorBrush(Color.FromRgb(0xBB, 0xD3, 0xFF));
    polyMoi.Stroke = new SolidColorBrush(Color.FromRgb(0x2D, 0x6C, 0xDF));
    polyMoi.StrokeThickness = 1.6;
    Canvas.SetZIndex(polyMoi, 355);
}
```

## 6. Rollout 2 commits

### Commit F.6 (priorité critique — fix bug visuel)
> `fix(mapviewer): masquer entités hors-combat pendant le combat (3 modes : overworld/placement/encours)`

- Refonte `Rafraichir()` selon §4.1
- Ajout `MettreAJourListeEntitesSelonMode` selon §3.3
- Ajout `DessinerHighlightCelluleMoi` selon §5.4
- Fix surélévation overlays placement §4.4 (cosmétique mais inclus)
- Diagnostic upfront §2 (2 lignes de log debug pendant 1 rebuild, à retirer après confirmation)
- **Pas de** nouvel event combat, **pas de** dépendance UI VueCombat
- **Test** : engager combat → cellules placement bleues/rouges visibles, pas d'entités overworld. Après GTM → rectangles combattants visibles, ennemis rouges et alliés verts, moi en bleu. Fin combat → entités overworld reviennent.

### Commit F.7 (priorité moyenne — highlights tactiques)
> `feat(mapviewer): highlights tactiques en combat (cellules attaquables + chemin proposé + tour actif)`

- Ajout champ `_sortSelectionne` et propagation depuis VueCombat (event `SortSelectionneChange` sur `Compte`)
- Implémentation `DessinerHighlightCellulesAttaquables` §5.1 (avec Bresenham LDV depuis `Divers/Cartes/LigneVue.cs` si existant, sinon stub `return true` en attendant)
- Ajout `Combat.CheminProposeChange` event (modèle) + propagation depuis `MoteurReglesCombat` Phase 1
- Implémentation `DessinerHighlightCheminPropose` §5.2 et `DessinerHighlightTour` §5.3
- **Test** : sélectionner Ronce (sort 183) dans UI Combat → cellules à portée 1-8 surlignées orange autour de moi. Lancer IA → chemin jaune dessiné brièvement avant déplacement.

### Risques

| Risque | Mitigation |
|--------|------------|
| `Combat.Allies` vide en phase Placement → écran « vide » entre les overlays | Garder `DessinerCombattants` même si liste vide (no-op gracieux), overlay placement reste visible |
| Pendant Termine, l'utilisateur veut encore voir les combattants morts pendant 1-2 s | Ajouter `Combat.Etat == Termine` au branche `enCombat` du §4.1 pendant 2 s post-fin |
| `_sortSelectionne` non implémenté → §5.1 sans donnée | Commit F.7 attend que ConfigCombat UI expose la sélection — déjà roadmap ADR-001 |
| Z-fight overlays bleus/rouges sous combattants z=410 | Z explicites partout : sol=prof, gris=prof+200, overlays placement=360, combattants=410, highlights=355 (chemin), 360 (attaquables), 405 (halo tour) |

### Métriques de succès

- Plus aucune pastille verte/rouge d'entité visible en combat sur les screenshots
- Rectangles combattants visibles dès le 1er GTM reçu (vérifiable via log)
- Mon perso = bleu épais distinguable des alliés verts
- Pas de régression overworld : entités, ressources, sorties visibles hors combat
