using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SqliteBrowser.Core.Models;

namespace SqliteBrowser.UI.Web.Services;

public class SqliteBrowserApiService
{
    private readonly HttpClient _http;

    public SqliteBrowserApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<DatabaseInfo>> GetDatabasesAsync(CancellationToken ct = default)
    {
        var res = await _http.GetAsync("api/databases", ct);
        res.EnsureSuccessStatusCode();
        var list = await res.Content.ReadFromJsonAsync<List<DatabaseInfo>>(ct);
        return list ?? [];
    }

    public async Task<SchemaInfo?> GetSchemaAsync(string dbName, CancellationToken ct = default)
    {
        var res = await _http.GetAsync($"api/databases/{Uri.EscapeDataString(dbName)}/schema", ct);
        if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<SchemaInfo>(ct);
    }

    public async Task<TableDataResponse?> GetTableDataAsync(string dbName, string tableName, int limit = 100, int offset = 0, CancellationToken ct = default)
    {
        var url = $"api/databases/{Uri.EscapeDataString(dbName)}/tables/{Uri.EscapeDataString(tableName)}/data?limit={limit}&offset={offset}";
        var res = await _http.GetAsync(url, ct);
        if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<TableDataResponse>(ct);
    }

    public async Task<(bool Success, List<ExecuteResult>? Results, string? Error)> ExecuteSqlAsync(string dbName, string sql, int? limit = null, CancellationToken ct = default)
    {
        var payload = new { sql, limit };
        var res = await _http.PostAsJsonAsync($"api/databases/{Uri.EscapeDataString(dbName)}/execute", payload, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
        {
            try
            {
                var err = JsonDocument.Parse(body);
                var error = err.RootElement.TryGetProperty("error", out var e) ? e.GetString() : body;
                return (false, null, error);
            }
            catch { return (false, null, body); }
        }
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var results = JsonSerializer.Deserialize<List<ExecuteResult>>(body, options);
        return (true, results, null);
    }

    public async Task<string> GetDatabasesFolderPathAsync(CancellationToken ct = default)
    {
        var res = await _http.GetAsync("api/debug/path", ct);
        res.EnsureSuccessStatusCode();
        var obj = await res.Content.ReadFromJsonAsync<DebugPathResponse>(ct);
        return obj?.DbPath ?? "";
    }

    private record DebugPathResponse([property: JsonPropertyName("dbPath")] string DbPath);
}
