using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Shared.Infra.Module.Observability.Logging.Configurations;

namespace Shared.Infra.Module.Observability.Logging.Interceptors.ReqAndResp;

/// <summary>
/// Middleware que faz o logging completo de requisições HTTP da API (REQUEST e RESPONSE).
/// Configurável via Serilog.json na seção :HttpRequestResponseLogging
/// </summary>
public class ApiRequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiRequestLoggingMiddleware> _logger;
    private readonly LoggingModuleOptions _options;

    public ApiRequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<ApiRequestLoggingMiddleware> logger,
        IOptions<LoggingModuleOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Verificar se o path deve ser excluído
        if (ShouldExcludePath(path))
        {
            await _next(context);
            return;
        }

        var requestId = Guid.NewGuid().ToString("N")[..8];
        var startTime = DateTime.UtcNow;

        // Extrair informações do request
        var method = context.Request.Method;
        var queryString = context.Request.QueryString.Value ?? "";
        var queryParameters = ParseQueryStringToDictionary(queryString);

        // Headers do request (com mascaramento de sensíveis)
        Dictionary<string, string>? requestHeaders = null;
        if (_options.HttpRequestResponse!.LogRequestHeaders)
        {
            requestHeaders = GetMaskedHeaders(context.Request.Headers);
        }

        // Ler body do request (se habilitado)
        string? requestBody = null;
        string? requestContentType = null;
        if (_options.HttpRequestResponse!.LogRequestBody &&
            context.Request.ContentLength > 0)
        {
            requestContentType = context.Request.ContentType;
            context.Request.EnableBuffering();
            
            // Ler como bytes primeiro para poder tratar binários
            using var memoryStream = new MemoryStream();
            await context.Request.Body.CopyToAsync(memoryStream);
            context.Request.Body.Position = 0;
            
            var bodyBytes = memoryStream.ToArray();

            // Truncar se necessário
            if (bodyBytes.Length > _options.HttpRequestResponse!.MaxBodySize)
            {
                var truncatedBytes = new byte[_options.HttpRequestResponse!.MaxBodySize];
                Array.Copy(bodyBytes, truncatedBytes, _options.HttpRequestResponse!.MaxBodySize);
                bodyBytes = truncatedBytes;
            }

            // Processar de acordo com o mimetype
            requestBody = ProcessBodyByContentType(bodyBytes, requestContentType);
        }

        // Capturar response - NÃO usar 'using' para evitar ObjectDisposedException
        var originalBodyStream = context.Response.Body;
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        Exception? caughtException = null;
        
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Sempre processar o response (mesmo com exceção)
        var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

        // Headers da resposta (com mascaramento de sensíveis)
        Dictionary<string, string>? responseHeaders = null;
        if (_options.HttpRequestResponse!.LogResponseHeaders)
        {
            responseHeaders = GetMaskedHeaders(context.Response.Headers);
        }

        // Ler body da resposta
        string? responseBodyText = null;
        string? responseContentType = null;
        responseBody.Seek(0, SeekOrigin.Begin);

        if (_options.HttpRequestResponse!.LogResponseBody)
        {
            responseContentType = context.Response.ContentType;
            
            // Ler como bytes primeiro para poder tratar binários
            var bodyBytes = responseBody.ToArray();

            // Truncar se necessário
            if (bodyBytes.Length > _options.HttpRequestResponse!.MaxBodySize)
            {
                var truncatedBytes = new byte[_options.HttpRequestResponse!.MaxBodySize];
                Array.Copy(bodyBytes, truncatedBytes, _options.HttpRequestResponse!.MaxBodySize);
                bodyBytes = truncatedBytes;
            }

            // Processar de acordo com o mimetype
            responseBodyText = ProcessBodyByContentType(bodyBytes, responseContentType);

            responseBody.Seek(0, SeekOrigin.Begin);
        }

        // Copiar response para o stream original
        await responseBody.CopyToAsync(originalBodyStream);
        
        // IMPORTANTE: Restaurar o stream original ANTES de propagar exceção
        context.Response.Body = originalBodyStream;
        
        // Agora podemos descartar o MemoryStream
        await responseBody.DisposeAsync();

        // Adicionar request/response bodies nos traces do OpenTelemetry (se houver span ativo)
        var activity = System.Diagnostics.Activity.Current;
        if (activity != null)
        {
            if (!string.IsNullOrWhiteSpace(requestBody))
            {
                activity.SetTag("http.request.body", requestBody);
            }
            if (!string.IsNullOrWhiteSpace(responseBodyText))
            {
                activity.SetTag("http.response.body", responseBodyText);
            }
        }

        // Criar RequestInfo e ResponseInfo ORIGINAL (Dictionary com Body processado de acordo com o tipo)
        var bodyReq = requestBody == null ? null : ProcessBodyForLogging(requestBody, requestContentType);
        var requestInfo = new Dictionary<string, object?>
        {
            ["Method"] = method,
            ["Path"] = path,
            ["QueryString"] = string.IsNullOrEmpty(queryString) ? null : queryString,
            ["QueryParameters"] = queryParameters.Count > 0 ? queryParameters : null,
            ["Headers"] = requestHeaders,
            ["Body"] = bodyReq
        };

        var bodyRes = responseBodyText == null ? null : ProcessBodyForLogging(responseBodyText, responseContentType);
        var responseInfo = new Dictionary<string, object?>
        {
            ["StatusCode"] = context.Response.StatusCode,
            ["DurationMs"] = Math.Round(duration, 2),
            ["Headers"] = responseHeaders,
            ["Body"] = bodyRes,
            ["Exception"] = caughtException?.Message
        };

        // Log com RequestInfo e ResponseInfo como Dictionary (destructureObjects: true)
        using (Serilog.Context.LogContext.PushProperty("LogType", "HTTP"))
        using (Serilog.Context.LogContext.PushProperty("HttpRequestId", requestId))
        using (Serilog.Context.LogContext.PushProperty("RequestInfo", requestInfo, destructureObjects: true))
        using (Serilog.Context.LogContext.PushProperty("ResponseInfo", responseInfo, destructureObjects: true))
        {
            if (caughtException != null)
            {
                _logger.LogError(
                    caughtException,
                    "REQUEST_END: HTTP {Method} {Path} - EXCEPTION - {Duration:F2}ms",
                    method, path, duration);
            }
            else
            {
                var logLevel = context.Response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
                _logger.Log(
                    logLevel,
                    "REQUEST_END: HTTP {Method} {Path} - {StatusCode} - {Duration:F2}ms",
                    method, path, context.Response.StatusCode, duration);
            }
        }

        // Propagar a exceção para que outros middlewares (como DeveloperExceptionPage) a tratem
        if (caughtException != null)
        {
            throw caughtException;
        }
    }

    /// <summary>
    /// Verifica se o path deve ser excluído do logging
    /// </summary>
    private bool ShouldExcludePath(string path)
    {
        foreach (var excludePath in _options.HttpRequestResponse!.ExcludePaths)
        {
            if (path.StartsWith(excludePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Retorna headers com valores sensíveis mascarados
    /// </summary>
    private Dictionary<string, string> GetMaskedHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, string>();

        foreach (var header in headers)
        {
            var value = string.Join(", ", header.Value);

            // Mascarar headers sensíveis
            if (_options.HttpRequestResponse!.SensitiveHeaders.Any(s =>
                header.Key.Equals(s, StringComparison.OrdinalIgnoreCase)))
            {
                value = "[MASKED]";
            }

            result[header.Key] = value;
        }

        return result;
    }

    /// <summary>
    /// Parseia a query string em um dicionário de parâmetros
    /// </summary>
    private static Dictionary<string, string> ParseQueryStringToDictionary(string queryString)
    {
        var result = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(queryString))
            return result;

        // Remover o '?' inicial se existir
        if (queryString.StartsWith("?"))
            queryString = queryString.Substring(1);

        if (string.IsNullOrEmpty(queryString))
            return result;

        // Separar os parâmetros
        var parameters = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var parameter in parameters)
        {
            var parts = parameter.Split('=', 2); // Limitar a 2 partes para preservar '=' nos valores
            if (parts.Length == 2)
            {
                var key = Uri.UnescapeDataString(parts[0]);
                var value = Uri.UnescapeDataString(parts[1]);
                result[key] = value;
            }
            else if (parts.Length == 1)
            {
                // Parâmetro sem valor (ex: ?debug)
                var key = Uri.UnescapeDataString(parts[0]);
                result[key] = "";
            }
        }

        return result;
    }
    
    /// <summary>
    /// Processa o body de acordo com o Content-Type.
    /// Binários → base64, JSON → objeto parseado, outros textos → string pura
    /// </summary>
    private static string ProcessBodyByContentType(byte[] bodyBytes, string? contentType)
    {
        if (bodyBytes == null || bodyBytes.Length == 0)
            return null;

        // Verificar se é binário
        if (IsBinaryContentType(contentType))
        {
            return Convert.ToBase64String(bodyBytes);
        }

        // Tentar ler como texto
        try
        {
            // Tentar UTF-8 primeiro
            var text = Encoding.UTF8.GetString(bodyBytes);
            // Limpar caracteres nulos
            text = text.Replace("\0", "");
            return text;
        }
        catch
        {
            // Se falhar ao decodificar como texto, tratar como binário
            return Convert.ToBase64String(bodyBytes);
        }
    }

    /// <summary>
    /// Verifica se o Content-Type indica conteúdo binário
    /// </summary>
    private static bool IsBinaryContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        var contentTypeLower = contentType.ToLowerInvariant();
        
        // Tipos binários conhecidos
        return contentTypeLower.Contains("application/octet-stream") ||
               contentTypeLower.Contains("image/") ||
               contentTypeLower.Contains("video/") ||
               contentTypeLower.Contains("audio/") ||
               contentTypeLower.Contains("application/pdf") ||
               contentTypeLower.Contains("application/zip") ||
               contentTypeLower.Contains("application/x-") ||
               contentTypeLower.Contains("multipart/form-data");
    }

    /// <summary>
    /// Processa o body para logging: JSON vira objeto, outros ficam como string/base64
    /// </summary>
    private static object? ProcessBodyForLogging(string body, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        // Se for binário (base64), retornar como string indicando que é base64
        if (IsBinaryContentType(contentType))
        {
            return body; // Já está em base64
        }

        // Se for JSON, tentar fazer parse
        if (IsJsonContentType(contentType))
        {
            try
            {
                var token = JToken.Parse(body);
                if (token is JObject jObj)
                {
                    return ConvertJTokenToDictionary(jObj);
                }
                // Se for array ou outro tipo, retornar como está parseado
                return ConvertJTokenToObject(token);
            }
            catch
            {
                // Se falhar o parse, retornar como string pura
                return body;
            }
        }

        // Outros tipos de texto: retornar como string pura
        return body;
    }

    /// <summary>
    /// Verifica se o Content-Type é JSON
    /// </summary>
    private static bool IsJsonContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        var contentTypeLower = contentType.ToLowerInvariant();
        return contentTypeLower.Contains("application/json") ||
               contentTypeLower.Contains("application/vnd.api+json") ||
               contentTypeLower.Contains("text/json");
    }

    /// <summary>
    /// Converte JToken para tipos nativos .NET recursivamente.
    /// JObject → Dictionary, JArray → List, JValue → valor primitivo
    /// </summary>
    private static Dictionary<string, object?> ConvertJTokenToDictionary(JToken token)
    {
        if (token is JObject jObj)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var prop in jObj.Properties())
            {
                dict[prop.Name] = ConvertJTokenToObject(prop.Value);
            }
            return dict;
        }
        return new Dictionary<string, object?>();
    }
    
    private static object? ConvertJTokenToObject(JToken? token)
    {
        if (token == null || token.Type == JTokenType.Null)
            return null;
            
        switch (token.Type)
        {
            case JTokenType.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var prop in ((JObject)token).Properties())
                {
                    dict[prop.Name] = ConvertJTokenToObject(prop.Value);
                }
                return dict;
                
            case JTokenType.Array:
                return token.Select(ConvertJTokenToObject).ToList();
                
            case JTokenType.Integer:
            case JTokenType.Float:
            case JTokenType.String:
            case JTokenType.Boolean:
            case JTokenType.Date:
            case JTokenType.Bytes:
                return ((JValue)token).Value;
                
            default:
                return token.ToString();
        }
    }
}
