using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;
using Xunit;

namespace ArbitrageCalculatorService.Tests;

public class ArbitrageControllerRequestTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ArbitrageControllerRequestTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CalculateArbitrage_ReturnsOk_WithValidParameters()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/arbitrage/calculate?firstSymbol=BTCUSDT&secondSymbol=ETHUSDT&timeFrame=1h&startTime=2025-01-01T00:00:00Z");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}