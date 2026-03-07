namespace Blockchain.Core
{
    public class BlockchainResponse
    {
    
        public List<Blocks>? Blocks { get; set; }
    }

    public class Blocks
    {
    
        public required string Timestamp { get; init; }
        
    
        public required string prev_hash { get; init; }

        public required string Hash { get; init; }

        public required int Nonce { get; init; }

    
        public List<TransactionData>? Transactions { get; init; }

        public required int Height { get; init; }
    }
}