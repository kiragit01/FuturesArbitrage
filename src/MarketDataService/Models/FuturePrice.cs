namespace MarketDataService.Models;

public class FuturePrice
{
    public string Symbol { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal Volume { get; set; }
}