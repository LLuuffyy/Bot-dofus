using System;

namespace BotDofus.Utilitaires.Journaux;

/// <summary>
/// Catégorisation visuelle des lignes de log (style MoonBot) : à partir
/// du tag <c>[XXX]</c> en tête de message + du niveau, déduit un libellé
/// court et une couleur. PUR / sans état → réutilisable UI et tests.
/// Aucune modification des appelants : la catégorie est dérivée à
/// l'affichage du tag déjà présent dans le message.
/// </summary>
public static class CategorieLog
{
    public readonly record struct Categorie(string Nom, string CouleurHex);

    // Palette sombre alignée sur le rendu MoonBot souhaité.
    private const string GrisReseau = "#7F8C9A"; // réseau bas niveau
    private const string Cyan       = "#4FC3F7"; // déplacement / map
    private const string Vert       = "#7BD389"; // récolte
    private const string VertVif    = "#5CE08A"; // ACTION (story)
    private const string Rouge      = "#FF5370"; // erreurs
    private const string Orange     = "#FFA726"; // avertissements
    private const string Ambre      = "#FFCA61"; // inventaire / banque
    private const string Violet     = "#B388FF"; // script / lua / cmd
    private const string Bleu       = "#5C9DFF"; // auth / connexion
    private const string Jaune      = "#FFD54F"; // important / quête
    private const string Defaut     = "#E0E5EC"; // info neutre

    /// <summary>
    /// Déduit (libellé, couleur) d'une entrée. Le niveau prime pour
    /// erreur/avertissement (rouge/orange visibles avant tout), sinon
    /// on classe par tag de tête.
    /// </summary>
    public static Categorie Resoudre(string message, NiveauJournal niveau)
    {
        if (niveau is NiveauJournal.Erreur or NiveauJournal.Critique)
            return new("Erreur", Rouge);
        if (niveau == NiveauJournal.Avertissement)
            return new("Alerte", Orange);

        var tag = ExtraireTag(message);
        return tag switch
        {
            "ACTION" => new("Action", VertVif),
            "RÉCOLTE" or "RECOLTE" or "RECOLTE " => new("Récolte", Vert),
            "MAP" or "CARTE" or "ANKA" or "TRAJET" or "PF" => new("Trajet", Cyan),
            "INV" or "BANQUE" or "HDV" or "CRAFT" => new("Inventaire", Ambre),
            "LUA" or "SCRIPT" or "CMD" or "ANKA-LUA" => new("Script", Violet),
            "COMBAT" or "IA" or "SORT" => new("Combat", Rouge),
            "AUTH" or "CONNEXION" or "ORCH" or "LAUNCH" or "PILOTE"
                => new("Auth", Bleu),
            "IMPORTANT" or "QUETE" or "QUÊTE" or "INFO-JEU"
                => new("Important", Jaune),
            "WD" or "REENC" or "CRYPT" or "OBS" or "OBS-BRUT" or "PKT"
                or "VOCAB" or "CRACK" or "POLICY" or "CIPHER" or "INJ"
                or "REENC C→S" or "OBS C→S '-'"
                => new("Réseau", GrisReseau),
            "MÉTIERS" or "METIERS" or "ENT" or "SORTS" or "BDD"
                => new("Jeu", GrisReseau),
            _ => new("Info", Defaut),
        };
    }

    /// <summary>Tag entre crochets en tête de message, MAJUSCULE, ou "".</summary>
    public static string ExtraireTag(string message)
    {
        if (string.IsNullOrEmpty(message) || message[0] != '[') return string.Empty;
        var fin = message.IndexOf(']');
        if (fin <= 1) return string.Empty;
        return message.Substring(1, fin - 1).Trim().ToUpperInvariant();
    }
}
