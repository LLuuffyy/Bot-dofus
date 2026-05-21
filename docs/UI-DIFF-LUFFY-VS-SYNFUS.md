# UI Diff — Luffy-bot vs SynFus Aqua (Onglet Combat)

Analyse comparative des screenshots du 2026-05-20 (run test soir).
Sources : `C:\Users\touki\Desktop\screen\` — 6 captures Luffy-bot + 2 références SynFus.

---

## 1. Description des screenshots

### Luffy-bot — UI avant Phase D (19:51:08 / 19:52:08 / 19:53:14)
- **19:51:08** : Onglet Combat visible derrière un explorateur Windows ouvert sur dossier `screen`. Thème sombre, header `Beiloddurul Classe #10 Niveau 13`, sidebar gauche avec compte `test/Beiloddurul/En jeu`, onglets `Chat | Personnage | Combat | Carte | MapViewer | Inventaire | Tools | Sniffer | Scripts | Config`. Badge `Hors combat` en haut à droite.
- **19:52:08** : Vue Combat propre, sections `ÉTAT COMBAT EN COURS` (Tour `-`, Alliés `0`, Ennemis `0`), `COMBATTANTS` (vide), 3 boutons `Finir tour / Pret / Quitter`, `SORTS CONFIGURÉS` (DataGrid colonnes # / Sort / Cible / Infos / Conditions — VIDE), `AJOUTER UN SORT` (form avec Sort, Cible, X FOIS / TOUR, X FOIS / CIBLE, PRIORITE, bouton `Ajouter a la rotation`).
- **19:53:14** : Même vue, mais MAINTENANT EN COMBAT (badge vert `En combat`, Tour `2`, Alliés `1`, Ennemis `1`, combattants `Beiloddurul PV 130/130 PA 6 PM 3`, `#-1 PV 10/10 PA 4 PM 3`).

### Luffy-bot — UI APRÈS Phase D (20:38:50 / 20:39:10 / 20:39:37)
- **20:38:50** : Nouvelle vue ScrollViewer. Section `AJOUTER UN SORT` (form Sort/Cible/X par tour/X par cible/Priorité), bouton `Ajouter a la rotation`, NOUVEAU bloc `MODE DE COMBAT` (radios `Agressif / Eloigne / Fuyard / Equilibre`, sliders `DISTANCE PREFEREE`, `SEUIL FUITE (PV %) = 1`, `DELAI ACTIONS (MS) = 65 / 200`), NOUVEAU bloc `TACTIQUE GLOBALE` (ComboBox `STRATEGIE`, bouton `Sauvegarder la config`), bloc `SORTS APPRIS (0)`.
- **20:39:10** : Même vue avec ComboBox `SORT` ouvert montrant une liste BLANC SUR BLANC (illisible).
- **20:39:37** : ComboBox `STRATEGIE` ouvert affichant `Agressif / Tactique (sélectionné) / Defensif / Soutien / Passif / Fugitif` — texte GRIS très pâle sur fond sombre, à peine lisible.

### SynFus Aqua — Référence cible
- **image.png** : Onglet `Combat > Sorts`. **DataGrid de rotation** en HAUT (colonnes `ID | Name | Focus | Nombre x par tours | Lancement`, lignes `2376 Baroud d'honneur MOI 1 LES_DEUX`, `2535 Force des géants MOI 1 LES_DEUX` SÉLECTIONNÉ bleu, etc.). À droite 3 boutons icônes (↑ / ↓ / ✕ / ℹ). En DESSOUS, formulaire `Ajouter un sort` (Sort ComboBox / Focus / Nombre / Méthode), puis bloc `Conditions — Force des géants` avec 5 colonnes (Distance / Cible / Joueur / Situation / Avancé) chacune avec 4-6 checkboxes + spinners. Bouton `Ajouter un sort` plein-largeur en bas. Statusbar PV/Énergie/XP/Poids.
- **image1.png** : Onglet `Combat > General`. Bloc `Préparation` (ComboBox `Positionnement en début de combat: Pas de déplacement`). Bloc `Pendant le combat` (Style ComboBox `Tactique`, Distance spinner `5`, 3 checkboxes `Bloquer le combat / Désactiver mode spectateur / Utiliser la monture`). Bloc `Consommable de soin` (ComboBox objet `Rougely (+21 PV) x1501`, bouton `Actualiser`, spinners `Utiliser si PV ≤ X% = 90`, `Jusqu'à Y% = 100`, `Délai ms Min = 150`, `Délai ms Max = 400`). Bouton `Sauvegarder` plein-largeur. Sidebar gauche : liste invocations Sadida avec sprites.

---

## 2. Diff sectionwise

| Section | État Luffy-bot actuel | État SynFus cible | Écart |
|---------|------------------------|---------------------|-------|
| **DataGrid rotation sorts** | Présente (`SORTS CONFIGURÉS` colonnes #/Sort/Cible/Infos/Conditions) mais vide même quand sorts ajoutés ; PAS de boutons ↑/↓/✕/ℹ ; pas de surlignage sélection | DataGrid haut de vue, sélection bleue, 3 boutons d'action à droite (réordonner, supprimer, info) | Manque boutons reorder/supprimer/info ; pas de sélection visuelle ; DataGrid présente mais cachée derrière le form Ajout |
| **Formulaire ajout sort** | Sort/Cible ComboBox + X tour / X cible / Priorité + bouton | Sort/Focus/Nombre/Méthode lancement (4 champs) | Champ `Priorité` en plus chez Luffy ; manque `Méthode de lancement` (CAC/Distance/LES_DEUX) ; libellés OK |
| **Conditions par sort** | **INEXISTANT** | Bloc complet 5 colonnes × 4-6 conditions (18 conditions au total : Distance Min/Max/IgnorerCAC/SeulementCAC/EviterZone, PV<%, Cible+faible/+forte, Joueur PV<>%, Situation Ennemis≥/≤ et Pas-si-taclé, 1er tour, Dernier tour, Si invoc présente, Avancé Tous les N tours / À partir tour / Élément / </≥ %) | Section critique manquante — bloque toute config IA fine |
| **Mode de combat** | NOUVEAU bloc Luffy : radios `Agressif / Eloigne / Fuyard / Equilibre` + sliders Distance/Seuil fuite/Délai actions | SynFus onglet General : Style ComboBox (Tactique/Agressif/Defensif/Soutien/Passif/Fugitif) + Distance spinner | Labels divergent (Eloigne/Fuyard/Equilibre vs Defensif/Soutien/Passif/Fugitif) ; format radios vs ComboBox ; sliders OK mais positions par défaut illisibles (`1` et `65`) |
| **Délais actions** | Slider `DELAI ACTIONS (MS) = 200` UNIQUE | SynFus a un onglet `Délais` complet (non visible ici mais existant) + dans Soin : Délai Min/Max séparés | Manque Min/Max séparés ; un seul slider trop simpliste |
| **Stratégie / Tactique globale** | Bloc Luffy `TACTIQUE GLOBALE` avec ComboBox Strategie (Agressif/Tactique/Defensif/Soutien/Passif/Fugitif) + bouton Sauvegarder | Idem dans onglet General SynFus | OK couvre le cas — MAIS texte ComboBox quasi-invisible (cf. §3) |
| **Positionnement début combat** | **INEXISTANT** | Bloc `Préparation` SynFus : ComboBox `Pas de déplacement / PresEnnemis / LoinEnnemis` | Section manquante — ConfigCombat.cs le supporte côté code mais pas exposé UI |
| **Bloquer combat / spectateur / monture** | **INEXISTANT** | 3 checkboxes SynFus dans `Pendant le combat` | Manque ; ConfigCombat.BloquerCombat existe |
| **Consommable de soin** | **INEXISTANT** | Bloc SynFus complet : ComboBox objet inventaire (`Rougely +21 PV x1501`) + bouton Actualiser + spinners seuils PV % + délais Min/Max | Section critique manquante — déjà modélisée dans ConfigCombat (ConsommableSoinIdTemplate + seuils + délais) mais pas branchée UI |
| **Invocations Sadida** | **INEXISTANT** (pas de sidebar) | SynFus a sidebar gauche avec sprites Rat-Mbo, Rat-Tatatat etc. | Hors scope onglet Combat actuel ; à prévoir pour Sadida (cf. roadmap CLAUDE.md "Invocations Sadida") |
| **Sauvegarder config** | Bouton `Sauvegarder la config` aligné droite, taille normale | Bouton SynFus `Sauvegarder` PLEIN LARGEUR bas de vue | Position OK mais visibilité faible ; envisager full-width |

---

## 3. Couleurs & lisibilité

### Problèmes critiques BLANC SUR BLANC / illisibles

| Contrôle | Vue concernée | Bug | Cause probable |
|----------|---------------|-----|----------------|
| `ComboBox Sort` (Ajouter un sort) | 20:38:50, 20:39:10 | **Fond BLANC, items BLANC** → liste invisible quand ouverte | Style ComboBox WPF par défaut Windows, override partiel sur Background mais pas sur `ComboBoxItem` |
| `ComboBox Cible` (Ajouter un sort) | 20:38:50 | Idem, fond blanc, placeholder grisé peu lisible | Idem |
| `ComboBox Strategie` (TACTIQUE GLOBALE) | 20:38:50, 20:39:37 | Fond blanc, items en gris pâle sur dropdown sombre → contraste très faible | Idem — items affichés en sombre mais texte grisé |
| `TextBox X FOIS / TOUR / CIBLE / PRIORITE` | 20:38:50 | Fond sombre OK, texte chiffre lisible — mais bordure quasi-invisible, sépare mal du fond | Manque BorderBrush sur Style TextBox |
| Labels `SORT`, `CIBLE`, `X FOIS / TOUR`, etc. | toutes vues 2038x | OK lisibles (gris-clair) mais hiérarchie typo faible (tous même taille) | Pas un bug, juste UX |
| Sliders Mode de combat | 20:38:50 | Track visible mais valeur `1` (SEUIL FUITE) et `65` (DELAI) affichées en blanc cassé minuscule au-dessus du thumb | Style Slider par défaut, manque label valeur stylé |
| Radios `Agressif/Eloigne/Fuyard/Equilibre` | 20:38:50 | Cercle radio quasi-invisible (gris foncé sur gris foncé) | Style RadioButton non thémisé |
| Section `SORTS APPRIS (0)` | 20:38:50 | Header gris pâle sur fond sombre — visible mais contenu vide pas exploitable | Pas un bug critique, juste vide |
| Header `Combat` sélectionné dans tabbar | toutes | Soulignement bleu OK | OK |

### Styles WPF à créer (à mettre dans `Themes/Combat.xaml` ou `App.xaml`)

```xml
<!-- 1. ComboBox sombre uniforme (override item template) -->
<Style x:Key="DarkComboBox" TargetType="ComboBox">
  <Setter Property="Background" Value="#1E1E2E"/>
  <Setter Property="Foreground" Value="#E0E0E8"/>
  <Setter Property="BorderBrush" Value="#3A3A4E"/>
  <Setter Property="ItemContainerStyle">
    <Setter.Value>
      <Style TargetType="ComboBoxItem">
        <Setter Property="Background" Value="#1E1E2E"/>
        <Setter Property="Foreground" Value="#E0E0E8"/>
        <Style.Triggers>
          <Trigger Property="IsHighlighted" Value="True">
            <Setter Property="Background" Value="#3F3F5A"/>
          </Trigger>
        </Style.Triggers>
      </Style>
    </Setter.Value>
  </Setter>
</Style>

<!-- 2. TextBox bordure visible -->
<Style x:Key="DarkTextBox" TargetType="TextBox">
  <Setter Property="Background" Value="#1E1E2E"/>
  <Setter Property="Foreground" Value="#E0E0E8"/>
  <Setter Property="BorderBrush" Value="#4A4A60"/>
  <Setter Property="BorderThickness" Value="1"/>
  <Setter Property="Padding" Value="6,4"/>
</Style>

<!-- 3. RadioButton stylé sombre -->
<Style x:Key="DarkRadioButton" TargetType="RadioButton">
  <Setter Property="Foreground" Value="#E0E0E8"/>
  <Setter Property="Background" Value="#2A2A3E"/>
  <!-- ControlTemplate custom avec ellipse visible -->
</Style>

<!-- 4. Slider label valeur -->
<!-- Ajouter TextBlock {Binding Value} en surimpression du thumb -->

<!-- 5. DataGrid sombre avec sélection bleue -->
<Style x:Key="DarkDataGrid" TargetType="DataGrid">
  <Setter Property="Background" Value="#181828"/>
  <Setter Property="RowBackground" Value="#1E1E2E"/>
  <Setter Property="AlternatingRowBackground" Value="#222234"/>
  <Setter Property="Foreground" Value="#E0E0E8"/>
  <Setter Property="GridLinesVisibility" Value="Horizontal"/>
  <Setter Property="HorizontalGridLinesBrush" Value="#2A2A3E"/>
</Style>
```

### Recommandations couleur (palette à figer)
- Fond principal : `#0F0F1A` (déjà OK)
- Surface card : `#1E1E2E`
- Surface élevée (input) : `#252538`
- Bordure : `#3A3A4E`
- Texte principal : `#E0E0E8`
- Texte secondaire/label : `#9090A0` (actuel `#6E6E80` trop pâle)
- Accent bleu (sélection) : `#5B6CFF`
- Vert succès (combat actif) : `#2DD4BF`
- Rouge danger : `#EF4444`

---

## 4. Priorité visuelle — 10 améliorations urgentes

1. **Fix ComboBox blanc sur blanc** (`Sort`, `Cible`, `Strategie`) — bug bloquant, l'utilisateur ne voit pas les options. Style `DarkComboBox` + ItemContainerStyle.
2. **Ajouter bloc Conditions par sort** sous la DataGrid (5 colonnes × ~6 conditions checkbox + spinner) — bloque toute config IA fine, déjà modélisée dans `RegleSort.cs`.
3. **Ajouter bloc Consommable de soin** (ComboBox objet inventaire + 4 spinners seuils/délais) — déjà dans `ConfigCombat`, manque juste le binding XAML.
4. **Boutons reorder ↑/↓/✕/ℹ DataGrid rotation** — UX SynFus indispensable pour gérer ordre de priorité sorts.
5. **Ajouter bloc Positionnement début combat** (ComboBox PasDeplacement/PresEnnemis/LoinEnnemis) — déjà dans `ConfigCombat.Positionnement`.
6. **Stylé RadioButton mode combat** (cercle visible) — actuellement invisibles, on clique à l'aveugle.
7. **Stylé TextBox avec bordure visible** — actuellement les inputs flottent sans délimitation.
8. **Afficher valeur sliders en gros** à côté du label (style SynFus `5` next to `Distance:`) — `65 / 200` minuscule actuellement.
9. **Séparer Délai actions Min/Max** (2 sliders ou spinners au lieu d'un seul) — SynFus a Min=150/Max=400, plus humanisé.
10. **Cohérence labels stratégie** : aligner radios `Mode de combat` (Agressif/Eloigne/Fuyard/Equilibre) avec ComboBox `Strategie` (Agressif/Tactique/Defensif/Soutien/Passif/Fugitif) — actuellement 2 systèmes parallèles, confusion garantie ; fusionner en 1 seul contrôle = ComboBox unique alignée SynFus.

---

## Fichiers de référence projet

- `C:\Users\touki\Desktop\Mélange\Bot-dofus\BotDofus.Wpf\Vues\VueCombat.xaml` (à refondre)
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\BotDofus.Wpf\Vues\VueCombat.xaml.cs`
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\ConfigCombat.cs` (modèle déjà complet)
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\Divers\Combats\IA\RegleSort.cs` (18 conditions déjà modélisées)
- `C:\Users\touki\Desktop\Mélange\Bot-dofus\BotDofus.Wpf\App.xaml` (ajouter styles globaux)
- Sources cibles UX : `C:\Users\touki\Desktop\screen\image.png`, `image1.png`
