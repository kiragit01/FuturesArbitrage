using ArbitrageCalculatorService.Interfaces;
using ArbitrageCalculatorService.Models;
using ArbitrageCalculatorService.Services;
using Polly;
using Polly.Extensions.Http;
using Prometheus;
using Serilog;

namespace ArbitrageCalculatorService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Настройка Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        builder.Host.UseSerilog();

        // Добавление конфигурации
        builder.Services.Configure<ArbitrageSettings>(
            builder.Configuration.GetSection("ArbitrageSettings"));

        // Регистрация сервисов
        builder.Services.AddHttpClient("MarketDataService", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:MarketDataService"]);
            })
            .AddPolicyHandler(GetRetryPolicy());

        builder.Services.AddScoped<IArbitrageCalculator, ArbitrageCalculator>();

        // Добавление контроллеров
        builder.Services.AddControllers();

        // Добавление Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Добавление health checks
        builder.Services.AddHealthChecks();

        var app = builder.Build();
        app.Urls.Add("http://*:80");

        // Настройка middleware
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseSerilogRequestLogging();
        app.UseRouting();
        
        //для метрик Prometheus
        app.UseMetricServer();
        app.UseHttpMetrics();
        
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health");
        });

        app.Run();
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}