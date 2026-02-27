using System;
using System.Collections.Generic; 
using System.Text.Json; 

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
                Data = "Genesis", 
                PreviousHash = "0", 
                Hash = "GENESIS_HASH",
                Nonce = 0
            };
            _chain.Add(genesis);
        }

        // Metodo per aggiungere blocchi locali
        public void AddBlock(string data, string hash, int nonce)
        {
            try 
            {
                var lastBlock = _chain[_chain.Count - 1];
                var newBlock = new Block
                {
                    Index = lastBlock.Index + 1,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Data = data,
                    PreviousHash = lastBlock.Hash,
                    Hash = hash,
                    Nonce = nonce
                };

                _chain.Add(newBlock);
                OnBlockAdded(newBlock);
            }
            catch (Exception ex)
            {
                // Implementazione di software robusto tramite gestione errori
                throw new InvalidOperationException("Errore durante la creazione del blocco", ex);
            }
        }

        // Nuovo metodo per integrare i dati provenienti da Go
        public void RiceviBloccoDaGo(string jsonRicevuto)
        {
            try 
            {
                // Uso della Type Safety per la deserializzazione
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                Block? nuovoBlocco = JsonSerializer.Deserialize<Block>(jsonRicevuto, options);

                if (nuovoBlocco != null)
                {
                    _chain.Add(nuovoBlocco);
                    OnBlockAdded(nuovoBlocco); 
                }
            }
            catch (JsonException ex)
            {
                // Gestione dell'errore per garantire la durevolezza del software
                Console.WriteLine($"Errore nei dati ricevuti da Go: {ex.Message}");
            }
        }

        protected virtual void OnBlockAdded(Block block)
        {
            BlockAdded?.Invoke(this, new BlockAddedEventArgs(block));
        }

        public IReadOnlyList<Block> Chain => _chain.AsReadOnly();
    }
}