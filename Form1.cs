using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using Blockchain.Core;
using System.IO;
using System.Text.Json;

namespace Blockchain
{
    public partial class Form1 : Form
    {
        // 1. Orientamento ai Componenti: gestore della logica come campo privato
        private readonly BlockchainManager _blockchainManager;

        public Form1()
        {
            InitializeComponent();

            // Inizializzazione della logica
            _blockchainManager = new BlockchainManager();

            btnNode1.Click += Nodo_Click;
            btnNode2.Click += Nodo_Click;
            btnNode3.Click += Nodo_Click;
            btnNode4.Click += Nodo_Click;

            // 2. Gestione Eventi Standard
            btnVisualizzaBlockchain.Click += BtnVisualizzaBlockchain_Click;
            btnAggiungiWallet.Click += BtnAggiungiWallet_Click;
            btnVisualizzaTransazione.Click += BtnVisualizzaTransazione_Click;
            btnAnalitiche.Click += BtnAnalitiche_Click;
            btnUTXOSet.Click += BtnUTXOSet_Click;
            btncreaindirizzo.Click += btncreaindirizzo_Click;
            btnInviaTransazione.Click += BtnInviaTransazione_Click;
            btnIndietroWallet.Click += BtnAggiungiWallet_Click;
            btnIndietroBlockchain.Click += BtnVisualizzaBlockchain_Click;
            btnMining.Click += BtnMining_Click;

        }

        // --- HANDLER EVENTI ---

        private async void Nodo_Click(object? sender, EventArgs e)
{
    if (sender is Button btn && btn.Tag != null)
    {
        string portaScelta = btn.Tag.ToString()!;
        
        // 1. Impostiamo la porta
        _blockchainManager.PortaCorrente = portaScelta;
        EntraNelNodo(portaScelta);

    }
}
        private async void BtnInviaTransazione_Click(object? sender, EventArgs e)
{

    DisegnaInterfacciaInvio();
    
    await CaricaDatiPerInvio();
}
        private async Task CaricaDatiPerInvio()
{
    // Recuperiamo la lista aggiornata direttamente dal metodo
    List<string> rubricaAggiornata = await _blockchainManager.SincronizzaRubricaGlobaleAsync();

    // Recuperiamo i tuoi wallet come oggetto WalletRoot
    WalletRoot? mieiWallet = await _blockchainManager.EstraiListaWallet();

        if (mieiWallet?.Addresses == null)
        {
            MessageBox.Show("Errore nel recupero dei wallet.");
            return;
        }

    // Dichiariamo la lista dei miei indirizzi
    List<string> mieiIndirizzi = new List<string>();

    // Se l'oggetto WalletRoot e la lista di Addresses non sono null, popoliamo la lista
    if (mieiWallet != null && mieiWallet.Addresses != null)
    {
        mieiIndirizzi = mieiWallet.Addresses;
    }

    // Creiamo la lista dei destinatari esterni
    List<string> destinatariEsterni = new List<string>();
    foreach (string addr in rubricaAggiornata)
    {
        if (!mieiIndirizzi.Contains(addr))
        {
            destinatariEsterni.Add(addr);
        }
    }

    // Assegniamo i valori ai controlli UI, se non sono null
    if (_selezioneFrom != null)
    {
        _selezioneFrom.DataSource = mieiIndirizzi;
    }
    if (_selezioneTo != null)
    {
        _selezioneTo.DataSource = destinatariEsterni;
    }
}

        private async void BtnVisualizzaBlockchain_Click(object? sender, EventArgs e)
        {
             PreparaAreaLavoro("BLOCKCHAIN");

    try
    {
       List<Blocks> blocchiRicevuti = await _blockchainManager.SincronizzaBlockchain();
        CaricaBlocchiGrafici(blocchiRicevuti);
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message, "Errore Sincronizzazione", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
        

        private void BtnAnalitiche_Click(object? sender, EventArgs e)
        {
            _blockchainManager.EseguiAggiornamentoPython();
            string nomeFile = "analitiche.json";

            if (File.Exists(nomeFile))
            {
                string contenutoJson = File.ReadAllText(nomeFile);
                 PreparaAreaLavoro("ANALITICHE");
                VisualizzaStatistiche(contenutoJson);
            }
            else
            {
                MessageBox.Show("File analitiche.json non trovato!");
            }
        }

        private async void BtnMining_Click(object? sender, EventArgs e)
        {
            PreparaAreaLavoro("MINING");
            await DisegnaSezioneMining();
        }

       private async void BtnAggiungiWallet_Click(object? sender, EventArgs e)
{
     PreparaAreaLavoro("WALLET");

    try
    {
         WalletRoot walletData = await _blockchainManager.EstraiListaWallet();
         if (walletData != null)
            {
                if (walletData.Addresses == null || walletData.Addresses.Count == 0)
                {
                    MessageBox.Show("Nessun wallet trovato nel nodo Go.");
                    return;
                }
            }
            else
            {
                MessageBox.Show("Errore nel recupero dei wallet.");
                return;
            }

        CaricaWalletGrafici(walletData.Addresses, walletData.Count);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Errore di runtime: {ex.Message}");
    }
}
        
        private async void BtnVisualizzaTransazione_Click(object? sender, EventArgs e)
{
    PreparaAreaLavoro("TUTTE LE TRANSAZIONI");

    try
    {
        // 1. Recuperiamo la blockchain aggiornata dal nodo Go
        List<Blocks> catenaCompleta = await _blockchainManager.SincronizzaBlockchain();

        // 2. Estraiamo TUTTE le transazioni da TUTTI i blocchi
        List<TransactionData> tutteLeTransazioni = new List<TransactionData>();
        
        foreach (var blocco in catenaCompleta)
        {
            if (blocco.Transactions != null)
            {
                tutteLeTransazioni.AddRange(blocco.Transactions);
            }
        }

        TransazioniGrafiche(tutteLeTransazioni);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Errore nel recupero transazioni: {ex.Message}");
    }
}

        
        private async void BtnUTXOSet_Click(object? sender, EventArgs e)
        {
            _blockchainManager.EseguiAggiornamentoPython();
              PreparaAreaLavoro("UTXOSET");
            try
            {
                UtxoResponse risposta = await _blockchainManager.EstraiUTXOSet();
                VisualizzaUTXOSet(risposta.count, risposta.Utxos);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore di runtime: {ex.Message}");
            }
            
        }

        private async void btncreaindirizzo_Click(object? sender, EventArgs e)
{
    // Cambiamo temporaneamente il titolo per dare feedback all'utente
     PreparaAreaLavoro("CREAINDIRIZZO");

    try 
    {
        
        string nuovoIndirizzo = await _blockchainManager.CreateAddressAsync();

        // Visualizziamo il risultato che arriva dalla tua funzione esterna
            VisualizzaNuovoIndirizzo(nuovoIndirizzo);
    }
    catch (Exception ex)
    {
        MessageBox.Show("Errore nel collegamento: " + ex.Message);
    }
}

        // --- LOGICA UI E GRAFICA ---
    
private void EntraNelNodo(string porta)
{
    _blockchainManager.PortaCorrente = porta;

    pnlLogin.Visible = false;
    pnlDettaglio.Visible = true;
    pnlHeaderDettaglio.Visible = true;
    pnlContainer.Visible = true;

    ConfiguraMenuNavigazione(porta);

    lblTitle.Text = $"ACCOUNT ATTIVO: NODO {porta} - SELEZIONA FUNZIONE";
    lblTitle.Location = new Point(200, 20);

}
private void PreparaAreaLavoro(string nomeOperazione)
{
    
    pnlContainer.Controls.Clear();
    lblTitle.Text = $"{nomeOperazione} - NODO {_blockchainManager.PortaCorrente}";
    lblTitle.Location = new Point(370, 20);
}

private void ConfiguraMenuNavigazione(string porta)
{
    pnlDettaglio.Controls.Clear();

    AggiungiBottoneAlMenu(btnUTXOSet);
    AggiungiBottoneAlMenu(btnAnalitiche);
    AggiungiBottoneAlMenu(btnVisualizzaTransazione);
    AggiungiBottoneAlMenu(btnInviaTransazione);
    AggiungiBottoneAlMenu(btnAggiungiWallet);
    AggiungiBottoneAlMenu(btnMining);
    AggiungiBottoneAlMenu(btncreaindirizzo);
    AggiungiBottoneAlMenu(btnVisualizzaBlockchain);
    AggiungiBottoneAlMenu(btnHome);
    AggiungiBottoneAlMenu(btnIndietroWallet);
    AggiungiBottoneAlMenu(btnIndietroBlockchain);
   

    
    StilizzaBottoneNav(btnHome, "🏠 HOME", Color.FromArgb(45, 45, 48));
    StilizzaBottoneNav(btnIndietroWallet, "INDIETRO", Color.FromArgb(45, 45, 48));
    StilizzaBottoneNav(btnIndietroBlockchain, "INDIETRO", Color.FromArgb(0, 122, 204));
    btnIndietroWallet.Visible = false; 
    btnIndietroBlockchain.Visible = false;
    btnHome.Click += BtnHome_Click;

}

private void AggiungiBottoneAlMenu(Button btn)
{
    btn.Dock = DockStyle.Top;
    btn.Height = 50;
    btn.FlatStyle = FlatStyle.Flat;
    btn.FlatAppearance.BorderSize = 0;
    btn.ForeColor = Color.White;
    btn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
    pnlDettaglio.Controls.Add(btn);
}
private void StilizzaBottoneNav(Button btn, string testo, Color colore)
{
    btn.Text = testo;
    btn.BackColor = colore;
    btn.Cursor = Cursors.Hand;
}

// Handler per il bottone Home: torna alla schermata di selezione nodi
private void BtnHome_Click(object? sender, EventArgs e)
{
    _blockchainManager.PortaCorrente = null; 
    
    pnlLogin.Visible = true;
    pnlDettaglio.Visible = false;
    pnlHeaderDettaglio.Visible = false;
    pnlContainer.Visible = false;
    pnlContainer.Controls.Clear();
}
   
        private void CaricaBlocchiGrafici(List<Blocks> catena)
{
    if (catena == null || catena.Count == 0) return;

    pnlContainer.Controls.Clear();
    pnlContainer.AutoScroll = true;
    btnIndietroBlockchain.Visible = false; 

    catena.Sort(delegate(Blocks a, Blocks b) {
        return a.Height.CompareTo(b.Height);
    });

    int altezzaMassima = 0;
    foreach (var b in catena) {
        if (b.Height > altezzaMassima) altezzaMassima = b.Height;
    }

    int coordinataX = 20;

    foreach (var bloccoDati in catena)
    {
        Panel bloccoGrafico = CreaSingoloBlocco(
            bloccoDati.Timestamp.ToString(),
            bloccoDati.Hash,
            bloccoDati.Nonce,
            bloccoDati.Transactions,
            bloccoDati.Height
        );

        bloccoGrafico.Location = new Point(coordinataX, 50);
        pnlContainer.Controls.Add(bloccoGrafico);

        if (bloccoDati.Height < altezzaMassima)
        {
            Label freccia = new Label
            {
                Text = "➔",
                ForeColor = Color.FromArgb(41, 171, 226),
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                Location = new Point(coordinataX + 230, 180), 
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlContainer.Controls.Add(freccia);
            freccia.BringToFront();
        }

        coordinataX += 285; 
    }
}
       private Panel CreaSingoloBlocco(string timestamp, string hash, int nonce, List<TransactionData>? transactions, int height)
{

    Panel card = new Panel
    {
        Size = new Size(230, 280),
        BackColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle
    };


    Label lblHeader = new Label
    {
        Text = "🔒 BLOCK #" + height,
        BackColor = Color.FromArgb(41, 171, 226),
        ForeColor = Color.White,
        Dock = DockStyle.Top,
        Height = 35,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 10, FontStyle.Bold)
    };

    
    Label lblTimestamp = new Label
    {
        Text = "Data: " + timestamp,
        ForeColor = Color.DimGray,
        Location = new Point(10, 45), 
        Size = new Size(200, 20),
        TextAlign = ContentAlignment.MiddleLeft,
        Font = new Font("Segoe UI", 8)
    };

    
    string hashBreve = (hash.Length > 15) ? hash.Substring(0, 15) + "..." : hash;
    Label lblHash = new Label
    {
        Text = "Hash: " + hashBreve,
        Location = new Point(10, 70),
        Size = new Size(200, 20),
        ForeColor = Color.Black,
        Font = new Font("Segoe UI", 8, FontStyle.Bold)
    };

    
    Label lblTitoloTrans = new Label
    {
        Text = "Transazioni:",
        Location = new Point(10, 100),
        Size = new Size(200, 15),
        Font = new Font("Segoe UI", 8, FontStyle.Underline)
    };
FlowLayoutPanel flowTrans = new FlowLayoutPanel {
    Location = new Point(10, 120),
    Size = new Size(210, 100),
    BackColor = Color.FromArgb(245, 245, 245),
    AutoScroll = true,
    FlowDirection = FlowDirection.TopDown,
    WrapContents = false
};
    if (transactions != null && transactions.Count > 0) {
    foreach (var tx in transactions) {
        Button btnLink = new Button {
            Text = "• " + (tx.id?.Length > 12 ? tx.id.Substring(0, 12) + "..." : tx.id),
            Size = new Size(185, 25),
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.Blue,
            Cursor = Cursors.Hand,
            Font = new Font("Consolas", 8, FontStyle.Underline),
            // METADATA: Salviamo l'ID intero nel Tag per il Late Binding
            Tag = tx.id 
        };
        btnLink.FlatAppearance.BorderSize = 0;
        
        // Sottoscrizione all'evento Click
        btnLink.Click += LinkTransazione_Click;
        flowTrans.Controls.Add(btnLink);
    }
}

    Label lblNonce = new Label
    {
        Text = "Nonce: " + nonce,
        Location = new Point(10, 230),
        Size = new Size(200, 25),
        TextAlign = ContentAlignment.MiddleRight,
        Font = new Font("Segoe UI", 8, FontStyle.Italic)
    };

    // Aggiungiamo tutto alla scheda
    card.Controls.Add(lblHeader);
    card.Controls.Add(lblTimestamp);
    card.Controls.Add(lblHash);
    card.Controls.Add(lblTitoloTrans);
    card.Controls.Add(flowTrans);
    
    card.Controls.Add(lblNonce);

    return card;
}
private async void LinkTransazione_Click(object? sender, EventArgs e)
{   btnIndietroWallet.Visible = true; 

    if (sender is Button btn && btn.Tag != null)
    {
        string idCercato = btn.Tag.ToString()!;

        try
        {
            // Recupero della catena (Software Robusto e Durevole)
            List<Blocks> catena = await _blockchainManager.SincronizzaBlockchain();
            TransactionData? txTrovata = null;

            // CICLO 1: Esaminiamo ogni blocco nella catena
            foreach (Blocks b in catena)
            {
                if (b.Transactions != null)
                {
                    foreach (TransactionData t in b.Transactions)
                    {
                        if (t.id == idCercato)
                        {
                            txTrovata = t;
                            break; // Trovata! Esco dal ciclo interno
                        }
                    }
                }
                if (txTrovata != null) break; // Esco dal ciclo esterno
            }

            if (txTrovata != null)
            {
                PreparaAreaLavoro("DETTAGLIO TRANSAZIONE");
                TransazioniGrafiche(new List<TransactionData> { txTrovata });
            }
            else
            {
                MessageBox.Show("Transazione non trovata nel registro.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Errore durante l'ispezione dei Metadata: " + ex.Message);
        }
    }
}
       private void VisualizzaStatistiche(string jsonContenuto)
{
   pnlContainer.Controls.Clear();
    var stats = _blockchainManager.EstraiAnalitiche(jsonContenuto);
    if (stats == null || stats.Count == 0) return;

    int coordinataX = 20;
    pnlContainer.AutoScroll = true;

    List<Analitica> altreStats = new List<Analitica>();
    Analitica? statRicchi = null;

    foreach (var s in stats)
    {
        if (s.Titolo.Contains("Ricchi") || s.Titolo.Contains("Indirizzi"))
        {
            statRicchi = s;
        }
        else
        {
            altreStats.Add(s);
        }
    }

   foreach (var s in altreStats)
{
    Panel card = new Panel
    {
        Size = new Size(170, 100),
        Location = new Point(coordinataX, 50),
        BackColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle
    };

    // Label per il TITOLO (Sempre Blu)
    Label lblTitolo = new Label
    {
        Text = s.Titolo.ToUpper(),
        Dock = DockStyle.Top,
        Height = 40,
        TextAlign = ContentAlignment.BottomCenter,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        ForeColor = Color.Blue 
    };

    Label lblValore = new Label
    {
        Text = s.Valore,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.TopCenter,
        Font = new Font("Segoe UI", 11, FontStyle.Bold),
        ForeColor = Color.Black 
    };

    card.Controls.Add(lblValore);
    card.Controls.Add(lblTitolo);
    
    pnlContainer.Controls.Add(card);
    coordinataX += 190;
}

    int yOffset = 180;
    if (statRicchi != null)
    {
        Panel leaderboard = new Panel
        {
            Size = new Size(500, 140),
            Location = new Point(120, yOffset),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        Label lblTitolo = new Label
        {
            Text = "🏆 TOP 3 INDIRIZZI PIÙ RICCHI",
            Dock = DockStyle.Top,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.Blue
        };

        Label lblDati = new Label
        {
            Text = statRicchi.Valore,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Consolas", 9, FontStyle.Bold),
            ForeColor = Color.Black
        };

        leaderboard.Controls.Add(lblDati);
        leaderboard.Controls.Add(lblTitolo);
        pnlContainer.Controls.Add(leaderboard);
        
        yOffset += 160; 
    }

    Size sizeGrafico1 = new Size(700, 400);
     Size sizeGrafico = new Size(700, 350);
    AggiungiGrafico("grafico_blockchain.png", yOffset, sizeGrafico1);
    AggiungiGrafico("grafico_nonce.png", yOffset + 450, sizeGrafico);
}

// Metodo di supporto per i grafici
private void AggiungiGrafico(string path, int y, Size size)
{
    if (File.Exists(path))
    {
        PictureBox pic = new PictureBox
        {
            Image = Image.FromFile(path),
            Location = new Point(20, y),
            Size = size,
            SizeMode = PictureBoxSizeMode.Zoom
        };
        pnlContainer.Controls.Add(pic);
    }
}

    private void CaricaWalletGrafici(List<string> walletList, int count)
{
    pnlContainer.Controls.Clear();
    pnlContainer.AutoScroll = true;
    btnIndietroWallet.Visible = false; 
    pnlContainer.BackColor = Color.FromArgb(30, 33, 40);
    
    Label lblTitolo = new Label
    {
        Text = $"Wallet Disponibili: {count}",
        Font = new Font("Segoe UI", 14, FontStyle.Bold),
        ForeColor = Color.White,
        Location = new Point(20, 15),
        AutoSize = true
    };
    pnlContainer.Controls.Add(lblTitolo);

    int coordinataY = 60; 

    foreach (var wallet in walletList)
    {
        Panel walletCard = new Panel
        {
            Size = new Size(pnlContainer.Width - 60, 80),
            BackColor = Color.FromArgb(45, 50, 60), 
            Location = new Point(20, coordinataY),
            Padding = new Padding(10)
        };

        Label lblTag = new Label
        {
            Text = "WALLET",
            Font = new Font("Segoe UI", 7, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 122, 204),
            Location = new Point(15, 12),
            AutoSize = true
        };

        Label lblAddress = new Label
        {
            Text = wallet ?? "N/D",
            ForeColor = Color.White,
            Font = new Font("Consolas", 10), 
            Location = new Point(15, 35),
            Size = new Size(walletCard.Width - 200, 30), 
            TextAlign = ContentAlignment.MiddleLeft
        };
        // All'interno del foreach (var wallet in walletList)
Button btnSaldo = new Button
{
    Text = "SALDO",
    Size = new Size(130, 35),
    Location = new Point(walletCard.Width - 150, 22),
    BackColor = Color.FromArgb(0, 122, 204),
    ForeColor = Color.White,
    FlatStyle = FlatStyle.Flat,
    Cursor = Cursors.Hand,
    Tag = wallet 
};

// Colleghiamo l'evento click
btnSaldo.Click += BtnSaldo_Click;

walletCard.Controls.Add(btnSaldo);

        walletCard.Controls.Add(lblTag);
        walletCard.Controls.Add(lblAddress);
        pnlContainer.Controls.Add(walletCard);

        coordinataY += 95; 
    }
}
private async void BtnSaldo_Click(object? sender, EventArgs e)
{
    if (sender is Button btn && btn.Tag is string indirizzo)
    {
        BalanceResponse risposta = await _blockchainManager.OttieniSaldoCompletoAsync(indirizzo);

        string wallet = "";
        string saldo = "0";

        if (risposta != null)
        {
            if (risposta.Address != null)
            {
                wallet = risposta.Address;
            }

            if (risposta.Result != null)
            {
                saldo = risposta.Result;
            }
        }

        VisualizzaDettaglioSaldo(wallet, saldo);
    }
}
private void VisualizzaDettaglioSaldo(string indirizzo, string saldo)
{
    // 1. Configurazione del contenitore principale
    pnlContainer.Controls.Clear();
    pnlContainer.BackColor = Color.FromArgb(20, 22, 29); // Blu notte profondo (sfondo immagine)

    // 2. Titolo della sezione
    Label lblInfo = new Label
    {
        Text = "SALDO ATTUALE",
        Font = new Font("Segoe UI", 14, FontStyle.Bold),
        ForeColor = Color.White,
        Location = new Point(25, 25),
        AutoSize = true
    };

    // 3. Card principale (stile "glassmorphism" scuro)
    Panel resCard = new Panel
    {
        Size = new Size(pnlContainer.Width - 50, 160),
        Location = new Point(25, 75),
        BackColor = Color.FromArgb(32, 36, 47), // Grigio-blu della card
        Padding = new Padding(20)
    };

    // 4. Etichetta "Indirizzo"
    Label lblIndirizzoTitolo = new Label {
        Text = "INDIRIZZO WALLET",
        Font = new Font("Segoe UI", 8, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 122, 204), // Azzurro brillante per i tag
        Location = new Point(20, 20),
        AutoSize = true
    };

    Label lblIndirizzoValore = new Label {
        Text = indirizzo,
        ForeColor = Color.White,
        Font = new Font("Consolas", 11),
        Location = new Point(20, 45),
        Size = new Size(resCard.Width - 40, 30),
        TextAlign = ContentAlignment.MiddleLeft
    };

    Label lblSaldoTitolo = new Label {
        Text = "DISPONIBILITÀ ATTUALE",
        Font = new Font("Segoe UI", 8, FontStyle.Bold),
        ForeColor = Color.Gray,
        Location = new Point(20, 100),
        AutoSize = true
    };

    Label lblSaldoValore = new Label {
        Text = $"{saldo} BTC",
        ForeColor = Color.FromArgb(255, 165, 0), // Arancione/Oro come nell'immagine
        Font = new Font("Segoe UI", 16, FontStyle.Bold),
        Location = new Point(20, 120),
        AutoSize = true
    };

    // Assemblaggio dei componenti orientati agli oggetti
    resCard.Controls.Add(lblIndirizzoTitolo);
    resCard.Controls.Add(lblIndirizzoValore);
    resCard.Controls.Add(lblSaldoTitolo);
    resCard.Controls.Add(lblSaldoValore);
    
    pnlContainer.Controls.Add(lblInfo);
    pnlContainer.Controls.Add(resCard);
    btnIndietroWallet.Visible = true;
    
}
 private ComboBox? _selezioneFrom;
private ComboBox? _selezioneTo;
private ComboBox? _selezioneImporto;
private void DisegnaInterfacciaInvio()
{
    pnlContainer.Controls.Clear();

    Label lblTitolo = new Label {
        Text = "INVIA TRANSAZIONE:",
        Font = new Font("Segoe UI", 14, FontStyle.Bold),
        ForeColor = Color.White,
        Location = new Point(30, 20),
        AutoSize = true
    };

    _selezioneFrom = new ComboBox { 
        Location = new Point(30, 90), 
        Size = new Size(400, 30), 
        DropDownStyle = ComboBoxStyle.DropDownList, 
        BackColor = Color.White 
    };

    _selezioneTo = new ComboBox { 
        Location = new Point(30, 160),
        Size = new Size(400, 30), 
        DropDownStyle = ComboBoxStyle.DropDownList, 
        BackColor = Color.White 
    };
    
    _selezioneImporto = new ComboBox { 
        Location = new Point(30, 230), 
        Size = new Size(150, 30), 
        DropDownStyle = ComboBoxStyle.DropDownList, 
        BackColor = Color.White 
    };
    for (int i = 1; i <= 10; i++) _selezioneImporto.Items.Add(i);
    _selezioneImporto.SelectedIndex = 0;

    Button btnSoloInvia = new Button {
        Text = "INVIA TRANSAZIONE",
        Location = new Point(30, 280),
        Size = new Size(190, 45),
        BackColor = Color.DodgerBlue,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        Cursor = Cursors.Hand
    };
    btnSoloInvia.Click += BtnSoloInvia_Click;

    // Aggiunta etichette (Testo Bianco su Sfondo Scuro)
    pnlContainer.Controls.Add(new Label { 
        Text = "DA (Mio Wallet):", 
        Location = new Point(30, 70), 
        ForeColor = Color.White, AutoSize = true });
    pnlContainer.Controls.Add(_selezioneFrom);
    
    pnlContainer.Controls.Add(new Label { 
        Text = "A (Destinatario):", 
        Location = new Point(30, 140), 
        ForeColor = Color.White, AutoSize = true });
    pnlContainer.Controls.Add(_selezioneTo);
    
    pnlContainer.Controls.Add(new Label { 
        Text = "Seleziona Importo:", 
        Location = new Point(30, 210), 
        ForeColor = Color.White, AutoSize = true });
    pnlContainer.Controls.Add(_selezioneImporto);
    
     pnlContainer.Controls.Add(lblTitolo);
    pnlContainer.Controls.Add(btnSoloInvia);
    pnlContainer.Refresh();
}

private async void BtnSoloInvia_Click(object? sender, EventArgs e)
{
    // 1. Validazione iniziale (Software Robusto)
    if (_selezioneFrom?.SelectedItem == null || _selezioneTo?.SelectedItem == null || _selezioneImporto?.SelectedItem == null)
    {
        MessageBox.Show("Seleziona mittente, destinatario e importo!");
        return;
    }

    string from = _selezioneFrom.SelectedItem.ToString()!;
    string to = _selezioneTo.SelectedItem.ToString()!;
    

    int ammontare = Convert.ToInt32(_selezioneImporto.SelectedItem);

    bool successo = await _blockchainManager.InviaTransazioneAsync(from, to, ammontare);

    if (successo)
    {
        MessageBox.Show("Transazione inviata con successo!");
    }
    else
    {
        MessageBox.Show("Errore nell'invio. Verifica se hai minato abbastanza coin prima!");
    }
}

private async Task DisegnaSezioneMining()
{
    pnlContainer.Controls.Clear();
    pnlContainer.BackColor = Color.FromArgb(30, 33, 40); 

    Label lblTitolo = new Label {
        Text = "SCEGLI TRA I TUOI INDIRIZZI CHI DEVE ESSERE MINER:",
        Font = new Font("Segoe UI", 14, FontStyle.Bold),
        ForeColor = Color.White, 
        Location = new Point(30, 20),
        AutoSize = true
    };

    _selezioneFrom = new ComboBox {
        Location = new Point(30, 70),
        Size = new Size(350, 30),
        DropDownStyle = ComboBoxStyle.DropDownList,
        Font = new Font("Segoe UI", 10)
    };

    
    // Recuperiamo i tuoi wallet come oggetto WalletRoot
    WalletRoot mieiWallet = await _blockchainManager.EstraiListaWallet();
    if (mieiWallet != null && mieiWallet.Addresses != null)
    {
     List<string> mieiIndirizzi = mieiWallet.Addresses;
     _selezioneFrom.DataSource = mieiIndirizzi;
    }

   

    Button btnSoloMining = new Button {
        Text = "ESEGUI MINING",
        Location = new Point(30, 120),
        Size = new Size(190, 45),
        BackColor = Color.Orange,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        Cursor = Cursors.Hand
    };
    btnSoloMining.Click += BtnSoloMining_Click;

    pnlContainer.Controls.Add(lblTitolo);
    pnlContainer.Controls.Add(_selezioneFrom);
    pnlContainer.Controls.Add(btnSoloMining);
}
private async void BtnSoloMining_Click(object? sender, EventArgs e)
{
    // Verifica che ci sia un indirizzo selezionato
    if (_selezioneFrom?.SelectedItem == null) 
    {
        MessageBox.Show("Seleziona un indirizzo per ricevere il premio di mining.");
        return;
    }

    string indirizzo = _selezioneFrom.SelectedItem.ToString()!;

    bool successo = await _blockchainManager.EseguiMiningAsync(indirizzo);
    
    if (successo) 
    {
        MessageBox.Show($"Mining riuscito per {indirizzo}.");
    }
    else 
    {
        MessageBox.Show("Errore durante il mining. Verifica che il nodo Go sia attivo.");
    }

    // Ripristina il tasto
    if (sender is Button btnRipristina) 
    {
        btnRipristina.Enabled = true;
        btnRipristina.Text = "ESEGUI MINING";
    }
}
    
       private void TransazioniGrafiche(List<TransactionData> transazioni)
{
    pnlContainer.Controls.Clear();
    pnlContainer.AutoScroll = true;
    int coordinataY = 20;

    foreach (var tx in transazioni)
    {
        // Aumentiamo l'altezza (Height) a 250 perché ora sono uno sotto l'altro
        Panel card = new Panel {
            Size = new Size(pnlContainer.Width - 60, 250), 
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Location = new Point(20, coordinataY)
        };

        // Header: ID PER INTERO
        Label lblId = new Label {
            Text = "🆔ID-TRANSAZIONE: " + tx.id, // Rimosso Substring per stamparlo tutto
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = Color.FromArgb(242, 242, 242),
            Font = new Font("Consolas", 9, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 10, 0)
        };

        // Pannello INPUT (Sopra)
        string testoIn = "📥 INPUT (Provenienza):\n";
        if (tx.vin != null && tx.vin.Count > 0)
        {
            testoIn += $"• TxID: {tx.vin[0].txid ?? "N/D"}\n";
            testoIn += $"• Vout Index: #{tx.vin[0].vout_index}\n";
           testoIn += $"• Sorgente: {tx.vin[0].pubkey?.Substring(0, 40) ?? "N/D"}\n";
            testoIn += $"• Signature: {(string.IsNullOrEmpty(tx.vin[0].signature) ? "N/D" : "Presente")}";
        }

        Label lblInput = new Label {
            Text = testoIn,
            Location = new Point(15, 50),
            Size = new Size(card.Width - 30, 80), 
            ForeColor = Color.Firebrick,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };

        // Pannello OUTPUT (Sotto)
        string testoOut = "📤 OUTPUT (Destinazione):\n";
        if (tx.vout != null && tx.vout.Count > 0)
        {
            testoOut += $"• Destinatario: {tx.vout[0].pubkey_hash}\n";
            testoOut += $"• Importo: {tx.vout[0].value} BTC";
        }

        Label lblOutput = new Label {
            Text = testoOut,
            Location = new Point(15, 160), // Posizionato sotto l'input
            Size = new Size(card.Width - 30, 80), // Larghezza piena
            ForeColor = Color.ForestGreen,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };

        card.Controls.Add(lblOutput);
        card.Controls.Add(lblInput);
        card.Controls.Add(lblId);
        pnlContainer.Controls.Add(card);

        coordinataY += 265; // Aumentato lo spazio tra le card
    }
}

private void VisualizzaUTXOSet(int count, List<Utxo> utxoset)
{
    pnlContainer.Controls.Clear();
    pnlContainer.AutoScroll = true;
    pnlContainer.BackColor = Color.FromArgb(30, 30, 30); 

    Label lblTitolo = new Label
    {
        Text = $"DISPONIBILITÀ UTXO: {count}",
        Font = new Font("Segoe UI", 16, FontStyle.Bold),
        ForeColor = Color.FromArgb(41, 171, 226), // Blu neon
        Location = new Point(20, 15),
        AutoSize = true
    };
    pnlContainer.Controls.Add(lblTitolo);

    int coordinataY = 65;

    foreach (var utxo in utxoset)
    {
        //
        Panel utxoCard = new Panel
        {
            Size = new Size(580, 140),
            BackColor = Color.White,
            Location = new Point(20, coordinataY),
        };

    
        string IdBreve = (utxo.tx_id?.Length > 15) ? utxo.tx_id.Substring(0, 30) + "..." : utxo.tx_id ?? "N/D";
        Label lblHeader = new Label
        {
            Text = $" TX-ID: {IdBreve}",
            Font = new Font("Consolas", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(240, 240, 240),
            ForeColor = Color.DarkSlateBlue,
            Dock = DockStyle.Top,
            Height = 30,
            TextAlign = ContentAlignment.MiddleLeft
        };

        Label lblValore = new Label
        {
            Text = $"{utxo.value} BTC", 
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.ForestGreen,
            Location = new Point(15, 40),
            Size = new Size(200, 50),
        
        };

    
        Label lblInfo = new Label
        {
            Text = $"Index: #{utxo.index}\nProprietario: {utxo.pub_key_hash}",
            Font = new Font("Consolas", 9, FontStyle.Regular),
            ForeColor = Color.Gray,
            Location = new Point(18, 85),
            Size = new Size(550, 40)
        };

        
        utxoCard.Controls.Add(lblInfo);
        utxoCard.Controls.Add(lblValore);
        utxoCard.Controls.Add(lblHeader);
        pnlContainer.Controls.Add(utxoCard);

        coordinataY += 155; 
    }
}
        private void VisualizzaNuovoIndirizzo(string indirizzo)
{
    
    pnlContainer.Controls.Clear();
    pnlContainer.AutoScroll = true;

    
    Panel addressCard = new Panel
    {
        Size = new Size(400, 100),
        BackColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        Location = new Point(20, 20) 
    };

    Label lblHeader = new Label
    {
        Text = "NUOVO INDIRIZZO GENERATO",
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        ForeColor = Color.White,
        BackColor = Color.FromArgb(0, 120, 215), 
        Dock = DockStyle.Top,
        Height = 30,
        TextAlign = ContentAlignment.MiddleCenter
    };

    Label lblAddress = new Label
    {
        Text = indirizzo,
        ForeColor = Color.FromArgb(45, 45, 45),
        Font = new Font("Consolas", 11, FontStyle.Bold),
        Location = new Point(10, 45),
        Size = new Size(380, 40),
        TextAlign = ContentAlignment.MiddleCenter
    };

    addressCard.Controls.Add(lblAddress);
    addressCard.Controls.Add(lblHeader);
    
    pnlContainer.Controls.Add(addressCard);
}

    }
}