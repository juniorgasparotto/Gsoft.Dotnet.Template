using Microsoft.Data.Sqlite;
using SqliteBrowser.Infra.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Resolve databases path
var configPath = builder.Configuration["Databases:Path"] ?? Environment.GetEnvironmentVariable("DATABASES_PATH");
string dbPath;
if (!string.IsNullOrWhiteSpace(configPath))
{
    dbPath = Path.IsPathRooted(configPath)
        ? configPath
        : Path.GetFullPath(Path.Combine(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..")), configPath));
}
else
{
    dbPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
if (!Directory.Exists(dbPath))
    Directory.CreateDirectory(dbPath);

var defaultDb = Path.Combine(dbPath, "todo.db");
if (!File.Exists(defaultDb))
{
    try
    {
        using var c = new SqliteConnection($"Data Source={defaultDb}");
        c.Open();
    }
    catch { /* ignora */ }
}

builder.Services.AddSingleton(new SqliteBrowserDataService(dbPath));

var app = builder.Build();
app.UseCors();

var dataService = app.Services.GetRequiredService<SqliteBrowserDataService>();

// API: List databases
app.MapGet("/api/databases", async (SqliteBrowserDataService svc) =>
{
    var list = await svc.GetDatabasesAsync();
    return Results.Ok(list);
});

// API: Get schema
app.MapGet("/api/databases/{dbName}/schema", async (string dbName, SqliteBrowserDataService svc) =>
{
    var schema = await svc.GetSchemaAsync(dbName);
    return schema is null ? Results.NotFound() : Results.Ok(schema);
});

// API: Get table structure
app.MapGet("/api/databases/{dbName}/tables/{tableName}/structure", async (string dbName, string tableName, SqliteBrowserDataService svc) =>
{
    var schema = await svc.GetSchemaAsync(dbName);
    if (schema is null) return Results.NotFound();
    // Structure via PRAGMA - DataService doesn't have it, keep inline for now or add to Infra
    var fullPath = Path.Combine(svc.DbPath, dbName);
    if (!File.Exists(fullPath)) return Results.NotFound();
    await using var conn = new SqliteConnection($"Data Source={fullPath}");
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = $"PRAGMA table_info(\"{tableName.Replace("\"", "\"\"")}\")";
    var columns = new List<object>();
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
    {
        columns.Add(new
        {
            cid = r.GetInt64(0),
            name = r.GetString(1),
            type = r.IsDBNull(2) ? "" : r.GetString(2),
            notnull = r.GetInt64(3) != 0,
            dflt_value = r.IsDBNull(4) ? null : r.GetString(4),
            pk = r.GetInt64(5) != 0
        });
    }
    return Results.Ok(columns);
});

// API: Get table data
app.MapGet("/api/databases/{dbName}/tables/{tableName}/data", async (string dbName, string tableName, int? limit, int? offset, SqliteBrowserDataService svc) =>
{
    var data = await svc.GetTableDataAsync(dbName, tableName, limit ?? 100, offset ?? 0);
    return data is null ? Results.NotFound() : Results.Ok(data);
});

// API: Execute SQL
app.MapPost("/api/databases/{dbName}/execute", async (string dbName, ExecuteRequest req, SqliteBrowserDataService svc) =>
{
    if (req is null || string.IsNullOrWhiteSpace(req.Sql))
        return Results.BadRequest(new { error = "SQL é obrigatório" });
    var (success, results, error) = await svc.ExecuteSqlAsync(dbName, req.Sql, req.Limit);
    return success ? Results.Ok(results) : Results.BadRequest(new { error });
});

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapGet("/api/debug/path", () => Results.Ok(new { dbPath = dataService.DbPath, exists = Directory.Exists(dataService.DbPath), files = Directory.Exists(dataService.DbPath) ? Directory.GetFiles(dataService.DbPath, "*.db").Select(Path.GetFileName).ToArray() : Array.Empty<string>() }));

app.Run();

record ExecuteRequest(
    [property: System.Text.Json.Serialization.JsonPropertyName("sql")] string Sql,
    [property: System.Text.Json.Serialization.JsonPropertyName("limit")] int? Limit);
