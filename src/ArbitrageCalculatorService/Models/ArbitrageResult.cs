using System.Text.Json.Serialization;

namespace ArbitrageCalculatorService.Models;

public class ArbitrageResult
{
    [JsonPropertyName("firstSymbol")]
    public string FirstSymbol { get; set; }
        
    [JsonPropertyName("secondSymbol")]
    public string SecondSymbol { get; set; }
        
    [JsonPropertyName("timeFrame")]
    public string TimeFrame { get; set; }
        
    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }
        
    [JsonPropertyName("endTime")]
    public DateTime EndTime { get; set; }
        
    [JsonPropertyName("spreadData")]
    public List<SpreadData> SpreadData { get; set; }
        
    [JsonPropertyName("minSpread")]
    public decimal MinSpread { get; set; }
        
    [JsonPropertyName("maxSpread")]
    public decimal MaxSpread { get; set; }
        
    [JsonPropertyName("averageSpread")]
    public decimal AverageSpread { get; set; }
}