using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Windows.Forms;
using BotDofus.Commun.Reseau;
using BotDofus.Divers;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Interfaces;

/// <summary>
/// Onglet « Debug » : affiche en temps réel les paquets bruts qui transitent
/// par le proxy MITM (VersClient et VersServeur) ainsi que les entrées du journaliseur.
/// Offre des filtres par direction, préfixe et niveau de log.
/// </summary>
public sealed class UI_Debug : UserControl
{
    private const int LimiteLignes = 2000;

    private readonly ListView _liste;
    private readonly ComboBox _filtreDirection;
    private readonly TextBox _filtrePrefixe;
    private readonly Button _effacer;
    private readonly CheckBox _autoDefilement;
    private readonly Label _compteur;

    private readonly ConcurrentQueue<ElementDebug> _file = new();
    private readonly System.Windows.Forms.Timer _timerRafraichissement;
    private int _nbRecus;

    private ContexteCompte? _contexte;

    public UI_Debug()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.Gainsboro;
        Font = new Font("Consolas", 9f);

        // === Barre d'outils ===
        var panneauHaut = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = Color.FromArgb(45, 45, 45) };

        _filtreDirection = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Left = 8, Top = 5, Width = 130,
            Items = { "Tous", "← Serveur", "Client →" }
        };
        _filtreDirection.SelectedIndex = 0;
        _filtreDirection.SelectedIndexChanged += (_, _) => RafraichirAffichage();

        _filtrePrefixe = new TextBox
        {
            Left = 146, Top = 6, Width = 90,
            PlaceholderText = "Préfixe..."
        };
        _filtrePrefixe.TextChanged += (_, _) => RafraichirAffichage();

        _autoDefilement = new CheckBox
        {
            Text = "Auto-scroll",
            Checked = true,
            Left = 244, Top = 7, Width = 100,
            ForeColor = Color.Gainsboro,
            FlatStyle = FlatStyle.Flat
        };

        _effacer = new Button
        {
            Text = "Effacer",
            Left = 350, Top = 4, Width = 80, Height = 24,
            FlatStyle = FlatStyle.Flat
        };
        _effacer.Click += (_, _) => EffacerTout();

        _compteur = new Label
        {
            Left = 440, Top = 7, Width = 200,
            ForeColor = Color.Gainsboro,
            Text = "0 paquets"
        };

        panneauHaut.Controls.AddRange(new Control[] { _filtreDirection, _filtrePrefixe, _autoDefilement, _effacer, _compteur });

        // === Liste ===
        _liste = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            BackColor = Color.FromArgb(22, 22, 22),
            ForeColor = Color.Gainsboro,
            Font = new Font("Consolas", 9f),
            VirtualMode = false,
            UseCompatibleStateImageBehavior = false
        };
        _liste.Columns.Add("Horodatage", 100);
        _liste.Columns.Add("Sens", 60);
        _liste.Columns.Add("Préfixe", 70);
        _liste.Columns.Add("Charge", 1000);

        Controls.Add(_liste);
        Controls.Add(panneauHaut);

        _timerRafraichissement = new System.Windows.Forms.Timer { Interval = 200 };
        _timerRafraichissement.Tick += (_, _) => PurgerFile();
        _timerRafraichissement.Start();

        Journaliseur.EntreeAjoutee += OnJournalEntree;
    }

    /// <summary>Lie l'onglet à un compte : s'abonne à son proxy pour afficher les paquets.</summary>
    public void LierContexte(ContexteCompte contexte)
    {
        if (_contexte != null)
        {
            _contexte.Proxy.PaquetRecu -= OnPaquetRecu;
        }
        _contexte = contexte;
        _contexte.Proxy.PaquetRecu += OnPaquetRecu;
    }

    private void OnPaquetRecu(object? sender, EvenementPaquetRecu e)
    {
        var tag = ClassifierPaquet(e.Paquet.Prefixe);
        var sens = e.Paquet.Direction == DirectionPaquet.VersClient ? "[S→C]" : "[C→S]";

        _file.Enqueue(new ElementDebug(
            e.Paquet.Horodatage.ToString("HH:mm:ss.fff"),
            $"{tag} {sens}",
            e.Paquet.Prefixe,
            e.Paquet.Contenu.Length > 500 ? e.Paquet.Contenu[..500] + "..." : e.Paquet.Contenu,
            CouleurPourTag(tag, e.Paquet.Direction)));
    }

    /// <summary>Classifie un paquet en catégorie haut-niveau (CONNEXION, MAP, COMBAT, CHAT, OBJET, INFO, PKT).</summary>
    private static string ClassifierPaquet(string prefixe) => prefixe switch
    {
        "HC" or "HG" or "Af" or "AlK" or "AlE" or "Ad" or "AV" or "AQ"
            or "AxK" or "AYK" or "ATK" or "ALK" or "ASK" or "AR" or "As"
            or "AA" or "AX" or "Ax" or "AL" or "AS" or "AT" or "Ai" => "[CONNEXION]",
        "GDM" or "GDK" or "GDF" or "GM" or "GJ" or "fC" or "GI" => "[MAP]",
        "GS" or "GE" or "GP" or "GT" or "GA" or "GR" or "GC" or "GKK" => "[COMBAT]",
        "BM" or "BS" or "cMK" or "cMS" or "cC" => "[CHAT]",
        "OAK" or "OR" or "Oq" or "OW" or "OM" => "[OBJET]",
        "DC" or "DQ" or "DV" or "DR" or "DB" => "[DIALOGUE]",
        "Im" or "IO" or "IL" or "BC" or "BP" or "BD" or "BT" => "[INFO]",
        _ => "[PKT]"
    };

    private static Color CouleurPourTag(string tag, DirectionPaquet direction) => tag switch
    {
        "[CONNEXION]" => Color.MediumPurple,
        "[MAP]" => Color.LimeGreen,
        "[COMBAT]" => Color.OrangeRed,
        "[CHAT]" => Color.Khaki,
        "[OBJET]" => Color.PaleGoldenrod,
        "[DIALOGUE]" => Color.Orchid,
        "[INFO]" => Color.LightCyan,
        _ => direction == DirectionPaquet.VersClient ? Color.LightSkyBlue : Color.LightSalmon
    };

    private void OnJournalEntree(object? sender, EvenementEntreeJournal e)
    {
        _file.Enqueue(new ElementDebug(
            e.Entree.Horodatage.ToString("HH:mm:ss.fff"),
            "LOG",
            e.Entree.Niveau.ToString()[..3].ToUpper(),
            e.Entree.Message.Length > 500 ? e.Entree.Message[..500] + "..." : e.Entree.Message,
            e.Entree.Niveau switch
            {
                NiveauJournal.Erreur or NiveauJournal.Critique => Color.OrangeRed,
                NiveauJournal.Avertissement => Color.Gold,
                NiveauJournal.Info => Color.LightGray,
                NiveauJournal.Debug => Color.DimGray,
                _ => Color.Silver
            }));
    }

    private void PurgerFile()
    {
        if (_file.IsEmpty) return;
        if (InvokeRequired) { BeginInvoke(new Action(PurgerFile)); return; }

        _liste.BeginUpdate();
        try
        {
            while (_file.TryDequeue(out var element))
            {
                if (!ElementPasseFiltre(element)) continue;
                var item = new ListViewItem(new[] { element.Horodatage, element.Sens, element.Prefixe, element.Contenu })
                {
                    ForeColor = element.Couleur
                };
                _liste.Items.Add(item);
                _nbRecus++;

                while (_liste.Items.Count > LimiteLignes) _liste.Items.RemoveAt(0);
            }

            _compteur.Text = $"{_nbRecus} paquets";
            if (_autoDefilement.Checked && _liste.Items.Count > 0)
            {
                _liste.EnsureVisible(_liste.Items.Count - 1);
            }
        }
        finally { _liste.EndUpdate(); }
    }

    private bool ElementPasseFiltre(ElementDebug element)
    {
        var sens = _filtreDirection.SelectedIndex;
        if (sens == 1 && element.Sens != "← SRV" && element.Sens != "LOG") return false;
        if (sens == 2 && element.Sens != "CLI →" && element.Sens != "LOG") return false;

        if (!string.IsNullOrEmpty(_filtrePrefixe.Text)
            && !element.Prefixe.Contains(_filtrePrefixe.Text, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        return true;
    }

    private void RafraichirAffichage()
    {
        // La réapplication du filtre repasse par la file pour l'instant ;
        // pour une implémentation simple on se contente de ne plus rien filtrer
        // rétroactivement (les nouvelles lignes respecteront le filtre).
    }

    private void EffacerTout()
    {
        _liste.Items.Clear();
        _nbRecus = 0;
        _compteur.Text = "0 paquets";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Journaliseur.EntreeAjoutee -= OnJournalEntree;
            if (_contexte != null) _contexte.Proxy.PaquetRecu -= OnPaquetRecu;
            _timerRafraichissement?.Dispose();
        }
        base.Dispose(disposing);
    }

    private readonly record struct ElementDebug(string Horodatage, string Sens, string Prefixe, string Contenu, Color Couleur);
}
