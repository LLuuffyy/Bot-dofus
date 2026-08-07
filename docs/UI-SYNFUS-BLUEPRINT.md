# UI Combat — Blueprint reproduction SynFus (v2)

> **Cible** : reproduire à l'identique l'onglet **Combat → Sorts** de SynFus Bot v1.1.0
> (skin Aqua, Windows Forms classique) dans Luffy-bot WPF.
> **Source** : screenshots `C:\Users\touki\Desktop\screen\image.png` (Sorts) et
> `image1.png` (General) — référence absolue.
> **État** : Luffy-bot a déjà la donnée (`ConfigCombat`, `RegleSort` 18+ conditions,
> `SortsAppris` collection). Il manque uniquement la **structure XAML/codebehind**
> qui matche SynFus pixel par pixel.
> **Public** : un coder peut implémenter cette UI sans réflexion en suivant
> les §3 à §7 dans l'ordre.

---

## 1. Description précise des 2 screenshots SynFus

### 1.1 — `image.png` (onglet **Combat → Sorts**, le PRINCIPAL à reproduire)

**Structure verticale, de haut en bas, 3 zones empilées dans le contenu de l'onglet :**

#### Zone A — Liste rotation (DataGrid) + barre boutons à droite
- DataGrid plein-largeur, ~7 lignes visibles, fond blanc, hauteur fixée (scroll vertical).
- **5 colonnes**, header gris clair, séparateurs verticaux fins :
  | Col | Largeur | Type | Bind |
  |-----|---------|------|------|
  | `ID` | ~50 px | TextColumn read-only | `Regle.IdSort` |
  | `Name` | ~220 px | TextColumn read-only | `NomSort` (depuis BaseSorts) |
  | `Focus` | ~100 px | ComboBoxColumn éditable | `Regle.Focus` (enum) — affiché en MAJUSCULES (`MOI`, `ENNEMI`, …) |
  | `Nombre x par tours` | ~150 px | TextColumn éditable (numeric) | `Regle.NombreParTour` |
  | `Lancement` | ~180 px | ComboBoxColumn éditable | `Regle.MethodeLancement` enum — `LES_DEUX`, `CAC`, `DISTANCE` |
- **Sélection ligne** : fond bleu (`#3B5998` env.), texte blanc — visible sur la ligne "Force des géants".
- **À droite du DataGrid** : colonne fixe ~70 px de large avec **4 gros boutons icône** empilés verticalement, séparation ~20 px :
  1. **↑ Bleu** (flèche bleue dans cercle bleu clair) — "Monter dans la rotation"
  2. **↓ Bleu** (flèche bas) — "Descendre"
  3. **✕ Rouge** (croix rouge épaisse) — "Supprimer le sort sélectionné"
  4. **ⓘ Bleu** (i bleu cerclé) — "Info sort" (ouvre une popup ou détail)
- Les boutons sont **désactivés tant qu'aucune ligne n'est sélectionnée** (sauf Info qui peut être toujours actif).

#### Zone B — Formulaire « Ajouter un sort »
- Encadré gris pâle (`GroupBox` WinForms titré **"Ajouter un sort"** en haut à gauche).
- 4 lignes label + champ étirable, label aligné à gauche (~280 px), champ ~620 px à droite :
  1. **Sort :** ComboBox → `[0] CaC (Corps à corps)` (l'élément sélectionné montre `[id] nom`).
  2. **Focus :** ComboBox → `Ennemi le + proche` (libellés en français, conversion enum visible).
  3. **Nombre de lancer par tour :** TextBox/NumericUpDown → `5`.
  4. **Méthode de lancement :** ComboBox → `CAC`.
- En bas du formulaire (large bouton plein-largeur, fond blanc, bordure noire fine) : **"Ajouter un sort"**.

#### Zone C — Sous-section « Conditions — <NomSortSélectionné> »
- Titre dynamique : `Conditions — Force des géants` (le nom du sort de la ligne sélectionnée dans le DataGrid).
- Grille **5 colonnes verticales** alignées côte-à-côte, chaque colonne = un `GroupBox` titré :

| Col 1 — **Distance** | Col 2 — **Cible** | Col 3 — **Joueur** | Col 4 — **Situation** | Col 5 — **Avancé** |
|---|---|---|---|---|
| ☐ Min: `[1]` | ☐ PV < `[1]` % | ☐ PV < `[1]` % | ☐ Ennemis ≥ `[1]` | ☐ Tous les `[5]` tours |
| ☐ Max: `[1]` | ☐ PV > `[1]` % | ☐ PV > `[1]` % | ☐ Ennemis ≤ `[1]` | ☐ À partir tour `[1]` |
| ☐ Ignorer CAC | ☐ Cible + faible | ☐ Pas si taclé | ☐ 1er tour | Élément : [Aucun ▼] |
| ☐ Seulement CAC | ☐ Cible + forte | ☐ Si invoc. présente | ☐ Dernier tour | ☐ ≤ `[25]` % |
| ☐ Éviter zone `[1]` | — | — | — | ☐ ≥ `[50]` % |

- Chaque ligne = `CheckBox` (active la condition) + label + `NumericUpDown` ou `ComboBox` désactivé tant que la case n'est pas cochée.
- En bas, **gros bouton plein-largeur "Ajouter un sort"** (≈ même bouton que zone B — c'est la barre de status SynFus "appliquer/sauver".)
- Bordure barre-status SynFus en bas (cœur PV / éclair PA / XP / balance pods / smiley humeur) → **hors scope** de l'onglet Combat lui-même.

### 1.2 — `image1.png` (onglet **Combat → General**, MOINS prioritaire)

Sections empilées :
- **Préparation** (GroupBox) : « Positionnement en début de combat » → ComboBox `Pas de déplacement` / `Près des ennemis` / `Loin des ennemis`.
- **Pendant le combat** (GroupBox) :
  - `Style:` ComboBox → `Tactique` (= `StrategieCombat`).
  - `Distance:` NumericUpDown → `5` (= `DistancePreferee`).
  - 3 cases à cocher horizontales : ☐ Bloquer le combat / ☐ Désactiver le mode spectateur / ☐ Utiliser la monture.
- **Consommable de soin** (GroupBox) :
  - ComboBox potions (`Rougely (+21 PV) x1501`) + bouton **Actualiser** à droite.
  - 4 lignes : `Utiliser si PV ≤ X%` (90), `Jusqu'à Y%` (100), `Délai (ms) Min` (150), `Délai (ms) Max` (400).
- Bouton plein-largeur **"Sauvegarder"** en bas.

> NOTE : la zone gauche d'`image1.png` montre une **palette des sorts appris** (icônes 64×64 + nom + niveau), déjà partiellement présente dans Luffy via `WrapPanel SortsAppris`. À garder en l'état pour cette refonte v2.

---

## 2. Diff Luffy actuel vs SynFus cible

| Section | État Luffy (VueCombat.xaml actuel) | État SynFus cible | Action |
|---|---|---|---|
| **Liste rotation sorts** | `ItemsControl` ad-hoc (Border CornerRadius + grid 6 col) | **DataGrid 5 colonnes** triable + sélection ligne | **REMPLACER** par `DataGrid` (§3) |
| **Boutons rotation** | 3 boutons fantômes inline DANS chaque row (↑↓✕) | **4 gros boutons icône colorés** dans une colonne séparée À DROITE du DataGrid | **DÉPLACER** + ajouter bouton `ⓘ` (§3) |
| **Tri sorts** | Ordre = ordre liste, pas de tri user | Ordre = ordre liste, montée/descente via boutons droits | OK pas de changement logique |
| **Formulaire ajout** | Grid 2 col × 3 rows : `SORT`, `CIBLE`, `X FOIS/TOUR`, `X FOIS/CIBLE`, `PRIORITE`, bouton ajouter | Stack vertical 4 lignes : `Sort`, `Focus`, `Nombre par tour`, `Méthode`, bouton ajouter plein-largeur | **REFAIRE** layout (§4) — supprimer `X fois/cible` + `Priorité` (gérée par ordre) |
| **Section Conditions** | **N'EXISTE PAS** (Luffy a juste un champ Priorité) | GroupBox 5-colonnes (Distance/Cible/Joueur/Situation/Avancé) avec 18+ conditions bindables | **AJOUTER ENTIER** (§5) |
| **Sort sélectionné** | Pas de notion de sélection | DataGrid `SelectedItem` drive la section Conditions | **AJOUTER** event `SelectionChanged` + binding (§6) |
| **Mode combat (4 radios)** | Présent (Agressif/Eloigne/Fuyard/Equilibre) | **Absent de SynFus Sorts** — SynFus a Style ComboBox dans General | **DÉPLACER** vers VueCombatGeneral (futur) ou garder masqué — hors v2 |
| **Sliders (DistPref/Fuite/Délai)** | Présents | Absents de Sorts | **DÉPLACER** vers General (hors v2) |
| **Strategie ComboBox** | Présent | Présent mais dans General | **DÉPLACER** vers General (hors v2) |
| **Bouton Sauvegarder** | Inline section Tactique | Bouton plein-largeur en bas du panneau Sorts | À conserver, le déplacer en bas |
| **Sorts appris (WrapPanel)** | Présent en bas | Présent à gauche (palette) | **OK garder** — utile en dev (hors scope v2 placement) |
| **Etat combat live** | Section dédiée en haut | Pas visible sur screenshots SynFus | **GARDER** Luffy (valeur ajoutée) |

**Décision globale v2** : on refait UNIQUEMENT le bloc « Sorts configurés + Ajouter un sort + Conditions »
(zones B+C des screenshots). Les autres sections (état live, Mode/Sliders, Sorts appris)
restent en place et descendent visuellement dans la page. Les sliders Mode/Stratégie pourront
plus tard être migrés vers un sous-onglet General (cf. §8 commit `e` optionnel).

---

## 3. Squelette XAML COMPLET du DataGrid rotation

> Section **« SORTS CONFIGURÉS »**, remplace l'ancien `ItemsControl x:Name="ListeSortsConfig"`.

```xml
<Border Style="{StaticResource SectionBorderStyle}">
    <StackPanel>
        <DockPanel Margin="0,0,0,12">
            <TextBlock Text="ROTATION DE SORTS"
                       Style="{StaticResource SectionTitleTextStyle}"
                       VerticalAlignment="Center"/>
            <Button Click="BtnViderTout_Click"
                    DockPanel.Dock="Right"
                    Background="Transparent"
                    BorderBrush="Transparent"
                    Foreground="{DynamicResource MutedTextBrush}"
                    Padding="4,0"
                    Cursor="Hand"
                    Content="Vider tout"/>
        </DockPanel>

        <!-- Grid 2-col : DataGrid à gauche, barre boutons à droite -->
        <Grid MinHeight="240">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="72"/>
            </Grid.ColumnDefinitions>

            <DataGrid x:Name="DataGridSorts"
                      Grid.Column="0"
                      ItemsSource="{Binding SortsConfig, RelativeSource={RelativeSource AncestorType=UserControl}}"
                      SelectionChanged="DataGridSorts_SelectionChanged"
                      AutoGenerateColumns="False"
                      CanUserAddRows="False"
                      CanUserDeleteRows="False"
                      CanUserReorderColumns="False"
                      CanUserResizeColumns="True"
                      CanUserSortColumns="False"
                      SelectionMode="Single"
                      SelectionUnit="FullRow"
                      HeadersVisibility="Column"
                      RowHeight="32"
                      ColumnWidth="*"
                      Style="{StaticResource DataGridSortsStyle}">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="ID"
                                        Binding="{Binding Regle.IdSort}"
                                        Width="60"
                                        IsReadOnly="True"/>

                    <DataGridTextColumn Header="Name"
                                        Binding="{Binding NomSort}"
                                        Width="2*"
                                        IsReadOnly="True"/>

                    <DataGridComboBoxColumn x:Name="ColFocus"
                                            Header="Focus"
                                            SelectedItemBinding="{Binding Regle.Focus, UpdateSourceTrigger=PropertyChanged}"
                                            Width="120"/>

                    <DataGridTextColumn Header="Nombre x par tours"
                                        Binding="{Binding Regle.NombreParTour, UpdateSourceTrigger=PropertyChanged}"
                                        Width="140"/>

                    <DataGridComboBoxColumn x:Name="ColMethode"
                                            Header="Lancement"
                                            SelectedItemBinding="{Binding Regle.MethodeLancement, UpdateSourceTrigger=PropertyChanged}"
                                            Width="140"/>
                </DataGrid.Columns>
            </DataGrid>

            <!-- Barre 4 boutons icône à droite -->
            <StackPanel Grid.Column="1"
                        Orientation="Vertical"
                        VerticalAlignment="Top"
                        Margin="10,0,0,0">
                <Button x:Name="BtnRotMonter" Click="BtnMonter_Click"
                        Style="{StaticResource RotationButtonStyle}"
                        ToolTip="Monter le sort dans la rotation"
                        Background="#1F4FA8" Foreground="#9DBEFF">
                    <materialDesign:PackIcon Kind="ArrowUpBold" Width="22" Height="22"/>
                </Button>
                <Button x:Name="BtnRotDescendre" Click="BtnDescendre_Click"
                        Style="{StaticResource RotationButtonStyle}"
                        ToolTip="Descendre le sort"
                        Background="#1F4FA8" Foreground="#9DBEFF">
                    <materialDesign:PackIcon Kind="ArrowDownBold" Width="22" Height="22"/>
                </Button>
                <Button x:Name="BtnRotSupprimer" Click="BtnSupprimer_Click"
                        Style="{StaticResource RotationButtonStyle}"
                        ToolTip="Supprimer le sort"
                        Background="#A33340" Foreground="#FFD7DC">
                    <materialDesign:PackIcon Kind="CloseThick" Width="22" Height="22"/>
                </Button>
                <Button x:Name="BtnRotInfo" Click="BtnInfo_Click"
                        Style="{StaticResource RotationButtonStyle}"
                        ToolTip="Infos sur le sort"
                        Background="#1F4FA8" Foreground="#9DBEFF">
                    <materialDesign:PackIcon Kind="InformationOutline" Width="22" Height="22"/>
                </Button>
            </StackPanel>
        </Grid>
    </StackPanel>
</Border>
```

**À fournir côté `.cs` pour les colonnes ComboBox** (dans `VueCombat()` constructor, après `InitializeComponent()`) :

```csharp
ColFocus.ItemsSource    = System.Enum.GetValues(typeof(FocusSort));
ColMethode.ItemsSource  = System.Enum.GetValues(typeof(MethodeLancement));
```

> **Note technique** : `DataGridComboBoxColumn.ItemsSource` est statique ; il faut
> l'attacher au code-behind (ctor ou `Loaded`). Pour un libellé custom
> (`LES_DEUX` style SynFus) ajouter plus tard un `IValueConverter` ; v2 = affichage
> enum brut (`LesDeux`/`CAC`/`Distance`), polish post-v2.

---

## 4. Squelette XAML du formulaire « Ajouter un sort »

> Section **« AJOUTER UN SORT »**, remplace le bloc actuel (Cible/X fois tour/X fois cible/Priorité).

```xml
<Border Style="{StaticResource SectionBorderStyle}">
    <StackPanel>
        <TextBlock Text="AJOUTER UN SORT"
                   Style="{StaticResource SectionTitleTextStyle}"
                   Margin="0,0,0,14"/>

        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="220"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>

            <!-- Ligne 1 : Sort -->
            <TextBlock Grid.Row="0" Grid.Column="0" Text="Sort :"
                       VerticalAlignment="Center" Margin="0,0,12,10"/>
            <ComboBox Grid.Row="0" Grid.Column="1" x:Name="CmbSort"
                      Margin="0,0,0,10" SelectionChanged="CmbSort_SelectionChanged">
                <ComboBox.ItemTemplate>
                    <DataTemplate>
                        <StackPanel Orientation="Horizontal">
                            <Ellipse Width="12" Height="12" Fill="{Binding CouleurBrush}" Margin="0,0,8,0"/>
                            <TextBlock Text="{Binding Affichage}"/>
                        </StackPanel>
                    </DataTemplate>
                </ComboBox.ItemTemplate>
            </ComboBox>

            <!-- Ligne 2 : Focus -->
            <TextBlock Grid.Row="1" Grid.Column="0" Text="Focus :"
                       VerticalAlignment="Center" Margin="0,0,12,10"/>
            <ComboBox Grid.Row="1" Grid.Column="1" x:Name="CmbFocus"
                      Margin="0,0,0,10" SelectedIndex="0">
                <ComboBoxItem Content="Ennemi le + proche" Tag="EnnemiLePlusProche"/>
                <ComboBoxItem Content="Ennemi le + faible" Tag="EnnemiLePlusFaible"/>
                <ComboBoxItem Content="Ennemi le + fort"   Tag="EnnemiLePlusFort"/>
                <ComboBoxItem Content="Moi"                Tag="Moi"/>
                <ComboBoxItem Content="Allié le + blessé"  Tag="AllieLePlusBlesse"/>
                <ComboBoxItem Content="Cellule vide"       Tag="CelluleVide"/>
            </ComboBox>

            <!-- Ligne 3 : Nombre de lancers par tour -->
            <TextBlock Grid.Row="2" Grid.Column="0" Text="Nombre de lancer par tour :"
                       VerticalAlignment="Center" Margin="0,0,12,10"/>
            <TextBox Grid.Row="2" Grid.Column="1" x:Name="TxtNombreParTour"
                     Text="1" Margin="0,0,0,10"/>

            <!-- Ligne 4 : Méthode de lancement -->
            <TextBlock Grid.Row="3" Grid.Column="0" Text="Méthode de lancement :"
                       VerticalAlignment="Center" Margin="0,0,12,0"/>
            <ComboBox Grid.Row="3" Grid.Column="1" x:Name="CmbMethode" SelectedIndex="0">
                <ComboBoxItem Content="Les deux" Tag="LesDeux"/>
                <ComboBoxItem Content="CAC"      Tag="CAC"/>
                <ComboBoxItem Content="Distance" Tag="Distance"/>
            </ComboBox>
        </Grid>

        <Button Click="BtnAjouter_Click"
                Style="{StaticResource AccentNavButtonStyle}"
                HorizontalAlignment="Stretch"
                Margin="0,16,0,0"
                Content="Ajouter à la rotation"/>
    </StackPanel>
</Border>
```

---

## 5. Squelette XAML de la section CONDITIONS

> Section **« CONDITIONS — <NomSortSelectionne> »**, AJOUTÉE intégralement.
> Le `DataContext` du contenu est lié à `DataGridSorts.SelectedItem.Regle` (`RegleSort`).

```xml
<Border x:Name="PanelConditions"
        Style="{StaticResource SectionBorderStyle}"
        DataContext="{Binding ElementName=DataGridSorts, Path=SelectedItem.Regle}">
    <StackPanel>
        <DockPanel Margin="0,0,0,12">
            <TextBlock Text="CONDITIONS — "
                       Style="{StaticResource SectionTitleTextStyle}"
                       VerticalAlignment="Center"/>
            <TextBlock x:Name="TxtCondTitre"
                       Text="(aucun sort sélectionné)"
                       Style="{StaticResource SectionTitleTextStyle}"
                       Foreground="{DynamicResource AccentBrush}"
                       VerticalAlignment="Center"/>
        </DockPanel>

        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!-- ============= Col 1 : Distance ============= -->
            <GroupBox Grid.Column="0" Header="Distance" Margin="0,0,8,0"
                      Style="{StaticResource ConditionGroupBoxStyle}">
                <StackPanel>
                    <DockPanel Margin="0,0,0,6">
                        <CheckBox x:Name="ChkDistMin" Content="Min :" DockPanel.Dock="Left"
                                  IsChecked="{Binding DistanceMin, Converter={StaticResource NullableIntToBoolConv}, Mode=OneWay}"
                                  Checked="ChkDistMin_Toggled" Unchecked="ChkDistMin_Toggled"/>
                        <TextBox x:Name="TxtDistMin" Width="50" HorizontalAlignment="Right"
                                 Text="{Binding DistanceMin, Mode=TwoWay, TargetNullValue=''}"/>
                    </DockPanel>
                    <DockPanel Margin="0,0,0,6">
                        <CheckBox x:Name="ChkDistMax" Content="Max :" DockPanel.Dock="Left"
                                  IsChecked="{Binding DistanceMax, Converter={StaticResource NullableIntToBoolConv}, Mode=OneWay}"
                                  Checked="ChkDistMax_Toggled" Unchecked="ChkDistMax_Toggled"/>
                        <TextBox x:Name="TxtDistMax" Width="50" HorizontalAlignment="Right"
                                 Text="{Binding DistanceMax, Mode=TwoWay, TargetNullValue=''}"/>
                    </DockPanel>
                    <CheckBox Content="Ignorer CAC" Margin="0,0,0,6"
                              IsChecked="{Binding IgnorerCAC, Mode=TwoWay}"/>
                    <CheckBox Content="Seulement CAC" Margin="0,0,0,6"
                              IsChecked="{Binding SeulementCAC, Mode=TwoWay}"/>
                    <DockPanel>
                        <CheckBox x:Name="ChkEviterZone" Content="Éviter zone" DockPanel.Dock="Left"
                                  IsChecked="{Binding EviterZone, Converter={StaticResource NullableIntToBoolConv}, Mode=OneWay}"
                                  Checked="ChkEviterZone_Toggled" Unchecked="ChkEviterZone_Toggled"/>
                        <TextBox Width="50" HorizontalAlignment="Right"
                                 Text="{Binding EviterZone, Mode=TwoWay, TargetNullValue=''}"/>
                    </DockPanel>
                </StackPanel>
            </GroupBox>

            <!-- ============= Col 2 : Cible ============= -->
            <GroupBox Grid.Column="1" Header="Cible" Margin="4,0,4,0"
                      Style="{StaticResource ConditionGroupBoxStyle}">
                <StackPanel>
                    <DockPanel Margin="0,0,0,6">
                        <CheckBox Content="PV &lt;" DockPanel.Dock="Left"
                                  IsChecked="{Binding CiblePvInfPourcent, Converter={StaticResource NullableIntToBoolConv}, Mode=OneWay}"
                                  Checked="ChkCiblePvInf_Toggled" Unchecked="ChkCiblePvInf_Toggled"/>
                        <TextBlock Text="%" DockPanel.Dock="Right" VerticalAlignment="Center" Margin="4,0,0,0"/>
                        <TextBox Width="50" HorizontalAlignment="Right"
                                 Text="{Binding CiblePvInfPourcent, Mode=TwoWay, TargetNullValue=''}"/>
                    </DockPanel>
                    <DockPanel Margin="0,0,0,6">
                        <CheckBox Content="PV &gt;" DockPanel.Dock="Left"
                                  IsChecked="{Binding CiblePvSupPourcent, Converter={StaticResource NullableIntToBoolConv}, Mode=OneWay}"
                                  Checked="ChkCiblePvSup_Toggled" Unchecked="ChkCiblePvSup_Toggled"/>
                        <TextBlock Text="%" DockPanel.Dock="Right" VerticalAlignment="Center" Margin="4,0,0,0"/>
                        <TextBox Width="50" HorizontalAlignment="Right"
                                 Text="{Binding CiblePvSupPourcent, Mode=TwoWay, TargetNullValue=''}"/>
                    </DockPanel>
                    <CheckBox Content="Cible + faible" Margin="0,0,0,6"
                              IsChecked="{Binding CiblePlusFaible, Mode=TwoWay}"/>
                    <CheckBox Content="Cible + forte"
                              IsChecked="{Binding CiblePlusForte, Mode=TwoWay}"/>
                </StackPanel>
            </GroupBox>

            <!-- ============= Col 3 : Joueur ============= -->
            <GroupBox Grid.Column="2" Header="Joueur" Margin="4,0,4,0"
                      Style="{StaticResource ConditionGroupBoxStyle}">
                <StackPanel>
                    <DockPanel Margin="0,0,0,6">
                        <CheckBox Content="PV &lt;" DockPanel.Dock="Left"
                                  IsChecked="{Binding MesPvInfPourcent, Converter={StaticResource NullableIntToBoolConv}, Mode=OneWay}"
                                  Checked="ChkMesPvInf_Toggled" Unchecked="ChkMesPvInf_Toggled"/>
                        <TextBlock Text="%" DockPanel.Dock="Right" VerticalAlignment="Center" Margin="4,0,0,0"/>
                        <TextBox Width="50" HorizontalAlignment="Right"
                                 Text="{Binding MesPvInfPourcent, Mode=TwoWay, TargetNullValue=''}"/>
                    </DockPanel>
                    <DockPanel Margin="0,0,0,6">
                        <CheckBox Content="PV &gt;" DockPanel.Dock="Left"
                                  IsChecked="{Binding MesPvSupPourcent, Converter={StaticResource NullableIntToBoolConv}, Mode=OneWay}"
                                  Checked="ChkMesPvSup_Toggled" Unchecked="ChkMesPvSup_Toggled"/>
                        <TextBlock Text="%" DockPanel.Dock="Right" VerticalAlignment="Center" Margin="4,0,0,0"/>
                        <TextBox Width="50" HorizontalAlignment="Right"
                                 Text="{Binding MesPvSupPourcent, Mode=TwoWay, TargetNullValue=''}"/>
                    </DockPanel>
                    <CheckBox Content="Pas si taclé" Margin="0,0,0,6"
                              IsChecked="{Binding PasSiTacle, Mode=TwoWay}"/>
                    <CheckBox Content="Si invoc. présente"
                              IsChecked="{Binding SiInvocPresente, Mode=TwoWay}"/>
                </StackPanel>
            </GroupBox>

            <!-- ============= Col 4 : Situation ============= -->
            <GroupBox Grid.Column="3" Header="Situation" Margin="4,0,4,0"
                      Style="{StaticResource ConditionGroupBoxStyle}">
                <StackPanel>
                    <DockPanel Margin="0,0,0,6">
                        <CheckBox Content="Ennemis ≥" DockPanel.Dock="Left"
                                  IsChecked="{Binding EnnemisMin, Converter={StaticResource NullableIntToBoolConv}, Mode=OneWay}"
                                  Checked="ChkEnnemisMin_Toggled" Unchecked="ChkEnnemisMin_Toggled"/>
                        <TextBox Width="50" HorizontalAlignment="Right"
                                 Text="{Binding EnnemisMin, Mode=TwoWay, TargetNullValue=''}"/>
                    </DockPanel>
                    <DockPanel Margin="0,0,0,6">
                        <CheckBox Content="Ennemis ≤" DockPanel.Dock="Left"
                                  IsChecked="{Binding EnnemisMax, Converter={StaticResource NullableIntToBoolConv}, Mode=OneWay}"
                                  Checked="ChkEnnemisMax_Toggled" Unchecked="ChkEnnemisMax_Toggled"/>
                        <TextBox Width="50" HorizontalAlignment="Right"
                                 Text="{Binding EnnemisMax, Mode=TwoWay, TargetNullValue=''}"/>
                    </DockPanel>
                    <CheckBox Content="1er tour" Margin="0,0,0,6"
                              IsChecked="{Binding PremierTour, Mode=TwoWay}"/>
                    <CheckBox Content="Dernier tour"
                              IsChecked="{Binding DernierTour, Mode=TwoWay}"/>
                </StackPanel>
            </GroupBox>

            <!-- ============= Col 5 : Avancé ============= -->
            <GroupBox Grid.Column="4" Header="Avancé" Margin="8,0,0,0"
                      Style="{StaticResource ConditionGroupBoxStyle}">
                <StackPanel>
                    <DockPanel Margin="0,0,0,6">
                        <CheckBox Content="Tous les" DockPanel.Dock="Left"
                                  IsChecked="{Binding TousLesNTours, Converter={StaticResource NullableIntToBoolConv}, Mode=OneWay}"
                                  Checked="ChkTousLesN_Toggled" Unchecked="ChkTousLesN_Toggled"/>
                        <TextBlock Text="tours" DockPanel.Dock="Right" Margin="4,0,0,0" VerticalAlignment="Center"/>
                        <TextBox Width="40" HorizontalAlignment="Right"
                                 Text="{Binding TousLesNTours, Mode=TwoWay, TargetNullValue=''}"/>
                    </DockPanel>
                    <DockPanel Margin="0,0,0,6">
                        <CheckBox Content="À partir tour" DockPanel.Dock="Left"
                                  IsChecked="{Binding APartirDuTour, Converter={StaticResource NullableIntToBoolConv}, Mode=OneWay}"
                                  Checked="ChkAPartirTour_Toggled" Unchecked="ChkAPartirTour_Toggled"/>
                        <TextBox Width="50" HorizontalAlignment="Right"
                                 Text="{Binding APartirDuTour, Mode=TwoWay, TargetNullValue=''}"/>
                    </DockPanel>
                    <DockPanel Margin="0,0,0,6">
                        <TextBlock Text="Élément :" VerticalAlignment="Center" DockPanel.Dock="Left"/>
                        <ComboBox x:Name="CmbElement" Margin="6,0,0,0"
                                  SelectedItem="{Binding ElementRequis, Mode=TwoWay}"/>
                    </DockPanel>
                    <DockPanel Margin="0,0,0,6">
                        <CheckBox Content="≤" DockPanel.Dock="Left"
                                  IsChecked="{Binding SeuilCritiqueInfPct, Converter={StaticResource NullableIntToBoolConv}, Mode=OneWay}"
                                  Checked="ChkCritInf_Toggled" Unchecked="ChkCritInf_Toggled"/>
                        <TextBlock Text="%" DockPanel.Dock="Right" Margin="4,0,0,0" VerticalAlignment="Center"/>
                        <TextBox Width="40" HorizontalAlignment="Right"
                                 Text="{Binding SeuilCritiqueInfPct, Mode=TwoWay, TargetNullValue=''}"/>
                    </DockPanel>
                    <DockPanel>
                        <CheckBox Content="≥" DockPanel.Dock="Left"
                                  IsChecked="{Binding SeuilCritiqueSupPct, Converter={StaticResource NullableIntToBoolConv}, Mode=OneWay}"
                                  Checked="ChkCritSup_Toggled" Unchecked="ChkCritSup_Toggled"/>
                        <TextBlock Text="%" DockPanel.Dock="Right" Margin="4,0,0,0" VerticalAlignment="Center"/>
                        <TextBox Width="40" HorizontalAlignment="Right"
                                 Text="{Binding SeuilCritiqueSupPct, Mode=TwoWay, TargetNullValue=''}"/>
                    </DockPanel>
                </StackPanel>
            </GroupBox>
        </Grid>
    </StackPanel>
</Border>
```

À fournir côté `.cs` au `Loaded` (ou ctor) :
```csharp
CmbElement.ItemsSource = System.Enum.GetValues(typeof(ElementSort));
```

---

## 6. Handlers code-behind à créer / modifier

| Handler | Signature | Rôle |
|---|---|---|
| `DataGridSorts_SelectionChanged` | `(object sender, SelectionChangedEventArgs e)` | Met à jour `TxtCondTitre.Text = vm.NomSort` ; `PanelConditions.IsEnabled = vm != null`. Le `DataContext` est déjà bindé en XAML — pas de code à ajouter pour la propagation. |
| `BtnMonter_Click` | `(object sender, RoutedEventArgs e)` | **Réécrire** pour utiliser `DataGridSorts.SelectedItem` au lieu de `b.Tag`. Re-sélectionner la ligne déplacée après `Rafraichir()`. |
| `BtnDescendre_Click` | idem | Idem `BtnMonter` mais index +1. |
| `BtnSupprimer_Click` | idem | Idem ; supprime la `Regle` du `ConfigCombat`, puis `Rafraichir()` + sélectionne la ligne d'après si possible. |
| `BtnInfo_Click` | `(object sender, RoutedEventArgs e)` | Ouvre un `MessageBox` (ou popup) avec : nom + description + stats par niveau (PA, portée min/max, LDV, surface) + élément. Source : `BaseSorts.Instance.Trouver(idSort)`. |
| `BtnAjouter_Click` | idem | **Réécrire** : lire `CmbSort.SelectedItem` (cast `SortItemVm`), `(CmbFocus.SelectedItem as ComboBoxItem)?.Tag` (parser `FocusSort`), `TxtNombreParTour.Text` (`int.TryParse`, fallback 1), `(CmbMethode.SelectedItem as ComboBoxItem)?.Tag` (parser `MethodeLancement`). Construire `RegleSort` avec stats par niveau (`info.Stats(niveauAppris).CoutPA/PorteeMin/PorteeMax`). |
| `CmbSort_SelectionChanged` | idem | Optionnel : auto-pré-remplir `TxtNombreParTour` selon PA budget (`max(1, 6 / sort.CoutPA)`). Polish post-v2. |
| `ChkDistMin_Toggled` etc. | `(object sender, RoutedEventArgs e)` | Quand la `CheckBox` se décoche → set le champ nullable à `null` ; quand elle se coche et que le champ est `null` → set la valeur par défaut (tableau §10). Le `TwoWay` du `TextBox` continue ensuite à driver. Le `Converter NullableIntToBoolConv` (`x => x.HasValue`) drive l'état coché. À la fin appeler `DemanderSauvegardeDebouncee()`. |
| `RafraichirRegleEnUI()` | `private void` | Helper : reconstruit `SortsConfig` puis re-sélectionne le `Regle` voulu si encore présent (sauver l'ID avant clear, re-trouver après). |
| `DemanderSauvegardeDebouncee()` | (existe ligne 363 actuelle) | À déclencher en fin de chaque handler de condition (debounce 800 ms vers `peleas/<perso>.json`). |

### Nouveau converter à créer

`Utilitaires/Convertisseurs/NullableIntToBoolConv.cs` :
```csharp
using System;
using System.Globalization;
using System.Windows.Data;

namespace BotDofus.Wpf.Utilitaires.Convertisseurs;

public sealed class NullableIntToBoolConv : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is int;  // hasValue → true (Nullable<int> avec valeur boxe en int, sinon null)
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => Binding.DoNothing;  // pas utilisé en OneWay (handler Checked/Unchecked drive l'écriture)
}
```

Déclaration en haut du XAML, dans `<UserControl.Resources>` :
```xml
<UserControl xmlns:conv="clr-namespace:BotDofus.Wpf.Utilitaires.Convertisseurs" …>
  <UserControl.Resources>
    <conv:NullableIntToBoolConv x:Key="NullableIntToBoolConv"/>
    …
  </UserControl.Resources>
```

---

## 7. Styles WPF locaux à ajouter dans `UserControl.Resources`

À placer dans `<UserControl.Resources>` de `VueCombat.xaml`, **après** les styles
inputs déjà présents (ne pas écraser ceux existants).

```xml
<!-- DataGrid spécifique rotation sorts (override fond blanc → sombre) -->
<Style x:Key="DataGridSortsStyle" TargetType="DataGrid" BasedOn="{StaticResource {x:Type DataGrid}}">
    <Setter Property="Background" Value="{DynamicResource SurfaceBrushSoft}"/>
    <Setter Property="RowBackground" Value="#2A2D35"/>
    <Setter Property="AlternatingRowBackground" Value="#262930"/>
    <Setter Property="BorderBrush" Value="{DynamicResource BorderSoftBrush}"/>
    <Setter Property="VerticalGridLinesBrush" Value="#00000000"/>
    <Setter Property="HorizontalGridLinesBrush" Value="#353944"/>
    <Setter Property="RowHeight" Value="34"/>
</Style>

<!-- Bouton 4 actions à droite du DataGrid -->
<Style x:Key="RotationButtonStyle" TargetType="Button">
    <Setter Property="Width" Value="62"/>
    <Setter Property="Height" Value="60"/>
    <Setter Property="Margin" Value="0,0,0,12"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Setter Property="BorderBrush" Value="{DynamicResource BorderSoftBrush}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border x:Name="RotRoot"
                        Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}"
                        CornerRadius="10"
                        Padding="6">
                    <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter TargetName="RotRoot" Property="Opacity" Value="0.85"/>
                    </Trigger>
                    <Trigger Property="IsEnabled" Value="False">
                        <Setter TargetName="RotRoot" Property="Opacity" Value="0.4"/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>

<!-- GroupBox "conditions" -->
<Style x:Key="ConditionGroupBoxStyle" TargetType="GroupBox">
    <Setter Property="Foreground" Value="#ECEFF5"/>
    <Setter Property="BorderBrush" Value="{DynamicResource BorderSoftBrush}"/>
    <Setter Property="Background" Value="{DynamicResource SurfaceBrushSoft}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="Padding" Value="8"/>
    <Setter Property="FontSize" Value="11"/>
    <Setter Property="HeaderStringFormat" Value="{}{0}"/>
</Style>

<!-- CheckBox condition (label + input) -->
<Style TargetType="CheckBox">
    <Setter Property="Foreground" Value="#ECEFF5"/>
    <Setter Property="VerticalAlignment" Value="Center"/>
    <Setter Property="FontSize" Value="11"/>
    <Setter Property="Margin" Value="0,0,4,0"/>
</Style>
```

> **Couleurs réutilisées depuis `App.xaml`** : `SurfaceBrushSoft`, `SurfaceBrushAlt`,
> `BorderSoftBrush`, `AccentBrush`, `MutedTextBrush`, `DangerBrush`, `SuccessBrush`.
> Les hex `#1F4FA8` (bleu boutons) et `#A33340` (rouge) sont des couleurs **locales**
> calquées sur le screenshot SynFus (pas dans la palette globale, à n'utiliser
> que pour les 4 boutons rotation §3).

---

## 8. Plan de découpe en commits

Le coder doit livrer 4 commits indépendants pour faciliter la revue.
Chaque commit DOIT compiler (`dotnet build BotDofus.Wpf/BotDofus.Wpf.csproj -c Debug`)
et passer le smoke test (lancer Luffy-bot, onglet Combat affichable sans crash).

### Commit (a) — DataGrid rotation + 4 boutons droits
- Ajoute styles `DataGridSortsStyle`, `RotationButtonStyle` dans `<UserControl.Resources>`.
- Remplace l'`ItemsControl x:Name="ListeSortsConfig"` par le `Grid` § 3.
- Ajoute `DataGridSorts_SelectionChanged` et `BtnInfo_Click` dans le code-behind.
- Réécrit `BtnMonter_Click` / `BtnDescendre_Click` / `BtnSupprimer_Click` pour utiliser `DataGridSorts.SelectedItem`.
- Attache `ColFocus.ItemsSource` / `ColMethode.ItemsSource` dans le ctor.
- **Acceptance** : ajouter 3 sorts depuis le formulaire existant, les voir dans le DataGrid avec ID/Name/Focus/Nombre/Lancement. Sélection ligne fonctionne. Boutons ↑↓✕ marchent.
- **Message commit** : `feat(ui-combat): DataGrid rotation SynFus + barre 4 boutons (↑↓✕ⓘ)`

### Commit (b) — Formulaire ajout refondu
- Remplace le `Grid 2×3` actuel par le formulaire §4 (Sort / Focus / Nombre / Méthode).
- Renomme `TxtFoisTour` → `TxtNombreParTour`, supprime `TxtFoisCible` + `TxtPriorite`.
- Réécrit `BtnAjouter_Click` pour lire les 4 champs et builder un `RegleSort` complet (stats par niveau via `BaseSorts.Instance.Trouver(id).Stats(niv)`).
- **Acceptance** : ajout d'un sort avec Focus=`Moi` et Méthode=`CAC` correctement reflété dans le DataGrid.
- **Message commit** : `feat(ui-combat): formulaire ajout sort façon SynFus (Sort/Focus/Nombre/Méthode)`

### Commit (c) — Section conditions (5 colonnes, 22 conditions)
- Ajoute `NullableIntToBoolConv` dans `Utilitaires/Convertisseurs/`.
- Ajoute la grosse `Border PanelConditions` §5 dans le XAML.
- Ajoute style `ConditionGroupBoxStyle`.
- Ajoute les ~10 handlers `Chk*_Toggled` (set valeur défaut quand coché / null quand décoché).
- Câble `CmbElement.ItemsSource` au ctor.
- Câble `TxtCondTitre.Text` dans `DataGridSorts_SelectionChanged`.
- **Acceptance** : sélectionner un sort, cocher `Distance Min: 3` + `Cible PV < 50` → champs grisés/dégrisés correctement, valeurs persistent dans `RegleSort`, fichier `peleas/<perso>.json` reflète après debounce 800 ms.
- **Message commit** : `feat(ui-combat): section Conditions 5 colonnes × 22 conditions bindées sur RegleSort`

### Commit (d) — Câblage IA `DecideurCombat` aux conditions
- *(Hors UI mais nécessaire pour valeur métier.)* Ajouter dans `DecideurCombat` la lecture effective des conditions (PV cible, PV soi, premier/dernier tour, ennemis min/max, etc.).
- Filtrer les sorts non éligibles avant le choix de cible.
- **Acceptance** : un sort configuré `Si PV cible < 50%` n'est cast que si la cible est sous 50% PV.
- **Message commit** : `feat(ia-combat): DecideurCombat respecte les 22 conditions RegleSort (filtrage avant cast)`

### Commit (e) — *(OPTIONNEL, post-v2)* sous-onglet General
- Migre les radios `Mode` + sliders + Stratégie + nouvelles options (Bloquer combat / Désactiver spectateur / Utiliser monture) vers `VueCombatGeneral.xaml`.
- Ajoute section « Consommable de soin » bindée sur `ConfigCombat.Consommable*`.
- Inclut un sous-`TabControl` dans `VueCombat.xaml` (`General` / `Sorts`).
- **Acceptance** : config `peleas/<perso>.json` charge/sauve les 4 nouveaux champs.
- **Message commit** : `feat(ui-combat): sous-onglet General (Préparation/Pendant combat/Consommable soin)`

---

## 9. Pièges connus à éviter

1. **MaterialDesign vs styles natifs** — `DataGridComboBoxColumn` ignore les setters
   du style global `ComboBox` ; il faut attacher l'`ItemsSource` au runtime (ctor)
   et NE PAS espérer voir notre style sombre dans la dropdown des cellules. Si le
   contraste est insuffisant, fournir une `ElementStyle` + `EditingElementStyle`
   custom sur la colonne (override fond + fg).
2. **Label blanc sur fond blanc** — déjà patché dans le `VueCombat.xaml` actuel
   (cf. comm. lignes 17-19). Garder les styles locaux `TextBox`/`ComboBox` qui
   forcent `Background=SurfaceBrushSoft`, `Foreground=#F2F4F8`.
3. **`ObservableCollection<SortConfigureVm>` vs `RegleSort` direct** — le
   `SortConfigureVm` actuel n'implémente PAS `INotifyPropertyChanged`. Si une
   condition change la `Regle`, les **propriétés calculées** (`InfoSort`, `CibleTexte`)
   ne se rafraîchissent pas. → **Choix v2 : binder DIRECTEMENT sur `Regle.*`**
   dans le DataGrid (§3 le fait déjà : `Binding Regle.IdSort` et non `IdSort`).
   Si plus tard tu as besoin de propriétés dérivées live, ajoute `INPC` au VM.
4. **`DataGrid.SelectedItem` reset après `ItemsSource.Reset`** — quand `Rafraichir()`
   clear/refill `SortsConfig`, la sélection saute. **Solution** : sauver `IdSort`
   avant clear, re-trouver après. Mieux : modifier la collection en place
   (`Move`/`Add`/`Remove`) au lieu de reconstruire.
5. **`DataGridComboBoxColumn.SelectedItemBinding`** — bug WPF historique : la
   valeur ne se commit que quand la cellule perd le focus. Pour live update,
   utiliser `UpdateSourceTrigger=PropertyChanged` (§3 OK).
6. **Element enum `ElementSort`** — la ComboBox doit afficher des libellés FR
   propres (`Aucun`/`Force`/etc.) plutôt que `EnumType.Force`. Soit `IValueConverter`,
   soit `ItemTemplate` avec dico. v2 = libellé enum brut, polish plus tard.
7. **Stats par niveau** — `BtnAjouter_Click` doit lire le NIVEAU appris du sort
   (`Personnage.SortsAppris[id]`) puis appeler `info.Stats(niveauAppris).CoutPA` etc.
   Ne PAS lire `info.CoutPA` direct (= valeurs niv 1 selon BIBLE CADERNIS, foireux).
8. **Debounce sauvegarde** — la méthode `DemanderSauvegardeDebouncee()` existe
   déjà (`VueCombat.xaml.cs` ligne 363). Tous les handlers `Chk*` et `Txt*` doivent
   l'appeler à la fin pour persister `peleas/<perso>.json` automatiquement.
9. **`GroupBox` Material Design Dark** — le header par défaut a parfois fond
   blanc. Le `ConditionGroupBoxStyle` §7 force le `Foreground` mais pas le
   template du header — si le rendu est moche, ajouter un `Style TargetType="GroupBox"`
   complet avec `Template` custom (voir doc MaterialDesignInXAML).
10. **Largeur écran** — la grille conditions à 5 colonnes nécessite ~1100 px de
    large pour respirer. Sur une fenêtre étroite (≤900 px), envisager un
    `WrapPanel` ou un `ItemsControl` à 2 lignes × 3 colonnes (3+2).
    v2 = grille fixe 5 colonnes, polish plus tard.
11. **`Personnage.SortsAppris`** — propriété déjà existante (`Dictionary<int,int>`
    sort→niveau). Important pour stats par niveau au `BtnAjouter_Click`.
12. **`DataContext` propagation pour `PanelConditions`** — quand `SelectedItem`
    devient null (collection vide après Vider tout), le `DataContext` du panel
    devient null aussi, et les bindings affichent vide — bien. Mais les `CheckBox`
    `Mode=TwoWay` peuvent essayer d'écrire sur un `RegleSort` null → ajoute un
    `if (_panelConditionsRegle == null) return;` au début des handlers, OU
    bind `PanelConditions.IsEnabled = SelectedItem != null` pour bloquer l'input.

---

## 10. Mapping condition UI ↔ propriété RegleSort (référence rapide)

| UI cellule | Propriété C# | Type | Défaut UI quand on coche |
|---|---|---|---|
| Distance Min | `DistanceMin` | `int?` | `1` |
| Distance Max | `DistanceMax` | `int?` | `12` |
| Ignorer CAC | `IgnorerCAC` | `bool` | `true` |
| Seulement CAC | `SeulementCAC` | `bool` | `true` |
| Éviter zone | `EviterZone` | `int?` | `1` |
| Cible PV < % | `CiblePvInfPourcent` | `int?` | `50` |
| Cible PV > % | `CiblePvSupPourcent` | `int?` | `50` |
| Cible + faible | `CiblePlusFaible` | `bool` | `true` |
| Cible + forte | `CiblePlusForte` | `bool` | `true` |
| Joueur PV < % | `MesPvInfPourcent` | `int?` | `30` |
| Joueur PV > % | `MesPvSupPourcent` | `int?` | `80` |
| Pas si taclé | `PasSiTacle` | `bool` | `true` |
| Si invoc. présente | `SiInvocPresente` | `bool` | `true` |
| Ennemis ≥ | `EnnemisMin` | `int?` | `2` |
| Ennemis ≤ | `EnnemisMax` | `int?` | `1` |
| 1er tour | `PremierTour` | `bool` | `true` |
| Dernier tour | `DernierTour` | `bool` | `true` |
| Tous les N tours | `TousLesNTours` | `int?` | `2` |
| À partir du tour | `APartirDuTour` | `int?` | `1` |
| Élément requis | `ElementRequis` | `ElementSort` | (combo) `Aucun` |
| Critique ≤ % | `SeuilCritiqueInfPct` | `int?` | `25` |
| Critique ≥ % | `SeuilCritiqueSupPct` | `int?` | `50` |

**Total : 22 conditions bindables** (18 originelles documentées + 4 supplémentaires Élément/Critique).

---

## 11. Checklist d'acceptance v2 (avant merge `claude/dofus-bot-continue-IUIN3` → `main`)

- [ ] Build `dotnet build BotDofus.Wpf/BotDofus.Wpf.csproj -c Debug` passe sans warning UI.
- [ ] L'onglet Combat s'ouvre sans crash sur un perso vide (`peleas/Beiloddurul.json` absent → config par défaut).
- [ ] DataGrid affiche tous les sorts configurés, colonnes ordre-correct.
- [ ] Sélection d'une ligne met à jour `TxtCondTitre` ET fait apparaître les cases cochées correspondant aux conditions sauvées en JSON.
- [ ] Modifier un champ TextBox condition → fichier JSON mis à jour 1s plus tard (debounce visible dans logs `[CFG-COMBAT] Sauvegardé`).
- [ ] Boutons ↑↓ déplacent la ligne et conservent la sélection.
- [ ] Bouton ✕ supprime la ligne, sélectionne la suivante.
- [ ] Bouton ⓘ ouvre un MessageBox avec nom + description + stats par niveau du sort.
- [ ] Le formulaire « Ajouter un sort » ajoute proprement un `RegleSort` avec les 4 champs (Focus/Nombre/Méthode + sort).
- [ ] Aucune régression sur la section État Combat Live (combattants/PV/PA/PM s'affichent toujours pendant un combat).

---

## Annexe — résumé pour ReasoningBank (mémoire pattern)

> **Clé** : `ui_synfus_blueprint_v2` (namespace `patterns`)
>
> Blueprint complet pour reproduire onglet Combat SynFus dans Luffy-bot WPF.
> 4 commits indépendants : (a) DataGrid 5-col + barre 4 boutons droits (↑↓✕ⓘ),
> (b) formulaire ajout refondu (Sort/Focus/Nombre/Méthode), (c) section
> Conditions 5 colonnes × 22 conditions bindées sur `RegleSort` via
> `DataGridSorts.SelectedItem.Regle`, (d) câblage `DecideurCombat`.
> Styles XAML PRÊT-À-COLLER fournis §3-§7. Couleurs depuis App.xaml
> (`SurfaceBrushSoft`, `BorderSoftBrush`, `AccentBrush`, `DangerBrush`).
> Convertisseur `NullableIntToBoolConv` à ajouter sous `Utilitaires/Convertisseurs/`.
> Bind direct sur `RegleSort.*` (PAS via `SortConfigureVm`) pour éviter `INotifyPropertyChanged`.
> Debounce sauvegarde 800 ms via `DemanderSauvegardeDebouncee()` déjà présent.
> Pièges : `DataGridComboBoxColumn.ItemsSource` au ctor (pas XAML),
> `UpdateSourceTrigger=PropertyChanged` pour live update,
> stats sort `info.Stats(niveauAppris)` PAS `info.CoutPA` direct.
