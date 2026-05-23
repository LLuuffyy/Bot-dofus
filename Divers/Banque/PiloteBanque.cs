using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BotDofus.Commun.Reseau;
using BotDofus.Divers.Donnees;
using BotDofus.Divers.Jeu.Personnage;
using BotDofus.Divers.Scripts.Api;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Banque;

/// <summary>
/// Pilote async du dépôt banque : zaap vers map banque → ouverture (<c>ApS</c>
/// CLAIR) → dépôt items (<c>EMO+&lt;uidInv&gt;|&lt;qte&gt;</c> chiffré '-')
/// → fermeture (<c>EV</c> chiffré '-') → zaap retour optionnel.
///
/// ✅ PROTOCOLE confirmé par capture user 2026-05-21 sur Hystoria :
/// <list type="bullet">
///   <item><c>ApS</c> ouvre le coffre interactif (pas de dialogue NPC, gratuit).</item>
///   <item><c>EMO+&lt;uid&gt;|&lt;qte&gt;</c> dépose (canal chiffré auto via DoitEtreChiffre).</item>
///   <item><c>EV</c> ferme (canal chiffré).</item>
///   <item>Attendre <c>OR&lt;persoId&gt;|&lt;uid&gt;</c> entre dépôts pour confirmation.</item>
/// </list>
/// </summary>
public sealed class PiloteBanque
{
    private readonly ApiBot _api;
    private readonly SessionProxy _session;
    private readonly Personnage _perso;
    private readonly ConfigBanque _cfg;

    /// <summary>Compteur monotone d'<c>OR</c> reçus (Object Remove de l'inventaire).
    /// Incrémenté par <see cref="TrameJeu"/> à chaque OR observé. Utilisé pour
    /// attendre la confirmation serveur entre 2 dépôts.</summary>
    public static int CompteurObjectRemove;

    /// <summary>Flag set à true quand un paquet <c>EV</c> est observé (S→C ou C→S)
    /// — la banque est fermée. Le pilote en cours doit arrêter ses dépôts.
    /// Reset à false par le pilote au début du workflow.</summary>
    public static volatile bool BanqueFermeeObservee;

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

        // 1) Si on est DÉJÀ sur la map banque OU si OuvertureDirecte est activé,
        //    sauter l'étape zaap et envoyer ApS directement depuis la position.
        int? mapActuelle = _perso.CarteCourante;
        bool dejaSurMapBanque = mapActuelle.HasValue && mapActuelle.Value == _cfg.MapBanqueId;
        if (dejaSurMapBanque)
        {
            Journaliseur.Info($"[BANQUE] Étape 1/4 : déjà sur map banque {_cfg.MapBanqueId} — skip zaap");
        }
        else if (_cfg.OuvertureDirecte)
        {
            Journaliseur.Info(
                $"[BANQUE] Étape 1/4 : OuvertureDirecte=true — skip zaap, ApS envoyé depuis map {mapActuelle}. "
                + "Le perso DOIT être à proximité d'un coffre banque ou ApS sera ignoré par le serveur.");
        }
        else
        {
            Journaliseur.Info($"[BANQUE] Étape 1/4 : zaap vers map banque {_cfg.MapBanqueId} (depuis map {mapActuelle})");
            bool zaapOk = await _api.UtiliserZaapAsync(_cfg.MapBanqueId, ct).ConfigureAwait(false);
            if (!zaapOk)
            {
                Journaliseur.Avertir(
                    $"[BANQUE] Zaap vers {_cfg.MapBanqueId} échoué — abandon workflow. "
                    + "Cause probable : pas de cellule zaap (gfx 7000) sur la map actuelle, "
                    + "ou map cible non débloquée. Mets-toi sur un zaap connu ou directement "
                    + "sur la map banque avant de lancer 'Tester maintenant'.");
                return false;
            }
            await Task.Delay(System.Random.Shared.Next(1500, 2500), ct).ConfigureAwait(false);
        }

        // 2) Dépôt items.
        Journaliseur.Info("[BANQUE] Étape 2/4 : dépôt items");
        await DeposerToutAsync(ct).ConfigureAwait(false);

        // 3) Retour vers la map de farm (zaap aller-retour — skip si banque mobile).
        if (_cfg.OuvertureDirecte)
        {
            Journaliseur.Info("[BANQUE] Étape 3/4 : OuvertureDirecte=true — pas de retour zaap, perso reste sur place");
        }
        else if (_cfg.RetourFarmApresDepot && carteFarmAvant.HasValue && carteFarmAvant.Value != _cfg.MapBanqueId)
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
        return true;
    }

    /// <summary>
    /// Workflow ouvrir-déposer-fermer (sans zaap). Retourne true si le
    /// pourcentage poids est passé sous CiblePoidsPct.
    /// </summary>
    public async Task<bool> DeposerToutAsync(CancellationToken ct = default)
    {
        Journaliseur.Info($"[BANQUE] Démarrage dépôt — poids actuel {_perso.PourcentagePoids:F1}% (seuil={_cfg.SeuilPoidsPct}%)");

        // Reset des flags observateurs avant ouverture (sinon un EV résiduel
        // d'un workflow précédent ferait croire que la banque est déjà fermée).
        BanqueFermeeObservee = false;

        // === Étape 1 : ouvrir le coffre banque (ApS, CLAIR) ===
        // ⚠ PROTOCOLE Hystoria : pas de dialogue NPC, le coffre interactif s'ouvre directement.
        // Le serveur répond ECK5 (Échange Créé kind=5) puis EL (liste vide ou contenu).
        Journaliseur.Info("[BANQUE] → ApS (ouverture coffre)");
        await _session.EnvoyerAuServeurAsync("ApS").ConfigureAwait(false);
        await Delai(ct).ConfigureAwait(false);

        // === Étape 2 : calculer la liste effective à déposer selon les filtres ===
        var snapshot = _perso.Inventaire.ToList();
        var aDeposer = CalculerItemsADeposer(snapshot);
        Journaliseur.Info($"[BANQUE] {aDeposer.Count}/{snapshot.Count} item(s) sélectionné(s) pour dépôt");

        // === Étape 3 : déposer chaque item via EMO+<uid>|<qte> (canal chiffré auto) ===
        int deposes = 0;
        int rejetesSec = 0;
        foreach (var item in aDeposer)
        {
            if (ct.IsCancellationRequested) break;
            // L'user (ou un autre process) a fermé la banque → on stoppe net,
            // sans envoyer la fermeture EV nous-mêmes (déjà fait).
            if (BanqueFermeeObservee)
            {
                Journaliseur.Avertir(
                    $"[BANQUE] EV observé pendant dépôt — arrêt immédiat ({deposes} item(s) déposés, "
                    + $"{aDeposer.Count - deposes} restants annulés).");
                return true;  // arrêt user → succès partiel
            }
            // On dépose TOUT ce qui matche les catégories cochées (pas d'arrêt cible).

            // Sécurité ultime (relue à CHAQUE item car l'user peut éditer la config
            // pendant le workflow → IdsAGarder peut grandir, catégorie peut être
            // décochée). Cause normale du refus à mi-workflow ; pas un bug.
            if (!EstAutoriseADeposer(item))
            {
                Journaliseur.Debogue(
                    $"[BANQUE] item refusé en cours de dépôt #{item.Identifiant} template={item.IdTemplate} "
                    + "(config a probablement été éditée en live ou item décoché)");
                rejetesSec++;
                continue;
            }

            // Skip items dont la quantité est 0 ou négative — l'inventaire peut
            // contenir un objet à qte=0 transitoirement (race entre OQ/OR et
            // notre snapshot, cf. log 04:11:44 où 3 items passaient qte=0).
            // EMO+<uid>|0 est inutile et pollue le journal.
            if (item.Quantite <= 0)
            {
                Journaliseur.Debogue($"[BANQUE] skip item #{item.Identifiant} template={item.IdTemplate} (qte=0)");
                continue;
            }

            int compteurAvant = System.Threading.Interlocked.CompareExchange(ref CompteurObjectRemove, 0, 0);
            // ⚠ PROTOCOLE Hystoria : EMO+<uid>|<qte> — séparateur '|' (PAS ';'),
            // préfixe 'EMO+' (PAS 'EM'). UID = identifiant inventaire long.
            var paquet = $"EMO+{item.Identifiant}|{item.Quantite}";
            Journaliseur.Info($"[BANQUE] → {paquet} (template {item.IdTemplate}, qte {item.Quantite})");
            await _session.EnvoyerAuServeurAsync(paquet).ConfigureAwait(false);
            deposes++;

            // Attendre la confirmation OR<persoId>|<uid> (= objet retiré inventaire)
            // OU un délai de garde de 3s pour éviter de boucler si pas de feedback.
            await AttendreObjectRemoveAsync(compteurAvant, 3000, ct).ConfigureAwait(false);
            await Delai(ct).ConfigureAwait(false);
        }

        // === Étape 4 : fermer la banque (EV, canal chiffré auto) ===
        Journaliseur.Info("[BANQUE] → EV (fermeture coffre)");
        await _session.EnvoyerAuServeurAsync("EV").ConfigureAwait(false);
        await Delai(ct).ConfigureAwait(false);

        Journaliseur.Info($"[BANQUE] Dépôt terminé — {deposes} item(s) déposés, {rejetesSec} refusés (sécurité), poids final {_perso.PourcentagePoids:F1}%");
        return true;
    }

    /// <summary>
    /// Calcule la liste effective d'items à déposer selon les filtres :
    /// catégorie (Équipement/Ressource/Consommable/Quête/Inconnu), liste
    /// blanche/noire d'IDs template, seuils par template.
    /// </summary>
    internal List<ObjetInventaire> CalculerItemsADeposer(IEnumerable<ObjetInventaire> inventaire)
    {
        var bdd = BaseDonnees.Instance;
        var resultat = new List<ObjetInventaire>();

        // FILTRE SAC UNIQUEMENT : Position==63 sur Dofus 1.29 = sac.
        // Les autres positions sont les équipements (amulette, arme, etc.)
        // qu'il ne faut JAMAIS déposer automatiquement. Aussi : quand le coffre
        // banque est ouvert, Hystoria ajoute parfois les items du coffre dans
        // la même liste avec d'autres positions → on aurait essayé de
        // « déposer » les items déjà dans le coffre (bug 2026-05-23 15:26 :
        // 25/817 items sélectionnés car 792 items hors-sac).
        var itemsSac = new List<ObjetInventaire>();
        foreach (var o in inventaire)
            if (o.Position == 63 && o.Quantite > 0) itemsSac.Add(o);

        // 1) Compter le total par template (pour les seuils) — sur les items du sac seulement.
        var totalParTemplate = new Dictionary<int, int>();
        foreach (var o in itemsSac)
            totalParTemplate[o.IdTemplate] = totalParTemplate.GetValueOrDefault(o.IdTemplate) + o.Quantite;

        foreach (var item in itemsSac)
        {
            // Liste noire : on garde TOUJOURS.
            if (_cfg.IdsAGarder.Contains(item.IdTemplate)) continue;

            // Liste blanche : on dépose TOUJOURS.
            if (_cfg.IdsADeposerForce.Contains(item.IdTemplate))
            {
                AjouterAvecSeuil(item, totalParTemplate, resultat);
                continue;
            }

            // Catégorie via le type de l'item dans la BDD.
            var info = bdd.Item(item.IdTemplate);
            var cat = info != null
                ? CategoriseurObjet.Categoriser(info.IdType)
                : CategorieObjet.Inconnu;

            bool autorise = cat switch
            {
                CategorieObjet.Equipement => _cfg.DeposerEquipements,
                CategorieObjet.Ressource => _cfg.DeposerRessources,
                CategorieObjet.Consommable => _cfg.DeposerConsommables,
                CategorieObjet.Quete => _cfg.DeposerQuetes,
                CategorieObjet.Inconnu => _cfg.DeposerInconnus,
                _ => false,
            };
            if (!autorise) continue;

            AjouterAvecSeuil(item, totalParTemplate, resultat);
        }
        return resultat;
    }

    /// <summary>
    /// Ajoute l'item à la liste de dépôt en respectant le <c>SeuilParTemplate</c> :
    /// si l'user veut garder N exemplaires d'un template (ex. 50 pains), on
    /// ajuste la quantité déposable pour conserver ce seuil en inventaire.
    /// </summary>
    private void AjouterAvecSeuil(ObjetInventaire item, Dictionary<int, int> totalParTemplate, List<ObjetInventaire> resultat)
    {
        if (!_cfg.SeuilParTemplate.TryGetValue(item.IdTemplate, out int seuil) || seuil <= 0)
        {
            resultat.Add(item);
            return;
        }
        // Combien de cet item dans l'inventaire ?
        int total = totalParTemplate.GetValueOrDefault(item.IdTemplate);
        // Combien on peut déposer en gardant `seuil` ?
        int dispo = total - seuil;
        if (dispo <= 0) return;

        int qteADep = System.Math.Min(item.Quantite, dispo);
        if (qteADep <= 0) return;

        // On dépose en ajustant la quantité (sans muter l'original).
        resultat.Add(new ObjetInventaire
        {
            Identifiant = item.Identifiant,
            IdTemplate = item.IdTemplate,
            Quantite = qteADep,
            Position = item.Position,
        });
        // Mettre à jour le total restant en inventaire pour les prochains items du même template.
        totalParTemplate[item.IdTemplate] = total - qteADep;
    }

    /// <summary>
    /// Vérifie une dernière fois (défensif) qu'on a le droit de déposer cet item :
    /// JAMAIS un item dans <see cref="ConfigBanque.IdsAGarder"/>, JAMAIS un item
    /// de catégorie Quête si <see cref="ConfigBanque.DeposerQuetes"/> est false.
    /// </summary>
    internal bool EstAutoriseADeposer(ObjetInventaire item)
    {
        if (_cfg.IdsAGarder.Contains(item.IdTemplate)) return false;
        if (_cfg.IdsADeposerForce.Contains(item.IdTemplate)) return true;

        var info = BaseDonnees.Instance.Item(item.IdTemplate);
        var cat = info != null ? CategoriseurObjet.Categoriser(info.IdType) : CategorieObjet.Inconnu;
        return cat switch
        {
            CategorieObjet.Equipement => _cfg.DeposerEquipements,
            CategorieObjet.Ressource => _cfg.DeposerRessources,
            CategorieObjet.Consommable => _cfg.DeposerConsommables,
            CategorieObjet.Quete => _cfg.DeposerQuetes,
            CategorieObjet.Inconnu => _cfg.DeposerInconnus,
            _ => false,
        };
    }

    /// <summary>
    /// Attend que <see cref="CompteurObjectRemove"/> avance (= un OR&lt;persoId&gt;|&lt;uid&gt;
    /// reçu, donc le dépôt est confirmé côté serveur). Sinon timeout — on
    /// continue quand même pour éviter le blocage si le compteur n'est pas câblé.
    /// </summary>
    private static async Task AttendreObjectRemoveAsync(int compteurAvant, int timeoutMs, CancellationToken ct)
    {
        int waitMs = 0;
        while (waitMs < timeoutMs)
        {
            if (ct.IsCancellationRequested) return;
            int actuel = System.Threading.Interlocked.CompareExchange(ref CompteurObjectRemove, 0, 0);
            if (actuel > compteurAvant) return;
            await Task.Delay(100, ct).ConfigureAwait(false);
            waitMs += 100;
        }
    }

    private Task Delai(CancellationToken ct = default)
        => Task.Delay(System.Random.Shared.Next(
            System.Math.Max(50, _cfg.DelaiActionMinMs),
            System.Math.Max(100, _cfg.DelaiActionMaxMs)), ct);
}
