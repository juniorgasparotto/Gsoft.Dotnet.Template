namespace Shared.Core.Attributes;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Marks a class to be auto-registered in the DI container with singleton lifetime.
/// Scanned by ServiceScanModule (default when no builder configuration is passed).
/// </summary>
/// <example>
/// <code>
/// [InjectAsSingleton]
/// public class CacheService : ICacheService { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class InjectAsSingletonAttribute : InjectAsAttribute
{
    /// <inheritdoc />
    public override ServiceLifetime Lifetime => ServiceLifetime.Singleton;
}
