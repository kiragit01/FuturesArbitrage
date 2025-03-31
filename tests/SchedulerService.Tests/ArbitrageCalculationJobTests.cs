using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Prometheus;
using SchedulerService.Jobs;
using SchedulerService.Models;
using Xunit;

namespace SchedulerService.Tests;

public class ArbitrageCalculationJobTests
{
    [Fact]
    public async Task Execute_WithValidFutures_ReturnsExpectedSymbols()
    {
        // Arrange
        var httpClientFactory = new Mock<IHttpClientFactory>();
        var marketDataClient = new HttpClient(new MockHttpMessageHandler(new List<string>
        {
            "BTCUSDT_250228",
            "BTCUSDT_250531",
            "BTCUSDT_250829"
        }));
        var arbitrageClient = new HttpClient(new MockHttpMessageHandler(new ArbitrageResult()));
        var storageClient = new HttpClient(new MockHttpMessageHandler(null));

        httpClientFactory.Setup(f => f.CreateClient("MarketDataService")).Returns(marketDataClient);
        httpClientFactory.Setup(f => f.CreateClient("ArbitrageCalculatorService")).Returns(arbitrageClient);
        httpClientFactory.Setup(f => f.CreateClient("DataStorageService")).Returns(storageClient);

        var settings = new SchedulerSettings
        {
            MarketDataEndpoint = "http://test/marketdata",
            ArbitrageEndpoint = "http://test/arbitrage",
            DataArbitrageBatchEndpoint = "http://test/storage",
            Asset = "BTC",
            TimeFrame = "1h"
        };
        var options = Options.Create(settings);
        var logger = NullLogger<ArbitrageCalculationJob>.Instance;
        var job = new ArbitrageCalculationJob(httpClientFactory.Object, options, logger);

        // Act
        await job.Execute(null);

        // Assert
        httpClientFactory.Verify(f => f.CreateClient("ArbitrageCalculatorService").GetAsync(
                It.Is<string>(uri => uri.Contains("BTCUSDT_250228") && uri.Contains("BTCUSDT_250531"))),
            Times.Once());
    }
    
    [Fact]
    public async Task Execute_SuccessfulRun_IncrementsSuccessCounter()
    {
        // Arrange
        var httpClientFactory = new Mock<IHttpClientFactory>();

        // Mock MarketDataService
        var marketDataClient = new HttpClient(new MockHttpMessageHandler(new List<string>
        {
            "BTCUSDT_250228",
            "BTCUSDT_250531"
        }));
        httpClientFactory.Setup(f => f.CreateClient("MarketDataService")).Returns(marketDataClient);

        // Mock ArbitrageCalculatorService
        var arbitrageClient = new HttpClient(new MockHttpMessageHandler(new ArbitrageResult
        {
            FirstSymbol = "BTCUSDT_250228",
            SecondSymbol = "BTCUSDT_250531",
            SpreadData = new List<SpreadData> { new SpreadData { Spread = 10 } }
        }));
        httpClientFactory.Setup(f => f.CreateClient("ArbitrageCalculatorService")).Returns(arbitrageClient);

        // Mock DataStorageService
        var storageClient = new HttpClient(new MockHttpMessageHandler(null));
        httpClientFactory.Setup(f => f.CreateClient("DataStorageService")).Returns(storageClient);

        var settings = new SchedulerSettings
        {
            MarketDataEndpoint = "http://test/marketdata",
            ArbitrageEndpoint = "http://test/arbitrage",
            DataArbitrageBatchEndpoint = "http://test/storage",
            Asset = "BTC",
            TimeFrame = "1h"
        };
        var options = Options.Create(settings);
        var logger = NullLogger<ArbitrageCalculationJob>.Instance;
        var job = new ArbitrageCalculationJob(httpClientFactory.Object, options, logger);

        // Act
        double initialSuccessCount = Metrics.CreateCounter("scheduler_job_successes_total", "").Value;
        await job.Execute(null);

        // Assert
        double finalSuccessCount = Metrics.CreateCounter("scheduler_job_successes_total", "").Value;
        Assert.Equal(initialSuccessCount + 1, finalSuccessCount);
    }
    
    [Fact]
    public async Task Execute_FailureRun_IncrementsFailureCounter()
    {
        // Arrange
        var httpClientFactory = new Mock<IHttpClientFactory>();

        // Mock MarketDataService
        var marketDataClient = new HttpClient(new MockHttpMessageHandler(new List<string>
        {
            "BTCUSDT_250228",
            "BTCUSDT_250531"
        }));
        httpClientFactory.Setup(f => f.CreateClient("MarketDataService")).Returns(marketDataClient);

        // Mock ArbitrageCalculatorService с ошибкой
        var arbitrageClient = new HttpClient(new MockHttpMessageHandler(null));
        arbitrageClient.BaseAddress = new Uri("http://test/");
        httpClientFactory.Setup(f => f.CreateClient("ArbitrageCalculatorService")).Returns(arbitrageClient);
        arbitrageClient.DefaultRequestHeaders.Accept.Clear();
        arbitrageClient.SendAsync(new HttpRequestMessage());

        var settings = new SchedulerSettings
        {
            MarketDataEndpoint = "http://test/marketdata",
            ArbitrageEndpoint = "http://test/arbitrage",
            Asset = "BTC",
            TimeFrame = "1h"
        };
        var options = Options.Create(settings);
        var logger = NullLogger<ArbitrageCalculationJob>.Instance;
        var job = new ArbitrageCalculationJob(httpClientFactory.Object, options, logger);

        // Act
        double initialFailureCount = Metrics.CreateCounter("scheduler_job_failures_total", "").Value;
        await Assert.ThrowsAsync<Exception>(() => job.Execute(null));

        // Assert
        double finalFailureCount = Metrics.CreateCounter("scheduler_job_failures_total", "").Value;
        Assert.Equal(initialFailureCount + 1, finalFailureCount);
    }
    
    // Обновленный MockHttpMessageHandler для поддержки разных типов ответов
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly object _responseData;

        public MockHttpMessageHandler(object responseData)
        {
            _responseData = responseData;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = _responseData != null ? JsonSerializer.Serialize(_responseData) : "";
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            };
        }
    }
}