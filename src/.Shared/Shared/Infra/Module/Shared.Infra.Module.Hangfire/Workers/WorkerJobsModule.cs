namespace Shared.Infra.Module.Hangfire.Workers;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Infra.Module.Base;
using Shared.Infra.Module.Hangfire.Options;

/// <summary>
/// Worker Jobs: Hangfire client (storage) + GET /jobs e GET /jobs/{name}. Sem server, sem Dashboard.
/// </summary>
public sealed class WorkerJobsModule(IOptions<WorkerModuleOptions> options, ILogger<WorkerJobsModule>? logger = null) : IWebModule
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
        HangfireSetup.RegisterJobEndpoints(app);
    }
}
