using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yarp.ReverseProxy.Configuration;

namespace ApiGateway.Tests;

public class ApiGatewayTests
{
    [Fact]
    public void ReverseProxy_ShouldLoadRoutesFromConfig()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "ReverseProxy:Routes:marketData:ClusterId", "marketDataCluster" },
                { "ReverseProxy:Routes:marketData:Match:Path", "/api/marketdata/{**catch-all}" },
                { "ReverseProxy:Clusters:marketDataCluster:Destinations:destination1:Address", "http://marketdataservice:80" }
            })
            .Build();

        var services = new ServiceCollection();
        services.AddReverseProxy().LoadFromConfig(configuration.GetSection("ReverseProxy"));
        var provider = services.BuildServiceProvider();

        // Act
        var proxyConfigProvider = provider.GetRequiredService<IProxyConfigProvider>();
        var config = proxyConfigProvider.GetConfig();

        // Assert
        var route = config.Routes.FirstOrDefault(r => r.RouteId == "marketData");
        Assert.NotNull(route);
        Assert.Equal("marketDataCluster", route.ClusterId);
        Assert.Equal("/api/marketdata/{**catch-all}", route.Match.Path);
    }
}