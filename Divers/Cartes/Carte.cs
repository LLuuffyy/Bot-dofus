using System;
using System.Collections.Generic;
using BotDofus.Divers.Cartes.Entites;

namespace BotDofus.Divers.Cartes;

/// <summary>
/// Carte Dofus Retro : 560 cellules (14 × 20 en losange standard, ou plus
/// pour certaines maps spéciales). Stocke la topologie, les entités présentes
/// (joueurs, monstres, PNJ) et les objets interactifs.
/// </summary>
public sealed class Carte
{
    // Carte Dofus 1.29 standard : 15 large × 17 haut. Le nombre de cellules
    // d'une carte vaut W·H + (W-1)·(H-1) → 15·17 + 14·16 = 479 (et NON 560).
    public const int LargeurParDefaut = 15;
    public const int HauteurParDefaut = 17;
    public static int NombreCellules(int largeur, int hauteur)
        => largeur * hauteur + (largeur - 1) * (hauteur - 1);
    public const int NombreCellulesParDefaut = 479;

    public int Identifiant { get; }
    public int Largeur { get; set; }
    public int Hauteur { get; set; }
    public Cellule[] Cellules { get; }

    /// <summary>
    /// Entités présentes (joueurs/monstres/PNJ). MUTÉE par le thread réseau
    /// (TrameJeu / canal '-') et LUE par l'UI (VueTools/VueMapViewer) →
    /// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>
    /// obligatoire : un Dictionary plantait l'UI (« Destination array is not
    /// long enough », CopyTo concurrent). .Values renvoie ici un instantané.
    /// </summary>
    public System.Collections.Concurrent.ConcurrentDictionary<int, Entite> Entites { get; } = new();

    private Dictionary<long, Cellule>? _indexParCoords;

    public event EventHandler? Rechargee;

    public Carte(int identifiant)
        : this(identifiant, LargeurParDefaut, HauteurParDefaut, NombreCellulesParDefaut)
    {
    }

    /// <summary>
    /// Construit une carte avec ses dimensions RÉELLES (lues dans le SWF).
    /// <paramref name="largeur"/> pilote la conversion id → (x,y) de chaque
    /// cellule : une largeur fausse décale toute la géométrie.
    /// </summary>
    public Carte(int identifiant, int largeur, int hauteur, int nombreCellules)
    {
        Identifiant = identifiant;
        Largeur = largeur > 0 ? largeur : LargeurParDefaut;
        Hauteur = hauteur > 0 ? hauteur : HauteurParDefaut;
        if (nombreCellules <= 0) nombreCellules = NombreCellules(Largeur, Hauteur);

        Cellules = new Cellule[nombreCellules];
        for (int i = 0; i < nombreCellules; i++)
        {
            Cellules[i] = new Cellule(i, TypesCellule.Marchable, Largeur);
        }

        MarquerBordsCommeTransitions();
    }

    public Cellule? Obtenir(int identifiantCellule)
        => identifiantCellule >= 0 && identifiantCellule < Cellules.Length
            ? Cellules[identifiantCellule]
            : null;

    /// <summary>
    /// Recherche O(1) d'une cellule par ses coordonnées (x, y). L'index est construit
    /// paresseusement à la première utilisation et invalidé quand <see cref="AppliquerMouvements"/>
    /// est rappelé (utile pour le pathfinder qui interroge 8 voisins par expansion).
    /// </summary>
    public Cellule? ObtenirParCoords(int x, int y)
    {
        if (_indexParCoords == null)
        {
            _indexParCoords = new Dictionary<long, Cellule>(Cellules.Length);
            foreach (var c in Cellules)
            {
                if (c == null) continue;
                _indexParCoords[CleCoords(c.X, c.Y)] = c;
            }
        }
        return _indexParCoords.TryGetValue(CleCoords(x, y), out var trouve) ? trouve : null;
    }

    private static long CleCoords(int x, int y) => ((long)x << 32) ^ (uint)y;

    /// <summary>
    /// Retourne les voisins directs d'une cellule. En Dofus Retro les 4 directions
    /// visuelles (haut, bas, gauche, droite) correspondent aux décalages d'id
    /// ±1 et ±14 sur une grille linéaire simplifiée. Les 4 diagonales sont ±13
    /// et ±15. Pour le pathfinding principal on se limite aux 4 orthogonaux.
    /// </summary>
    public IEnumerable<Cellule> Voisins(Cellule centre)
    {
        foreach (var decalage in new[] { 1, -1, Largeur, -Largeur })
        {
            var id = centre.Identifiant + decalage;
            // Évite que id+1 déborde sur la ligne suivante.
            if (Math.Abs(decalage) == 1
                && (centre.Identifiant / Largeur) != (id / Largeur)) continue;
            var c = Obtenir(id);
            if (c != null) yield return c;
        }
    }

    /// <summary>Notifie les abonnés (vues UI) que le contenu de la carte a changé après un rechargement de données.</summary>
    public void SignalerRechargee()
    {
        _indexParCoords = null;
        Rechargee?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Appliquer les types de cellules depuis la chaîne de mouvement décodée.</summary>
    public void AppliquerMouvements(ReadOnlySpan<int> codesMouvement)
    {
        int n = Math.Min(codesMouvement.Length, Cellules.Length);
        for (int i = 0; i < n; i++)
        {
            Cellules[i].Type = codesMouvement[i] switch
            {
                0 => TypesCellule.Obstacle,
                1 => TypesCellule.Marchable,
                2 => TypesCellule.LignDeVueSeule,
                4 => TypesCellule.Interactif,
                _ => TypesCellule.Marchable
            };
        }
        _indexParCoords = null;
        Rechargee?.Invoke(this, EventArgs.Empty);
    }

    private void MarquerBordsCommeTransitions()
    {
        if (Cellules.Length == 0) return;

        var minX = Cellules.Min(c => c.X);
        var maxX = Cellules.Max(c => c.X);
        var minY = Cellules.Min(c => c.Y);
        var maxY = Cellules.Max(c => c.Y);

        foreach (var cellule in Cellules)
        {
            if (cellule.X == minX || cellule.X == maxX || cellule.Y == minY || cellule.Y == maxY)
            {
                cellule.Type = TypesCellule.Transition;
            }
        }
    }
}
