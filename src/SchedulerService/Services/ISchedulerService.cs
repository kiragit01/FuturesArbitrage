namespace SchedulerService.Services;

public interface ISchedulerService
{
    Task TriggerArbitrageCalculationAsync(string firstFuture, string secondFuture, string timeFrame);
}