namespace Shared.UI.Api.Base.Sdk;

using Shared.Infra.Module.Hangfire.Workers;
using Shared.Infra.Module.Observability.Logging;
using Shared.Infra.Module.Observability.OpenTelemetry;
using Shared.Infra.Module.ServiceScan;
using Shared.UI.Api.Base.Modules;

/// <summary>
/// Registro central dos SDKs. Cada SDK é um Action&lt;Startup&gt; que adiciona módulos via AddModule.
/// Customize com Configure.
/// </summary>
public static class SdkRegistry
{
    private static readonly Dictionary<SdkType, Action<Startup>> Actions = new()
    {
        [SdkType.Web] = s => s
            .AddModule<LoggingModule>()
            .AddModule<ResponseHeadersModule>()
            .AddModule<OpenTelemetryModule>()
            .AddModule<ServiceScanModule>()
            .AddModule<SwaggerModule>()
            .AddModule<ScalarUIModule>()
            .AddModule<CorsModule>()
            .AddModule<ErrorHandlingModule>(),

        [SdkType.Console] = s => s.AddModule<LoggingModule>(),

        [SdkType.WorkerJobs] = s => s
            .AddModule<LoggingModule>()
            .AddModule<ResponseHeadersModule>()
            .AddModule<OpenTelemetryModule>()
            .AddModule<ServiceScanModule>()
            .AddModule<CorsModule>()
            .AddModule<ErrorHandlingModule>()
            .AddModule<SwaggerModule>()
            .AddModule<ScalarUIModule>()
            .AddModule<WorkerJobsModule>(),

        [SdkType.WorkerExecutor] = s => s
            .AddModule<LoggingModule>()
            .AddModule<ResponseHeadersModule>()
            .AddModule<OpenTelemetryModule>()
            .AddModule<ServiceScanModule>()
            .AddModule<CorsModule>()
            .AddModule<ErrorHandlingModule>()
            .AddModule<WorkerExecutorModule>(),

        [SdkType.WorkerDashboard] = s => s
            .AddModule<LoggingModule>()
            .AddModule<ResponseHeadersModule>()
            .AddModule<OpenTelemetryModule>()
            .AddModule<ServiceScanModule>()
            .AddModule<CorsModule>()
            .AddModule<ErrorHandlingModule>()
            .AddModule<WorkerDashboardModule>(),

        [SdkType.WorkerScheduler] = s => s
            .AddModule<LoggingModule>()
            .AddModule<ResponseHeadersModule>()
            .AddModule<OpenTelemetryModule>()
            .AddModule<ServiceScanModule>()
            .AddModule<CorsModule>()
            .AddModule<ErrorHandlingModule>()
            .AddModule<WorkerSchedulerModule>()
    };

    /// <summary>
    /// Configura (substitui) o Action do SDK. Recebe o Startup e adiciona módulos com AddModule.
    /// </summary>
    public static void Configure(SdkType sdkType, Action<Startup> action)
    {
        Actions[sdkType] = action ?? throw new ArgumentNullException(nameof(action));
    }

    /// <summary>
    /// Aplica o SDK ao Startup (invoca o Action registrado).
    /// </summary>
    internal static void Apply(SdkType sdkType, Startup startup)
    {
        if (!Actions.TryGetValue(sdkType, out var action))
            throw new ArgumentOutOfRangeException(nameof(sdkType), sdkType, $"SDK {sdkType} não definido.");

        action(startup);
    }
}
