using System.Text.Json.Serialization;

namespace ArbitrageCalculatorService.Models;

public class FuturePrice
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; }
            
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
            
    [JsonPropertyName("closePrice")]
    public decimal ClosePrice { get; set; }
}