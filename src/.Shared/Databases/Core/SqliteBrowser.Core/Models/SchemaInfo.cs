using System.Text.Json.Serialization;

namespace SqliteBrowser.Core.Models;

public record SchemaInfo(
    [property: JsonPropertyName("tables")] List<TableInfo>? Tables = null,
    [property: JsonPropertyName("indexes")] List<IndexInfo>? Indexes = null,
    [property: JsonPropertyName("triggers")] List<TriggerInfo>? Triggers = null)
{
    public List<TableInfo> TablesList => Tables ?? [];
    public List<IndexInfo> IndexesList => Indexes ?? [];
    public List<TriggerInfo> TriggersList => Triggers ?? [];
}

public record TableInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("type")] string Type);

public record IndexInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("table")] string Table);

public record TriggerInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("table")] string Table);
