using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using BotDofus.Commun.Reseau;
using BotDofus.Divers;
using BotDofus.Divers.Cartes;
using BotDofus.Divers.Cartes.Entites;
using BotDofus.Divers.Donnees;

namespace BotDofus.Wpf.Vues;

public partial class VueMapViewer : UserControl
{
    private ContexteCompte? _contexte;
    private ContexteCompte? _contexteLie;
    private BotDofus.Utilitaires.Auto.CalibrationClic? _calibration;
    public ObservableCollection<string> EntitesAffichees { get; } = new();

    private double _largeurCellule = 32;
    private double _hauteurCellule = 16;
    private double _decalageX = 500;
    private double _decalageY = 60;

    private readonly Dictionary<int, Polygon> _cellulesPolygons = new();
    private Cellule? _celluleHover;
    private bool _enPan;
    private Point _panOrigine;
    private bool _centrageNecessaire = true;
    private int _carteCarteSuivie = -1;
    private int? _celluleSelectionnee;

    public VueMapViewer()
    {
        InitializeComponent();
        ListeEntites.ItemsSource = EntitesAffichees;
    }

    public void Lier(ContexteCompte contexte)
    {
        if (ReferenceEquals(_contexteLie, contexte)) return;
        if (_contexteLie != null)
        {
            _contexteLie.PaquetRecu -= OnPaquet;
        }

        _contexteLie = contexte;
        _contexte = contexte;
        contexte.PaquetRecu += OnPaquet;
        Rafraichir();
    }

    private void OnPaquet(object? sender, EvenementPaquetRecu e)
    {
        var contenu = e.Paquet.Contenu;
        // GT* (GTM combattants+cellules, GTS tour, GTF/GTR) : indispensable
        // pour rafraîchir la grille PENDANT le combat — les positions des
        // combattants Abrak arrivent par GTM (préfixe "GT", PAS "GM").
        if (!contenu.StartsWith("GM", StringComparison.Ordinal)
            && !contenu.StartsWith("GT", StringComparison.Ordinal)
            && !contenu.StartsWith("GDM", StringComparison.Ordinal)
            && !contenu.StartsWith("GDK", StringComparison.Ordinal)
            && !contenu.StartsWith("GDF", StringComparison.Ordinal)
            && !contenu.StartsWith("GP", StringComparison.Ordinal)
            && !contenu.StartsWith("GS", StringComparison.Ordinal)
            && !contenu.StartsWith("GE", StringComparison.Ordinal))
        {
            return;
        }

        Dispatcher.BeginInvoke(new Action(Rafraichir), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void Rafraichir()
    {
        if (_contexte == null) return;
        var carte = _contexte.EtatJeu.CarteCourante;
        if (carte == null) return;

        // Détection changement de carte → flag pour re-centrer.
        if (carte.Identifiant != _carteCarteSuivie)
        {
            _carteCarteSuivie = carte.Identifiant;
            _centrageNecessaire = true;
            _celluleSelectionnee = _contexte.EtatJeu.Personnage.CellulePosition;
            RecalculerCadreCarte(carte);
        }

        var info = BaseDonnees.Instance.Map(carte.Identifiant);
        TxtMapId.Text = info != null
            ? $"Carte : {carte.Identifiant} [{info.X},{info.Y}] ({carte.Cellules.Length} cells)"
            : $"Carte : {carte.Identifiant} ({carte.Cellules.Length} cells)";
        var pos = _contexte.EtatJeu.Personnage.CellulePosition;
        TxtCellId.Text = $"Cellule : {pos?.ToString() ?? "-"}";
        var cellPerso = pos.HasValue ? carte.Obtenir(pos.Value) : null;
        TxtCoords.Text = cellPerso != null ? $"(x, y) : ({cellPerso.X}, {cellPerso.Y})" : "(x, y) : -";

        if (info != null && cellPerso != null)
        {
            TxtPositionFooter.Text = $"Position : [{info.X}, {info.Y}] - [{carte.Identifiant}] | cellule {cellPerso.Identifiant}";
            TxtTravelX.Text = info.X.ToString();
            TxtTravelY.Text = info.Y.ToString();
        }
        else
        {
            TxtPositionFooter.Text = $"Position : carte {carte.Identifiant} | cellule {pos}";
        }

        CanvasMap.Children.Clear();
        _cellulesPolygons.Clear();
        DessinerGrille(carte);
        DessinerCellulesPlacement(carte);
        if (ChkAfficherTransitions.IsChecked == true) DessinerTransitions(carte);
        if (ChkAfficherEntites.IsChecked == true) DessinerEntites(carte);
        DessinerJoueur();
        MettreAJourListeEntites(carte);
        CentrerSiNecessaire(carte);
    }

    private void RecalculerCadreCarte(Carte carte)
    {
        var bounds = CalculerBounds(carte, decalageX: 0, decalageY: 0);
        _decalageX = -bounds.minX + 90;
        _decalageY = -bounds.minY + 90;

        var framed = CalculerBounds(carte, _decalageX, _decalageY);
        CanvasMap.Width = Math.Max(900, framed.maxX + 90);
        CanvasMap.Height = Math.Max(620, framed.maxY + 90);
    }

    private (double minX, double minY, double maxX, double maxY) CalculerBounds(Carte carte, double decalageX, double decalageY)
    {
        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        foreach (var cell in carte.Cellules)
        {
            if (cell == null) continue;
            var cx = decalageX + (cell.X - cell.Y) * _largeurCellule;
            var cy = decalageY + (cell.X + cell.Y) * _hauteurCellule;
            minX = Math.Min(minX, cx - _largeurCellule);
            minY = Math.Min(minY, cy);
            maxX = Math.Max(maxX, cx + _largeurCellule);
            maxY = Math.Max(maxY, cy + _hauteurCellule * 2);
        }

        if (minX == double.MaxValue) return (0, 0, 900, 620);
        return (minX, minY, maxX, maxY);
    }

    private void CentrerSiNecessaire(Carte carte)
    {
        if (!_centrageNecessaire || ScrollerMap == null) return;
        var cellule = _contexte?.EtatJeu.Personnage.CellulePosition is int pos ? carte.Obtenir(pos) : null;
        if (cellule == null) return;

        _centrageNecessaire = false;
        var (cx, cy) = ProjeterIso(cellule.X, cellule.Y);
        Dispatcher.BeginInvoke(new Action(() =>
        {
            ScrollerMap.ScrollToHorizontalOffset(Math.Max(0, cx * ZoomTransform.ScaleX - ScrollerMap.ViewportWidth / 2));
            ScrollerMap.ScrollToVerticalOffset(Math.Max(0, cy * ZoomTransform.ScaleY - ScrollerMap.ViewportHeight / 2));
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void DessinerGrille(Carte carte)
    {
        bool afficherIds = ChkAfficherIds.IsChecked == true;
        foreach (var cell in carte.Cellules)
        {
            if (cell == null) continue;
            var (cx, cy) = ProjeterIso(cell.X, cell.Y);

            var poly = CreerPolygoneCellule(cell, CouleurCellule(cell), 0.5);
            if (_celluleSelectionnee == cell.Identifiant)
            {
                poly.Stroke = new SolidColorBrush(Color.FromRgb(0x6C, 0x76, 0xFF));
                poly.StrokeThickness = 2.2;
            }
            poly.MouseEnter += Poly_MouseEnter;
            poly.MouseLeave += Poly_MouseLeave;
            poly.MouseRightButtonDown += Poly_MouseRightButtonDown;
            _cellulesPolygons[cell.Identifiant] = poly;
            CanvasMap.Children.Add(poly);

            if (!afficherIds) continue;
            var txt = new TextBlock
            {
                Text = cell.Identifiant.ToString(),
                Foreground = Brushes.White,
                FontSize = 8,
                IsHitTestVisible = false,
            };
            Canvas.SetLeft(txt, cx - 8);
            Canvas.SetTop(txt, cy + _hauteurCellule - 6);
            Canvas.SetZIndex(txt, 5);
            CanvasMap.Children.Add(txt);
        }
    }

    private void DessinerTransitions(Carte carte)
    {
        foreach (var cell in carte.Cellules)
        {
            if (cell?.Type != TypesCellule.Transition) continue;
            var (cx, cy) = ProjeterIso(cell.X, cell.Y);

            var poly = CreerPolygoneCellule(cell, new SolidColorBrush(Color.FromRgb(255, 152, 0)), 1);
            poly.Stroke = Brushes.DarkOrange;
            poly.MouseEnter += Poly_MouseEnter;
            poly.MouseLeave += Poly_MouseLeave;
            poly.MouseRightButtonDown += Poly_MouseRightButtonDown;
            Canvas.SetZIndex(poly, 3);
            CanvasMap.Children.Add(poly);

            var txt = new TextBlock
            {
                Text = cell.Identifiant.ToString(),
                Foreground = Brushes.White,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                IsHitTestVisible = false,
            };
            Canvas.SetLeft(txt, cx - 8);
            Canvas.SetTop(txt, cy + _hauteurCellule - 7);
            Canvas.SetZIndex(txt, 5);
            CanvasMap.Children.Add(txt);
        }
    }

    private void DessinerCellulesPlacement(Carte carte)
    {
        var combat = _contexte?.EtatJeu.Combat;
        if (combat == null) return;
        if (combat.PositionsEquipe1.Count == 0 && combat.PositionsEquipe2.Count == 0) return;

        foreach (var id in combat.PositionsEquipe1)
        {
            var cell = carte.Obtenir(id);
            if (cell == null) continue;
            DessinerOverlayCellule(cell, Color.FromArgb(210, 32, 82, 255), Color.FromRgb(15, 48, 190));
        }

        foreach (var id in combat.PositionsEquipe2)
        {
            var cell = carte.Obtenir(id);
            if (cell == null) continue;
            DessinerOverlayCellule(cell, Color.FromArgb(210, 255, 38, 38), Color.FromRgb(176, 20, 20));
        }
    }

    private void DessinerOverlayCellule(Cellule cell, Color fill, Color stroke)
    {
        var poly = CreerPolygoneCellule(cell, new SolidColorBrush(fill), 1.5);
        poly.Stroke = new SolidColorBrush(stroke);
        poly.IsHitTestVisible = false;
        Canvas.SetZIndex(poly, 6);
        CanvasMap.Children.Add(poly);
    }

    private Polygon CreerPolygoneCellule(Cellule cell, Brush fill, double strokeThickness)
    {
        var (cx, cy) = ProjeterIso(cell.X, cell.Y);
        return new Polygon
        {
            Points = new PointCollection
            {
                new Point(cx, cy),
                new Point(cx + _largeurCellule, cy + _hauteurCellule),
                new Point(cx, cy + _hauteurCellule * 2),
                new Point(cx - _largeurCellule, cy + _hauteurCellule),
            },
            Fill = fill,
            Stroke = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            StrokeThickness = strokeThickness,
            Tag = cell,
            Cursor = Cursors.Hand,
        };
    }

    private void DessinerEntites(Carte carte)
    {
        foreach (var ent in System.Linq.Enumerable.ToList(carte.Entites.Values))
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
                Width = 18,
                Height = 18,
                Fill = new SolidColorBrush(couleur),
                Stroke = Brushes.White,
                StrokeThickness = 1,
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

        // Highlight la cellule courante (losange vert clair sous le joueur).
        if (_cellulesPolygons.TryGetValue(cell.Identifiant, out var poly))
        {
            poly.Fill = new SolidColorBrush(Color.FromRgb(0x65, 0xC5, 0x6F));
            poly.Stroke = new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71));
            poly.StrokeThickness = 2;
            Canvas.SetZIndex(poly, 4);
        }

        // Marker bleu : ellipse + halo qui pulse pour la visibilité.
        var halo = new Ellipse
        {
            Width = 36, Height = 36,
            Stroke = new SolidColorBrush(Color.FromArgb(180, 33, 150, 243)),
            StrokeThickness = 2,
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(halo, cx - 18);
        Canvas.SetTop(halo, cy + _hauteurCellule - 18);
        Canvas.SetZIndex(halo, 19);
        CanvasMap.Children.Add(halo);

        var anim = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 0.2, To = 1.0,
            Duration = TimeSpan.FromSeconds(1),
            AutoReverse = true,
            RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
        };
        halo.BeginAnimation(UIElement.OpacityProperty, anim);

        var marker = new Ellipse
        {
            Width = 22, Height = 22,
            Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
            Stroke = Brushes.White,
            StrokeThickness = 2,
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(marker, cx - 11);
        Canvas.SetTop(marker, cy + _hauteurCellule - 11);
        Canvas.SetZIndex(marker, 20);
        CanvasMap.Children.Add(marker);

        // Pas de scroll auto-centre car CanvasMap n'est pas dans un ScrollViewer (zoom only).
        // L'utilisateur peut zoomer/dézoomer pour voir tout.
        _ = _centrageNecessaire;
    }

    private void MettreAJourListeEntites(Carte carte)
    {
        EntitesAffichees.Clear();
        var nbJoueurs = 0;
        var nbMonstres = 0;
        var nbPnjs = 0;

        foreach (var ent in System.Linq.Enumerable.ToList(carte.Entites.Values))
        {
            var typeNom = ent.GetType().Name.Replace("Entite", "");
            var info = ent is EntiteMonstre m ? $" Lv{m.NiveauGroupe} (id:{m.IdGabarit})" : "";
            EntitesAffichees.Add($"{typeNom} #{ent.Identifiant} cell {ent.CellulePosition}{info} - {ent.Nom}");
            if (ent is EntiteJoueur) nbJoueurs++;
            else if (ent is EntiteMonstre) nbMonstres++;
            else if (ent is EntitePNJ) nbPnjs++;
        }

        if (EntitesAffichees.Count == 0)
        {
            EntitesAffichees.Add("Aucune entite detectee sur cette map");
        }
        else
        {
            EntitesAffichees.Insert(0, $"{nbJoueurs} joueur(s) | {nbMonstres} groupe(s) mob | {nbPnjs} PNJ");
        }
    }

    private (double, double) ProjeterIso(int x, int y)
        => (_decalageX + (x - y) * _largeurCellule, _decalageY + (x + y) * _hauteurCellule);

    private Brush CouleurCellule(Cellule cell)
    {
        if (_celluleSelectionnee == cell.Identifiant) return new SolidColorBrush(Color.FromRgb(0x3A, 0x42, 0x55));
        if (cell.Type == TypesCellule.Transition) return new SolidColorBrush(Color.FromRgb(255, 152, 0));
        if (cell.EstInteractif) return new SolidColorBrush(Color.FromRgb(118, 94, 58));
        if (cell.Type == TypesCellule.Obstacle) return new SolidColorBrush(Color.FromRgb(38, 43, 51));
        if (cell.Type == TypesCellule.LignDeVueSeule) return new SolidColorBrush(Color.FromRgb(72, 82, 96));

        var relief = Math.Clamp((int)cell.LayerNiveau, 0, 12) * 7;
        var pente = Math.Clamp((int)cell.LayerSlope, 0, 8) * 4;
        var baseGris = (byte)Math.Clamp(96 + relief - pente, 64, 174);
        return new SolidColorBrush(Color.FromRgb(baseGris, (byte)Math.Clamp(baseGris + 4, 0, 190), (byte)Math.Clamp(baseGris + 14, 0, 205)));
    }

    private void Poly_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is not Polygon poly || poly.Tag is not Cellule cell) return;
        _celluleHover = cell;
        poly.Stroke = Brushes.Orange;
        poly.StrokeThickness = 2;
        TxtTooltip.Text = $"Cell #{cell.Identifiant}  ({cell.X}, {cell.Y})\nType : {cell.Type}\nNiveau : {cell.LayerNiveau}  Slope : {cell.LayerSlope}";
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
        _celluleSelectionnee = cible;
        TxtCellId.Text = $"Cellule : {cible}";
        TxtCoords.Text = $"(x, y) : ({_celluleHover.X}, {_celluleHover.Y})";
        if (ChkDeplacerAuClic?.IsChecked != true)
        {
            Rafraichir();
            return;
        }

        // Découverte (logs Abrak + bot réf. dyshay/SynFus) : les paquets de
        // CONTRÔLE (GR1/GT) sont en CLAIR et injectables. On teste donc
        // l'injection du déplacement map au format EXACT SynFus :
        // "GA001" + chemin (dir+hash A*), via le canal cleartext humanisé —
        // PAS de pixel. Si Abrak l'accepte → auto-déplacement/engage complet.
        try
        {
            bool ok = await _contexte.Api.SeDeplacerVersCelluleAsync(cible);
            if (!ok)
                BotDofus.Utilitaires.Journaux.Journaliseur.Avertir($"[MOVE] Pas de chemin vers {cible} (ou position perso inconnue).");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Déplacement échoué : {ex.Message}", "Erreur",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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

        MenuContextuelContenu.Children.Add(new TextBlock
        {
            Text = $"Cellule {cell.Identifiant} - {(cell.EstMarchable ? "marchable" : cell.Type.ToString().ToLower())}",
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 4)
        });
        MenuContextuelContenu.Children.Add(new TextBlock
        {
            Text = $"Coords ({cell.X}, {cell.Y}) | niveau {cell.LayerNiveau}",
            Foreground = Brushes.LightGray,
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 8)
        });

        AjouterBoutonMenu($"Copier cellId ({cell.Identifiant})", () => Clipboard.SetText(cell.Identifiant.ToString()));

        var infoMap = _contexte?.EtatJeu.CarteCourante != null
            ? BaseDonnees.Instance.Map(_contexte.EtatJeu.CarteCourante.Identifiant)
            : null;
        if (infoMap != null)
        {
            AjouterBoutonMenu($"Travel vers [{infoMap.X},{infoMap.Y}]", async () => await EnvoyerTravelAsync(infoMap.X, infoMap.Y));
            AjouterBoutonMenu($"Copier .travel {infoMap.X},{infoMap.Y}", () => Clipboard.SetText($".travel {infoMap.X},{infoMap.Y}"));
        }

        if (_contexte != null)
        {
            var libelle = cell.Type == TypesCellule.Transition
                ? $"Aller (transition) cellule {cell.Identifiant}"
                : $"Aller cellule {cell.Identifiant}";
            AjouterBoutonMenu(libelle, async () =>
                await _contexte.Api.SeDeplacerVersCelluleAsync(cell.Identifiant));
        }

        AjouterBoutonMenu("Fermer", () => { });
        MenuContextuel.Margin = new Thickness(pos.X, pos.Y, 0, 0);
        MenuContextuel.Visibility = Visibility.Visible;
    }

    private void AfficherMenuContextuel(Entite ent, Point pos)
    {
        MenuContextuelContenu.Children.Clear();

        var titreTexte = ent is EntiteMonstre mob
            ? $"Groupe - {mob.TailleGroupe} mob(s) Lv{mob.NiveauGroupe}"
            : $"{ent.GetType().Name.Replace("Entite", "")} #{ent.Identifiant}";

        MenuContextuelContenu.Children.Add(new TextBlock
        {
            Text = titreTexte,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 4)
        });
        MenuContextuelContenu.Children.Add(new TextBlock
        {
            Text = $"Cellule : {ent.CellulePosition}",
            Foreground = Brushes.LightGray,
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 8)
        });

        if (ent is EntiteMonstre mobs && mobs.IdGabarit > 0)
        {
            var info = BaseDonnees.Instance.Monstre(mobs.IdGabarit);
            MenuContextuelContenu.Children.Add(new TextBlock
            {
                Text = info != null ? $"{info.Nom} Lv{mobs.NiveauGroupe} (id:{mobs.IdGabarit})" : $"Mob id:{mobs.IdGabarit}",
                Foreground = Brushes.OrangeRed,
                FontSize = 11,
                Margin = new Thickness(8, 2, 0, 2)
            });
        }

        if (ent is EntitePNJ)
        {
            AjouterBoutonMenu($"Ouvrir dialogue DB{ent.Identifiant}", async () =>
            {
                if (_contexte != null) await _contexte.Api.EnvoyerPaquetBrutAsync($"DB{ent.Identifiant}");
            });
        }

        AjouterBoutonMenu("Copier pour script", () =>
        {
            Clipboard.SetText($"-- Lua : {ent.GetType().Name} cell={ent.CellulePosition} id={ent.Identifiant}\nbot.deplacer({ent.CellulePosition})\n");
        });
        AjouterBoutonMenu("Fermer", () => { });

        MenuContextuel.Margin = new Thickness(pos.X, pos.Y, 0, 0);
        MenuContextuel.Visibility = Visibility.Visible;
    }

    private void AjouterBoutonMenu(string texte, Action action)
    {
        var btn = new Button { Content = texte, Margin = new Thickness(0, 4, 0, 0) };
        btn.Click += (_, __) =>
        {
            try { action(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Action", MessageBoxButton.OK, MessageBoxImage.Warning); }
            MenuContextuel.Visibility = Visibility.Collapsed;
        };
        MenuContextuelContenu.Children.Add(btn);
    }

    private void AjouterBoutonMenu(string texte, Func<Task> action)
    {
        var btn = new Button { Content = texte, Margin = new Thickness(0, 4, 0, 0) };
        btn.Click += async (_, __) =>
        {
            try { await action(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Action", MessageBoxButton.OK, MessageBoxImage.Warning); }
            MenuContextuel.Visibility = Visibility.Collapsed;
        };
        MenuContextuelContenu.Children.Add(btn);
    }

    private void CanvasMap_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
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
        if (_contexte?.EtatJeu.CarteCourante is Carte carte)
        {
            RecalculerCadreCarte(carte);
            _centrageNecessaire = true;
        }
        ZoomTransform.ScaleX = 1;
        ZoomTransform.ScaleY = 1;
        Rafraichir();
    }

    private void ChkAfficherIds_Changed(object sender, RoutedEventArgs e) => Rafraichir();

    private async void BtnTravelCoords_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtTravelX.Text, out var x) || !int.TryParse(TxtTravelY.Text, out var y))
        {
            MessageBox.Show("Coordonnees invalides. Exemple : -5,-24", "Travel", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await EnvoyerTravelAsync(x, y);
    }

    private async Task EnvoyerTravelAsync(int x, int y)
    {
        if (_contexte == null)
        {
            MessageBox.Show("Aucun compte selectionne.", "Travel", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await _contexte.Api.EnvoyerTravelAsync(x, y, CancellationToken.None);
        TxtPositionFooter.Text = $"Commande envoyee : .travel {x},{y}";
    }
}
