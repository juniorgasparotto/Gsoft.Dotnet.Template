using Microsoft.Data.Sqlite;
using SqliteBrowser.Core.Models;

namespace SqliteBrowser.Infra.Services;

public class SqliteBrowserDataService
{
    private readonly string _dbPath;

    public SqliteBrowserDataService(string dbPath)
    {
        _dbPath = dbPath;
    }

    public string DbPath => _dbPath;

    public Task<List<DatabaseInfo>> GetDatabasesAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(_dbPath))
            return Task.FromResult(new List<DatabaseInfo>());

        var files = Directory.GetFiles(_dbPath, "*.db")
            .Select(f => new DatabaseInfo(Path.GetFileName(f), f, new FileInfo(f).Length))
            .ToList();
        return Task.FromResult(files);
    }

    public async Task<SchemaInfo?> GetSchemaAsync(string dbName, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_dbPath, dbName);
        if (!File.Exists(fullPath))
            return null;

        await using var conn = new SqliteConnection($"Data Source={fullPath}");
        await conn.OpenAsync(ct);

        var tables = new List<TableInfo>();
        var indexes = new List<IndexInfo>();
        var triggers = new List<TriggerInfo>();

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT name, type FROM sqlite_master WHERE type IN ('table', 'view') AND name NOT LIKE 'sqlite_%' ORDER BY type, name";
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                tables.Add(new TableInfo(r.GetString(0), r.GetString(1)));
        }

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT name, tbl_name FROM sqlite_master WHERE type = 'index' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                indexes.Add(new IndexInfo(r.GetString(0), r.GetString(1)));
        }

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT name, tbl_name FROM sqlite_master WHERE type = 'trigger' ORDER BY name";
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                triggers.Add(new TriggerInfo(r.GetString(0), r.GetString(1)));
        }

        return new SchemaInfo(tables, indexes, triggers);
    }

    public async Task<TableDataResponse?> GetTableDataAsync(string dbName, string tableName, int limit = 100, int offset = 0, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_dbPath, dbName);
        if (!File.Exists(fullPath))
            return null;

        var useLimit = limit > 0;
        var take = useLimit ? Math.Clamp(limit, 1, 10000) : 0;
        var skip = Math.Max(offset, 0);
        var safeTable = $"\"{tableName.Replace("\"", "\"\"")}\"";

        await using var conn = new SqliteConnection($"Data Source={fullPath}");
        await conn.OpenAsync(ct);

        long totalRows = 0;
        await using (var countCmd = conn.CreateCommand())
        {
            countCmd.CommandText = $"SELECT COUNT(*) FROM {safeTable}";
            totalRows = (long)(await countCmd.ExecuteScalarAsync(ct) ?? 0L);
        }

        var isTable = false;
        await using (var typeCmd = conn.CreateCommand())
        {
            typeCmd.CommandText = "SELECT type FROM sqlite_master WHERE name = $name";
            typeCmd.Parameters.AddWithValue("$name", tableName);
            var typeObj = await typeCmd.ExecuteScalarAsync(ct);
            isTable = string.Equals(typeObj?.ToString(), "table", StringComparison.OrdinalIgnoreCase);
        }

        var columns = new List<string>();
        if (isTable) columns.Add("rowid");
        await using (var colCmd = conn.CreateCommand())
        {
            colCmd.CommandText = $"PRAGMA table_info({safeTable})";
            await using var cr = await colCmd.ExecuteReaderAsync(ct);
            while (await cr.ReadAsync(ct))
                columns.Add(cr.GetString(1));
        }

        var selectCols = string.Join(", ", columns.Select(c => c.Equals("rowid", StringComparison.OrdinalIgnoreCase) ? "rowid" : $"\"{c}\""));
        var limitClause = useLimit ? $" LIMIT {take} OFFSET {skip}" : "";

        var rows = new List<object?[]>();
        await using (var dataCmd = conn.CreateCommand())
        {
            dataCmd.CommandText = $"SELECT {selectCols} FROM {safeTable}{limitClause}";
            await using var dr = await dataCmd.ExecuteReaderAsync(ct);
            while (await dr.ReadAsync(ct))
            {
                var row = new object?[dr.FieldCount];
                for (var i = 0; i < dr.FieldCount; i++)
                    row[i] = dr.IsDBNull(i) ? null : dr.GetValue(i);
                rows.Add(row);
            }
        }

        return new TableDataResponse(columns, rows, totalRows);
    }

    public async Task<(bool Success, List<ExecuteResult>? Results, string? Error)> ExecuteSqlAsync(string dbName, string sql, int? limit = null, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_dbPath, dbName);
        if (!File.Exists(fullPath))
            return (false, null, "Arquivo não encontrado");

        if (string.IsNullOrWhiteSpace(sql))
            return (false, null, "SQL é obrigatório");

        await using var conn = new SqliteConnection($"Data Source={fullPath}");
        await conn.OpenAsync(ct);

        var results = new List<ExecuteResult>();
        var statements = sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var effectiveLimit = limit is > 0 ? Math.Clamp(limit.Value, 1, 10000) : 0;

        foreach (var stmt in statements.Where(s => !string.IsNullOrWhiteSpace(s)))
        {
            var trimmed = stmt.TrimStart();
            var isSelectOrPragma = trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                                  trimmed.StartsWith("PRAGMA", StringComparison.OrdinalIgnoreCase);
            var hasLimit = System.Text.RegularExpressions.Regex.IsMatch(stmt, @"\bLIMIT\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var cmdText = stmt;
            if (effectiveLimit > 0 && isSelectOrPragma && !hasLimit)
                cmdText = stmt.TrimEnd() + $" LIMIT {effectiveLimit}";

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = cmdText;

            try
            {
                if (trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("PRAGMA", StringComparison.OrdinalIgnoreCase))
                {
                    await using var r = await cmd.ExecuteReaderAsync(ct);
                    var columns = new List<string>();
                    for (var i = 0; i < r.FieldCount; i++)
                        columns.Add(r.GetName(i));

                    var rows = new List<object?[]>();
                    while (await r.ReadAsync(ct))
                    {
                        var row = new object?[r.FieldCount];
                        for (var i = 0; i < r.FieldCount; i++)
                            row[i] = r.IsDBNull(i) ? null : r.GetValue(i);
                        rows.Add(row);
                    }
                    results.Add(new ExecuteResult { Columns = columns, Rows = rows });
                }
                else
                {
                    var affected = await cmd.ExecuteNonQueryAsync(ct);
                    results.Add(new ExecuteResult { RowsAffected = affected });
                }
            }
            catch (SqliteException ex)
            {
                return (false, null, ex.Message);
            }
        }

        return (true, results, null);
    }
}
