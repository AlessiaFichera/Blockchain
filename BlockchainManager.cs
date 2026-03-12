using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Blockchain.Core
{
    public class BlockchainManager
    {
        // Dati principali
        private List<Blocks> _chain;
        // Rubrica globale degli indirizzi (aggiornata periodicamente)
        private List<string> _rubricaIndirizziRete;
        // Porta del nodo Go attualmente in uso (corrispondente all'account selezionato)
        public string? PortaCorrente { get; set; }
        // URL base per le API del nodo Go (costruito dinamicamente in base alla porta corrente)
        private string BaseUrl
        {
            get
            {
                return "http://localhost:" + PortaCorrente + "/api";
            }
        }
        // Proprietà di sola lettura per accedere alla blockchain e alla rubrica degli indirizzi
        public IReadOnlyList<Blocks> Chain
        {
            get
            {
                return _chain.AsReadOnly();
            }
        }

        public IReadOnlyList<string> RubricaIndirizziRete
        {
            get
            {
                return _rubricaIndirizziRete.AsReadOnly();
            }
        }
        // Evento per notificare l'aggiunta di un nuovo blocco alla blockchain
        public event EventHandler<BlockAddedEventArgs>? BlockAdded;
        // Costruttore che inizializza la blockchain e la rubrica degli indirizzi
        public BlockchainManager()
        {
            _chain = new List<Blocks>();
            _rubricaIndirizziRete = new List<string>();
        }
        // Metodo per sincronizzare la blockchain con il nodo Go, recuperando l'intera catena di blocchi
        public async Task<List<Blocks>> SincronizzaBlockchain()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    string jsonRicevuto = await client.GetStringAsync($"{BaseUrl}/print-blockchain");

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var root = JsonSerializer.Deserialize<BlockchainResponse>(jsonRicevuto, options);

                    if (root?.Blocks != null)
                    {
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
        // Metodo protetto per sollevare l'evento di aggiunta di un blocco, passando il blocco appena aggiunto come argomento
        protected virtual void OnBlockAdded(Blocks block)
        {
            BlockAdded?.Invoke(this, new BlockAddedEventArgs(block));
        }
        // Metodo per sincronizzare la rubrica globale degli indirizzi
        public async Task<List<string>> SincronizzaRubricaGlobaleAsync()
        {
            string[] tutteLePorte = { "8080", "8081", "8082", "8083" };

            using var client = new HttpClient();

            client.Timeout = TimeSpan.FromSeconds(1);

            foreach (var porta in tutteLePorte)
            {
                try
                {
                    string json = await client.GetStringAsync($"http://localhost:{porta}/api/get-addresses");

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var dati = JsonSerializer.Deserialize<WalletRoot>(json, options);

                    if (dati?.Addresses != null)
                    {
                        foreach (var indirizzo in dati.Addresses)
                        {
                            if (!_rubricaIndirizziRete.Contains(indirizzo))
                            {
                                _rubricaIndirizziRete.Add(indirizzo);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Nodo sulla porta {porta} non raggiungibile: {ex.Message}");
                }
            }

            return _rubricaIndirizziRete.ToList();
        }
        // Metodo per eseguire il mining di un nuovo blocco, specificando l'indirizzo del miner 
        public async Task<bool> EseguiMiningAsync(string indirizzo)
        {
            using var client = new HttpClient();

            var payload = new { address = indirizzo };
            string jsonBody = JsonSerializer.Serialize(payload);

            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{BaseUrl}/mine", content);

            return response.IsSuccessStatusCode;
        }
        // Metodo per inviare una nuova transazione alla blockchain, specificando mittente, destinatario e ammontare
        public async Task<bool> InviaTransazioneAsync(string mittente, string destinatario, int ammontare)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var transazione = new
                    {
                        from = mittente,
                        to = destinatario,
                        amount = ammontare
                    };

                    string jsonBody = JsonSerializer.Serialize(transazione);

                    var contentTx = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    var responseTx = await client.PostAsync($"{BaseUrl}/tx", contentTx);

                    return responseTx.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Errore critico: {ex.Message}");
                    return false;
                }
            }
        }
        // Metodo per eseguire l'aggiornamento dei dati analitici tramite lo script Python
        public void EseguiAggiornamentoPython()
        {
            try
            {
                ProcessStartInfo start = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "blockchain_analyzer.py",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

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
        // Metodo per estrarre le analitiche principali dalla risposta JSON ricevuta dal nodo python
        public List<Analitica> EstraiAnalitiche(string jsonRicevuto)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var root = JsonSerializer.Deserialize<StatisticheRoot>(jsonRicevuto, options);

                if (root?.statistiche == null)
                    return new List<Analitica>();

                var d = root.statistiche;

                var lista = new List<Analitica>
                {
                    new Analitica { Titolo = "Mining Medio", Valore = d.tempo_medio_mining.ToString("F2") + "s" },
                    new Analitica { Titolo = "Transazioni", Valore = d.totale_transazioni.ToString() },
                    new Analitica { Titolo = "Difficoltà Media", Valore = d.difficolta_media.ToString() },
                    new Analitica { Titolo = "Valore Medio Utxo", Valore = d.valore_medio_btc.ToString("F2") + " BTC" }
                };

                if (d.top_ricchi != null)
                {
                    string classifica = "";

                    foreach (var record in d.top_ricchi)
                    {
                        if (record.Count >= 2)
                        {
                            string indirizzo = record[0]?.ToString() ?? "Unknown";
                            string val = record[1]?.ToString() ?? "0";

                            string indirizzoCorto = indirizzo.Length > 40
                                ? indirizzo.Substring(0, 40)
                                : indirizzo;

                            classifica += $"{indirizzoCorto}: {val} BTC\n";
                        }
                    }

                    lista.Add(new Analitica
                    {
                        Titolo = "Indirizzi più ricchi",
                        Valore = classifica
                    });
                }

                return lista;
            }
            catch
            {
                return new List<Analitica>();
            }
        }
        // Metodo per estrarre la lista completa dei wallet disponibili 
        public async Task<WalletRoot> EstraiListaWallet()
        {
            using HttpClient client = new HttpClient();

            try
            {
                var response = await client.GetAsync($"{BaseUrl}/get-addresses");

                response.EnsureSuccessStatusCode();

                string jsonRicevuto = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var dati = JsonSerializer.Deserialize<WalletRoot>(jsonRicevuto, options);

                return dati ?? new WalletRoot
                {
                    Addresses = new List<string>(),
                    Count = 0
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore: {ex.Message}");

                return new WalletRoot
                {
                    Addresses = new List<string>(),
                    Count = 0
                };
            }
        }
        // Metodo per estrarre la lista completa delle transazioni 
        public List<TransactionData> EstraiListaTransazioni(string jsonRicevuto)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

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
        // Metodo per estrarre l'UTXO Set completo dalla blockchain
        public async Task<UtxoResponse> EstraiUTXOSet()
        {
            using HttpClient client = new HttpClient();

            try
            {
                var response = await client.GetAsync($"{BaseUrl}/print-utxoset");

                response.EnsureSuccessStatusCode();

                string jsonRicevuto = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var root = JsonSerializer.Deserialize<UtxoResponse>(jsonRicevuto, options);

                if (root?.Utxos == null)
                {
                    return new UtxoResponse
                    {
                        count = 0,
                        Utxos = new List<Utxo>()
                    };
                }

                return new UtxoResponse
                {
                    count = root.count,
                    Utxos = root.Utxos
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore: {ex.Message}");

                return new UtxoResponse
                {
                    count = 0,
                    Utxos = new List<Utxo>()
                };
            }
        }
        //Metodo per creare un nuovo indirizzo tramite il nodo Go
        public async Task<string> CreateAddressAsync()
        {
            using HttpClient client = new HttpClient();

            try
            {
                var response = await client.GetAsync($"{BaseUrl}/create-address");

                response.EnsureSuccessStatusCode();

                string jsonContenuto = await response.Content.ReadAsStringAsync();

                var opzioni = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var dati = JsonSerializer.Deserialize<WalletAccount>(jsonContenuto, opzioni);

                return dati?.Address ?? "Indirizzo non trovato";
            }
            catch (Exception ex)
            {
                return "Errore: " + ex.Message;
            }
        }
        // Metodo per ottenere il saldo completo di un indirizzo specifico
        public async Task<BalanceResponse> OttieniSaldoCompletoAsync(string indirizzoRichiesto)
        {
            using var client = new HttpClient();

            try
            {
                string url = $"{BaseUrl}/get-balance?address={indirizzoRichiesto}";

                string json = await client.GetStringAsync(url);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                BalanceResponse? risposta =
                    JsonSerializer.Deserialize<BalanceResponse>(json, options);

                if (risposta != null)
                {
                    risposta.Address = indirizzoRichiesto;
                    return risposta;
                }

                return new BalanceResponse
                {
                    Address = indirizzoRichiesto,
                    Result = "0"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore critico nel recupero saldo: {ex.Message}");

                return new BalanceResponse
                {
                    Address = indirizzoRichiesto,
                    Result = "0"
                };
            }
        }
    }
}