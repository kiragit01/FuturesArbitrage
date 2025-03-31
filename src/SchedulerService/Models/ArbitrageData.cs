namespace SchedulerService.Models;

public class ArbitrageData
{
    public Guid Id { get; set; }
    public string FirstSymbol { get; set; }
    public string SecondSymbol { get; set; }
    public string TimeFrame { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal FirstPrice { get; set; }
    public decimal SecondPrice { get; set; }
    public decimal Spread { get; set; }
    public decimal PercentageSpread { get; set; }
    public DateTime CreatedAt { get; set; }
}