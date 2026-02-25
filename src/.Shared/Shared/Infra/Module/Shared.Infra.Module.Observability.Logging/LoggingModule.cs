using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Shared.Infra.Module.Base;
using Shared.Infra.Module.Base.Attributes;
using Shared.Infra.Module.Base.Utils;
using Shared.Infra.Module.Observability.Logging.Configurations;
using Shared.Infra.Module.Observability.Logging.Interceptors.ReqAndResp;

namespace Shared.Infra.Module.Observability.Logging;

[Module(SectionName = nameof(LoggingModule), Title = "Logging Module", Description = "Configures structured logging with Serilog. Provides log enrichment, configurable sinks, HTTP request/response interceptors and OpenTelemetry integration.")]
public class LoggingModule(IOptions<LoggingModuleOptions>? options) : IWebModule
{
    private readonly LoggingModuleOptions _options = options?.Value ?? new LoggingModuleOptions();

    public void AddConfigurations(WebModuleConfigurationOptions configurationOptions)
    {
        if (_options.Serilog?.Enabled == true)
        {
            Serilog.Debugging.SelfLog.Enable(msg =>
            {
                /// TODO: 2026-02-17T02:22:12.4470574Z Maximum destructuring depth reached
                Console.WriteLine("SERILOG ERROR: " + msg);
            });

            var rootPath = Directory.GetCurrentDirectory();
            var binPath = AppContext.BaseDirectory;

            string GetPath(string path)
            {
                var fullPath = Path.Combine(rootPath, path);
                if (File.Exists(fullPath))
                    return fullPath;
                fullPath = Path.Combine(binPath, path);
                if (File.Exists(fullPath))
                    return fullPath;
                
                throw new FileNotFoundException($"Configuration file '{path}' not found in '{rootPath}' or '{binPath}'.");
            }

            // Para caso de sobrescrita total das configs do Serilog
            var localSerilogJson = Path.Combine(rootPath, "Serilog.json");
            if (File.Exists(localSerilogJson))
            {
                configurationOptions.AddJsonFile(localSerilogJson, optional: false, reloadOnChange: false);
            }
            else
            {
                // Lista de arquivos JSON do Serilog para fazer merge (na ordem de prioridade)
                var serilogJsonFiles = new List<string>
                {
                    GetPath("Serilog.Base.json")
                };

                // Se Postgres estiver habilitado, adicionar ao merge
                if (this._options.Serilog.WriteToPostgres)
                    serilogJsonFiles.Add(GetPath("Serilog.WriteTo.Postgres.json"));

                if (this._options.Serilog.EnableCallerInfo)
                    serilogJsonFiles.Add(GetPath("Serilog.Enrich.CallerInfo.json"));

                if (this._options.Serilog.EnableDefaultMinimalLevel)
                    serilogJsonFiles.Add(GetPath("Serilog.MinimalLevel.json"));

                // Se OpenTelemetry estiver disponível, adicionar ao merge
                if (this._options.Serilog.WriteToOpenTelemetry &&
                    !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
                    serilogJsonFiles.Add(GetPath("Serilog.WriteTo.OpenTelemetry.json"));

                // Fazer merge dos JSONs usando Newtonsoft.Json ANTES de adicionar
                // Adicionar configurações mescladas diretamente na memória (sem arquivo temporário)
                configurationOptions.AddInMemoryCollection(JsonConfigurationMerger.MergeJsonFilesToDictionary([.. serilogJsonFiles]));
            }
        }
    }

    public void ConfigureHost(WebApplicationBuilder builder)
    {
        if (this._options.Serilog?.Enabled == true)
        {
            // ============================================================================
            // IMPORTANTE: Serilog + OpenTelemetry (Aspire) Coexistindo
            // ============================================================================
            // ANTES: UseSerilog() substituía TODOS os providers (incluindo OpenTelemetry) ❌
            // AGORA: AddSerilog() adiciona Serilog SEM remover OpenTelemetry ✅
            //
            // Resultado:
            //   ILogger → OpenTelemetry Provider → Aspire Dashboard (logs estruturados) ✅
            //          → Serilog Provider → PostgreSQL + Console ✅
            // ============================================================================

            // Criar o logger Serilog configurado
            // O OpenTelemetrySink está configurado no Serilog.json (seção WriteTo)
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                //.Destructure.ToMaximumDepth(4)
                //.Destructure.ByTransforming<Exception>(ex => new
                //{
                //    Type = ex.GetType().FullName,
                //    Message = ex.Message,
                //    StackTrace = ex.StackTrace ?? ""
                //})
                .CreateLogger();

            // ============================================================================
            // Limpar providers padrão do ASP.NET Core (Console, Debug, etc)
            // ============================================================================
            // MOTIVO: O Serilog já tem seu próprio Console Sink configurado no Serilog.json
            // Se não limpar, teremos logs duplicados no console:
            //   - Console provider do ASP.NET Core (formato: "info: Namespace[0] Message")
            //   - Console sink do Serilog (formato: "2026-01-10 HH:mm:ss.fff Level Message")
            // ============================================================================
            builder.Logging.ClearProviders();

            // Adicionar Serilog como o ÚNICO provider de logging
            // Serilog gerencia todos os destinos via Serilog.json (Console, PostgreSQL, OpenTelemetry)
            builder.Logging.AddSerilog(Log.Logger, dispose: true);

            // Registrar DiagnosticContext necessário para UseSerilogRequestLogging()
            // UseSerilog() faz isso automaticamente, mas como usamos AddSerilog(), 
            // precisamos registrar manualmente (ambos os tipos: concreto + interface)
            var diagnosticContext = new Serilog.Extensions.Hosting.DiagnosticContext(Log.Logger);
            builder.Services.AddSingleton(diagnosticContext);
            builder.Services.AddSingleton<IDiagnosticContext>(diagnosticContext);
        }
    }

    public void Configure(WebApplication app)
    {
        // Obter logger para logs de inicialização (vai para Aspire + Serilog)
        var logger = app.Services.GetRequiredService<ILogger<LoggingModule>>();

        if (this._options.Serilog?.Enabled == true)
        {
            if (this._options.Serilog.ApiRequestLogging == "All")
            {
                // Adicionar middleware de logging de Request/Response
                // Deve ser adicionado no início do pipeline para capturar tudo
                app.UseMiddleware<ApiRequestLoggingMiddleware>();
                logger.LogInformation("HTTP Request/Response logging enabled");
            }
            else if (this._options.Serilog.ApiRequestLogging == "Basic")
            {
                // ============================================================================
                // Fallback: UseSerilogRequestLogging (logging básico de requisições)
                // ============================================================================
                // Se o ApiRequestLoggingMiddleware estiver desabilitado, usa o middleware
                // básico do Serilog para logar requisições HTTP (sem body/headers)
                // ============================================================================
                app.UseSerilogRequestLogging();
                logger.LogInformation("Serilog request logging enabled (basic mode)");
            }
        }
    }
}