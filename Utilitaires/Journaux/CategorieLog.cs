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
    private const string GrisReseau = "#7F8C9A"; // réseau cipher/paquets (REENC/VOCAB)
    private const string Indigo     = "#7986CB"; // network TCP/WD/policy
    private const string Cyan       = "#4FC3F7"; // déplacement / map
    private const string Emeraude   = "#26D07C"; // game (events serveur jeu)
    private const string Vert       = "#7BD389"; // récolte
    private const string VertVif    = "#5CE08A"; // ACTION (story)
    private const string Rouge      = "#FF5370"; // erreurs
    private const string Orange     = "#FFA726"; // avertissements
    private const string Corail     = "#FF8A65"; // bot lifecycle (orch/launch/pilote)
    private const string Ambre      = "#FFCA61"; // inventaire
    private const string AmbreClair = "#FFD988"; // banque / HDV
    private const string Brun       = "#BCAAA4"; // server selection / liste serveurs
    private const string Violet     = "#B388FF"; // script / lua
    private const string VioletVif  = "#D0A2FF"; // commandes / cmd
    private const string Bleu       = "#5C9DFF"; // auth / haapi / login
    private const string Jaune      = "#FFD54F"; // important
    private const string JauneVif   = "#FFE57F"; // quête
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
            "RÉCOLTE" or "RECOLTE" or "RECOLTE " or "FARM"
                => new("Récolte", Vert),
            "MAP" or "CARTE" or "ANKA" or "TRAJET" or "PF" or "ROADREC"
                => new("Trajet", Cyan),
            "INV" => new("Inventaire", Ambre),
            "BANQUE" or "HDV" or "CRAFT" => new("Banque", AmbreClair),
            "CMD" or "COMMANDE" or "COMMANDES"
                => new("Commandes", VioletVif),
            "LUA" or "SCRIPT" or "ANKA-LUA"
                => new("Script", Violet),
            "COMBAT" or "IA" or "SORT" => new("Combat", Rouge),
            // « Bot » = lifecycle / orchestration côté bot (lance le jeu,
            // patche le client, redirige les paquets, contexte compte…).
            // Distinct d'« Auth » qui couvre les paquets login/HAAPI/ticket.
            "ORCH" or "LAUNCH" or "PILOTE" or "AUTO" or "AUTO-ADD"
                or "PATCH" or "SWF" or "BOT"
                => new("Bot", Corail),
            "AUTH" or "CONNEXION" or "CRYPT" or "CIPHER"
                => new("Auth", Bleu),
            "QUETE" or "QUÊTE" => new("Quête", JauneVif),
            "IMPORTANT" or "INFO-JEU" => new("Important", Jaune),
            // « Network » = couche TCP/Windivert/policy (connexion, redirection).
            // Distinct de « Réseau » (paquets cipher déjà parsés).
            "WD" or "POLICY" or "TCP" or "NET" or "NETWORK"
                => new("Network", Indigo),
            // « Server » = liste serveurs, sélection, redirection AYK.
            "SERVER" or "SERVEURS" or "AYK"
                => new("Server", Brun),
            // « Game » = events HAUT NIVEAU du serveur de jeu (entrée en
            // jeu, sélection perso, zaap user-facing). PAS les détails
            // (GA0/GDF/CARTE/UI sont techniques → catégorie Jeu).
            "GAME" or "JEU-HIGH" or "ZAAP"
                => new("Game", Emeraude),
            "REENC" or "OBS" or "OBS-BRUT" or "PKT" or "VOCAB"
                or "CRACK" or "INJ"
                or "REENC C→S" or "OBS C→S '-'"
                => new("Réseau", GrisReseau),
            // « Jeu » = données protocole/runtime techniques (rafraîchissements
            // de carte, échos GA0, GDF état interactif, snapshots d'entités,
            // métiers JSK, base de données apprises, stats As). Logique
            // technique qu'on veut hors du Chat — utile en debug uniquement.
            "MÉTIERS" or "METIERS" or "ENT" or "SORTS" or "BDD"
                or "AS" or "STATS" or "GA0" or "GDF" or "CARTE"
                or "UI" or "DIALOGUE" or "MAP"
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
