using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using MarketDataService.Interfaces;
using MarketDataService.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketDataService.Services
{
    public class BinanceClient : IExchangeClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BinanceClient> _logger;
        private readonly MarketDataSettings _settings;

        public BinanceClient(
            HttpClient httpClient,
            IOptions<MarketDataSettings> options,
            ILogger<BinanceClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _httpClient.Timeout = TimeSpan.FromMilliseconds(_settings.RequestTimeoutMs);
            _httpClient.BaseAddress = new Uri(_settings.BinanceApiUrl);
        }

        public async Task<IEnumerable<FuturePrice>> GetFuturesPrice(string symbol, string timeFrame, DateTime startTime, DateTime endTime)
        {
            try
            {
                var interval = MapTimeFrameToInterval(timeFrame);
                var startTimestamp = ((DateTimeOffset)startTime.ToUniversalTime()).ToUnixTimeMilliseconds();
                var endTimestamp = ((DateTimeOffset)endTime.ToUniversalTime()).ToUnixTimeMilliseconds();

                var requestUri = $"{_settings.FuturesEndpoint}" +
                                 $"?symbol={symbol}" +
                                 $"&interval={interval}" +
                                 $"&startTime={startTimestamp}" +
                                 $"&endTime={endTimestamp}" +
                                 $"&limit={_settings.MaxCandlesPerRequest}";
                
                _logger.LogInformation("Requesting data from Binance: {RequestUri}", requestUri);
                
                var response = await _httpClient.GetFromJsonAsync<List<List<JsonElement>>>(requestUri);
                
                if (response == null || response.Count == 0)
                {
                    _logger.LogWarning("No data returned from Binance for {Symbol}", symbol);
                    return Array.Empty<FuturePrice>();
                }
                
                var prices = response.Select(candle => new FuturePrice
                {
                    Symbol = symbol,
                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(candle[0].GetInt64()).UtcDateTime,
                    OpenPrice = decimal.Parse(candle[1].GetString(), CultureInfo.InvariantCulture),
                    HighPrice = decimal.Parse(candle[2].GetString(), CultureInfo.InvariantCulture),
                    LowPrice = decimal.Parse(candle[1].GetString(), CultureInfo.InvariantCulture),
                    ClosePrice = decimal.Parse(candle[4].GetString(), CultureInfo.InvariantCulture),
                    Volume = decimal.Parse(candle[5].GetString(), CultureInfo.InvariantCulture)
                }).ToList();

                _logger.LogInformation("Retrieved {Count} price records for {Symbol}", prices.Count, symbol);
                
                return prices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting futures price data from Binance");
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetAvailableFuturesAsync()
        {
            try
            {
                var requestUri = _settings.AvailableFuturesEndpoint;
                var response = await _httpClient.GetFromJsonAsync<ExchangeInfoResponse>(requestUri);
                if (response == null)
                {
                    _logger.LogError("Failed to retrieve exchange info from Binance");
                    throw new Exception("Exchange info is null");
                }
                var quarterlyFutures = response.symbols
                    .Where(s => s.status == "TRADING" && 
                                (s.contractType == "CURRENT_QUARTER" || s.contractType == "NEXT_QUARTER"))
                    .Select(s => s.symbol)
                    .ToList();
        
                _logger.LogInformation("Retrieved {Count} available quarterly futures symbols", quarterlyFutures.Count);
        
                return quarterlyFutures;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available futures symbols from Binance");
                throw;
            }
        }

        private string MapTimeFrameToInterval(string timeFrame)
        {
            return timeFrame.ToLower() switch
            {
                "1m" => "1m",
                "3m" => "3m",
                "5m" => "5m",
                "15m" => "15m",
                "30m" => "30m",
                "1h" => "1h",
                "2h" => "2h",
                "4h" => "4h",
                "6h" => "6h",
                "8h" => "8h",
                "12h" => "12h",
                "1d" => "1d",
                "3d" => "3d",
                "1w" => "1w",
                "1M" or "1mth" => "1M",
                _ => "1h"
            };
        }
    }
}