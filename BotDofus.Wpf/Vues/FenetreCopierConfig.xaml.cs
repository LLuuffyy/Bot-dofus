using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using BotDofus.Divers;
using BotDofus.Divers.MultiAccount;

namespace BotDofus.Wpf.Vues;

/// <summary>
/// Dialog modale pour copier la <see cref="BotDofus.Divers.Combats.IA.ConfigCombat"/>
/// active vers d'autres persos du groupe. Affiche en cases à cocher la liste
/// des persos cibles possibles (master + tous les héros liés sauf celui en
/// source).
///
/// Au clic Copier, la config source est clonée via sérialisation JSON puis
/// affectée et persistée pour chaque perso coché.
/// </summary>
public partial class FenetreCopierConfig : Window
{
    public ObservableCollection<CibleVm> Cibles { get; } = new();

    private readonly ContexteCompte _contexte;
    private readonly BotDofus.Divers.Combats.IA.ConfigCombat _configSource;
    private readonly MembreHeros? _membreSource;

    public FenetreCopierConfig(
        ContexteCompte contexte,
        BotDofus.Divers.Combats.IA.ConfigCombat configSource,
        MembreHeros? membreSource)
    {
        InitializeComponent();
        _contexte = contexte;
        _configSource = configSource;
        _membreSource = membreSource;
        TxtSource.Text = "Source : " + (membreSource is null
            ? $"{contexte.EtatJeu.Personnage.Nom ?? "Master"} (master)"
            : $"{membreSource.Nom} (id {membreSource.IdJeu})");
        PeuplerCibles();
        ListeCibles.ItemsSource = Cibles;
    }

    private void PeuplerCibles()
    {
        // Master toujours en tête sauf si c'est lui la source.
        if (_membreSource is not null)
        {
            var nomMaster = _contexte.EtatJeu.Personnage.Nom ?? "Master";
            Cibles.Add(new CibleVm
            {
                Membre = null,
                Nom = nomMaster,
                SousTitre = "Master • config peleas/" + _contexte.Compte.Identifiant + ".json",
                Initiale = string.IsNullOrEmpty(nomMaster) ? "?" : nomMaster[0].ToString().ToUpperInvariant(),
                CouleurClasse = CouleurClasse(_contexte.EtatJeu.Personnage.IdClasse),
            });
        }

        var groupe = _contexte.Compte.GroupeHeros;
        if (groupe is not null)
        {
            foreach (var m in groupe.SnapshotMembres())
            {
                if (m.Role == RoleDansGroupe.Leader) continue;
                if (_membreSource is not null && m.IdJeu == _membreSource.IdJeu) continue;
                var nom = string.IsNullOrWhiteSpace(m.Nom) ? $"Perso #{m.IdJeu}" : m.Nom;
                Cibles.Add(new CibleVm
                {
                    Membre = m,
                    Nom = nom,
                    SousTitre = $"id {m.IdJeu} • {NomClasse(m.IdClasse)} • niv {m.Niveau}",
                    Initiale = nom[0].ToString().ToUpperInvariant(),
                    CouleurClasse = CouleurClasse(m.IdClasse),
                });
            }
        }
    }

    private void BtnToutCocher_Click(object sender, RoutedEventArgs e)
    {
        bool toutCoche = Cibles.All(c => c.Selectionne);
        foreach (var c in Cibles) c.Selectionne = !toutCoche;
        // Force le refresh des CheckBox.
        var src = ListeCibles.ItemsSource;
        ListeCibles.ItemsSource = null;
        ListeCibles.ItemsSource = src;
    }

    private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnCopier_Click(object sender, RoutedEventArgs e)
    {
        var cibles = Cibles.Where(c => c.Selectionne).ToList();
        if (cibles.Count == 0)
        {
            MessageBox.Show("Aucune cible cochée.", "Copier config",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        int nbOK = 0;
        foreach (var cible in cibles)
        {
            try
            {
                var clone = ClonerConfig(_configSource);
                if (cible.Membre is null)
                {
                    // Copier vers master.
                    AffecterAuMaster(clone);
                }
                else
                {
                    cible.Membre.ConfigCombat = clone;
                    ServiceConfigsHeros.Sauvegarder(cible.Membre);
                }
                nbOK++;
            }
            catch (System.Exception ex)
            {
                BotDofus.Utilitaires.Journaux.Journaliseur.Avertir(
                    $"[COPIE-CFG] Échec copie vers {cible.Nom} : {ex.Message}");
            }
        }

        MessageBox.Show($"Config copiée vers {nbOK} perso(s).", "Copier config",
            MessageBoxButton.OK, MessageBoxImage.Information);
        DialogResult = true;
        Close();
    }

    private void AffecterAuMaster(BotDofus.Divers.Combats.IA.ConfigCombat clone)
    {
        // On copie les champs un par un sur la ConfigCombat existante du
        // contexte plutôt que de remplacer la référence (le master est
        // partagé avec Compte.ConfigCombat et le moteur de règles).
        var dst = _contexte.ConfigCombat;
        dst.Mode = clone.Mode;
        dst.Strategie = clone.Strategie;
        dst.DistancePreferee = clone.DistancePreferee;
        dst.DistanceMinEloigne = clone.DistanceMinEloigne;
        dst.SeuilFuitePv = clone.SeuilFuitePv;
        dst.Regles.Clear();
        foreach (var r in clone.Regles) dst.Regles.Add(r);
        dst.Sauvegarder(System.IO.Path.Combine("peleas",
            $"{_contexte.Compte.Identifiant}.json"));
    }

    /// <summary>Clone profond via JSON (les ConfigCombat n'ont pas de cloner natif).</summary>
    private static BotDofus.Divers.Combats.IA.ConfigCombat ClonerConfig(
        BotDofus.Divers.Combats.IA.ConfigCombat src)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(src);
        return System.Text.Json.JsonSerializer.Deserialize<BotDofus.Divers.Combats.IA.ConfigCombat>(json)
               ?? new BotDofus.Divers.Combats.IA.ConfigCombat();
    }

    private static Brush CouleurClasse(int idClasse) => idClasse switch
    {
        1 => new SolidColorBrush(Color.FromRgb(0x6F, 0xC4, 0xE8)),
        2 => new SolidColorBrush(Color.FromRgb(0x9C, 0xD8, 0x5D)),
        3 => new SolidColorBrush(Color.FromRgb(0xF2, 0xC8, 0x4B)),
        4 => new SolidColorBrush(Color.FromRgb(0xE0, 0x84, 0xC6)),
        5 => new SolidColorBrush(Color.FromRgb(0xD0, 0x67, 0x5F)),
        6 => new SolidColorBrush(Color.FromRgb(0xC8, 0xB0, 0x84)),
        7 => new SolidColorBrush(Color.FromRgb(0xFF, 0xA8, 0x5C)),
        8 => new SolidColorBrush(Color.FromRgb(0xEF, 0x6B, 0x6B)),
        9 => new SolidColorBrush(Color.FromRgb(0xB5, 0xC8, 0xD8)),
        10 => new SolidColorBrush(Color.FromRgb(0x8B, 0xD0, 0x8B)),
        11 => new SolidColorBrush(Color.FromRgb(0xD2, 0x9A, 0xE8)),
        12 => new SolidColorBrush(Color.FromRgb(0xE8, 0xC8, 0x6F)),
        _ => new SolidColorBrush(Color.FromRgb(0xA0, 0xA8, 0xB8)),
    };

    private static string NomClasse(int idClasse) => idClasse switch
    {
        1 => "Féca", 2 => "Osamodas", 3 => "Enutrof", 4 => "Sram",
        5 => "Xelor", 6 => "Ecaflip", 7 => "Eniripsa", 8 => "Iop",
        9 => "Cra", 10 => "Sadida", 11 => "Sacrieur", 12 => "Pandawa",
        _ => $"Classe {idClasse}",
    };

    public sealed class CibleVm : System.ComponentModel.INotifyPropertyChanged
    {
        public MembreHeros? Membre { get; init; }
        public string Nom { get; init; } = string.Empty;
        public string SousTitre { get; init; } = string.Empty;
        public string Initiale { get; init; } = "?";
        public Brush CouleurClasse { get; init; } = Brushes.Gray;

        private bool _sel;
        public bool Selectionne
        {
            get => _sel;
            set
            {
                if (_sel == value) return;
                _sel = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Selectionne)));
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }
}
