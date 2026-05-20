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
    // Panneau droit catégorisé (style MoonBot).
    public ObservableCollection<string> Monstres { get; } = new();
    public ObservableCollection<string> Pnjs { get; } = new();
    public ObservableCollection<string> Joueurs { get; } = new();
    public ObservableCollection<string> Ressources { get; } = new();
    public ObservableCollection<string> Sorties { get; } = new();

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
    // Auto-ajustement du zoom : tant que true, la carte entière est cadrée
    // pour tenir dans la vue (plus besoin de dézoomer à la main). Repassé à
    // true à chaque changement de carte et au bouton Recentrer ; désactivé
    // dès que l'utilisateur zoome manuellement.
    private bool _autoFit = true;
    private bool _sizeChangedAbonne;

    public VueMapViewer()
    {
        InitializeComponent();
        ListeMonstres.ItemsSource = Monstres;
        ListePnj.ItemsSource = Pnjs;
        ListeJoueurs.ItemsSource = Joueurs;
        ListeRessources.ItemsSource = Ressources;
        ListeSorties.ItemsSource = Sorties;
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

    private int _dialoguePnjId;
    private int _dialogueQuestionId;
    private string _dernierDialogue = "";

    private void OnPaquet(object? sender, EvenementPaquetRecu e)
    {
        var contenu = e.Paquet.Contenu;

        // --- Dialogue PNJ (serveur → client) : DCK / DQ / DV ---
        if (e.Paquet.Direction == BotDofus.Commun.Reseau.DirectionPaquet.VersClient
            && contenu.Length >= 2 && contenu[0] == 'D' && char.IsUpper(contenu[1]))
        {
            var c = contenu;
            Dispatcher.BeginInvoke(new Action(() => TraiterDialogue(c)),
                System.Windows.Threading.DispatcherPriority.Background);
            return;
        }
        // GT* (GTM combattants+cellules, GTS tour, GTF/GTR) : indispensable
        // pour rafraîchir la grille PENDANT le combat — les positions des
        // combattants Abrak arrivent par GTM (préfixe "GT", PAS "GM").
        if (!contenu.StartsWith("GM", StringComparison.Ordinal)
            && !contenu.StartsWith("GA", StringComparison.Ordinal)
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

    /// <summary>
    /// Affiche le dialogue PNJ. Paquets serveur :
    ///  - <c>DCK&lt;npc&gt;,&lt;char&gt;</c> : dialogue ouvert.
    ///  - <c>DQ&lt;qid&gt;[;p1;p2…][|r1;r2…]</c> : message du PNJ (+ réponses).
    ///  - <c>DV</c> : fin du dialogue.
    /// Le texte vient de dialogs_fr.json (champ « q » / « a »).
    /// </summary>
    private void TraiterDialogue(string contenu)
    {
        // Évite de reconstruire les boutons sous le curseur si le serveur
        // renvoie le même paquet (sinon clic réponse perdu).
        if (contenu == _dernierDialogue) return;
        _dernierDialogue = contenu;
        try
        {
            if (contenu.StartsWith("DCK", StringComparison.Ordinal))
            {
                var corps = contenu[3..];
                var npc = corps.Split(',', ';')[0];
                int.TryParse(npc, out _dialoguePnjId);
                var nom = _dialoguePnjId != 0
                    ? BaseDonnees.Instance.Npc(System.Math.Abs(_dialoguePnjId))?.Nom
                    : null;
                TxtDialoguePnj.Text = !string.IsNullOrWhiteSpace(nom)
                    ? $"💬 {nom} (PNJ #{_dialoguePnjId})"
                    : $"💬 PNJ #{_dialoguePnjId}";
                TxtDialogueTexte.Text = "(en attente du PNJ…)";
                DialogueReponses.Children.Clear();
                PanneauDialogue.Visibility = Visibility.Visible;
                return;
            }

            if (contenu.StartsWith("DV", StringComparison.Ordinal))
            {
                PanneauDialogue.Visibility = Visibility.Collapsed;
                DialogueReponses.Children.Clear();
                _dialogueQuestionId = 0;
                _dernierDialogue = "";
                return;
            }

            if (contenu.StartsWith("DQ", StringComparison.Ordinal))
            {
                var corps = contenu[2..];
                // Sépare message (gauche) et réponses (droite) : <q>[;params]|<r1;r2…>
                var parts = corps.Split('|');
                var gauche = parts[0].Split(';');
                int.TryParse(gauche[0], out var qid);
                _dialogueQuestionId = qid;
                var bdd = BaseDonnees.Instance;
                var texte = bdd.DialogueQ(qid) ?? $"(dialogue #{qid})";
                // Substitution best-effort des #N par les paramètres.
                for (int i = 1; i < gauche.Length; i++)
                    texte = texte.Replace($"#{i}", gauche[i]);
                TxtDialogueTexte.Text = texte;

                DialogueReponses.Children.Clear();
                if (parts.Length > 1)
                {
                    foreach (var r in parts[1].Split(';', ',',
                                 StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!int.TryParse(r, out var rid)) continue;
                        var libelle = bdd.DialogueA(rid) ?? $"Réponse #{rid}";
                        var btn = new System.Windows.Controls.Button
                        {
                            Content = libelle,
                            Margin = new Thickness(0, 2, 0, 2),
                            Padding = new Thickness(8, 4, 8, 4),
                            HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left,
                            Tag = rid
                        };
                        btn.Click += async (_, __) =>
                        {
                            if (_contexte == null) return;
                            // Le serveur attend DR<questionId>|<replyId>.
                            _dernierDialogue = "";
                            await _contexte.Api.RepondreDialogueAsync(
                                _dialogueQuestionId, (int)btn.Tag);
                        };
                        DialogueReponses.Children.Add(btn);
                    }
                }

                // Aucune réponse → message TERMINAL : un seul bouton clair
                // « Terminer » qui ferme proprement le dialogue (DV), comme le
                // vrai client (capture : DQ32256 sans réponses → C→S DV).
                if (DialogueReponses.Children.Count == 0)
                {
                    var fin = new System.Windows.Controls.Button
                    {
                        Content = "Terminer ▸",
                        Margin = new Thickness(0, 4, 0, 0),
                        Padding = new Thickness(10, 6, 10, 6),
                        FontWeight = System.Windows.FontWeights.SemiBold,
                        Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x4A, 0x33)),
                        Foreground = new SolidColorBrush(Color.FromRgb(0x8F, 0xE0, 0xA6)),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right
                    };
                    fin.Click += async (_, __) =>
                    {
                        PanneauDialogue.Visibility = Visibility.Collapsed;
                        _dialogueQuestionId = 0;
                        _dernierDialogue = "";
                        if (_contexte != null) await _contexte.Api.QuitterDialogueAsync();
                    };
                    DialogueReponses.Children.Add(fin);
                }
                BotDofus.Utilitaires.Journaux.Journaliseur.Info(
                    $"[DIALOGUE] PNJ #{_dialoguePnjId} q={qid} brut='{contenu}'");
                PanneauDialogue.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            BotDofus.Utilitaires.Journaux.Journaliseur.Avertir(
                $"[DIALOGUE] parse '{contenu}' : {ex.Message}");
        }
    }


    private async void BtnDialogueQuitter_Click(object sender, RoutedEventArgs e)
    {
        PanneauDialogue.Visibility = Visibility.Collapsed;
        if (_contexte != null) await _contexte.Api.QuitterDialogueAsync();
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
            _autoFit = true; // nouvelle carte → on re-cadre tout automatiquement
            _celluleSelectionnee = _contexte.EtatJeu.Personnage.CellulePosition;
            RecalculerCadreCarte(carte);
        }

        var info = BaseDonnees.Instance.Map(carte.Identifiant);
        int nbInteract = carte.Cellules.Count(c => c is { IdInteractif: >= 0 });
        int nbTransit = carte.Cellules.Count(c => c is { Type: TypesCellule.Transition });
        TxtMapId.Text = info != null
            ? $"Carte : {carte.Identifiant} [{info.X},{info.Y}] ({carte.Cellules.Length} c., {nbInteract} interactifs, {nbTransit} transitions)"
            : $"Carte : {carte.Identifiant} ({carte.Cellules.Length} c., {nbInteract} interactifs, {nbTransit} transitions)";
        BotDofus.Utilitaires.Journaux.Journaliseur.Info(
            $"[CARTE] #{carte.Identifiant} : {nbInteract} cellule(s) interactive(s)/récoltable(s), "
            + $"{nbTransit} transition(s) (cases vertes = récolte, oranges = changement map).");
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
        if (ScrollerMap == null) return;

        // Abonnement unique : si la fenêtre est redimensionnée, on re-cadre
        // la carte (reste « propre » sans dézoomer).
        if (!_sizeChangedAbonne)
        {
            _sizeChangedAbonne = true;
            ScrollerMap.SizeChanged += (_, __) =>
            {
                if (_autoFit) AjusterZoomAuto();
            };
        }

        if (!_centrageNecessaire) return;
        _centrageNecessaire = false;

        var cellule = _contexte?.EtatJeu.Personnage.CellulePosition is int pos
            ? carte.Obtenir(pos) : null;
        var (cx, cy) = cellule != null ? ProjeterIso(cellule.X, cellule.Y) : (0d, 0d);

        Dispatcher.BeginInvoke(new Action(() =>
        {
            // 1) Cadre toute la carte dans la vue (auto-fit), puis 2) si la
            // carte déborde encore (zoom plancher atteint), centre sur le perso.
            AjusterZoomAuto();
            if (cellule != null)
            {
                ScrollerMap.ScrollToHorizontalOffset(
                    Math.Max(0, cx * ZoomTransform.ScaleX - ScrollerMap.ViewportWidth / 2));
                ScrollerMap.ScrollToVerticalOffset(
                    Math.Max(0, cy * ZoomTransform.ScaleY - ScrollerMap.ViewportHeight / 2));
            }
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Calcule et applique le zoom qui fait tenir TOUTE la carte dans la zone
    /// visible (≈ 97 % pour une petite marge). N'agit que si <see cref="_autoFit"/>
    /// est actif (désactivé dès que l'utilisateur zoome manuellement).
    /// </summary>
    private void AjusterZoomAuto()
    {
        if (!_autoFit || ScrollerMap == null) return;
        double dispoW = ScrollerMap.ViewportWidth > 10
            ? ScrollerMap.ViewportWidth : ScrollerMap.ActualWidth;
        double dispoH = ScrollerMap.ViewportHeight > 10
            ? ScrollerMap.ViewportHeight : ScrollerMap.ActualHeight;
        if (dispoW < 10 || dispoH < 10
            || CanvasMap.Width < 1 || CanvasMap.Height < 1) return;

        double scale = Math.Min(dispoW / CanvasMap.Width, dispoH / CanvasMap.Height) * 0.97;
        scale = Math.Clamp(scale, 0.15, 1.0);
        ZoomTransform.ScaleX = scale;
        ZoomTransform.ScaleY = scale;
    }

    private void DessinerGrille(Carte carte)
    {
        bool afficherIds = ChkAfficherIds.IsChecked == true;
        foreach (var cell in carte.Cellules)
        {
            if (cell == null) continue;
            var (cx, cy) = ProjeterIso(cell.X, cell.Y);

            var couleur = CouleurCellule(cell);

            // VOLUME 3D : le GRIS (obstacle/non-marchable) est un PLATEAU dont
            // le DESSUS est remonté à l'écran (élévation) → il surplombe le sol
            // blanc qui, lui, reste au niveau bas. La falaise (faces latérales)
            // descend du dessus remonté jusqu'au niveau du sol → vraie marche.
            // Une TRANSITION (sortie/soleil) n'est PAS une falaise : elle
            // reste à PLAT au niveau du sol, sinon le marqueur orange (dessiné
            // à plat) est décalé par rapport au bloc surélevé.
            bool blocCell = cell.Type != TypesCellule.Transition
                            && (!cell.EstMarchable
                                || cell.Type == TypesCellule.Obstacle
                                || cell.Type == TypesCellule.LignDeVueSeule);
            double elev = blocCell ? 20 : 0;  // hauteur du plateau gris
            const double epais = 4;           // fine épaisseur commune (dalle)
            // Profondeur iso (peintre) : plus (x+y) est grand, plus la case
            // est « devant ». On l'utilise pour empiler proprement :
            //   sol < murs (falaises) < dessus gris  — chaque couche triée
            // par profondeur. Sans ça, TOUS les murs (z=1) passaient sous
            // TOUS les sols (z=2) → on voyait à travers la falaise de droite.
            int prof = cell.X + cell.Y;

            if (couleur is SolidColorBrush sc)
            {
                // Sommets bas du losange, REMONTÉS de `elev` (dessus du plateau).
                // On élargit LÉGÈREMENT (0.6px) et on descend un peu plus bas
                // que nécessaire : la falaise déborde sur la case d'en dessous
                // → plus de fin liseré blanc entre le bloc et le sol.
                const double deb = 0.6;                 // débord latéral
                double bas = elev + epais + 2.0;        // débord vers le bas
                var pG = new Point(cx - _largeurCellule - deb, cy + _hauteurCellule - elev);
                var pB = new Point(cx, cy + _hauteurCellule * 2 - elev + deb);
                var pD = new Point(cx + _largeurCellule + deb, cy + _hauteurCellule - elev);

                var brushG = new SolidColorBrush(AssombrirCouleur(sc.Color, 0.78));
                var brushD = new SolidColorBrush(AssombrirCouleur(sc.Color, 0.62));
                var faceG = new Polygon
                {
                    Points = new PointCollection
                    {
                        pG, pB,
                        new Point(pB.X, pB.Y + bas),
                        new Point(pG.X, pG.Y + bas),
                    },
                    Fill = brushG,
                    // stroke = même couleur → ferme les hairlines d'anti-crénelage
                    Stroke = brushG, StrokeThickness = 0.9,
                    IsHitTestVisible = false,
                };
                var faceD = new Polygon
                {
                    Points = new PointCollection
                    {
                        pB, pD,
                        new Point(pD.X, pD.Y + bas),
                        new Point(pB.X, pB.Y + bas),
                    },
                    Fill = brushD,
                    Stroke = brushD, StrokeThickness = 0.9,
                    IsHitTestVisible = false,
                };
                System.Windows.Media.RenderOptions.SetEdgeMode(
                    faceG, System.Windows.Media.EdgeMode.Aliased);
                System.Windows.Media.RenderOptions.SetEdgeMode(
                    faceD, System.Windows.Media.EdgeMode.Aliased);
                // Murs AU-DESSUS de tous les sols (offset +100 > maxProf) →
                // la falaise couvre le sol qui est devant elle (plus de
                // « transparence » à droite), triée par profondeur entre murs.
                Canvas.SetZIndex(faceG, prof + 100);
                Canvas.SetZIndex(faceD, prof + 100);
                CanvasMap.Children.Add(faceG);
                CanvasMap.Children.Add(faceD);
            }

            var poly = CreerPolygoneCellule(cell, couleur, 0.8);
            // Le DESSUS du gris est remonté de `elev` → il passe au-dessus du
            // sol blanc (resté en bas) = effet de plateau / marche 3D.
            if (elev > 0)
                poly.RenderTransform = new System.Windows.Media.TranslateTransform(0, -elev);
            bool sol = cell.EstMarchable && cell.IdInteractif < 0
                       && cell.Type != TypesCellule.Transition;
            // BORDURE sur TOUTES les cases grises (obstacle/non-marchable),
            // même au milieu de la carte : chaque bloc gris est délimité par
            // un liseré plus sombre → la masse grise se lit en blocs surélevés
            // distincts (pas un aplat). Le sol garde sa fine grille claire.
            bool gris = cell.Type != TypesCellule.Transition
                        && (!cell.EstMarchable
                            || cell.Type == TypesCellule.Obstacle
                            || cell.Type == TypesCellule.LignDeVueSeule);
            // Dessus gris AU-DESSUS des murs (offset +200) → la surface du
            // plateau est nette ; trié par profondeur pour qu'un bloc gris
            // devant masque la falaise d'un bloc gris derrière. Le sol/les
            // cases colorées restent en bas (z = prof) sous les falaises.
            // Transition : léger boost (+60) → ressort nettement au-dessus
            // de la grille du sol, mais reste sous les murs (prof+100) et
            // l'UI (300+). Gris : +200 (au-dessus des murs). Sol : prof.
            Canvas.SetZIndex(poly,
                cell.Type == TypesCellule.Transition ? prof + 60
                : gris ? prof + 200 : prof);
            if (gris && couleur is SolidColorBrush gc)
            {
                poly.Stroke = new SolidColorBrush(AssombrirCouleur(gc.Color, 0.78));
                poly.StrokeThickness = 0.5;
            }
            else if (!sol)
            {
                poly.Stroke = couleur;
            }
            else
            {
                // SOL blanc : grille très discrète. Avant elle gardait le
                // stroke par défaut (#BCC0C7, 0.8) → trop contrasté, et
                // l'anti-crénelage doublait la ligne entre 2 cases voisines
                // (démarcations « grosses »). Liseré quasi-blanc + très fin
                // + Aliased (pas de doublon flou) = grille propre façon SynFus.
                poly.Stroke = new SolidColorBrush(Color.FromRgb(0xF1, 0xF3, 0xF5));
                poly.StrokeThickness = 0.15;
                System.Windows.Media.RenderOptions.SetEdgeMode(
                    poly, System.Windows.Media.EdgeMode.Aliased);
            }
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
                Foreground = new SolidColorBrush(Color.FromRgb(0x6A, 0x6E, 0x76)),
                FontSize = 8,
                IsHitTestVisible = false,
            };
            Canvas.SetLeft(txt, cx - 8);
            Canvas.SetTop(txt, cy + _hauteurCellule - 6 - elev); // suit le plateau
            Canvas.SetZIndex(txt, 320);
            CanvasMap.Children.Add(txt);
        }
    }

    private void DessinerTransitions(Carte carte)
    {
        foreach (var cell in carte.Cellules)
        {
            if (cell?.Type != TypesCellule.Transition) continue;
            var (cx, cy) = ProjeterIso(cell.X, cell.Y);

            // PAS de 2e polygone : la grille (DessinerGrille) dessine DÉJÀ la
            // case de transition en orange, à PLEINE TAILLE et donc PARFAITEMENT
            // alignée sur la cellule (même polygone que toutes les cases). Un
            // 2e losange réduit par-dessus créait le halo/décalage. Ici on
            // n'ajoute QUE le numéro de la cellule.
            var txt = new TextBlock
            {
                Text = cell.Identifiant.ToString(),
                Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x59, 0x60)),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                IsHitTestVisible = false,
            };
            Canvas.SetLeft(txt, cx - 8);
            Canvas.SetTop(txt, cy + _hauteurCellule - 7);
            Canvas.SetZIndex(txt, 320);
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
        Canvas.SetZIndex(poly, 360);
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
            Stroke = new SolidColorBrush(Color.FromRgb(0xBC, 0xC0, 0xC7)),
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
                EntiteMonstre => Color.FromRgb(0x9E, 0x2B, 0x2B),  // rouge sombre SynFus
                EntitePNJ => Color.FromRgb(0x3F, 0xA9, 0xC9),
                EntiteJoueur => Color.FromRgb(0x3C, 0xB0, 0x55),
                _ => Color.FromRgb(0x8A, 0x8A, 0x8A)
            };

            var ellipse = new Ellipse
            {
                Width = 16,
                Height = 16,
                Fill = new SolidColorBrush(couleur),
                Stroke = Brushes.White,
                StrokeThickness = 1.5,
                Tag = ent,
                Cursor = Cursors.Hand,
            };
            ellipse.MouseRightButtonDown += Entity_MouseRightButtonDown;
            Canvas.SetLeft(ellipse, cx - 8);
            Canvas.SetTop(ellipse, cy + _hauteurCellule - 8);
            Canvas.SetZIndex(ellipse, 340);
            CanvasMap.Children.Add(ellipse);
        }
    }

    private void DessinerJoueur()
    {
        if (_contexte?.EtatJeu.Personnage.CellulePosition is not int posId) return;
        var cell = _contexte.EtatJeu.CarteCourante?.Obtenir(posId);
        if (cell == null) return;
        var (cx, cy) = ProjeterIso(cell.X, cell.Y);

        // Léger surlignage bleu pâle de la case courante (discret, propre).
        if (_cellulesPolygons.TryGetValue(cell.Identifiant, out var poly))
        {
            poly.Fill = new SolidColorBrush(Color.FromRgb(0xBB, 0xD3, 0xFF));
            poly.Stroke = new SolidColorBrush(Color.FromRgb(0x2D, 0x6C, 0xDF));
            poly.StrokeThickness = 1.4;
            Canvas.SetZIndex(poly, 350);
        }

        // Pion joueur : disque bleu net (style SynFus), sans halo pulsant.
        var marker = new Ellipse
        {
            Width = 18, Height = 18,
            Fill = new SolidColorBrush(Color.FromRgb(0x2D, 0x6C, 0xDF)),
            Stroke = Brushes.White,
            StrokeThickness = 2,
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(marker, cx - 9);
        Canvas.SetTop(marker, cy + _hauteurCellule - 9);
        Canvas.SetZIndex(marker, 400);
        CanvasMap.Children.Add(marker);

        // Pas de scroll auto-centre car CanvasMap n'est pas dans un ScrollViewer (zoom only).
        // L'utilisateur peut zoomer/dézoomer pour voir tout.
        _ = _centrageNecessaire;
    }

    private void MettreAJourListeEntites(Carte carte)
    {
        Monstres.Clear();
        Pnjs.Clear();
        Joueurs.Clear();
        Ressources.Clear();
        Sorties.Clear();

        foreach (var ent in System.Linq.Enumerable.ToList(carte.Entites.Values))
        {
            switch (ent)
            {
                case EntiteMonstre m:
                    Monstres.Add($"{(string.IsNullOrWhiteSpace(m.Nom) ? "Groupe" : m.Nom)} "
                        + $"Lv.{m.NiveauGroupe} [{m.CellulePosition}]");
                    break;
                case EntitePNJ p:
                    Pnjs.Add($"{(string.IsNullOrWhiteSpace(p.Nom) ? "PNJ" : p.Nom)} "
                        + $"#{p.Identifiant} cell:{p.CellulePosition} gfx:{p.IdGabarit}");
                    break;
                case EntiteJoueur j:
                    Joueurs.Add($"{j.Nom} [{j.CellulePosition}]"
                        + (j.Niveau > 0 ? $" niv.{j.Niveau}" : ""));
                    break;
            }
        }

        // Ressources / interactifs + sorties (transitions) depuis les cellules.
        foreach (var cell in carte.Cellules)
        {
            if (cell == null) continue;

            if (cell.Type == TypesCellule.Transition)
            {
                Sorties.Add($"Sortie cell:{cell.Identifiant} ({cell.X},{cell.Y})");
                continue;
            }

            bool aObjet = cell.IdInteractif >= 0 || cell.LayerObjet2 > 0;
            if (!aObjet) continue;

            int gfx = cell.LayerObjet2 > 0 ? cell.LayerObjet2 : cell.LayerObjet1;
            string nom = cell.IdInteractif >= 0
                ? (BotDofus.Divers.Donnees.BaseDonnees.Instance
                       .Interactif(cell.IdInteractif)?.Nom ?? "Interactif")
                : "Ressource";
            Ressources.Add($"{nom} cell:{cell.Identifiant} gfx:{gfx}");
        }

        if (TxtNbMonstres != null) TxtNbMonstres.Text = Monstres.Count.ToString();
        if (TxtNbPnj != null) TxtNbPnj.Text = Pnjs.Count.ToString();
        if (TxtNbJoueurs != null) TxtNbJoueurs.Text = Joueurs.Count.ToString();
        if (TxtNbRessources != null) TxtNbRessources.Text = Ressources.Count.ToString();
        if (TxtNbSorties != null) TxtNbSorties.Text = Sorties.Count.ToString();

        if (Monstres.Count == 0) Monstres.Add("Aucun");
        if (Pnjs.Count == 0) Pnjs.Add("Aucun");
        if (Joueurs.Count == 0) Joueurs.Add("Aucun");
        if (Ressources.Count == 0) Ressources.Add("Aucune");
        if (Sorties.Count == 0) Sorties.Add("Aucune");
    }

    private (double, double) ProjeterIso(int x, int y)
        => (_decalageX + (x - y) * _largeurCellule, _decalageY + (x + y) * _hauteurCellule);

    private static Color AssombrirCouleur(Color c, double f)
        => Color.FromRgb((byte)(c.R * f), (byte)(c.G * f), (byte)(c.B * f));

    private Brush CouleurCellule(Cellule cell)
    {
        // Palette claire façon SynFus : cases blanches, blocs gris foncés.
        if (_celluleSelectionnee == cell.Identifiant) return new SolidColorBrush(Color.FromRgb(0xC8, 0xDA, 0xFF));
        if (cell.Type == TypesCellule.Transition) return new SolidColorBrush(Color.FromRgb(0xF2, 0xA3, 0x3C));
        // ZAAP / ZAAPI : couleur dédiée (prête ; s'affiche dès que le décodeur
        // type la case en Zaap — ce qui nécessite de capturer ton interaction
        // avec le zaap, cf. Paquet B).
        if (cell.Type == TypesCellule.Zaap) return new SolidColorBrush(Color.FromRgb(0x29, 0xB6, 0xF6));
        if (cell.Type == TypesCellule.Zaapi) return new SolidColorBrush(Color.FromRgb(0x7E, 0x57, 0xC2));
        // Élément interactif / récoltable :
        //  - épuisé (GDF) → vert très terne
        //  - skill connu de ce perso → vert vif (récoltable)
        //  - skill appris en BDD mais PAS dans tes métiers → orange/brun
        //    (« tu n'as pas le métier/niveau pour celle-ci »)
        //  - skill encore inconnu (jamais récolté) → vert moyen
        if (cell.IdInteractif >= 0)
        {
            if (!cell.RessourceDisponible)
                return new SolidColorBrush(Color.FromRgb(0xCF, 0xE3, 0xD4)); // épuisée (pâle)

            var ioMap = BaseDonnees.Instance.Interactif(cell.IdInteractif);
            var skillsPerso = _contexte?.EtatJeu.Personnage.SkillsConnus;
            if (ioMap is { IdSkill: > 0 } && skillsPerso is { Count: > 0 })
            {
                return new SolidColorBrush(skillsPerso.Contains(ioMap.IdSkill)
                    ? Color.FromRgb(0x5B, 0xCB, 0x7A)   // récoltable par toi
                    : Color.FromRgb(0xE0, 0xB2, 0x4C)); // métier/niveau insuffisant
            }
            return new SolidColorBrush(Color.FromRgb(0x5B, 0xCB, 0x7A));
        }
        if (cell.EstInteractif) return new SolidColorBrush(Color.FromRgb(0xD8, 0xC7, 0xA8));
        // Obstacle / hors LoS = blocs gris foncés « surélevés » comme SynFus.
        // Blocs gris foncé NEUTRE comme SynFus (≈ #3D3D3D, pas bleuté).
        if (cell.Type == TypesCellule.Obstacle) return new SolidColorBrush(Color.FromRgb(0x3D, 0x3D, 0x3D));
        if (cell.Type == TypesCellule.LignDeVueSeule) return new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
        if (!cell.EstMarchable) return new SolidColorBrush(Color.FromRgb(0x3D, 0x3D, 0x3D));

        // Marchable : quasi-blanc neutre comme SynFus, très léger relief.
        var relief = Math.Clamp((int)cell.LayerNiveau, 0, 12) * 2;
        var g = (byte)Math.Clamp(242 - relief, 224, 242);
        return new SolidColorBrush(Color.FromRgb(g, g, g));
    }

    private void Poly_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is not Polygon poly || poly.Tag is not Cellule cell) return;
        _celluleHover = cell;
        poly.Stroke = Brushes.Orange;
        poly.StrokeThickness = 2;
        var txt = $"Cell #{cell.Identifiant}  ({cell.X}, {cell.Y})\nType : {cell.Type}\nNiveau : {cell.LayerNiveau}  Slope : {cell.LayerSlope}";
        if (cell.IdInteractif >= 0)
        {
            var io = BaseDonnees.Instance.Interactif(cell.IdInteractif);
            var etat = cell.RessourceDisponible ? "disponible" : "épuisée";
            var skillsPerso = _contexte?.EtatJeu.Personnage.SkillsConnus;
            string metier = "";
            if (io is { IdSkill: > 0 } && skillsPerso is { Count: > 0 })
                metier = skillsPerso.Contains(io.IdSkill)
                    ? "  ✓ récoltable"
                    : "  ✗ métier/niveau insuffisant";
            txt += io != null && !string.IsNullOrEmpty(io.Nom)
                ? $"\n🌿 {io.Nom} (gfx #{cell.IdInteractif}, skill {io.IdSkill}) — {etat}{metier}"
                : $"\n🌿 Interactif gfx #{cell.IdInteractif} — {etat} (récolte-le pour l'identifier)";
        }
        TxtTooltip.Text = txt;
        TooltipBorder.Visibility = Visibility.Visible;
    }

    private void Poly_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Polygon poly)
        {
            poly.StrokeThickness = 0.8;
            // Restaure : grille claire sur le sol, pas de liseré sur les blocs.
            bool sol = poly.Tag is Cellule c && c.EstMarchable
                       && c.IdInteractif < 0 && c.Type != TypesCellule.Transition;
            poly.Stroke = sol
                ? new SolidColorBrush(Color.FromRgb(0xBC, 0xC0, 0xC7))
                : poly.Fill;
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
                ? $"Aller (transition → change map) cellule {cell.Identifiant}"
                : $"Aller cellule {cell.Identifiant}";
            AjouterBoutonMenu(libelle, async () =>
                await _contexte.Api.SeDeplacerVersCelluleAsync(cell.Identifiant));

            // Récolte : cellule avec élément interactif (vert). Le nom/skill
            // viennent de la BDD interactifs auto-apprise (GA500 capturés).
            if (cell.IdInteractif >= 0)
            {
                var ioRec = BaseDonnees.Instance.Interactif(cell.IdInteractif);
                var libelleRec = ioRec != null && !string.IsNullOrEmpty(ioRec.Nom)
                    ? $"{ioRec.Nom} (#{cell.IdInteractif})"
                    : $"objet #{cell.IdInteractif}";
                int skillRec = ioRec != null && ioRec.IdSkill > 0 ? ioRec.IdSkill : 45;
                AjouterBoutonMenu($"🌿 Récolter : {libelleRec}", async () =>
                {
                    if (_contexte == null) return;
                    BotDofus.Utilitaires.Journaux.Journaliseur.Info(
                        $"[UI] Récolter cellule {cell.Identifiant} : {libelleRec} skill {skillRec}.");
                    await _contexte.Api.RecolterAsync(cell.Identifiant, cell.IdInteractif, skillRec);
                });
            }
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
            // Combattre CE groupe : approche (GA001+GKK0) PUIS GA907. Le
            // GA907 seul (de loin) est ignoré par le serveur — il FAUT
            // marcher au contact d'abord (même flux que le farm).
            AjouterBoutonMenu($"⚔ Combattre ce groupe (#{ent.Identifiant} cell {ent.CellulePosition})", async () =>
            {
                if (_contexte == null) return;
                BotDofus.Utilitaires.Journaux.Journaliseur.Info(
                    $"[UI] Combattre groupe #{ent.Identifiant} « {ent.Nom} » cell {ent.CellulePosition} (approche+GA907).");
                await _contexte.Api.EngagerGroupeAsync(ent.CellulePosition, ent.Identifiant);
            });
        }

        if (ent is EntitePNJ)
        {
            AjouterBoutonMenu($"💬 Parler à ce PNJ (#{ent.Identifiant} cell {ent.CellulePosition})", async () =>
            {
                if (_contexte == null) return;
                BotDofus.Utilitaires.Journaux.Journaliseur.Info(
                    $"[UI] Parler PNJ #{ent.Identifiant} « {ent.Nom} » cell {ent.CellulePosition} (approche+DC).");
                await _contexte.Api.ParlerPnjAsync(ent.CellulePosition, ent.Identifiant);
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
        _autoFit = false; // zoom manuel : on respecte le choix de l'utilisateur
        double facteur = e.Delta > 0 ? 1.15 : 0.87;
        ZoomTransform.ScaleX = Math.Clamp(ZoomTransform.ScaleX * facteur, 0.12, 4.0);
        ZoomTransform.ScaleY = ZoomTransform.ScaleX;
    }

    private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
    {
        _autoFit = false;
        ZoomTransform.ScaleX = Math.Clamp(ZoomTransform.ScaleX * 1.2, 0.12, 4.0);
        ZoomTransform.ScaleY = ZoomTransform.ScaleX;
    }

    private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
    {
        _autoFit = false;
        ZoomTransform.ScaleX = Math.Clamp(ZoomTransform.ScaleX / 1.2, 0.12, 4.0);
        ZoomTransform.ScaleY = ZoomTransform.ScaleX;
    }

    private void BtnRecentrer_Click(object sender, RoutedEventArgs e)
    {
        _decalageX = 500;
        _decalageY = 60;
        _autoFit = true; // re-cadre toute la carte automatiquement
        if (_contexte?.EtatJeu.CarteCourante is Carte carte)
        {
            RecalculerCadreCarte(carte);
            _centrageNecessaire = true;
        }
        Rafraichir();
    }

    private void ChkAfficherIds_Changed(object sender, RoutedEventArgs e) => Rafraichir();

    private readonly BotDofus.Divers.Scripts.EnregistreurTrajet _recTrajet = new();
    private FenetreRoadCreator? _roadFenetre;
    private EventHandler<BotDofus.Divers.Cartes.Carte>? _roadCarteHandler;
    private int _roadMapId;
    private string _roadCoords = "?,?";
    private bool _roadLigneDejaValidee;

    private (int id, string coords) MapCourante()
    {
        int id = _contexte?.EtatJeu.Personnage.CarteCourante ?? 0;
        var m = BaseDonnees.Instance.Map(id);
        return (id, m != null ? $"{m.X},{m.Y}" : id.ToString());
    }

    /// <summary>Petite boîte de saisie texte modale (nom de script…).</summary>
    private string? PromptTexte(string titre, string label, string defaut)
    {
        var win = new Window
        {
            Title = titre, Width = 380, SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this), ResizeMode = ResizeMode.NoResize,
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E))
        };
        var sp = new StackPanel { Margin = new Thickness(16) };
        sp.Children.Add(new TextBlock
        {
            Text = label, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 8)
        });
        var tb = new TextBox { Text = defaut };
        tb.SelectAll();
        sp.Children.Add(tb);
        var dp = new DockPanel { Margin = new Thickness(0, 12, 0, 0) };
        var ok = new Button { Content = "OK", Width = 80, IsDefault = true };
        var cancel = new Button { Content = "Annuler", Width = 80, Margin = new Thickness(8, 0, 0, 0), IsCancel = true };
        string? res = null;
        ok.Click += (_, __) => { res = tb.Text; win.DialogResult = true; };
        cancel.Click += (_, __) => win.DialogResult = false;
        var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        btns.Children.Add(ok); btns.Children.Add(cancel);
        DockPanel.SetDock(btns, Dock.Right);
        dp.Children.Add(btns);
        sp.Children.Add(dp);
        win.Content = sp;
        tb.Focus();
        return win.ShowDialog() == true ? res : null;
    }

    /// <summary>
    /// Exécute la ligne validée dans le Road Creator : le bot enchaîne
    /// combat → récolte → PNJ → sortie (cellule ou direction) tout seul.
    /// Ainsi pas besoin de refaire l'action manuellement après l'avoir cochée.
    /// </summary>
    private async System.Threading.Tasks.Task ExecuterLigneRoadCreator(
        BotDofus.Divers.Scripts.EnregistreurTrajet.Ligne l,
        bool deplacementSeul = false)
    {
        if (_contexte == null) return;
        var api = _contexte.Api;
        var ct = System.Threading.CancellationToken.None;
        try
        {
            // deplacementSeul (clic direction pendant l'ENREGISTREMENT) : on
            // change juste de map pour naviguer, SANS auto-récolte/combat
            // (sinon le bot récolte toute la map seul pendant qu'on enregistre).
            if (!deplacementSeul && l.Fight)
            {
                await api.EngagerCombatAsync(ct);
                // attend la fin du combat (max ~3 min)
                for (int i = 0; i < 360
                     && _contexte.EtatJeu.Combat.Etat
                        != BotDofus.Divers.Combats.Enums.EtatCombat.Inactif; i++)
                    await System.Threading.Tasks.Task.Delay(500);
            }
            if (!deplacementSeul && l.Gather) await api.RecolterToutAsync(ct);
            // NB : le PNJ a déjà été parlé EN DIRECT dans la fenêtre Road
            // Creator (réponses enregistrées) → on ne le refait pas ici.
            if (l.Cellule > 0)
                await api.SeDeplacerVersCelluleAsync(l.Cellule, ct);
            else if (!string.IsNullOrEmpty(l.Direction))
            {
                var dir = l.Direction switch
                {
                    "top" => "nord", "bottom" => "sud",
                    "right" => "est", "left" => "ouest", _ => l.Direction
                };
                await api.ChangerMapDirectionAsync(dir, ct);
            }
        }
        catch (Exception ex)
        {
            BotDofus.Utilitaires.Journaux.Journaliseur.Avertir(
                $"[ROADREC] exécution ligne : {ex.Message}");
        }
    }

    private void BtnRecTrajet_Click(object sender, RoutedEventArgs e)
    {
        if (_contexte == null)
        {
            MessageBox.Show("Sélectionne un compte d'abord.", "RoadCreator",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!_recTrajet.EnCours)
        {
            var nom = PromptTexte("Road Creator — nom du script",
                "Nom du trajet (fichier .lua) :", $"trajet_{DateTime.Now:yyyyMMdd_HHmm}");
            if (string.IsNullOrWhiteSpace(nom)) return; // annulé
            _recTrajet.Demarrer(nom);
            BtnRecTrajet.Content = "■ STOP Road Creator";
            BtnRecTrajet.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xC2, 0x7A));

            _roadFenetre = new FenetreRoadCreator { Owner = Window.GetWindow(this) };
            _roadFenetre.Initialiser(_contexte);
            _roadFenetre.Validee += async (_, ligne) =>
            {
                _recTrajet.AjouterLigne(ligne);
                _roadFenetre?.MajCompteur(_recTrajet.NbLignes);
                _roadLigneDejaValidee = true; // évite le double-enregistrement
                // Clic direction/cellule = on change RÉELLEMENT de map pour
                // naviguer pendant l'enregistrement, mais SANS auto-récolte
                // ni combat (deplacementSeul) → le perso ne récolte/combat
                // plus tout seul ; ça reste juste coché et enregistré.
                await ExecuterLigneRoadCreator(ligne, deplacementSeul: true);
            };
            var (id, co) = MapCourante();
            _roadMapId = id;
            _roadCoords = co;
            _roadLigneDejaValidee = false;
            _roadFenetre.Preparer(id, co);

            // À chaque changement de carte : on CAPTURE d'abord (SYNCHRONE, sur
            // le thread réseau) la VRAIE cellule de sortie. CarteChangee est
            // levé AVANT que le GM de la nouvelle carte mette à jour la
            // position → Personnage.CellulePosition contient encore la case
            // exacte où on a quitté la map (sortie d'auberge incluse). On
            // enregistre automatiquement la ligne de la map quittée avec CETTE
            // cellule, sauf si l'utilisateur a déjà cliqué « Valider ».
            _roadCarteHandler = (_, __) =>
            {
                int sortie = _contexte?.EtatJeu.Personnage.CellulePosition ?? 0;
                int mapQuittee = _roadMapId;
                string coordsQuittee = _roadCoords;
                bool dejaValidee = _roadLigneDejaValidee;
                _roadLigneDejaValidee = false;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_roadFenetre == null) return;
                    if (!dejaValidee && mapQuittee != 0 && _recTrajet.EnCours)
                    {
                        var ligne = _roadFenetre.ConstruireLigneAuto(
                            mapQuittee, coordsQuittee, sortie);
                        _recTrajet.AjouterLigne(ligne);
                        _roadFenetre.MajCompteur(_recTrajet.NbLignes);
                    }
                    var (mid, mco) = MapCourante();
                    _roadMapId = mid;
                    _roadCoords = mco;
                    _roadFenetre.Preparer(mid, mco);
                }));
            };
            _contexte.EtatJeu.CarteChangee += _roadCarteHandler;
        }
        else
        {
            if (_roadCarteHandler != null && _contexte != null)
                _contexte.EtatJeu.CarteChangee -= _roadCarteHandler;
            _roadCarteHandler = null;
            _roadFenetre?.FermerVraiment();
            _roadFenetre = null;

            var chemin = _recTrajet.Arreter();
            BtnRecTrajet.Content = "🛣 Road Creator";
            BtnRecTrajet.Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0x50, 0x50));
            MessageBox.Show(
                chemin != null
                    ? $"Trajet enregistré :\n{chemin}\n\nCharge-le dans l'onglet Scripts pour le rejouer."
                    : "Aucun waypoint enregistré.",
                "RoadCreator", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void BtnTravelCoords_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtTravelX.Text, out var x) || !int.TryParse(TxtTravelY.Text, out var y))
        {
            MessageBox.Show("Coordonnees invalides. Exemple : -5,-24", "Travel", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await EnvoyerTravelAsync(x, y);
    }

    private async void BtnMapGauche_Click(object s, RoutedEventArgs e) { if (_contexte != null) await _contexte.Api.ChangerMapDirectionAsync("ouest"); }
    private async void BtnMapDroite_Click(object s, RoutedEventArgs e) { if (_contexte != null) await _contexte.Api.ChangerMapDirectionAsync("est"); }
    private async void BtnMapHaut_Click(object s, RoutedEventArgs e) { if (_contexte != null) await _contexte.Api.ChangerMapDirectionAsync("nord"); }
    private async void BtnMapBas_Click(object s, RoutedEventArgs e) { if (_contexte != null) await _contexte.Api.ChangerMapDirectionAsync("sud"); }

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
