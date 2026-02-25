namespace Shared.Infra.Module.Hangfire;

using global::Hangfire;
using global::Hangfire.PostgreSql;
using global::Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infra.Module.Hangfire.Options;

internal static class HangfireSetup
{
    public static void AddHangfireWithStorage(
        IServiceCollection services,
        string connectionString,
        HangfireStorageProvider storageProvider,
        ILogger? logger,
        bool addServer,
        int? workerCount = null)
    {
        services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();
            ApplyStorage(config, connectionString, storageProvider);
        });

        if (addServer)
        {
            services.AddHangfireServer(serverOptions =>
            {
                if (workerCount.HasValue)
                    serverOptions.WorkerCount = workerCount.Value;
            });
        }

        logger?.LogInformation(
            "Hangfire registrado. Storage: {StorageProvider}. Modo: {Mode}",
            storageProvider,
            addServer ? "Server" : "Client-only");
    }

    public static void ApplyStorage(IGlobalConfiguration config, string connectionString, HangfireStorageProvider provider)
    {
        switch (provider)
        {
            case HangfireStorageProvider.SQLite:
                config.UseSQLiteStorage(connectionString);
                break;
            case HangfireStorageProvider.SqlServer:
                config.UseSqlServerStorage(connectionString);
                break;
            case HangfireStorageProvider.Postgres:
                config.UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }
    }

    public static void RegisterJobEndpoints(WebApplication app)
    {
        app.MapGet("jobs", (HttpContext ctx) =>
            {
                var jobs = ctx.RequestServices.GetService<IEnumerable<IJob>>() ?? [];
                var names = jobs.Select(j => j.Name).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToArray();
                return Results.Ok(names);
            })
            .WithTags("Jobs")
            .WithSummary("Listar jobs")
            .WithDescription("Lista os nomes dos jobs disponíveis.");

        using var scope = app.Services.CreateScope();
        var jobNames = (scope.ServiceProvider.GetService<IEnumerable<IJob>>() ?? [])
            .Select(j => j.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order()
            .ToList();

        foreach (var name in jobNames)
        {
            var jobName = name;
            app.MapGet($"jobs/{jobName}", async (HttpContext ctx) =>
                {
                    var jobs = ctx.RequestServices.GetService<IEnumerable<IJob>>() ?? [];
                    var job = jobs.FirstOrDefault(j => string.Equals(j.Name, jobName, StringComparison.OrdinalIgnoreCase));
                    if (job is null)
                        return Results.NotFound(new { message = $"Job '{jobName}' não encontrado." });
                    await job.ExecuteAsync(ctx.RequestAborted);
                    return Results.Ok(new { job = jobName, status = "executed" });
                })
                .WithTags("Jobs")
                .WithSummary($"Executar job '{jobName}'")
                .WithDescription($"Executa o job '{jobName}' uma vez (para testes).");
        }
    }
}
