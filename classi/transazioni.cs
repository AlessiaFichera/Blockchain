
namespace Blockchain.Core
{
    public class TransactionRoot
{

    public required List<TransactionData> Transactions { get; set; }
}



public class TransactionData
{
  
    public required string id { get; set; }

 
    public required List<TxInputData> vin { get; set; }

    public required List<TxOutputData> vout { get; set; }
}

public class TxInputData
{
    public required string txid { get; set; }
    public int vout_index { get; set; }
    public required string signature { get; set; }
    public required string pubkey { get; set; }
}

public class TxOutputData
{
    public double value { get; set; }
    public required string pubkey_hash { get; set; }
}
}