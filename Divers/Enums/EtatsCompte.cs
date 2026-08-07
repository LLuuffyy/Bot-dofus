namespace BotDofus.Divers.Enums;

/// <summary>
/// États de haut niveau d'un compte bot durant sa session.
/// </summary>
public enum EtatsCompte
{
    Deconnecte,
    Connexion,
    SelectionServeur,
    FileAttente,
    SelectionPersonnage,
    Regeneration,
    EnJeu,
    Deplacement,
    Dialogue,
    Echange,
    EnCombat,
    ScriptEnCours,
    ScriptEnPause,
    Erreur
}
