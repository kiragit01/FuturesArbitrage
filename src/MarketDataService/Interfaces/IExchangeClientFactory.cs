namespace MarketDataService.Interfaces;

public interface IExchangeClientFactory
{
    IExchangeClient CreateClient(string exchangeName);
}