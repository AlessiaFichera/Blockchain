using System.Text.Json.Serialization;
namespace Blockchain.Core

{

   public class Analitica 
{ 
    public string Titolo { get; set; } = ""; 
    public string Valore { get; set; } = ""; 
}

public class StatisticheRoot
{
    public required DatiStatistici statistiche { get; set; }
}

public class DatiStatistici
{
    public double tempo_medio_mining { get; set; }
    public int totale_transazioni { get; set; }
    public int utxo_totale { get; set; }
    public double valore_medio_btc { get; set; }
}

} 