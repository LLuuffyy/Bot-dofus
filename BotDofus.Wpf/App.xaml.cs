using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace BotDofus.Wpf;

public partial class App : Application
{
    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            AfficherCrash("Crash non géré (AppDomain)", e.ExceptionObject as Exception);
            // AppDomain crash : process déjà mort, rien à faire de plus.
        };

        DispatcherUnhandledException += (_, e) =>
        {
            AfficherCrash("Crash UI (Dispatcher)", e.Exception);
            e.Handled = true;
            // Sans Shutdown(), le process reste en vie en état pourri (visual tree
            // cassée mais dispatcher actif). On force la fermeture propre pour pas
            // laisser de zombies qui verrouillent l'exe pour le prochain build.
            try { Current?.Shutdown(1); } catch { Environment.Exit(1); }
        };
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Mode CLI de validation parsers : Luffy-bot.exe --testparsers
        // Affiche le résultat dans une MessageBox + console puis quitte.
        // Réutilisable pour démontrer la maîtrise du protocole en soutenance.
        if (e.Args.Contains("--testparsers"))
        {
            var rapport = BotDofus.Commun.Messages.ValidateurParsers.ExecuterTout();
            try { Console.WriteLine(rapport); } catch { }
            MessageBox.Show(rapport, "Luffy-bot — Validation parsers Aqua 1.39",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown(0);
            return;
        }

        base.OnStartup(e);
    }

    private static void AfficherCrash(string titre, Exception? ex)
    {
        // Walk la chaîne InnerException pour voir la vraie cause sous une
        // TargetInvocationException ou XamlParseException.
        var sb = new StringBuilder();
        var courant = ex;
        var profondeur = 0;
        while (courant != null && profondeur < 6)
        {
            sb.AppendLine($"--- Exception #{profondeur} : {courant.GetType().FullName} ---");
            sb.AppendLine(courant.Message);
            if (!string.IsNullOrEmpty(courant.StackTrace))
            {
                // Limite la stack à ~25 lignes pour rester lisible.
                var lignes = courant.StackTrace.Split('\n');
                foreach (var ligne in lignes.Take(25))
                {
                    sb.AppendLine(ligne.TrimEnd());
                }
                if (lignes.Length > 25) sb.AppendLine($"  ... ({lignes.Length - 25} lignes de plus)");
            }
            sb.AppendLine();
            courant = courant.InnerException;
            profondeur++;
        }

        MessageBox.Show(sb.ToString(), $"Luffy-bot — {titre}", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
