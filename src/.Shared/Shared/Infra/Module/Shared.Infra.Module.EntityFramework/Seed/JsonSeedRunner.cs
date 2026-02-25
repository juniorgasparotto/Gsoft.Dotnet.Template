namespace Shared.Infra.Module.EntityFramework.Seed;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Reflection;
using System.Text.Json;

/// <summary>
/// Seed result per file. When Error is set, it's an error row (R/I/U are 0).
/// Deleted is set when mode is UpsertOrDelete and orphans were removed.
/// </summary>
public sealed record SeedResult(string Context, string Entity, string File, int Read, int Inserted, int Updated, string? Error = null, int Deleted = 0);

/// <summary>
/// Full error for display below the table.
/// </summary>
public sealed record SeedError(string File, string Message);

/// <summary>
/// Executes seed of entities from JSON files.
/// For each item: checks by primary key if it exists. If it exists, updates; otherwise inserts.
/// Format: [{"Type":"TodoItem","Data":[{...},...]}]
/// </summary>
public static class JsonSeedRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Executes the seed and returns the results (for table display at the end).
    /// </summary>
    public static (IReadOnlyList<SeedResult> Results, IReadOnlyList<SeedError> Errors) Run(IServiceProvider sp, Type contextType, Options.SeedConfiguration seed)
    {
        var results = new List<SeedResult>();
        var errors = new List<SeedError>();
        var files = ResolveFilesByGrob(seed.Path!).ToList();
        var mode = (seed.Mode ?? "Upsert").Trim();
        var isUpsertOrDelete = string.Equals(mode, "UpsertOrDelete", StringComparison.OrdinalIgnoreCase);

        // Collect all keys from ALL JSONs (for UpsertOrDelete: delete DB rows not in this set)
        var keysInJson = new Dictionary<Type, HashSet<SeedKey>>();

        foreach (var jsonPath in files)
        {
            if (!File.Exists(jsonPath)) continue;

            var filePath = Path.GetRelativePath(AppContext.BaseDirectory, jsonPath);

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
            }
            catch (Exception ex)
            {
                results.Add(new SeedResult(contextType.Name, "-", filePath, 0, 0, 0, ex.Message));
                errors.Add(new SeedError(filePath, ex.Message));
                continue;
            }

            using (doc)
            {
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    var msg = "Root must be a JSON array";
                    results.Add(new SeedResult(contextType.Name, "-", filePath, 0, 0, 0, msg));
                    errors.Add(new SeedError(filePath, msg));
                    continue;
                }

                foreach (var block in doc.RootElement.EnumerateArray())
                {
                    if (block.ValueKind != JsonValueKind.Object) continue;

                    var typeName = block.TryGetProperty("Type", out var tp) ? tp.GetString() : null;
                    if (string.IsNullOrWhiteSpace(typeName)) continue;

                    if (!block.TryGetProperty("Data", out var dataEl) || dataEl.ValueKind != JsonValueKind.Array)
                        continue;

                    var entityType = ResolveEntityType(contextType, typeName);
                    if (entityType is null)
                    {
                        var msg = $"Unknown entity type: {typeName}";
                        results.Add(new SeedResult(contextType.Name, typeName, filePath, 0, 0, 0, msg));
                        errors.Add(new SeedError(filePath, msg));
                        continue;
                    }

                    try
                    {
                        var items = new List<object>();
                        foreach (var item in dataEl.EnumerateArray())
                        {
                            var obj = JsonSerializer.Deserialize(item.GetRawText(), entityType, JsonOptions);
                            if (obj != null) items.Add(obj);
                        }

                        if (items.Count == 0)
                        {
                            results.Add(new SeedResult(contextType.Name, entityType.Name, filePath, 0, 0, 0, "Empty Data"));
                            continue;
                        }

                        if (isUpsertOrDelete)
                            CollectKeys(sp, contextType, keysInJson, entityType, items);

                        var (inserted, updated) = SeedEntities(sp, contextType, entityType, items);

                        results.Add(new SeedResult(
                            contextType.Name,
                            entityType.Name,
                            filePath,
                            items.Count,
                            inserted,
                            updated));
                    }
                    catch (Exception ex)
                    {
                        results.Add(new SeedResult(contextType.Name, entityType.Name, filePath, 0, 0, 0, ex.Message));
                        errors.Add(new SeedError(filePath, ex.Message));
                    }
                }
            }
        }

        if (isUpsertOrDelete && keysInJson.Count > 0)
        {
            foreach (var (entityType, keySet) in keysInJson)
            {
                try
                {
                    var deleted = DeleteOrphans(sp, contextType, entityType, keySet);
                    if (deleted > 0)
                        results.Add(new SeedResult(contextType.Name, entityType.Name, "(deleting orphans)", 0, 0, 0, Deleted: deleted));
                }
                catch (Exception ex)
                {
                    results.Add(new SeedResult(contextType.Name, entityType.Name, "(deleting orphans)", 0, 0, 0, ex.Message));
                    errors.Add(new SeedError("(orphans)", ex.Message));
                }
            }
        }

        return (results, errors);
    }

    private static void CollectKeys(IServiceProvider sp, Type contextType, Dictionary<Type, HashSet<SeedKey>> keysInJson, Type entityType, List<object> items)
    {
        var context = sp.GetRequiredService(contextType);
        if (context is not DbContext dbContext) return;

        var entityTypeMetadata = dbContext.Model.FindEntityType(entityType);
        var primaryKey = entityTypeMetadata?.FindPrimaryKey();
        if (primaryKey is null || primaryKey.Properties.Count == 0) return;

        var keyProps = primaryKey.Properties
            .Select(p => entityType.GetProperty(p.Name))
            .Where(p => p != null)
            .Cast<PropertyInfo>()
            .ToList();
        if (keyProps.Count == 0) return;

        if (!keysInJson.TryGetValue(entityType, out var set))
        {
            set = new HashSet<SeedKey>();
            keysInJson[entityType] = set;
        }
        foreach (var item in items)
            set.Add(GetKey(keyProps, item));
    }

    private static Type? ResolveEntityType(Type contextType, string typeName)
    {
        var dbSetDef = typeof(DbSet<>);

        var entityTypes = contextType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType &&
                        p.PropertyType.GetGenericTypeDefinition() == dbSetDef)
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .Where(t => t.IsClass);

        // accepts "TodoItem" or full namespace
        return entityTypes.FirstOrDefault(t =>
            string.Equals(t.Name, typeName, StringComparison.Ordinal) ||
            string.Equals(t.FullName, typeName, StringComparison.Ordinal));
    }

    private static (int Inserted, int Updated) SeedEntities(IServiceProvider sp, Type contextType, Type entityType, List<object> items)
    {
        if (items.Count == 0) return (0, 0);

        var context = sp.GetRequiredService(contextType);
        if (context is not DbContext dbContext) return (0, 0);

        var entityTypeMetadata = dbContext.Model.FindEntityType(entityType);
        var primaryKey = entityTypeMetadata?.FindPrimaryKey();
        if (primaryKey is null || primaryKey.Properties.Count == 0)
            return (0, 0);

        var keyProps = primaryKey.Properties
            .Select(p => entityType.GetProperty(p.Name))
            .Where(p => p != null)
            .Cast<PropertyInfo>()
            .ToList();

        if (keyProps.Count == 0) return (0, 0);

        var query = dbContext.SetDynamic(entityType.Name);
        var existingList = query.Cast<object>().ToList();
        var addedInBatch = new List<object>();

        var inserted = 0;
        var updated = 0;

        foreach (var seedItem in items)
        {
            var existing = existingList.FirstOrDefault(e => KeyMatches(keyProps, e, seedItem))
                ?? addedInBatch.FirstOrDefault(e => KeyMatches(keyProps, e, seedItem));

            if (existing is not null)
            {
                dbContext.Entry(existing).CurrentValues.SetValues(seedItem);
                updated++;
            }
            else
            {
                dbContext.Add(seedItem);
                addedInBatch.Add(seedItem);
                inserted++;
            }
        }

        if (inserted > 0 || updated > 0)
            dbContext.SaveChanges();

        return (inserted, updated);
    }

    private static bool KeyMatches(IEnumerable<PropertyInfo> keyProps, object a, object b)
    {
        foreach (var prop in keyProps)
        {
            var va = prop.GetValue(a);
            var vb = prop.GetValue(b);
            if (!Equals(va, vb))
                return false;
        }
        return true;
    }

    private static SeedKey GetKey(IEnumerable<PropertyInfo> keyProps, object entity)
    {
        var values = keyProps.Select(p => p.GetValue(entity)).ToArray();
        return new SeedKey(values);
    }

    private static int DeleteOrphans(IServiceProvider sp, Type contextType, Type entityType, HashSet<SeedKey> keysInJson)
    {
        var context = sp.GetRequiredService(contextType);
        if (context is not DbContext dbContext) return 0;

        var entityTypeMetadata = dbContext.Model.FindEntityType(entityType);
        var primaryKey = entityTypeMetadata?.FindPrimaryKey();
        if (primaryKey is null || primaryKey.Properties.Count == 0) return 0;

        var keyProps = primaryKey.Properties
            .Select(p => entityType.GetProperty(p.Name))
            .Where(p => p != null)
            .Cast<PropertyInfo>()
            .ToList();
        if (keyProps.Count == 0) return 0;

        var query = dbContext.SetDynamic(entityType.Name);
        var existingList = query.Cast<object>().ToList();
        var toDelete = new List<object>();
        foreach (var entity in existingList)
        {
            var key = GetKey(keyProps, entity);
            if (!keysInJson.Contains(key))
                toDelete.Add(entity);
        }
        if (toDelete.Count == 0) return 0;
        foreach (var e in toDelete)
            dbContext.Remove(e);
        dbContext.SaveChanges();
        return toDelete.Count;
    }

    /// <summary>
    /// Resolves paths: "**" = all *.json recursive; otherwise single file.
    /// </summary>
    public static IEnumerable<string> ResolveFilesByGrob(string path)
    {
        var matcher = new Matcher();
        matcher.AddInclude(path);

        return matcher.GetResultsInFullPath(AppContext.BaseDirectory);
    }

    private sealed class SeedKey
    {
        public object[] Values { get; }
        public SeedKey(object[] values) => Values = values;
        public override bool Equals(object? obj) => obj is SeedKey other && Values.Length == other.Values.Length &&
            Values.Zip(other.Values, (a, b) => Equals(a, b)).All(x => x);
        public override int GetHashCode()
        {
            var h = new HashCode();
            foreach (var v in Values) h.Add(v);
            return h.ToHashCode();
        }
    }
}
