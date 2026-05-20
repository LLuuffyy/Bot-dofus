using System.Collections.Generic;

namespace BotDofus.Divers.Donnees;

/// <summary>
/// Table de référence des objets interactifs récoltables Dofus Retro 1.29
/// (gfx → nom + skills possibles). Source : <c>dyshay/Bot-Dofus-Retro</c>
/// <c>Resources/otros/interactivos.xml</c> + zaap capturé en jeu Hystoria.
///
/// **Multi-skills** : certaines ressources (Lin, Chanvre…) acceptent
/// plusieurs skills selon le métier. Ex. Lin = {68 Cueillir/Alchimiste,
/// 50 Faucher/Paysan-alt}. <see cref="SkillCompatible"/> choisit le premier
/// skill que le perso possède réellement (sinon 0). Évite « il n'a pas
/// ramassé le lin » quand on filtrait sur le skill primaire dyshay.
/// </summary>
public static class CatalogueInteractifs
{
    private static readonly Dictionary<int, (string Nom, int[] Skills)> _table = new()
    {
        // ── Arbres (Couper / Ramasser, skill spécifique par essence)
        { 7500, ("Frêne",            new[] { 6 })  },
        { 7501, ("Châtaignier",      new[] { 39 }) },
        { 7502, ("Noyer",            new[] { 40 }) },
        { 7503, ("Chêne",            new[] { 10 }) },
        { 7504, ("Érable",           new[] { 37 }) },
        { 7505, ("If",               new[] { 33 }) },
        { 7506, ("Cerisier sauvage", new[] { 41 }) },
        { 7507, ("Ebène",            new[] { 34 }) },
        { 7508, ("Charme",           new[] { 38 }) },
        { 7509, ("Orme",             new[] { 35 }) },
        { 7510, ("Patate",           new[] { 42 }) },

        // ── Céréales (Faucher) — Lin/Chanvre aussi via Cueillir (Alchimiste)
        { 7511, ("Blé",     new[] { 45 }) },
        { 7512, ("Houblon", new[] { 46 }) },
        { 7513, ("Lin",     new[] { 68, 50 }) },   // Cueillir prioritaire, alt Faucher
        { 7514, ("Chanvre", new[] { 69, 54 }) },   // idem
        { 7515, ("Orge",    new[] { 53 }) },
        { 7516, ("Seigle",  new[] { 52 }) },
        { 7517, ("Avoine",  new[] { 57 }) },
        { 7518, ("Malte",   new[] { 58 }) },

        // ── Minerais (Collecter / Mineur)
        { 7520, ("Fer",                new[] { 24 }) },
        { 7521, ("Étain",              new[] { 55 }) },
        { 7522, ("Pierre de cuivre",   new[] { 25 }) },
        { 7523, ("Bronze",             new[] { 26 }) },
        { 7524, ("Manganèse",          new[] { 56 }) },
        { 7525, ("Pierre de Kobalt",   new[] { 28 }) },
        { 7526, ("Argent",             new[] { 29 }) },
        { 7527, ("Or",                 new[] { 30 }) },
        { 7528, ("Pierre de bauxite",  new[] { 31 }) },

        // ── Poissons (Pêcher)
        { 7529, ("Petits poissons (rivière)", new[] { 124 }) },
        { 7530, ("Petits poissons (mer)",     new[] { 128 }) },
        { 7531, ("Poissons (mer)",            new[] { 129 }) },
        { 7532, ("Poissons (rivière)",        new[] { 125 }) },
        { 7537, ("Grand poisson (rivière)",   new[] { 126 }) },
        { 7538, ("Grand poisson (mer)",       new[] { 130 }) },
        { 7539, ("Poissons géants (rivière)", new[] { 127 }) },
        { 7540, ("Poissons géants (mer)",     new[] { 131 }) },
        { 7544, ("Pichon",                    new[] { 136 }) },
        { 7549, ("Quaquack",                  new[] { 152 }) },

        // ── Plantes (Cueillir / Alchimiste)
        { 7533, ("Trèfle à 5 feuilles", new[] { 71 }) },
        { 7534, ("Menthe sauvage",      new[] { 72 }) },
        { 7535, ("Orchidée freyesque",  new[] { 73 }) },
        { 7536, ("Edelweiss",           new[] { 74 }) },

        // ── Divers / spécialités
        { 7541, ("Bombu",        new[] { 139 }) },
        { 7542, ("Olive",        new[] { 141 }) },
        { 7550, ("Riz",          new[] { 159 }) },
        { 7551, ("Pandouille",   new[] { 160 }) },
        { 7552, ("Bambou sacré", new[] { 158 }) },
        { 7553, ("Bambou",       new[] { 154 }) },
        { 7554, ("Bambou sombre",new[] { 155 }) },
        { 7555, ("Dolomite",     new[] { 161 }) },
        { 7556, ("Silicate",     new[] { 162 }) },
        { 7557, ("Kaliptus",     new[] { 174 }) },
        { 7005, ("Dent",         new[] { 48 })  },

        // ── Zaap — capture jeu manuel Hystoria 2026-05-20 (skill 114 « Utiliser »
        // — la doc dyshay disait 157, faux sur Hystoria).
        { 7000, ("Zaap", new[] { 114 }) },
    };

    /// <summary>
    /// Skill PRIMAIRE pour ce gfx (premier de la liste, 0 si inconnu).
    /// Utile en fallback simple ; préférer <see cref="SkillCompatible"/>
    /// dès qu'on a la liste des skills du perso pour gérer le multi.
    /// </summary>
    public static int Skill(int gfx)
        => _table.TryGetValue(gfx, out var v) && v.Skills.Length > 0 ? v.Skills[0] : 0;

    /// <summary>Tous les skills valides pour ce gfx (vide si inconnu).</summary>
    public static int[] Skills(int gfx)
        => _table.TryGetValue(gfx, out var v) ? v.Skills : System.Array.Empty<int>();

    /// <summary>
    /// Premier skill du gfx que le perso possède réellement (depuis
    /// <c>SkillsConnus</c> agrégés via JXK). 0 si aucun match → cellule
    /// non récoltable par ce perso (à filtrer). Évite de taper un Lin
    /// avec skill Faucher quand le perso n'a que Cueillir.
    /// </summary>
    public static int SkillCompatible(int gfx, IReadOnlyCollection<int> skillsPerso)
    {
        if (!_table.TryGetValue(gfx, out var v)) return 0;
        foreach (var s in v.Skills)
            if (skillsPerso.Contains(s)) return s;
        return 0;
    }

    /// <summary>Nom lisible pour ce gfx ("" = inconnu).</summary>
    public static string Nom(int gfx)
        => _table.TryGetValue(gfx, out var v) ? v.Nom : string.Empty;

    /// <summary>Vrai si ce gfx est listé comme récoltable.</summary>
    public static bool EstRecoltable(int gfx) => _table.ContainsKey(gfx);
}
