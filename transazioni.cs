namespace Blockchain.Core
{
    public class TransactionRoot
{

    public List<TransactionData>? Transactions { get; set; }
}

public class TransactionData
{
    public string? ID { get; set; }
    public List<TxOutputData>? Outputs { get; set; }
}

public class TxOutputData
{
    public double Value { get; set; }
    public string? PubKeyHash { get; set; }
}
}