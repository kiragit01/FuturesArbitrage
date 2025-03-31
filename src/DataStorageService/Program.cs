using DataStorageService.Data;
using DataStorageService.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog;

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

        // Настройка DbContext
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Регистрация репозитория
        builder.Services.AddScoped<IArbitrageRepository, ArbitrageRepository>();

        // Добавление контроллеров
        builder.Services.AddControllers();

        // Добавление Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Добавление health checks
        builder.Services.AddHealthChecks()
            .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection"));

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

        // Применяем миграции при запуске
        ApplyMigrations(app);

        app.Run();
    }

    private static void ApplyMigrations(WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.Migrate();
        }
    }
}