namespace Blockchain;
partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private Panel pnlDettaglio;
    private Panel pnlContainer;
    private Panel pnlHeaderDettaglio;
    private Label lblTitle;
    private Button btnAggiungiWallet;
    private Button btnInviaTransazione;
    private Button btnVisualizzaBlockchain; 
    private Button btnAnalitiche;

    private PictureBox picGrafico;


     private System.ComponentModel.IContainer components = null;
    
    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        pnlDettaglio = new Panel();
        pnlContainer = new Panel();
        pnlHeaderDettaglio = new Panel();
        lblTitle = new Label();
        btnAggiungiWallet = new Button();
        btnInviaTransazione = new Button();
        btnVisualizzaBlockchain = new Button();
        btnAnalitiche = new Button();
        // 
        // Form1
        // 
        this.ClientSize = new Size(1000, 600); // Più grande per ospitare la catena
        this.BackColor = Color.FromArgb(24, 28, 36); // Colore scuro dell'immagine
        this.Text = "BlockchainHome";

        //Grafico
        this.picGrafico = new PictureBox();
        this.picGrafico.Size = new Size(600, 400); // Dimensione adatta al grafico
        this.picGrafico.Location = new Point(20, 200); // Posizionalo sotto le card delle statistiche
        this.picGrafico.SizeMode = PictureBoxSizeMode.Zoom; // Mantiene le proporzioni (Software Robusto)
        this.picGrafico.Visible = false;
        this.pnlContainer.Controls.Add(this.picGrafico);

        // 
        // pnlDettaglio (Il menù a tendina laterale)
        // 
        pnlDettaglio.Dock = DockStyle.Left;
        pnlDettaglio.Width = 230;
        pnlDettaglio.BackColor = Color.FromArgb(32, 38, 50);
        pnlDettaglio.Visible = false; 

        // 
        // pnlHeaderDettaglio (Header del menù laterale)
        // 
        pnlHeaderDettaglio.Dock = DockStyle.Top;
        pnlHeaderDettaglio.Height = 70;
        pnlHeaderDettaglio.BackColor = Color.FromArgb(0, 80, 200);
        pnlHeaderDettaglio.Visible = false;
        pnlHeaderDettaglio.Controls.Add(lblTitle);
        // 
        // pnlContainer (Dove caricheremo i blocchi o il wallet)
        // 
        pnlContainer.Dock = DockStyle.Fill;

        // 
        // lblTitle
        // 
        lblTitle.Text = "🔒BENVENUTO NELLA BLOCKCHAIN";
        lblTitle.ForeColor = Color.White;
        lblTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
        lblTitle.Location = new Point(250, 50);
        lblTitle.AutoSize = true;
       

        // Stilizziamo i bottoni come "Card" per l'inizio
        StilizzaBottone(btnAggiungiWallet, "Wallet Disponibili", 150);
        StilizzaBottone(btnInviaTransazione, "Cronologia Transazioni", 220);
        StilizzaBottone(btnVisualizzaBlockchain, "Visualizza Blockchain", 290);
        StilizzaBottone(btnAnalitiche, "Analitiche", 360);
        
    
        // I bottoni delle azioni li aggiungiamo inizialmente al form centrale
        this.Controls.Add(pnlContainer);
        this.Controls.Add(pnlDettaglio);
        this.Controls.Add(pnlHeaderDettaglio);
        pnlContainer.Controls.Add(lblTitle);
        pnlContainer.Controls.Add(btnAggiungiWallet);
        pnlContainer.Controls.Add(btnInviaTransazione);
        pnlContainer.Controls.Add(btnVisualizzaBlockchain);
        pnlContainer.Controls.Add(btnAnalitiche);
    }

    private void StilizzaBottone(Button btn, string testo, int y)
    {
        btn.Text = testo;
        btn.Size = new Size(250, 50);
        btn.Location = new Point(380, y);
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = Color.FromArgb(0, 120, 215);
        btn.ForeColor = Color.White;
        btn.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        btn.Cursor = Cursors.Hand;
    }
    

    #endregion
}

