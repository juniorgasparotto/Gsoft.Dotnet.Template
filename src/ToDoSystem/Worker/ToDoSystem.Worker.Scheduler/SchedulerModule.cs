namespace ToDoSystem.Worker.Scheduler;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Infra.Module.Base;

/// <summary>
/// Registra o agendamento recorrente (RecurringJob.AddOrUpdate) no Hangfire.
/// </summary>
public sealed class SchedulerModule : IWebModule
{
    public void ConfigureHost(WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<SchedulerHostedService>();
    }

    public void Configure(WebApplication app) { }
}
