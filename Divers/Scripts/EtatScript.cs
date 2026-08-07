namespace BotDofus.Divers.Scripts;

/// <summary>États de vie d'un script en cours d'exécution.</summary>
public enum EtatScript
{
    /// <summary>Jamais démarré (ou chargement).</summary>
    Inactif,
    /// <summary>Script actif, étape en cours d'exécution.</summary>
    EnExecution,
    /// <summary>Mis en pause par l'utilisateur.</summary>
    EnPause,
    /// <summary>Terminé normalement (fin de boucle / dernière étape).</summary>
    Termine,
    /// <summary>Arrêté suite à une erreur fatale.</summary>
    Erreur,
    /// <summary>Arrêté par l'utilisateur (Stop).</summary>
    Interrompu
}
