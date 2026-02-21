namespace Shared.Infra.Module.Hangfire.Workers;

using global::Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Infra.Module.Base;
using Shared.Infra.Module.Hangfire.Options;

/// <summary>
/// Worker Dashboard: Hangfire client + UI do Dashboard. Sem server.
/// </summary>
public sealed class WorkerDashboardModule(IOptions<WorkerModuleOptions> options, ILogger<WorkerDashboardModule>? logger = null) : IWebModule
{
    private readonly WorkerModuleOptions _options = options.Value;
    private readonly ILogger? _logger = logger;

    public void ConfigureHost(WebApplicationBuilder builder)
    {
        var connectionString = _options.ConnectionString ?? throw new InvalidOperationException("Hangfire ConnectionString undefined");
        HangfireSetup.AddHangfireWithStorage(builder.Services, connectionString, _options.StorageProvider, _logger, addServer: false);
    }

    public void Configure(WebApplication app)
    {
        app.UseHangfireDashboard(_options.DashboardPath, new DashboardOptions { DashboardTitle = "Hangfire" });
    }
}
