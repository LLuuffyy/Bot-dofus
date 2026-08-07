using System.Windows;

namespace BotDofus.Wpf;

public partial class AjouterCompteDialog : Window
{
    public string Identifiant => TxtIdentifiant.Text.Trim();
    public string Login => TxtLogin.Text.Trim();
    public string MotDePasse => TxtMdp.Password;
    public bool ModePassif => ChkPassif.IsChecked == true;

    public AjouterCompteDialog()
    {
        InitializeComponent();
    }

    /// <summary>Constructeur pour pré-remplir le login (capturé depuis le jeu en cours).</summary>
    public AjouterCompteDialog(string loginPreRempli) : this()
    {
        TxtLogin.Text = loginPreRempli ?? string.Empty;
        TxtIdentifiant.Text = loginPreRempli ?? string.Empty;
        Loaded += (_, _) => TxtMdp.Focus();
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Identifiant) && string.IsNullOrWhiteSpace(Login))
        {
            MessageBox.Show("Identifiant ou login requis", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!ModePassif && string.IsNullOrWhiteSpace(MotDePasse))
        {
            MessageBox.Show("Mot de passe requis pour la connexion automatique", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
