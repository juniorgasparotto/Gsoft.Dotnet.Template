namespace Shared.UI.Api.Base.Bootstrap;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Shared.Infra.Module.Base;
using Shared.Infra.Module.Base.Attributes;
using Steeltoe.Configuration.Placeholder;
using System.Reflection;

/// <summary>
/// Implementação padrão do IConfigurationResolver.
/// </summary>
internal class DefaultConfigurationResolver : IConfigurationResolver
{
    /// <summary>
    /// Cria um IConfiguration bootstrap chamando os métodos estáticos AddConfigurations de todos os módulos.
    /// </summary>
    public void CreateBootstrapConfiguration(
        IHostApplicationBuilder builder,
        IEnumerable<object> modules,
        string[] args
    )
    {
        // Carregar arquivos de configuração padrão da aplicação (appsettings.json e appsettings.{Environment}.json) a partir do diretório base da aplicação
        if (AppContext.BaseDirectory != Directory.GetCurrentDirectory())
        {
            builder.Configuration
                .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true, reloadOnChange: true)
                .AddJsonFile(Path.Combine(AppContext.BaseDirectory, $"appsettings.{builder.Environment.EnvironmentName}.json"), optional: true, reloadOnChange: true);
        }

        // Carregar arquivos de configuração específicos de cada módulo
        AddModulesConfigsIfExists(modules, builder.Configuration);

        // Configuração final antes do build 
        builder.Configuration
            .AddEnvironmentVariables()  // Permite sobrescrever configurações via variáveis de ambiente
            .AddCommandLine(args)       // Permite sobrescrever configurações via linha de comando
            .AddPlaceholderResolver();  // Steeltoe Placeholder Resolver
    }

    /// <summary>
    /// Configura o IConfiguration da aplicação final, chamando AddConfigurations de instância dos módulos.
    /// </summary>
    public void ConfigureApplicationConfiguration(
        WebApplicationBuilder builder,
        IEnumerable<IWebModule> modules,
        string[] args)
    {
        var options = new WebModuleConfigurationOptions(builder);

        // Executar AddConfigurations de instância de todos os módulos na ordem
        foreach (var module in modules)
        {
            module.AddConfigurations(options);
        }

        builder.Configuration
            .AddEnvironmentVariables() // Permite sobrescrever configurações via variáveis de ambiente
            .AddCommandLine(args)      // Permite sobrescrever configurações via linha de comando
            .AddPlaceholderResolver(); // Steeltoe Placeholder Resolver
    }

    private static void AddModulesConfigsIfExists(IEnumerable<object> modules, IConfigurationBuilder configurationBuilder)
    {
        var cache = new List<string>();

        void AddFile(string path)
        {
            if (!cache.Contains(path))
            {
                configurationBuilder.AddJsonFile(path, optional: true, reloadOnChange: true);
                cache.Add(path);
            }
        }

        foreach (var module in modules)
        {
            Type? moduleType = null;

            if (module is Type type)
            {
                moduleType = type;
            }
            else if (module is IWebModule webModule)
            {
                moduleType = webModule.GetType();
            }

            if (moduleType != null)
            {
                var rootPath = Directory.GetCurrentDirectory();
                var binPath = AppContext.BaseDirectory;
                bool configAdded = false;

                // Verificar se o módulo tem o atributo ModuleAttribute com ConfigPath
                var moduleAttribute = moduleType.GetCustomAttribute<ModuleAttribute>(inherit: false);
                if (moduleAttribute != null && !string.IsNullOrWhiteSpace(moduleAttribute.ConfigPath))
                {
                    // Usar o caminho especificado no atributo (relativo)
                    var rootPathJson = Path.Combine(rootPath, moduleAttribute.ConfigPath);
                    var binPathJson = Path.Combine(binPath, moduleAttribute.ConfigPath);

                    // Tentar rootPath primeiro
                    if (File.Exists(rootPathJson))
                    {
                        AddFile(rootPathJson);
                        configAdded = true;
                    }
                    // Se não encontrou no root, tentar binPath
                    else if (File.Exists(binPathJson))
                    {
                        AddFile(binPathJson);
                        configAdded = true;
                    }
                }

                // Se não encontrou pelo atributo, tentar os candidatos padrão
                if (!configAdded)
                {
                    var moduleFullName = moduleType.FullName;
                    var moduleName = moduleType.Name;
                    var ns = moduleType.Namespace;
                    var assemblyName = moduleType.Assembly.GetName().Name;

                    if (!string.IsNullOrEmpty(moduleFullName) && !string.IsNullOrEmpty(moduleName))
                    {
                        // Lista de candidatos para o arquivo de configuração
                        var candidates = new List<string>
                        {
                            // Raiz
                            $"{assemblyName}.json",
                            $"{ns}.json",
                            $"{moduleFullName}.json",
                            $"{moduleName}.json",
                            
                            // Config/
                            $"Configs/{assemblyName}.json",
                            $"Configs/{ns}.json",
                            $"Configs/{moduleFullName}.json",
                            $"Configs/{moduleName}.json",
                            
                            // Settings/
                            $"Settings/{assemblyName}.json",
                            $"Settings/{ns}.json",
                            $"Settings/{moduleFullName}.json",
                            $"Settings/{moduleName}.json",
                            
                            // Options/
                            $"Options/{assemblyName}.json",
                            $"Options/{ns}.json",
                            $"Options/{moduleFullName}.json",
                            $"Options/{moduleName}.json",
                            
                            // Configurations/
                            $"Configurations/{assemblyName}.json",
                            $"Configurations/{ns}.json",
                            $"Configurations/{moduleFullName}.json",
                            $"Configurations/{moduleName}.json",

                             // Module/
                            $"Module/{assemblyName}.json",
                            $"Module/{ns}.json",
                            $"Module/{moduleFullName}.json",
                            $"Module/{moduleName}.json",

                            // Modules/
                            $"Modules/{assemblyName}.json",
                            $"Modules/{ns}.json",
                            $"Modules/{moduleFullName}.json",
                            $"Modules/{moduleName}.json",
                        };

                        // Tentar cada candidato (case-insensitive no Windows)
                        foreach (var candidate in candidates)
                        {
                            var rootPathJson = Path.Combine(rootPath, candidate);
                            var binPathJson = Path.Combine(binPath, candidate);

                            bool added = false;
                            // File.Exists é case-insensitive no Windows
                            if (File.Exists(rootPathJson))
                            {
                                AddFile(rootPathJson);
                                added = true;
                            }
                            else if (File.Exists(binPathJson))
                            {
                                AddFile(binPathJson);
                                added = true;
                            }

                            if (added)
                            {
                                // Faz merge com o arquivo .merge.json se existir, se nao houver o .merge, ele vai sobrescrever com o conteúdo da raiz
                                // e muitas vezes é interessante não sobrescrever tudo, apenas algumas chaves
                                var mergeFile = Path.Combine(Path.GetDirectoryName(binPathJson) ?? "", Path.GetFileNameWithoutExtension(binPathJson) + ".merge.json");
                                if (File.Exists(mergeFile))
                                    AddFile(mergeFile);

                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}

/// <summary>
/// Classe estática com métodos de compatibilidade e instância padrão do ConfigurationResolver.
/// </summary>
internal static class ConfigurationResolver
{
    /// <summary>
    /// Instância padrão do ConfigurationResolver.
    /// </summary>
    public static readonly DefaultConfigurationResolver Default = new();

    /// <summary>
    /// Cria um IConfiguration bootstrap chamando os métodos estáticos AddConfigurations de todos os módulos.
    /// Método estático mantido para compatibilidade.
    /// </summary>
    public static void CreateBootstrapConfiguration(
        IHostApplicationBuilder builder,
        IEnumerable<object> modules,
        string[] args
    )
    {
        Default.CreateBootstrapConfiguration(builder, modules, args);
    }

    /// <summary>
    /// Configura o IConfiguration da aplicação final, chamando AddConfigurations de instância dos módulos.
    /// Método estático mantido para compatibilidade.
    /// </summary>
    public static void ConfigureApplicationConfiguration(
        WebApplicationBuilder builder,
        IEnumerable<IWebModule> modules,
        string[] args)
    {
        Default.ConfigureApplicationConfiguration(builder, modules, args);
    }
}
