using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Shared.Infra.Module.Base;
using Shared.Infra.Module.Base.Attributes;

namespace Shared.Infra.Module.Observability.OpenTelemetry;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
[ModuleAttribute(Title = "OpenTelemetry Module", Description = "Adds observability with OpenTelemetry, including metrics, traces, health checks and service discovery. Provides complete instrumentation for application monitoring.")]
public class OpenTelemetryModule : IWebModule
{
    private readonly OpenTelemetryModuleOptions _options;

    public OpenTelemetryModule(IOptions<OpenTelemetryModuleOptions> options)
    {
        _options = options.Value;
    }

    public void AddConfigurations(WebModuleConfigurationOptions configurationOptions)
    {
    }

    public void ConfigureHost(WebApplicationBuilder builder)
    {
        this.ConfigureOpenTelemetry(builder);

        // Health Checks
        if (_options.HealthChecks?.Enabled != false)
        {
            this.AddDefaultHealthChecks(builder);
        }

        // Service Discovery
        if (_options.ServiceDiscovery?.Enabled != false)
        {
            builder.Services.AddServiceDiscovery();

            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                // Turn on resilience by default
                http.AddStandardResilienceHandler();

                // Turn on service discovery by default
                http.AddServiceDiscovery();
            });

            // Configurar schemes permitidos se especificado
            var allowedSchemes = _options.ServiceDiscovery?.AllowedSchemes;
            if (allowedSchemes != null && allowedSchemes.Count > 0)
            {
                builder.Services.Configure<Microsoft.Extensions.ServiceDiscovery.ServiceDiscoveryOptions>(options =>
                {
                    options.AllowedSchemes = allowedSchemes;
                });
            }
        }
    }

    public void ConfigureOpenTelemetry(IHostApplicationBuilder builder)
    {
        // Configurar OpenTelemetry Logging Provider para enviar logs via OTLP
        if (_options.Logging?.Enabled == true)
        {
            var loggingOpts = _options.Logging!;
            builder.Logging.AddOpenTelemetry(logging =>
            {
                if (loggingOpts.IncludeFormattedMessage == true)
                    logging.IncludeFormattedMessage = true;
                if (loggingOpts.IncludeScopes == true)
                    logging.IncludeScopes = true;
                if (loggingOpts.UseOtlpExporter != false)
                    logging.AddOtlpExporter(); // Exporter para logs via OTLP
            });
        }

        var otelBuilder = builder.Services.AddOpenTelemetry();

        // Configurar Metrics
        if (_options.Metrics?.Enabled != false)
        {
            var metricsOpts = _options.Metrics;
            otelBuilder.WithMetrics(metrics =>
            {
                if (metricsOpts?.AspNetCoreInstrumentation != false)
                    metrics.AddAspNetCoreInstrumentation();
                if (metricsOpts?.HttpClientInstrumentation != false)
                    metrics.AddHttpClientInstrumentation();
                if (metricsOpts?.RuntimeInstrumentation != false)
                    metrics.AddRuntimeInstrumentation();
                if (metricsOpts?.UseOtlpExporter != false)
                    metrics.AddOtlpExporter(); // Exporter para metrics
            });
        }

        // Configurar Tracing
        var tracingOpts = _options.Tracing;
        if (tracingOpts != null && tracingOpts.Enabled != false)
        {
            otelBuilder.WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName ?? "App");
                
                if (tracingOpts.AspNetCoreInstrumentation != false)
                {
                    tracing.AddAspNetCoreInstrumentation(options =>
                    {
                        // Exclude health check requests from tracing
                        var healthPath = _options.HealthChecks?.HealthEndpointPath ?? "/health";
                        var alivePath = _options.HealthChecks?.AlivenessEndpointPath ?? "/alive";
                        var excludePaths = tracingOpts.ExcludePaths ?? new List<string> { healthPath, alivePath };
                        options.Filter = context =>
                        {
                            foreach (var path in excludePaths)
                            {
                                if (context.Request.Path.StartsWithSegments(path))
                                    return false;
                            }
                            return true;
                        };

                        // Capturar exceções completas
                        if (tracingOpts.RecordException != false)
                            options.RecordException = true;
                    });
                }
                
                if (tracingOpts.HttpClientInstrumentation != false)
                    tracing.AddHttpClientInstrumentation();
                    
                if (tracingOpts.UseOtlpExporter != false)
                    tracing.AddOtlpExporter(); // Exporter para traces
            });
        }
    }

    private void AddDefaultHealthChecks(WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
    }

    public void Configure(WebApplication app)
    {
        // Health Checks endpoints
        if (_options.HealthChecks?.Enabled != false)
        {
            var onlyInDevelopment = _options.HealthChecks?.OnlyInDevelopment ?? true;
            
            if (!onlyInDevelopment || app.Environment.IsDevelopment())
            {
                var healthPath = _options.HealthChecks?.HealthEndpointPath ?? "/health";
                var alivePath = _options.HealthChecks?.AlivenessEndpointPath ?? "/alive";

                // All health checks must pass for app to be considered ready to accept traffic after starting
                app.MapHealthChecks(healthPath);

                // Only health checks tagged with the "live" tag must pass for app to be considered alive
                app.MapHealthChecks(alivePath, new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("live")
                });
            }
        }
    }

}
