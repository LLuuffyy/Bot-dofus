using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BotDofus.Commun.Messages;
using BotDofus.Commun.Reseau;
using BotDofus.Divers;
using BotDofus.Interfaces;
using BotDofus.Utilitaires.Config;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Formulaires;

public partial class FormulairePrincipal : Form
{
    private readonly Dictionary<string, ContexteCompte> _contextes = new();

    public FormulairePrincipal()
    {
        InitializeComponent();

        menuFichierGestionComptes.Click += (_, _) => OuvrirGestionComptes();
        menuFichierQuitter.Click += (_, _) => Close();
        menuOutilsOptions.Click += (_, _) => new FormulaireOptions().ShowDialog(this);
        menuAideAPropos.Click += (_, _) => MessageBox.Show(
            this,
            "Bot Dofus Retro 1.29\nPrivé — Hystoria\nÀ titre ludique",
            "À propos",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        // La fermeture propre arrête tous les proxies et scripts.
        FormClosing += FormulairePrincipal_FermetureDemandee;
    }

    private void FormulairePrincipal_Load(object sender, EventArgs e)
    {
        FabriqueMessages.EnregistrerMessagesStandards();

        // Niveau de log selon App.config
        var niveau = ConfigurationManager.AppSettings["Journalisation.Niveau"] ?? "Info";
        if (Enum.TryParse<NiveauJournal>(niveau, out var n)) Journaliseur.NiveauMinimum = n;

        Journaliseur.Info("Bot Dofus démarré");

        // Crée un onglet par compte connu.
        foreach (var entree in FichierComptes.Charger())
        {
            CreerOngletPourCompte(entree);
        }

        if (ongletsComptes.TabPages.Count == 0)
        {
            etiquetteEtat.Text = "Aucun compte — ouvrez Fichier > Gestion des comptes";
        }
    }

    private void OuvrirGestionComptes()
    {
        using var dlg = new FormulaireGestionComptes();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        // Synchronise les onglets avec la nouvelle liste.
        var nomsConnus = dlg.Comptes.Select(c => c.Identifiant).ToHashSet();

        // Supprime ceux qui n'existent plus.
        for (int i = ongletsComptes.TabPages.Count - 1; i >= 0; i--)
        {
            var page = ongletsComptes.TabPages[i];
            if (!nomsConnus.Contains(page.Text))
            {
                if (_contextes.TryGetValue(page.Text, out var ctx)) { ctx.Dispose(); _contextes.Remove(page.Text); }
                ongletsComptes.TabPages.Remove(page);
            }
        }

        // Ajoute les nouveaux.
        foreach (var entree in dlg.Comptes)
        {
            if (!_contextes.ContainsKey(entree.Identifiant))
            {
                CreerOngletPourCompte(entree);
            }
        }
    }

    private void CreerOngletPourCompte(EntreeCompte entree)
    {
        var compte = new Compte(entree.Identifiant, entree.MotDePasse);
        var configReseau = LireConfigReseau();
        var contexte = new ContexteCompte(compte, configReseau);
        _contextes[entree.Identifiant] = contexte;

        var page = new TabPage(entree.Identifiant);
        var sousOnglets = new TabControl { Dock = DockStyle.Fill };

        var uiPrincipal = new UI_Principal();
        uiPrincipal.LierContexte(contexte);
        sousOnglets.TabPages.Add(CreerPage("Principal", uiPrincipal));

        var uiPerso = new UI_Personnage();
        uiPerso.LierContexte(contexte);
        sousOnglets.TabPages.Add(CreerPage("Personnage", uiPerso));

        var uiCarte = new UI_Carte();
        uiCarte.LierContexte(contexte);
        sousOnglets.TabPages.Add(CreerPage("Carte", uiCarte));

        var uiInventaire = new UI_Inventaire();
        uiInventaire.LierContexte(contexte);
        sousOnglets.TabPages.Add(CreerPage("Inventaire", uiInventaire));

        var uiSorts = new UI_Sorts();
        uiSorts.LierContexte(contexte);
        sousOnglets.TabPages.Add(CreerPage("Sorts", uiSorts));

        var uiMetiers = new UI_Metiers();
        uiMetiers.LierContexte(contexte);
        sousOnglets.TabPages.Add(CreerPage("Métiers", uiMetiers));

        var uiCombat = new UI_Combat();
        uiCombat.LierContexte(contexte);
        sousOnglets.TabPages.Add(CreerPage("Combat", uiCombat));

        var uiDebug = new UI_Debug();
        uiDebug.LierContexte(contexte);
        sousOnglets.TabPages.Add(CreerPage("Debug", uiDebug));

        page.Controls.Add(sousOnglets);
        ongletsComptes.TabPages.Add(page);
    }

    private static TabPage CreerPage(string titre, Control contenu)
    {
        var p = new TabPage(titre);
        contenu.Dock = DockStyle.Fill;
        p.Controls.Add(contenu);
        return p;
    }

    private static ConfigReseau LireConfigReseau()
    {
        var config = new ConfigReseau();
        config.HoteDistant = ConfigurationManager.AppSettings["ServeurAuth.Hote"] ?? config.HoteDistant;
        if (int.TryParse(ConfigurationManager.AppSettings["ServeurAuth.Port"], out var pd)) config.PortDistant = pd;
        if (int.TryParse(ConfigurationManager.AppSettings["Proxy.PortLocal"], out var pl)) config.PortEcouteLocal = pl;
        return config;
    }

    private void FormulairePrincipal_FermetureDemandee(object? sender, FormClosingEventArgs e)
    {
        foreach (var ctx in _contextes.Values)
        {
            try { ctx.Dispose(); } catch { }
        }
        _contextes.Clear();
    }
}
