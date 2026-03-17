namespace Blockchain.Core
{
    public class UtxoResponse
{
    public int count { get; set; }
    public List<Utxo> Utxos { get; set; } = new List<Utxo>();
}
    public class Utxo
{

     public required string tx_id { get; set; }
    

    public required int index { get; set; }
    
    public double value { get; set; }
    public required string pub_key_hash { get; set; }
}
}