namespace Shared.Core.Attributes;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Marks a class to be auto-registered in the DI container with scoped lifetime.
/// Scanned by ServiceScanModule (default when no builder configuration is passed).
/// </summary>
/// <example>
/// <code>
/// [InjectAsScoped]
/// public class OrderService : IOrderService { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class InjectAsScopedAttribute : InjectAsAttribute
{
    /// <inheritdoc />
    public override ServiceLifetime Lifetime => ServiceLifetime.Scoped;
}
