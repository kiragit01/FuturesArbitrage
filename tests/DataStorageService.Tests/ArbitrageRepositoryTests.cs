using DataStorageService.Data;
using DataStorageService.Data.Repositories;
using DataStorageService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DataStorageService.Tests;

public class ArbitrageRepositoryTests
{
    [Fact]
    public async Task SaveArbitrageDataAsync_AddsDataSuccessfully()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;
        var context = new AppDbContext(options);
        var logger = new NullLogger<ArbitrageRepository>();
        var repository = new ArbitrageRepository(context, logger);
        var data = new ArbitrageData { Id = Guid.NewGuid(), FirstSymbol = "BTC", SecondSymbol = "ETH", Spread = 10 };

        // Act
        await repository.SaveArbitrageDataBatchAsync(new List<ArbitrageData> { data });
        var savedData = await context.ArbitrageData.FirstOrDefaultAsync(d => d.Id == data.Id);

        // Assert
        Assert.NotNull(savedData);
        Assert.Equal(data.Spread, savedData.Spread);
    }
}