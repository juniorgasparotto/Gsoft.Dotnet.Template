namespace Shared.Infra.Module.Hangfire.Options;

/// <summary>
/// Provedor de storage do Hangfire.
/// </summary>
public enum HangfireStorageProvider
{
    /// <summary>SQLite. Adequado para dev/simples; em produção com carga prefira SqlServer ou Postgres.</summary>
    SQLite,

    /// <summary>Microsoft SQL Server.</summary>
    SqlServer,

    /// <summary>PostgreSQL.</summary>
    Postgres
}
