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
        }

        // --- HANDLER EVENTI ---

        private void BtnVisualizzaBlockchain_Click(object? sender, EventArgs e)
        {
            EntraInModalitaDettaglio("BLOCKCHAIN");
            string nomeFile = "blockchain.json";

            if (File.Exists(nomeFile))
            {
                try
                {
                    string contenutoJson = File.ReadAllText(nomeFile);
                    _blockchainManager.RiceviBloccoDaGo(contenutoJson);
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
                string contenutoJson = File.ReadAllText(nomeFile);
                EntraInModalitaDettaglio("ANALITICHE");
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
                    string contenutoJson = File.ReadAllText(nomeFile);
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

        private void BtnUTXOSet_Click(object? sender, EventArgs e)
        {
            _blockchainManager.EseguiAggiornamentoPython();
            string nomeFile = "utxoset.json";

            if (File.Exists(nomeFile))
            {
                string contenutoJson = File.ReadAllText(nomeFile);
                EntraInModalitaDettaglio("UTXOSET");
                VisualizzaUTXOSet(contenutoJson);
            }
            else
            {
                MessageBox.Show("File utxoset.json non trovato!");
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

            pnlDettaglio.Controls.Add(btnAggiungiWallet);
            pnlDettaglio.Controls.Add(btnInviaTransazione);
            pnlDettaglio.Controls.Add(btnVisualizzaBlockchain);
            pnlDettaglio.Controls.Add(btnAnalitiche);
            pnlDettaglio.Controls.Add(btnUTXOSet);

            pnlDettaglio.Visible = true;
            pnlHeaderDettaglio.Visible = true;

            lblTitle.Visible = true;
            lblTitle.Parent = pnlHeaderDettaglio;
            lblTitle.Location = new Point(250, 10);
            lblTitle.BringToFront();
        }

        private void CaricaBlocchiGrafici()
        {
            pnlContainer.Controls.Clear();
            pnlContainer.AutoScroll = true;
            int coordinataX = 20;

            var catena = _blockchainManager.Chain;

            foreach (var bloccoDati in catena)
            {
                Panel bloccoGrafico = CreaSingoloBlocco(
                    bloccoDati.Index.ToString(),
                    bloccoDati.Timestamp,
                    bloccoDati.Transactions,
                    bloccoDati.Hash,
                    bloccoDati.Nonce.ToString(),
                    bloccoDati.Height
                );

                bloccoGrafico.Location = new Point(coordinataX, 100);
                pnlContainer.Controls.Add(bloccoGrafico);

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

        private Panel CreaSingoloBlocco(string id, long timestamp, List<TransactionData>? transactions, string hash,string nonce, int height)
        {
            Panel card = new Panel
            {
                Size = new Size(220, 270),
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

            Label lblTimestamp = new Label
            {
                Text = "Timestamp: " + timestamp,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Location = new Point(10, 40),
                Size = new Size(200, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8)
            };

            string elencoTransazioni = "";
            if (transactions != null && transactions.Count > 0)
            {
                foreach (var tx in transactions)
                {
                    string idBreve = (tx.ID?.Length > 10) 
                        ? tx.ID.Substring(0, 10) 
                        : tx.ID ?? "Errore ID";
                    elencoTransazioni += $"• ID: {idBreve}...\n";
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
                Location = new Point(10, 100),
                Size = new Size(200, 100),
                TextAlign = ContentAlignment.TopCenter,
                Font = new Font("Segoe UI", 9),
                AutoSize = false
            };

            Label lblHash = new Label
            {
                Text = "Hash:" + (hash.Length > 15 
                    ? hash.Substring(0, 15) + "..." 
                    : hash),
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

            card.Controls.Add(lblHeader);
            card.Controls.Add(lblTimestamp);
            card.Controls.Add(lblData);
            card.Controls.Add(lblHash);
            card.Controls.Add(lblNonce);
            card.Controls.Add(lblHeight);

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
                Label lblWallet = new Label
                {
                    Text = "WALLET:",
                    Font = new Font("Consolas", 9, FontStyle.Bold),
                    Location = new Point(10, 15),
                    Size = new Size(430, 15),
                    ForeColor = Color.FromArgb(24, 28, 36)
                };

                Label lblAddress = new Label
                {
                    Text = "Indirizzo: " + wallet.Address,
                    ForeColor = Color.FromArgb(45, 45, 45),
                    Location = new Point(10, 35),
                    Size = new Size(280, 40),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold)
                };

                walletCard.Controls.Add(lblWallet);
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
                           $"• A: {primoOut?.PubKeyHash ?? "N/D"}\n" +
                           $"• Valore: {primoOut?.Value} BTC",
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

        private void VisualizzaUTXOSet(string jsonContenuto)
        {
            pnlContainer.Controls.Clear();
            var utxoset = _blockchainManager.EstraiUTXOSet(jsonContenuto);
            int coordinataY = 20;
            pnlContainer.AutoScroll = true;

            foreach (var utxo in utxoset)
            {
                Panel utxoCard = new Panel
                {
                    Size = new Size(600, 160),
                    BackColor = Color.GhostWhite,
                    BorderStyle = BorderStyle.FixedSingle,
                    Location = new Point(20, coordinataY)
                };

                Label lblHeader = new Label
                {
                    Text = $"OUTPUT NON SPESO DALLA TX: {utxo.TxID ?? "N/D"}",
                    Font = new Font("Consolas", 9, FontStyle.Bold),
                    Location = new Point(10, 10),
                    Size = new Size(580, 20),
                    ForeColor = Color.DarkSlateBlue
                };

                Label lblDettagli = new Label
                {
                    Text = $"• Output Index: {utxo.Index}\n",
                    Font = new Font("Consolas", 9, FontStyle.Regular),
                    Location = new Point(10, 50),
                    Size = new Size(500, 40),
                    ForeColor = Color.Black
                };

                var primoOut = utxo.Outputs?.Count > 0 ? utxo.Outputs[0] : null;
                Label lblValore = new Label
                {
                    Text = $"DETTAGLI BILANCIO:\n" +
                           $"• Indirizzo: {primoOut?.PubKeyHash ?? "N/D"}\n" +
                           $"• Importo Disponibile: {primoOut?.Value} BTC",
                    Font = new Font("Consolas", 10, FontStyle.Bold),
                    Location = new Point(15, 90),
                    Size = new Size(500, 60),
                    ForeColor = Color.DarkGreen
                };

                utxoCard.Controls.Add(lblHeader);
                utxoCard.Controls.Add(lblDettagli);
                utxoCard.Controls.Add(lblValore);
                pnlContainer.Controls.Add(utxoCard);
                coordinataY += 180;
            }
        }
    }
}