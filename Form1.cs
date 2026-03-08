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
    // Eseguiamo prima la parte grafica (immediata)
    DisegnaInterfacciaInvio();
    
    // Poi carichiamo i dati in modo asincrono (Software Durevole)
    await CaricaDatiPerInvio();
}
private async Task CaricaDatiPerInvio()
{
    try 
    {
        // 1. Sincronizziamo la rubrica di tutti i nodi (8080-8083)
        await _blockchainManager.SincronizzaRubricaGlobaleAsync();
        
        // 2. Recuperiamo i TUOI indirizzi locali del nodo corrente
        var (mieiIndirizzi, _) = await _blockchainManager.EstraiListaWallet();

        // 3. Filtriamo: nel menu 'TO' mostriamo solo indirizzi NON locali
        var destinatariEsterni = _blockchainManager.RubricaIndirizziRete
            .Where(addr => !mieiIndirizzi.Contains(addr))
            .ToList();

        // 4. Alimentiamo i componenti (Type Safety)
        if (_cmbFrom != null) _cmbFrom.DataSource = mieiIndirizzi;
        if (_cmbTo != null) _cmbTo.DataSource = destinatariEsterni;
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Errore nel recupero dati: {ex.Message}");
    }
}

        private async void BtnVisualizzaBlockchain_Click(object? sender, EventArgs e)
        {
             PreparaAreaLavoro("BLOCKCHAIN");

    try
    {
        // Chiamata al metodo sincronizza
       List<Blocks> blocchiRicevuti = await _blockchainManager.SincronizzaBlockchain();

        // Aggiornamento della grafica con i nuovi blocchi
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

       private async void BtnAggiungiWallet_Click(object? sender, EventArgs e)
{
     PreparaAreaLavoro("WALLET");

    try
    {
        // Riceviamo la tupla (Lista e Conteggio) dal manager
        var (listaWallet, totale) = await _blockchainManager.EstraiListaWallet();


        // Passiamo la lista al metodo che gestisce la parte grafica
        CaricaWalletGrafici(listaWallet, totale);
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
                 var (count, utxoSet)  = await _blockchainManager.EstraiUTXOSet();
                VisualizzaUTXOSet(count, utxoSet);
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

    AggiungiBottoneAlMenu(btncreaindirizzo);
    AggiungiBottoneAlMenu(btnUTXOSet);
    AggiungiBottoneAlMenu(btnAnalitiche);
    AggiungiBottoneAlMenu(btnVisualizzaBlockchain);
    AggiungiBottoneAlMenu(btnVisualizzaTransazione);
    AggiungiBottoneAlMenu(btnInviaTransazione);
    AggiungiBottoneAlMenu(btnAggiungiWallet);
    
    StilizzaBottoneNav(btnHome, "🏠 HOME", Color.FromArgb(45, 45, 48));
    btnHome.Click += BtnHome_Click;
    AggiungiBottoneAlMenu(btnHome);
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
   
       
// Metodo per visualizzare il nuovo indirizzo creato
        private void CaricaBlocchiGrafici(List<Blocks> catena)
{
    if (catena == null || catena.Count == 0) return;

    pnlContainer.Controls.Clear();
    pnlContainer.AutoScroll = true;

    // Ordinamento senza =>
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
{
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
            int coordinataX = 20;
            pnlContainer.AutoScroll = true;

            foreach (var s in stats)
            {
                Panel card = new Panel
                {
                    Size = new Size(170, 100),
                    Location = new Point(coordinataX, 50),
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle
                };

                Label lbl = new Label
                {
                    Text = $"{s.Titolo}\n{s.Valore}",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };

                card.Controls.Add(lbl);
                pnlContainer.Controls.Add(card);
                coordinataX += 190;
            }

            string percorsoImmagine = "grafico_blockchain.png";
            if (File.Exists(percorsoImmagine))
            {
                PictureBox pic = new PictureBox
                {
                    Image = Image.FromFile(percorsoImmagine),
                    Location = new Point(20, 180),
                    Size = new Size(700, 350),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.None
                };
                pnlContainer.Controls.Add(pic);
            }
        }

    private void CaricaWalletGrafici(List<string> walletList, int count)
{
    pnlContainer.Controls.Clear();
    pnlContainer.AutoScroll = true;
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

        walletCard.Controls.Add(lblTag);
        walletCard.Controls.Add(lblAddress);
        pnlContainer.Controls.Add(walletCard);

        coordinataY += 95; 
    }
}
 private ComboBox? _cmbFrom;
private ComboBox? _cmbTo;
private ComboBox? _selezioneImporto;
private void DisegnaInterfacciaInvio()
{
    pnlContainer.Controls.Clear();

    _cmbFrom = new ComboBox { 
        Location = new Point(30, 60), 
        Size = new Size(400, 30), 
        DropDownStyle = ComboBoxStyle.DropDownList, 
        BackColor = Color.White };

    _cmbTo = new ComboBox { 
        Location = new Point(30, 130),
        Size = new Size(400, 30), 
        DropDownStyle = ComboBoxStyle.DropDownList, 
        BackColor = Color.White };
    
    // Menu a tendina per l'importo (da 1 a 10)
    _selezioneImporto = new ComboBox { 
        Location = new Point(30, 200), 
        Size = new Size(150, 30), 
        DropDownStyle = ComboBoxStyle.DropDownList, 
        BackColor = Color.White 
    };
    for (int i = 1; i <= 10; i++) _selezioneImporto.Items.Add(i);
    _selezioneImporto.SelectedIndex = 0;

    // 2. BOTTONE 1: SOLO MINING (Per caricare i soldi)
    Button btnSoloMining = new Button {
        Text = "1. ESEGUI MINING",
        Location = new Point(30, 250),
        Size = new Size(190, 45),
        BackColor = Color.Orange,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        Cursor = Cursors.Hand
    };
    btnSoloMining.Click += BtnSoloMining_Click;

    // 3. BOTTONE 2: SOLO INVIA (Per spedire i soldi minati)
    Button btnSoloInvia = new Button {
        Text = "2. INVIA TRANSAZIONE",
        Location = new Point(240, 250),
        Size = new Size(190, 45),
        BackColor = Color.DodgerBlue,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        Cursor = Cursors.Hand
    };
    btnSoloInvia.Click += BtnSoloInvia_Click;

    // 4. Aggiunta etichette e controlli al pannello
    pnlContainer.Controls.Add(new Label { 
        Text = "DA (Mio Wallet):", 
        Location = new Point(30, 40), 
        ForeColor = Color.White, AutoSize = true });
    pnlContainer.Controls.Add(_cmbFrom);
    
    pnlContainer.Controls.Add(new Label { 
        Text = "A (Destinatario):", 
        Location = new Point(30, 110), 
        ForeColor = Color.White, AutoSize = true });
    pnlContainer.Controls.Add(_cmbTo);
    
    pnlContainer.Controls.Add(new Label { 
        Text = "QUANTITÀ (Coin):", 
        Location = new Point(30, 180), 
        ForeColor = Color.White, AutoSize = true });
    pnlContainer.Controls.Add(_selezioneImporto);
    
    pnlContainer.Controls.Add(btnSoloMining);
    pnlContainer.Controls.Add(btnSoloInvia);
}

private async void BtnSoloMining_Click(object? sender, EventArgs e)
{
    if (_cmbFrom?.SelectedItem == null) return;
    string indirizzo = _cmbFrom.SelectedItem.ToString()!;

    bool successo = await _blockchainManager.EseguiMiningAsync(indirizzo);
    
    if (successo) 
        MessageBox.Show($"Mining riuscito per {indirizzo}. Ora hai 10 coin in più!");
    else 
        MessageBox.Show("Errore nel mining. Controlla il terminale del nodo Go.");
}


private async void BtnSoloInvia_Click(object? sender, EventArgs e)
{
    // 1. Validazione iniziale (Software Robusto)
    if (_cmbFrom?.SelectedItem == null || _cmbTo?.SelectedItem == null || _selezioneImporto?.SelectedItem == null)
    {
        MessageBox.Show("Seleziona mittente, destinatario e importo!");
        return;
    }

    string from = _cmbFrom.SelectedItem.ToString()!;
    string to = _cmbTo.SelectedItem.ToString()!;
    

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