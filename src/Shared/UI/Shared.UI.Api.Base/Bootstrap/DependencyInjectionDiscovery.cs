namespace Shared.UI.Api.Base.Bootstrap;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shared.Infra.Module.Base;
using Shared.Infra.Module.Base.Attributes;
using System.Reflection;

internal static class DependencyInjectionDiscovery
{
    private record OptionInfo(Type OptionType, string SectionName, Type? ModuleType);

    public static void AddIOptions(IHostApplicationBuilder builder, IEnumerable<object> modules)
    {
        // 0) Crie um record Type OptionType, string SectionName, Type? ModuleType, string? InstanceName.
        // 1) Busca dos construtores dos modulos
        var fromConstructors = CollectFromConstructors(modules, builder.Configuration);
        
        // 2) Obtem dos assemblies.
        var fromAssemblies = CollectFromAssemblies(builder.Configuration);
        
        // 3) Faz a eliminação por duplicidade, se houver 2 com o mesmo tipo e section name diferente, gere exception
        var allOptions = MergeAndValidate(fromConstructors, fromAssemblies);
        
        RegisterOptions(builder, allOptions);
    }

    private static List<OptionInfo> CollectFromConstructors(IEnumerable<object> modules, IConfiguration configuration)
    {
        var options = new List<OptionInfo>();
        
        foreach (var module in modules)
        {
            var moduleType = module as Type ?? (module is IWebModule webModule ? webModule.GetType() : null);
            if (moduleType is null) continue;

            var constructors = moduleType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var ctor in constructors)
            {
                foreach (var param in ctor.GetParameters())
                {
                    var paramType = Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType;
                    
                    if (!paramType.IsGenericType) continue;
                    
                    var genericDef = paramType.GetGenericTypeDefinition();
                    if (genericDef == typeof(IOptions<>) || 
                        genericDef == typeof(IOptionsSnapshot<>) || 
                        genericDef == typeof(IOptionsMonitor<>))
                    {
                        var optionType = paramType.GetGenericArguments()[0];
                        var sectionName = GetSectionName(optionType, moduleType);
                        
                        options.Add(new OptionInfo(optionType, sectionName, moduleType));
                    }
                }
            }
        }
        
        return options;
    }

    private static List<OptionInfo> CollectFromAssemblies(IConfiguration configuration)
    {
        var options = new List<OptionInfo>();
        var assemblies = GetAssembliesToScan();
        
        foreach (var assembly in assemblies)
        {
            var optionTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface)
                .Where(t => t.GetCustomAttribute<ModuleOptionAttribute>(inherit: false) != null);
            
            foreach (var optionType in optionTypes)
            {
                var attribute = optionType.GetCustomAttribute<ModuleOptionAttribute>(inherit: false);
                var sectionName = GetSectionName(optionType);
                
                options.Add(new OptionInfo(optionType, sectionName, null));
            }
        }
        
        return options;
    }

    private static List<OptionInfo> MergeAndValidate(List<OptionInfo> fromConstructors, List<OptionInfo> fromAssemblies)
    {
        var merged = new Dictionary<Type, OptionInfo>();
        
        foreach (var option in fromConstructors)
        {
            if (merged.TryGetValue(option.OptionType, out var existing) && existing.SectionName != option.SectionName)
            {
                throw new InvalidOperationException(
                    $"Tipo de opções '{option.OptionType.FullName}' foi encontrado com section names diferentes: " +
                    $"'{existing.SectionName}' e '{option.SectionName}'. Cada tipo de opções deve ter apenas um section name.");
            }
            
            merged[option.OptionType] = option;
        }
        
        foreach (var option in fromAssemblies)
        {
            if (merged.TryGetValue(option.OptionType, out var existing) && existing.SectionName != option.SectionName)
            {
                throw new InvalidOperationException(
                    $"Tipo de opções '{option.OptionType.FullName}' foi encontrado com section names diferentes: " +
                    $"'{existing.SectionName}' e '{option.SectionName}'. Cada tipo de opções deve ter apenas um section name.");
            }
            
            merged[option.OptionType] = option;
        }
        
        return merged.Values.ToList();
    }

    private static void RegisterOptions(IHostApplicationBuilder builder, List<OptionInfo> options)
    {
        foreach (var option in options)
        {
            if (string.IsNullOrWhiteSpace(option.SectionName))
            {
                // instanceName não está implementado, mas poderá ser futuramente quando um Modulo tiver um sectionName especifico
                ConfigureOptionsByReflection(builder.Services, option.OptionType, instanceName: null, null, builder.Configuration);
            }
            else
            {
                var section = builder.Configuration.GetSection(option.SectionName);
                if (!section.Exists()) continue;

                // instanceName não está implementado, mas poderá ser futuramente quando um Modulo tiver um sectionName especifico
                ConfigureOptionsByReflection(builder.Services, option.OptionType, instanceName: null, section);
            }
        }
    }

    private static IEnumerable<Assembly> GetAssembliesToScan()
    {
        var assemblies = new HashSet<Assembly>();
        var entryAssembly = Assembly.GetEntryAssembly();

        if (entryAssembly != null)
        {
            assemblies.Add(entryAssembly);

            foreach (var referencedAssemblyName in entryAssembly.GetReferencedAssemblies())
            {
                try
                {
                    var referencedAssembly = Assembly.Load(referencedAssemblyName);
                    var assemblyName = referencedAssemblyName.Name ?? "";
                    
                    if (!assemblyName.StartsWith("System", StringComparison.OrdinalIgnoreCase) &&
                        !assemblyName.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) &&
                        !assemblyName.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase) &&
                        !assemblyName.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase))
                    {
                        assemblies.Add(referencedAssembly);
                    }
                }
                catch
                {
                    // Ignora assemblies que não podem ser carregados
                }
            }
        }

        var currentAssembly = Assembly.GetExecutingAssembly();
        if (currentAssembly != null)
        {
            assemblies.Add(currentAssembly);
        }

        return assemblies;
    }

    private static string GetSectionName(Type optionsType, Type? moduleType = null)
    {
        var sectionNameAttribute = optionsType.GetCustomAttribute<ModuleOptionAttribute>(inherit: false);
        if (sectionNameAttribute != null)
            return sectionNameAttribute.Name;

        // Se não tiver atributo é pq a classe é usada apenas no modulo em questao
        return moduleType?.Name ?? optionsType.Name;
    }


    private static void ConfigureOptionsByReflection(
        IServiceCollection services,
        Type optionsType,
        string? instanceName = null,
        IConfigurationSection? section = null,
        IConfiguration? configuration = null)
    {
        var methods = typeof(OptionsConfigurationServiceCollectionExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == nameof(OptionsConfigurationServiceCollectionExtensions.Configure) && m.IsGenericMethodDefinition)
            .Select(m => new { Method = m, Params = m.GetParameters() });

        if (instanceName == null)
        {
            // Chama: services.Configure<TOptions>(IConfiguration config)
            // (é o caminho mais comum e compatível)
            var method = methods
                .FirstOrDefault(x =>
                    x.Method.GetGenericArguments().Length == 1 &&
                    x.Params.Length == 2 &&
                    x.Params[0].ParameterType == typeof(IServiceCollection) &&
                    typeof(IConfiguration).IsAssignableFrom(x.Params[1].ParameterType))
                ?.Method;

            if (method is null)
                throw new InvalidOperationException("Não foi possível localizar o método services.Configure<TOptions>(IConfiguration).");

            method.MakeGenericMethod(optionsType).Invoke(null, [services, section ?? configuration]);
        }
        else
        {
            var method = methods
                .FirstOrDefault(x =>
                    x.Method.GetGenericArguments().Length == 1 &&
                    x.Params.Length == 3 &&
                    x.Params[0].ParameterType == typeof(IServiceCollection) &&
                    x.Params[1].ParameterType == typeof(string) &&
                    typeof(IConfiguration).IsAssignableFrom(x.Params[2].ParameterType))
                ?.Method;

            if (method is null)
                throw new InvalidOperationException("Não foi possível localizar o método services.Configure<TOptions>(IConfiguration).");

            method.MakeGenericMethod(optionsType).Invoke(null, [services, instanceName, section ?? configuration]);
        }
    }
}
