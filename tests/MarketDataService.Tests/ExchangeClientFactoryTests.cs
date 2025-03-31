using MarketDataService.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MarketDataService.Tests;

public class ExchangeClientFactoryTests
{
    [Fact]
    public void CreateClient_ThrowsException_ForUnsupportedExchange()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var mockLogger = new Mock<ILogger<ExchangeClientFactory>>();
        var factory = new ExchangeClientFactory(serviceProvider, mockLogger.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => factory.CreateClient("UnsupportedExchange"));
        Assert.Equal("Exchange UnsupportedExchange is not supported", exception.Message);
    }
}