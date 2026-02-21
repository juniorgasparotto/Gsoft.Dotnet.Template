namespace Shared.Infra.Module.Observability.Logging.Interceptors.ReqAndResp;

/// <summary>
/// Contexto de Trace ID para rastreamento de execuções através de logs HTTP e DB
/// Usa AsyncLocal para propagar o Trace ID através do contexto assíncrono
/// </summary>
public static class TraceContext
{
    private static readonly AsyncLocal<string?> _traceId = new AsyncLocal<string?>();

    /// <summary>
    /// Obtém o Trace ID atual do contexto assíncrono
    /// </summary>
    public static string? CurrentTraceId => _traceId.Value;

    /// <summary>
    /// Define o Trace ID no contexto assíncrono
    /// </summary>
    public static void SetTraceId(string traceId)
    {
        _traceId.Value = traceId;
    }

    /// <summary>
    /// Cria um novo Trace ID e define no contexto assíncrono
    /// Formato: {jobName}-{hash} (hash pequeno de 6 caracteres)
    /// </summary>
    public static string CreateTraceId(string jobName)
    {
        // Normalizar nome do job (remover caracteres especiais, limitar tamanho)
        var normalizedJobName = jobName
            .Replace("/", "-")
            .Replace(" ", "-")
            .ToLower()
            .Substring(0, Math.Min(jobName.Length, 20));
        
        // Hash pequeno (6 caracteres)
        var hash = Guid.NewGuid().ToString("N")[..6];
        
        var traceId = $"{normalizedJobName}-{hash}";
        SetTraceId(traceId);
        return traceId;
    }

    /// <summary>
    /// Limpa o Trace ID do contexto assíncrono
    /// </summary>
    public static void ClearTraceId()
    {
        _traceId.Value = null;
    }

    /// <summary>
    /// Executa uma ação dentro de um contexto de Trace ID
    /// </summary>
    public static async Task<T> RunWithTraceIdAsync<T>(string jobName, Func<Task<T>> action)
    {
        var traceId = CreateTraceId(jobName);
        try
        {
            return await action();
        }
        finally
        {
            ClearTraceId();
        }
    }

    /// <summary>
    /// Executa uma ação dentro de um contexto de Trace ID (sem retorno)
    /// </summary>
    public static async Task RunWithTraceIdAsync(string jobName, Func<Task> action)
    {
        var traceId = CreateTraceId(jobName);
        try
        {
            await action();
        }
        finally
        {
            ClearTraceId();
        }
    }
}

