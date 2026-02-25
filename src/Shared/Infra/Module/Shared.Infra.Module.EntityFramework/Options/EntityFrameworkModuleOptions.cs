using Shared.Infra.Module.Base.Attributes;

namespace Shared.Infra.Module.EntityFramework.Options;

/// <summary>
/// Configuration options for EntityFrameworkModule.
/// </summary>
[ModuleOption("EntityFrameworkModule")]
public class EntityFrameworkModuleOptions
{
    public bool? Enabled { get; set; }
    public List<DbContextConfiguration>? DbContexts { get; set; }
    public EntityFrameworkBehaviorOptions? Behavior { get; set; }
}

public static class DbContextConfigurationExtensions
{
    /// <summary>
    /// Resolves config for a DbContext by name. Specific config (matching Name) wins over generic (Name null or empty).
    /// </summary>
    public static DbContextConfiguration? GetConfigForContext(this List<DbContextConfiguration>? configs, string contextTypeName)
    {
        if (configs == null || configs.Count == 0) return null;

        var specific = configs.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Name) && string.Equals(c.Name, contextTypeName, StringComparison.OrdinalIgnoreCase));
        if (specific != null) return specific;

        return configs.FirstOrDefault(c => string.IsNullOrWhiteSpace(c.Name));
    }
}
