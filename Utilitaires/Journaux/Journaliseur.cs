using System;
using System.Diagnostics;

namespace BotDofus.Utilitaires.Journaux;

/// <summary>
/// Niveaux de gravité de la journalisation, du plus verbeux au plus critique.
/// </summary>
public enum NiveauJournal
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Avertissement = 3,
    Erreur = 4,
    Critique = 5
}

/// <summary>
/// Façade de journalisation minimale, utilisée par toutes les couches.
/// Écrit sur la console, dans le Debug Output de Visual Studio et déclenche
/// un événement que l'UI Debug pourra capter pour afficher les logs en direct.
/// </summary>
public static class Journaliseur
{
    public static NiveauJournal NiveauMinimum { get; set; } = NiveauJournal.Info;

    public static event EventHandler<EvenementEntreeJournal>? EntreeAjoutee;

    public static void Trace(string message) => Ecrire(NiveauJournal.Trace, message);
    public static void Debogue(string message) => Ecrire(NiveauJournal.Debug, message);
    public static void Info(string message) => Ecrire(NiveauJournal.Info, message);
    public static void Avertir(string message) => Ecrire(NiveauJournal.Avertissement, message);
    public static void Erreur(string message, Exception? ex = null)
        => Ecrire(NiveauJournal.Erreur, ex is null ? message : $"{message} | {ex.GetType().Name}: {ex.Message}");
    public static void Critique(string message, Exception? ex = null)
        => Ecrire(NiveauJournal.Critique, ex is null ? message : $"{message} | {ex.GetType().Name}: {ex.Message}");

    private static void Ecrire(NiveauJournal niveau, string message)
    {
        if (niveau < NiveauMinimum) return;

        var entree = new EntreeJournal(DateTime.Now, niveau, message);
        var ligne = $"[{entree.Horodatage:HH:mm:ss.fff}] [{niveau,-13}] {message}";

        Debug.WriteLine(ligne);
        Console.WriteLine(ligne);
        EntreeAjoutee?.Invoke(null, new EvenementEntreeJournal(entree));
    }
}

public readonly record struct EntreeJournal(DateTime Horodatage, NiveauJournal Niveau, string Message);

public sealed class EvenementEntreeJournal : EventArgs
{
    public EvenementEntreeJournal(EntreeJournal entree) { Entree = entree; }
    public EntreeJournal Entree { get; }
}
