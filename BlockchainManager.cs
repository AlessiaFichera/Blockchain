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
        private List<string> _rubricaIndirizziRete;
        public string? PortaCorrente { get; set; }

        // Helper per non ripetere l'URL in ogni funzione
       private string BaseUrl => $"http://localhost:{PortaCorrente}/api";
        public IReadOnlyList<Blocks> Chain => _chain.AsReadOnly();
        public List<string> RubricaIndirizziRete => _rubricaIndirizziRete;

        public event EventHandler<BlockAddedEventArgs>? BlockAdded;


        public BlockchainManager()
        {
            _chain = new List<Blocks>();
            _rubricaIndirizziRete = new List<string>();
        }

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
        public async Task SincronizzaRubricaGlobaleAsync()
        {
            string[] tutteLePorte = { "8080", "8081", "8082", "8083" };
            using var client = new HttpClient();

            foreach (var porta in tutteLePorte)
            {
                try
                {
                    string json = await client.GetStringAsync($"http://localhost:{porta}/api/get-addresses");
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var dati = JsonSerializer.Deserialize<WalletRoot>(json, options);
                    
                    if (dati?.Addresses != null)
                    {
                        foreach (var addr in dati.Addresses)
                        {
                            if (!_rubricaIndirizziRete.Contains(addr))
                                _rubricaIndirizziRete.Add(addr);
                        }
                    }
                }
                catch { /* Software robusto: se un nodo è offline, proseguiamo */ }
            }
        }

        // --- MINING E TRANSAZIONI ---
        public async Task<bool> EseguiMiningAsync(string indirizzo)
        {
            using var client = new HttpClient();
            // Incapsulamento del dato in un oggetto anonimo
            var payload = new { address = indirizzo };
            string jsonBody = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{BaseUrl}/mine", content);
            return response.IsSuccessStatusCode;
        }
        public async Task<bool> InviaTransazioneAsync(string mittente, string destinatario, int ammontare)
{
    using (var client = new HttpClient())
    {
        try 
        {
            // 1. AUTOMAZIONE MINING (Software Durevole)
            // Prima di inviare, eseguiamo il mining per accreditare i 10 coin al mittente
            var payloadMine = new { address = mittente };
            string jsonMine = JsonSerializer.Serialize(payloadMine);
            var contentMine = new StringContent(jsonMine, Encoding.UTF8, "application/json");
            
            var responseMine = await client.PostAsync($"{BaseUrl}/mine", contentMine);
            
            if (!responseMine.IsSuccessStatusCode)
            {
                Console.WriteLine("Errore durante il mining preventivo.");
                return false;
            }

            // 2. INVIO TRANSAZIONE
            var transazione = new { from = mittente, to = destinatario, amount = ammontare };
            string jsonBody = JsonSerializer.Serialize(transazione);
            var contentTx = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var responseTx = await client.PostAsync($"{BaseUrl}/tx", contentTx);
            return responseTx.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            // Software Robusto: gestione dell'eccezione in runtime
            Console.WriteLine($"Errore critico: {ex.Message}");
            return false;
        }
    }
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
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var root = JsonSerializer.Deserialize<StatisticheRoot>(jsonRicevuto, options);
        
        if (root?.statistiche == null) return new List<Analitica>();
        
        var d = root.statistiche;

        var lista = new List<Analitica>
        {
            new Analitica { Titolo = "Mining Medio",Valore = d.tempo_medio_mining.ToString("F2") + "s" },
            new Analitica { Titolo = "Transazioni", Valore = d.totale_transazioni.ToString() },
            new Analitica { Titolo = "Difficoltà Media", Valore = d.difficolta_media.ToString() },
            new Analitica { Titolo = "Valore Medio", Valore = d.valore_medio_btc.ToString("F2") }
        };

        if (d.top_ricchi != null)
        {
            string classifica = "";
            foreach (var record in d.top_ricchi)
            {
                if (record.Count >= 2)
                {
                    // Usiamo ?.ToString() ?? "" per evitare il null
                    string indirizzo = record[0]?.ToString() ?? "Unknown";
                    string val = record[1]?.ToString() ?? "0";
                    
                    string indirizzo_corto = indirizzo.Length > 40 ? indirizzo.Substring(0, 40) : indirizzo;
                    classifica += $"{indirizzo_corto}: {val} BTC\n";
                }
            }
            lista.Add(new Analitica { Titolo = "Indirizzi più ricchi", Valore = classifica });
        }

        return lista;
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
                        id = tx.id ?? "ID Sconosciuto",
                        vin = tx.vin ?? new List<TxInputData>(),
                        vout = tx.vout ?? new List<TxOutputData>()
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

    }
}