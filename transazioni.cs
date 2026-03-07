namespace Blockchain.Core
{
    public class TransactionRoot
{

    public List<TransactionData>? Transactions { get; set; }
}

public class TransactionData
{
    public string? ID { get; set; }
    public List<TxInputData>? Inputs { get; set; }
    public List<TxOutputData>? Outputs { get; set; }
}

public class TxInputData
{
    public string? TxID { get; set; }
    public int OutputIndex { get; set; }
    public string? Signature { get; set; }
    public string? pub_key_hash { get; set; }
}

public class TxOutputData
{
    public double value { get; set; }
    public string? pub_key_hash { get; set; }
}
}