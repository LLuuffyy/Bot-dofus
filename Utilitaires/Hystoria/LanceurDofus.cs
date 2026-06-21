using System;
using System.Diagnostics;
using System.IO;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Utilitaires.Hystoria;

/// <summary>
/// Lance Dofus.exe (le client Flash standalone Dofus Retro) directement, en
/// bypass du launcher Electron. Le client se connecte automatiquement à notre
/// proxy local via redirection WinDivert (ou patch hosts/config.xml).
///
/// Le chemin par défaut est lu depuis <c>ConfigReseau.CheminClientDofus</c>
/// pour suivre la cible courante (Rafale = <c>C:\Games\Rafal\Dofus.exe</c>).
/// </summary>
public sealed class LanceurDofus
{
    /// <summary>Chemin par défaut de Dofus.exe pour la cible courante,
    /// lu depuis <see cref="BotDofus.Commun.Reseau.ConfigReseau.CheminClientDofus"/>.</summary>
    public static string CheminDefaut
        => BotDofus.Commun.Reseau.ConfigReseau.ChargerOuDefaut().CheminClientDofus;

    public string CheminExe { get; }

    public LanceurDofus(string? cheminExe = null)
    {
        CheminExe = cheminExe ?? CheminDefaut;
    }

    public bool ExisteCoteDisque() => File.Exists(CheminExe);

    /// <summary>
    /// Lance Dofus.exe et retourne le Process. La working directory est positionnée
    /// au dossier du .exe (sinon le client ne trouve pas ses ressources).
    /// </summary>
    public Process Lancer()
    {
        if (!ExisteCoteDisque())
        {
            throw new FileNotFoundException($"Dofus.exe introuvable : {CheminExe}");
        }

        var psi = new ProcessStartInfo
        {
            FileName = CheminExe,
            WorkingDirectory = Path.GetDirectoryName(CheminExe)!,
            UseShellExecute = false,
            CreateNoWindow = false,
        };

        var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Process.Start a retourné null pour " + CheminExe);

        Journaliseur.Info($"[LAUNCH] Dofus.exe lancé (PID : {proc.Id})");
        return proc;
    }

    /// <summary>
    /// Localise automatiquement Dofus.exe en cherchant dans les emplacements standards.
    /// Retourne null si introuvable.
    /// </summary>
    public static string? Localiser()
    {
        var candidats = new[]
        {
            CheminDefaut,
        };

        foreach (var c in candidats)
        {
            if (File.Exists(c)) return c;
        }
        return null;
    }
}
