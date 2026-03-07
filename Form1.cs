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

            // 2. Gestione Eventi Standard
            btnVisualizzaBlockchain.Click += BtnVisualizzaBlockchain_Click;
            btnAggiungiWallet.Click += BtnAggiungiWallet_Click;
            btnInviaTransazione.Click += BtnInviaTransazione_Click;
            btnAnalitiche.Click += BtnAnalitiche_Click;
            btnUTXOSet.Click += BtnUTXOSet_Click;
            btncreaindirizzo.Click += btncreaindirizzo_Click;
        }

        // --- HANDLER EVENTI ---

        private async void BtnVisualizzaBlockchain_Click(object? sender, EventArgs e)
        {
            EntraInModalitaDettaglio("BLOCKCHAIN");

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
                EntraInModalitaDettaglio("ANALITICHE");
                VisualizzaStatistiche(contenutoJson);
            }
            else
            {
                MessageBox.Show("File analitiche.json non trovato!");
            }
        }

       private async void BtnAggiungiWallet_Click(object? sender, EventArgs e)
{
    EntraInModalitaDettaglio("WALLET");

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
        


        private void BtnInviaTransazione_Click(object? sender, EventArgs e)
        {
            EntraInModalitaDettaglio("TRANSAZIONE");
            string nomeFile = "transazioni.json";

            if (File.Exists(nomeFile))
            {
                try
                {
                    string contenutoJson = File.ReadAllText(nomeFile);
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

        private async void BtnUTXOSet_Click(object? sender, EventArgs e)
        {
            _blockchainManager.EseguiAggiornamentoPython();
             EntraInModalitaDettaglio("UTXOSET");
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
     EntraInModalitaDettaglio("CREAINDIRIZZO");

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

        private void EntraInModalitaDettaglio(string modalita)
        {
            btnAggiungiWallet.Dock = DockStyle.Top;
            btnInviaTransazione.Dock = DockStyle.Top;
            btnVisualizzaBlockchain.Dock = DockStyle.Top;
            btnAnalitiche.Dock = DockStyle.Top;
            btnUTXOSet.Dock = DockStyle.Top;
            btncreaindirizzo.Dock = DockStyle.Top;

            pnlDettaglio.Controls.Add(btnAggiungiWallet);
            pnlDettaglio.Controls.Add(btnInviaTransazione);
            pnlDettaglio.Controls.Add(btnVisualizzaBlockchain);
            pnlDettaglio.Controls.Add(btnAnalitiche);
            pnlDettaglio.Controls.Add(btnUTXOSet);
            pnlDettaglio.Controls.Add(btncreaindirizzo);

            pnlDettaglio.Visible = true;
            pnlHeaderDettaglio.Visible = true;

            lblTitle.Visible = true;
            lblTitle.Parent = pnlHeaderDettaglio;
            lblTitle.Location = new Point(250, 10);
            lblTitle.BringToFront();
        }

        private void CaricaBlocchiGrafici(List<Blocks> catena)
        {
            pnlContainer.Controls.Clear();
            pnlContainer.AutoScroll = true;
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

                bloccoGrafico.Location = new Point(coordinataX, 100);
                pnlContainer.Controls.Add(bloccoGrafico);

                if (bloccoDati.Height < catena.Count - 1)
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

    string elencoTransazioni = "";
    if (transactions != null && transactions.Count > 0)
    {
        foreach (var tx in transactions)
        {
            string idBreve = (tx.ID?.Length > 20) ? tx.ID.Substring(0, 20) : tx.ID ?? "Errore";
            elencoTransazioni += $"• {idBreve}..\n";
        }
    }
    else { elencoTransazioni = "Nessuna"; }

    Label lblData = new Label
    {
        Text = elencoTransazioni,
        BackColor = Color.FromArgb(240, 240, 240), 
        ForeColor = Color.Black,
        Location = new Point(10, 120),
        Size = new Size(200, 100), 
        TextAlign = ContentAlignment.TopLeft,
        Font = new Font("Consolas", 8) 
    };

    
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
    card.Controls.Add(lblData);
    card.Controls.Add(lblNonce);

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
            Size = new Size(walletCard.Width - 200, 30), // Ridotto per far spazio al bottone
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Componente Bottone per l'invio (Orientamento ai Componenti)
        Button btnInvia = new Button
        {
            Text = "INVIA",
            Size = new Size(120, 40),
            Location = new Point(walletCard.Width - 140, 20),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Tag = wallet // Utilizziamo il Tag per trasportare l'indirizzo
        };

        // Sottoscrizione all'evento 
        btnInvia.Click += (s, e) => {
             if (s is Button b && b.Tag != null)
             {
                 MostraSchermataDettaglioInvio(b.Tag.ToString()!);
             }
        };

        walletCard.Controls.Add(lblTag);
        walletCard.Controls.Add(lblAddress);
        walletCard.Controls.Add(btnInvia);
        pnlContainer.Controls.Add(walletCard);

        coordinataY += 95; 
    }
}
// Componente di classe per l'ammontare (Orientamento ai Componenti)
private ComboBox? _selezioneImporto;
private void MostraSchermataDettaglioInvio(string destinatario)
{
    pnlContainer.Controls.Clear();

    Label lblDest = new Label {
        Text = $"INVIO A: {destinatario}",
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 12, FontStyle.Bold),
        Location = new Point(30, 30),
        AutoSize = true
    };

    // Inizializzazione del componente di classe
    _selezioneImporto = new ComboBox {
        Location = new Point(30, 80),
        Size = new Size(150, 30),
        DropDownStyle = ComboBoxStyle.DropDownList,
        BackColor = Color.White
    };
    for (int i = 1; i <= 10; i++) _selezioneImporto.Items.Add(i);
    _selezioneImporto.SelectedIndex = 0;

    Button btnConferma = new Button {
        Text = "CONFERMA TRANSAZIONE",
        Location = new Point(30, 130),
        Size = new Size(200, 45),
        BackColor = Color.SeaGreen,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Tag = destinatario // Trasporto dati tramite oggetto Tag
    };

    // Sottoscrizione formale all'evento (Concetto di prima classe)
    btnConferma.Click += BtnConferma_Click;

    pnlContainer.Controls.Add(lblDest);
    pnlContainer.Controls.Add(_selezioneImporto);
    pnlContainer.Controls.Add(btnConferma);
}
private async void BtnConferma_Click(object? sender, EventArgs e)
{
    // Validazione per evitare dereferenziazione nulla (Software Robusto)
    if (_selezioneImporto?.SelectedItem == null) return;

    if (sender is Button btn && btn.Tag != null)
    {
        // Recupero sicuro dei dati dal componente
        string destinatario = btn.Tag.ToString()!;
        int ammontare = (int)_selezioneImporto.SelectedItem;
        string mittente = "1FJxTcQfU7U5MGwz31LGZwqafgWAtqi2kj";

        try 
        {
            // Chiamata asincrona al manager per preservare l'investimento
            bool esito = await _blockchainManager.InviaTransazioneAsync(mittente, destinatario, ammontare);
            
            if (esito) 
                MessageBox.Show("Transazione inviata al nodo Go!", "Blockchain Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else 
                MessageBox.Show("Errore nell'invio. Verifica il server Go.", "Errore Runtime", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            // Gestione errori per un software durevole
            MessageBox.Show($"Eccezione in fase di esecuzione: {ex.Message}");
        }
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
                                  $"• PubKey:    {primoIn.pub_key_hash}";
                }
                else
                {
                    testoInput += "• Transazione Coinbase (nessun input)";
                }

                Label lblInput = new Label
                {
                    Text = testoInput,
                    Font = new Font("Consolas", 9, FontStyle.Bold),
                    Location = new Point(10, 40),
                    Size = new Size(500, 100),
                    ForeColor = Color.Firebrick
                };

                var primoOut = tx.Outputs?.Count > 0 ? tx.Outputs[0] : null;
                Label lblOutput = new Label
                {
                    Text = $"OUTPUT:\n" +
                           $"• A: {primoOut?.pub_key_hash ?? "N/D"}\n" +
                           $"• Valore: {primoOut?.value} BTC",
                    Font = new Font("Consolas", 9, FontStyle.Bold),
                    Location = new Point(15, 140),
                    Size = new Size(500, 135),
                    ForeColor = Color.ForestGreen
                };

                txCard.Controls.Add(lblId);
                txCard.Controls.Add(lblInput);
                txCard.Controls.Add(lblOutput);
                pnlContainer.Controls.Add(txCard);
                coordinataY += 220;
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