namespace Blockchain;
using System;
using System.Drawing;
using System.Windows.Forms;
public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
        
        // Colleghiamo l'evento a tutti i bottoni
        btnVisualizzaBlockchain.Click += (s, e) => EntraInModalitaDettaglio("BLOCKCHAIN");
        btnAggiungiWallet.Click += (s, e) =>EntraInModalitaDettaglio("WALLET");
        btnInviaTransazione.Click += (s, e) =>EntraInModalitaDettaglio("TRANSAZIONE");

    }
    private void EntraInModalitaDettaglio(string modalita)
    {
        // 1. Sposta i bottoni nella barra laterale
        btnAggiungiWallet.Dock = DockStyle.Top;
        btnInviaTransazione.Dock = DockStyle.Top;
        btnVisualizzaBlockchain.Dock = DockStyle.Top;

        pnlDettaglio.Controls.Add(btnAggiungiWallet);
        pnlDettaglio.Controls.Add(btnInviaTransazione);
        pnlDettaglio.Controls.Add(btnVisualizzaBlockchain);

        pnlDettaglio.Visible = true;
        lblTitle.Visible = true;
        lblTitle.Parent = pnlHeaderDettaglio;
        lblTitle.Location = new Point(250, 10);
        lblTitle.BringToFront();
        pnlHeaderDettaglio.Visible = true;

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

    for (int i = 1; i <= 5; i++)// Creiamo 5 blocchi di esempio
    {
        // Creiamo il blocco grafico (Panel)
        Panel blocco = CreaSingoloBlocco(i.ToString(), "HASH: 000abc...12f", i != 3);
        blocco.Location = new Point(coordinataX, 100); // Posizionato al centro altezza
        
        pnlContainer.Controls.Add(blocco);

        // Aggiungiamo una freccia tra i blocchi (tranne che dopo l'ultimo)
        if (i < 5)
        {
            Label freccia = new Label();
            freccia.Text = "➔";
            freccia.ForeColor = Color.FromArgb(41, 171, 226);
            freccia.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            freccia.Location = new Point(coordinataX + 215, 170);
            freccia.AutoSize = true;
            pnlContainer.Controls.Add(freccia);
        }

        coordinataX += 280; // Spostiamo il prossimo blocco più a destra
    }
}

private Panel CreaSingoloBlocco(string id, string hash, bool isValid)
{
    Panel card = new Panel();
    card.Size = new Size(210, 200);
    card.BackColor = Color.White;
    card.BorderStyle = BorderStyle.FixedSingle;

    // Header Azzurro
    Label lblHeader = new Label();
    lblHeader.Text = "🔒 BLOCK #" + id;
    lblHeader.BackColor = Color.FromArgb(41, 171, 226);
    lblHeader.ForeColor = Color.White;
    lblHeader.Dock = DockStyle.Top;
    lblHeader.Height = 30;
    lblHeader.TextAlign = ContentAlignment.MiddleCenter;
    lblHeader.Font = new Font("Segoe UI", 10, FontStyle.Bold);

    // Hash (Sfondo scuro come immagine)
    Label lblHash = new Label();
    lblHash.Text = hash;
    lblHash.BackColor = Color.FromArgb(45, 45, 45);
    lblHash.ForeColor = Color.White;
    lblHash.Location = new Point(10, 60);
    lblHash.Size = new Size(190, 25);
    lblHash.TextAlign = ContentAlignment.MiddleCenter;

    // Stato in fondo
    Label lblStatus = new Label();
    lblStatus.Text = isValid ? "Stato: VALIDO" : "Stato: MODIFICATO!";
    lblStatus.ForeColor = isValid ? Color.Green : Color.Red;
    lblStatus.Dock = DockStyle.Bottom;
    lblStatus.TextAlign = ContentAlignment.MiddleCenter;
    lblStatus.Height = 30;

    card.Controls.Add(lblHash);
    card.Controls.Add(lblHeader);
    card.Controls.Add(lblStatus);

    return card;
    }       
}
        

