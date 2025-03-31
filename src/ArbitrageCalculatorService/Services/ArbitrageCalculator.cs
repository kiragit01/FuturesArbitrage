using ArbitrageCalculatorService.Interfaces;
using ArbitrageCalculatorService.Models;
using Microsoft.Extensions.Options;

namespace ArbitrageCalculatorService.Services;

public class ArbitrageCalculator : IArbitrageCalculator
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ArbitrageCalculator> _logger;
    private readonly ArbitrageSettings _settings;
    
    public ArbitrageCalculator(
        IHttpClientFactory httpClientFactory,
        IOptions<ArbitrageSettings> options,
        ILogger<ArbitrageCalculator> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<ArbitrageResult> CalculateArbitrage(
        string firstSymbol, 
        string secondSymbol, 
        string timeFrame, 
        DateTime startTime, 
        DateTime endTime)
    {
        try
        {
            _logger.LogInformation("Fetching price data for {FirstSymbol} and {SecondSymbol}", firstSymbol, secondSymbol);
            
            var client = _httpClientFactory.CreateClient("MarketDataService");
                
            var firstPricesTask = GetPriceData(client, firstSymbol, timeFrame, startTime, endTime);
            var secondPricesTask = GetPriceData(client, secondSymbol, timeFrame, startTime, endTime);
                
            await Task.WhenAll(firstPricesTask, secondPricesTask);
                
            var firstPrices = await firstPricesTask;
            var secondPrices = await secondPricesTask;
                
            _logger.LogInformation("Retrieved {FirstCount} price records for {FirstSymbol} and {SecondCount} for {SecondSymbol}",
                firstPrices.Count(), firstSymbol, secondPrices.Count(), secondSymbol);

            // все необходимые временные метки на основе timeFrame от startTime до endTime
            var allTimestamps = GenerateTimestamps(timeFrame, startTime, endTime);
            
            var arbitrageData = CalculateSpread(firstPrices.ToList(), secondPrices.ToList(), allTimestamps);
                
            return new ArbitrageResult
            {
                FirstSymbol = firstSymbol,
                SecondSymbol = secondSymbol,
                TimeFrame = timeFrame,
                StartTime = startTime,
                EndTime = endTime,
                SpreadData = arbitrageData,
                MinSpread = arbitrageData.Any() ? arbitrageData.Min(d => d.Spread) : 0,
                MaxSpread = arbitrageData.Any() ? arbitrageData.Max(d => d.Spread) : 0,
                AverageSpread = arbitrageData.Any() ? arbitrageData.Average(d => d.Spread) : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating arbitrage between {FirstSymbol} and {SecondSymbol}", 
                firstSymbol, secondSymbol);
            throw;
        }
    }

    private async Task<IEnumerable<FuturePrice>> GetPriceData(
        HttpClient client, 
        string symbol, 
        string timeFrame, 
        DateTime startTime, 
        DateTime endTime)
    {
        var requestUri = $"{_settings.MarketDataEndpoint}/{symbol}/{timeFrame}" +
                         $"?startTime={startTime:O}&endTime={endTime:O}";
            
        _logger.LogInformation("Requesting data from Market Data Service: {RequestUri}", requestUri);
        var response = await client.GetAsync(requestUri);
            
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error fetching market data: {response.StatusCode}, {errorContent}");
        }
        
        var prices = await response.Content.ReadFromJsonAsync<IEnumerable<FuturePrice>>();
        return prices ?? Enumerable.Empty<FuturePrice>();
    }

    private List<SpreadData> CalculateSpread(
        IList<FuturePrice> firstPrices,
        IList<FuturePrice> secondPrices,
        IList<DateTime> allTimestamps)
    {
        var result = new List<SpreadData>();
        int indexFirst = 0;
        int indexSecond = 0;
        decimal? lastFirstPrice = null;
        decimal? lastSecondPrice = null;
        
        // последние известные цены перед началом цикла
        while (indexFirst < firstPrices.Count && firstPrices[indexFirst].Timestamp <= allTimestamps[0])
        {
            lastFirstPrice = firstPrices[indexFirst].ClosePrice;
            indexFirst++;
        }
        while (indexSecond < secondPrices.Count && secondPrices[indexSecond].Timestamp <= allTimestamps[0])
        {
            lastSecondPrice = secondPrices[indexSecond].ClosePrice;
            indexSecond++;
        }
        
        // Итерация по каждой временной метке
        foreach (var timestamp in allTimestamps)
        {
            // Обновление lastFirstPrice всеми новыми ценами до этой временной метки
            while (indexFirst < firstPrices.Count && firstPrices[indexFirst].Timestamp <= timestamp)
            {
                lastFirstPrice = firstPrices[indexFirst].ClosePrice;
                indexFirst++;
            }
            
            // Обновление lastSecondPrice всеми новыми ценами до этой временной метки
            while (indexSecond < secondPrices.Count && secondPrices[indexSecond].Timestamp <= timestamp)
            {
                lastSecondPrice = secondPrices[indexSecond].ClosePrice;
                indexSecond++;
            }
            
            // Установка текущих цен
            decimal? currentFirstPrice = lastFirstPrice;
            decimal? currentSecondPrice = lastSecondPrice;
            
            // Если доступны обе цены, рассчитайте спред
            if (currentFirstPrice == null || currentSecondPrice == null) continue;
            decimal spread = currentFirstPrice.Value - currentSecondPrice.Value;
            decimal percentageSpread = (spread / currentSecondPrice.Value) * 100;
            result.Add(new SpreadData
            {
                Timestamp = timestamp,
                FirstPrice = currentFirstPrice.Value,
                SecondPrice = currentSecondPrice.Value,
                Spread = spread,
                PercentageSpread = percentageSpread
            });
            // Иначе пропускаем эту метку времени (нет данных для одного или обоих символов).
        }
        return result;
    }

    private List<DateTime> GenerateTimestamps(string timeFrame, DateTime startTime, DateTime endTime)
    {
        var interval = ParseTimeFrame(timeFrame);
        var timestamps = new List<DateTime>();
        var current = startTime;
        while (current <= endTime)
        {
            timestamps.Add(current);
            current = current.Add(interval);
        }
        return timestamps;
    }

    private TimeSpan ParseTimeFrame(string timeFrame)
    {
        if (string.IsNullOrWhiteSpace(timeFrame))
            throw new ArgumentException("Time frame cannot be empty or null");

        timeFrame = timeFrame.Trim();
    
        // Извлекаем число и единицу измерения
        var numberPart = timeFrame.Substring(0, timeFrame.Length - 1);
        var unit = timeFrame[^1]; // Последний символ
        if (!int.TryParse(numberPart, out var value) || value <= 0)
            throw new FormatException("Invalid time frame format: number must be a positive integer");

        return unit switch
        {
            'm' => TimeSpan.FromMinutes(value),  // Минуты
            'h' => TimeSpan.FromHours(value),    // Часы
            'd' => TimeSpan.FromDays(value),     // Дни
            'w' => TimeSpan.FromDays(value * 7), // Недели (1w = 7 дней)
            'M' => TimeSpan.FromDays(value * 30), // Месяцы (30 дней)
            _ => throw new FormatException("Invalid time frame unit. Use 'm' (minutes), 'h' (hours), 'd' (days), 'w' (weeks), or 'M' (months)")
        };
    }
}