namespace ArbitrageCalculatorService.Models;

public class ArbitrageSettings
{
    /// <summary> Базовый URL API MarketDataService </summary>
    public string MarketDataEndpoint { get; set; } = "/api/marketdata/futures/";
}