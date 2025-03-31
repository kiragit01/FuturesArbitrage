namespace SchedulerService.Models;

public class SchedulerSettings
{
    public string Asset { get; set; }
    public string TimeFrame { get; set; }
    public string MarketDataEndpoint { get; set; } = "/api/marketdata/available-futures";
    public string ArbitrageEndpoint { get; set; } = "/api/arbitrage/calculate";
    public string DataArbitrageBatchEndpoint { get; set; } = "/api/data/arbitrage/batch";
}