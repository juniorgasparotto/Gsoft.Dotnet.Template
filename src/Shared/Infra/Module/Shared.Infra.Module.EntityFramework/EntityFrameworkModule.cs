namespace Shared.Infra.Module.EntityFramework;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Core.Repositories;
using Shared.Infra.Module.Base;
using Shared.Infra.Module.Base.Attributes;
using Shared.Infra.Module.EntityFramework.Options;
using Shared.Infra.Module.EntityFramework.Repositories;
using Shared.Infra.Module.EntityFramework.Seed;

/// <summary>
/// Module that configures Entity Framework Core with support for multiple database providers.
/// </summary>
[Module(
    Title = "Entity Framework Module",
    Description = "Configures Entity Framework Core with support for multiple providers (SQLite, PostgreSQL, SQL Server). Manages DbContext, migrations and database configurations."
)]
public class EntityFrameworkModule
    (
        IOptions<EntityFrameworkModuleOptions> options,
        EntityFrameworkModuleBuilder args,
        ILogger<EntityFrameworkModule>? logger = null
) : IWebModule
{
    protected readonly EntityFrameworkModuleOptions _options = options.Value;
    protected readonly EntityFrameworkModuleBuilder _args = args;
    protected readonly ILogger<EntityFrameworkModule>? _logger = logger;

    public void AddConfigurations(WebModuleConfigurationOptions configurationOptions)
    {

    }

    public virtual void ConfigureHost(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<EntityInfoService>();
        foreach (var a in _args.AddActions)
        {
            a(builder.Services);
        }
    }

    public void Configure(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;

        this.ExecuteSeed(sp);
    }

    /// <summary>
    /// Executes the configured seeds for each registered DbContext.
    /// </summary>
    private void ExecuteSeed(IServiceProvider sp)
    {
        var allResults = new List<SeedResult>();
        var allErrors = new List<SeedError>();

        foreach (var contextType in _args.RegisteredDbContextTypes)
        {
            var config = _options.DbContexts.GetConfigForContext(contextType.Name!);
            var seedConfig = config?.Seed;

            if (!string.IsNullOrWhiteSpace(seedConfig?.Path))
            {
                var (results, errors) = JsonSeedRunner.Run(sp, contextType, seedConfig);
                allResults.AddRange(results);
                allErrors.AddRange(errors);
            }
        }

        SeedTableWriter.Write(allResults, allErrors);
    }
}
