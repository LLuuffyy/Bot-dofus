# DIAG — UI Combat : radios "Mode" invisibles + crash conditions

Date : 2026-05-20
Auteurs : Claude (Code Analyzer)
Liens : `BotDofus.Wpf/Vues/VueCombat.xaml`, `BotDofus.Wpf/Vues/VueCombat.xaml.cs`, `Divers/Combats/IA/RegleSort.cs`
Commits incriminés : `66a4db4` (radio Mode), `8827aa5` (panneau Conditions)

---

## §1 — Pourquoi le radio « Mode » est blanc

### Analyse XAML

**`App.xaml` (lignes 8-13)** charge Material Design v5.1.0 :

```xml
<materialDesign:BundledTheme BaseTheme="Dark" PrimaryColor="BlueGrey" SecondaryColor="Teal" />
<ResourceDictionary Source="...MaterialDesignThemes.Wpf;component/Themes/MaterialDesign3.Defaults.xaml" />
```

Le `MaterialDesign3.Defaults.xaml` définit un `Style TargetType="RadioButton"` implicite avec un `ControlTemplate` MD3 qui dessine la pastille interne via une `Ellipse` dont le `Fill` est bindé sur la propriété `Foreground` du RadioButton.

**`VueCombat.xaml` (lignes 71-76)** définit son propre style local :

```xml
<Style TargetType="RadioButton">
    <Setter Property="Foreground" Value="#ECEFF5"/>   <!-- blanc cassé -->
    <Setter Property="VerticalContentAlignment" Value="Center"/>
    <Setter Property="Padding" Value="6,2,0,2"/>
    <Setter Property="FontSize" Value="12"/>
</Style>
```

Pas de `BasedOn`, pas de `Template`. WPF prend le template MD3 hérité du dictionnaire global mais override `Foreground=#ECEFF5`.

**Résultat visuel** : `IsChecked=true` → MD3 dessine une pastille `Fill=Foreground=#ECEFF5` (blanc) à l'intérieur d'un cercle externe également clair → **pastille blanche invisible sur fond blanc cassé**.

Les 4 radios `RbModeAgressif`, `RbModeEloigne`, `RbModeFuyard`, `RbModeEquilibre` (lignes 587-594) sont concernés. `RbModeEquilibre` a `IsChecked="True"` par défaut (l 594), donc même au démarrage le user voit 4 cercles strictement identiques.

### Fix radio Mode

Style explicite avec `ControlTemplate` forçant un `Ellipse` accent visible :

```xml
<Style TargetType="RadioButton">
    <Setter Property="Foreground" Value="#ECEFF5"/>
    <Setter Property="VerticalContentAlignment" Value="Center"/>
    <Setter Property="Padding" Value="6,2,0,2"/>
    <Setter Property="FontSize" Value="12"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="RadioButton">
                <StackPanel Orientation="Horizontal" Background="Transparent">
                    <Grid Width="18" Height="18" VerticalAlignment="Center" Margin="0,0,8,0">
                        <Ellipse x:Name="OuterCircle"
                                 Stroke="{DynamicResource BorderStrongBrush}"
                                 StrokeThickness="2"
                                 Fill="{DynamicResource SurfaceBrushSoft}"/>
                        <Ellipse x:Name="InnerDot"
                                 Width="9" Height="9"
                                 Fill="{DynamicResource AccentBrush}"
                                 Opacity="0"/>
                    </Grid>
                    <ContentPresenter VerticalAlignment="Center"/>
                </StackPanel>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsChecked" Value="True">
                        <Setter TargetName="InnerDot" Property="Opacity" Value="1"/>
                        <Setter TargetName="OuterCircle" Property="Stroke" Value="{DynamicResource AccentBrush}"/>
                    </Trigger>
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter TargetName="OuterCircle" Property="Stroke" Value="{DynamicResource AccentBrushSoft}"/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

Effet : cercle externe gris → bleu accent + pastille bleue centrale visible quand sélectionné.

---

## §2 — Stack trace probable du crash conditions

### Scénario reproduit

1. User clique ⓘ sur ligne de sort → `BtnInfo_Click` (l 294) : `PanelConditions.DataContext = vm.Regle`
2. User coche `ChkDistMin` (case "Min :") → `Checked` event fire
3. → `ChkDistMin_Toggled(...)` → `ToggleNullable("ChkDistMin")` (l 335)
4. → `prop.SetValue(r, (int?)1)` (l 348) — set `DistanceMin = 1`
5. → l 358-360 : `PanelConditions.DataContext = null; PanelConditions.DataContext = ctx;`

### Mécanisme de récursion

Le binding XAML ligne 438 :

```xml
IsChecked="{Binding DistanceMin, Converter={StaticResource NullableIntToBoolConv}, Mode=OneWay}"
```

est en **`Mode=OneWay`** — re-évalué à chaque changement de `DataContext`.

À l'étape 5, en deux passes :
- `DataContext = null` → tous les bindings résolus avec `null` source. Le converter `NullableIntToBoolConv.Convert(null)` retourne `value is int` → **false** → `ChkDistMin.IsChecked` passe à **false** → fire `Unchecked` event.
- `DataContext = ctx` (le RegleSort) → binding re-évalué, `DistanceMin = 1`, converter → **true** → `ChkDistMin.IsChecked` repasse à **true** → fire `Checked` event.

Le `Checked` event ré-appelle `ChkDistMin_Toggled` → `ToggleNullable` → … → cycle infini.

À chaque tour :
- `prop.GetValue(r) is null` → **false** (déjà settée), donc on ne réécrit pas (l 347)
- mais on entre quand même dans le toggle DataContext (l 358-360) inconditionnellement
- → re-fire des `Unchecked`/`Checked` → récursion en cascade

### Stack trace attendue (résumée)

```
System.StackOverflowException
  at VueCombat.ToggleNullable(String chkName)
  at VueCombat.ChkDistMin_Toggled(Object sender, RoutedEventArgs e)
  at System.Windows.Controls.Primitives.ToggleButton.OnChecked(RoutedEventArgs)
  at ... (réentrée binding update)
  at VueCombat.ToggleNullable(String chkName)
  at VueCombat.ChkDistMin_Toggled(Object sender, RoutedEventArgs e)
  ... (5000+ frames)
  Process terminated (StackOverflow = FailFast, non-catchable)
```

**Note** : `StackOverflowException` n'est PAS attrapable par `try/catch` depuis .NET 2.0 — kill process direct, aucun log Journaliseur ne sortira. Le user voit Luffy-bot disparaître silencieusement.

### Pourquoi `Unchecked` ne sauve pas

Le `Unchecked` re-rentre aussi dans `ToggleNullable` (handler identique sur Checked et Unchecked l 439). À l'étape `Unchecked` (DataContext=null) : `cb.IsChecked == true` → faux → branche `else` → `prop.SetValue(r, null)` — **on RESET la valeur à null**. Donc à l'étape suivante `DataContext = ctx` → `DistanceMin=null` → converter false → checkbox false → `Unchecked` re-fire → on entre, `cb.IsChecked == false` → set null (déjà null)… toggle DataContext encore → boucle. Même verdict : récursion.

---

## §3 — Fix précis

### Fix 1 — Radio Mode (XAML uniquement)

Remplacer le style RadioButton de `VueCombat.xaml` lignes 71-76 par le bloc `ControlTemplate` complet du §1 ci-dessus. Pas de modif de code-behind. Pas d'impact sur le reste de l'app (style local au UserControl).

**Validation visuelle** : démarrage app → onglet Combat → MODE DE COMBAT : pastille bleue (#6C76FF) bien visible sur le radio sélectionné. Clic sur Agressif → pastille migre. `RbModeEquilibre.IsChecked=true` par défaut → pastille visible dès l'ouverture.

### Fix 2 — Crash conditions (3 options, par ordre de préférence)

#### Option A (la plus propre) — `INotifyPropertyChanged` sur `RegleSort`

Modifier `Divers/Combats/IA/RegleSort.cs` :

```csharp
public sealed class RegleSort : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    private int? _distanceMin;
    public int? DistanceMin
    {
        get => _distanceMin;
        set { if (_distanceMin != value) { _distanceMin = value; OnPropChanged(nameof(DistanceMin)); } }
    }
    // ... idem pour les 10 props nullable

    private void OnPropChanged(string n) => PropertyChanged?.Invoke(this,
        new System.ComponentModel.PropertyChangedEventArgs(n));
}
```

Puis dans `VueCombat.xaml.cs` ligne 358-360, **supprimer le toggle DataContext** :

```csharp
// AVANT :
var ctx = PanelConditions.DataContext;
PanelConditions.DataContext = null;
PanelConditions.DataContext = ctx;

// APRÈS :
// (rien — INPC fait le boulot)
```

Effet : `prop.SetValue(r, 1)` lève `PropertyChanged("DistanceMin")` → le TextBox `Text={Binding DistanceMin, Mode=TwoWay}` se refresh tout seul, le CheckBox `IsChecked={Binding ..., Mode=OneWay}` reste cohérent. **Pas de réentrance**.

Coût : ~50 lignes boilerplate INPC ou ~5 lignes avec `CommunityToolkit.Mvvm` (`[ObservableProperty]`).

#### Option B (minimal) — Garde de réentrance + try/catch

Ajouter dans `VueCombat.xaml.cs` :

```csharp
private bool _inToggleNullable;

private void ToggleNullable(string chkName)
{
    if (_inToggleNullable) return;  // garde réentrance
    if (PanelConditions?.DataContext is not RegleSort r) return;
    if (!_mapChkNullable.TryGetValue(chkName, out var mapping)) return;
    if (FindName(chkName) is not CheckBox cb) return;
    var prop = typeof(RegleSort).GetProperty(mapping.prop);
    if (prop == null) return;

    _inToggleNullable = true;
    try
    {
        if (cb.IsChecked == true)
        {
            if (prop.GetValue(r) is null)
                prop.SetValue(r, (int?)mapping.defaut);
        }
        else
        {
            prop.SetValue(r, null);
        }
        var ctx = PanelConditions.DataContext;
        PanelConditions.DataContext = null;
        PanelConditions.DataContext = ctx;
        DemanderSauvegardeDebouncee();
    }
    catch (System.Exception ex)
    {
        BotDofus.Utilitaires.Journaux.Journaliseur.Erreur($"[UI] ToggleNullable({chkName}) crash", ex);
    }
    finally
    {
        _inToggleNullable = false;
    }
}
```

Effet : le flag empêche la récursion lors du toggle DataContext. Le `try/catch` capture toute autre exception future (null prop, type mismatch). **Le crash StackOverflow disparaît**.

Inconvénient : le toggle DataContext déclenche quand même des cycles `Unchecked`/`Checked` une fois, qui sont ignorés par la garde — petit gaspillage CPU mais pas critique. Solution suffisante.

#### Option C (zéro modif modèle) — Refresh ciblé sans toggle DataContext

Remplacer le toggle DataContext par un refresh explicite des bindings TextBox associés :

```csharp
// Au lieu de toggle DataContext, trouve le TextBox lié et update son binding :
var tbName = chkName.Replace("Chk", "Txt"); // convention de nommage (à instaurer)
// ... ou parcourir VisualTreeHelper pour retrouver le TextBox par Tag = mapping.prop
// et appeler BindingOperations.GetBindingExpression(tb, TextBox.TextProperty)?.UpdateTarget();
```

Plus complexe et fragile. **Non recommandé** sauf si Option A et B refusées.

### Recommandation finale

**Option B en hotfix immédiat** (10 lignes, ne touche pas au modèle, crash résolu). **Option A en suivi** (refonte propre — fait partie de la roadmap "Décideur IA refondu avec toutes les conditions SynFus" mentionnée dans CLAUDE.md).

---

## §4 — Validation post-fix

Plan de test minimal :

1. Démarrer Luffy-bot → onglet Combat → vérifier la pastille bleue sur "Equilibre" par défaut.
2. Cliquer Agressif → pastille bleue migre, ConfigCombat.Mode passe à `Agressif`, sauvegarde debouncee déclenchée (vérifier log `[ACTION]`).
3. Ajouter un sort (ex Ronce 183) à la rotation → cliquer ⓘ sur sa ligne → PanelConditions s'active.
4. Cocher `ChkDistMin` ("Min :") → CheckBox coche, TextBox passe à `1`, **aucun crash**, sauvegarde debouncee déclenchée.
5. Décocher `ChkDistMin` → TextBox vide, `DistanceMin = null` dans `peleas/<perso>.json`.
6. Répéter pour les 10 cases nullable (Dist Max, Cible PV Inf/Sup, MesPV Inf/Sup, Ennemis Min/Max, TousLesN, APartirTour) → aucune doit crasher.

---

## §5 — Annexe : pourquoi `OneWay` + handlers manuels ?

Le pattern actuel (CheckBox `IsChecked` en `OneWay` + écriture par handlers code-behind) est un workaround typique pour les modèles **sans** `INotifyPropertyChanged` qui exposent des `int?` :

- `TwoWay` direct sur `IsChecked` → faudrait un `IValueConverter.ConvertBack` qui retourne soit `null` soit une valeur — mais la valeur par défaut (1, 50, 8…) dépend de la propriété → impossible à exprimer côté converter générique.
- Le handler `Checked/Unchecked` apporte la logique conditionnelle "set défaut si null, sinon laisser tel quel" qui ne tient pas dans un binding.

Le **vrai fix architectural** est d'introduire INPC + un ViewModel `RegleSortVm` séparé qui expose des paires `(bool ActiverX, int ValeurX)` bindées TwoWay sans converter. La roadmap "UI WPF onglet Combat refondu" prévoit ça.

---

## §6 — Récapitulatif

| Bug | Cause | Sévérité | Fix conseillé |
|-----|-------|----------|---------------|
| Radio Mode blanc | Style local sans Template → template MD3 hérité avec pastille blanche | Cosmétique | XAML : ControlTemplate explicite (§3 fix 1) |
| Crash conditions | Récursion infinie via toggle DataContext + binding OneWay re-fire | **Bloquant** (StackOverflow = kill process) | Option B hotfix (§3 fix 2 option B) puis Option A à terme |

**Aucune modification de code apportée** — diagnostic uniquement, conformément à la consigne.
