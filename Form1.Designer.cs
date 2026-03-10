namespace Blockchain;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    // Pannelli principali
    private Panel pnlDettaglio;        
    private Panel pnlContainer;        
    private Panel pnlHeaderDettaglio;  
    private Panel pnlLogin;           
    // Elementi UI
    private Label lblTitle;
    private Label lblSeleziona;
    private PictureBox picGrafico;

    // Bottoni Funzionalità (Dashboard)
    private Button btnAggiungiWallet;
    private Button btnVisualizzaTransazione;
    private Button btnInviaTransazione;
    private Button btnVisualizzaBlockchain;
    private Button btnAnalitiche;
    private Button btnUTXOSet;
    private Button btncreaindirizzo; 
    private Button btnMining;   
    private Button btnIndietroWallet;
    private Button btnIndietroBlockchain;

    // Bottoni Selezione Account (Login)
    private Button btnNode1;
    private Button btnNode2;
    private Button btnNode3;
    private Button btnNode4;
    private Button btnHome;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        // Inizializzazione controlli
        pnlDettaglio = new Panel();
        pnlContainer = new Panel();
        pnlHeaderDettaglio = new Panel();
        pnlLogin = new Panel();
        lblTitle = new Label();
        lblSeleziona = new Label();
        picGrafico = new PictureBox();

        btnAggiungiWallet = new Button();
        btnVisualizzaTransazione = new Button();
        btnInviaTransazione = new Button();
        btnVisualizzaBlockchain = new Button();
        btnAnalitiche = new Button();
        btnUTXOSet = new Button();
        btnMining = new Button();
        btncreaindirizzo = new Button();
        btnIndietroWallet = new Button();
        btnIndietroBlockchain = new Button();
        btnNode1 = new Button();
        btnNode2 = new Button();
        btnNode3 = new Button();
        btnNode4 = new Button();
        btnHome = new Button();

        // 
        // Form1
        // 
        this.ClientSize = new Size(1000, 600);
        this.BackColor = Color.FromArgb(24, 28, 36);
        this.Text = "Blockchain Simulator";
        this.StartPosition = FormStartPosition.CenterScreen;

        // 
        // 1. PANNELLO LOGIN (Schermata iniziale)
        // 
        pnlLogin.Dock = DockStyle.Fill;
        pnlLogin.BackColor = Color.FromArgb(24, 28, 36);
        
        lblSeleziona.Text = "BENVENUTO, SELEZIONA IL TUO ACCOUNT NODO";
        lblSeleziona.ForeColor = Color.White;
        lblSeleziona.Font = new Font("Segoe UI", 16, FontStyle.Bold);
        lblSeleziona.Location = new Point(230, 100);
        lblSeleziona.AutoSize = true;

        StilizzaBottoneLogin(btnNode1, "ACCEDI AL NODO: 8080", 200);
        StilizzaBottoneLogin(btnNode2, "ACCEDI AL NODO: 8081", 270);
        StilizzaBottoneLogin(btnNode3, "ACCEDI AL NODO: 8082", 340);
        StilizzaBottoneLogin(btnNode4, "ACCEDI AL NODO: 8083", 410);

// Nel file Form1.Designer.cs (o dove crei i bottoni)
        btnNode1.Tag = "8080";
        btnNode2.Tag = "8081";
        btnNode3.Tag = "8082";
        btnNode4.Tag = "8083";

        pnlLogin.Controls.Add(lblSeleziona);
        pnlLogin.Controls.Add(btnNode1);
        pnlLogin.Controls.Add(btnNode2);
        pnlLogin.Controls.Add(btnNode3);
        pnlLogin.Controls.Add(btnNode4);

        // 
        // 2. PANNELLI DASHBOARD (Inizialmente nascosti)
        // 
        pnlDettaglio.Dock = DockStyle.Left;
        pnlDettaglio.Width = 230;
        pnlDettaglio.BackColor = Color.FromArgb(32, 38, 50);
        pnlDettaglio.Visible = false;

        pnlHeaderDettaglio.Dock = DockStyle.Top;
        pnlHeaderDettaglio.Height = 70;
        pnlHeaderDettaglio.BackColor = Color.FromArgb(0, 80, 200);
        pnlHeaderDettaglio.Visible = false;

        pnlContainer.Dock = DockStyle.Fill;
        pnlContainer.Visible = false;

        lblTitle.Text = "🔒 BLOCKCHAIN MANAGER";
        lblTitle.ForeColor = Color.White;
        lblTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
        lblTitle.Location = new Point(10, 20);
        lblTitle.AutoSize = true;
        pnlHeaderDettaglio.Controls.Add(lblTitle);

        // 
        // 3. BOTTONI FUNZIONALI (Dashboard)
        // 
        StilizzaBottoneFunzione(btnAggiungiWallet, "Wallet Disponibili", 100);
        StilizzaBottoneFunzione(btnVisualizzaTransazione, "Cronologia Transazioni", 170);
        StilizzaBottoneFunzione(btnVisualizzaBlockchain, "Visualizza Blockchain", 240);
        StilizzaBottoneFunzione(btnInviaTransazione, "Invia Transazione", 310);
        StilizzaBottoneFunzione(btnAnalitiche, "Analitiche", 380);
        StilizzaBottoneFunzione(btnUTXOSet, "UTXO Set", 450);
        StilizzaBottoneFunzione(btncreaindirizzo, "Crea Indirizzo", 520);
        StilizzaBottoneFunzione(btnMining, "Mining", 590);

        // PictureBox per i grafici Python
        picGrafico.Size = new Size(500, 350);
        picGrafico.Location = new Point(20, 100);
        picGrafico.SizeMode = PictureBoxSizeMode.Zoom;
        picGrafico.Visible = false;
        pnlContainer.Controls.Add(picGrafico);

        // Aggiunta controlli al Container
        pnlContainer.Controls.Add(btnAggiungiWallet);
        pnlContainer.Controls.Add(btnVisualizzaTransazione);
        pnlContainer.Controls.Add(btnVisualizzaBlockchain);
        pnlContainer.Controls.Add(btnAnalitiche);
        pnlContainer.Controls.Add(btnUTXOSet);
        pnlContainer.Controls.Add(btncreaindirizzo);
        pnlContainer.Controls.Add(btnInviaTransazione);
        pnlContainer.Controls.Add(btnMining);

        // Aggiunta pannelli principali al Form
        this.Controls.Add(pnlLogin); // Il login è l'ultimo aggiunto sopra gli altri
        this.Controls.Add(pnlContainer);
        this.Controls.Add(pnlDettaglio);
        this.Controls.Add(pnlHeaderDettaglio);
    }

    private void StilizzaBottoneLogin(Button btn, string testo, int y)
    {
        btn.Text = testo;
        btn.Size = new Size(350, 50);
        btn.Location = new Point(325, y);
        btn.FlatStyle = FlatStyle.Flat;
        btn.BackColor = Color.FromArgb(0, 120, 215);
        btn.ForeColor = Color.White;
        btn.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        btn.Cursor = Cursors.Hand;
        btn.FlatAppearance.BorderSize = 0;
    }

    private void StilizzaBottoneFunzione(Button btn, string testo, int y)
    {
        btn.Text = testo;
        btn.Size = new Size(250, 50);
        btn.Location = new Point(350, y); // Centrati nel container
        btn.FlatStyle = FlatStyle.Flat;
        btn.BackColor = Color.FromArgb(41, 171, 226);
        btn.ForeColor = Color.White;
        btn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        btn.Cursor = Cursors.Hand;
        btn.FlatAppearance.BorderSize = 0;
    }

    #endregion
}