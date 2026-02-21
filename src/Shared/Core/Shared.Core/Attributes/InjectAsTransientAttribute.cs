namespace Shared.Core.Attributes;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Marks a class to be auto-registered in the DI container with transient lifetime.
/// Scanned by ServiceScanModule (default when no builder configuration is passed).
/// </summary>
/// <example>
/// <code>
/// [InjectAsTransient]
/// public class EmailValidator : IValidator&lt;EmailRequest&gt; { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class InjectAsTransientAttribute : InjectAsAttribute
{
    /// <inheritdoc />
    public override ServiceLifetime Lifetime => ServiceLifetime.Transient;
}
