using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BotDofus.Divers;
using IoFile = System.IO.File;
using IoPath = System.IO.Path;

namespace BotDofus.Wpf.Vues;

public partial class VueWorldMap : UserControl
{
    private const double TileSize = 250;
    private readonly List<TuileWorldMap> _tuiles = new();
    private readonly Dictionary<int, (int x, int y)> _coordsParCarte = new();
    private ContexteCompte? _contexte;
    private ScaleTransform _zoom = new(1, 1);
    private TuileWorldMap? _tuileSelectionnee;
    private Ellipse? _marqueurPerso;
    private Ellipse? _haloPerso;
    private Rectangle? _bordureTuilePerso;
    private int _minXCache, _minYCache;
    private bool _centrageInitial = true;

    public VueWorldMap()
    {
        InitializeComponent();
        CanvasWorld.LayoutTransform = _zoom;
        ChargerWorldMap();
        ChargerCoordsCartes();
    }

    public void Lier(ContexteCompte contexte)
    {
        _contexte = contexte;
        // Quand le perso change de map, on actualise le marqueur de position.
        contexte.PaquetRecu += (_, __) => Dispatcher.Invoke(MettreAJourMarqueurPerso);
        MettreAJourMarqueurPerso();
    }

    private void ChargerCoordsCartes()
    {
        var chemin = IoPath.Combine(AppContext.BaseDirectory, "Resources", "data", "maps_hystoria.json");
        if (!IoFile.Exists(chemin)) return;

        try
        {
            using var doc = JsonDocument.Parse(IoFile.ReadAllText(chemin));
            if (!doc.RootElement.TryGetProperty("maps", out var maps)) return;
            foreach (var prop in maps.EnumerateObject())
            {
                if (!int.TryParse(prop.Name, out var idCarte)) continue;
                if (!prop.Value.TryGetProperty("x", out var xp)) continue;
                if (!prop.Value.TryGetProperty("y", out var yp)) continue;
                _coordsParCarte[idCarte] = (xp.GetInt32(), yp.GetInt32());
            }
        }
        catch (Exception ex)
        {
            Utilitaires.Journaux.Journaliseur.Avertir($"[MAPVIEW] Echec chargement maps_hystoria.json : {ex.Message}");
        }
    }

    private void MettreAJourMarqueurPerso()
    {
        if (_contexte == null || _marqueurPerso == null) return;
        var idCarte = _contexte.EtatJeu.Personnage.CarteCourante;
        if (idCarte == null || !_coordsParCarte.TryGetValue(idCarte.Value, out var coords))
        {
            _marqueurPerso.Visibility = Visibility.Collapsed;
            if (_haloPerso != null) _haloPerso.Visibility = Visibility.Collapsed;
            if (_bordureTuilePerso != null) _bordureTuilePerso.Visibility = Visibility.Collapsed;
            return;
        }

        var centreLeft = (coords.x - _minXCache) * TileSize + TileSize / 2;
        var centreTop = (coords.y - _minYCache) * TileSize + TileSize / 2;

        Canvas.SetLeft(_marqueurPerso, centreLeft - 12);
        Canvas.SetTop(_marqueurPerso, centreTop - 12);
        Canvas.SetZIndex(_marqueurPerso, 100);
        _marqueurPerso.Visibility = Visibility.Visible;

        if (_haloPerso != null)
        {
            Canvas.SetLeft(_haloPerso, centreLeft - 30);
            Canvas.SetTop(_haloPerso, centreTop - 30);
            Canvas.SetZIndex(_haloPerso, 99);
            _haloPerso.Visibility = Visibility.Visible;
        }

        if (_bordureTuilePerso != null)
        {
            Canvas.SetLeft(_bordureTuilePerso, (coords.x - _minXCache) * TileSize);
            Canvas.SetTop(_bordureTuilePerso, (coords.y - _minYCache) * TileSize);
            Canvas.SetZIndex(_bordureTuilePerso, 98);
            _bordureTuilePerso.Visibility = Visibility.Visible;
        }

        if (TxtStatut != null)
        {
            TxtStatut.Text = $"Perso sur carte #{idCarte} = [{coords.x},{coords.y}]";
        }

        // Auto-centre la worldmap sur le perso au premier marker visible.
        if (_centrageInitial && Scroller != null)
        {
            _centrageInitial = false;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Scroller is null) return;
                Scroller.ScrollToHorizontalOffset(centreLeft * _zoom.ScaleX - Scroller.ViewportWidth / 2);
                Scroller.ScrollToVerticalOffset(centreTop * _zoom.ScaleY - Scroller.ViewportHeight / 2);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    /// <summary>Bouton "Centrer perso" : recentre la vue sur la position courante.</summary>
    public void CentrerSurPerso()
    {
        _centrageInitial = true;
        MettreAJourMarqueurPerso();
    }

    private void ChargerWorldMap()
    {
        _tuiles.Clear();
        CanvasWorld.Children.Clear();

        var dossier = IoPath.Combine(AppContext.BaseDirectory, "Resources", "worldmap");
        var mapping = IoPath.Combine(dossier, "tile_mapping.json");
        if (!IoFile.Exists(mapping))
        {
            TxtStatut.Text = "Resources/worldmap introuvable";
            return;
        }

        var doc = JsonDocument.Parse(IoFile.ReadAllText(mapping));
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var item = prop.Value;
            if (!item.TryGetProperty("x", out var xProp) || !item.TryGetProperty("y", out var yProp) || !item.TryGetProperty("file", out var fileProp))
            {
                continue;
            }

            var tuile = new TuileWorldMap
            {
                X = xProp.GetInt32(),
                Y = yProp.GetInt32(),
                Fichier = fileProp.GetString() ?? string.Empty
            };
            if (IoFile.Exists(IoPath.Combine(dossier, tuile.Fichier)))
            {
                _tuiles.Add(tuile);
            }
        }

        Dessiner();
    }

    private void Dessiner()
    {
        if (CanvasWorld is null || TxtStatut is null)
        {
            return;
        }

        CanvasWorld.Children.Clear();
        if (_tuiles.Count == 0) return;

        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;
        foreach (var tuile in _tuiles)
        {
            minX = Math.Min(minX, tuile.X);
            minY = Math.Min(minY, tuile.Y);
            maxX = Math.Max(maxX, tuile.X);
            maxY = Math.Max(maxY, tuile.Y);
        }

        var dossier = IoPath.Combine(AppContext.BaseDirectory, "Resources", "worldmap");
        foreach (var tuile in _tuiles)
        {
            var left = (tuile.X - minX) * TileSize;
            var top = (tuile.Y - minY) * TileSize;
            var chemin = IoPath.Combine(dossier, tuile.Fichier);

            var imageSource = ChargerImageTuile(chemin);
            if (imageSource != null)
            {
                var image = new Image
                {
                    Width = TileSize,
                    Height = TileSize,
                    Stretch = Stretch.Fill,
                    Source = imageSource,
                    Tag = tuile,
                    Cursor = Cursors.Hand
                };
                image.MouseRightButtonDown += Tuile_MouseRightButtonDown;
                image.MouseLeftButtonDown += Tuile_MouseLeftButtonDown;
                Canvas.SetLeft(image, left);
                Canvas.SetTop(image, top);
                CanvasWorld.Children.Add(image);
            }
            else
            {
                var placeholder = new Rectangle
                {
                    Width = TileSize,
                    Height = TileSize,
                    Fill = new SolidColorBrush(Color.FromRgb(82, 84, 88)),
                    Tag = tuile,
                    Cursor = Cursors.Hand
                };
                placeholder.MouseRightButtonDown += Tuile_MouseRightButtonDown;
                placeholder.MouseLeftButtonDown += Tuile_MouseLeftButtonDown;
                Canvas.SetLeft(placeholder, left);
                Canvas.SetTop(placeholder, top);
                CanvasWorld.Children.Add(placeholder);
            }

            if (ChkGrille.IsChecked == true)
            {
                var rect = new Rectangle
                {
                    Width = TileSize,
                    Height = TileSize,
                    Stroke = new SolidColorBrush(Color.FromArgb(70, 20, 20, 20)),
                    StrokeThickness = 1,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(rect, left);
                Canvas.SetTop(rect, top);
                CanvasWorld.Children.Add(rect);
            }

            if (ChkCoords.IsChecked == true)
            {
                var label = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(180, 30, 34, 40)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 3, 6, 3),
                    IsHitTestVisible = false,
                    Child = new TextBlock
                    {
                        Text = $"[{tuile.X},{tuile.Y}]",
                        Foreground = Brushes.White,
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 11
                    }
                };
                Canvas.SetLeft(label, left + 8);
                Canvas.SetTop(label, top + 8);
                CanvasWorld.Children.Add(label);
            }
        }

        CanvasWorld.Width = (maxX - minX + 1) * TileSize;
        CanvasWorld.Height = (maxY - minY + 1) * TileSize;

        // Mémorise minX/minY pour pouvoir repositionner le marqueur perso après chaque redessin.
        _minXCache = minX;
        _minYCache = minY;

        // Marqueur position perso : halo qui pulse + pastille rouge.
        _haloPerso = new Ellipse
        {
            Width = 60, Height = 60,
            Stroke = new SolidColorBrush(Color.FromArgb(200, 0xFF, 0x44, 0x55)),
            StrokeThickness = 4,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        CanvasWorld.Children.Add(_haloPerso);
        var pulseAnim = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 0.15, To = 1.0,
            Duration = TimeSpan.FromSeconds(1),
            AutoReverse = true,
            RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
        };
        _haloPerso.BeginAnimation(UIElement.OpacityProperty, pulseAnim);

        _marqueurPerso = new Ellipse
        {
            Width = 24,
            Height = 24,
            Fill = new SolidColorBrush(Color.FromRgb(0xFF, 0x44, 0x55)),
            Stroke = Brushes.White,
            StrokeThickness = 2,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        CanvasWorld.Children.Add(_marqueurPerso);

        // Bordure de surlignage de la tuile courante.
        _bordureTuilePerso = new Rectangle
        {
            Width = TileSize, Height = TileSize,
            Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0x44, 0x55)),
            StrokeThickness = 3,
            Fill = Brushes.Transparent,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        CanvasWorld.Children.Add(_bordureTuilePerso);

        MettreAJourMarqueurPerso();

        TxtStatut.Text = $"{_tuiles.Count} tuiles | clic gauche remplit .travel | clic droit ouvre le menu";
    }

    private void Tuile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!ExtraireTuile(sender, out var tuile)) return;
        Selectionner(tuile);
        MenuContextuel.Visibility = Visibility.Collapsed;
    }

    private void Tuile_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (!ExtraireTuile(sender, out var tuile)) return;
        Selectionner(tuile);
        TxtMenuCoords.Text = $"[{tuile.X}, {tuile.Y}]";
        var pos = e.GetPosition(this);
        MenuContextuel.Margin = new Thickness(pos.X, pos.Y, 0, 0);
        MenuContextuel.Visibility = Visibility.Visible;
    }

    private void Selectionner(TuileWorldMap tuile)
    {
        _tuileSelectionnee = tuile;
        TxtX.Text = tuile.X.ToString();
        TxtY.Text = tuile.Y.ToString();
        TxtStatut.Text = $"Selection : .travel {tuile.X},{tuile.Y}";
    }

    private static ImageSource? ChargerImageTuile(string chemin)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(chemin, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    private static bool ExtraireTuile(object sender, out TuileWorldMap tuile)
    {
        tuile = null!;
        if (sender is FrameworkElement { Tag: TuileWorldMap t })
        {
            tuile = t;
            return true;
        }

        return false;
    }

    private void BtnCentrerPerso_Click(object sender, RoutedEventArgs e)
    {
        CentrerSurPerso();
    }

    private async void BtnTravel_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtX.Text, out var x) || !int.TryParse(TxtY.Text, out var y))
        {
            MessageBox.Show("Coordonnees invalides.", "MapViewer", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await EnvoyerTravel(x, y);
    }

    private async void BtnMenuTravel_Click(object sender, RoutedEventArgs e)
    {
        if (_tuileSelectionnee == null) return;
        MenuContextuel.Visibility = Visibility.Collapsed;
        await EnvoyerTravel(_tuileSelectionnee.X, _tuileSelectionnee.Y);
    }

    private void BtnMenuCopier_Click(object sender, RoutedEventArgs e)
    {
        if (_tuileSelectionnee == null) return;
        Clipboard.SetText($".travel {_tuileSelectionnee.X},{_tuileSelectionnee.Y}");
        MenuContextuel.Visibility = Visibility.Collapsed;
        TxtStatut.Text = $"Copie : .travel {_tuileSelectionnee.X},{_tuileSelectionnee.Y}";
    }

    private async Task EnvoyerTravel(int x, int y)
    {
        if (_contexte == null)
        {
            MessageBox.Show("Aucun compte selectionne.", "MapViewer", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await _contexte.Api.EnvoyerTravelAsync(x, y, CancellationToken.None);
        TxtStatut.Text = $"Commande envoyee : .travel {x},{y}";
    }

    private void CanvasWorld_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var facteur = e.Delta > 0 ? 1.1 : 0.9;
        _zoom.ScaleX = Math.Clamp(_zoom.ScaleX * facteur, 0.35, 3.0);
        _zoom.ScaleY = _zoom.ScaleX;
    }

    private void Options_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized || CanvasWorld is null)
        {
            return;
        }

        Dessiner();
    }

    private sealed class TuileWorldMap
    {
        public int X { get; init; }
        public int Y { get; init; }
        public string Fichier { get; init; } = "";
    }
}
