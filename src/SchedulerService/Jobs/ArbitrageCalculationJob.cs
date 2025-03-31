using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Prometheus;
using Quartz;
using SchedulerService.Models;

namespace SchedulerService.Jobs;

[DisallowConcurrentExecution]
public class ArbitrageCalculationJob : IJob
{
    private static readonly Counter jobSuccesses = Metrics.CreateCounter("scheduler_job_successes_total", "Number of successful scheduler jobs");
    private static readonly Counter jobFailures = Metrics.CreateCounter("scheduler_job_failures_total", "Number of failed scheduler jobs");
    private static readonly Histogram jobDuration = Metrics.CreateHistogram("scheduler_job_duration_seconds", "Duration of scheduler jobs");
    
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ArbitrageCalculationJob> _logger;
    private readonly SchedulerSettings _settings;
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ArbitrageCalculationJob(
        IHttpClientFactory httpClientFactory,
        IOptions<SchedulerSettings> options,
        ILogger<ArbitrageCalculationJob> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var timer = jobDuration.NewTimer();
        try
        {
            _logger.LogInformation("Starting arbitrage calculation job");
            
            // Получаем список доступных фьючерсов
            var (firstFuture, secondFuture) = await GetAvailableFuturesAsync();
            // Получаем текущее время и время начала периода
            var endTime = DateTime.UtcNow;
            var startTime = DetermineStartTime(endTime);
                
            _logger.LogInformation("Calculating arbitrage for period: {StartTime} to {EndTime}", 
                startTime, endTime);

            // Получаем данные из сервиса ArbitrageCalculator
            var arbitrageClient = _httpClientFactory.CreateClient("ArbitrageCalculatorService");
                
            var requestUri = $"{_settings.ArbitrageEndpoint}" +
                             $"?firstSymbol={firstFuture}" +
                             $"&secondSymbol={secondFuture}" +
                             $"&timeFrame={_settings.TimeFrame}" +
                             $"&startTime={startTime:yyyy-MM-ddTHH:mm:ss}" +
                             $"&endTime={endTime:yyyy-MM-ddTHH:mm:ss}";
                
            _logger.LogInformation("Requesting arbitrage calculation: {RequestUri}", requestUri);
                
            var response = await arbitrageClient.GetAsync(requestUri);
                
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error calculating arbitrage: {response.StatusCode}, {errorContent}");
            }
                
            var arbitrageResult = await response.Content.ReadFromJsonAsync<ArbitrageResult>();
                
            // Сохраняем данные в DataStorage
            await SaveArbitrageData(arbitrageResult);
            jobSuccesses.Inc();
            _logger.LogInformation("Arbitrage calculation job completed successfully");
        }
        catch (Exception ex)
        {
            jobFailures.Inc();
            _logger.LogError(ex, "Error executing arbitrage calculation job");
            throw;
        }
        finally
        {
            timer.ObserveDuration();
        }
    }

    private async Task<(string,string)> GetAvailableFuturesAsync()
    {
        try
        {
            var marketDataClient = _httpClientFactory.CreateClient("MarketDataService");
            var responseMarketData = await marketDataClient.GetAsync(_settings.MarketDataEndpoint);
            if (!responseMarketData.IsSuccessStatusCode)
            {
                var errorContent = await responseMarketData.Content.ReadAsStringAsync();
                throw new Exception(
                    $"Ошибка получения доступных фьючерсов: {responseMarketData.StatusCode}, {errorContent}");
            }

            var availableFutures = await responseMarketData.Content.ReadFromJsonAsync<List<string>>();
            
            // Фильтруем фьючерсы для указанного актива (например, BTC)
            var assetFutures = availableFutures
                .Where(f => f.StartsWith(_settings.Asset + "USDT_"))
                .ToList();

            // Парсим даты истечения и выбираем два ближайших
            var parsedFutures = new List<(string symbol, DateTime expiration)>();
            foreach (var future in assetFutures)
            {
                var parts = future?.Split('_');
                if (parts?.Length != 2) continue;
                var dateStr = parts[1];
                if (dateStr.Length != 6) continue;
                var year = "20" + dateStr.Substring(0, 2);
                var month = dateStr.Substring(2, 2);
                var day = dateStr.Substring(4, 2);
                if (!int.TryParse(year, out var y) || y < 2000 || y > 2100) continue;
                if (!int.TryParse(month, out var m) || m < 1 || m > 12) continue;
                if (!int.TryParse(day, out var d) || d < 1 || d > 31) continue;
                var expiration = new DateTime(y, m, d);
                parsedFutures.Add((future, expiration)!);
            }

            var futureFutures = parsedFutures
                .Where(f => f.expiration > DateTime.UtcNow)
                .OrderBy(f => f.expiration)
                .ToList();

            if (futureFutures.Count < 2)
            {
                throw new Exception("Недостаточно фьючерсов для расчёта");
            }

            var firstFuture = futureFutures[0].symbol;
            var secondFuture = futureFutures[1].symbol;
            return (firstFuture, secondFuture);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получение доступных фьючерсов");
            throw;
        }
    }

    private DateTime DetermineStartTime(DateTime endTime)
    {
        // Определяем время начала периода на основе настроек
        return _settings.TimeFrame.ToLower() switch
        {
            "1h" => endTime.AddHours(-1),
            "4h" => endTime.AddHours(-4),
            "1d" => endTime.AddDays(-1),
            "1w" => endTime.AddDays(-7),
            _ => endTime.AddHours(-1) // По умолчанию 1 час
        };
    }

    private async Task SaveArbitrageData(ArbitrageResult arbitrageResult)
    {
        var storageClient = _httpClientFactory.CreateClient("DataStorageService");
        var arbitrageDataList = arbitrageResult.SpreadData
            .Select(spreadData => new ArbitrageData
            {
                Id = Guid.NewGuid(),
                FirstSymbol = arbitrageResult.FirstSymbol,
                SecondSymbol = arbitrageResult.SecondSymbol,
                TimeFrame = arbitrageResult.TimeFrame,
                Timestamp = spreadData.Timestamp,
                FirstPrice = spreadData.FirstPrice,
                SecondPrice = spreadData.SecondPrice,
                Spread = spreadData.Spread,
                PercentageSpread = spreadData.PercentageSpread,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        var content = new StringContent(
            JsonSerializer.Serialize(arbitrageDataList, _jsonOptions),
            Encoding.UTF8,
            "application/json");
    
        var response = await storageClient.PostAsync(_settings.DataArbitrageBatchEndpoint, content);
    
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error saving arbitrage data batch: {StatusCode}, {ErrorContent}", 
                response.StatusCode, errorContent);
        }
    }
}