using System.Text.Json.Serialization;

namespace SqliteBrowser.Core.Models;

public record TableDataResponse(
    [property: JsonPropertyName("columns")] List<string> Columns,
    [property: JsonPropertyName("rows")] List<object?[]> Rows,
    [property: JsonPropertyName("totalRows")] long TotalRows);
