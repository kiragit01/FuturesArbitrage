using Microsoft.Extensions.Options;
using SchedulerService.Models;

namespace SchedulerService.Services;

public class SchedulerService : ISchedulerService
{
    private readonly ILogger<SchedulerService> _logger;
    private readonly HttpClient _httpClient;
    private readonly SchedulerSettings _settings;

    public SchedulerService(
        ILogger<SchedulerService> logger
        , IHttpClientFactory httpClientFactory
        , IOptions<SchedulerSettings> settings
        )
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("ArbitrageClient");
        _settings = settings.Value;
    }

    public async Task TriggerArbitrageCalculationAsync(string firstSymbol, string secondSymbol, string timeFrame)
    {
        try
        {
            _logger.LogInformation("Manually triggering arbitrage calculation for {FirstFuture} and {SecondFuture} with {TimeFrame} timeframe",
                firstSymbol, secondSymbol, timeFrame);

            var requestUrl = $"{_httpClient.BaseAddress}{_settings.ArbitrageEndpoint}" +
                         $"?firstSymbol={Uri.EscapeDataString(firstSymbol)}" +
                         $"&secondSymbol={Uri.EscapeDataString(secondSymbol)}" +
                         $"&timeFrame={Uri.EscapeDataString(timeFrame)}";

            var response = await _httpClient.PostAsync(requestUrl, null);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully triggered manual arbitrage calculation");
            }
            else
            {
                _logger.LogError("Failed to trigger manual arbitrage calculation. Status code: {StatusCode}",
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while triggering manual arbitrage calculation");
            throw;
        }
    }
}