using System;
using System.Collections.Generic;
using BotDofus.Commun.Reseau;

namespace BotDofus.Commun.Messages.VersClient.Jeu;

// =====================================================================
// Messages additionnels VersClient observés dans Hystoria 1.29 :
// - GKK : reçu après une demande de mouvement, pas vraiment une réponse
//   (souvent c'est la requête CLIENT puis BN serveur qui acquittent)
// - GM  : Game Movement / Map (entités) — gros paquet multi-entités
// - GDF : finalize map data (vide)
// - GDK : map data ok / keyframe (vide)
// - fC  : fight count sur la carte
// =====================================================================

/// <summary>fC : nombre de combats actifs sur la carte courante.</summary>
public sealed class MessageNombreCombats : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "fC";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public int NombreCombats { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        int.TryParse(charge, out var n);
        NombreCombats = n;
    }
}

/// <summary>GDF : signal "fin de chargement de carte" (charge utile vide).</summary>
public sealed class MessageDonneesCarteFin : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "GDF";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public override void Desserialiser(string charge) { Charge = charge; }
}

/// <summary>GDK : keyframe de carte chargée — l'UI peut afficher la carte.</summary>
public sealed class MessageDonneesCarteKeyframe : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "GDK";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public override void Desserialiser(string charge) { Charge = charge; }
}

/// <summary>
/// GM : Game Movement / Map. Plusieurs sous-formats :
///   GM|+&lt;cell&gt;;&lt;type&gt;;...   spawn/update d'une entité (un ou plusieurs séparés par "|+")
///   GM|-&lt;id&gt;                  despawn d'une entité
///   GM|=&lt;cell&gt;;...             ?
///
/// Format spawn observé pour un joueur :
/// <c>+36;3;0;11125;Toutan-kamou;5;50^100;0;0,0,0,11221;ffffff;ffffff;320000;,986,98d,330c,cf856;0;;;;;0;;0;</c>
/// soit : cell ; type ; param1 ; entityId ; nom ; niveau ; vieRatio ; gender ; couleurs1 ; couleur2 ; couleur3 ; accent ; equip ; ...
///
/// Format spawn observé pour un groupe de monstres :
/// <c>+126;1;200;-14;275,273,276;-3;1172^106,1174^102,1173^104;36,32,34;...</c>
/// soit : cell ; type=1 (monstre) ; param ; param ; idMonstres,... ; param ; gabarits^look,... ; niveaux,...
/// </summary>
public sealed class MessageMouvementCarte : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "GM";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;

    public List<EntreeGM> Entrees { get; private set; } = new();

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        Entrees = new List<EntreeGM>();

        // Le payload commence par '|' (déjà mangé par le préfixe), suivi de plusieurs
        // entrées concaténées chacune introduite par '+' (spawn), '-' (despawn) ou '='.
        // Ex : "+36;3;...|+126;1;..."
        // Pour parser, on split sur '|' (sans le tout premier qui est vide).
        var blocs = charge.Split('|', StringSplitOptions.RemoveEmptyEntries);
        foreach (var bloc in blocs)
        {
            if (bloc.Length == 0) continue;
            var operation = bloc[0] switch
            {
                '+' => OperationGM.Spawn,
                '-' => OperationGM.Despawn,
                '=' => OperationGM.Update,
                _ => OperationGM.Spawn
            };
            var corps = bloc[1..];

            int entiteId = 0;
            int cellule = 0;

            if (operation == OperationGM.Despawn)
            {
                int.TryParse(corps, out entiteId);
            }
            else
            {
                var champs = corps.Split(';');
                if (champs.Length > 0) int.TryParse(champs[0], out cellule);
                if (champs.Length > 3) int.TryParse(champs[3], out entiteId);
            }

            Entrees.Add(new EntreeGM(operation, entiteId, cellule, bloc));
        }
    }

    public readonly record struct EntreeGM(OperationGM Operation, int IdentifiantEntite, int Cellule, string ContenuBrut);
}

/// <summary>Type d'événement porté par un sous-bloc de <see cref="MessageMouvementCarte"/>.</summary>
public enum OperationGM
{
    Spawn,
    Despawn,
    Update
}
