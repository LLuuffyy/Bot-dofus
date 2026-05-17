using System;
using BotDofus.Utilitaires.Crypto;

namespace BotDofus.Divers.Cartes;

/// <summary>
/// Décompresse le champ "data" d'un paquet GDM (et du XML map cache) après déchiffrement par
/// <see cref="DechiffreurCarte"/>.
///
/// Format Dofus 1.29 : chaque cellule est encodée sur 10 caractères de l'alphabet
/// <see cref="HashCarte.Alphabet"/> (6 bits/char × 10 chars = 60 bits/cellule).
///
/// Bits packés (réf. dyshay/Bot-Dofus-Retro Map.DecompressCell) :
///   octet[0] : bit5 = active, bit2 = (object1 high bit), bit1 = (object2 high bit), bit0 = !LOS
///   octet[1] : bits 0..3 = layer ground niveau (0-15)
///   octet[2] : bits 3..5 = cellType (0=obstacle, 1=interactif, 2=teleport, 4=marchable...)
///   octet[4] : bits 2..5 = layer ground slope, bit0 = (object1 high bit)
///   octet[5..6] : layer object 1 num (low bits)
///   octet[7] : bit1 = has interactive, bit0 = (object2 high bit)
///   octet[8..9] : layer object 2 num (low bits)
///
/// La data totale = 10 × nombreCellules chars (5600 chars pour une 560-cellules standard).
/// </summary>
public static class DecompresseurMapData
{
    /// <summary>
    /// Applique les données décompressées sur les cellules d'une carte préexistante.
    /// Suppose que <see cref="Carte.Cellules"/> est déjà alloué à la bonne taille.
    /// </summary>
    public static int Appliquer(Carte carte, string mapData)
    {
        if (string.IsNullOrEmpty(mapData)) return 0;
        int nbCellules = mapData.Length / 10;
        if (nbCellules == 0) return 0;

        int nbAppliquees = Math.Min(nbCellules, carte.Cellules.Length);
        for (int i = 0; i < nbAppliquees; i++)
        {
            DecompresserCellule(carte.Cellules[i], mapData.AsSpan(i * 10, 10));
        }
        carte.SignalerRechargee();
        return nbAppliquees;
    }

    private static void DecompresserCellule(Cellule cellule, ReadOnlySpan<char> dataCellule)
    {
        Span<byte> bits = stackalloc byte[10];
        for (int i = 0; i < 10; i++)
        {
            int idx = HashCarte.IndexCar(dataCellule[i]);
            bits[i] = (byte)(idx < 0 ? 0 : idx);
        }

        // 'movement' Dofus 1.29 (0..7) : ce N'EST PAS un enum de type de cellule.
        // C'est le poids de déplacement / walkabilité : movement == 0 → infranchissable,
        // movement != 0 → marchable. Les téléports et interactifs viennent des sprites
        // de layer (cf. EstCelluleTeleport / hasInteractive), PAS de ce champ. L'ancien
        // mapping (1→Interactif, 2→Transition) classait à tort des cellules d'herbe
        // normales en transition/interactif → carte « rien à voir » avec le client.
        int movement = (bits[2] & 0b0011_1000) >> 3;        // 3 bits
        bool active = (bits[0] & 0b0010_0000) != 0;         // bit 5 oct[0]
        bool lineOfSight = (bits[0] & 0b0000_0001) != 0;    // bit 0 oct[0] (Ankama : LOS si bit=1)
        bool hasInteractive = (bits[7] & 0b0000_0010) != 0; // bit 1 oct[7]

        short layerObjet2 = (short)(((bits[0] & 0b0000_0010) << 12)
                                  + ((bits[7] & 0b0000_0001) << 12)
                                  + (bits[8] << 6)
                                  + bits[9]);
        short layerObjet1 = (short)(((bits[0] & 0b0000_0100) << 11)
                                  + ((bits[4] & 0b0000_0001) << 12)
                                  + (bits[5] << 6)
                                  + bits[6]);
        byte niveau = (byte)(bits[1] & 0b0000_1111);
        byte slope = (byte)((bits[4] & 0b0011_1100) >> 2);

        // Walkabilité Dofus 1.29 : movement == 0 OU cellule inactive → obstacle ;
        // sinon marchable. La spécialisation Zaap/Transition/Interactif est appliquée
        // ensuite à partir des sprites de layer (plus fiable que ce champ).
        TypesCellule type = (movement == 0 || !active)
            ? TypesCellule.Obstacle
            : TypesCellule.Marchable;

        cellule.Type = type;
        cellule.LayerNiveau = niveau;
        cellule.LayerSlope = slope;
        cellule.LayerObjet1 = layerObjet1;
        cellule.LayerObjet2 = layerObjet2;
        cellule.EnLigneDeVue = lineOfSight;
        cellule.IdInteractif = hasInteractive ? layerObjet2 : (short)-1;

        // Spécialisation Zaap / Transition basée sur les sprites layer connus.
        if (cellule.EstCelluleTeleport()) cellule.Type = TypesCellule.Transition;
    }
}
