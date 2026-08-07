namespace BotDofus.Formulaires;

partial class FormulairePrincipal
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Code généré par le Concepteur Windows Form

    private void InitializeComponent()
    {
        this.menuPrincipal = new System.Windows.Forms.MenuStrip();
        this.menuFichier = new System.Windows.Forms.ToolStripMenuItem();
        this.menuFichierGestionComptes = new System.Windows.Forms.ToolStripMenuItem();
        this.menuFichierSeparateur1 = new System.Windows.Forms.ToolStripSeparator();
        this.menuFichierQuitter = new System.Windows.Forms.ToolStripMenuItem();
        this.menuOutils = new System.Windows.Forms.ToolStripMenuItem();
        this.menuOutilsOptions = new System.Windows.Forms.ToolStripMenuItem();
        this.menuAide = new System.Windows.Forms.ToolStripMenuItem();
        this.menuAideAPropos = new System.Windows.Forms.ToolStripMenuItem();
        this.ongletsComptes = new System.Windows.Forms.TabControl();
        this.barreEtat = new System.Windows.Forms.StatusStrip();
        this.etiquetteEtat = new System.Windows.Forms.ToolStripStatusLabel();
        this.menuPrincipal.SuspendLayout();
        this.barreEtat.SuspendLayout();
        this.SuspendLayout();
        //
        // menuPrincipal
        //
        this.menuPrincipal.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFichier,
            this.menuOutils,
            this.menuAide});
        this.menuPrincipal.Location = new System.Drawing.Point(0, 0);
        this.menuPrincipal.Name = "menuPrincipal";
        this.menuPrincipal.Size = new System.Drawing.Size(1024, 24);
        this.menuPrincipal.TabIndex = 0;
        //
        // menuFichier
        //
        this.menuFichier.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFichierGestionComptes,
            this.menuFichierSeparateur1,
            this.menuFichierQuitter});
        this.menuFichier.Name = "menuFichier";
        this.menuFichier.Size = new System.Drawing.Size(54, 20);
        this.menuFichier.Text = "Fichier";
        //
        // menuFichierGestionComptes
        //
        this.menuFichierGestionComptes.Name = "menuFichierGestionComptes";
        this.menuFichierGestionComptes.Size = new System.Drawing.Size(200, 22);
        this.menuFichierGestionComptes.Text = "Gestion des comptes...";
        //
        // menuFichierSeparateur1
        //
        this.menuFichierSeparateur1.Name = "menuFichierSeparateur1";
        this.menuFichierSeparateur1.Size = new System.Drawing.Size(197, 6);
        //
        // menuFichierQuitter
        //
        this.menuFichierQuitter.Name = "menuFichierQuitter";
        this.menuFichierQuitter.Size = new System.Drawing.Size(200, 22);
        this.menuFichierQuitter.Text = "Quitter";
        //
        // menuOutils
        //
        this.menuOutils.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuOutilsOptions});
        this.menuOutils.Name = "menuOutils";
        this.menuOutils.Size = new System.Drawing.Size(50, 20);
        this.menuOutils.Text = "Outils";
        //
        // menuOutilsOptions
        //
        this.menuOutilsOptions.Name = "menuOutilsOptions";
        this.menuOutilsOptions.Size = new System.Drawing.Size(135, 22);
        this.menuOutilsOptions.Text = "Options...";
        //
        // menuAide
        //
        this.menuAide.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuAideAPropos});
        this.menuAide.Name = "menuAide";
        this.menuAide.Size = new System.Drawing.Size(43, 20);
        this.menuAide.Text = "Aide";
        //
        // menuAideAPropos
        //
        this.menuAideAPropos.Name = "menuAideAPropos";
        this.menuAideAPropos.Size = new System.Drawing.Size(136, 22);
        this.menuAideAPropos.Text = "À propos...";
        //
        // ongletsComptes
        //
        this.ongletsComptes.Dock = System.Windows.Forms.DockStyle.Fill;
        this.ongletsComptes.Location = new System.Drawing.Point(0, 24);
        this.ongletsComptes.Name = "ongletsComptes";
        this.ongletsComptes.SelectedIndex = 0;
        this.ongletsComptes.Size = new System.Drawing.Size(1024, 554);
        this.ongletsComptes.TabIndex = 1;
        //
        // barreEtat
        //
        this.barreEtat.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.etiquetteEtat});
        this.barreEtat.Location = new System.Drawing.Point(0, 578);
        this.barreEtat.Name = "barreEtat";
        this.barreEtat.Size = new System.Drawing.Size(1024, 22);
        this.barreEtat.TabIndex = 2;
        //
        // etiquetteEtat
        //
        this.etiquetteEtat.Name = "etiquetteEtat";
        this.etiquetteEtat.Size = new System.Drawing.Size(32, 17);
        this.etiquetteEtat.Text = "Prêt";
        //
        // FormulairePrincipal
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1024, 600);
        this.Controls.Add(this.ongletsComptes);
        this.Controls.Add(this.barreEtat);
        this.Controls.Add(this.menuPrincipal);
        this.MainMenuStrip = this.menuPrincipal;
        this.Name = "FormulairePrincipal";
        this.Text = "Bot Dofus Retro 1.29";
        this.Load += new System.EventHandler(this.FormulairePrincipal_Load);
        this.menuPrincipal.ResumeLayout(false);
        this.menuPrincipal.PerformLayout();
        this.barreEtat.ResumeLayout(false);
        this.barreEtat.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.MenuStrip menuPrincipal;
    private System.Windows.Forms.ToolStripMenuItem menuFichier;
    private System.Windows.Forms.ToolStripMenuItem menuFichierGestionComptes;
    private System.Windows.Forms.ToolStripSeparator menuFichierSeparateur1;
    private System.Windows.Forms.ToolStripMenuItem menuFichierQuitter;
    private System.Windows.Forms.ToolStripMenuItem menuOutils;
    private System.Windows.Forms.ToolStripMenuItem menuOutilsOptions;
    private System.Windows.Forms.ToolStripMenuItem menuAide;
    private System.Windows.Forms.ToolStripMenuItem menuAideAPropos;
    private System.Windows.Forms.TabControl ongletsComptes;
    private System.Windows.Forms.StatusStrip barreEtat;
    private System.Windows.Forms.ToolStripStatusLabel etiquetteEtat;
}
