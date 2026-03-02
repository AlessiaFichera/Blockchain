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
            btnUTXOSet.Click += BtnUTXOSet_Click;

            

            
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
        MessageBox.Show("Il file transazioni.json non è stato trovato.");
    }
        }
    private void BtnUTXOSet_Click(object? sender, EventArgs e)
{
    _blockchainManager.EseguiAggiornamentoPython();

    string nomeFile = "utxoset.json"; 

    if (File.Exists(nomeFile))
    {
        // Leggiamo tutto il testo del file JSON
        string contenutoJson = File.ReadAllText(nomeFile);
        
        // Entriamo nella modalità grafica
        EntraInModalitaDettaglio("UTXOSET");
        
        // Chiamiamo la funzione di stampa passandogli i dati veri
        VisualizzaUTXOSet(contenutoJson);
    }
    else
    {
        MessageBox.Show("File utxoset.json non trovato!");
    }
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
                    bloccoDati.Height,
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

        private Panel CreaSingoloBlocco(string id, List<TransactionData>? transactions, string hash,string nonce,int height, bool isValid)
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
            Label lblHeight = new Label
            {
                Text = "Height:" + height,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Location = new Point(10, 230),
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
            card.Controls.Add(lblHeight);
            card.Controls.Add(lblStatus);

            return card;
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

                walletCard.Controls.Add(lblAddress);
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
            Size = new Size(600, 220),
            BackColor = Color.WhiteSmoke,
            BorderStyle = BorderStyle.FixedSingle,
            Location = new Point(20, coordinataY)
        };

        Label lblId = new Label
        {
            Text = "TRANSACTION ID: " + (tx.ID ?? "N/D"),
            Font = new Font("Consolas", 9, FontStyle.Bold),
            Location = new Point(10, 10),
            Size = new Size(430, 20),
            ForeColor = Color.MidnightBlue
        };

        var primoIn = tx.Inputs?.Count > 0 ? tx.Inputs[0] : null;
        string testoInput = " INPUT:\n";
        
        if (primoIn != null)
        {
           testoInput += $"• Prev TxID: {primoIn.TxID}\n" +
                          $"• Index:     {primoIn.OutputIndex}\n" +
                          $"• Signature: {primoIn.Signature}\n" +
                          $"• PubKey:    {primoIn.PubKey}";
        }
        else
        {
            testoInput += "• Transazione Coinbase (Generazione nuovi fondi)";
        }

        Label lblInput = new Label
        {
            Text = testoInput,
            Font = new Font("Consolas", 9, FontStyle.Bold),
            Location = new Point(10, 40),
            Size = new Size(500, 100),
            ForeColor = Color.Firebrick // Rosso per identificare l'origine dei fondi
        };

        // 3. Sezione OUTPUT (Destinatario)
        var primoOut = tx.Outputs?.Count > 0 ? tx.Outputs[0] : null;
        Label lblOutput = new Label
        {
            Text = $"OUTPUT:\n" +
                   $"• A: {primoOut?.PubKeyHash ?? "N/D"}\n" +
                   $"• Valore: {primoOut?.Value} BTC",
            Font = new Font("Consolas", 9, FontStyle.Bold),
            Location = new Point(15, 140),
            Size = new Size(500, 135),
            ForeColor = Color.ForestGreen
        };

        // Aggiunta di tutti i componenti alla card
        txCard.Controls.Add(lblId);
        txCard.Controls.Add(lblInput);
        txCard.Controls.Add(lblOutput);

        // Aggiunta della card al contenitore principale
        pnlContainer.Controls.Add(txCard);
        
        // Incremento coordinata per la card successiva (con spazio di 20px)
        coordinataY += 220;
    }
}

   private void VisualizzaUTXOSet(string jsonContenuto)
{
    pnlContainer.Controls.Clear();
    var utxoset = _blockchainManager.EstraiUTXOSet(jsonContenuto);
    int coordinataY = 20;
      pnlContainer.AutoScroll = true; 

    foreach (var utxo in utxoset)
    {
        // 1. Creazione della Card per l'UTXO
        Panel utxoCard = new Panel
        {
            Size = new Size(600, 160),
            BackColor = Color.GhostWhite,
            BorderStyle = BorderStyle.FixedSingle,
            Location = new Point(20, coordinataY)
        };

        // 2. Intestazione: Riferimento alla Transazione Originale
        Label lblHeader = new Label
        {
            Text = $"OUTPUT NON SPESO DALLA TX: {utxo.TxID ?? "N/D"}",
            Font = new Font("Consolas", 9, FontStyle.Bold),
            Location = new Point(10, 10),
            Size = new Size(580, 20),
            ForeColor = Color.DarkSlateBlue
        };

        // 3. Dettagli dell'UTXO (Indice e provenienza)
        Label lblDettagli = new Label
        {
            Text = $"• Output Index: {utxo.Index}\n",
            Font = new Font("Consolas", 9, FontStyle.Regular),
            Location = new Point(10, 50),
            Size = new Size(500, 40),
            ForeColor = Color.Black
        };

        // 4. Sezione Valore e Destinatario (PubKeyHash)
        // Prendiamo il primo output della lista per visualizzare il valore disponibile
        var primoOut = utxo.Outputs?.Count > 0 ? utxo.Outputs[0] : null;
        Label lblValore = new Label
        {
            Text = $"DETTAGLI BILANCIO:\n" +
                   $"• Indirizzo: {primoOut?.PubKeyHash ?? "N/D"}\n" +
                   $"• Importo Disponibile: {primoOut?.Value} BTC",
            Font = new Font("Consolas", 10, FontStyle.Bold),
            Location = new Point(15, 90),
            Size = new Size(500, 60),
            ForeColor = Color.DarkGreen // Verde per indicare fondi disponibili
        };

        // Aggiunta dei componenti alla card
        utxoCard.Controls.Add(lblHeader);
        utxoCard.Controls.Add(lblDettagli);
        utxoCard.Controls.Add(lblValore);

        // Aggiunta della card al contenitore
        pnlContainer.Controls.Add(utxoCard);

        // Incremento coordinata (altezza card 160 + 20px di margine)
        coordinataY += 180;
    }
}
       
    }
}