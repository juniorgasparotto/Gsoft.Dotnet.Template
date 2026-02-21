namespace Shared.Infra.Module.Hangfire.Workers;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Infra.Module.Base;
using Shared.Infra.Module.Hangfire.Options;

/// <summary>
/// Worker Scheduler: Hangfire client-only (enfileira/agenda). Sem server, sem Dashboard. Recurring jobs no SchedulerModule do app.
/// </summary>
public sealed class WorkerSchedulerModule(IOptions<WorkerModuleOptions> options, ILogger<WorkerSchedulerModule>? logger = null) : IWebModule
{
    private readonly WorkerModuleOptions _options = options.Value;
    private readonly ILogger? _logger = logger;

    public void ConfigureHost(WebApplicationBuilder builder)
    {
        var connectionString = _options.ConnectionString ?? throw new InvalidOperationException("Hangfire ConnectionString undefined");
        HangfireSetup.AddHangfireWithStorage(builder.Services, connectionString, _options.StorageProvider, _logger, addServer: false);
    }

    public void Configure(WebApplication app) { }
}
