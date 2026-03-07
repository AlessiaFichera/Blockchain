
namespace Blockchain.Core
{
    public class TransactionRoot
{

    public List<TransactionData>? Transactions { get; set; }
}



public class TransactionData
{
  
    public string? id { get; set; }

 
    public List<TxInputData>? vin { get; set; }

    public List<TxOutputData>? vout { get; set; }
}

public class TxInputData
{
    public string? txid { get; set; }
    public int vout_index { get; set; }
    public string? signature { get; set; }
    public string? pubkey { get; set; }
}

public class TxOutputData
{
    public double value { get; set; }
    public string? pubkey_hash { get; set; }
}
}