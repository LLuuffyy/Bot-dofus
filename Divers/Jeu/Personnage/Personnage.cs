using System;
using System.Collections.Generic;

namespace BotDofus.Divers.Jeu.Personnage;

/// <summary>
/// Modèle runtime du personnage actif : stats, position, inventaire, sorts.
/// Émet des événements à chaque mise à jour pour rafraîchir l'UI.
/// </summary>
public sealed class Personnage
{
    public int Identifiant { get; set; }
    public string Nom { get; set; } = string.Empty;
    public int IdClasse { get; set; }
    public int Niveau { get; set; }
    public int Sexe { get; set; }

    public int Vie { get; private set; }
    public int VieMax { get; private set; }
    public int Energie { get; private set; }
    public int EnergieMax { get; private set; }

    public int PoidsActuel { get; private set; }
    public int PoidsMax { get; private set; }

    public long XpActuelle { get; set; }
    public long XpPalierCourant { get; set; }
    public long XpPalierSuivant { get; set; }
    public long Kamas { get; set; }

    public int PointsCaracteristiques { get; set; }

    /// <summary>
    /// Compteur monotone d'OQ (loot quantité) reçus du serveur — incrémenté
    /// à chaque OQ dans TrameJeu.OnObjetQuantite. Permet à la récolte de
    /// détecter « notre cell a effectivement looté » vs « un autre joueur
    /// sur la map a pris la cell avant nous » (cf. log 11:55-11:57 :
    /// 25 GA500 → 1 seul OQ car autre joueur récolte la même map).
    /// </summary>
    public long NbLootsRecus { get; set; }
    public int PointsSorts { get; set; }

    public int PA { get; set; }
    public int PM { get; set; }

    public int? CarteCourante { get; set; }
    public int? CellulePosition { get; set; }

    public List<ObjetInventaire> Inventaire { get; } = new();

    /// <summary>Sorts appris par le personnage. Clé = ID sort, valeur = niveau (SR/SM packets).</summary>
    public Dictionary<int, int> SortsAppris { get; } = new();

    /// <summary>Niveau par métier. Clé = jobId, valeur = niveau (paquet JXK).</summary>
    public Dictionary<int, int> MetiersNiveaux { get; } = new();

    /// <summary>Skills (recettes/récoltes) par métier. Clé = jobId, valeur = liste d'idSkill (paquet JSK).</summary>
    public Dictionary<int, List<int>> MetiersSkills { get; } = new();

    /// <summary>
    /// Tous les idSkill que le personnage SAIT utiliser (récolte/craft),
    /// agrégés depuis JSK. Sert à savoir si une ressource (gfx→skill via la
    /// BDD interactifs auto-apprise) est récoltable PAR CE perso.
    /// </summary>
    public HashSet<int> SkillsConnus { get; } = new();

    /// <summary>Caractéristiques (total affiché). Clé = statId Dofus (AB) :
    /// 10=Vitalité 11=Sagesse 12=Force 13=Intelligence 14=Chance 15=Agilité.</summary>
    public Dictionary<int, int> Caracteristiques { get; } = new();

    // Ordre RÉEL Dofus Retro (vérifié sur capture : client envoie AB;10 pour
    // la Force, et le paquet As ordonne Force,Vita,Sag,Chance,Agi,Intel).
    public static readonly Dictionary<int, string> NomsCaracteristiques = new()
    {
        [10] = "Force", [11] = "Vitalité", [12] = "Sagesse",
        [13] = "Chance", [14] = "Agilité", [15] = "Intelligence"
    };

    public event EventHandler? Mis_A_Jour;
    public event EventHandler? InventaireChange;
    public event EventHandler? SortsChanges;

    public void AjouterOuMajSort(int idSort, int niveau)
    {
        SortsAppris[idSort] = niveau;
        SortsChanges?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Batch — utilisé par le parsing du paquet SL (liste complète des sorts appris).
    /// Émet UN SEUL <see cref="SortsChanges"/> à la fin pour éviter les races
    /// UI thread (Dictionary modifié pendant `.OrderBy().ToList()`) — cf. crash
    /// VuePersonnage L 84 du 21/05 06:27.
    /// </summary>
    public void AjouterPlusieursSortsAppris(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, int>> sorts)
    {
        foreach (var kv in sorts)
            SortsAppris[kv.Key] = kv.Value;
        SortsChanges?.Invoke(this, EventArgs.Empty);
    }

    public void ActualiserVie(int vie, int vieMax)
    {
        Vie = vie; VieMax = vieMax;
        Mis_A_Jour?.Invoke(this, EventArgs.Empty);
    }

    public void ActualiserEnergie(int energie, int energieMax)
    {
        Energie = energie; EnergieMax = energieMax;
        Mis_A_Jour?.Invoke(this, EventArgs.Empty);
    }

    public void ActualiserPoids(int actuel, int max)
    {
        PoidsActuel = actuel;
        // Un Ow PARTIEL (sans le max, ex. "Ow<id>;<actuel>") donne max=0 :
        // ne PAS écraser le vrai max (25575) avec 0, sinon « 814 / 0 », % pods
        // à 0 et la banque ne se déclenche jamais. On garde le dernier max
        // connu tant qu'on n'en reçoit pas un valide.
        if (max > 0) PoidsMax = max;
        Mis_A_Jour?.Invoke(this, EventArgs.Empty);
    }

    public void NotifierInventaireChange()
    {
        InventaireChange?.Invoke(this, EventArgs.Empty);
        Mis_A_Jour?.Invoke(this, EventArgs.Empty);
    }

    public double PourcentageVie => VieMax > 0 ? Math.Clamp(100.0 * Vie / VieMax, 0, 100) : 0;
    public double PourcentageEnergie => EnergieMax > 0 ? Math.Clamp(100.0 * Energie / EnergieMax, 0, 100) : 0;
    public double PourcentagePoids => PoidsMax > 0 ? Math.Clamp(100.0 * PoidsActuel / PoidsMax, 0, 100) : 0;
    public double PourcentageXp => XpPalierSuivant > XpPalierCourant
        ? Math.Clamp(100.0 * (XpActuelle - XpPalierCourant) / (XpPalierSuivant - XpPalierCourant), 0, 100)
        : 0;
}

public sealed class ObjetInventaire
{
    // UID d'instance objet Hystoria : dépasse Int32 (ex. 0x1dc7e9d08 =
    // 7 994 252 552). DOIT être long sinon overflow → objet jamais ajouté.
    public long Identifiant { get; set; }
    public int IdTemplate { get; set; }
    public int Quantite { get; set; } = 1;
    public int Position { get; set; }
    public List<string> EffetsBruts { get; } = new();
}
