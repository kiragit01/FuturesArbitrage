using DataStorageService.Models;
using Microsoft.EntityFrameworkCore;

namespace DataStorageService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ArbitrageData> ArbitrageData { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ArbitrageData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.FirstSymbol).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SecondSymbol).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TimeFrame).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Timestamp).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)).IsRequired();
            entity.Property(e => e.FirstPrice).IsRequired().HasColumnType("decimal(18,8)");
            entity.Property(e => e.SecondPrice).IsRequired().HasColumnType("decimal(18,8)");
            entity.Property(e => e.Spread).IsRequired().HasColumnType("decimal(18,8)");
            entity.Property(e => e.PercentageSpread).HasColumnType("decimal(18,8)");
            entity.Property(e => e.CreatedAt).HasConversion(
                    v => v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc)).IsRequired();
                
            // Создаем индекс для оптимизации запросов
            entity.HasIndex(e => new { e.FirstSymbol, e.SecondSymbol, e.TimeFrame, e.Timestamp });
        });
    }
}