using System;
using System.Collections.Generic;
using System.Linq;
using BotDofus.Commun.Reseau;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Divers.Interception;

/// <summary>
/// Pile de règles d'interception appliquées à chaque paquet relayé par le proxy MITM.
///
/// C'est le moteur du mode "intercept-and-modify" (Phase 2 du projet) : on modifie
/// les paquets du vrai client Dofus à la volée, ce qui est la méthode la plus furtive
/// pour agir sur le serveur (timing/keep-alive/format = ceux du client officiel).
///
/// Branché dans <see cref="ContexteCompte"/> via <c>SessionProxy.ModificateurPaquet</c>,
/// en amont de l'interception AYK. Thread-safe en lecture/écriture concurrente
/// (la boucle relai lit pendant que l'UI/Lua ajoute des règles).
/// </summary>
public sealed class GestionnaireInterception
{
    private readonly List<RegleInterception> _regles = new();
    private readonly object _verrou = new();

    /// <summary>Active/désactive globalement l'interception (kill-switch sécurité).</summary>
    public bool Active { get; set; } = true;

    /// <summary>Évènement émis à chaque modification effective (pour log/UI/stats).</summary>
    public event EventHandler<EvenementInterception>? PaquetModifie;

    public IReadOnlyList<RegleInterception> Regles
    {
        get { lock (_verrou) return _regles.ToArray(); }
    }

    public void Ajouter(RegleInterception regle)
    {
        lock (_verrou) _regles.Add(regle);
        Journaliseur.Info($"[INTERCEPT] Règle ajoutée : {regle.Nom}");
    }

    public bool Retirer(string nom)
    {
        lock (_verrou)
        {
            var r = _regles.FirstOrDefault(x => x.Nom == nom);
            if (r == null) return false;
            _regles.Remove(r);
            Journaliseur.Info($"[INTERCEPT] Règle retirée : {nom}");
            return true;
        }
    }

    public void Vider()
    {
        lock (_verrou) _regles.Clear();
        Journaliseur.Info("[INTERCEPT] Toutes les règles retirées");
    }

    /// <summary>
    /// Applique la première règle correspondante.
    /// Retourne :
    ///   null         → inchangé (laisser le proxy continuer sa chaîne, ex. AYK)
    ///   ""            → supprimer le paquet
    ///   autre string  → contenu de remplacement
    /// </summary>
    public string? Appliquer(string contenu, DirectionPaquet direction)
    {
        if (!Active || string.IsNullOrEmpty(contenu)) return null;

        RegleInterception[] snapshot;
        lock (_verrou) snapshot = _regles.ToArray();

        foreach (var regle in snapshot)
        {
            if (!regle.Correspond(contenu, direction)) continue;

            ResultatInterception res;
            try
            {
                res = regle.Appliquer(contenu, direction);
            }
            catch (Exception ex)
            {
                // Une règle qui crashe ne doit JAMAIS casser le relai → on l'ignore.
                Journaliseur.Avertir($"[INTERCEPT] Règle '{regle.Nom}' a levé {ex.GetType().Name} (ignorée) : {ex.Message}");
                continue;
            }

            switch (res.Action)
            {
                case ActionInterception.Remplacer when res.NouveauContenu != null:
                    Journaliseur.Info($"[INTERCEPT] '{regle.Nom}' modifie [{direction}] : " +
                                      $"{Tronquer(contenu)} → {Tronquer(res.NouveauContenu)}");
                    PaquetModifie?.Invoke(this, new EvenementInterception(
                        regle.Nom, direction, contenu, res.NouveauContenu));
                    return res.NouveauContenu;

                case ActionInterception.Supprimer:
                    Journaliseur.Info($"[INTERCEPT] '{regle.Nom}' supprime [{direction}] : {Tronquer(contenu)}");
                    PaquetModifie?.Invoke(this, new EvenementInterception(
                        regle.Nom, direction, contenu, string.Empty));
                    return string.Empty;

                case ActionInterception.Laisser:
                default:
                    continue;
            }
        }

        return null;
    }

    private static string Tronquer(string s) => s.Length > 50 ? s[..50] + "…" : s;
}

/// <summary>EventArgs émis lors d'une modification de paquet (alimente l'UI/les stats).</summary>
public sealed class EvenementInterception : EventArgs
{
    public EvenementInterception(string regle, DirectionPaquet direction, string avant, string apres)
    {
        Regle = regle;
        Direction = direction;
        Avant = avant;
        Apres = apres;
        Horodatage = DateTime.Now;
    }

    public string Regle { get; }
    public DirectionPaquet Direction { get; }
    public string Avant { get; }
    public string Apres { get; }
    public DateTime Horodatage { get; }
}
