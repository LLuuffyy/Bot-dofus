# ADR-001 — Modes de combat (Agressif / Eloigne / Fuyard / Equilibre) et refonte UI VueCombat

- Statut : Accepté
- Date : 2026-05-20
- Auteurs : System Architecture Designer (assist. Claude)
- Lié à : roadmap CLAUDE.md "Decideur IA refondu avec toutes les conditions SynFus" + "UI WPF onglet Combat"

## 1. Contexte

L'enum existant `StrategieCombat` (Agressif/Tactique/Defensif/Soutien/Passif/Fugitif) decrit un **style** de jeu (priorite dommages vs survie vs soutien). Il est aujourd'hui :

- charge depuis `ConfigCombat.Strategie` (peleas/<perso>.json)
- expose dans la UI (`VueCombat.xaml` CmbStrategie) avec 5 valeurs cablees (Fugitif absent du combo)
- **non lu** par `MoteurReglesCombat` (Phase 1/2/3) — c'est un champ "informatif" pour l'instant
- partiellement double par `ConfigCombat.FuirSiPvBas` + `SeuilFuitePv`
- partiellement double par `PositionnementCombat` (PasDeDeplacement/PresDesEnnemis/LoinDesEnnemis) qui ne pilote QUE le placement INITIAL

Le user demande un systeme SynFus-style avec **4 modes orthogonaux au comportement existant** :

| Mode | Choix cellule cible | Condition fuite |
|------|---------------------|-----------------|
| Agressif | CAC (dist=1 de l'ennemi) | jamais |
| Eloigne | max portee sort + kite (recule apres cast) | jamais |
| Fuyard | max portee, puis fuit si PV < seuil | si PV < SeuilFuitePv |
| Equilibre | distance preferee (= cfg.DistancePreferee) | si PV < SeuilFuitePv |

La question : ce nouveau concept est-il :
- (A) un **nouvel enum** `ModeCombat`, separe et orthogonal a `StrategieCombat`
- (B) une **extension** de `StrategieCombat` (4 valeurs ajoutees, collision avec Tactique/Defensif/etc.)
- (C) un **renommage** `StrategieCombat` -> `ModeCombat` (breaking change JSON existants)

## 2. Decision

**Option A retenue : nouvel enum `ModeCombat` distinct de `StrategieCombat`.**

```csharp
// Nouveau fichier : Divers/Combats/IA/ModeCombat.cs
public enum ModeCombat
{
    Agressif,    // Engage CAC, ignore PV
    Eloigne,     // Maintient max portee + kite
    Fuyard,      // Max portee + fuit si PV bas
    Equilibre    // Tient DistancePreferee, fuit si PV bas (par defaut)
}
```

`ConfigCombat` recoit un nouveau champ :

```csharp
public ModeCombat Mode { get; set; } = ModeCombat.Equilibre;
```

`StrategieCombat` est **conserve tel quel** (compat JSON, role secondaire = profil de filtre de sorts : Soutien -> regles de soin prioritaires ; Passif -> skip total ; etc.). A terme il pourra fusionner avec les filtres de regles ou etre marque `[Obsolete]`, mais pas dans ce ticket.

### Justification

| Critere | A (nouvel enum) | B (extend Strategie) | C (rename) |
|---------|-----------------|----------------------|------------|
| Compat JSON `peleas/*.json` existants | OK | OK | KO (breaking) |
| Semantique propre (mode = positionnement, strategie = style) | OK | KO (mix) | partiel |
| Migration progressive | OK | KO | KO |
| Conflits avec `PositionnementCombat` | gerable (Mode supersede pour deplacement combat, Positionnement reste pour placement initial) | KO | KO |
| Mappable vers UI radio buttons 4 valeurs | OK | KO (6+ valeurs) | OK |

**Risque accepte** : duplication apparente Strategie/Mode/Positionnement. Mitigation : doc et mapping dans UI (voir section 4) ; `Strategie` deviendra a terme un champ derive ou supprime quand le moteur saura tout faire via Mode + regles.

## 3. Algorithme de choix de cellule cible par mode

Implementation cible : nouvelle methode `MoteurReglesCombat.ChoisirCelluleCastEtDeplacement(combat, cfg, sortChoisi, ennemiCible)` qui retourne `(int celluleCast, int[] cheminMove)` selon le mode.

### Formule distance — corrigee

Le code actuel utilise `Cellule.CalculerCoordonnees(id, 14)` (MoteurReglesCombat.cs:179, TrameJeu.cs:1088, 1139). **Bug latent** : les cartes Hystoria sont 15x17, pas 14xN. Doit etre remplace partout par :

```csharp
int largeur = _etat.CarteCourante?.Largeur ?? Carte.LargeurParDefaut; // 15
var (x, y) = Cellule.CalculerCoordonnees(id, largeur);
```

Le moteur etant stateless, il faut lui passer la `Carte` (ou la largeur) en parametre. Refacto :

```csharp
public static ResultatRegle? Evaluer(Combat combat, ConfigCombat cfg,
                                     IReadOnlyDictionary<int,int> sortsAppris,
                                     Carte carte);  // <-- nouveau parametre
```

### Algorithmes par mode

Pseudo-code (pour `sortPorteeMin..sortPorteeMax`, ennemi en `cellEnnemi`, mes PM, PV%) :

```
Agressif :
  cellulesCandidates = cells adjacentes a ennemi (Chebyshev=1)
                       filter (Marchable, non-occupee, non-interactif)
  cible = candidate la + proche de moi via Pathfinder (<= PM)
  // si aucune adjacente possible -> Equilibre comme fallback

Eloigne :
  cellulesCandidates = cells a distance Chebyshev = porteeMax de ennemi
                       (puis porteeMax-1, ... porteeMin si vide)
  cible = candidate la + proche de moi via Pathfinder (<= PM)
  apres cast : repeter sur cells a porteeMax + 1..PM_restant (kite)

Fuyard :
  si PV% < cfg.SeuilFuitePv :
    cellulesCandidates = cells max(dist_a_tous_ennemis), borne Pathfinder PM
    cible = max-eloignement, pas de cast (skip Gt direct)
  sinon : comportement Eloigne

Equilibre :
  distancePref = cfg.DistancePreferee (5 par defaut)
  if abs(distActuelle - distancePref) <= 1 : pas de move, cast direct
  else :
    cellulesCandidates = cells a Chebyshev = distancePref de ennemi
                         (puis distancePref +/- 1, 2, ... jusqu'a porteeMax)
    cible = candidate la + proche via Pathfinder (<= PM)
  si PV% < cfg.SeuilFuitePv : delegue a Fuyard
```

### Points d'integration TrameJeu

`JouerTourCombatAsync` (l 872-1076) garde son squelette mais `TrouverApprocheCombat` (l 1104-1178) devient un cas particulier de la nouvelle fonction (mode = Equilibre, distancePref = porteeMax). Le mode Agressif/Eloigne/Fuyard sont nouveaux et reroutent vers la nouvelle fonction.

## 4. UI WPF — VueCombat refondu

### Sections proposees (top-to-bottom)

1. **Etat combat live** (existant, OK, l 53-155) — pas touche
2. **Mode de combat** (NOUVEAU) — RadioButton group 4 valeurs avec icones
3. **Strategie + Distance preferee + Seuil fuite** (existait partiellement, deplacer ici)
4. **Rotation sorts** (DataGrid reordonnable, remplace ItemsControl existant)
5. **Conditions par sort** (popup au clic-droit sur ligne DataGrid)
6. **Consommable de soin** (NOUVEAU, expose ConsommableSoin* deja dans ConfigCombat)
7. **Delais d'humanisation** (sliders : DelaiEntreActionsMs)
8. **Sorts appris** (existant l 363-393, OK)

### Squelette XAML (section Mode + Strategie)

```xml
<Border Style="{StaticResource SectionBorderStyle}">
  <StackPanel>
    <TextBlock Text="MODE DE COMBAT" Style="{StaticResource SectionTitleTextStyle}"/>
    <UniformGrid Columns="4" Margin="0,8,0,12">
      <RadioButton x:Name="RbAgressif"  GroupName="Mode" Content="Agressif"  Tag="Agressif"
                   Checked="ModeCombat_Checked" Style="{StaticResource ModeRadioStyle}"/>
      <RadioButton x:Name="RbEloigne"   GroupName="Mode" Content="Eloigne"   Tag="Eloigne"
                   Checked="ModeCombat_Checked" Style="{StaticResource ModeRadioStyle}"/>
      <RadioButton x:Name="RbFuyard"    GroupName="Mode" Content="Fuyard"    Tag="Fuyard"
                   Checked="ModeCombat_Checked" Style="{StaticResource ModeRadioStyle}"/>
      <RadioButton x:Name="RbEquilibre" GroupName="Mode" Content="Equilibre" Tag="Equilibre"
                   Checked="ModeCombat_Checked" IsChecked="True"
                   Style="{StaticResource ModeRadioStyle}"/>
    </UniformGrid>

    <Grid>
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/>
      </Grid.ColumnDefinitions>
      <StackPanel Grid.Column="0">
        <TextBlock Text="DISTANCE PREFEREE" Style="{StaticResource SectionLabelStyle}"/>
        <Slider x:Name="SldDistancePref" Minimum="1" Maximum="12" TickFrequency="1"
                IsSnapToTickEnabled="True" ValueChanged="SldDistancePref_Changed"/>
      </StackPanel>
      <StackPanel Grid.Column="1">
        <TextBlock Text="SEUIL FUITE (PV %)" Style="{StaticResource SectionLabelStyle}"/>
        <Slider x:Name="SldSeuilFuite" Minimum="0" Maximum="100" TickFrequency="5"
                IsSnapToTickEnabled="True" ValueChanged="SldSeuilFuite_Changed"/>
      </StackPanel>
      <StackPanel Grid.Column="2">
        <TextBlock Text="DELAI ACTIONS (MS)" Style="{StaticResource SectionLabelStyle}"/>
        <Slider x:Name="SldDelaiActions" Minimum="200" Maximum="2500" TickFrequency="100"
                IsSnapToTickEnabled="True" ValueChanged="SldDelaiActions_Changed"/>
      </StackPanel>
    </Grid>
  </StackPanel>
</Border>
```

### Rotation sorts : ItemsControl -> DataGrid

L'`ItemsControl` actuel (l 190-260) marche mais ne supporte pas drag-reorder natif. Remplacer par `DataGrid` avec :
- colonnes : # / Sort (template) / Cible (ComboBox) / FoisTour / FoisCible / Priorite / Actions
- `CanUserAddRows="False"`, `CanUserReorderColumns="False"`, `AutoGenerateColumns="False"`
- drag-drop via `PreviewMouseLeftButtonDown` + `DragDrop.DoDragDrop` (rebind SortsConfig)
- bouton "Conditions..." par ligne ouvre `FenetreConditionsRegle` (popup modale, formulaire des 18 conditions de `RegleSort`)

### Popup conditions

Nouvelle `FenetreConditionsRegle.xaml` (Window modale) avec sections en accordeon SynFus :
- **Distance** : DistanceMin / DistanceMax / IgnorerCAC / SeulementCAC / EviterZone
- **Cible** : CiblePvInfPourcent / CiblePvSupPourcent / CiblePlusFaible / CiblePlusForte
- **Joueur** : MesPvInfPourcent / MesPvSupPourcent / PasSiTacle / SiInvocPresente
- **Situation** : EnnemisMin/Max / PremierTour / DernierTour / APartirDuTour / TousLesNTours
- **Avancees** : ElementRequis / SeuilCritique*

## 5. Workflow sauvegarde : auto vs manuel

**Decision : auto-save debouncee, bouton "Sauver" supprime.**

- Tout changement UI (radio, slider, ajout regle, edition condition, reorder) declenche `_contexte.ConfigCombat.Sauvegarder(...)` apres un `DispatcherTimer` 800 ms de debounce
- Avantage : zero risque d'oubli, calque SynFus
- Risque : ecrire trop souvent -> mitige par debounce
- Le bouton `BtnSauver_Click` actuel est conserve mais juste force-flush du debounce timer (utile pour le user qui veut etre rassure)

## 6. Consequences

### Positives
- Mode 4 valeurs clair, mappe 1-pour-1 vers radio UI
- Pas de breaking change JSON (Mode optionnel, defaut Equilibre = comportement actuel TrameJeu)
- Bug largeur 14 vs 15 corrige (effet de bord positif : portees correctes sur toutes les maps Hystoria)
- Decoupe Strategie / Mode / Positionnement claire (Strategie = profil sorts, Mode = positionnement combat, Positionnement = placement initial)

### Negatives
- 3 concepts proches qui peuvent dem la confusion -> doc obligatoire dans CLAUDE.md
- `MoteurReglesCombat.Evaluer` change de signature (ajout `Carte`) -> 1 callsite a maj dans TrameJeu (l 917)

### Risques + mitigation
- **R1** : Mode Eloigne sans LDV (Bresenham) cast a travers obstacles -> kick. Mitigation : ne pas activer kite avant LDV (cf. roadmap). Pour v1, Eloigne se limite a max portee + cast.
- **R2** : Fuyard infinite loop si pas de chemin vers safe -> guard "pas de candidate trouvee -> Gt direct".
- **R3** : Auto-save trop frequent corrompt JSON -> ecriture atomique (temp file + rename).

## 7. Alternatives rejetees

- **B (extend StrategieCombat)** : ajoute Agressif/Eloigne/Fuyard/Equilibre a l'enum existant qui contient deja Agressif. Collision de noms + UI radio 10 valeurs = inutilisable. REJETE.
- **C (rename)** : casse les peleas/*.json existants (Strategie -> Mode). Necessite migration. REJETE.
- **D (booleens flat)** : `bool FuirSiPvBas`, `bool ResteCAC`, `bool MaxPortee`. Combinatoire explose, configs incoherentes possibles. REJETE.

## 8. Fichiers a modifier (pour le CODER)

| Fichier | Lignes approx | Changement |
|---------|---------------|------------|
| `Divers/Combats/IA/ModeCombat.cs` | nouveau ~25 | enum + xmldoc |
| `Divers/Combats/IA/ConfigCombat.cs` | ajout l ~30 | `public ModeCombat Mode { get; set; } = ModeCombat.Equilibre;` |
| `Divers/Combats/IA/MoteurReglesCombat.cs` | l 42, 121, 177-182 | signature Evaluer(+Carte), fix largeur 14->cfg.Largeur |
| `Commun/Frames/TrameJeu.cs` | l 917, 1086-1090, 1104-1178 | passer Carte au moteur, fix DistanceDofus largeur, brancher Mode dans TrouverApprocheCombat |
| `BotDofus.Wpf/Vues/VueCombat.xaml` | l 330-361 (Tactique globale) + nouveau Border Mode | ajouter section Mode radio + sliders + remplacer Strategie combo par radio |
| `BotDofus.Wpf/Vues/VueCombat.xaml.cs` | l 49 + nouveaux handlers | bind Mode, sliders, debounce auto-save |
| `BotDofus.Wpf/Vues/FenetreConditionsRegle.xaml(.cs)` | nouveau ~250 | popup modale conditions |
| `CLAUDE.md` | section "IA combat" | doc 4 modes + relation Strategie/Mode/Positionnement |
