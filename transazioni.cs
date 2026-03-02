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
    public string? PubKey { get; set; }
}

public class TxOutputData
{
    public double Value { get; set; }
    public string? PubKeyHash { get; set; }
}
}