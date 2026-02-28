using SqliteBrowser.Core.Models;

namespace SqliteBrowser.UI.Services;

public class SqliteBrowserStateService
{
    public List<DatabaseInfo> Databases { get; set; } = [];
    public string SelectedDb { get; set; } = "";
    public SchemaInfo? Schema { get; set; }
    public string? CurrentTable { get; set; }
    public TableDataResponse? TableData { get; set; }
    public int PageSize { get; set; } = 200;
    public int PageOffset { get; set; }
    public string? LastError { get; set; }
    public string DatabasesFolderPath { get; set; } = "";
    public event Action? OnChange;
    public void NotifyStateChanged() => OnChange?.Invoke();
}
