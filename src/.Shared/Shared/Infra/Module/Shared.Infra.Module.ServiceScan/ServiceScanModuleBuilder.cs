namespace Shared.Infra.Module.ServiceScan;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using Shared.Core.Attributes;

/// <summary>
/// Fluent builder for configuring convention-based service registration via Scrutor assembly scanning.
/// </summary>
public class ServiceScanModuleBuilder
{
    internal List<Action<IServiceCollection>> ScanActions { get; } = [];

    /// <summary>
    /// Scans the assembly containing <typeparamref name="T"/> and registers classes marked with
    /// <see cref="InjectAsSingletonAttribute"/>, <see cref="InjectAsTransientAttribute"/>,
    /// or <see cref="InjectAsScopedAttribute"/> using Scrutor.
    /// </summary>
    public ServiceScanModuleBuilder AddClassesWithInjectAttributesFromAssemblyOf<T>()
    {
        ScanActions.Add(services => RunDefaultAttributeScan(services, typeof(T).Assembly));
        return this;
    }

    /// <summary>
    /// Internal: runs the default attribute scan on the given assembly.
    /// Used when no configuration is passed to the module.
    /// </summary>
    internal static void RunDefaultAttributeScan(IServiceCollection services, Assembly assembly)
    {
        AddAttributeScan<InjectAsSingletonAttribute>(services, assembly, ServiceLifetime.Singleton);
        AddAttributeScan<InjectAsTransientAttribute>(services, assembly, ServiceLifetime.Transient);
        AddAttributeScan<InjectAsScopedAttribute>(services, assembly, ServiceLifetime.Scoped);
    }

    private static void AddAttributeScan<TAttr>(IServiceCollection services, Assembly assembly, ServiceLifetime lifetime)
        where TAttr : InjectAsAttribute
    {
        // AsImplementedInterfaces: classe tem interfaces E AsSelf != true
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes
                .WithAttribute<TAttr>()
                .Where(t => t.GetInterfaces().Length > 0 && t.GetCustomAttribute<TAttr>(inherit: false)?.AsSelf != true))
            .AsImplementedInterfaces()
            .WithLifetime(lifetime));

        // AsSelf: todas as classes (permite injetar IMyService OU MyService)
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.WithAttribute<TAttr>())
            .AsSelf()
            .WithLifetime(lifetime));
    }

    /// <summary>
    /// Adds a custom scan rule. Use for full control over assembly scanning.
    /// </summary>
    public ServiceScanModuleBuilder AddScan(Action<IServiceCollection> scanAction)
    {
        ScanActions.Add(scanAction);
        return this;
    }
}
