using System.Linq;
using System.Threading.Tasks;
using BotDofus.Commun.Reseau;
using BotDofus.Divers.Jeu.Personnage;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Banque;

/// <summary>
/// Pilote async du dépôt banque : ouverture (EBM/interactif), dépôt items
/// (EM&lt;uid&gt;;qte;1), fermeture (EV).
///
/// État : SQUELETTE — protocole Dofus 1.29 à valider en capture réelle
/// avant déploiement prod. Voir docs/FEATURES-ROADMAP.md §"Plan Dépôt banque".
/// </summary>
public sealed class PiloteBanque
{
    private readonly SessionProxy _session;
    private readonly Personnage _perso;
    private readonly ConfigBanque _cfg;

    public PiloteBanque(SessionProxy session, Personnage perso, ConfigBanque cfg)
    {
        _session = session;
        _perso = perso;
        _cfg = cfg;
    }

    /// <summary>
    /// Workflow complet : ouvre, dépose, ferme. Retourne true si le pourcentage
    /// poids est passé sous CiblePoidsPct.
    /// </summary>
    public async Task<bool> DeposerToutAsync()
    {
        Journaliseur.Info($"[BANQUE] Démarrage dépôt — poids actuel {_perso.PourcentagePoids:F1}% (seuil={_cfg.SeuilPoidsPct}%, cible={_cfg.CiblePoidsPct}%)");

        // Étape 1 : ouvrir la banque. Format Dofus Retro à confirmer en capture.
        // Probablement EBM (Echange Banque Mode) ou GA500<cellNPC>;<skill>.
        await _session.EnvoyerAuServeurAsync("EBM").ConfigureAwait(false);
        await Delai().ConfigureAwait(false);

        // Étape 2 : déposer les items (filtrage selon cfg).
        var itemsADeposer = _perso.Inventaire
            .Where(o => !_cfg.ItemsAGarder.Contains(o.IdTemplate))
            .Where(o => _cfg.ItemsADeposer.Count == 0 || _cfg.ItemsADeposer.Contains(o.IdTemplate))
            .ToList();

        int deposes = 0;
        foreach (var item in itemsADeposer)
        {
            if (_perso.PourcentagePoids <= _cfg.CiblePoidsPct)
            {
                Journaliseur.Info($"[BANQUE] Cible {_cfg.CiblePoidsPct}% atteinte ({_perso.PourcentagePoids:F1}%), arrêt dépôt");
                break;
            }
            // Format Dofus 1.29 : EM<uid>;<quantite>;<1=depot>
            await _session.EnvoyerAuServeurAsync($"EM{item.Identifiant};{item.Quantite};1").ConfigureAwait(false);
            deposes++;
            await Delai().ConfigureAwait(false);
        }

        // Étape 3 : fermer la banque (EV = Exchange Validate/leave).
        await _session.EnvoyerAuServeurAsync("EV").ConfigureAwait(false);
        await Delai().ConfigureAwait(false);

        Journaliseur.Info($"[BANQUE] Dépôt terminé — {deposes} item(s) déposés, poids final {_perso.PourcentagePoids:F1}%");
        return _perso.PourcentagePoids <= _cfg.CiblePoidsPct;
    }

    private Task Delai()
        => Task.Delay(System.Random.Shared.Next(
            System.Math.Max(50, _cfg.DelaiActionMinMs),
            System.Math.Max(100, _cfg.DelaiActionMaxMs)));
}
