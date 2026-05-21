using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BotDofus.Commun.Reseau;
using BotDofus.Divers.Jeu.Personnage;
using BotDofus.Divers.Scripts.Api;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Banque;

/// <summary>
/// Pilote async du dépôt banque : zaap vers map banque → ouverture (EBM) →
/// dépôt items (EM&lt;uid&gt;;qte;1) → fermeture (EV) → zaap retour optionnel.
///
/// ⚠ PROTOCOLE Dofus 1.29 — paquets EBM/EM/EV utilisés par dyshay/cadernis
/// mais à VALIDER en capture user sur Hystoria (peut différer légèrement).
/// </summary>
public sealed class PiloteBanque
{
    private readonly ApiBot _api;
    private readonly SessionProxy _session;
    private readonly Personnage _perso;
    private readonly ConfigBanque _cfg;

    public PiloteBanque(ApiBot api, SessionProxy session, Personnage perso, ConfigBanque cfg)
    {
        _api = api;
        _session = session;
        _perso = perso;
        _cfg = cfg;
    }

    /// <summary>
    /// Workflow COMPLET : zaap vers banque → dépôt → zaap retour si configuré.
    /// Retourne true si le pourcentage poids est passé sous CiblePoidsPct.
    /// </summary>
    public async Task<bool> WorkflowCompletAsync(int? carteFarmAvant, CancellationToken ct = default)
    {
        Journaliseur.Info($"[BANQUE] === Workflow complet démarré (poids {_perso.PourcentagePoids:F1}%) ===");

        // 1) Zaap vers la map banque (Astrub bank par défaut = 10117).
        Journaliseur.Info($"[BANQUE] Étape 1/4 : zaap vers map banque {_cfg.MapBanqueId}");
        bool zaapOk = await _api.UtiliserZaapAsync(_cfg.MapBanqueId, ct).ConfigureAwait(false);
        if (!zaapOk)
        {
            Journaliseur.Avertir($"[BANQUE] Zaap vers {_cfg.MapBanqueId} échoué — abandon workflow");
            return false;
        }
        await Task.Delay(System.Random.Shared.Next(1500, 2500), ct).ConfigureAwait(false);

        // 2) Dépôt items.
        Journaliseur.Info("[BANQUE] Étape 2/4 : dépôt items");
        var depotOk = await DeposerToutAsync(ct).ConfigureAwait(false);
        if (!depotOk)
        {
            Journaliseur.Avertir($"[BANQUE] Dépôt incomplet (poids={_perso.PourcentagePoids:F1}% > cible {_cfg.CiblePoidsPct}%)");
        }

        // 3) Retour vers la map de farm (si configuré et map sauvegardée).
        if (_cfg.RetourFarmApresDepot && carteFarmAvant.HasValue && carteFarmAvant.Value != _cfg.MapBanqueId)
        {
            Journaliseur.Info($"[BANQUE] Étape 3/4 : zaap retour vers map {carteFarmAvant.Value}");
            bool retourOk = await _api.UtiliserZaapAsync(carteFarmAvant.Value, ct).ConfigureAwait(false);
            if (!retourOk)
            {
                Journaliseur.Avertir($"[BANQUE] Zaap retour échoué — perso reste à la banque");
            }
            await Task.Delay(System.Random.Shared.Next(1500, 2500), ct).ConfigureAwait(false);
        }
        else
        {
            Journaliseur.Info("[BANQUE] Étape 3/4 : pas de retour configuré, perso reste à la banque");
        }

        Journaliseur.Info($"[BANQUE] === Workflow terminé (poids final {_perso.PourcentagePoids:F1}%) ===");
        return depotOk;
    }

    /// <summary>
    /// Workflow ouvrir-déposer-fermer (sans zaap). Retourne true si le
    /// pourcentage poids est passé sous CiblePoidsPct.
    /// </summary>
    public async Task<bool> DeposerToutAsync(CancellationToken ct = default)
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

    private Task Delai(CancellationToken ct = default)
        => Task.Delay(System.Random.Shared.Next(
            System.Math.Max(50, _cfg.DelaiActionMinMs),
            System.Math.Max(100, _cfg.DelaiActionMaxMs)), ct);
}
