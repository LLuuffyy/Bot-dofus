using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BotDofus.Divers.Jeu.Personnage;
using BotDofus.Divers.Scripts.Api;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Caracteristiques;

/// <summary>
/// Calcule et exécute la distribution des points de caractéristique
/// disponibles selon la répartition configurée.
/// </summary>
/// <remarks>
/// Pipeline :
/// <list type="number">
///   <item>Au level-up détecté dans TrameJeu, on récupère Personnage.PointsCaracteristiques</item>
///   <item>On répartit ces points selon les pourcentages de <see cref="ConfigRepartitionCaracs"/></item>
///   <item>Pour chaque carac avec un budget &gt; 0, on calcule combien de stats on peut acheter
///         en tenant compte des paliers progressifs (cf. <see cref="PaliersCaracs"/>)</item>
///   <item>On envoie <c>AB&lt;idPerso&gt;;&lt;statId&gt;;&lt;nb&gt;</c> via <see cref="ApiBot.MonterCaracteristiqueAsync"/></item>
/// </list>
/// </remarks>
public sealed class DistributeurCaracs
{
    private readonly ApiBot _api;
    private readonly Personnage _perso;
    private readonly ConfigRepartitionCaracs _cfg;

    public DistributeurCaracs(ApiBot api, Personnage perso, ConfigRepartitionCaracs cfg)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _perso = perso ?? throw new ArgumentNullException(nameof(perso));
        _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
    }

    /// <summary>
    /// Calcule le plan de distribution sans envoyer de paquet. Retourne la liste
    /// des (statId, nbStatsAjoutees, pointsConsommes) pour debug et mode Preview.
    /// </summary>
    public List<(int statId, int statsAjoutees, int pointsConsommes)> Planifier()
    {
        var plan = new List<(int, int, int)>();
        if (!_cfg.EstValide) return plan;
        if (_perso.PointsCaracteristiques <= 0) return plan;

        int totalDispo = _perso.PointsCaracteristiques;
        var budgets = new Dictionary<int, int>
        {
            [IdsCaracs.Vitalite] = totalDispo * _cfg.PctVitalite / 100,
            [IdsCaracs.Sagesse] = totalDispo * _cfg.PctSagesse / 100,
            [IdsCaracs.Force] = totalDispo * _cfg.PctForce / 100,
            [IdsCaracs.Intelligence] = totalDispo * _cfg.PctIntelligence / 100,
            [IdsCaracs.Chance] = totalDispo * _cfg.PctChance / 100,
            [IdsCaracs.Agilite] = totalDispo * _cfg.PctAgilite / 100,
        };
        // Le reste de la division entière va sur la 1ère carac ayant un %>0.
        int budgetTotal = 0;
        foreach (var v in budgets.Values) budgetTotal += v;
        int reste = totalDispo - budgetTotal;
        if (reste > 0)
        {
            foreach (var kv in budgets)
            {
                if (kv.Value > 0) { budgets[kv.Key] += reste; break; }
            }
        }

        foreach (var (statId, budget) in budgets)
        {
            if (budget <= 0) continue;
            int valeurActuelle = _perso.Caracteristiques.TryGetValue(statId, out var v) ? v : 0;
            var (statsAjoutees, pointsConsommes) = PaliersCaracs.CombienAvecBudget(statId, valeurActuelle, budget);
            if (statsAjoutees > 0)
                plan.Add((statId, statsAjoutees, pointsConsommes));
        }
        return plan;
    }

    /// <summary>
    /// Exécute la distribution. En mode Preview, log uniquement sans envoyer.
    /// En mode Automatique, envoie les paquets AB.
    /// </summary>
    public async Task DistribuerAsync(System.Threading.CancellationToken ct = default)
    {
        if (_cfg.Mode == ModeDistribCaracs.Manuel) return;
        var plan = Planifier();
        if (plan.Count == 0)
        {
            Journaliseur.Info($"[CARACS] Rien à distribuer ({_perso.PointsCaracteristiques} pts dispo, cfg total={_cfg.Total}).");
            return;
        }

        Journaliseur.Info($"[CARACS] Mode={_cfg.Mode} — {_perso.PointsCaracteristiques} pts à distribuer :");
        foreach (var (statId, n, cout) in plan)
        {
            Journaliseur.Info($"[CARACS]   {NomStat(statId)} +{n} (coût {cout} pts)");
        }
        if (_cfg.Mode == ModeDistribCaracs.Preview)
        {
            Journaliseur.Info("[CARACS] Preview seulement — aucun paquet AB envoyé.");
            return;
        }

        // Mode Automatique : envoi des AB un par un, délai humanisé.
        foreach (var (statId, n, _) in plan)
        {
            for (int i = 0; i < n; i++)
            {
                ct.ThrowIfCancellationRequested();
                await _api.MonterCaracteristiqueAsync(statId, 1, ct).ConfigureAwait(false);
                await Task.Delay(_cfg.DelaiEntreAbMs, ct).ConfigureAwait(false);
            }
        }
        Journaliseur.Info("[CARACS] Distribution terminée.");
    }

    private static string NomStat(int id) => id switch
    {
        IdsCaracs.Vitalite => "Vitalité",
        IdsCaracs.Sagesse => "Sagesse",
        IdsCaracs.Force => "Force",
        IdsCaracs.Intelligence => "Intelligence",
        IdsCaracs.Chance => "Chance",
        IdsCaracs.Agilite => "Agilité",
        _ => $"#{id}"
    };
}
