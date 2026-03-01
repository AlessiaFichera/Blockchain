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
        // 1. Orientamento ai Componenti: definiamo il gestore della logica come campo privato
        // Questo garantisce che la logica sia separata dall'interfaccia.
        private readonly BlockchainManager _blockchainManager;

        public Form1()
        {
            InitializeComponent();

            // Inizializzazione della logica
            _blockchainManager = new BlockchainManager();

            // 2. Gestione Eventi Standard: usiamo il pattern EventHandler 
            btnVisualizzaBlockchain.Click += BtnVisualizzaBlockchain_Click;
            btnAggiungiWallet.Click += BtnAggiungiWallet_Click;
            btnInviaTransazione.Click += BtnInviaTransazione_Click;
            btnAnalitiche.Click += BtnAnalitiche_Click;

            // Sottoscrizione all'evento della logica per aggiornamenti in tempo reale
            _blockchainManager.BlockAdded += BlockchainManager_BlockAdded;

            
        }

        // 3. Metodi Handler: rispettano la firma (object sender, EventArgs e)
        private void BtnVisualizzaBlockchain_Click(object? sender, EventArgs e)
        {
            EntraInModalitaDettaglio("BLOCKCHAIN");
            string nomeFile = "blockchain.json";

    if (File.Exists(nomeFile))
    {
        try 
        {
            // Leggiamo il file esterno
            string contenutoJson = File.ReadAllText(nomeFile);
            
            // Passiamo i dati alla logica aggiornata
            _blockchainManager.RiceviBloccoDaGo(contenutoJson);

            // Carichiamo i blocchi grafici (che ora iterano sulla catena aggiornata)
            CaricaBlocchiGrafici();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore di runtime: {ex.Message}");
        }
    }
    else
    {
        MessageBox.Show("Il file blockchain.json non è stato trovato.");
    }
}
private void BtnAnalitiche_Click(object? sender, EventArgs e)
{
    _blockchainManager.EseguiAggiornamentoPython();

    string nomeFile = "analitiche.json"; 

    if (File.Exists(nomeFile))
    {
        // Leggiamo tutto il testo del file JSON
        string contenutoJson = File.ReadAllText(nomeFile);
        
        // Entriamo nella modalità grafica
        EntraInModalitaDettaglio("ANALITICHE");
        
        // Chiamiamo la funzione di stampa passandogli i dati veri
        VisualizzaStatistiche(contenutoJson);
    }
    else
    {
        MessageBox.Show("File analitiche.json non trovato!");
    }
}

        private void BtnAggiungiWallet_Click(object? sender, EventArgs e)
        {
            EntraInModalitaDettaglio("WALLET");
            string nomeFile = "wallet.json";

    if (File.Exists(nomeFile))
    {
        try 
        {
            // Leggiamo il file esterno
            string contenutoJson = File.ReadAllText(nomeFile);
            
        

            // Carichiamo i blocchi grafici (che ora iterano sulla catena aggiornata)
            CaricaWalletGrafici(contenutoJson);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore di runtime: {ex.Message}");
        }
    }
    else
    {
        MessageBox.Show("Il file wallet.json non è stato trovato.");
    }
        }

        private void BtnInviaTransazione_Click(object? sender, EventArgs e)
        {
            EntraInModalitaDettaglio("TRANSAZIONE");
             string nomeFile = "transazioni.json";

    if (File.Exists(nomeFile))
    {
        try 
        {
            // Leggiamo il file esterno
            string contenutoJson = File.ReadAllText(nomeFile);
            
        

            // Carichiamo i blocchi grafici (che ora iterano sulla catena aggiornata)
            CaricaTransazioniGrafiche(contenutoJson);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore di runtime: {ex.Message}");
        }
    }
    else
    {
        MessageBox.Show("Il file wallet.json non è stato trovato.");
    }
        }
        private void BlockchainManager_BlockAdded(object? sender, BlockAddedEventArgs e)
        {
            // Notifica all'utente che un nuovo oggetto (blocco) è stato creato,testato + avanti
            MessageBox.Show($"Nuovo blocco aggiunto: {e.NewBlock.Index}", "Blockchain Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EntraInModalitaDettaglio(string modalita)
{
    
    btnAggiungiWallet.Dock = DockStyle.Top;
    btnInviaTransazione.Dock = DockStyle.Top;
    btnVisualizzaBlockchain.Dock = DockStyle.Top;
    btnAnalitiche.Dock = DockStyle.Top;

    pnlDettaglio.Controls.Add(btnAggiungiWallet);
    pnlDettaglio.Controls.Add(btnInviaTransazione);
    pnlDettaglio.Controls.Add(btnVisualizzaBlockchain);
    pnlDettaglio.Controls.Add(btnAnalitiche);

    
    pnlDettaglio.Visible = true;
    pnlHeaderDettaglio.Visible = true;

    
    lblTitle.Visible = true;
    lblTitle.Parent = pnlHeaderDettaglio;
    lblTitle.Location = new Point(250, 10);
    lblTitle.BringToFront();

    // Carichiamo i dati specifici in base alla modalità selezionata
    if (modalita == "BLOCKCHAIN")
    {
        CaricaBlocchiGrafici();
    }
}
        private void CaricaBlocchiGrafici()
        {
            pnlContainer.Controls.Clear();
            pnlContainer.AutoScroll = true; 

            int coordinataX = 20; 

            // 4. Type Safety: Iteriamo sulla collezione generica della logica
            var catena = _blockchainManager.Chain;

            foreach (var bloccoDati in catena)
            {
                // Creiamo l'istanza grafica basandoci sui metadati dell'oggetto blocco
                Panel bloccoGrafico = CreaSingoloBlocco(
                    bloccoDati.Index.ToString(), 
                    bloccoDati.Transactions,
                    bloccoDati.Hash,
                    bloccoDati.Nonce.ToString(),
                    true
                );
                
                bloccoGrafico.Location = new Point(coordinataX, 100);
                pnlContainer.Controls.Add(bloccoGrafico);

                // Aggiungiamo la freccia se non è l'ultimo elemento
                if (bloccoDati.Index < catena.Count - 1)
                {
                    Label freccia = new Label
                    {
                        Text = "➔",
                        ForeColor = Color.FromArgb(41, 171, 226),
                        Font = new Font("Segoe UI", 20, FontStyle.Bold),
                        Location = new Point(coordinataX + 215, 170),
                        AutoSize = true
                    };
                    pnlContainer.Controls.Add(freccia);
                }

                coordinataX += 280;
            }
        }

        private Panel CreaSingoloBlocco(string id, List<TransactionData>? transactions, string hash,string nonce, bool isValid)
        {
            Panel card = new Panel
            {
                Size = new Size(220, 300),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblHeader = new Label
            {
                Text = "🔒 BLOCK #" + id,
                BackColor = Color.FromArgb(41, 171, 226),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            string elencoTransazioni = "";
            if (transactions != null && transactions.Count > 0)
    {
        foreach (var tx in transactions)
        {
            // Prendiamo i primi 10 caratteri dell'ID per ogni transazione
            string idBreve = (tx.ID?.Length > 10) ? tx.ID.Substring(0, 10) : tx.ID ?? "N/D";
            elencoTransazioni += $"• TX: {idBreve}...\n";
        }
    }
    else
    {
        elencoTransazioni = "Nessuna transazione";
    }
            Label lblData = new Label
            {
               Text = elencoTransazioni,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Location = new Point(10, 100), // Posizione Y
                Size = new Size(200, 100),
                TextAlign = ContentAlignment.TopCenter,
                Font = new Font("Segoe UI", 9),
                AutoSize = false //
            };

            Label lblHash = new Label
            {
                Text = hash.Length > 15 ? hash.Substring(0, 15) + "..." : hash,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Location = new Point(10, 60),
                Size = new Size(200, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblNonce = new Label
            {
                Text = "Nonce:" + nonce,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Location = new Point(10, 200),
                Size = new Size(200, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblStatus = new Label
            {
                Text = isValid ? "Stato: VALIDO" : "Stato: CORROTTO!",
                ForeColor = isValid ? Color.Green : Color.Red,
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 30
            };

           
            card.Controls.Add(lblHeader);
            card.Controls.Add(lblData);
            card.Controls.Add(lblHash);
            card.Controls.Add(lblNonce);
            card.Controls.Add(lblStatus);

            return card;
        }  
        private void VisualizzaStatistiche(string jsonContenuto)
{
    pnlContainer.Controls.Clear();
    var stats = _blockchainManager.EstraiAnalitiche(jsonContenuto);
    int coordinataX = 20;
    pnlContainer.AutoScroll = true; 

    // Disegno delle Card (Componenti)
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

    // Verifica se il file esiste per garantire un software robusto (previene crash a runtime)
    if (File.Exists(percorsoImmagine))
    {
        PictureBox pic = new PictureBox
        {
            // Carica l'immagine dal file generato da Python
            Image = Image.FromFile(percorsoImmagine),
            Location = new Point(20, 180), // Posizionato sotto le card
            Size = new Size(700, 350),     // Dimensione grande per il grafico
            SizeMode = PictureBoxSizeMode.Zoom, // Adatta l'immagine senza sgranare
            BorderStyle = BorderStyle.None
        };
        
        pnlContainer.Controls.Add(pic);
    }
    else
    {
        
        Console.WriteLine("Immagine del grafico non trovata.");
    }
}
        private void CaricaWalletGrafici(string jsonContenuto)
        {
            pnlContainer.Controls.Clear();
            pnlContainer.AutoScroll = true; 

            int coordinataY = 20; 

            var walletList = _blockchainManager.EstraiListaWallet(jsonContenuto);

            foreach (var wallet in walletList)
            {
                Panel walletCard = new Panel
                {
                    Size = new Size(300, 80),
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    Location = new Point(20, coordinataY)
                };

                Label lblAddress = new Label
                {
                    Text = "Indirizzo: " + wallet.Address,
                    ForeColor = Color.FromArgb(45, 45, 45),
                    Location = new Point(10, 10),
                    Size = new Size(280, 25),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9)
                };

                Label lblBalance = new Label
                {
                    Text = "Saldo: " + wallet.Balance.ToString("F2") + " BTC",
                    ForeColor = Color.FromArgb(45, 45, 45),
                    Location = new Point(10, 40),
                    Size = new Size(280, 25),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9)
                };

                walletCard.Controls.Add(lblAddress);
                walletCard.Controls.Add(lblBalance);
                pnlContainer.Controls.Add(walletCard);

                coordinataY += 100; 
            }
        }
        private void CaricaTransazioniGrafiche(string jsonContenuto)
{
    pnlContainer.Controls.Clear();
    var listaTx = _blockchainManager.EstraiListaTransazioni(jsonContenuto);
    int coordinataY = 20;

    foreach (var tx in listaTx)
    {
        Panel txCard = new Panel
        {
            Size = new Size(400, 90),
            BackColor = Color.FromArgb(245, 245, 245),
            BorderStyle = BorderStyle.FixedSingle,
            Location = new Point(20, coordinataY)
        };

        Label lblId = new Label
        {
            Text = "🆔 TX: " + (tx.ID?.Substring(0, 15) ?? "N/D") + "...",
            Font = new Font("Consolas", 9, FontStyle.Bold),
            Location = new Point(10, 10),
            Size = new Size(380, 20)
        };

        // Mostriamo il primo output come esempio di destinazione
        var primoOut = tx.Outputs?[0];
        Label lblDettaglio = new Label
        {
            Text = $"Inviati {primoOut?.Value} BTC a {primoOut?.PubKeyHash?.Substring(0, 10)}...",
            Location = new Point(10, 40),
            Size = new Size(380, 40)
        };

        txCard.Controls.Add(lblId);
        txCard.Controls.Add(lblDettaglio);
        pnlContainer.Controls.Add(txCard);
        coordinataY += 100;
    }
}
       
    }
}