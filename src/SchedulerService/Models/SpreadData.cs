using System.Text.Json.Serialization;

namespace SchedulerService.Models;

public class SpreadData
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
        
    [JsonPropertyName("firstPrice")]
    public decimal FirstPrice { get; set; }
        
    [JsonPropertyName("secondPrice")]
    public decimal SecondPrice { get; set; }
        
    [JsonPropertyName("spread")]
    public decimal Spread { get; set; }
        
    [JsonPropertyName("percentageSpread")]
    public decimal PercentageSpread { get; set; }
}