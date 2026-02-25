namespace Shared.Infra.Module.Hangfire.Workers;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Infra.Module.Base;
using Shared.Infra.Module.Hangfire.Options;

/// <summary>
/// Worker Executor: Hangfire + Server (processa jobs). Sem Dashboard, sem endpoints GET /jobs.
/// </summary>
public sealed class WorkerExecutorModule(IOptions<WorkerModuleOptions> options, ILogger<WorkerExecutorModule>? logger = null) : IWebModule
{
    private readonly WorkerModuleOptions _options = options.Value;
    private readonly ILogger? _logger = logger;

    public void ConfigureHost(WebApplicationBuilder builder)
    {
        var connectionString = _options.ConnectionString ?? throw new InvalidOperationException("Hangfire ConnectionString undefined");
        HangfireSetup.AddHangfireWithStorage(builder.Services, connectionString, _options.StorageProvider, _logger, addServer: true, _options.WorkerCount);
    }

    public void Configure(WebApplication app) { }
}
