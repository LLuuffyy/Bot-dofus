using System;
using System.Linq;
using System.Threading.Tasks;
using BotDofus.Commun.Reseau;
using BotDofus.Divers.Cartes;
using BotDofus.Divers.Combats;
using BotDofus.Divers.Combats.IA;
using BotDofus.Divers.Jeu.Personnage.Spells;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.MultiAccount;

/// <summary>
/// IA de combat simplifiée pour piloter les héros liés en mode héros Abrak.
///
/// Quand <c>GTS&lt;idLié&gt;</c> arrive, ce moteur exécute :
/// <list type="number">
///   <item>Sélectionne l'ennemi le plus proche (distance Chebyshev).</item>
///   <item>Itère la <see cref="ConfigCombat.Regles"/> par priorité décroissante.</item>
///   <item>Pour chaque règle : vérifie que le sort est appris
///         (<see cref="MembreHeros.SortsAppris"/>), que le membre a assez de PA,
///         et que l'ennemi est dans la portée [min, max] au niveau appris.</item>
///   <item>Cast : <c>GA300&lt;sortId&gt;;&lt;cellEnnemi&gt;</c> → <c>GKK0</c>.</item>
///   <item>Continue tant qu'il reste des PA et des règles utilisables.</item>
///   <item>Fin de tour : <c>Gt</c>.</item>
/// </list>
///
/// Pas de déplacement dans cette version : si aucun sort ne porte, le membre
/// passe directement son tour. Refactor complet (réutilisation de
/// <see cref="MoteurReglesCombat"/> en mode multi-perso) à venir.
/// </summary>
public sealed class IACombatHerosSimple
{
    /// <summary>Délai humanisé entre 2 actions (ms).</summary>
    private const int DelaiEntreActionsMs = 1200;
    /// <summary>Délai après cast avant GKK0 (ms).</summary>
    private const int DelaiAvantAckMs = 350;
    /// <summary>Délai avant Gt en fin de tour (ms).</summary>
    private const int DelaiAvantFinTourMs = 800;

    public static async Task JouerTourAsync(
        MembreHeros membre,
        Combat combat,
        Carte? carte,
        SessionProxy session)
    {
        if (membre is null) return;
        if (combat is null) return;
        if (session is null) return;
        if (membre.SortsAppris.Count == 0)
        {
            Journaliseur.Info($"[IA-HEROS:{membre.Nom}] Aucun sort connu → Gt direct");
            await EnvoyerFinTourAsync(session);
            return;
        }
        var cfg = membre.ConfigCombat;
        if (cfg is null || cfg.Regles.Count == 0)
        {
            Journaliseur.Info($"[IA-HEROS:{membre.Nom}] ConfigCombat vide → Gt direct");
            await EnvoyerFinTourAsync(session);
            return;
        }

        // Ennemis vivants. Sur mode héros mono-client, les ennemis sont des
        // monstres (id < 0) — pas besoin de filtrer.
        var ennemis = combat.Ennemis.Where(e => !e.EstMort).ToList();
        if (ennemis.Count == 0)
        {
            Journaliseur.Info($"[IA-HEROS:{membre.Nom}] Plus d'ennemis vivants → Gt direct");
            await EnvoyerFinTourAsync(session);
            return;
        }

        int largeur = carte?.Largeur ?? 14;
        int paRestant = membre.Pa;
        int casts = 0;

        // Tri par priorité décroissante (priorité haute = essayé d'abord).
        var reglesOrdonnees = cfg.Regles
            .Where(r => r != null)
            .OrderByDescending(r => r.Priorite)
            .ToList();

        // Boucle : tant qu'on a au moins 1 PA pour lancer quelque chose et
        // qu'on n'a pas atteint la limite (NombreParTour de chaque règle).
        var compteursParSort = new System.Collections.Generic.Dictionary<int, int>();
        bool actionTrouvee;
        do
        {
            actionTrouvee = false;
            foreach (var regle in reglesOrdonnees)
            {
                int idSort = regle.IdSort;
                if (!membre.SortsAppris.TryGetValue(idSort, out var niveau) || niveau <= 0) continue;
                if (compteursParSort.TryGetValue(idSort, out var dejaCast) && dejaCast >= regle.NombreParTour) continue;

                var infoSort = BaseSorts.Instance.Trouver(idSort);
                if (infoSort is null) continue;
                var stats = infoSort.Stats(niveau);
                if (stats is null) continue;
                if (stats.CoutPA <= 0) continue;
                if (stats.CoutPA > paRestant) continue;

                // Sélection cible la plus proche en portée.
                var cible = ChoisirCible(membre.Cellule, ennemis, stats.PorteeMin, stats.PorteeMax, largeur);
                if (cible is null) continue;

                Journaliseur.Info(
                    $"[IA-HEROS:{membre.Nom}] cast sort #{idSort} niv{niveau} sur cell {cible.CellulePosition} "
                    + $"(coût {stats.CoutPA} PA, portée [{stats.PorteeMin}-{stats.PorteeMax}])");
                try
                {
                    await session.EnvoyerAuServeurAsync($"GA300{idSort};{cible.CellulePosition}").ConfigureAwait(false);
                    await Task.Delay(DelaiAvantAckMs).ConfigureAwait(false);
                    await session.EnvoyerAuServeurAsync("GKK0").ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Journaliseur.Avertir($"[IA-HEROS:{membre.Nom}] échec cast : {ex.Message}");
                    goto fin; // sortir des deux boucles
                }
                paRestant -= stats.CoutPA;
                casts++;
                compteursParSort[idSort] = dejaCast + 1;
                actionTrouvee = true;
                await Task.Delay(DelaiEntreActionsMs).ConfigureAwait(false);
                break; // reprendre la rotation par priorité
            }
        } while (actionTrouvee && paRestant > 0);

    fin:
        Journaliseur.Info(
            $"[IA-HEROS:{membre.Nom}] Fin tour ({casts} cast(s), PA restants {paRestant})");
        await Task.Delay(DelaiAvantFinTourMs).ConfigureAwait(false);
        await EnvoyerFinTourAsync(session);
    }

    private static Combats.Combattants.Combattant? ChoisirCible(
        int cellMembre,
        System.Collections.Generic.IEnumerable<Combats.Combattants.Combattant> ennemis,
        int porteeMin, int porteeMax, int largeur)
    {
        var (xMembre, yMembre) = Cellule.CalculerCoordonnees(cellMembre, largeur);
        Combats.Combattants.Combattant? meilleur = null;
        int meilleureDist = int.MaxValue;
        foreach (var e in ennemis)
        {
            if (e.CellulePosition <= 0) continue;
            var (xe, ye) = Cellule.CalculerCoordonnees(e.CellulePosition, largeur);
            int d = Math.Max(Math.Abs(xMembre - xe), Math.Abs(yMembre - ye));
            if (d < porteeMin || d > porteeMax) continue;
            if (d < meilleureDist)
            {
                meilleureDist = d;
                meilleur = e;
            }
        }
        return meilleur;
    }

    private static async Task EnvoyerFinTourAsync(SessionProxy session)
    {
        try
        {
            await session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"[IA-HEROS] échec Gt : {ex.Message}");
        }
    }
}
