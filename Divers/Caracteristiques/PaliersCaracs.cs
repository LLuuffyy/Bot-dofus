namespace BotDofus.Divers.Caracteristiques;

/// <summary>
/// IDs des caractéristiques sur Dofus 1.29 (paquet AB).
/// Confirmé par dyshay/SynFus + capture user en jeu.
/// </summary>
public static class IdsCaracs
{
    public const int Vitalite = 10;
    public const int Sagesse = 11;
    public const int Force = 12;
    public const int Intelligence = 13;
    public const int Chance = 14;
    public const int Agilite = 15;
}

/// <summary>
/// Paliers de coût en points par +1 statistique sur Dofus 1.29.
///
/// <para>Force / Intelligence / Chance / Agilité :</para>
/// <list type="bullet">
///   <item>0–100 stat : 1 point dépensé = +1 stat</item>
///   <item>100–200 stat : 2 points dépensés = +1 stat</item>
///   <item>200–300 stat : 3 points = +1 stat</item>
///   <item>300+ stat : 4 points = +1 stat (jusqu'à infini)</item>
/// </list>
///
/// <para>Vitalité : 1 point = +1 vita (constant).</para>
/// <para>Sagesse : 3 points = +1 sagesse (constant, plus cher).</para>
/// </summary>
public static class PaliersCaracs
{
    /// <summary>
    /// Retourne le coût en points pour ajouter +1 à une carac selon la valeur actuelle.
    /// </summary>
    public static int CoutPourUnPoint(int statId, int valeurActuelle)
    {
        return statId switch
        {
            IdsCaracs.Vitalite => 1,
            IdsCaracs.Sagesse => 3,
            // Force / Int / Chance / Agi : paliers progressifs.
            IdsCaracs.Force or IdsCaracs.Intelligence
                or IdsCaracs.Chance or IdsCaracs.Agilite
                => valeurActuelle switch
                {
                    < 100 => 1,
                    < 200 => 2,
                    < 300 => 3,
                    _ => 4
                },
            _ => 1
        };
    }

    /// <summary>
    /// Calcule combien de stats peuvent être ajoutées avec <paramref name="pointsDispo"/>
    /// en commençant à <paramref name="valeurActuelle"/>. Retourne le couple
    /// (statsAjoutees, pointsConsommes).
    /// </summary>
    public static (int statsAjoutees, int pointsConsommes) CombienAvecBudget(
        int statId, int valeurActuelle, int pointsDispo)
    {
        if (pointsDispo <= 0) return (0, 0);

        int statsAjoutees = 0;
        int pointsConsommes = 0;
        int courante = valeurActuelle;

        while (pointsConsommes < pointsDispo)
        {
            int cout = CoutPourUnPoint(statId, courante);
            if (pointsConsommes + cout > pointsDispo) break;
            pointsConsommes += cout;
            statsAjoutees++;
            courante++;
        }
        return (statsAjoutees, pointsConsommes);
    }
}
