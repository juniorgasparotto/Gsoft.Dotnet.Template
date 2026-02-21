namespace SqliteBrowser.Core.Models;

public record ExecuteResult
{
    public List<string>? Columns { get; init; }
    public List<object?[]>? Rows { get; init; }
    public int? RowsAffected { get; init; }
}
