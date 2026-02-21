using Shared.Infra.Module.Base.Attributes;

namespace Shared.Infra.Module.Hangfire.Options;

/// <summary>
/// Opções de configuração do Hangfire (storage e parâmetros por worker). Server/Dashboard/ativo vêm do módulo.
/// </summary>
[ModuleOption("WorkerModule")]
public class WorkerModuleOptions
{
    /// <summary>
    /// Provedor de storage: SQLite, SqlServer ou Postgres.
    /// </summary>
    public HangfireStorageProvider StorageProvider { get; set; } = HangfireStorageProvider.SQLite;

    /// <summary>
    /// Connection string para o storage do Hangfire.
    /// Formato depende do StorageProvider (ex: SQLite "Data Source=hangfire.db", SqlServer/Postgres connection string).
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Número de workers para processamento paralelo (só usado no Worker Executor).
    /// </summary>
    public int? WorkerCount { get; set; }

    /// <summary>
    /// Caminho do Dashboard (ex: "hangfire"). Só usado no Worker Dashboard.
    /// </summary>
    public string DashboardPath { get; set; } = "hangfire";
}
