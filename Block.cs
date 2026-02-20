using System;

namespace Blockchain.Core
{
    // Usiamo una classe (Reference Type) 
    public class Block
    {
        // Proprietà richieste per un blocco
        
        public required int Index { get; init; }
        public required long Timestamp { get; init; }
        public required string Data { get; init; }
        public required string PreviousHash { get; init; }
        public required string Hash { get; init; }

        // Metodo per visualizzare le informazioni (Override di Object.ToString)
        public override string ToString()
        {
            
            return $"Blocco #{Index} [Hash: {Hash.Substring(0, 8)}...]";
        }
    }
}