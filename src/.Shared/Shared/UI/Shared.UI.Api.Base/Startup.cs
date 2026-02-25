namespace Shared.UI.Api.Base;

using DotNetEnv;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infra.Module.Base;
using Shared.UI.Api.Base.Bootstrap;
using Shared.UI.Api.Base.Sdk;
using System.Reflection;

public sealed class Startup
{
    private readonly List<object> _modulesDefinitions = [];
    private WebApplicationBuilder builder;
    private readonly string[] args;
    private IConfigurationResolver? _configurationResolver;

    public static Startup New(string[] args, bool loadEnvFile = true)
    {
        if (loadEnvFile) LoadEnvFile();

        // GetEntryAssembly() returns "ef" when running under dotnet ef (design-time).
        // GetCallingAssembly() returns the app's Program assembly (API or migrations project).
        var entry = Assembly.GetEntryAssembly()?.GetName().Name;
        var mainAssemblyName = string.Equals(entry, "ef", StringComparison.OrdinalIgnoreCase)
            ? Assembly.GetCallingAssembly().GetName().Name
            : entry;

        Environment.SetEnvironmentVariable("SHARED_MAIN_ASSEMBLY", mainAssemblyName);
        Environment.SetEnvironmentVariable("SHARED_MAIN_ASSEMBLY_PREFIX", mainAssemblyName?.Split(".").First() ?? "");
        return new Startup(args);
    }

    private Startup(string[] args)
    {
        this.args = args;

        this.builder = WebApplication.CreateBuilder(args);

        // Adiciona suporte a ILogger
        builder.Services.AddLogging(b => b.AddConsole());

        // Garante que o pipeline de Options está disponível
        builder.Services.AddOptions();
    }

    private static void LoadEnvFile()
    {
        var baseDir = AppContext.BaseDirectory;
        foreach (var name in new[] { ".env", "build.env" })
        {
            var envPath = Path.Combine(baseDir, name);
            if (File.Exists(envPath))
                Env.Load(envPath);
        }
    }

    /// <summary>
    /// Adiciona um SDK (conjunto pré-definido de módulos via Action no SdkRegistry).
    /// Use AddModule/RemoveModule para customizar após AddSdk.
    /// </summary>
    public Startup AddSdk(SdkType sdkType)
    {
        SdkRegistry.Apply(sdkType, this);
        return this;
    }

    /// <summary>
    /// Remove um módulo do conjunto e do DI. Útil para customizar após AddSdk.
    /// </summary>
    public Startup RemoveModule<TModule>() where TModule : class, IWebModule
    {
        var type = typeof(TModule);
        _modulesDefinitions.RemoveAll(m => GetModuleType(m) == type);
        foreach (var d in builder.Services.Where(x => x.ServiceType == type).ToList())
            builder.Services.Remove(d);

        // TODO: Remover tb os options no futuro, 
        // mas precisa pensar em mais um modulo usando o mesmo option

        return this;
    }

    private static Type GetModuleType(object m) => m is Type t ? t : m.GetType();

    /// <summary>
    /// Registra um módulo de startup por tipo.
    /// O módulo deve implementar IWebModule.
    /// </summary>
    public Startup AddModule<TModule>() where TModule : class, IWebModule
    {
        var type = typeof(TModule);
        if (_modulesDefinitions.Any(m => GetModuleType(m) == type))
            return this;

        this.builder.Services.AddSingleton<TModule>();
        this._modulesDefinitions.Add(type);
        return this;
    }

    /// <summary>
    /// Registra um módulo de startup por tipo.
    /// O módulo deve implementar IWebModule.
    /// </summary>
    public Startup AddModule<TModule, TConstructorOptions>(TConstructorOptions options)
        where TModule : class, IWebModule where TConstructorOptions : class
    {
        this.builder.Services.AddSingleton<TModule>();
        this.builder.Services.AddSingleton(options);
        this._modulesDefinitions.Add(typeof(TModule));
        return this;
    }

    /// <summary>
    /// Registra uma instância de módulo de startup.
    /// O módulo deve implementar IWebModule.
    /// </summary>
    public Startup AddModule(IWebModule module)
    {
        this.builder.Services.AddSingleton(module);
        this._modulesDefinitions.Add(module);
        return this;
    }

    /// <summary>
    /// Registra múltiplos módulos de uma vez.
    /// </summary>
    public Startup AddModules(params IWebModule[] modules)
    {
        foreach (var module in modules)
            this.AddModule(module);

        return this;
    }

    /// <summary>
    /// Registra um ConfigurationResolver customizado por tipo.
    /// O tipo deve implementar IConfigurationResolver.
    /// </summary>
    public Startup AddConfigurationResolver<T>() where T : class, IConfigurationResolver
    {
        this._configurationResolver = Activator.CreateInstance<T>();
        return this;
    }

    /// <summary>
    /// Registra uma instância de ConfigurationResolver customizado.
    /// </summary>
    public Startup AddConfigurationResolver(IConfigurationResolver resolver)
    {
        this._configurationResolver = resolver;
        return this;
    }

    /// <summary>
    /// Executa a aplicação web.
    /// </summary>
    public async Task RunAsync()
    {
        // ============================================================================
        // FASE 1: Carregar configurações de TODOS os módulos (método estático)
        // ============================================================================
        var resolver = _configurationResolver ?? ConfigurationResolver.Default;
        resolver.CreateBootstrapConfiguration(builder, _modulesDefinitions, args);

        // Descobrir e registrar IOptions<T> dos construtores dos módulos e assemblies
        DependencyInjectionDiscovery.AddIOptions(builder, _modulesDefinitions);

        // ============================================================================
        // FASE 2: Criar os modulos
        // ============================================================================

        WebApplication app;

        /// TODO: Melhorar isso aqui pra nao duplicar instancias, talvez no AddModule, usar outro ServiceCollections
        await using (var bootstrapProvider = builder.Services.BuildServiceProvider())
        {
            var instancies = ModuleResolver.CreateModules(builder, bootstrapProvider, _modulesDefinitions);

            // Listar todos os módulos carregados
            StartupInfo.ListLoadedModules(instancies);

            // ============================================================================
            // FASE 3: Configurar IConfiguration da aplicação final
            // ============================================================================
            resolver.ConfigureApplicationConfiguration(builder, instancies, args);

            // Configurações padrão
            builder.Services.AddHttpClient();
            builder.Services.AddHttpContextAccessor(); // Necessário para Serilog.Enrichers.CorrelationId

            // Adicionar controllers do assembly de entrada + assembly WebApi (módulo)
            var mvc = builder.Services.AddControllers()
                .AddApplicationPart(typeof(Startup).Assembly) // ✅ Controllers desse projeto
                .AddDataAnnotationsLocalization(); // ✅ Localização de mensagens de validação (ex: pt-BR, en-US, etc.)

            foreach (var module in instancies)
            {
                mvc.AddApplicationPart(module.GetType().Assembly);
            }

            // Executar ConfigureHost de todos os módulos na ordem
            foreach (var module in instancies)
            {
                module.ConfigureHost(builder);
            }

            // Build da aplicação
            app = builder.Build();

            // Habilita a localização de mensagens de validação
            app.UseRequestLocalization();

            // Executar Configure de todos os módulos na ordem (middlewares primeiro!)
            foreach (var module in instancies)
            {
                module.Configure(app);
            }

            // Configurações padrão do app (DEPOIS dos módulos para middlewares funcionarem)
            app.MapControllers();
        }

        await app.RunAsync();
    }
}
