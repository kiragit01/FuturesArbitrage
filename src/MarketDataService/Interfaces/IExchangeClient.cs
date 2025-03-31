using MarketDataService.Models;

namespace MarketDataService.Interfaces;

public interface IExchangeClient
{
    Task<IEnumerable<FuturePrice>> GetFuturesPrice(string symbol, string timeFrame, DateTime startTime, DateTime endTime);
    Task<IEnumerable<string>> GetAvailableFuturesAsync();
}