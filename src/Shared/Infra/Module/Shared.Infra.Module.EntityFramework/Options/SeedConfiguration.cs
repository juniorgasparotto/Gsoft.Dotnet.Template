namespace Shared.Infra.Module.EntityFramework.Options;

/// <summary>
/// Configuration for JSON-based seeds. Used as default; overridden when AddSeedFromJson is used.
/// </summary>
public class SeedConfiguration
{
    /// <summary>
    /// Path pattern for seed JSON files. Supports glob: "**" for recursive.
    /// Ex: "./Configurations/Seeds/DefaultDbContext/**/*.json"
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Seed mode: Upsert (default), UpsertOrDelete.
    /// Upsert: insert or update by primary key.
    /// UpsertOrDelete: same as Upsert, plus deletes DB rows whose keys are not in any JSON.
    /// </summary>
    public string Mode { get; set; } = "Upsert"; // UpsertOrDelete
}
