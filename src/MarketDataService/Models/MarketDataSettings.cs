namespace MarketDataService.Models;

/// <summary>
/// Настройки для сервиса рыночных данных (читаются из конфигурации)
/// </summary>
public class MarketDataSettings
{
    /// <summary>Базовый URL API Binance</summary>
    public string BinanceApiUrl { get; set; } = "https://fapi.binance.com";

    /// <summary>Таймаут запросов к API в миллисекундах</summary>
    public int RequestTimeoutMs { get; set; } = 30000;

    /// <summary>Путь к API для получения свечей фьючерсов</summary>
    public string FuturesEndpoint { get; set; } = "/fapi/v1/klines";
    
    /// <summary>Путь к API для получения доступных фьючерсов</summary>
    public string AvailableFuturesEndpoint { get; set; } = "/fapi/v1/exchangeInfo";

    /// <summary>Максимальное количество свечей за один запрос</summary>
    public int MaxCandlesPerRequest { get; set; } = 1000;

}