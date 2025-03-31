using MarketDataService.Interfaces;

namespace MarketDataService.Services;

public class ExchangeClientFactory : IExchangeClientFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExchangeClientFactory> _logger;

    public ExchangeClientFactory(
        IServiceProvider serviceProvider,
        ILogger<ExchangeClientFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IExchangeClient CreateClient(string exchangeName)
    {
        _logger.LogInformation("Creating exchange client for {ExchangeName}", exchangeName);
            
        return exchangeName.ToLower() switch
        {
            "binance" => _serviceProvider.GetRequiredService<BinanceClient>(),
            _ => throw new ArgumentException($"Exchange {exchangeName} is not supported")
        };
    }
}