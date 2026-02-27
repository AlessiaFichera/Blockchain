using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using Blockchain.Core; 



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

            // Sottoscrizione all'evento della logica per aggiornamenti in tempo reale
            _blockchainManager.BlockAdded += BlockchainManager_BlockAdded;

            
        }

        // 3. Metodi Handler: rispettano la firma (object sender, EventArgs e)
        private void BtnVisualizzaBlockchain_Click(object? sender, EventArgs e)
        {
            EntraInModalitaDettaglio("BLOCKCHAIN");
            // Simuliamo la ricezione di un blocco da Go per dimostrare l'integrazione
            SimulaRicezioneDaGo();
           
        }

        private void BtnAggiungiWallet_Click(object? sender, EventArgs e)
        {
            EntraInModalitaDettaglio("WALLET");
        }

        private void BtnInviaTransazione_Click(object? sender, EventArgs e)
        {
            EntraInModalitaDettaglio("TRANSAZIONE");
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

    pnlDettaglio.Controls.Add(btnAggiungiWallet);
    pnlDettaglio.Controls.Add(btnInviaTransazione);
    pnlDettaglio.Controls.Add(btnVisualizzaBlockchain);

    
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
                    bloccoDati.Hash, 
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

        private Panel CreaSingoloBlocco(string id, string hash, bool isValid)
        {
            Panel card = new Panel
            {
                Size = new Size(210, 200),
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

            Label lblHash = new Label
            {
                Text = hash.Length > 15 ? hash.Substring(0, 15) + "..." : hash,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Location = new Point(10, 60),
                Size = new Size(190, 25),
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

            card.Controls.Add(lblHash);
            card.Controls.Add(lblHeader);
            card.Controls.Add(lblStatus);

            return card;
        }    
        private void SimulaRicezioneDaGo()
{
    // Stringa JSON che rispetta la struttura della classe Block (Type Safety)
    string jsonFinto = "{\"Index\": 1, \"Timestamp\": 1715673600, \"Data\": \"Transazione simulata da Go\", \"PreviousHash\": \"GENESIS_HASH\", \"Hash\": \"ABC123_HASH_RICEVUTO\"}";

    // Chiamata al metodo che gestisce la logica (Orientamento ai componenti)
    _blockchainManager.RiceviBloccoDaGo(jsonFinto);

    // Aggiornamento della grafica dopo la ricezione
    CaricaBlocchiGrafici();
}   
    }
}