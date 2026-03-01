using System;
using System.Collections.Generic;

namespace Blockchain.Core
{
    public class Block
    {
        public required int Index { get; init; }
        public required long Timestamp { get; init; }
        
        // MODIFICA: Da string a List per gestire i dati reali di Go
        public List<TransactionData>? Transactions { get; init; }

        public required string PreviousHash { get; init; }
        public required string Hash { get; init; }
        public required int Nonce { get; init; }

        // Metodo ToString aggiornato per gestire stringhe corte (Software Robusto)
        public override string ToString()
        {
            string hashBreve = (Hash.Length > 8) ? Hash.Substring(0, 8) : Hash;
            return $"Blocco #{Index} [Hash: {hashBreve}...] - TX: {Transactions?.Count ?? 0}";
        }
    }
}