using System.ComponentModel.DataAnnotations;
using MarketDataService.Interfaces;
using MarketDataService.Models;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace MarketDataService.Controllers;

[ApiController]
[Route("api/marketdata")]
public class MarketDataController : ControllerBase
{
    private static readonly Counter marketdataRequestsTotal = Metrics.CreateCounter("marketdata_requests_total", "Number of market data requests", new string[] { "exchange", "symbol" });
    private static readonly Counter marketdataRequestsAvailableFuturesTotal = Metrics.CreateCounter("marketdata_requests_available_futures_total", "Number of market data requests", new string[] { "exchange", "symbol" });
    private static readonly Histogram marketdataRequestDuration = Metrics.CreateHistogram("marketdata_request_duration_seconds", "Duration of market data requests", new string[] { "exchange", "symbol" });
    private static readonly Counter marketdataErrorsTotal = Metrics.CreateCounter("marketdata_errors_total", "Number of market data errors", new string[] { "error_type", "exchange", "symbol" });
    private static readonly Counter marketdataAvailableFuturesErrorsTotal = Metrics.CreateCounter("marketdata_available_futures_errors_total", "Number of market data errors", new string[] { "error_type", "exchange", "symbol" });
    
    private readonly IExchangeClientFactory _exchangeClientFactory;
    private readonly ILogger<MarketDataController> _logger;

    public MarketDataController(
        IExchangeClientFactory exchangeClientFactory,
        ILogger<MarketDataController> logger)
    {
        _exchangeClientFactory = exchangeClientFactory ?? throw new ArgumentNullException(nameof(exchangeClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("futures/{symbol}/{timeFrame}")]
    public async Task<ActionResult<IEnumerable<FuturePrice>>> GetFuturesPrices(
        [Required] string symbol,
        [Required] string timeFrame, 
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime = null)
    {
        var timer = marketdataRequestDuration.WithLabels("Binance", symbol).NewTimer();
        try
        {
            _logger.LogInformation(
                "Getting futures price data for {Symbol}, TimeFrame: {TimeFrame}, StartTime: {StartTime}",
                symbol, timeFrame, startTime);

            var client = _exchangeClientFactory.CreateClient("Binance");

            // Если время начала не указано, берем день назад
            var startTimeChecked = startTime ?? DateTime.UtcNow - TimeSpan.FromDays(1);
            var prices = await client.GetFuturesPrice(
                symbol,
                timeFrame,
                startTimeChecked,
                endTime ?? DateTime.UtcNow);

            marketdataRequestsTotal.WithLabels("Binance", symbol).Inc();
            return Ok(prices);
        }
        catch (Exception ex)
        {
            marketdataErrorsTotal.WithLabels(ex.GetType().Name, "Binance", symbol).Inc();
            _logger.LogError(ex, "Error getting futures price data");
            return StatusCode(500, new { message = "An error occurred while fetching market data" });
        }
        finally
        {
            timer.Dispose();
        }
    }

    [HttpGet("available-futures")]
    public async Task<ActionResult<IEnumerable<string>>> GetAvailableFutures()
    {
        var timer = marketdataRequestDuration.WithLabels("Binance", "AvailableFutures").NewTimer();
        try
        {
            var client = _exchangeClientFactory.CreateClient("Binance");
            var symbols = await client.GetAvailableFuturesAsync();
            marketdataRequestsAvailableFuturesTotal.WithLabels("Binance", "AvailableFutures").Inc();
            return Ok(symbols);
        }
        catch (Exception ex)
        {
            marketdataAvailableFuturesErrorsTotal.WithLabels(ex.GetType().Name, "Binance", "AvailableFutures").Inc();
            _logger.LogError(ex, "Error getting available futures");
            return StatusCode(500, new { message = "An error occurred while fetching available futures" });
        }
        finally
        {
            timer.Dispose();
        }
    }
}