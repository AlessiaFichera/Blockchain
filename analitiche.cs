using System.Text.Json.Serialization;

namespace Blockchain.Core
{
    public class StatisticheRoot
    {
        [JsonPropertyName("statistiche")]
        public required DatiStatistici Dati { get; set; }

        [JsonPropertyName("file_grafico")]
        public string? FileGrafico { get; set; }
    }

    public class DatiStatistici
    {
        [JsonPropertyName("tempo_medio_mining")]
        public double TempoMedio { get; set; }

        [JsonPropertyName("totale_transazioni")]
        public int TotaleTransazioni { get; set; }

        [JsonPropertyName("utxo_totale")]
        public int UtxoTotale { get; set; }

        [JsonPropertyName("valore_medio_euro")]
        public double ValoreMedio { get; set; }
    }
}