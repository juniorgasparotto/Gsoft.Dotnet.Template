using System.Text.Json.Serialization;

namespace SqliteBrowser.Core.Models;

public record DatabaseInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("size")] long Size);
