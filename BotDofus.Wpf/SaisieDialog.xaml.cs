using System.Windows;
using System.Windows.Input;

namespace BotDofus.Wpf;

/// <summary>
/// Petit dialog WPF "saisir une valeur" propre et stylé dark, en remplacement de
/// Microsoft.VisualBasic.Interaction.InputBox qui rend mal en dark theme.
/// </summary>
public partial class SaisieDialog : Window
{
    public string Valeur { get; private set; } = string.Empty;

    public SaisieDialog(string titre, string message, string valeurInitiale = "")
    {
        InitializeComponent();
        Title = titre;
        TxtMessage.Text = message;
        TxtSaisie.Text = valeurInitiale;
        Loaded += (_, _) => { TxtSaisie.Focus(); TxtSaisie.SelectAll(); };
    }

    /// <summary>Helper static : ouvre le dialog, retourne la valeur saisie ou null si annulé.</summary>
    public static string? Demander(Window? parent, string titre, string message, string valeurInitiale = "")
    {
        var dlg = new SaisieDialog(titre, message, valeurInitiale);
        if (parent != null) dlg.Owner = parent;
        return dlg.ShowDialog() == true ? dlg.Valeur : null;
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        Valeur = TxtSaisie.Text ?? string.Empty;
        DialogResult = true;
    }

    private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void TxtSaisie_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) DialogResult = false;
    }
}
