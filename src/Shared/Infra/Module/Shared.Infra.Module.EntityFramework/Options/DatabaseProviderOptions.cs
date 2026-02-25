namespace Shared.Infra.Module.EntityFramework.Options;

public class DatabaseProviderOptions
{
    public string? ConnectionString { get; set; }
    public string? MigrationsAssembly { get; set; }
    public string? MigrationsHistoryTable { get; set; }
    public bool? EnableSensitiveDataLogging { get; set; }
    public bool? EnableDetailedErrors { get; set; }
    public int? CommandTimeout { get; set; }
    public bool? EnableRetryOnFailure { get; set; }
    public int? MaxRetryCount { get; set; }
    public int? MaxRetryDelaySeconds { get; set; }
}
