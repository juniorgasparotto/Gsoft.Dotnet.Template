namespace Shared.Infra.Module.ServiceScan;

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infra.Module.Base;
using Shared.Infra.Module.Base.Attributes;

/// <summary>
/// Module that registers services by convention using Scrutor assembly scanning.
/// Default (when no builder config): scans entry assembly + referenced application assemblies for
/// <see cref="Shared.Core.Attributes.InjectAsSingletonAttribute"/>,
/// <see cref="Shared.Core.Attributes.InjectAsTransientAttribute"/>,
/// <see cref="Shared.Core.Attributes.InjectAsScopedAttribute"/>.
/// Use with <see cref="ServiceScanModuleBuilder"/> for custom configuration.
/// </summary>
[Module(
    Title = "Service Scan Module",
    Description = "Registers services marked with [InjectAs*] attributes. Default: scan entry + referenced assemblies. Use builder for custom config."
)]
public class ServiceScanModule(ServiceScanModuleBuilder? builder = null, ILogger<ServiceScanModule>? logger = null) : IWebModule
{
    /// <inheritdoc />
    public void AddConfigurations(WebModuleConfigurationOptions options)
    {
    }

    /// <inheritdoc />
    public void ConfigureHost(WebApplicationBuilder webBuilder)
    {
        builder ??= new ServiceScanModuleBuilder();
        var count = builder.ScanActions.Count;
        if (count == 0)
        {
            var assemblies = GetApplicationAssemblies();
            if (assemblies.Count != 0)
            {
                foreach (var assembly in assemblies)
                {
                    ServiceScanModuleBuilder.RunDefaultAttributeScan(webBuilder.Services, assembly);
                }
                logger?.LogDebug("ServiceScanModule: default scan by attribute on {Count} assembly/assemblies", assemblies.Count);
            }
            else
            {
                logger?.LogWarning("ServiceScanModule: no scan rules and no application assemblies found, skipping");
            }
        }
        else
        {
            foreach (var action in builder.ScanActions)
            {
                action(webBuilder.Services);
            }
            logger?.LogDebug("ServiceScanModule: executed {Count} scan rule(s)", count);
        }
    }

    /// <inheritdoc />
    public void Configure(WebApplication app)
    {
    }

    /// <summary>
    /// Returns entry assembly + referenced application assemblies (excludes System*, Microsoft*, etc).
    /// Ensures services with [InjectAs*] in Core/Infra projects are discovered.
    /// </summary>
    private static HashSet<Assembly> GetApplicationAssemblies()
    {
        var assemblies = new HashSet<Assembly>();
        var entryAssembly = Assembly.GetEntryAssembly();

        if (entryAssembly is null)
            return assemblies;

        assemblies.Add(entryAssembly);

        foreach (var refName in entryAssembly.GetReferencedAssemblies())
        {
            try
            {
                var assembly = Assembly.Load(refName);
                var name = refName.Name ?? "";

                if (!name.StartsWith("System", StringComparison.OrdinalIgnoreCase) &&
                    !name.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) &&
                    !name.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase) &&
                    !name.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase))
                {
                    assemblies.Add(assembly);
                }
            }
            catch
            {
                // Ignora assemblies que não podem ser carregados
            }
        }

        return assemblies;
    }
}
