using System.Net.Http;
using MarketDataService.Interfaces;
using MarketDataService.Models;
using MarketDataService.Services;
using Polly;
using Polly.Extensions.Http;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Настройка Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Добавление конфигурации
builder.Services.Configure<MarketDataSettings>(
    builder.Configuration.GetSection("MarketDataSettings"));

// Регистрация сервисов
builder.Services.AddHttpClient<BinanceClient>()
    .AddPolicyHandler(GetRetryPolicy());

builder.Services.AddSingleton<IExchangeClientFactory, ExchangeClientFactory>();
builder.Services.AddScoped<IExchangeClient, BinanceClient>(); 

// Добавление контроллеров и Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Добавление health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

app.Urls.Add("http://*:80");

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

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}