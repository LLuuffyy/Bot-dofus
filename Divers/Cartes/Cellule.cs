using System;

namespace BotDofus.Divers.Cartes;

/// <summary>
/// Représente une cellule de la grille de carte Dofus Retro.
/// Les cartes standards font 14 × 17 en pattern losange, soit 560 cellules.
///
/// Le système de coordonnées Dofus est isométrique losange. Pour un mapWidth=14 :
///   loc5 = id / 27          // 27 = mapWidth*2 - 1
///   loc6 = id - loc5 * 27
///   loc7 = loc6 % 14
///   y    = loc5 - loc7
///   x    = (id - 13*y) / 14   // 13 = mapWidth - 1
/// (référence : dyshay Cell.cs)
///
/// Champs A* (coste_g/h/f, parentNode) sont mis à jour par le Pathfinder
/// pour éviter d'allouer des structures auxiliaires.
/// </summary>
public sealed class Cellule
{
    public Cellule(int identifiant, TypesCellule type, int mapWidth = 14)
    {
        Identifiant = identifiant;
        Type = type;
        (X, Y) = CalculerCoordonnees(identifiant, mapWidth);
    }

    public int Identifiant { get; }
    public TypesCellule Type { get; set; }
    public int X { get; }
    public int Y { get; }

    /// <summary>Hauteur du sol (layer ground), influence le timing déplacement et la LOS.</summary>
    public byte LayerNiveau { get; set; }

    /// <summary>Pente du sol (0=plat, 1=montée).</summary>
    public byte LayerSlope { get; set; }

    /// <summary>ID de l'objet sur layer 1 (utilisé pour détecter les cellules de téléport).</summary>
    public short LayerObjet1 { get; set; }

    /// <summary>ID de l'objet sur layer 2.</summary>
    public short LayerObjet2 { get; set; }

    /// <summary>True si en LOS (line of sight) — utilisé pour les calculs de cast de sort.</summary>
    public bool EnLigneDeVue { get; set; } = true;

    /// <summary>ID d'objet interactif sur cette cellule (-1 si aucun).</summary>
    public short IdInteractif { get; set; } = -1;

    /// <summary>
    /// Ressource interactive encore exploitable ? Mis à jour par les paquets
    /// GDF (états des éléments interactifs) : passe à false quand la ressource
    /// est récoltée/épuisée, true quand elle repousse. Permet à la carte de
    /// « s'actualiser » (vert vif = dispo, terne = épuisé) en temps réel.
    /// </summary>
    public bool RessourceDisponible { get; set; } = true;

    /// <summary>Dernier état brut reçu via GDF pour cette cellule (diagnostic).</summary>
    public int EtatInteractif { get; set; }

    public bool EstMarchable => Type == TypesCellule.Marchable || Type == TypesCellule.Interactif;
    public bool EstInteractif => Type == TypesCellule.Interactif
                               || Type == TypesCellule.Zaap
                               || Type == TypesCellule.Zaapi
                               || Type == TypesCellule.Transition;

    // -------------------------------------------------------------
    // État A* (réinitialisé par Pathfinder à chaque calcul)
    // -------------------------------------------------------------
    public int CouG { get; set; }
    public int CouH { get; set; }
    public int CouF { get; set; }
    public Cellule? ParentNoeud { get; set; }

    public void ResetA()
    {
        CouG = 0;
        CouH = 0;
        CouF = 0;
        ParentNoeud = null;
    }

    // -------------------------------------------------------------
    // Distances et géométrie
    // -------------------------------------------------------------
    public int DistanceChebyshev(Cellule autre)
        => Math.Max(Math.Abs(X - autre.X), Math.Abs(Y - autre.Y));

    public int DistanceManhattan(Cellule autre)
        => Math.Abs(X - autre.X) + Math.Abs(Y - autre.Y);

    public bool SurMemeLigne(Cellule autre) => X == autre.X || Y == autre.Y;

    /// <summary>
    /// Détermine la direction (a..h) entre cette cellule et une cellule voisine.
    /// 8 directions Dofus : 0=NE, 1=E, 2=SE, 3=S, 4=SW, 5=W, 6=NW, 7=N.
    /// Note : c'est l'index dans l'alphabet ASCII donc 'a'+0='a', 'a'+7='h'.
    /// (référence : dyshay Cell.GetCharDirection)
    /// </summary>
    public char DirectionVers(Cellule voisine)
    {
        // Mapping (signe Δx, signe Δy) → direction Dofus 0..7, DÉRIVÉ et
        // VÉRIFIÉ sur 5 segments de déplacements RÉELS du vrai client
        // (paquets GA001 capturés déchiffrés). Coordonnées via
        // CalculerCoordonnees(Largeur=15) — déjà correctes ; c'est ce
        // mapping qui était faux (dir3/7 et 0/4 inversés) → le serveur
        // rejetait tous les GA001 injectés. Opposés = +4 & vecteur négatif.
        //   (0,-1)=7  (1,-1)=0  (1,0)=1  (1,1)=2
        //   (0, 1)=3  (-1,1)=4  (-1,0)=5 (-1,-1)=6
        int dx = Math.Sign(voisine.X - X);
        int dy = Math.Sign(voisine.Y - Y);
        int dir = (dx, dy) switch
        {
            (0, -1) => 7,
            (1, -1) => 0,
            (1, 0) => 1,
            (1, 1) => 2,
            (0, 1) => 3,
            (-1, 1) => 4,
            (-1, 0) => 5,
            (-1, -1) => 6,
            _ => 0
        };
        return (char)('a' + dir);
    }

    /// <summary>True si la cellule est un pad de téléport (changement de map).</summary>
    public bool EstCelluleTeleport()
    {
        // Sprites layer Dofus 1.29 connus pour téléport
        return LayerObjet1 == 1030 || LayerObjet1 == 1029 || LayerObjet1 == 1764 || LayerObjet1 == 2298 || LayerObjet1 == 745
            || LayerObjet2 == 1030 || LayerObjet2 == 1029 || LayerObjet2 == 1764 || LayerObjet2 == 2298 || LayerObjet2 == 745;
    }

    // -------------------------------------------------------------
    // Conversion id ↔ (x,y)
    // -------------------------------------------------------------
    public static (int x, int y) CalculerCoordonnees(int identifiant, int mapWidth = 14)
    {
        int loc5 = identifiant / ((mapWidth * 2) - 1);
        int loc6 = identifiant - (loc5 * ((mapWidth * 2) - 1));
        int loc7 = loc6 % mapWidth;
        int y = loc5 - loc7;
        int x = (identifiant - ((mapWidth - 1) * y)) / mapWidth;
        return (x, y);
    }

    public static int CoordonneesVersId(int x, int y, int mapWidth = 14)
    {
        // Inverse formule Dofus
        return (mapWidth - 1) * y + x * mapWidth - (x - y) * 0;
        // Note : implémentation directe rare car les ids viennent du serveur.
        // Cette fonction n'est utile que pour navigation manuelle.
    }

    public override string ToString() => $"Cellule #{Identifiant} ({X},{Y}) {Type}";
}
