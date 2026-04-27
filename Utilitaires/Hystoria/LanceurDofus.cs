using System;
using System.Diagnostics;
using System.IO;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Utilitaires.Hystoria;

/// <summary>
/// Lance Dofus.exe (le client Flash standalone d'Hystoria) directement, en bypass
/// du launcher Hystoria. Comme on a patché core.swf et le hosts file,
/// le client se connectera automatiquement à notre proxy local.
///
/// Le launcher Hystoria n'apporte rien d'utile dans le flow MITM (login/credentials/etc.)
/// — Dofus.exe fonctionne tout seul si tu lui passes les bons fichiers à côté.
/// </summary>
public sealed class LanceurDofus
{
    /// <summary>Chemin par défaut de Dofus.exe Hystoria sur une install standard.</summary>
    public static readonly string CheminDefaut = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        @"Hystoria\Dofus\resources\app\retroclient\Dofus.exe");

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
        string[] candidats =
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
