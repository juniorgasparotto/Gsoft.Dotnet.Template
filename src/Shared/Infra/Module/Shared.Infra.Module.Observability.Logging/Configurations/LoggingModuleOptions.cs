namespace Shared.Infra.Module.Observability.Logging.Configurations;

public class LoggingModuleOptions
{
    public SerilogLoggingOptions? Serilog { get; set; }
    public HttpRequestResponseLoggingOptions? HttpRequestResponse { get; set; }
}

public class SerilogLoggingOptions
{
    public bool Enabled { get; set; }
    public bool EnableCallerInfo { get; set; }
    public bool EnableDefaultMinimalLevel { get; set; }
    public bool WriteToOpenTelemetry { get; set; }
    public bool WriteToPostgres { get; set; }
    public string? ApiRequestLogging { get; set; }
}

/// <summary>
/// Opções para configurar o logging de Request/Response HTTP.
/// Configurável via Serilog.json na seção :HttpRequestResponseLogging
/// </summary>
public class HttpRequestResponseLoggingOptions
{
    public bool LogRequestBody { get; set; }
    public bool LogResponseBody { get; set; }
    public bool LogRequestHeaders { get; set; }
    public bool LogResponseHeaders { get; set; }
    public int MaxBodySize { get; set; }
    public List<string> ExcludePaths { get; set; } = new List<string>();
    public List<string> SensitiveHeaders { get; set; } = new List<string>();
}
