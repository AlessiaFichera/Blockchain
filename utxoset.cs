namespace Blockchain.Core
{
    public class Utxo
{
    // ID della transazione (hash)
     public string? TxID { get; set; }
    

    public int Index { get; set; }
    
    // Il contenuto effettivo dell'output
    public List<TxOutputData>? Outputs { get; set; }
}
}