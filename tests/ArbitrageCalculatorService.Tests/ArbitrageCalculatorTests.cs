using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using ArbitrageCalculatorService.Services;
using ArbitrageCalculatorService.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ArbitrageCalculatorService.Tests;

public class ArbitrageCalculatorTests
{
    [Fact]
    public void CalculateSpread_FillsMissingData_WithLastObservation()
    {
        // Arrange: подготовка двух серий цен с пропуском у второй серии
        var baseTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var firstPrices = new List<FuturePrice>
        {
            new FuturePrice { Symbol = "TEST1", Timestamp = baseTime, ClosePrice = 100 },
            new FuturePrice { Symbol = "TEST1", Timestamp = baseTime.AddMinutes(5), ClosePrice = 105 },
            new FuturePrice { Symbol = "TEST1", Timestamp = baseTime.AddMinutes(10), ClosePrice = 110 }
        };
        var secondPrices = new List<FuturePrice>
        {
            new FuturePrice { Symbol = "TEST2", Timestamp = baseTime, ClosePrice = 90 },
            // Пропуск данных на отметке baseTime + 5 минут для второго символа
            new FuturePrice { Symbol = "TEST2", Timestamp = baseTime.AddMinutes(10), ClosePrice = 95 }
        };

        // Создаем экземпляр ArbitrageCalculator с заглушками зависимостей
        var httpClientFactory = new DummyHttpClientFactory();
        var options = Options.Create(new ArbitrageCalculatorService.Models.ArbitrageSettings());
        var logger = NullLogger<ArbitrageCalculator>.Instance;
        var calculator = new ArbitrageCalculator(httpClientFactory, options, logger);

        // Act: вызываем приватный метод CalculateSpread через рефлексию (для тестирования внутренней логики)
        var methodInfo = typeof(ArbitrageCalculator).GetMethod("CalculateSpread", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(methodInfo);
        var result = methodInfo.Invoke(calculator, new object[] { firstPrices, secondPrices }) as List<SpreadData>;

        // Assert: проверяем, что пропущенные данные заполнены и спред рассчитан корректно
        Assert.NotNull(result);
        // Ожидается 3 точки во времени: 0 мин, 5 мин, 10 мин
        Assert.Equal(3, result.Count);

        // Найдем данные спреда на 5-й минуте
        var spreadAt5 = result.Single(x => x.Timestamp == baseTime.AddMinutes(5));
        var spreadAt0 = result.Single(x => x.Timestamp == baseTime);

        // Цена второго символа на 5-й минуте должна быть взята с 0-й (последняя известная)
        Assert.Equal(spreadAt0.SecondPrice, spreadAt5.SecondPrice);
        // Цена первого символа на 5-й минуте должна соответствовать заданной (105)
        Assert.Equal(105m, spreadAt5.FirstPrice);
        // Спред на 5-й минуте: 105 - 90 = 15
        Assert.Equal(15m, spreadAt5.Spread);
        // Процентный спред на 5-й минуте: 15/90 * 100 ≈ 16.67%
        Assert.True(Math.Abs(spreadAt5.PercentageSpread - (15m / 90m * 100m)) < 0.0001m);
    }

    // Вспомогательный класс-заглушка для IHttpClientFactory (не используется в данном тесте)
    private class DummyHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new System.Net.Http.HttpClient();
    }
}