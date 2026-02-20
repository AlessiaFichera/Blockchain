using System;
using System.Collections.Generic; 

namespace Blockchain.Core
{
    
    public class BlockAddedEventArgs : EventArgs
    {
        public Block NewBlock { get; }
        public BlockAddedEventArgs(Block block) => NewBlock = block;
    }

    public class BlockchainManager
    {
        private List<Block> _chain;

        // Definizione dell'evento basato sul delegato EventHandler
        public event EventHandler<BlockAddedEventArgs>? BlockAdded;

        public BlockchainManager()
        {
            _chain = new List<Block>();
            AddGenesisBlock();
        }
        // Metodo per aggiungere il blocco genesi alla catena
        private void AddGenesisBlock()
        {
            var genesis = new Block 
            { 
                Index = 0, 
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 
                Data = "Genesis Block", 
                PreviousHash = "0", 
                Hash = "GENESIS_HASH"
            };
            _chain.Add(genesis);
        }

        public void AddBlock(string data)
        {
            // Logica per creare un nuovo blocco robusto
            try 
            {
                var lastBlock = _chain[_chain.Count - 1];
                var newBlock = new Block
                {
                    Index = lastBlock.Index + 1,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Data = data,
                    PreviousHash = lastBlock.Hash,
                    Hash = "NEW_HASH_XYZ" //da sostituire con una funzione di hashing reale
                };

                _chain.Add(newBlock);

                // Invocazione dell'evento per notificare la UI (Form1)
                OnBlockAdded(newBlock);
            }
            catch (Exception ex)
            {
                // Gestione eccezioni 
                throw new InvalidOperationException("Errore durante la creazione del blocco", ex);
            }
        }

        // Metodo protetto per scatenare l'evento 
        protected virtual void OnBlockAdded(Block block)
        {
            BlockAdded?.Invoke(this, new BlockAddedEventArgs(block));
        }

        // Proprietà per accedere alla catena in sola lettura
        public IReadOnlyList<Block> Chain => _chain.AsReadOnly();
    }
}