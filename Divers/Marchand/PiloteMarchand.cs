using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BotDofus.Commun.Reseau;
using BotDofus.Divers.Banque;
using BotDofus.Divers.Donnees;
using BotDofus.Divers.Jeu.Personnage;
using BotDofus.Divers.Scripts.Api;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Marchand;

/// <summary>
/// Workflow de vente au PNJ marchand. Pattern identique à <see cref="PiloteBanque"/> :
/// <list type="number">
///   <item>Parler au PNJ (<c>DC&lt;idPerso&gt;,&lt;idPnj&gt;</c>)</item>
///   <item>Attendre l'ouverture du dialogue / vue marchand</item>
///   <item>Pour chaque item à vendre : <c>EMO+&lt;uid&gt;|&lt;qte&gt;</c></item>
///   <item>Quitter (<c>DV</c> ou <c>EV</c> selon serveur)</item>
/// </list>
///
/// <para>NOTE : sur Dofus 1.29 Retro / Hystoria, la "vente PNJ" passe par le
/// même opcode que la banque (<c>EMO+</c>) car la vente est un échange
/// unidirectionnel comme un dépôt — confirmé par captures réelles dyshay.
/// Si Hystoria utilise un opcode différent, ce sera à ajuster ici.</para>
/// </summary>
public sealed class PiloteMarchand
{
    private readonly ApiBot _api;
    private readonly SessionProxy _session;
    private readonly Personnage _perso;
    private readonly ConfigMarchand _cfg;

    /// <summary>True quand on a observé un EV ou DV du serveur (= dialogue marchand fermé).</summary>
    public bool DialogueFermeObserve { get; private set; }

    public PiloteMarchand(ApiBot api, SessionProxy session, Personnage perso, ConfigMarchand cfg)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _perso = perso ?? throw new ArgumentNullException(nameof(perso));
        _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
    }

    /// <summary>
    /// Ouvre le dialogue avec le PNJ marchand puis vend tous les items
    /// correspondant aux catégories cochées.
    /// </summary>
    public async Task<bool> VendreToutAsync(CancellationToken ct = default)
    {
        Journaliseur.Info($"[MARCHAND] Démarrage vente — poids actuel {_perso.PourcentagePoids:F1}% (seuil={_cfg.SeuilPoidsPct}%, PNJ gabarit #{_cfg.IdPnjMarchand})");

        DialogueFermeObserve = false;

        // 1) Ouvrir dialogue PNJ : DC<idPerso>,<idPnj>
        // L'API résout le gabarit → id contextuel automatiquement.
        Journaliseur.Info($"[MARCHAND] → DC PNJ#{_cfg.IdPnjMarchand}");
        await _api.ParlerPnjAsync(0, _cfg.IdPnjMarchand, ct).ConfigureAwait(false);
        await Delai(ct).ConfigureAwait(false);
        await Delai(ct).ConfigureAwait(false);

        // 2) Calculer items à vendre — filtre Position==63 (sac), Quantite>0,
        // catégories cochées dans la config.
        var snapshot = _perso.Inventaire.ToList();
        var aVendre = CalculerItemsAVendre(snapshot);
        Journaliseur.Info($"[MARCHAND] {aVendre.Count}/{snapshot.Count} item(s) sélectionné(s) pour vente");

        // 3) Envoyer EMO+<uid>|<qte> pour chaque item (même pattern que banque).
        int vendus = 0;
        foreach (var item in aVendre)
        {
            if (ct.IsCancellationRequested) break;
            if (DialogueFermeObserve)
            {
                Journaliseur.Avertir(
                    $"[MARCHAND] Dialogue fermé pendant vente — arrêt ({vendus}/{aVendre.Count} vendus).");
                return true;
            }
            if (_cfg.IdsAGarder.Contains(item.IdTemplate))
            {
                Journaliseur.Debogue($"[MARCHAND] skip item #{item.Identifiant} template={item.IdTemplate} (liste noire)");
                continue;
            }
            if (item.Quantite <= 0) continue;

            var paquet = $"EMO+{item.Identifiant}|{item.Quantite}";
            Journaliseur.Info($"[MARCHAND] → {paquet} (template {item.IdTemplate}, qte {item.Quantite})");
            await _session.EnvoyerAuServeurAsync(paquet).ConfigureAwait(false);
            vendus++;
            await Delai(ct).ConfigureAwait(false);
        }

        // 4) Quitter dialogue (DV).
        Journaliseur.Info("[MARCHAND] → DV (quitter dialogue)");
        await _session.EnvoyerAuServeurAsync("DV").ConfigureAwait(false);
        await Delai(ct).ConfigureAwait(false);

        Journaliseur.Info($"[MARCHAND] Vente terminée — {vendus} item(s) vendus, poids final {_perso.PourcentagePoids:F1}%");
        return true;
    }

    /// <summary>
    /// Calcule les items à vendre selon les catégories cochées.
    /// Identique à <see cref="PiloteBanque.CalculerItemsADeposer"/> :
    /// filtre Position==63 (sac) + Quantite>0 + catégorie autorisée.
    /// </summary>
    internal List<ObjetInventaire> CalculerItemsAVendre(IEnumerable<ObjetInventaire> inventaire)
    {
        var bdd = BaseDonnees.Instance;
        var resultat = new List<ObjetInventaire>();
        foreach (var item in inventaire)
        {
            if (item.Position != 63) continue;  // sac uniquement
            if (item.Quantite <= 0) continue;
            if (_cfg.IdsAGarder.Contains(item.IdTemplate)) continue;

            var info = bdd.Item(item.IdTemplate);
            var cat = info != null
                ? CategoriseurObjet.Categoriser(info.IdType)
                : CategorieObjet.Inconnu;
            bool autorise = cat switch
            {
                CategorieObjet.Equipement => _cfg.VendreEquipements,
                CategorieObjet.Ressource => _cfg.VendreRessources,
                CategorieObjet.Consommable => _cfg.VendreConsommables,
                CategorieObjet.Inconnu => _cfg.VendreInconnus,
                _ => false,
            };
            if (!autorise) continue;
            resultat.Add(item);
        }
        return resultat;
    }

    /// <summary>Signale au pilote qu'on a observé une fermeture EV/DV serveur.</summary>
    public void SignalerDialogueFerme() => DialogueFermeObserve = true;

    private async Task Delai(CancellationToken ct)
    {
        int min = _cfg.DelaiActionMinMs;
        int max = Math.Max(min + 50, _cfg.DelaiActionMaxMs);
        int d = Random.Shared.Next(min, max);
        await Task.Delay(d, ct).ConfigureAwait(false);
    }
}
