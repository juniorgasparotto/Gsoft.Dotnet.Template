namespace Shared.Infra.Module.Observability.OpenTelemetry;

/// <summary>
/// Opções de configuração para o OpenTelemetryModule.
/// </summary>
public class OpenTelemetryModuleOptions
{
    public LoggingOptions? Logging { get; set; }
    public MetricsOptions? Metrics { get; set; }
    public TracingOptions? Tracing { get; set; }
    public OpenTelemetryServiceDiscoveryOptions? ServiceDiscovery { get; set; }
    public HealthChecksOptions? HealthChecks { get; set; }
}

public class LoggingOptions
{
    public bool? Enabled { get; set; }
    public bool? IncludeFormattedMessage { get; set; }
    public bool? IncludeScopes { get; set; }
    public bool? UseOtlpExporter { get; set; }
}

public class MetricsOptions
{
    public bool? Enabled { get; set; }
    public bool? AspNetCoreInstrumentation { get; set; }
    public bool? HttpClientInstrumentation { get; set; }
    public bool? RuntimeInstrumentation { get; set; }
    public bool? UseOtlpExporter { get; set; }
}

public class TracingOptions
{
    public bool? Enabled { get; set; }
    public bool? AspNetCoreInstrumentation { get; set; }
    public bool? HttpClientInstrumentation { get; set; }
    public bool? RecordException { get; set; }
    public List<string>? ExcludePaths { get; set; }
    public bool? UseOtlpExporter { get; set; }
}

public class OpenTelemetryServiceDiscoveryOptions
{
    public bool? Enabled { get; set; }
    public List<string>? AllowedSchemes { get; set; }
}

public class HealthChecksOptions
{
    public bool? Enabled { get; set; }
    public bool? OnlyInDevelopment { get; set; }
    public string? HealthEndpointPath { get; set; }
    public string? AlivenessEndpointPath { get; set; }
}