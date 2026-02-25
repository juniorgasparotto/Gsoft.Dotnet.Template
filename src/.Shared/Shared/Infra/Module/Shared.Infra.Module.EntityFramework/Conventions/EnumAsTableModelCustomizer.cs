namespace Shared.Infra.Module.EntityFramework.Conventions;

using System.ComponentModel;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using Shared.Infra.Module.EntityFramework.Options;

/// <summary>
/// Cria tabelas para enums usando SharedTypeEntity&lt;Dictionary&gt;.
/// Fluent (.HasEnumTable().HasData()) ganha sobre config JSON. Ambos convivem.
/// </summary>
public class EnumAsTableModelCustomizer : RelationalModelCustomizer
{
    private readonly DbContextConfiguration _config;

    public EnumAsTableModelCustomizer(
        ModelCustomizerDependencies dependencies,
        DbContextConfiguration config)
        : base(dependencies)
    {
        _config = config;
    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        var model = modelBuilder.Model;
        var convention = GetConventionConfig(context) ?? new ConventionConfiguration { Enabled = true, HasData = true };

        // 1) Fluent (.HasEnumTable) - ganha sempre
        var fluentEnums = GetFluentEnumConfig(model);

        // 2) JSON - só se Enabled e não sobrescrito
        var jsonFiltered = convention.Enabled
            ? FilterEnums(GetAllEnumTypes(model), convention)
            : new Dictionary<string, Type>(StringComparer.Ordinal);

        // 3) Merge: fluent + JSON (fluent sobrescreve quando mesmo enum)
        var allEnums = new Dictionary<string, (Type Type, string TableName, bool Seed)>(StringComparer.Ordinal);
        foreach (var (tableName, type, seed) in fluentEnums)
            allEnums[type.Name] = (type, tableName, seed);

        foreach (var (name, type) in jsonFiltered)
        {
            if (!allEnums.ContainsKey(name))
                allEnums[name] = (type, name, convention.HasData);
        }

        foreach (var (_, (enumType, enumName, seed)) in allEnums)
        {
            var seedData = seed ? BuildSeedData(enumType) : [];

            modelBuilder.SharedTypeEntity<Dictionary<string, object>>(enumName, b =>
            {
                b.Property<int>("Id").ValueGeneratedNever();
                b.Property<string>("Name")
                    .HasMaxLength(100)
                    .IsRequired();
                b.Property<DateTime>("CreatedDate");
                b.Property<string>("CreatedBy")
                    .HasMaxLength(100);
                b.HasKey("Id");
                b.ToTable(enumName);
                if (seedData.Length > 0)
                    b.HasData(seedData);
            });
        }

        //foreach (var entityType in model.GetEntityTypes())
        //{
        //    if (entityType.ClrType == typeof(Dictionary<string, object>))
        //        continue;

        //    foreach (var property in entityType.GetDeclaredProperties())
        //    {
        //        if (!property.ClrType.IsEnum)
        //            continue;

        //        var enumName = property.ClrType.Name;
        //        if (!allEnums.ContainsKey(enumName))
        //            continue;

        //        modelBuilder.Entity(entityType.ClrType)
        //            .Property(property.Name)
        //            .IsRequired()
        //            .HasConversion<int>();
        //    }
        //}
    }

    private static List<(string TableName, Type Type, bool Seed)> GetFluentEnumConfig(IReadOnlyModel model)
    {
        var result = new List<(string, Type, bool)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entityType in model.GetEntityTypes())
        {
            if (entityType.ClrType == typeof(Dictionary<string, object>))
                continue;

            foreach (var property in entityType.GetDeclaredProperties())
            {
                if (!property.ClrType.IsEnum)
                    continue;

                var annValue = property.FindAnnotation(PropertyBuilderEnumExtensions.AnnotationKey)?.Value as string;
                if (annValue == null)
                    continue;

                var (tableName, seed) = PropertyBuilderEnumExtensions.ParseAnnotation(annValue);
                var type = property.ClrType;
                if (seen.Add(type.Name))
                    result.Add((tableName ?? type.Name, type, seed));
            }
        }
        return result;
    }

    private static Dictionary<string, Type> GetAllEnumTypes(IReadOnlyModel model)
    {
        var dict = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var entityType in model.GetEntityTypes())
        {
            foreach (var property in entityType.GetDeclaredProperties())
            {
                if (property.ClrType.IsEnum && !dict.ContainsKey(property.ClrType.Name))
                    dict[property.ClrType.Name] = property.ClrType;
            }
        }
        return dict;
    }

    private ConventionConfiguration? GetConventionConfig(DbContext context)
    {
        var contextName = context.GetType().Name;
        return _config?.Conventions?.FirstOrDefault(c =>
            nameof(EnumAsTableModelCustomizer).Equals(c.Type, StringComparison.OrdinalIgnoreCase));
    }

    private static Dictionary<string, Type> FilterEnums(
        Dictionary<string, Type> enumTypesByName,
        ConventionConfiguration convention)
    {
        var include = convention.EnumInclude ?? ["*"];
        var exclude = convention.EnumExclude ?? [];
        var excludeSet = new HashSet<string>(exclude, StringComparer.OrdinalIgnoreCase);

        return enumTypesByName
            .Where(kv =>
            {
                if (excludeSet.Contains(kv.Key))
                    return false;
                if (include.Contains("*", StringComparer.OrdinalIgnoreCase))
                    return true;
                return include.Contains(kv.Key, StringComparer.OrdinalIgnoreCase);
            })
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
    }

    private static object[] BuildSeedData(Type enumType)
    {
        var values = Enum.GetValues(enumType);
        var result = new List<object>();
        var seedDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        foreach (var value in values)
        {
            var id = Convert.ToInt32(value);
            var name = GetEnumValueName(enumType, value);
            result.Add(new Dictionary<string, object>
            {
                ["Id"] = id,
                ["Name"] = name,
                ["CreatedDate"] = seedDate,
                ["CreatedBy"] = "Seed"
            });
        }

        return [.. result];
    }

    private static string GetEnumValueName(Type enumType, object value)
    {
        var name = Enum.GetName(enumType, value) ?? value.ToString()!;
        var field = enumType.GetField(name);
        if (field != null)
        {
            var desc = field.GetCustomAttribute<DescriptionAttribute>();
            if (!string.IsNullOrEmpty(desc?.Description))
                return desc.Description;
        }
        return name;
    }
}
