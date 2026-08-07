using System;
using System.Windows.Forms;
using BotDofus.Formulaires;

namespace BotDofus;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new FormulairePrincipal());
    }
}
