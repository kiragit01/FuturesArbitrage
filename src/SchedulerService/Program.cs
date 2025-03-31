
using SchedulerService.Models;
using SchedulerService.Jobs;
using SchedulerService.Services;
using Quartz;
using Serilog;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http;
using Prometheus;
using Quartz.Spi;

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
        builder.Services.Configure<SchedulerSettings>(
            builder.Configuration.GetSection("SchedulerSettings"));

        // Регистрация HTTP клиентов
        builder.Services.AddHttpClient("ArbitrageCalculatorService",client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:ArbitrageCalculatorService"]);
            }).AddPolicyHandler(GetRetryPolicy(builder.Configuration));

        builder.Services.AddHttpClient("MarketDataService", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:MarketDataService"]);
            }).AddPolicyHandler(GetRetryPolicy(builder.Configuration));
        
        builder.Services.AddHttpClient("DataStorageService", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:DataStorageService"]);
            }).AddPolicyHandler(GetRetryPolicy(builder.Configuration));
        
        // Настройка Quartz
        builder.Services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();

            if (!bool.Parse(builder.Configuration["SchedulerSettings:DisableScheduling"]))
            {
                // Регистрация задачи
                var jobKey = new JobKey("ArbitrageCalculationJob");
                q.AddJob<ArbitrageCalculationJob>(jobOpts => jobOpts.WithIdentity(jobKey));

                // Настройка триггера для задачи
                q.AddTrigger(triggerOpts => triggerOpts
                    .ForJob(jobKey)
                    .WithIdentity("ArbitrageCalculationTrigger")
                    .WithCronSchedule(builder.Configuration["SchedulerSettings:CronExpression"]));
            }
        });

        builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        builder.Services.AddSingleton<IJobFactory, JobFactory>();

        // Добавление сервиса планировщика
        builder.Services.AddSingleton<ISchedulerService, SchedulerService.Services.SchedulerService>();
        builder.Services.AddTransient<ArbitrageCalculationJob>();
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

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IConfiguration config)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(config.GetValue<int>("SchedulerSettings:RetryAttemptsCount"),
                retryAttempt => TimeSpan.FromSeconds(config.GetValue<int>("SchedulerSettings:RetryDelayInSeconds")));
    }
}