using System;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;

namespace SchedulerService.Jobs;

public class JobFactory : IJobFactory
{
    private readonly IServiceProvider _serviceProvider;

    public JobFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        var jobType = bundle.JobDetail.JobType;
            
        // Создаем скоуп для получения экземпляра задачи
        using var scope = _serviceProvider.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService(jobType) as IJob;
            
        return job;
    }

    public void ReturnJob(IJob job)
    {
        (job as IDisposable)?.Dispose();
    }
}