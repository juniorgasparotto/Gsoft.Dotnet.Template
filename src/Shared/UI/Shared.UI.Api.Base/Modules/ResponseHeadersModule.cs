namespace Shared.UI.Api.Base.Modules;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Shared.Infra.Module.Base;
using Shared.Infra.Module.Base.Attributes;
using System.Diagnostics;
using System.Globalization;

/// <summary>
/// Opções de configuração para o ResponseHeadersModule.
/// </summary>
public class ResponseHeadersModuleOptions
{
    public SecurityHeadersOptions? Security { get; set; }
    public ApplicationHeadersOptions? Application { get; set; }
    public TracingHeadersOptions? Tracing { get; set; }
    public TimingHeadersOptions? Timing { get; set; }
}

public class SecurityHeadersOptions
{
    public bool? XContentTypeOptions { get; set; }
    public string? XContentTypeOptionsValue { get; set; }
    public bool? XFrameOptions { get; set; }
    public string? XFrameOptionsValue { get; set; }
    public bool? XXssProtection { get; set; }
    public string? XXssProtectionValue { get; set; }
    public bool? ReferrerPolicy { get; set; }
    public string? ReferrerPolicyValue { get; set; }
}

public class ApplicationHeadersOptions
{
    public bool? XApplicationName { get; set; }
    public bool? XRequestId { get; set; }
    public bool? XCorrelationId { get; set; }
}

public class TracingHeadersOptions
{
    public bool? XTraceId { get; set; }
    public bool? XSpanId { get; set; }
    public bool? XParentSpanId { get; set; }
    public bool? XTraceFlags { get; set; }
    public bool? XTraceState { get; set; }
}

public class TimingHeadersOptions
{
    public bool? XRequestTime { get; set; }
    public bool? XResponseTime { get; set; }
    public bool? XRequestDurationSeconds { get; set; }
    public bool? XRequestDurationMilliseconds { get; set; }
}

/// <summary>
/// Módulo que adiciona headers customizados em todas as respostas HTTP.
/// </summary>
[ModuleAttribute(Title = "Response Headers Module", Description = "Adds custom headers to all HTTP responses, including security, application, tracing and timing headers. Configurable via options.")]
public class ResponseHeadersModule : IWebModule
{
    private readonly ResponseHeadersModuleOptions _options;

    public ResponseHeadersModule(IOptions<ResponseHeadersModuleOptions> options)
    {
        _options = options.Value;
    }
    public void AddConfigurations(WebModuleConfigurationOptions configurationOptions)
    {
    }

    public void ConfigureHost(WebApplicationBuilder builder)
    {
    }

    public void Configure(WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var response = context.Response;
            var startTime = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            // Headers de segurança
            if (_options.Security?.XContentTypeOptions != false)
            {
                response.Headers["X-Content-Type-Options"] = _options.Security?.XContentTypeOptionsValue ?? "nosniff";
            }
            if (_options.Security?.XFrameOptions != false)
            {
                response.Headers["X-Frame-Options"] = _options.Security?.XFrameOptionsValue ?? "DENY";
            }
            if (_options.Security?.XXssProtection != false)
            {
                response.Headers["X-XSS-Protection"] = _options.Security?.XXssProtectionValue ?? "1; mode=block";
            }
            if (_options.Security?.ReferrerPolicy != false)
            {
                response.Headers["Referrer-Policy"] = _options.Security?.ReferrerPolicyValue ?? "strict-origin-when-cross-origin";
            }

            // Informações da aplicação
            if (_options.Application?.XApplicationName != false)
            {
                var assemblyName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";
                response.Headers["X-Application-Name"] = assemblyName;
            }

            // Request ID - lê do header de entrada se existir, senão usa TraceIdentifier
            if (_options.Application?.XRequestId != false)
            {
                var requestId = context.Request.Headers["X-Request-Id"].FirstOrDefault() 
                             ?? context.Request.Headers["Request-Id"].FirstOrDefault()
                             ?? context.TraceIdentifier;
                response.Headers["X-Request-Id"] = requestId;
            }

            // Correlation ID - lê do header de entrada se existir
            if (_options.Application?.XCorrelationId != false)
            {
                var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                                 ?? context.Request.Headers["correlation-id"].FirstOrDefault();
                if (!string.IsNullOrEmpty(correlationId))
                {
                    response.Headers["X-Correlation-Id"] = correlationId;
                }
            }

            // Timestamp do início do request
            if (_options.Timing?.XRequestTime != false)
            {
                response.Headers["X-Request-Time"] = startTime.ToString("O");
            }

            // Informações de tracing (Activity)
            if (_options.Tracing != null)
            {
                var activity = Activity.Current;
                if (activity != null)
                {
                    // TraceId (W3C Trace Context)
                    if (_options.Tracing.XTraceId != false && activity.TraceId != default)
                    {
                        response.Headers["X-Trace-Id"] = activity.TraceId.ToString();
                    }

                    // SpanId (W3C Trace Context)
                    if (_options.Tracing.XSpanId != false && activity.SpanId != default)
                    {
                        response.Headers["X-Span-Id"] = activity.SpanId.ToString();
                    }

                    // Parent SpanId
                    if (_options.Tracing.XParentSpanId != false && activity.ParentSpanId != default)
                    {
                        response.Headers["X-Parent-Span-Id"] = activity.ParentSpanId.ToString();
                    }

                    // TraceFlags
                    if (_options.Tracing.XTraceFlags != false)
                    {
                        response.Headers["X-Trace-Flags"] = activity.ActivityTraceFlags.ToString();
                    }

                    // TraceState (se disponível)
                    if (_options.Tracing.XTraceState != false && !string.IsNullOrEmpty(activity.TraceStateString))
                    {
                        response.Headers["X-Trace-State"] = activity.TraceStateString;
                    }
                }
            }

            // Registrar callback para adicionar headers calculados antes da resposta ser enviada
            response.OnStarting(() =>
            {
                stopwatch.Stop();
                var durationSeconds = stopwatch.Elapsed.TotalSeconds;
                var durationMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

                // Timestamp da resposta
                if (_options.Timing?.XResponseTime != false)
                {
                    response.Headers["X-Response-Time"] = DateTime.UtcNow.ToString("O");
                }

                // Duração do request em segundos (padrão US com ponto)
                if (_options.Timing?.XRequestDurationSeconds != false)
                {
                    response.Headers["X-Request-Duration-Seconds"] = durationSeconds.ToString("F6", CultureInfo.InvariantCulture);
                }

                // Duração do request em milissegundos (padrão US com ponto)
                if (_options.Timing?.XRequestDurationMilliseconds != false)
                {
                    response.Headers["X-Request-Duration-Milliseconds"] = durationMilliseconds.ToString("F3", CultureInfo.InvariantCulture);
                }

                return Task.CompletedTask;
            });

            await next();
        });
    }
}
