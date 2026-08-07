namespace BotDofus.Divers.Combats.Combattants;

/// <summary>Participant d'un combat : allié, ennemi ou invocation.</summary>
public class Combattant
{
    public int Identifiant { get; set; }
    public string Nom { get; set; } = string.Empty;
    public int Equipe { get; set; }         // 0 = alliés, 1 = ennemis, 2 = spectateur
    public int CellulePosition { get; set; }
    public int PV { get; set; }
    public int PVMax { get; set; }
    public int PA { get; set; }
    public int PM { get; set; }
    public bool EstInvocation { get; set; }
    public bool EstMort { get; set; }
}

/// <summary>Allié (personnage du bot, ou membre de groupe).</summary>
public sealed class CombattantAllie : Combattant
{
    public int IdClasse { get; set; }
    public int Niveau { get; set; }
}

/// <summary>Monstre en combat.</summary>
public sealed class CombattantMonstre : Combattant
{
    public int IdGabarit { get; set; }
    public int NiveauGabarit { get; set; }
}
