using System;
using System.Collections.Generic;
using System.Linq;
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

/// <summary>
/// NL : famille « acteur » d'Abrak (protocole custom v1.48, REMPLACE le GM
/// vanilla pour l'IDENTITÉ). Format spawn observé :
/// <c>NLK+401774;Aerawiol;6;31;3;-1;-1;-1;,,,,;0;5;0;0;0;1daa6d13b~27df~1~~32e#0#0#620,…</c>
/// soit : +id ; nom ; niveau ; skin ; sexe ; c1 ; c2 ; c3 ; … ; equip.
///
/// IMPORTANT : Abrak NE met PAS la cellule ici (contrairement au GM 1.29).
/// La position/déplacement des acteurs transite par le canal CHIFFRÉ `-`
/// (anti-triche Abrak). On récupère donc l'identité (nom/niveau) mais pas
/// la position tant que ce canal n'est pas reversé.
/// </summary>
public sealed class MessageActeurAbrak : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "NL";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;

    public readonly record struct Acteur(int Id, string Nom, int Niveau);
    public List<Acteur> Spawns { get; } = new();
    public List<int> Despawns { get; } = new();

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        // charge = ce qui suit "NL" : souvent "K+<id>;<nom>;<niveau>;…" ;
        // plusieurs acteurs possibles séparés par '|'.
        var corps = charge.StartsWith("K", StringComparison.Ordinal) ? charge[1..] : charge;
        foreach (var bloc in corps.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            if (bloc.Length < 2) continue;
            var op = bloc[0];
            var champs = bloc[1..].Split(';');
            if (!int.TryParse(champs.ElementAtOrDefault(0), out var id)) continue;

            if (op == '-')
            {
                Despawns.Add(id);
            }
            else // '+' (ou autre = ajout)
            {
                var nom = champs.ElementAtOrDefault(1) ?? string.Empty;
                int.TryParse(champs.ElementAtOrDefault(2), out var niveau);
                Spawns.Add(new Acteur(id, nom, niveau));
            }
        }
    }
}

/// <summary>Nx : despawn d'un acteur Abrak (« Nx&lt;id&gt; »).</summary>
public sealed class MessageActeurAbrakRetrait : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "Nx";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public int Identifiant { get; private set; }
    public override void Desserialiser(string charge)
    {
        Charge = charge;
        int.TryParse(charge, out var id);
        Identifiant = id;
    }
}

/// <summary>
/// GTM (Abrak) : liste des COMBATTANTS avec leur cellule. EN CLAIR (le combat
/// n'est pas dans le canal chiffré). Format réel (validé sur capture live),
/// combattants séparés par '|' : <c>id;vivant;PV;PA;PM;CELLULE;;PVMax</c> — ex.
/// <c>401781;0;80;6;3;54;;80</c> (vivant, 80/80 PV, 6 PA, 3 PM, cellule 54),
/// <c>-1;0;12;4;3;414;;12</c> (monstre cellule 414, 4 PA 3 PM),
/// <c>-1;1</c> (forme courte = combattant mort).
///
/// Heuristique d'équipe (PvM Incarnam) : id &lt; 0 = monstre/ennemi,
/// id &gt; 0 = joueur/allié. C'est CE paquet qui donne enfin les positions
/// des entités pour l'IA combat.
/// </summary>
public sealed class MessageCombattantsAbrak : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "GTM";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;

    public readonly record struct Combattant(int Id, bool Vivant, int Cellule, int Pv, int PvMax, int Pa, int Pm);
    public List<Combattant> Combattants { get; } = new();

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        foreach (var bloc in charge.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var f = bloc.Split(';');
            if (f.Length < 2 || !int.TryParse(f[0], out var id)) continue;

            // Forme courte "id;1" = mort. f[1] == "0" => vivant.
            bool vivant = f[1] == "0";
            int cell = 0, pv = 0, pvMax = 0, pa = 0, pm = 0;
            if (f.Length >= 8)
            {
                int.TryParse(f[2], out pv);
                int.TryParse(f[3], out pa);   // PA (ex. 6)
                int.TryParse(f[4], out pm);   // PM (ex. 3)
                int.TryParse(f[5], out cell);
                int.TryParse(f[7], out pvMax);
            }
            Combattants.Add(new Combattant(id, vivant, cell, pv, pvMax, pa, pm));
        }
    }
}

/// <summary>
/// GTS (Abrak) : « c'est le tour de &lt;id&gt; ». Format <c>id|timer|numTour</c>
/// (ex. <c>GTS401770|45000|1</c>). Remplace le GT vanilla (id nu) que le
/// parseur générique ne savait pas lire pour Abrak.
/// </summary>
public sealed class MessageTourCombatAbrak : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "GTS";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public int IdentifiantCombattant { get; private set; }
    public int NumeroTour { get; private set; }
    /// <summary>false pour les paquets GTSX (liste de sorts) qui partagent le
    /// préfixe "GTS" mais ne sont PAS un changement de tour.</summary>
    public bool EstTour { get; private set; }

    /// <summary>
    /// <c>true</c> si le paquet est un <c>GTSX&lt;idMaster&gt;;&lt;idPersoLié&gt;;0;1;0;&lt;5stats&gt;</c>
    /// — signal exclusif mode héros sur Abrak (cf. docs/PROTOCOLE-MODE-HEROS-ABRAK.md).
    /// </summary>
    public bool EstGTSX { get; private set; }
    /// <summary>Master du groupe héros (= notre perso connecté). Renseigné si <see cref="EstGTSX"/>.</summary>
    public int IdMaster { get; private set; }
    /// <summary>Perso lié auquel ce GTSX initialise les buffs. Renseigné si <see cref="EstGTSX"/>.</summary>
    public int IdPersoLie { get; private set; }
    /// <summary>Charge brute du GTSX (sans le préfixe "X") — diag/debug.</summary>
    public string DonneesGTSX { get; private set; } = string.Empty;

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        // GTSX<idMaster>;<idLié>;0;1;0;<5stats> = signal mode héros, PAS un tour.
        if (charge.StartsWith("X", StringComparison.Ordinal))
        {
            EstTour = false;
            EstGTSX = true;
            DonneesGTSX = charge[1..];
            var champs = DonneesGTSX.Split(';');
            if (champs.Length >= 2
                && int.TryParse(champs[0], out var idM)
                && int.TryParse(champs[1], out var idL))
            {
                IdMaster = idM;
                IdPersoLie = idL;
            }
            return;
        }
        var p = charge.Split('|');
        if (!int.TryParse(p.ElementAtOrDefault(0), out var cid))
        {
            EstTour = false;
            return;
        }
        IdentifiantCombattant = cid;
        int.TryParse(p.ElementAtOrDefault(2), out var nt);
        NumeroTour = nt;
        EstTour = true;
    }
}

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
/// JSK : liste des SKILLS (récoltes/recettes) du personnage, groupés par
/// métier. Format : <c>&lt;charId&gt;_&lt;jobId&gt;;&lt;skill&gt;~a~b~c~d,&lt;skill&gt;~…|&lt;jobId&gt;;…</c>
/// (ex. <c>401770_64;165~3~0~0~1,167~3~0~0~1|2;6~1~2~0~11900,101~2~0~0~50|…</c>).
/// Un bloc <c>1;</c> = métier connu sans skill listé.
/// </summary>
public sealed class MessageMetiersSkills : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "JSK";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;

    /// <summary>jobId → liste d'idSkill.</summary>
    public Dictionary<int, List<int>> Metiers { get; } = new();

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var us = charge.IndexOf('_');
        var corps = us >= 0 ? charge[(us + 1)..] : charge;
        foreach (var bloc in corps.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var pv = bloc.IndexOf(';');
            if (pv < 0) continue;
            if (!int.TryParse(bloc[..pv], out var jobId)) continue;
            var liste = new List<int>();
            foreach (var sk in bloc[(pv + 1)..].Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var tete = sk.Split('~')[0];
                if (int.TryParse(tete, out var idSkill) && idSkill > 0)
                    liste.Add(idSkill);
            }
            Metiers[jobId] = liste;
        }
    }
}

/// <summary>
/// JXK : niveau / XP par métier. Format :
/// <c>&lt;charId&gt;~&lt;jobId&gt;;&lt;niveau&gt;;&lt;xp&gt;;&lt;xpMin&gt;;&lt;xpMax&gt;;|&lt;jobId&gt;;…</c>
/// (ex. <c>401770~64;1;0;0;50;|2;1;0;0;50;|…</c>).
/// </summary>
public sealed class MessageMetiersXp : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "JXK";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;

    /// <summary>jobId → niveau.</summary>
    public Dictionary<int, int> Niveaux { get; } = new();

    public override void Desserialiser(string charge)
    {
        Charge = charge;
        var tilde = charge.IndexOf('~');
        var corps = tilde >= 0 ? charge[(tilde + 1)..] : charge;
        foreach (var bloc in corps.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var champs = bloc.Split(';');
            if (champs.Length < 2) continue;
            if (int.TryParse(champs[0], out var jobId)
                && int.TryParse(champs[1], out var niveau))
            {
                Niveaux[jobId] = niveau;
            }
        }
    }
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
            // Le PRÉFIXE est le discriminant fiable du type d'entité :
            //   '+' = acteur (joueur si id>0, PNJ si id<0)
            //   '~' = GROUPE DE MONSTRES (toujours — jamais un joueur/PNJ)
            //   '-' = despawn   '=' = update
            // Avant : '~' était mappé sur Spawn comme '+' → monstres/PNJ
            // confondus (heuristique fragile en aval). Désormais distinct.
            var operation = bloc[0] switch
            {
                '+' => OperationGM.Spawn,
                '~' => OperationGM.MonstreGroupe,
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
    /// <summary>Préfixe '+' : acteur (joueur si id&gt;0, PNJ si id&lt;0).</summary>
    Spawn,
    /// <summary>Préfixe '~' : groupe de monstres (jamais joueur/PNJ).</summary>
    MonstreGroupe,
    /// <summary>Préfixe '-' : disparition.</summary>
    Despawn,
    /// <summary>Préfixe '=' : mise à jour.</summary>
    Update
}
