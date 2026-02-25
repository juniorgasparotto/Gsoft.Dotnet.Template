namespace Shared.Core.Attributes;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Base attribute for marking classes to be auto-registered in the DI container.
/// Use <see cref="InjectAsSingletonAttribute"/>, <see cref="InjectAsTransientAttribute"/>,
/// or <see cref="InjectAsScopedAttribute"/> instead.
/// Scanned by ServiceScanModule (default when no builder configuration is passed).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public abstract class InjectAsAttribute : Attribute
{
    /// <summary>
    /// Gets the service lifetime for DI registration.
    /// </summary>
    public abstract ServiceLifetime Lifetime { get; }

    /// <summary>
    /// When true, registers the class as itself (AsSelf). When false (default), registers as implemented interfaces.
    /// Classes without interfaces are always registered as self (concrete class injectable).
    /// </summary>
    public bool AsSelf { get; set; }
}
