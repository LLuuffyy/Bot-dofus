# UI Groupe Héros — Design (Luffy-bot)

Cible : nouvel onglet **« Groupe »** (à insérer dans `TabsContent` de `MainWindow.xaml` après l'onglet « Combat »), permettant de configurer le mode héros serveur, de visualiser l'état de chaque perso pendant un combat, et d'ouvrir la config combat individuelle.

Périmètre fonctionnel :
- 8 persos max (1 Sadida leader + 7 Enutrof actuellement).
- Lien automatique via mode héros serveur (Hystoria) → un seul GTM partagé.
- L'UI est **un onglet** dédié, pas une fenêtre détachée — cohérent avec `MainWindow.TabsContent`.

> ⚠️ Aucun fichier `.xaml`/`.cs` n'est créé ici. Le squelette XAML ci-dessous est indicatif et doit être implémenté ultérieurement dans `BotDofus.Wpf/Vues/VueGroupeHeros.xaml(.cs)` (hors scope de ce design doc).

---

## A. Layout général

Vue découpée en **2 colonnes** dans un `Grid` (panneau central + panneau latéral droit), elle-même structurée en **3 lignes verticales** dans la colonne centrale (Config groupe / État live / Bandeau bas).

```
+-----------------------------------------------------------------------------+
| [ONGLET Groupe]   (header partagé MainWindow.xaml — pas redessine ici)      |
+-----------------------------------------------------------------------------+
| Grid principal   Col 0 = *   |   Col 1 = 300 (panneau droit)                |
| Row 0 = Auto, Row 1 = *, Row 2 = Auto                                       |
|                                                                             |
| +-------------------- COLONNE CENTRALE --------------------+ +------------+ |
| | [Border PanelBorderStyle]  CONFIGURATION DU GROUPE       | | ORDRE      | |
| |   Etat groupe :  [pastille verte/grise]  Actif | Inactif | | DES TOURS  | |
| |   Tour global  :  [#12]   (visible mode combat)          | |            | |
| |                                                          | | Tour #12   | |
| |   Persos connectes (ItemsControl)                        | |            | |
| |   [x] LEADER (o) Beiloddurul   Sadida   Niv 13           | | (highlight | |
| |   [x]        ( ) Aerawiol      Enutrof  Niv 21           | |  vert vif  | |
| |   [x]        ( ) Vrottigrat    Enutrof  Niv 19           | |  perso en  | |
| |   [ ]        ( ) Otherperso    Enutrof  Niv 18           | |  cours)    | |
| |                                                          | |            | |
| |   [Lier en groupe heros]  [Delier]      [Rafraichir]     | | ListBox    | |
| |                                                          | |  ordonnee  | |
| +----------------------------------------------------------+ |  serveur   | |
|                                                              |            | |
| +------------------ VUE COMBAT (Row 1 = *) ---------------+  | 1. Beilodd | |
| | [Border PanelBorderStyle]  COMBAT EN COURS  [badge tour]|  | 2. Aerawio | |
| |                                                         |  | 3. Mob A   | |
| | ItemsControl  ListeMembres  (1 ligne par perso)         |  | 4. Vrottig | |
| |                                                         |  | 5. Mob B   | |
| | +----------------------------------------------------+  |  | ...        | |
| | |[O] Beilodd   Sad N13  PV [####----] 480/820        |  |  |            | |
| | | (TOUR ACTIF : border vert vif + glow)              |  |  | Auto-scroll| |
| | |               PA [####] 6/6  PM [###-] 3/4         |  |  | sur perso  | |
| | |               [Config combat]  [Mode passif: off]  |  |  |  actif     | |
| | +----------------------------------------------------+  |  |            | |
| | +----------------------------------------------------+  |  |            | |
| | |[O] Aerawiol  Enu N21  PV [########] 1240/1240      |  |  |            | |
| | |               PA [######] 8/8  PM [###-] 3/4       |  |  |            | |
| | |               [Config combat]                      |  |  |            | |
| | +----------------------------------------------------+  |  |            | |
| | ...                                                     |  |            | |
| | (vide hors combat -> placeholder "Pas de combat")       |  |            | |
| +---------------------------------------------------------+  |            | |
|                                                              |            | |
| +--------------- BANDEAU BAS (Row 2 = Auto) --------------+  |            | |
| | [Mode heros : ACTIF]  [Combat #34]  [Strategie : Pillage]|  |            | |
| +---------------------------------------------------------+  +------------+ |
+-----------------------------------------------------------------------------+
```

Composants WPF de haut niveau utilisés :

| Zone | Composant WPF | Notes |
|------|---------------|-------|
| Conteneur racine | `UserControl` + `Grid` 2 col × 3 lignes | Cohérent avec `VueCombat`, `VueDashboard` |
| Section 1 — Config groupe | `Border` (style `PanelBorderStyle`) + `ItemsControl` | Liste persos = items avec `CheckBox` + `RadioButton` |
| Section 2 — Vue combat | `Border` + `ItemsControl` (PAS DataGrid) | Mêmes raisons que `VueCombat.ListeCombattants` (Border colorés par état) |
| Section 3 — Ordre des tours | `Border` + `ListBox` virtualisée | Style proche `ListeChat`/`ListeConsole` de `VueDashboard` |
| Bandeau bas | `Border` + `StackPanel` horizontal | Compact, 1 ligne de chips |

Wrap général dans un `ScrollViewer` vertical (cf. `VueCombat`, `VuePersonnage`) pour cas écran réduit.

---

## B. Détail par section

### B.1 — Configuration du groupe (Row 0)

But : choisir qui fait partie du groupe héros + désigner le leader avant de cliquer « Lier ».

| Élément | Composant | Binding | État visuel |
|---------|-----------|---------|-------------|
| Titre section | `TextBlock` style `SectionTitleTextStyle` | — | « COMPOSITION DU GROUPE » |
| Badge état | `Border` arrondi + `Ellipse` + `TextBlock` | `{Binding EstActif}` → couleur (`SuccessBrush` / `FaintTextBrush`) | Vert plein « Groupe actif » / gris « Inactif » |
| Tour global | `TextBlock` | `{Binding NumeroTour, StringFormat='Tour #{0}'}` | Visible seulement quand `EstEnCombat=true` |
| Liste persos | `ItemsControl` + `ItemsSource={Binding Membres}` | `Membres : ObservableCollection<MembreGroupeVM>` | 1 ligne par perso connecté au proxy |
| Case « inclure » | `CheckBox` | `IsChecked={Binding Inclus, Mode=TwoWay}` | Désactive le perso du groupe sans le déconnecter |
| Case « leader » | `RadioButton GroupName="Leader"` | `IsChecked={Binding EstLeader, Mode=TwoWay}` | Un seul leader à la fois, géré par ViewModel |
| Avatar/sprite | `Grid` 32×32 avec `Ellipse` + `TextBlock` initial classe | `{Binding AvatarBrush}` + `{Binding ClasseAbrev}` | Style identique `MainWindow.LstComptes` (cercle 40×40) |
| Nom + classe + niveau | 3 `TextBlock` en `StackPanel` | `{Binding Nom}`, `{Binding ClasseTexte}`, `{Binding NiveauTexte}` | — |
| Bouton « Lier » | `Button` style `AccentNavButtonStyle` | `Click=BtnLier_Click` | Désactivé si 0 leader ou < 2 inclus ou groupe déjà actif |
| Bouton « Délier » | `Button` style `TopNavButtonStyle` (rouge doux) | `Click=BtnDelier_Click` | Désactivé si `EstActif=false` |
| Bouton « Rafraîchir » | `Button` style `TopNavButtonStyle` | `Click=BtnRafraichir_Click` | Re-scan des comptes proxy actifs |

**ViewModel suggéré** :

```
MembreGroupeVM
  - bool Inclus        (TwoWay, persiste local)
  - bool EstLeader     (TwoWay, exclusif via VM parent)
  - string Nom         (= Personnage.Nom)
  - string ClasseTexte (= "Sadida", "Enutrof", ...)
  - string ClasseAbrev (= "SA", "EN", ...) pour le cercle 32×32
  - string NiveauTexte (= "Niv 13")
  - Brush  AvatarBrush (cohérent palette classe)
  - bool   EstConnecte (pour griser si proxy down)

VueGroupeHerosVM
  - ObservableCollection<MembreGroupeVM> Membres
  - bool EstActif        (= compte.MaisonGroupeHeros)
  - int  NumeroTour
  - bool EstEnCombat
  - ICommand LierCommand, DelierCommand, RafraichirCommand
```

### B.2 — Vue d'ensemble pendant combat (Row 1)

But : voir d'un coup d'œil l'état de chaque allié pendant combat + accès rapide à la config combat individuelle.

| Élément | Composant | Binding | État visuel |
|---------|-----------|---------|-------------|
| Conteneur ligne | `Border` (CornerRadius 8) avec `BorderBrush={Binding CouleurBordure}` BorderThickness 2 | `CouleurBordure` = vert `#43A047` si `EstSonTour=true`, sinon `{StaticResource BorderSoftBrush}` | Ligne en cours = border vert vif (cf. `VueCombat` row style) |
| Avatar | `Grid` 40×40 + `Ellipse` + initiales | identique B.1 | — |
| Nom + classe + niveau | `StackPanel` 3 `TextBlock` | `{Binding Nom}`, `{Binding ClasseTexte}`, `{Binding NiveauTexte}` | Nom `FontWeight=Bold` |
| Barre PV | `ProgressBar` + `TextBlock` superposé | `Value={Binding PourcentagePv}`, `Foreground=DangerBrush`, texte `{Binding PvTexte}` (ex. « 480 / 820 ») | Cf. `MainWindow.BarVie` (style identique) |
| Compteur PA | `Border` arrondi + icône + `TextBlock` | `{Binding PaTexte}` (ex. « 6/6 ») `Foreground=#1E88E5` | Couleur bleue (PA) identique `VueCombat` |
| Compteur PM | `Border` arrondi + icône + `TextBlock` | `{Binding PmTexte}` (ex. « 3/4 ») `Foreground=#43A047` | Couleur verte (PM) identique `VueCombat` |
| Indicateur tour actif | `Border` (intérieur ligne) + `materialDesign:PackIcon Kind=Play` | `Visibility={Binding EstSonTour, Converter=BoolToVis}` | Pulse léger (DoubleAnimation Opacity 0.6↔1.0) en plus du border vert |
| Bouton config combat | `Button` style `TopNavButtonStyle` | `Click=BtnConfigCombat_Click` + `Tag={Binding}` | Ouvre `VueCombat` pré-bindée sur ce perso (via `ContexteCompte` cible) |
| Bouton mode passif | `CheckBox` | `IsChecked={Binding ModePassif, Mode=TwoWay}` | Coché → ce perso n'agit pas, tank/leech |
| Placeholder hors combat | `TextBlock` italique muted | `Visibility` inverse `EstEnCombat` | Texte : « Aucun combat en cours. La vue se remplira automatiquement. » (cf. `VueCombat.TxtCombatVide`) |

**Choix : `ItemsControl` plutôt que `DataGrid`** — voir section D.

**ViewModel par membre combat** :

```
MembreCombatVM (étend MembreGroupeVM)
  - bool   EstSonTour
  - int    PvActuels, PvMax    (-> PourcentagePv, PvTexte)
  - int    PaActuels, PaMax    (-> PaTexte)
  - int    PmActuels, PmMax    (-> PmTexte)
  - bool   ModePassif          (TwoWay -> Compte.ModePassif via VM)
  - int    Cellule             (debug, tooltip seulement)
  - Brush  CouleurBordure      (calc selon EstSonTour)
```

### B.3 — Ordre des tours (Col 1, panneau latéral droit)

But : afficher la timeline serveur des tours du combat en cours. Read-only.

| Élément | Composant | Binding | État visuel |
|---------|-----------|---------|-------------|
| Conteneur | `Border` style `PanelBorderStyle` 300×* | — | Bord gauche soft pour séparer visuellement |
| Titre | `TextBlock` style `SectionTitleTextStyle` | — | « ORDRE DES TOURS » |
| Numéro tour | `TextBlock` taille 22 bold (cf. `VueCombat.TxtTour`) | `{Binding NumeroTour}` | Centré, accent doré |
| Liste | `ListBox` virtualisée style `StyleListBoxLog` (cf. `VueDashboard`) | `ItemsSource={Binding OrdreTours}` | Hauteur 1 ligne ≈ 28px |
| Item template | `DataTemplate` avec `Border` CornerRadius 4 + `StackPanel` horizontal | — | Border accent vert si `EstActif=true`, transparent sinon |
| Indicateur côté | `Ellipse` 10×10 | `Fill={Binding CouleurEquipe}` (vert allié, rouge ennemi, gris mob neutre) | Cf. `VueCombat.ListeCombattants` |
| Texte | `TextBlock` | `{Binding Affichage}` (« N° + nom » — ex. « 1. Beiloddurul ») | Mono : `FontFamily=Consolas` |
| Highlight actif | `Border.BorderBrush={Binding CouleurBordure}` BorderThickness 1.5 | logique VM | Auto-scroll vers item actif via `ListBox.ScrollIntoView` côté code-behind |

**ViewModel ordre tours** :

```
EntreeOrdreTourVM
  - int    NumeroDansOrdre
  - string Nom
  - bool   EstAllie       (false = ennemi/mob)
  - bool   EstActif       (= c'est son tour MAINTENANT)
  - Brush  CouleurEquipe  (#65C56F allié, #C95761 ennemi)
  - Brush  CouleurBordure (calc)
  - string Affichage      (ex. "1. Beiloddurul")
```

### États visuels — récap

| Contexte | Visibilité / style |
|----------|--------------------|
| Groupe inactif | Bouton « Lier » `Enabled=true` si conditions OK ; bouton « Délier » `Enabled=false` ; badge gris |
| Groupe actif hors combat | Bouton « Lier » `Enabled=false` ; bouton « Délier » `Enabled=true` ; badge vert ; section combat affiche placeholder |
| Groupe actif en combat | Bouton « Délier » `Enabled=false` (sécurité, on délie pas en plein fight) ; section combat peuplée ; panneau ordre tours peuplé |
| Perso non son tour | Border ligne `BorderSoftBrush`, icon Play hidden |
| Perso son tour | Border vert vif `#43A047` 2px + icon Play visible + pulse opacity |

---

## C. Squelette XAML (indicatif, NON créé)

À titre de guide pour l'implémentation future. Tous les styles référencés (`PanelBorderStyle`, `SectionTitleTextStyle`, `MutedTextStyle`, `AccentNavButtonStyle`, `TopNavButtonStyle`, `BorderSoftBrush`, `AccentBrush`, `SuccessBrush`, `DangerBrush`, `MutedTextBrush`, `FaintTextBrush`, `SurfaceBrushSoft`, `SurfaceBrushAlt`) **existent déjà** dans le ResourceDictionary global (App.xaml) — on ne touche à rien.

```xml
<UserControl x:Class="BotDofus.Wpf.Vues.VueGroupeHeros"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">

    <ScrollViewer VerticalScrollBarVisibility="Auto"
                  HorizontalScrollBarVisibility="Disabled"
                  PanningMode="VerticalOnly"
                  Padding="14">
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="300"/>
            </Grid.ColumnDefinitions>

            <!-- ============ COLONNE CENTRALE ============ -->
            <Grid Grid.Column="0" Margin="0,0,12,0">
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>  <!-- Config groupe -->
                    <RowDefinition Height="*"/>     <!-- Vue combat -->
                    <RowDefinition Height="Auto"/>  <!-- Bandeau bas -->
                </Grid.RowDefinitions>

                <!-- ===== SECTION 1 : COMPOSITION DU GROUPE ===== -->
                <Border Grid.Row="0"
                        Style="{StaticResource PanelBorderStyle}"
                        Padding="16"
                        Margin="0,0,0,12">
                    <StackPanel>
                        <DockPanel Margin="0,0,0,12">
                            <TextBlock Text="COMPOSITION DU GROUPE"
                                       Style="{StaticResource SectionTitleTextStyle}"
                                       VerticalAlignment="Center"/>
                            <Border DockPanel.Dock="Right"
                                    Background="{Binding EtatBadgeBrush}"
                                    CornerRadius="10" Padding="10,3">
                                <StackPanel Orientation="Horizontal">
                                    <Ellipse Width="8" Height="8"
                                             Fill="{Binding EtatPointBrush}"
                                             Margin="0,0,6,0" VerticalAlignment="Center"/>
                                    <TextBlock Text="{Binding EtatTexte}"
                                               FontSize="11" FontWeight="SemiBold"/>
                                </StackPanel>
                            </Border>
                        </DockPanel>

                        <!-- En-tête colonnes (alignement avec template item) -->
                        <Grid Margin="0,0,0,6">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="40"/>   <!-- Inclus -->
                                <ColumnDefinition Width="70"/>   <!-- Leader -->
                                <ColumnDefinition Width="56"/>   <!-- Avatar -->
                                <ColumnDefinition Width="*"/>    <!-- Nom -->
                                <ColumnDefinition Width="120"/>  <!-- Classe -->
                                <ColumnDefinition Width="80"/>   <!-- Niveau -->
                            </Grid.ColumnDefinitions>
                            <TextBlock Grid.Column="0" Text="Inclu" Style="{StaticResource MutedTextStyle}"/>
                            <TextBlock Grid.Column="1" Text="Leader" Style="{StaticResource MutedTextStyle}"/>
                            <TextBlock Grid.Column="3" Text="Nom" Style="{StaticResource MutedTextStyle}"/>
                            <TextBlock Grid.Column="4" Text="Classe" Style="{StaticResource MutedTextStyle}"/>
                            <TextBlock Grid.Column="5" Text="Niveau" Style="{StaticResource MutedTextStyle}"/>
                        </Grid>

                        <!-- Liste membres -->
                        <ItemsControl ItemsSource="{Binding Membres}">
                            <ItemsControl.ItemTemplate>
                                <DataTemplate>
                                    <Border Background="{StaticResource SurfaceBrushAlt}"
                                            BorderBrush="{StaticResource BorderSoftBrush}"
                                            BorderThickness="1"
                                            CornerRadius="6"
                                            Padding="8,6" Margin="0,0,0,5">
                                        <Grid>
                                            <Grid.ColumnDefinitions>
                                                <ColumnDefinition Width="40"/>
                                                <ColumnDefinition Width="70"/>
                                                <ColumnDefinition Width="56"/>
                                                <ColumnDefinition Width="*"/>
                                                <ColumnDefinition Width="120"/>
                                                <ColumnDefinition Width="80"/>
                                            </Grid.ColumnDefinitions>
                                            <CheckBox Grid.Column="0"
                                                      IsChecked="{Binding Inclus, Mode=TwoWay}"
                                                      VerticalAlignment="Center"/>
                                            <RadioButton Grid.Column="1"
                                                         GroupName="LeaderGroupe"
                                                         IsChecked="{Binding EstLeader, Mode=TwoWay}"
                                                         IsEnabled="{Binding Inclus}"
                                                         VerticalAlignment="Center"/>
                                            <Grid Grid.Column="2" Width="36" Height="36" VerticalAlignment="Center">
                                                <Ellipse Fill="{Binding AvatarBrush}"/>
                                                <TextBlock Text="{Binding ClasseAbrev}"
                                                           HorizontalAlignment="Center"
                                                           VerticalAlignment="Center"
                                                           FontWeight="Bold"
                                                           Foreground="White"/>
                                            </Grid>
                                            <TextBlock Grid.Column="3"
                                                       Text="{Binding Nom}"
                                                       VerticalAlignment="Center"
                                                       FontWeight="SemiBold"/>
                                            <TextBlock Grid.Column="4"
                                                       Text="{Binding ClasseTexte}"
                                                       VerticalAlignment="Center"
                                                       Foreground="{StaticResource MutedTextBrush}"/>
                                            <TextBlock Grid.Column="5"
                                                       Text="{Binding NiveauTexte}"
                                                       VerticalAlignment="Center"/>
                                        </Grid>
                                    </Border>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>

                        <!-- Actions -->
                        <StackPanel Orientation="Horizontal" Margin="0,12,0,0">
                            <Button Command="{Binding LierCommand}"
                                    Style="{StaticResource AccentNavButtonStyle}"
                                    Margin="0,0,8,0">
                                <StackPanel Orientation="Horizontal">
                                    <materialDesign:PackIcon Kind="AccountGroup"
                                                             Width="14" Height="14"
                                                             Margin="0,0,6,0"/>
                                    <TextBlock Text="Lier en groupe héros"/>
                                </StackPanel>
                            </Button>
                            <Button Command="{Binding DelierCommand}"
                                    Style="{StaticResource TopNavButtonStyle}"
                                    Background="#4A2C31"
                                    Foreground="#F0808A"
                                    BorderBrush="#6B3D45"
                                    BorderThickness="1"
                                    Margin="0,0,8,0">
                                <StackPanel Orientation="Horizontal">
                                    <materialDesign:PackIcon Kind="AccountMinus"
                                                             Width="14" Height="14"
                                                             Margin="0,0,6,0"/>
                                    <TextBlock Text="Délier"/>
                                </StackPanel>
                            </Button>
                            <Button Command="{Binding RafraichirCommand}"
                                    Style="{StaticResource TopNavButtonStyle}">
                                <materialDesign:PackIcon Kind="Refresh" Width="14" Height="14"/>
                            </Button>
                        </StackPanel>
                    </StackPanel>
                </Border>

                <!-- ===== SECTION 2 : VUE COMBAT ===== -->
                <Border Grid.Row="1"
                        Style="{StaticResource PanelBorderStyle}"
                        Padding="16"
                        Margin="0,0,0,12">
                    <StackPanel>
                        <DockPanel Margin="0,0,0,12">
                            <TextBlock Text="COMBAT EN COURS"
                                       Style="{StaticResource SectionTitleTextStyle}"
                                       VerticalAlignment="Center"/>
                            <Border DockPanel.Dock="Right"
                                    Background="#3D3D3D"
                                    CornerRadius="10" Padding="10,3"
                                    Visibility="{Binding EstEnCombatVisibility}">
                                <TextBlock Text="{Binding NumeroTour, StringFormat='Tour #{0}'}"
                                           FontSize="11" FontWeight="SemiBold"/>
                            </Border>
                        </DockPanel>

                        <!-- Une ligne par membre du groupe (vue combat) -->
                        <ItemsControl ItemsSource="{Binding MembresCombat}">
                            <ItemsControl.ItemTemplate>
                                <DataTemplate>
                                    <Border BorderBrush="{Binding CouleurBordure}"
                                            BorderThickness="2"
                                            CornerRadius="8"
                                            Background="{StaticResource SurfaceBrushAlt}"
                                            Padding="10,8"
                                            Margin="0,0,0,6">
                                        <Grid>
                                            <Grid.ColumnDefinitions>
                                                <ColumnDefinition Width="44"/>   <!-- Avatar -->
                                                <ColumnDefinition Width="180"/>  <!-- Nom/classe -->
                                                <ColumnDefinition Width="*"/>    <!-- Barres -->
                                                <ColumnDefinition Width="Auto"/> <!-- Actions -->
                                            </Grid.ColumnDefinitions>

                                            <Grid Grid.Column="0" Width="40" Height="40" VerticalAlignment="Center">
                                                <Ellipse Fill="{Binding AvatarBrush}"/>
                                                <TextBlock Text="{Binding ClasseAbrev}"
                                                           HorizontalAlignment="Center"
                                                           VerticalAlignment="Center"
                                                           FontWeight="Bold"
                                                           Foreground="White"/>
                                                <!-- Indicateur tour actif -->
                                                <materialDesign:PackIcon Kind="PlayCircle"
                                                                          Width="16" Height="16"
                                                                          Foreground="#43A047"
                                                                          VerticalAlignment="Top"
                                                                          HorizontalAlignment="Right"
                                                                          Visibility="{Binding EstSonTourVisibility}"/>
                                            </Grid>

                                            <StackPanel Grid.Column="1" VerticalAlignment="Center" Margin="10,0,0,0">
                                                <TextBlock Text="{Binding Nom}" FontWeight="Bold"/>
                                                <TextBlock Text="{Binding ClasseEtNiveauTexte}"
                                                           FontSize="11"
                                                           Foreground="{StaticResource FaintTextBrush}"/>
                                            </StackPanel>

                                            <!-- 3 barres : PV / PA / PM -->
                                            <Grid Grid.Column="2" Margin="12,0,12,0" VerticalAlignment="Center">
                                                <Grid.ColumnDefinitions>
                                                    <ColumnDefinition Width="2*"/>
                                                    <ColumnDefinition Width="*"/>
                                                    <ColumnDefinition Width="*"/>
                                                </Grid.ColumnDefinitions>

                                                <StackPanel Grid.Column="0" Margin="0,0,10,0">
                                                    <TextBlock Text="VIE"
                                                               Style="{StaticResource MutedTextStyle}"
                                                               FontSize="10"/>
                                                    <ProgressBar Value="{Binding PourcentagePv, Mode=OneWay}"
                                                                 Maximum="100"
                                                                 Height="8"
                                                                 Foreground="{StaticResource DangerBrush}"
                                                                 Background="#1F2127"/>
                                                    <TextBlock Text="{Binding PvTexte}"
                                                               FontSize="10"
                                                               FontFamily="Consolas"
                                                               Foreground="{StaticResource MutedTextBrush}"/>
                                                </StackPanel>

                                                <StackPanel Grid.Column="1" Margin="0,0,10,0">
                                                    <TextBlock Text="PA"
                                                               Style="{StaticResource MutedTextStyle}"
                                                               FontSize="10"/>
                                                    <ProgressBar Value="{Binding PourcentagePa, Mode=OneWay}"
                                                                 Maximum="100"
                                                                 Height="8"
                                                                 Foreground="#1E88E5"
                                                                 Background="#1F2127"/>
                                                    <TextBlock Text="{Binding PaTexte}"
                                                               FontSize="10"
                                                               FontFamily="Consolas"
                                                               Foreground="{StaticResource MutedTextBrush}"/>
                                                </StackPanel>

                                                <StackPanel Grid.Column="2">
                                                    <TextBlock Text="PM"
                                                               Style="{StaticResource MutedTextStyle}"
                                                               FontSize="10"/>
                                                    <ProgressBar Value="{Binding PourcentagePm, Mode=OneWay}"
                                                                 Maximum="100"
                                                                 Height="8"
                                                                 Foreground="#43A047"
                                                                 Background="#1F2127"/>
                                                    <TextBlock Text="{Binding PmTexte}"
                                                               FontSize="10"
                                                               FontFamily="Consolas"
                                                               Foreground="{StaticResource MutedTextBrush}"/>
                                                </StackPanel>
                                            </Grid>

                                            <StackPanel Grid.Column="3" Orientation="Horizontal" VerticalAlignment="Center">
                                                <CheckBox Content="Passif"
                                                          IsChecked="{Binding ModePassif, Mode=TwoWay}"
                                                          FontSize="11"
                                                          Margin="0,0,8,0"
                                                          ToolTip="Ce perso reste spectateur (tank/leech)"/>
                                                <Button Style="{StaticResource TopNavButtonStyle}"
                                                        Command="{Binding ConfigCombatCommand}"
                                                        CommandParameter="{Binding}"
                                                        ToolTip="Ouvre l'onglet Combat focalisé sur ce perso">
                                                    <StackPanel Orientation="Horizontal">
                                                        <materialDesign:PackIcon Kind="Cog"
                                                                                 Width="14" Height="14"
                                                                                 Margin="0,0,6,0"/>
                                                        <TextBlock Text="Combat"/>
                                                    </StackPanel>
                                                </Button>
                                            </StackPanel>
                                        </Grid>
                                    </Border>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>

                        <TextBlock Text="Aucun combat en cours. La vue se remplira automatiquement."
                                   Foreground="{StaticResource FaintTextBrush}"
                                   FontStyle="Italic"
                                   TextWrapping="Wrap"
                                   Margin="0,8,0,0"
                                   Visibility="{Binding PlaceholderVisibility}"/>
                    </StackPanel>
                </Border>

                <!-- ===== BANDEAU BAS ===== -->
                <Border Grid.Row="2"
                        Style="{StaticResource PanelBorderStyle}"
                        Padding="12,8">
                    <StackPanel Orientation="Horizontal">
                        <Border Background="#153C34" CornerRadius="8" Padding="8,4" Margin="0,0,8,0">
                            <TextBlock Text="{Binding ResumeModeHeros}"
                                       FontSize="11" Foreground="#2FD39D"/>
                        </Border>
                        <Border Background="#243D55" CornerRadius="8" Padding="8,4" Margin="0,0,8,0">
                            <TextBlock Text="{Binding ResumeCombat}"
                                       FontSize="11" Foreground="#72B7FF"/>
                        </Border>
                        <Border Background="#3D2D4A" CornerRadius="8" Padding="8,4">
                            <TextBlock Text="{Binding ResumeStrategie}"
                                       FontSize="11" Foreground="#C79BF0"/>
                        </Border>
                    </StackPanel>
                </Border>
            </Grid>

            <!-- ============ COLONNE DROITE : ORDRE DES TOURS ============ -->
            <Border Grid.Column="1"
                    Style="{StaticResource PanelBorderStyle}"
                    Padding="14">
                <DockPanel>
                    <StackPanel DockPanel.Dock="Top" Margin="0,0,0,10">
                        <TextBlock Text="ORDRE DES TOURS"
                                   Style="{StaticResource SectionTitleTextStyle}"
                                   HorizontalAlignment="Center"/>
                        <TextBlock Text="{Binding NumeroTour, StringFormat='Tour #{0}'}"
                                   FontSize="22"
                                   FontWeight="Bold"
                                   HorizontalAlignment="Center"
                                   Foreground="{StaticResource AccentBrush}"
                                   Margin="0,6,0,0"/>
                    </StackPanel>

                    <ListBox ItemsSource="{Binding OrdreTours}"
                             Background="Transparent"
                             BorderThickness="0"
                             VirtualizingStackPanel.IsVirtualizing="True"
                             VirtualizingStackPanel.VirtualizationMode="Recycling">
                        <ListBox.ItemContainerStyle>
                            <Style TargetType="ListBoxItem">
                                <Setter Property="Padding" Value="0"/>
                                <Setter Property="Margin" Value="0,2"/>
                                <Setter Property="Background" Value="Transparent"/>
                                <Setter Property="BorderThickness" Value="0"/>
                                <Setter Property="Template">
                                    <Setter.Value>
                                        <ControlTemplate TargetType="ListBoxItem">
                                            <ContentPresenter/>
                                        </ControlTemplate>
                                    </Setter.Value>
                                </Setter>
                            </Style>
                        </ListBox.ItemContainerStyle>
                        <ListBox.ItemTemplate>
                            <DataTemplate>
                                <Border BorderBrush="{Binding CouleurBordure}"
                                        BorderThickness="1.5"
                                        Background="{Binding CouleurFond}"
                                        CornerRadius="4"
                                        Padding="8,4">
                                    <StackPanel Orientation="Horizontal">
                                        <Ellipse Width="10" Height="10"
                                                 Fill="{Binding CouleurEquipe}"
                                                 Margin="0,0,8,0"
                                                 VerticalAlignment="Center"/>
                                        <TextBlock Text="{Binding Affichage}"
                                                   FontFamily="Consolas"
                                                   FontSize="12"
                                                   FontWeight="{Binding PoidsTexte}"/>
                                    </StackPanel>
                                </Border>
                            </DataTemplate>
                        </ListBox.ItemTemplate>
                    </ListBox>
                </DockPanel>
            </Border>
        </Grid>
    </ScrollViewer>
</UserControl>
```

> Note implémentation : le code-behind devra côté `Loaded` se brancher sur `MainWindow.CompteActif` (ou plus propre : un service `IGestionnaireGroupe`) pour récupérer la liste des `ContexteCompte` actifs et leur état combat. Pour `ConfigCombatCommand`, déléguer au `TabsContent.SelectedItem = VueCombatTab` du parent et call `VueCombat.AssignerCompte(membre.Compte)` — pattern identique à ce que `MainWindow.LstComptes_SelectionChanged` fait déjà pour `VueCombatTab`. Aucune duplication de logique combat.

---

## D. Palette / style / accessibilité

### Couleurs (déjà définies dans App.xaml — référencées par DynamicResource, pas redéfinies ici)

| Token | Hex (référence) | Usage dans cette vue |
|-------|-----------------|----------------------|
| `AppBackgroundBrush` | `#1B1D23` (estim.) | Fond fenêtre — hors scope (MainWindow) |
| `ShellBackgroundBrush` | `#1F2229` | Fond du panneau racine — hors scope |
| `SurfaceBrushSoft` | (panneaux secondaires) | Fond des `Border` lignes (ItemsControl items) |
| `SurfaceBrushAlt` | `#2A2E38` (estim.) | Fond des items membres et items ordre tours |
| `BorderSoftBrush` | `#393D47` (estim.) | Bordures des panneaux + lignes non-actives |
| `AccentBrush` | (orange dyshay) | Numéro tour, boutons primaires « Lier » |
| `SuccessBrush` | `#65C56F` | Badge « Groupe actif », allié dans ordre tours |
| `DangerBrush` | `#E53935` | Barre PV, ennemis dans ordre tours |
| `WarningBrush` | (jaune ambre) | (réservé futur — ex. perso bas PV) |
| `MutedTextBrush` | `#9AA0AC` | Labels secondaires, classe/niveau |
| `FaintTextBrush` | `#5A6472` | Placeholders, hint texts |

Couleurs **locales spécifiques** (cohérentes avec `VueCombat.ListeCombattants` et `MainWindow` boutons) :

| Élément | Hex |
|---------|-----|
| Border tour actif | `#43A047` (vert vif, identique pulse `VueCombat`) |
| Texte PA | `#1E88E5` |
| Texte PM | `#43A047` |
| Texte PV | `#E53935` |
| Chip mode héros actif | fond `#153C34` / texte `#2FD39D` (cf. badge Pro `MainWindow`) |
| Chip combat # | fond `#243D55` / texte `#72B7FF` (cf. bouton « Lancer jeu ») |
| Chip stratégie | fond `#3D2D4A` / texte `#C79BF0` (cf. bouton « Farm Auto ») |

### Classes/équipes — pastilles avatar

| Classe | Couleur cercle avatar (recommandation) | Initiales |
|--------|---------------------------------------|-----------|
| Sadida | `#7BCB58` (vert nature) | SA |
| Enutrof | `#D59638` (or — couleur déjà utilisée `MainWindow`) | EN |
| Cra | `#D87C7C` | CR |
| Iop | `#C95761` | IO |
| (autres classes : à étendre — palette uniforme) | — | — |

### Accessibilité

- **Contraste** : tous les textes principaux (`#ECEFF5`/`#F2F4F8` sur fond `#1F2229`/`#2A2E38`) → ratio WCAG AA ≥ 7:1 (déjà validé par les vues existantes).
- **État focus** : RadioButton et CheckBox ont déjà un style global avec Coche visible (cf. `VueCombat.UserControl.Resources`) — ne pas réinventer.
- **Cibles clic** : checkboxes et radio padding 6,2 (cf. global) → cible ≥ 22×22 OK.
- **Lecture rapide** : barres PV/PA/PM avec texte numérique en plus de la couleur (pas seulement couleur — daltoniens OK).
- **Tour actif** : double signal (border vert + icône PlayCircle), pas que la couleur.

### Cohérence visuelle

- Padding 16 dans les `Border` de section (= `VueCombat.SectionBorderStyle`).
- Margin 12 entre sections (= `VueCombat`).
- CornerRadius 6-8 sur les items (= `VueCombat.ListeCombattants` et `VueInventaire` groupe headers).
- `FontFamily="Consolas"` pour tous les chiffres tabulaires (PA/PM/PV/tour) — convention déjà adoptée dans `VueDashboard` et `VueCombat`.

---

## E. Hors scope (pour itération suivante)

- Drag & drop pour réordonner les membres dans la composition (l'ordre serveur en combat est fixé par initiative, pas par UI).
- Stratégie de groupe globale (focus fire / heal cascading) — sera un panneau additionnel sous le bandeau bas plus tard.
- Mini-map combat partagée — délégué à `VueMapViewer` existant.
- Édition multi-perso de la config combat (broadcast d'une rotation à tous les Enutrof) — geste avancé à designer séparément.
