# FOCUS-OPTIONS — Choix de cible pour les règles de combat

> Référence : `Divers/Combats/IA/RegleSort.cs` — enum `FocusSort` et
> `MoteurReglesCombat.ChoisirCible()`. Le Focus détermine SUR QUI / OÙ le
> moteur applique un sort donné quand sa règle est éligible (PA OK, portée
> OK, conditions OK).

## Tableau complet

| Focus | Description | Cas d'usage typique |
|-------|-------------|----------------------|
| **EnnemiLePlusProche** | Ennemi vivant à plus courte distance Chebyshev. | Sort offensif par défaut. |
| **EnnemiLePlusFaible** | Ennemi non-invocation avec moins de PV. | Achever, focus boss. |
| **EnnemiLePlusFort** | Ennemi non-invocation avec plus de PV. | Burst contre tank. |
| **EnnemiLePlusLoin** | Ennemi le plus distant. | Sort à portée min (Tir Lourd Cra). |
| **Moi** | Le perso lui-même. | Heal soi (Ronce Apaisante), buffs. |
| **AllieLePlusBlesse** | Allié humain le plus blessé (EXCLUT moi). | Heal coéquipier ; en solo retourne null. |
| **AllieLePlusProche** | Allié humain à plus courte distance. | Buff CC, heal critique. |
| **AlliePlusGrosHeal** | Allié avec le plus de PV manquants. | Heal optimal. |
| **InvocationLaPlusBlessee** | Mes invocations blessées. | Puissance Sylvestre (Sadida). |
| **InvocationLaPlusProche** | Mes invocations proches. | Buff invoc adjacent. |
| **CelluleVide** | 1re cellule vide adjacente à moi (sans priorisation). | Legacy — préférer `CelluleAdjacenteMoi`. |
| **CelluleAdjacenteMoi** | Cellule vide adjacente avec PRIORISATION : entre moi/ennemi → opposé → fallback. | **Invocation Sadida (La Folle, Sacrifice Poupesque).** |
| **CelluleAdjacenteEnnemi** | Cellule vide adjacente à l'ennemi le + proche. | Invocation de blocage (La Bloqueuse). |

## Pièges & règles d'or

### `AllieLePlusBlesse` exclut le caster
Depuis 2026-05-21 (fix forensic-recolte), `AllieLePlusBlesse` et
`AlliePlusGrosHeal` excluent le caster pour éviter le rejet en boucle
quand on est seul (Ronce Apaisante avec porteeMin=1 sur soi → dist 0 ⇒
rejetée). **Pour heal soi-même, utiliser explicitement `Moi`.**

### `CelluleVide` vs `CelluleAdjacenteMoi`
- `CelluleVide` (legacy) : retourne la **première** cellule vide adjacente
  trouvée — ordre dépendant du parcours `carte.Cellules` (souvent haut-gauche).
  L'invocation peut spawn dans une position non stratégique.
- `CelluleAdjacenteMoi` (nouveau) : applique 3 niveaux de priorité :
  1. **Cellule entre moi et l'ennemi le + proche** (bloque le chemin).
  2. **Cellule opposée à l'ennemi** (protège l'invoc derrière moi).
  3. **Première cellule vide** (fallback si 1+2 occupées).

→ **Préférer systématiquement `CelluleAdjacenteMoi` pour les invocations.**

### `Cible vs Focus` (compat)
La propriété `RegleSort.Cible` est un alias legacy de `Focus` (mapping
restreint sur `CibleSort` obsolète). Le moteur lit `Focus`, pas `Cible`.

## Mise à jour du préset Sadida (commit 7134031)

```csharp
Ajout(182, "La Folle",            FocusSort.CelluleAdjacenteMoi,   100, 3);  // 3 poupées/tour si PA dispo
Ajout(193, "La Bloqueuse",        FocusSort.CelluleAdjacenteEnnemi, 95, 1, premierTour: true);
Ajout(183, "Ronce",               FocusSort.EnnemiLePlusFaible,     80, 2);
Ajout(195, "Larme",               FocusSort.EnnemiLePlusFaible,     75, 1);
Ajout(200, "Poison Paralysant",   FocusSort.EnnemiLePlusFort,       70, 1);
Ajout(192, "Ronce Apaisante",     FocusSort.AllieLePlusBlesse,      60, 1);  // ne self-cast plus
Ajout(197, "Puissance Sylvestre", FocusSort.InvocationLaPlusBlessee,50, 1);  // log silencieux si pas d'invoc
```

## Logs `[DECIDEUR]` rejet

Le moteur émet `[DECIDEUR] rejet règle #N 'Nom' : <raison>` en niveau
**Debogue** pour chaque règle non retenue (filtre par catégorie de log
combat). Anti-spam depuis 2026-05-21 : focus invocation/allié qui
retournent null en solo ne produisent **plus** de Diag (cas légitime —
remplit la console pour rien).
