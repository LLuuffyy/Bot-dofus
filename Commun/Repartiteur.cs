using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BotDofus.Commun.Messages;
using BotDofus.Commun.Reseau;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Commun;

/// <summary>
/// Convertit les paquets bruts reçus du proxy en messages typés (<see cref="MessageDofus"/>)
/// puis les route vers les gestionnaires abonnés par type.
///
/// Modèle d'utilisation :
/// <code>
/// var repartiteur = new Repartiteur();
/// proxy.PaquetRecu += (_, e) =&gt; repartiteur.TraiterPaquet(e.Paquet);
/// repartiteur.Abonner&lt;MessageHelloConnexion&gt;(msg =&gt; ...);
/// </code>
/// </summary>
public sealed class Repartiteur
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _abonnements = new();

    /// <summary>Événement global émis pour chaque message (typé ou inconnu).</summary>
    public event EventHandler<MessageDofus>? MessageRecu;

    /// <summary>Abonne un gestionnaire typé à tous les messages d'un type donné.</summary>
    public void Abonner<T>(Action<T> gestionnaire) where T : MessageDofus
    {
        var liste = _abonnements.GetOrAdd(typeof(T), _ => new List<Delegate>());
        lock (liste) { liste.Add(gestionnaire); }
    }

    /// <summary>Désabonne un gestionnaire précédemment enregistré.</summary>
    public void Desabonner<T>(Action<T> gestionnaire) where T : MessageDofus
    {
        if (_abonnements.TryGetValue(typeof(T), out var liste))
        {
            lock (liste) { liste.Remove(gestionnaire); }
        }
    }

    /// <summary>Traite un paquet brut : le convertit en message typé et dispatche.</summary>
    public void TraiterPaquet(PaquetBrut paquet)
    {
        MessageDofus message;
        try
        {
            message = FabriqueMessages.FabriquerDepuis(paquet);
        }
        catch (Exception ex)
        {
            Journaliseur.Avertir($"Échec de désérialisation du paquet [{paquet.Prefixe}] : {ex.Message}");
            return;
        }

        MessageRecu?.Invoke(this, message);

        var type = message.GetType();
        if (_abonnements.TryGetValue(type, out var liste))
        {
            Delegate[] copies;
            lock (liste) { copies = liste.ToArray(); }
            foreach (var d in copies)
            {
                try { d.DynamicInvoke(message); }
                catch (Exception ex)
                {
                    Journaliseur.Erreur($"Gestionnaire {type.Name} a échoué", ex);
                }
            }
        }

        // Abonnements par hiérarchie : permet d'abonner à MessageDofus ou à une interface.
        foreach (var kv in _abonnements)
        {
            if (kv.Key == type || !kv.Key.IsAssignableFrom(type)) continue;
            Delegate[] copies;
            lock (kv.Value) { copies = kv.Value.ToArray(); }
            foreach (var d in copies)
            {
                try { d.DynamicInvoke(message); }
                catch (Exception ex)
                {
                    Journaliseur.Erreur($"Gestionnaire {kv.Key.Name} a échoué", ex);
                }
            }
        }
    }
}
