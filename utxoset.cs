namespace Blockchain.Core
{
    public class UtxoResponse
{
    public int count { get; set; }
    public List<Utxo> Utxos { get; set; } = new List<Utxo>();
}
    public class Utxo
{

     public string? tx_id { get; set; }
    

    public int index { get; set; }
    
    public double value { get; set; }
    public string? pub_key_hash { get; set; }
}
}