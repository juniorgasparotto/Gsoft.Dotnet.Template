namespace ToDoSystem.Worker.Scheduler;

using global::Hangfire;
using global::Hangfire.Common;
using Microsoft.Extensions.Hosting;
using ToDoSystem.Worker.Jobs.Jobs;

/// <summary>
/// Registra os recurring jobs no Hangfire usando IRecurringJobManager (API baseada em DI).
/// O static RecurringJob exige JobStorage já inicializado; com IRecurringJobManager o storage vem do container.
/// </summary>
public sealed class SchedulerHostedService : IHostedService
{
    private readonly IRecurringJobManager _recurringJobManager;

    public SchedulerHostedService(IRecurringJobManager recurringJobManager)
    {
        _recurringJobManager = recurringJobManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _recurringJobManager.AddOrUpdate(
            "example",
            Job.FromExpression<ExampleJob>(x => x.ExecuteHangFireAsync(null!)),
            Cron.Minutely(),
            new RecurringJobOptions());

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
