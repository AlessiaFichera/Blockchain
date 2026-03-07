using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Diagnostics;
using System.Net.Http; 
using System.Threading.Tasks;
using System.Text;

namespace Blockchain.Core
{
    // Classe per gli argomenti dell'evento
    public class BlockAddedEventArgs : EventArgs
    {
        public Blocks NewBlock { get; }
        public BlockAddedEventArgs(Blocks block) => NewBlock = block;
    }

    public class BlockchainManager
    {
        private List<Blocks> _chain;

        public string? PortaCorrente { get; set; }

        // Helper per non ripetere l'URL in ogni funzione
        private string BaseUrl => $"http://localhost:{PortaCorrente}/api";

/*public async Task<bool> MinaBloccoAsync()
{
    using (var client = new HttpClient())
    {
        try 
        {
            // Chiamata all'endpoint di mining del nodo corrente
            var response = await client.GetAsync($"{BaseUrl}/mine");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore durante il mining: {ex.Message}");
            return false;
        }
    }
}*/
        
        public event EventHandler<BlockAddedEventArgs>? BlockAdded;

        public BlockchainManager()
        {
            _chain = new List<Blocks>();
        }

        public IReadOnlyList<Blocks> Chain => _chain.AsReadOnly();

        public async Task<List<Blocks>> SincronizzaBlockchain()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    // Chiamata all'endpoint dinamico
                    string jsonRicevuto = await client.GetStringAsync($"{BaseUrl}/print-blockchain");

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    // Deserializzazione della radice "blocks"
                    var root = JsonSerializer.Deserialize<BlockchainResponse>(jsonRicevuto, options);
                    
                    if (root?.Blocks != null)
                    {
                        // Software Robusto: puliamo la catena per evitare dati incoerenti
                        _chain.Clear(); 
                        
                        foreach (var b in root.Blocks)
                        {
                            _chain.Add(b);
                            OnBlockAdded(b);
                        }
                        return _chain;
                    }
                    return new List<Blocks>();
                }
                catch (HttpRequestException ex)
                {
                    throw new Exception($"Il nodo Go non risponde sulla porta {PortaCorrente}.", ex);
                }
                catch (JsonException ex)
                {
                    throw new Exception("Errore nel formato dei dati (JSON) ricevuti.", ex);
                }
            }
        }

        protected virtual void OnBlockAdded(Blocks block)
        {
            BlockAdded?.Invoke(this, new BlockAddedEventArgs(block));
        }

        public void EseguiAggiornamentoPython()
        {
            try
            {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = "python";
                start.Arguments = "blockchain_analyzer.py";
                start.UseShellExecute = false;
                start.CreateNoWindow = true; 

                using (Process? process = Process.Start(start))
                {
                    process?.WaitForExit(); 
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
                var root = JsonSerializer.Deserialize<StatisticheRoot>(jsonRicevuto);
                if (root == null || root.statistiche == null)
                {
                    return new List<Analitica>();
                }
                var d = root.statistiche;

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

        public async Task<(List<string> Lista, int Totale)> EstraiListaWallet() 
        {
            using HttpClient client = new HttpClient();
            try
            {
                var response = await client.GetAsync($"{BaseUrl}/get-addresses");
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

        public async Task<(int count, List<Utxo> Utxos)> EstraiUTXOSet()
        {
            using HttpClient client = new HttpClient();
            try
            {
                var response = await client.GetAsync($"{BaseUrl}/print-utxoset");
                response.EnsureSuccessStatusCode();

                string jsonRicevuto = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var root = JsonSerializer.Deserialize<UtxoResponse>(jsonRicevuto, options);

                if (root?.Utxos == null)
                {
                    return (0, new List<Utxo>());
                }

                return (root.count, root.Utxos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore: {ex.Message}");
                return (0, new List<Utxo>());
            }
        }

        public async Task<string> CreateAddressAsync()
        {
            using HttpClient client = new HttpClient();
            try 
            {
                var response = await client.GetAsync($"{BaseUrl}/create-address");
                response.EnsureSuccessStatusCode(); 
                
                string jsonContenuto = await response.Content.ReadAsStringAsync();

                var opzioni = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var dati = JsonSerializer.Deserialize<WalletAccount>(jsonContenuto, opzioni);

                return dati?.Address ?? "Indirizzo non trovato";
            }
            catch (Exception ex)
            {
                return "Errore: " + ex.Message;
            }
        }

        public async Task<bool> InviaTransazioneAsync(string mittente, string destinatario, int ammontare)
        {
            using (var client = new HttpClient())
            {
                var transazione = new { from = mittente, to = destinatario, amount = ammontare };

                try 
                {
                    string jsonBody = JsonSerializer.Serialize(transazione);
                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync($"{BaseUrl}/tx", content);
                    return response.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Errore: {ex.Message}");
                    return false;
                }
            }
        }
    }
}