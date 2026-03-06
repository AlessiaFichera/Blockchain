using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Diagnostics;
using System.Net.Http; 
using System.Threading.Tasks;

namespace Blockchain.Core
{
    // Classe per gli argomenti dell'evento
    public class BlockAddedEventArgs : EventArgs
    {
        public Block NewBlock { get; }
        public BlockAddedEventArgs(Block block) => NewBlock = block;
    }

    public class BlockchainManager
    {
        private List<Block> _chain;

        // Definizione dell'evento per notificare quando un nuovo blocco viene aggiunto
        public event EventHandler<BlockAddedEventArgs>? BlockAdded;

        public BlockchainManager()
        {
            _chain = new List<Block>();
            AddGenesisBlock();
        }

        // --- GESTIONE CORE BLOCKCHAIN ---

        private void AddGenesisBlock()
        {
            var genesis = new Block
            {
                Index = 0,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Transactions = new List<TransactionData>(),
                PreviousHash = "0",
                Hash = "GENESIS_HASH",
                Nonce = 0,
                Height = 1
            };
            _chain.Add(genesis);
        }

        public void AddBlock(int Index, long Timestamp, List<TransactionData> transactions, string hash, int nonce, int height)
        {
            try
            {
                var lastBlock = _chain[_chain.Count - 1];
                var newBlock = new Block
                {
                    Index = Index,
                    Timestamp = Timestamp,
                    Transactions = transactions,
                    PreviousHash = lastBlock.Hash,
                    Hash = hash,
                    Nonce = nonce,
                    Height = height
                };

                _chain.Add(newBlock);
                OnBlockAdded(newBlock);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Errore durante la creazione del blocco", ex);
            }
        }

        protected virtual void OnBlockAdded(Block block)
        {
            BlockAdded?.Invoke(this, new BlockAddedEventArgs(block));
        }

        public IReadOnlyList<Block> Chain => _chain.AsReadOnly();


        public void RiceviBloccoDaGo(string jsonRicevuto)
        {
            try
            {
                // 1. Type Safety: Definiamo le opzioni per far corrispondere le proprietà JSON alle classi 
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
            
                start.FileName = "python";

                start.Arguments = "blockchain_analyzer.py";

                // 3. Opzioni per l'esecuzione pulita
                start.UseShellExecute = false;
                start.CreateNoWindow = true; 

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

       // Nel file BlockchainManager.cs
public async Task<(List<string> Lista, int Totale)> EstraiListaWallet() 
{
    using HttpClient client = new HttpClient();
    try
    {
        // Assicurati che l'URL sia corretto per il tuo server
        var response = await client.GetAsync("http://localhost:8080/api/get-addresses");
        response.EnsureSuccessStatusCode();

        string jsonRicevuto = await response.Content.ReadAsStringAsync();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dati = JsonSerializer.Deserialize<WalletRoot>(jsonRicevuto, options);

        if (dati?.Addresses == null)
        {
            return (new List<string>(), 0);
        }

        return (dati.Addresses, dati.Count);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Errore: {ex.Message}");
        return (new List<string>(), 0);
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
                        Inputs = tx.Inputs ?? new List<TxInputData>(),
                        Outputs = tx.Outputs ?? new List<TxOutputData>()
                    });
                }
                return listaTransazioni;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore critico transazioni: {ex.Message}");
                return new List<TransactionData>();
            }
        }

        public List<Utxo> EstraiUTXOSet(string jsonRicevuto)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var listaUtxo = JsonSerializer.Deserialize<List<Utxo>>(jsonRicevuto, options);

                if (listaUtxo == null)
                {
                    return new List<Utxo>();
                }

                var risultato = new List<Utxo>();
                foreach (var utxo in listaUtxo)
                {
                    risultato.Add(new Utxo
                    {
                        TxID = utxo.TxID ?? "ID Transazione Sconosciuto",
                        Index = utxo.Index,
                        Outputs = utxo.Outputs ?? new List<TxOutputData>()
                    });
                }
                return risultato;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore critico durante l'estrazione dell'UTXO Set: {ex.Message}");
                return new List<Utxo>();
            }
        }
        // Esempio di collegamento a una funzione esistente

public async Task<string> CreateAddressAsync()
{
    using HttpClient client = new HttpClient();
    try 
    {
        var response = await client.GetAsync("http://localhost:8080/api/create-address");
        response.EnsureSuccessStatusCode(); 
        
        string jsonContenuto = await response.Content.ReadAsStringAsync();

        // Trasformiamo il JSON in un oggetto C# (Type Safety)
        var opzioni = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dati = JsonSerializer.Deserialize<WalletAccount>(jsonContenuto, opzioni);

        // Restituiamo solo l'indirizzo pulito, non tutto il JSON
        return dati?.Address ?? "Indirizzo non trovato";
    }
    catch (Exception ex)
    {
        return "Errore: " + ex.Message;
    }
}
    }

}