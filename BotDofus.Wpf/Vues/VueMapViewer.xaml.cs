using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using BotDofus.Divers;
using BotDofus.Divers.Cartes;
using BotDofus.Divers.Cartes.Entites;
using BotDofus.Divers.Donnees;

namespace BotDofus.Wpf.Vues;

public partial class VueMapViewer : UserControl
{
    private ContexteCompte? _contexte;
    public ObservableCollection<string> EntitesAffichees { get; } = new();

    private double _largeurCellule = 32;
    private double _hauteurCellule = 16;
    private double _decalageX = 500;
    private double _decalageY = 60;

    private readonly Dictionary<int, Polygon> _cellulesPolygons = new();
    private Cellule? _celluleHover;
    private bool _enPan;
    private Point _panOrigine;

    public VueMapViewer()
    {
        InitializeComponent();
        ListeEntites.ItemsSource = EntitesAffichees;
    }

    public void Lier(ContexteCompte contexte)
    {
        _contexte = contexte;
        contexte.PaquetRecu += (_, __) => Dispatcher.Invoke(Rafraichir);
    }

    private void Rafraichir()
    {
        if (_contexte == null) return;
        var carte = _contexte.EtatJeu.CarteCourante;
        if (carte == null) return;

        TxtMapId.Text = $"Carte : {carte.Identifiant} ({carte.Cellules.Length} cells)";
        var pos = _contexte.EtatJeu.Personnage.CellulePosition;
        TxtCellId.Text = $"Cellule : {pos?.ToString() ?? "—"}";
        var cellPerso = pos.HasValue ? carte.Obtenir(pos.Value) : null;
        TxtCoords.Text = cellPerso != null ? $"(x, y) : ({cellPerso.X}, {cellPerso.Y})" : "(x, y) : —";

        // Position footer style SynFus : "Position : [-5, -8] - [2191]"
        var info = BaseDonnees.Instance.Map(carte.Identifiant);
        if (info != null && cellPerso != null)
            TxtPositionFooter.Text = $"Position : [{info.X}, {info.Y}] - [{carte.Identifiant}]  ·  cellule {cellPerso.Identifiant}";
        else
            TxtPositionFooter.Text = $"Position : carte {carte.Identifiant}  ·  cellule {pos}";

        CanvasMap.Children.Clear();
        _cellulesPolygons.Clear();
        DessinerGrille(carte);
        if (ChkAfficherTransitions.IsChecked == true) DessinerTransitions(carte);
        if (ChkAfficherEntites.IsChecked == true) DessinerEntites(carte);
        DessinerJoueur();
        MettreAJourListeEntites(carte);
    }

    private void DessinerGrille(Carte carte)
    {
        bool afficherIds = ChkAfficherIds.IsChecked == true;
        foreach (var cell in carte.Cellules)
        {
            if (cell == null) continue;
            var (cx, cy) = ProjeterIso(cell.X, cell.Y);

            var poly = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(cx, cy),
                    new Point(cx + _largeurCellule, cy + _hauteurCellule),
                    new Point(cx, cy + _hauteurCellule * 2),
                    new Point(cx - _largeurCellule, cy + _hauteurCellule),
                },
                Fill = CouleurCellule(cell),
                Stroke = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                StrokeThickness = 0.5,
                Tag = cell,
            };
            poly.MouseEnter += Poly_MouseEnter;
            poly.MouseLeave += Poly_MouseLeave;
            poly.MouseRightButtonDown += Poly_MouseRightButtonDown;
            poly.Cursor = Cursors.Hand;
            _cellulesPolygons[cell.Identifiant] = poly;
            CanvasMap.Children.Add(poly);

            if (afficherIds)
            {
                var txt = new TextBlock
                {
                    Text = cell.Identifiant.ToString(),
                    Foreground = Brushes.White, FontSize = 8, IsHitTestVisible = false,
                };
                Canvas.SetLeft(txt, cx - 8);
                Canvas.SetTop(txt, cy + _hauteurCellule - 6);
                Canvas.SetZIndex(txt, 5);
                CanvasMap.Children.Add(txt);
            }
        }
    }

    private void DessinerTransitions(Carte carte)
    {
        // Cellules de type Transition affichées en orange avec leur ID (style SynFus)
        foreach (var cell in carte.Cellules)
        {
            if (cell?.Type != TypesCellule.Transition) continue;
            var (cx, cy) = ProjeterIso(cell.X, cell.Y);

            var poly = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(cx, cy),
                    new Point(cx + _largeurCellule, cy + _hauteurCellule),
                    new Point(cx, cy + _hauteurCellule * 2),
                    new Point(cx - _largeurCellule, cy + _hauteurCellule),
                },
                Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                Stroke = Brushes.DarkOrange, StrokeThickness = 1,
                Tag = cell, // permet le clic pour téléporter
                Cursor = Cursors.Hand,
            };
            poly.MouseEnter += Poly_MouseEnter;
            poly.MouseLeave += Poly_MouseLeave;
            Canvas.SetZIndex(poly, 3);
            CanvasMap.Children.Add(poly);

            // Affiche le numéro de cellule (style SynFus : "23" sur les transitions)
            var txt = new TextBlock
            {
                Text = cell.Identifiant.ToString(),
                Foreground = Brushes.White, FontSize = 10, FontWeight = FontWeights.Bold,
                IsHitTestVisible = false,
            };
            Canvas.SetLeft(txt, cx - 8);
            Canvas.SetTop(txt, cy + _hauteurCellule - 7);
            Canvas.SetZIndex(txt, 5);
            CanvasMap.Children.Add(txt);
        }
    }

    /// <summary>Colorie la cellule sous une entité (PNJ violet, Monstre rouge sombre).</summary>
    private void ColorierCelluleSousEntite(Carte carte, Entite ent)
    {
        var cell = carte.Obtenir(ent.CellulePosition);
        if (cell == null) return;
        if (!_cellulesPolygons.TryGetValue(cell.Identifiant, out var poly)) return;

        var couleur = ent switch
        {
            EntiteMonstre => Color.FromRgb(120, 30, 30),  // rouge sombre
            EntitePNJ => Color.FromRgb(110, 60, 140),     // violet
            EntiteJoueur => Color.FromRgb(40, 90, 50),    // vert sombre
            _ => Color.FromRgb(80, 80, 80)
        };
        poly.Fill = new SolidColorBrush(couleur);
    }

    private void DessinerEntites(Carte carte)
    {
        foreach (var ent in carte.Entites.Values)
        {
            var cell = carte.Obtenir(ent.CellulePosition);
            if (cell == null) continue;
            var (cx, cy) = ProjeterIso(cell.X, cell.Y);

            var couleur = ent switch
            {
                EntiteMonstre => Color.FromRgb(220, 60, 60),
                EntitePNJ => Color.FromRgb(80, 180, 220),
                EntiteJoueur => Color.FromRgb(80, 200, 80),
                _ => Color.FromRgb(160, 160, 160)
            };

            var ellipse = new Ellipse
            {
                Width = 18, Height = 18,
                Fill = new SolidColorBrush(couleur),
                Stroke = Brushes.White, StrokeThickness = 1,
                Tag = ent,
                Cursor = Cursors.Hand,
            };
            ellipse.MouseRightButtonDown += Entity_MouseRightButtonDown;
            Canvas.SetLeft(ellipse, cx - 9);
            Canvas.SetTop(ellipse, cy + _hauteurCellule - 9);
            Canvas.SetZIndex(ellipse, 10);
            CanvasMap.Children.Add(ellipse);
        }
    }

    private void DessinerJoueur()
    {
        if (_contexte?.EtatJeu.Personnage.CellulePosition is not int posId) return;
        var cell = _contexte.EtatJeu.CarteCourante?.Obtenir(posId);
        if (cell == null) return;
        var (cx, cy) = ProjeterIso(cell.X, cell.Y);

        var marker = new Ellipse
        {
            Width = 22, Height = 22,
            Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
            Stroke = Brushes.Black, StrokeThickness = 2,
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(marker, cx - 11);
        Canvas.SetTop(marker, cy + _hauteurCellule - 11);
        Canvas.SetZIndex(marker, 20);
        CanvasMap.Children.Add(marker);
    }

    private void MettreAJourListeEntites(Carte carte)
    {
        EntitesAffichees.Clear();
        foreach (var ent in carte.Entites.Values)
        {
            var typeNom = ent.GetType().Name.Replace("Entite", "");
            var info = ent is EntiteMonstre m ? $" Lv{m.NiveauGroupe} (id:{m.IdGabarit})" : "";
            EntitesAffichees.Add($"{typeNom} #{ent.Identifiant} cell {ent.CellulePosition}{info} — {ent.Nom}");
        }
    }

    private (double, double) ProjeterIso(int x, int y)
        => (_decalageX + (x - y) * _largeurCellule, _decalageY + (x + y) * _hauteurCellule);

    private Brush CouleurCellule(Cellule cell)
    {
        if (cell.Type == TypesCellule.Obstacle) return new SolidColorBrush(Color.FromRgb(40, 40, 40));
        if (cell.Type == TypesCellule.Transition) return new SolidColorBrush(Color.FromRgb(255, 152, 0));
        if (cell.EstInteractif) return new SolidColorBrush(Color.FromRgb(120, 90, 50));
        if (cell.LayerNiveau > 0) return new SolidColorBrush(Color.FromRgb((byte)(60 + cell.LayerNiveau * 8), 90, 60));
        return new SolidColorBrush(Color.FromRgb(245, 245, 245)); // marchable = blanc style SynFus
    }

    // -------------------------------------------------------------
    // Interactions
    // -------------------------------------------------------------

    private void Poly_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is not Polygon poly || poly.Tag is not Cellule cell) return;
        _celluleHover = cell;
        poly.Stroke = Brushes.Orange;
        poly.StrokeThickness = 2;
        TxtTooltip.Text = $"Cell #{cell.Identifiant}  ({cell.X}, {cell.Y})\n"
                        + $"Type : {cell.Type}\n"
                        + $"Niveau : {cell.LayerNiveau}  Slope : {cell.LayerSlope}";
        TooltipBorder.Visibility = Visibility.Visible;
    }

    private void Poly_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Polygon poly)
        {
            poly.Stroke = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            poly.StrokeThickness = 0.5;
        }
        _celluleHover = null;
        TooltipBorder.Visibility = Visibility.Collapsed;
    }

    private async void CanvasMap_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        MenuContextuel.Visibility = Visibility.Collapsed;
        if (_celluleHover == null || _contexte == null) return;
        var cible = _celluleHover.Identifiant;
        try { await _contexte.Api.SeDeplacerVersCelluleAsync(cible); }
        catch (Exception ex) { MessageBox.Show($"Déplacement échoué : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void Entity_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is not Ellipse el || el.Tag is not Entite ent || _contexte == null) return;
        AfficherMenuContextuel(ent, e.GetPosition(this));
    }

    private void Poly_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is not Polygon poly || poly.Tag is not Cellule cell) return;
        AfficherMenuContextuelCellule(cell, e.GetPosition(this));
    }

    private void AfficherMenuContextuelCellule(Cellule cell, Point pos)
    {
        MenuContextuelContenu.Children.Clear();

        var titre = new TextBlock
        {
            Text = $"Cellule {cell.Identifiant} — {(cell.EstMarchable ? "marchable" : cell.Type.ToString().ToLower())}",
            FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 4)
        };
        MenuContextuelContenu.Children.Add(titre);
        MenuContextuelContenu.Children.Add(new TextBlock
        {
            Text = $"Coords ({cell.X}, {cell.Y}) · niveau {cell.LayerNiveau}",
            Foreground = Brushes.LightGray, FontSize = 11, Margin = new Thickness(0, 0, 0, 8)
        });

        // Bouton "Copier cellId" — style SynFus
        var btnCopier = new Button
        {
            Content = $"Copier cellId ({cell.Identifiant})",
            Margin = new Thickness(0, 4, 0, 0)
        };
        btnCopier.Click += (_, __) =>
        {
            try { Clipboard.SetText(cell.Identifiant.ToString()); }
            catch { }
            MenuContextuel.Visibility = Visibility.Collapsed;
        };
        MenuContextuelContenu.Children.Add(btnCopier);

        // Si transition, propose action "Aller à"
        if (cell.Type == TypesCellule.Transition && _contexte != null)
        {
            var btnAller = new Button
            {
                Content = $"Aller cellule {cell.Identifiant} (transition)",
                Margin = new Thickness(0, 4, 0, 0)
            };
            btnAller.Click += async (_, __) =>
            {
                MenuContextuel.Visibility = Visibility.Collapsed;
                try { await _contexte.Api.SeDeplacerVersCelluleAsync(cell.Identifiant); }
                catch (Exception ex) { MessageBox.Show($"Échec : {ex.Message}", "Erreur"); }
            };
            MenuContextuelContenu.Children.Add(btnAller);
        }

        var btnFermer = new Button
        {
            Content = "Fermer", Margin = new Thickness(0, 4, 0, 0)
        };
        btnFermer.Click += (_, __) => MenuContextuel.Visibility = Visibility.Collapsed;
        MenuContextuelContenu.Children.Add(btnFermer);

        MenuContextuel.Margin = new Thickness(pos.X, pos.Y, 0, 0);
        MenuContextuel.Visibility = Visibility.Visible;
    }

    private void AfficherMenuContextuel(Entite ent, Point pos)
    {
        MenuContextuelContenu.Children.Clear();

        var titre = new TextBlock
        {
            Text = ent is EntiteMonstre mob ? $"Groupe — {mob.TailleGroupe} mob(s) Lv{mob.NiveauGroupe}"
                                            : $"{ent.GetType().Name.Replace("Entite", "")} #{ent.Identifiant}",
            FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 4)
        };
        MenuContextuelContenu.Children.Add(titre);
        MenuContextuelContenu.Children.Add(new TextBlock
        {
            Text = $"Cellule : {ent.CellulePosition}",
            Foreground = Brushes.LightGray, FontSize = 11, Margin = new Thickness(0, 0, 0, 8)
        });

        // Si monstre, lister les sous-mobs (à compléter selon parsing GM)
        if (ent is EntiteMonstre mobs && mobs.IdGabarit > 0)
        {
            var info = BaseDonnees.Instance.Monstre(mobs.IdGabarit);
            MenuContextuelContenu.Children.Add(new TextBlock
            {
                Text = info != null ? $"{info.Nom} Lv{mobs.NiveauGroupe} (id:{mobs.IdGabarit})" : $"Mob id:{mobs.IdGabarit}",
                Foreground = Brushes.OrangeRed, FontSize = 11, Margin = new Thickness(8, 2, 0, 2)
            });
        }

        // Bouton "Copier pour script"
        var btnCopier = new Button
        {
            Content = "Copier pour script",
            Margin = new Thickness(0, 8, 0, 0)
        };
        btnCopier.Click += (_, __) =>
        {
            Clipboard.SetText($"-- Lua : entité {ent.GetType().Name} cell={ent.CellulePosition} id={ent.Identifiant}\n"
                            + $"bot.deplacer({ent.CellulePosition})\n");
            MenuContextuel.Visibility = Visibility.Collapsed;
        };
        MenuContextuelContenu.Children.Add(btnCopier);

        var btnFermer = new Button
        {
            Content = "Fermer", Margin = new Thickness(0, 4, 0, 0)
        };
        btnFermer.Click += (_, __) => MenuContextuel.Visibility = Visibility.Collapsed;
        MenuContextuelContenu.Children.Add(btnFermer);

        MenuContextuel.Margin = new Thickness(pos.X, pos.Y, 0, 0);
        MenuContextuel.Visibility = Visibility.Visible;
    }

    private void CanvasMap_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Si clic droit sur le canvas vide, on amorce le pan (pas si sur entité, qui a son handler)
        if (e.OriginalSource == CanvasMap)
        {
            _enPan = true;
            _panOrigine = e.GetPosition(this);
            CanvasMap.CaptureMouse();
            MenuContextuel.Visibility = Visibility.Collapsed;
        }
    }

    private void CanvasMap_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        _enPan = false;
        CanvasMap.ReleaseMouseCapture();
    }

    private void CanvasMap_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_enPan) return;
        var p = e.GetPosition(this);
        _decalageX += p.X - _panOrigine.X;
        _decalageY += p.Y - _panOrigine.Y;
        _panOrigine = p;
        Rafraichir();
    }

    private void CanvasMap_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        double facteur = e.Delta > 0 ? 1.15 : 0.87;
        ZoomTransform.ScaleX = Math.Clamp(ZoomTransform.ScaleX * facteur, 0.3, 4.0);
        ZoomTransform.ScaleY = ZoomTransform.ScaleX;
    }

    private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
    {
        ZoomTransform.ScaleX = Math.Clamp(ZoomTransform.ScaleX * 1.2, 0.3, 4.0);
        ZoomTransform.ScaleY = ZoomTransform.ScaleX;
    }

    private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
    {
        ZoomTransform.ScaleX = Math.Clamp(ZoomTransform.ScaleX / 1.2, 0.3, 4.0);
        ZoomTransform.ScaleY = ZoomTransform.ScaleX;
    }

    private void BtnRecentrer_Click(object sender, RoutedEventArgs e)
    {
        _decalageX = 500;
        _decalageY = 60;
        ZoomTransform.ScaleX = 1;
        ZoomTransform.ScaleY = 1;
        Rafraichir();
    }

    private void ChkAfficherIds_Changed(object sender, RoutedEventArgs e) => Rafraichir();
}
