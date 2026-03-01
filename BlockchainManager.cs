using System;
using System.Collections.Generic; 
using System.Text.Json; 
using System.Diagnostics;


namespace Blockchain.Core
{
    // Classe per gli argomenti dell'evento, come previsto dall'orientamento ai componenti
    public class BlockAddedEventArgs : EventArgs
    {
        public Block NewBlock { get; }
        public BlockAddedEventArgs(Block block) => NewBlock = block;
    }

    public class BlockchainManager
    {
        private List<Block> _chain;

        // Definizione dell'evento: concetto di "prima classe" in C#
        public event EventHandler<BlockAddedEventArgs>? BlockAdded;

        public BlockchainManager()
        {
            _chain = new List<Block>();
            AddGenesisBlock();
        }

        private void AddGenesisBlock()
        {
            var genesis = new Block 
            { 
                Index = 0, 
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 
                Transactions = new List<TransactionData>(), 
                PreviousHash = "0", 
                Hash = "GENESIS_HASH",
                Nonce = 0
            };
            _chain.Add(genesis);
        }

        // Metodo per aggiungere blocchi locali
        public void AddBlock(List<TransactionData> transactions, string hash, int nonce)
    {
        try 
        {
            var lastBlock = _chain[_chain.Count - 1];
            var newBlock = new Block
            {
                Index = lastBlock.Index + 1,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Transactions = transactions, // Ora accettiamo la lista di componenti transazione
                PreviousHash = lastBlock.Hash,
                Hash = hash,
                Nonce = nonce
            };

            _chain.Add(newBlock);
            OnBlockAdded(newBlock);
        }
        catch (Exception ex)
        {
            // Gestione errori per un software durevole
            throw new InvalidOperationException("Errore durante la creazione del blocco", ex);
        }
    }

    protected virtual void OnBlockAdded(Block block)
    {
        BlockAdded?.Invoke(this, new BlockAddedEventArgs(block));
    }

    public IReadOnlyList<Block> Chain => _chain.AsReadOnly();


        // Nuovo metodo per integrare i dati provenienti da Go
        // In BlockchainManager.cs
public void RiceviBloccoDaGo(string jsonRicevuto)
{
    try 
    {
        // 1. Type Safety: Definiamo le opzioni per far corrispondere le proprietà JSON alle classi C#
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        
        // 2. Orientamento ai componenti: Deserializziamo i dati in una collezione di oggetti Block
        List<Block>? nuoviBlocchi = JsonSerializer.Deserialize<List<Block>>(jsonRicevuto, options);

        if (nuoviBlocchi != null)
        {
            foreach (var b in nuoviBlocchi)
            {
                // Verifichiamo l'integrità prima di aggiungere alla catena esistente
                if (!_chain.Exists(x => x.Hash == b.Hash))
                {
                    _chain.Add(b);
                    
                    // 3. Eventi: Notifichiamo il sistema del nuovo componente aggiunto
                    OnBlockAdded(b);
                }
            }
        }
    }
    catch (JsonException ex)
    {
        // 4. Software Robusto: Gestiamo l'errore a runtime senza interrompere l'esecuzione
        Console.WriteLine($"Errore nei metadati JSON ricevuti: {ex.Message}");
    }
    }
public void EseguiAggiornamentoPython()
{
    try
    {
        ProcessStartInfo start = new ProcessStartInfo();
        // 1. Specifica l'eseguibile (python o python3 a seconda del sistema)
        start.FileName = "python"; 
        
        // 2. Passa il percorso del file come argomento
        start.Arguments = "blockchain_analyzer.py"; 
        
        // 3. Opzioni per l'esecuzione pulita
        start.UseShellExecute = false;
        start.CreateNoWindow = true; // Nasconde la finestra nera del terminale

        using (Process? process = Process.Start(start))
        {
            process?.WaitForExit(); // Aspetta che Python finisca prima di proseguire
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Errore nell'esecuzione di Python: {ex.Message}");
    }
}
public List<Analitica> EstraiAnalitiche(string jsonRicevuto)
{
    try 
    {
        // Deserializzazione (Type Safety)
        var root = JsonSerializer.Deserialize<StatisticheRoot>(jsonRicevuto);
        if (root == null || root.statistiche == null) 
        {
            return new List<Analitica>(); 
        }
        var d = root.statistiche;

        // Creiamo una lista di componenti 
        return new List<Analitica> 
        {
            new Analitica { Titolo = "Mining Medio", Valore = d.tempo_medio_mining.ToString("F2") + "s" },
            new Analitica { Titolo = "Transazioni", Valore = d.totale_transazioni.ToString() },
            new Analitica { Titolo = "UTXO Totale", Valore = d.utxo_totale.ToString() },
            new Analitica { Titolo = "Valore Medio", Valore = "€" + d.valore_medio_euro.ToString("F2") }
        };
    }
    catch (Exception) { return new List<Analitica>(); } 
}
public class StatisticaUI 
{ 
    public string Titolo { get; set; } = ""; 
    public string Valore { get; set; } = ""; 
}


       public List<WalletAccount> EstraiListaWallet(string jsonRicevuto)
{
    try 
    {
        // 1. Type Safety: Opzioni per far corrispondere il JSON ai Metadata C#
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        
        // 2. Deserializzazione: Usiamo il tipo annullabile '?' come nelle slide
        var root = JsonSerializer.Deserialize<WalletRoot>(jsonRicevuto, options);
        
        if (root == null || root.Accounts == null) 
        {
            return new List<WalletAccount>(); 
        }

        // Creiamo la lista finale di WalletAccount, coerente con la tua classe
        var listaWallet = new List<WalletAccount>();

        foreach (var acc in root.Accounts)
        {
            // Aggiungiamo l'oggetto WalletAccount con i dati da Go
            listaWallet.Add(new WalletAccount 
            { 
                Address = acc.Address ?? "Indirizzo non trovato", 
                Balance = acc.Balance 
            });
        }

        return listaWallet;
    }
    catch (Exception ex) 
    { 
        // 3. Software Robusto: Gestione errori a runtime
        Console.WriteLine($"Errore caricamento Wallet: {ex.Message}");
        return new List<WalletAccount>(); 
    } 
}
public List<TransactionData> EstraiListaTransazioni(string jsonRicevuto)
{
    try 
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var root = JsonSerializer.Deserialize<TransactionRoot>(jsonRicevuto, options);
        
        if (root == null || root.Transactions == null) 
        {
            return new List<TransactionData>(); 
        }

        var listaTransazioni = new List<TransactionData>();

        foreach (var tx in root.Transactions)
        {
            listaTransazioni.Add(new TransactionData 
            { 
                ID = tx.ID ?? "ID Sconosciuto",
                Outputs = tx.Outputs ?? new List<TxOutputData>() 
            });
        }

        return listaTransazioni;
    }
    catch (Exception ex) 
    { 
        // Catturiamo l'eccezione per non far chiudere il programma
        Console.WriteLine($"Errore critico transazioni: {ex.Message}");
        return new List<TransactionData>(); 
    } 
}
    
    }
}