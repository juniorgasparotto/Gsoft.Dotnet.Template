namespace Shared.UI.Api.Base.Sdk;

/// <summary>
/// Tipo de SDK: conjunto pré-definido de módulos para cada tipo de aplicação.
/// </summary>
public enum SdkType
{
    /// <summary>API Web (ASP.NET Core)</summary>
    Web,

    /// <summary>Aplicação Console</summary>
    Console,

    /// <summary>Worker (Background Service)</summary>
    Worker,

    /// <summary>Worker Jobs (expõe GET /jobs; Hangfire + Swagger/Scalar)</summary>
    WorkerJobs,

    /// <summary>Worker Executor (processa jobs Hangfire)</summary>
    WorkerExecutor,

    /// <summary>Worker Dashboard (Hangfire Dashboard)</summary>
    WorkerDashboard,

    /// <summary>Worker Scheduler (agenda jobs)</summary>
    WorkerScheduler
}
