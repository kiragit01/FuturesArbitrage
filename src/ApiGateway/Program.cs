using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Prometheus;
using Yarp.ReverseProxy.Model;

var builder = WebApplication.CreateBuilder(args);

// Настройка Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Определение пользовательских метрик
var proxyRequestCounter = Metrics.CreateCounter(
    "api_gateway_proxy_requests_total",
    "Total number of proxied requests",
    new CounterConfiguration { LabelNames = new[] { "destination", "status" } }
);

var proxyRequestDuration = Metrics.CreateHistogram(
    "api_gateway_proxy_request_duration_seconds",
    "Duration of proxied requests in seconds",
    new HistogramConfiguration { LabelNames = new[] { "destination" } }
);

var proxyConfigErrors = Metrics.CreateCounter(
    "api_gateway_proxy_config_errors_total",
    "Total number of proxy configuration errors"
);



// Добавление сервисов для конфигурации прокси
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

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

// Пользовательский middleware для замера времени проксирования
app.Use(async (context, next) =>
{
    var destination = context.Request.Path.Value ?? "unknown";
    var timer = proxyRequestDuration.WithLabels(destination).NewTimer();

    try
    {
        await next();
        var status = context.Response.StatusCode.ToString();
        proxyRequestCounter.WithLabels(destination, status).Inc();
    }
    catch (Exception ex)
    {
        proxyRequestCounter.WithLabels(destination, "error").Inc();
        app.Logger.LogError(ex, "Error proxying request to {Destination}", destination);
        throw;
    }
    finally
    {
        timer.ObserveDuration();
    }
});

// Middleware для проверки конфигурации прокси
app.Use(async (context, next) =>
{
    var proxyFeature = context.Features.Get<IReverseProxyFeature>();
    if (proxyFeature == null)
    {
        proxyConfigErrors.Inc();
        app.Logger.LogWarning("Proxy configuration failed for request to {Path}", context.Request.Path);
    }
    await next();
});

app.UseEndpoints(endpoints =>
{
    endpoints.MapReverseProxy();
    endpoints.MapHealthChecks("/health");
});
app.MapGet("/", () => "API Gateway is running!");

app.Run();