using System;
using System.Collections.Generic;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Commun.Frames;

/// <summary>
/// Machine à états légère qui garantit qu'une seule Frame à la fois est active
/// (sauf autorisation explicite via <see cref="EmpilerTrame"/> pour les Frames
/// transitoires type dialogue / échange).
/// </summary>
public sealed class GestionnaireTrames
{
    private readonly Stack<TrameBase> _pile = new();

    public TrameBase? TrameActive => _pile.Count > 0 ? _pile.Peek() : null;
    public int ProfondeurPile => _pile.Count;

    public event EventHandler<TrameBase>? TrameActivee;
    public event EventHandler<TrameBase>? TrameDesactivee;

    /// <summary>Remplace la Frame courante par une nouvelle (désactive l'ancienne, active la nouvelle).</summary>
    public void RemplacerTrame(TrameBase nouvelle)
    {
        Vider();
        EmpilerTrame(nouvelle);
    }

    /// <summary>Empile une Frame transitoire par dessus la Frame active (sans désactiver cette dernière).</summary>
    public void EmpilerTrame(TrameBase nouvelle)
    {
        _pile.Push(nouvelle);
        nouvelle.Activer();
        Journaliseur.Info($"Trame activée : {nouvelle.GetType().Name} (profondeur = {_pile.Count})");
        TrameActivee?.Invoke(this, nouvelle);
    }

    /// <summary>Dépile la Frame au sommet de la pile (ex. fin d'un dialogue).</summary>
    public TrameBase? DepilerTrame()
    {
        if (_pile.Count == 0) return null;
        var enleve = _pile.Pop();
        enleve.Desactiver();
        Journaliseur.Info($"Trame désactivée : {enleve.GetType().Name} (profondeur = {_pile.Count})");
        TrameDesactivee?.Invoke(this, enleve);
        return enleve;
    }

    /// <summary>Vide toute la pile (fin de session).</summary>
    public void Vider()
    {
        while (_pile.Count > 0) DepilerTrame();
    }
}
