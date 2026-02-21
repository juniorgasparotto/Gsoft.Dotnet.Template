namespace Shared.Infra.Module.EntityFramework.Conventions;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Fluent API para propriedades enum: tabela lookup + seed. Ganha sobre a config JSON.
/// </summary>
public static class PropertyBuilderEnumExtensions
{
    /// <summary>
    /// Nome da annotation usada internamente. Formato: "TableName|Seed" (string scaffoldable).
    /// </summary>
    internal const string AnnotationKey = "Shared.Infra.Module.EntityFramework.EnumAsTable";

    /// <summary>
    /// Marca a propriedade enum para ter tabela lookup. Nome opcional sobrescreve o nome da tabela (default = nome do enum).
    /// </summary>
    public static PropertyBuilder<TProperty> HasEnumTable<TProperty>(
        this PropertyBuilder<TProperty> builder,
        string? name = null) where TProperty : Enum
    {
        builder.HasAnnotation(AnnotationKey, FormatAnnotation(name, false));
        return builder;
    }

    /// <summary>
    /// Seed com os valores do enum (Id, Name com [Description] quando existir). Deve ser encadeado após HasEnumTable.
    /// </summary>
    public static PropertyBuilder<TProperty> HasData<TProperty>(
        this PropertyBuilder<TProperty> builder) where TProperty : Enum
    {
        var existing = builder.Metadata.FindAnnotation(AnnotationKey)?.Value as string;
        var (name, _) = ParseAnnotation(existing);
        builder.HasAnnotation(AnnotationKey, FormatAnnotation(name, true));
        return builder;
    }

    internal static string FormatAnnotation(string? tableName, bool seed) =>
        $"{(tableName ?? string.Empty)}|{(seed ? "1" : "0")}";

    internal static (string? Name, bool Seed) ParseAnnotation(string? value)
    {
        if (string.IsNullOrEmpty(value)) return (null, false);
        var parts = value.Split('|');
        var name = parts.Length > 0 && !string.IsNullOrEmpty(parts[0]) ? parts[0] : null;
        var seed = parts.Length > 1 && parts[1] == "1";
        return (name, seed);
    }
}
