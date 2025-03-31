using MarketDataService.Models;
using MarketDataService.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MarketDataService.Tests;

public class GetAvailableFuturesTests
{
    [Fact]
    public async Task GetAvailableFuturesAsync_ReturnsSymbols_WhenResponseIsValid()
    {
        // Arrange
        var wireMockServer = WireMockServer.Start();
        var mockLogger = new Mock<ILogger<BinanceClient>>();
        var settings = new MarketDataSettings
        {
            BinanceApiUrl = wireMockServer.Urls[0],
            AvailableFuturesEndpoint = "/fapi/v1/exchangeInfo",
            RequestTimeoutMs = 5000
        };
        var options = Options.Create(settings);

        var expectedResponse = new ExchangeInfoResponse
        {
            symbols = new List<SymbolInfo>
            {
                new SymbolInfo { symbol = "BTCUSDT_250228", status = "TRADING", contractType = "CURRENT_QUARTER" },
                new SymbolInfo { symbol = "BTCUSDT_250531", status = "TRADING", contractType = "NEXT_QUARTER" }
            }
        };
        wireMockServer.Given(Request.Create().WithPath("/fapi/v1/exchangeInfo").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(expectedResponse));

        var httpClient = new HttpClient { BaseAddress = new Uri(wireMockServer.Urls[0]) };
        var client = new BinanceClient(httpClient, options, mockLogger.Object);

        // Act
        var symbols = await client.GetAvailableFuturesAsync();

        // Assert
        Assert.Equal(2, symbols.Count());
        Assert.Contains("BTCUSDT_250228", symbols);
        Assert.Contains("BTCUSDT_250531", symbols);

        wireMockServer.Stop();
    }
}