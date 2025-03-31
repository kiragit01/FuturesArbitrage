using MarketDataService.Controllers;
using MarketDataService.Interfaces;
using MarketDataService.Models;
using MarketDataService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MarketDataService.Tests;

public class GetFuturesPricesErrorHandlingTests
{
    [Fact]
    public async Task GetFuturesPrice_ReturnsPrices_WhenResponseIsValid()
    {
        // Arrange
        var wireMockServer = WireMockServer.Start();
        var mockLogger = new Mock<ILogger<BinanceClient>>();
        var settings = new MarketDataSettings
        {
            BinanceApiUrl = wireMockServer.Urls[0],
            FuturesEndpoint = "/fapi/v1/klines",
            MaxCandlesPerRequest = 1000,
            RequestTimeoutMs = 5000
        };
        var options = Options.Create(settings);

        var expectedResponse = new List<List<object>>
        {
            new List<object> { 1640995200000, "50000", "51000", "49000", "50500", "100" }
        };
        wireMockServer.Given(Request.Create().WithPath("/fapi/v1/klines").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(expectedResponse));

        var httpClient = new HttpClient { BaseAddress = new Uri(wireMockServer.Urls[0]) };
        var client = new BinanceClient(httpClient, options, mockLogger.Object);

        // Act
        var prices = await client.GetFuturesPrice("BTCUSDT", "1h", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);

        // Assert
        Assert.Single(prices);
        Assert.Equal(50500m, prices.First().ClosePrice);

        wireMockServer.Stop();
    }
}