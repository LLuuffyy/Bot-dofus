# DIAG — `CmbSort` vide & ComboBox mal stylées (onglet Combat)

Screenshot user : `Desktop\screen\Capture d'écran 2026-05-20 220505.png`
Branche : `claude/dofus-bot-continue-IUIN3`

---

## §1 État actuel de `CmbSort` — pourquoi vide ?

**Wiring code** (`BotDofus.Wpf/Vues/VueCombat.xaml.cs`) :
- L37 : `CmbSort.ItemsSource = SortsAppris;` ✅ binding bien posé au ctor.
- L153-165 : `RafraichirSortsAppris()` lit `_contexte.EtatJeu.Personnage.SortsAppris`, vide+remplit `SortsAppris`.
- L58 : `_personnageLie.SortsChanges += OnSortsChanges;` ✅ event câblé.
- L106-107 : `OnSortsChanges` → `Dispatcher.BeginInvoke(RafraichirSortsAppris)`.

**Bugs trouvés (3, cumulatifs) :**

### Bug A — `SortsChanges` n'est JAMAIS déclenché (CRITIQUE)
`Commun/Frames/TrameJeu.cs` L53-58 :
```csharp
Ecouter<MessageListeSorts>(msg => {
    foreach (var kv in msg.Sorts) _etat.Personnage.SortsAppris[kv.Key] = kv.Value;  // ← écriture DIRECTE sur le Dictionary
    Journaliseur.Info($"[SORTS] {msg.Sorts.Count} sort(s) ...");
});
```
La méthode `Personnage.AjouterOuMajSort(id, niv)` (L83-87) qui invoque `SortsChanges?.Invoke(...)` existe mais n'est **appelée nulle part dans le codebase** (cf. grep). Résultat : la `VueCombat` ne reçoit jamais l'event, donc si SL arrive APRÈS sélection du compte, `CmbSort` reste vide pour toujours.

### Bug B — `OnPaquetRecu` filtre `SL` mais ne rafraîchit PAS les sorts
`VueCombat.xaml.cs` L88-104 : le handler `OnPaquetRecu` whitelist `SL`/`SM`/`SR` (L92-94) **mais le callback Dispatcher (L99-103) n'appelle que `Rafraichir()` + `RafraichirCombatLive()`** — pas `RafraichirSortsAppris()`. Even si on bypass Bug A, la vue ne se mettra pas à jour.

### Bug C — `SM` / `SR` ne sont pas enregistrés dans `FabriqueMessages`
`Commun/Messages/FabriqueMessages.cs` (grep `"SM"|"SR"`) → 0 match. Seul `SL` (login full-scan) est routé. Tout level-up de sort en cours de jeu (SM `<id>~<niv>`, SR `<id>~<niv>~<pos>`) est ignoré → un sort appris à chaud n'apparaîtra jamais dans `CmbSort`.

### Conséquence visuelle (screenshot)
La ComboBox `CmbSort` est vide tant que le `Personnage.SortsAppris` est vide au moment du `Lier()`. Le user voit un champ "SORT" sans dropdown utilisable, et le bouton "Ajouter a la rotation" ne fait rien (L196 : `CmbSort.SelectedItem is not SortItemVm sortVm` → return).

---

## §2 Code exact à modifier

| Fichier | Ligne(s) | Modif |
|---------|----------|-------|
| `Divers/Jeu/Personnage/Personnage.cs` | L83-87 (existant) | Garder `AjouterOuMajSort`, OK. |
| `Commun/Frames/TrameJeu.cs` | **L53-58** | Remplacer l'écriture directe par appel à `AjouterOuMajSort(kv.Key, kv.Value)` dans la boucle. Trigger `SortsChanges` une fois la boucle terminée. |
| `Commun/Messages/FabriqueMessages.cs` | après L118 | Enregistrer `SM` et `SR` (level-up sort runtime). |
| `Commun/Messages/VersClient/Authentification/MessagesAuthentification.cs` | nouveau | Créer `MessageSortMisAJour` (`SM`) + `MessageSortRange` (`SR`). |
| `Commun/Frames/TrameJeu.cs` | ajouter listener | `Ecouter<MessageSortMisAJour>(m => _etat.Personnage.AjouterOuMajSort(m.IdSort, m.Niveau));` idem `SR`. |
| `BotDofus.Wpf/Vues/VueCombat.xaml.cs` | **L99-103** | Ajouter `RafraichirSortsAppris();` dans le Dispatcher (filet de sécurité quand SL arrive après `Lier()`). |

---

## §3 Contrôles UI encore mal stylés (depuis screenshot)

Sur le screenshot (UI Combat ouverte, dropdown CmbMethode déployé) :

| Contrôle | État | Cause / fix |
|----------|------|-------------|
| **`CmbSort`** (SORT) | vide (pas d'items, sélection impossible) | §1 (data). Style OK une fois peuplé. |
| **`CmbCible`** (FOCUS) | OK, items visibles | rien |
| **`CmbMethode`** (METHODE) | OK, dropdown ouvert lisible | rien |
| **`CmbStrategie`** (TACTIQUE GLOBALE, bas de page) | **VIDE / pas de sélection visible** | `InitialiserModeEtTactique` L413 : `CmbStrategie.SelectedIndex = (int)cfg.Strategie;` — si `cfg.Strategie` n'a pas été persistée ou hors range [0..5], SelectedIndex peut rester à -1 (rien affiché). Forcer fallback. |
| **`CmbElement`** (Avancé) | OK fond sombre, visible | rien |
| TextBox `TxtFoisTour`, `TxtFoisCible`, `TxtPriorite` | OK fond sombre | rien |
| GroupBox Distance/Cible/Joueur/Situation/Avancé | header presque invisible (gris sur fond gris) | header GroupBox WPF default n'hérite pas du `Foreground` du Style — `ConditionGroupBoxStyle` ne style que le contenu, pas le header. Ajouter `HeaderTemplate` qui force `Foreground="#ECEFF5"`. |
| Combobox `CmbElement` dropdown items | enum `ElementSort` brut (`Aucun`, `Force`…) sans accent | acceptable v1, à `ItemTemplate` plus tard. |

---

## §4 Patches précis prêt-à-coller

### Patch 1 — `TrameJeu.cs` (Bug A)
```csharp
// Commun/Frames/TrameJeu.cs L53-58 — REMPLACER
Ecouter<BotDofus.Commun.Messages.VersClient.Authentification.MessageListeSorts>(msg =>
{
    foreach (var kv in msg.Sorts)
        _etat.Personnage.AjouterOuMajSort(kv.Key, kv.Value);   // ← déclenche SortsChanges N fois
    Journaliseur.Info($"[SORTS] {msg.Sorts.Count} sort(s) scanné(s) : "
        + string.Join(", ", msg.Sorts.Select(s => $"#{s.Key} niv{s.Value}")));
});
```
*Note : N invocations OK, le handler UI debounce via Dispatcher. Alternative : ajouter une méthode `Personnage.RemplacerSortsAppris(Dictionary)` qui fait un seul event.*

### Patch 2 — `VueCombat.xaml.cs` (Bug B, filet de sécurité)
```csharp
// VueCombat.xaml.cs L99-103 — REMPLACER le corps du Dispatcher
Dispatcher.BeginInvoke(() =>
{
    Rafraichir();
    RafraichirSortsAppris();   // ← ajout : couvre cas SL/SM/SR reçu hors event Personnage
    RafraichirCombatLive();
});
```

### Patch 3 — `CmbStrategie` fallback (problème ComboBox vide bas de page)
```csharp
// VueCombat.xaml.cs L413 — REMPLACER
int idx = (int)cfg.Strategie;
CmbStrategie.SelectedIndex = (idx >= 0 && idx < CmbStrategie.Items.Count) ? idx : 0;
```

### Patch 4 — header GroupBox lisible (XAML)
```xml
<!-- VueCombat.xaml L15-22 — REMPLACER le Style ConditionGroupBoxStyle -->
<Style x:Key="ConditionGroupBoxStyle" TargetType="GroupBox">
    <Setter Property="Foreground" Value="#ECEFF5"/>
    <Setter Property="BorderBrush" Value="{DynamicResource BorderSoftBrush}"/>
    <Setter Property="Background" Value="{DynamicResource SurfaceBrushSoft}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="Padding" Value="8"/>
    <Setter Property="FontSize" Value="11"/>
    <Setter Property="HeaderTemplate">
        <Setter.Value>
            <DataTemplate>
                <TextBlock Text="{Binding}" Foreground="#ECEFF5"
                           FontWeight="SemiBold" FontSize="11"/>
            </DataTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

### Patch 5 (optionnel) — enregistrer `SM`/`SR` (level-up sort runtime)
À faire dans un commit séparé (création de 2 nouveaux MessageDofus + listener). Non bloquant pour le bug `CmbSort` vide — couvert par Patches 1+2.

---

## Test plan (après patches 1+2+3+4)
1. Lancer Luffy-bot, connecter Beiloddurul (compte `test`).
2. Sélectionner le compte dans la liste → vérifier que **CmbSort** affiche les 7 sorts Sadida (`[183] Ronce (niv.5)`, etc.) dans le dropdown.
3. Sélectionner Ronce + Focus "Ennemi le plus proche" + cliquer "Ajouter a la rotation" → la règle apparaît dans la liste "SORTS CONFIGURES".
4. Vérifier que **CmbStrategie** a un item sélectionné dès l'ouverture (pas vide).
5. Cliquer ⓘ sur la règle → panel "CONDITIONS" devient interactif, headers GroupBox lisibles.
