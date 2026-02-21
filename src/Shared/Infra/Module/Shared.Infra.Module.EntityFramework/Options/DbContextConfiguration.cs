namespace Shared.Infra.Module.EntityFramework.Options;

public class DbContextConfiguration
{
    /// <summary>
    /// When null or empty, this config applies to ALL DbContexts (generic).
    /// When specified, applies only to the DbContext with matching name. Specific config overrides generic.
    /// </summary>
    public string? Name { get; set; }
    public string? DefaultProvider { get; set; }
    public bool EnableLogging { get; set; }
    public SeedConfiguration? Seed { get; set; }
    public List<ConventionConfiguration>? Conventions { get; set; }
    public Dictionary<string, DatabaseProviderOptions>? Providers { get; set; }
}
