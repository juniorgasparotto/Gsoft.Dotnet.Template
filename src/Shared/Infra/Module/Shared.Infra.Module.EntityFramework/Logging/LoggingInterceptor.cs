//using System.Collections.Concurrent;
//using System.Data;
//using System.Data.Common;
//using System.Text;
//using System.Text.Json;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Diagnostics;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Configuration;

//namespace Shared.Infra.Module.Observability.Logging.Interceptors.Database;

///// <summary>
///// Interceptor para capturar e logar todas as execuções de queries SQL no banco de dados
///// </summary>
//public class LoggingInterceptor : DbCommandInterceptor
//{
//    internal readonly ILogger<LoggingInterceptor> _logger;
//    internal readonly string _logsBasePath;
//    private readonly bool _enableConsoleLogs;
//    private readonly ConcurrentDictionary<int, (string FolderPath, string FileName, string CommandJson, string SqlRaw)> _commandData = new();

//    public LoggingInterceptor(ILogger<LoggingInterceptor> logger, IConfiguration configuration)
//    {
//        _logger = logger;
//        _logsBasePath = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
//        Directory.CreateDirectory(_logsBasePath);
//        _enableConsoleLogs = configuration.GetValue<bool>("LoggingConfig:EnableDbConsoleLogs", false);
//    }

//    public override InterceptionResult<DbDataReader> ReaderExecuting(
//        DbCommand command,
//        CommandEventData eventData,
//        InterceptionResult<DbDataReader> result)
//    {
//        // Ignorar queries relacionadas à tabela de logs para evitar recursão
//        if (ShouldIgnoreCommand(command))
//            return base.ReaderExecuting(command, eventData, result);
            
//        LogCommand(command, eventData, "SELECT");
//        return base.ReaderExecuting(command, eventData, result);
//    }

//    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
//        DbCommand command,
//        CommandEventData eventData,
//        InterceptionResult<DbDataReader> result,
//        CancellationToken cancellationToken = default)
//    {
//        // Ignorar queries relacionadas à tabela de logs para evitar recursão
//        if (ShouldIgnoreCommand(command))
//            return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
            
//        await LogCommandAsync(command, eventData, "SELECT");
//        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
//    }

//    public override InterceptionResult<int> NonQueryExecuting(
//        DbCommand command,
//        CommandEventData eventData,
//        InterceptionResult<int> result)
//    {
//        // Ignorar queries relacionadas à tabela de logs para evitar recursão
//        if (ShouldIgnoreCommand(command))
//            return base.NonQueryExecuting(command, eventData, result);
            
//        LogCommand(command, eventData, GetCommandType(command));
//        return base.NonQueryExecuting(command, eventData, result);
//    }

//    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
//        DbCommand command,
//        CommandEventData eventData,
//        InterceptionResult<int> result,
//        CancellationToken cancellationToken = default)
//    {
//        // Ignorar queries relacionadas à tabela de logs para evitar recursão
//        if (ShouldIgnoreCommand(command))
//            return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
            
//        await LogCommandAsync(command, eventData, GetCommandType(command));
//        return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
//    }

//    public override InterceptionResult<object> ScalarExecuting(
//        DbCommand command,
//        CommandEventData eventData,
//        InterceptionResult<object> result)
//    {
//        // Ignorar queries relacionadas à tabela de logs para evitar recursão
//        if (ShouldIgnoreCommand(command))
//            return base.ScalarExecuting(command, eventData, result);
            
//        LogCommand(command, eventData, GetCommandType(command));
//        return base.ScalarExecuting(command, eventData, result);
//    }

//    public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
//        DbCommand command,
//        CommandEventData eventData,
//        InterceptionResult<object> result,
//        CancellationToken cancellationToken = default)
//    {
//        // Ignorar queries relacionadas à tabela de logs para evitar recursão
//        if (ShouldIgnoreCommand(command))
//            return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
            
//        await LogCommandAsync(command, eventData, GetCommandType(command));
//        return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
//    }

//    public override DbDataReader ReaderExecuted(
//        DbCommand command,
//        CommandExecutedEventData eventData,
//        DbDataReader result)
//    {
//        // Ignorar queries relacionadas à tabela de logs para evitar recursão
//        if (ShouldIgnoreCommand(command))
//            return base.ReaderExecuted(command, eventData, result);
            
//        // Capturar dados do reader de forma assíncrona sem interferir
//        CaptureReaderDataAsync(result, command, eventData);
//        LogResult(command, eventData, result, success: true);
//        return base.ReaderExecuted(command, eventData, result);
//    }

//    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
//        DbCommand command,
//        CommandExecutedEventData eventData,
//        DbDataReader result,
//        CancellationToken cancellationToken = default)
//    {
//        // Ignorar queries relacionadas à tabela de logs para evitar recursão
//        if (ShouldIgnoreCommand(command))
//            return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
            
//        // Capturar dados do reader de forma assíncrona sem interferir
//        CaptureReaderDataAsync(result, command, eventData);
//        await LogResultAsync(command, eventData, result, success: true);
//        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
//    }
    
//    private void CaptureReaderDataAsync(DbDataReader reader, DbCommand command, CommandExecutedEventData eventData)
//    {
//        // Capturar dados em background sem interferir com o reader original
//        _ = Task.Run(async () =>
//        {
//            try
//            {
//                var traceId = TraceContext.CurrentTraceId ?? "no-trace";
//                var commandId = GetCommandId(command);
//                var commandType = GetCommandType(command);
//                var duration = eventData.Duration.TotalMilliseconds;
                
//                // Tentar recuperar dados do comando
//                string folderPath;
//                string fileName;
//                string commandJson;
//                string sqlRaw;
                
//                if (!_commandData.TryRemove(command.GetHashCode(), out var commandInfo))
//                {
//                    // Fallback se não encontrou
//                    var jobName = ExtractJobName(traceId);
//                    var dateFolder = DateTime.Now.ToString("yyyyMMdd");
//                    var timePrefix = DateTime.Now.ToString("HH-mm-ss");
//                    var queryPart = ExtractQueryPart(command.CommandText, commandType, 40);
//                    folderPath = Path.Combine(_logsBasePath, dateFolder, jobName, traceId);
//                    fileName = $"{timePrefix}-data-{queryPart}.log";
//                    commandJson = JsonSerializer.Serialize(new { Note = "Command data not available" }, new JsonSerializerOptions { WriteIndented = true });
//                    sqlRaw = ReplaceParametersInCommand(command);
//                }
//                else
//                {
//                    folderPath = commandInfo.FolderPath;
//                    fileName = commandInfo.FileName;
//                    commandJson = commandInfo.CommandJson;
//                    sqlRaw = commandInfo.SqlRaw;
//                }
                
//                // Preparar resultado (sem o campo Result, que vai no SQL RESULT)
//                var resultData = new
//                {
//                    CommandId = commandId,
//                    TraceId = traceId,
//                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff UTC"),
//                    Success = true,
//                    DurationMs = duration,
//                    ResultType = "DataReader",
//                    RowsAffected = (int?)null
//                };
//                var resultJson = JsonSerializer.Serialize(resultData, new JsonSerializerOptions { WriteIndented = true });
                
//                // SQL RESULT vazio (não podemos ler o DataReader aqui)
//                var sqlResultJson = "[]"; // Array vazio, pois não podemos ler o reader
                
//                // Agrupar tudo em um único arquivo
//                await SaveGroupedLogFile(folderPath, fileName, sqlRaw, sqlResultJson, commandJson, resultJson);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogWarning(ex, "Could not capture DataReader results");
//            }
//        });
//    }

//    public override int NonQueryExecuted(
//        DbCommand command,
//        CommandExecutedEventData eventData,
//        int result)
//    {
//        // Ignorar queries relacionadas à tabela de logs para evitar recursão
//        if (ShouldIgnoreCommand(command))
//            return base.NonQueryExecuted(command, eventData, result);
            
//        LogResult(command, eventData, result, success: true);
//        return base.NonQueryExecuted(command, eventData, result);
//    }

//    public override async ValueTask<int> NonQueryExecutedAsync(
//        DbCommand command,
//        CommandExecutedEventData eventData,
//        int result,
//        CancellationToken cancellationToken = default)
//    {
//        // Ignorar queries relacionadas à tabela de logs para evitar recursão
//        if (ShouldIgnoreCommand(command))
//            return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
            
//        await LogResultAsync(command, eventData, result, success: true);
//        return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
//    }

//    public override object? ScalarExecuted(
//        DbCommand command,
//        CommandExecutedEventData eventData,
//        object? result)
//    {
//        // Ignorar queries relacionadas à tabela de logs para evitar recursão
//        if (ShouldIgnoreCommand(command))
//            return base.ScalarExecuted(command, eventData, result);
            
//        LogResult(command, eventData, result, success: true);
//        return base.ScalarExecuted(command, eventData, result);
//    }

//    public override async ValueTask<object?> ScalarExecutedAsync(
//        DbCommand command,
//        CommandExecutedEventData eventData,
//        object? result,
//        CancellationToken cancellationToken = default)
//    {
//        // Ignorar queries relacionadas à tabela de logs para evitar recursão
//        if (ShouldIgnoreCommand(command))
//            return await base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
            
//        await LogResultAsync(command, eventData, result, success: true);
//        return await base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
//    }

//    /// <summary>
//    /// Cria um snapshot imediato dos parâmetros para evitar "Collection was modified"
//    /// </summary>
//    private List<DbParameter> GetParametersSnapshot(DbCommand command)
//    {
//        var snapshot = new List<DbParameter>(command.Parameters.Count);
//        foreach (DbParameter param in command.Parameters)
//        {
//            snapshot.Add(param);
//        }
//        return snapshot;
//    }

//    /// <summary>
//    /// Verifica se o comando deve ser ignorado para evitar recursão (queries relacionadas à tabela de logs)
//    /// </summary>
//    private bool ShouldIgnoreCommand(DbCommand command)
//    {
//        if (command == null || string.IsNullOrWhiteSpace(command.CommandText))
//            return false;
            
//        var commandText = command.CommandText.Trim().ToUpperInvariant();
        
//        // Ignorar queries que envolvem a tabela 'logs' (tabela de logs do Serilog)
//        // Isso evita recursão infinita quando o Serilog tenta salvar logs no banco
//        if (commandText.Contains("\"logs\"", StringComparison.OrdinalIgnoreCase) ||
//            commandText.Contains("'logs'", StringComparison.OrdinalIgnoreCase) ||
//            commandText.Contains(" logs ", StringComparison.OrdinalIgnoreCase) ||
//            commandText.Contains("FROM logs", StringComparison.OrdinalIgnoreCase) ||
//            commandText.Contains("INTO logs", StringComparison.OrdinalIgnoreCase) ||
//            commandText.Contains("UPDATE logs", StringComparison.OrdinalIgnoreCase) ||
//            commandText.Contains("DELETE FROM logs", StringComparison.OrdinalIgnoreCase) ||
//            commandText.Contains("CREATE TABLE logs", StringComparison.OrdinalIgnoreCase) ||
//            commandText.Contains("ALTER TABLE logs", StringComparison.OrdinalIgnoreCase) ||
//            commandText.Contains("DROP TABLE logs", StringComparison.OrdinalIgnoreCase))
//        {
//            return true;
//        }
        
//        return false;
//    }

//    internal string GetCommandType(DbCommand command)
//    {
//        var commandText = command.CommandText.TrimStart().ToUpper();
//        if (commandText.StartsWith("SELECT")) return "SELECT";
//        if (commandText.StartsWith("INSERT")) return "INSERT";
//        if (commandText.StartsWith("UPDATE")) return "UPDATE";
//        if (commandText.StartsWith("DELETE")) return "DELETE";
//        if (commandText.StartsWith("CREATE")) return "CREATE";
//        if (commandText.StartsWith("ALTER")) return "ALTER";
//        if (commandText.StartsWith("DROP")) return "DROP";
//        return "OTHER";
//    }

//    internal string ExtractJobName(string traceId)
//    {
//        // Trace ID format: JOB-{JOBNAME}-{timestamp}-{random}
//        // Exemplo: JOB-GET-BETS-20251129145123-520001
//        if (string.IsNullOrEmpty(traceId) || !traceId.StartsWith("JOB-"))
//            return "no-trace";
        
//        var parts = traceId.Split('-');
//        if (parts.Length >= 2)
//        {
//            // Pegar tudo entre JOB- e o timestamp (que começa com número)
//            var jobParts = new List<string>();
//            for (int i = 1; i < parts.Length; i++)
//            {
//                // Se começar com número, é o timestamp, parar
//                if (parts[i].Length > 0 && char.IsDigit(parts[i][0]))
//                    break;
//                jobParts.Add(parts[i]);
//            }
//            return string.Join("-", jobParts).ToUpper();
//        }
        
//        return "unknown";
//    }

//    internal string GetCommandId(DbCommand command)
//    {
//        // Criar ID único baseado no hash do comando e timestamp para garantir consistência
//        var commandHash = command.GetHashCode();
//        var timestamp = DateTime.UtcNow.Ticks;
//        return $"{Math.Abs(commandHash)}_{timestamp % 1000000:X}";
//    }

//    private string ReplaceParametersInCommand(DbCommand command)
//    {
//        try
//        {
//            var sql = command.CommandText;
            
//            // Criar uma cópia imediata dos parâmetros para evitar "Collection was modified"
//            // Isso é necessário porque a coleção pode ser modificada durante a execução
//            if (command.Parameters == null || command.Parameters.Count == 0)
//                return sql;
                
//            var parametersArray = new DbParameter[command.Parameters.Count];
//            command.Parameters.CopyTo(parametersArray, 0);
            
//            // Filtrar parâmetros nulos e com nome nulo/vazio
//            // Ordenar parâmetros por nome em ordem reversa para evitar substituições parciais
//            // Ex: @p10 antes de @p1
//            var parameters = parametersArray
//                .Where(p => p != null && !string.IsNullOrWhiteSpace(p.ParameterName))
//                .OrderByDescending(p => p.ParameterName!.Length)
//                .ThenByDescending(p => p.ParameterName)
//                .ToList();
            
//            if (parameters.Count == 0)
//                return sql;
            
//            foreach (var param in parameters)
//            {
//                var paramName = param.ParameterName;
//                var paramValue = param.Value;
                
//                // Pular se o nome do parâmetro for nulo ou vazio
//                if (string.IsNullOrWhiteSpace(paramName))
//                    continue;
                
//                string replacement;
                
//                if (paramValue == null || paramValue == DBNull.Value)
//                {
//                    replacement = "NULL";
//                }
//                else
//                {
//                    var dbType = param.DbType;
                    
//                    // Formatar baseado no tipo
//                    if (dbType == System.Data.DbType.String || 
//                        dbType == System.Data.DbType.AnsiString ||
//                        dbType == System.Data.DbType.StringFixedLength ||
//                        dbType == System.Data.DbType.AnsiStringFixedLength ||
//                        dbType == System.Data.DbType.Xml ||
//                        dbType == System.Data.DbType.Guid)
//                    {
//                        // Strings: escapar aspas e adicionar aspas simples
//                        var strValue = paramValue.ToString() ?? "";
//                        strValue = strValue.Replace("'", "''"); // Escapar aspas simples
//                        replacement = $"'{strValue}'";
//                    }
//                    else if (dbType == System.Data.DbType.DateTime || 
//                             dbType == System.Data.DbType.DateTime2 ||
//                             dbType == System.Data.DbType.Date)
//                    {
//                        // Datas: formato PostgreSQL
//                        if (paramValue is DateTime dt)
//                        {
//                            replacement = $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'";
//                        }
//                        else
//                        {
//                            var strValue = paramValue.ToString() ?? "";
//                            strValue = strValue.Replace("'", "''");
//                            replacement = $"'{strValue}'";
//                        }
//                    }
//                    else if (dbType == System.Data.DbType.Time)
//                    {
//                        // Time: formato PostgreSQL
//                        var strValue = paramValue.ToString() ?? "";
//                        strValue = strValue.Replace("'", "''");
//                        replacement = $"'{strValue}'";
//                    }
//                    else if (dbType == System.Data.DbType.Boolean)
//                    {
//                        // Boolean: true/false
//                        replacement = paramValue.ToString()?.ToLower() ?? "false";
//                    }
//                    else if (dbType == System.Data.DbType.Decimal ||
//                             dbType == System.Data.DbType.Currency ||
//                             dbType == System.Data.DbType.Double ||
//                             dbType == System.Data.DbType.Single)
//                    {
//                        // Números decimais: sem aspas
//                        replacement = paramValue.ToString() ?? "0";
//                    }
//                    else if (dbType == System.Data.DbType.Int16 ||
//                             dbType == System.Data.DbType.Int32 ||
//                             dbType == System.Data.DbType.Int64 ||
//                             dbType == System.Data.DbType.UInt16 ||
//                             dbType == System.Data.DbType.UInt32 ||
//                             dbType == System.Data.DbType.UInt64 ||
//                             dbType == System.Data.DbType.Byte ||
//                             dbType == System.Data.DbType.SByte)
//                    {
//                        // Números inteiros: sem aspas
//                        replacement = paramValue.ToString() ?? "0";
//                    }
//                    else if (dbType == System.Data.DbType.Binary ||
//                             dbType == System.Data.DbType.Object)
//                    {
//                        // Binary/Object: converter para hex ou string
//                        if (paramValue is byte[] bytes)
//                        {
//                            replacement = $"E'\\\\x{BitConverter.ToString(bytes).Replace("-", "")}'";
//                        }
//                        else
//                        {
//                            var strValue = paramValue.ToString() ?? "";
//                            strValue = strValue.Replace("'", "''");
//                            replacement = $"'{strValue}'";
//                        }
//                    }
//                    else
//                    {
//                        // Default: tratar como string
//                        var strValue = paramValue.ToString() ?? "";
//                        strValue = strValue.Replace("'", "''");
//                        replacement = $"'{strValue}'";
//                    }
//                }
                
//                // Substituir o parâmetro no SQL
//                sql = sql.Replace(paramName, replacement);
//            }
            
//            return sql;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogWarning(ex, "Error replacing parameters in command");
//            return command.CommandText + " /* Error replacing parameters */";
//        }
//    }

//    internal string ExtractQueryPart(string queryText, string commandType, int maxLength = 40)
//    {
//        if (string.IsNullOrWhiteSpace(queryText))
//            return "unknown";
        
//        // Remover quebras de linha e espaços extras
//        var cleaned = queryText.Replace("\n", " ").Replace("\r", " ").Trim();
        
//        string result;
        
//        // Extrair parte relevante baseado no tipo de comando
//        if (commandType == "SELECT")
//        {
//            // SELECT * FROM tabela WHERE ...
//            // Pegar FROM tabela
//            var fromIndex = cleaned.IndexOf(" FROM ", StringComparison.OrdinalIgnoreCase);
//            if (fromIndex >= 0)
//            {
//                var afterFrom = cleaned.Substring(fromIndex + 6).TrimStart();
//                var nextSpace = afterFrom.IndexOf(' ');
//                var tableName = nextSpace > 0 ? afterFrom.Substring(0, nextSpace) : afterFrom;
//                result = $"SELECT-{tableName}";
//            }
//            else
//            {
//                result = "SELECT";
//            }
//        }
//        else if (commandType == "INSERT")
//        {
//            // INSERT INTO tabela (...)
//            // Pegar INTO tabela
//            var intoIndex = cleaned.IndexOf(" INTO ", StringComparison.OrdinalIgnoreCase);
//            if (intoIndex >= 0)
//            {
//                var afterInto = cleaned.Substring(intoIndex + 6).TrimStart();
//                var nextSpace = afterInto.IndexOf(' ');
//                var tableName = nextSpace > 0 ? afterInto.Substring(0, nextSpace) : afterInto;
//                // Remover parênteses se houver
//                tableName = tableName.Split('(')[0].Trim();
//                result = $"INSERT-{tableName}";
//            }
//            else
//            {
//                result = "INSERT";
//            }
//        }
//        else if (commandType == "UPDATE")
//        {
//            // UPDATE tabela SET ...
//            var updateIndex = cleaned.IndexOf("UPDATE ", StringComparison.OrdinalIgnoreCase);
//            if (updateIndex >= 0)
//            {
//                var afterUpdate = cleaned.Substring(updateIndex + 7).TrimStart();
//                var nextSpace = afterUpdate.IndexOf(' ');
//                var tableName = nextSpace > 0 ? afterUpdate.Substring(0, nextSpace) : afterUpdate;
//                result = $"UPDATE-{tableName}";
//            }
//            else
//            {
//                result = "UPDATE";
//            }
//        }
//        else if (commandType == "DELETE")
//        {
//            // DELETE FROM tabela WHERE ...
//            var fromIndex = cleaned.IndexOf(" FROM ", StringComparison.OrdinalIgnoreCase);
//            if (fromIndex >= 0)
//            {
//                var afterFrom = cleaned.Substring(fromIndex + 6).TrimStart();
//                var nextSpace = afterFrom.IndexOf(' ');
//                var tableName = nextSpace > 0 ? afterFrom.Substring(0, nextSpace) : afterFrom;
//                result = $"DELETE-{tableName}";
//            }
//            else
//            {
//                result = "DELETE";
//            }
//        }
//        else
//        {
//            // Para outros tipos, pegar primeiras palavras
//            var words = cleaned.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
//            if (words.Length > 0)
//            {
//                result = string.Join("-", words.Take(3));
//            }
//            else
//            {
//                result = commandType;
//            }
//        }
        
//        // Sanitizar para nome de arquivo
//        var invalidChars = Path.GetInvalidFileNameChars();
//        var sanitized = string.Join("-", result.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
//        sanitized = sanitized.Replace(" ", "-")
//                            .Replace("=", "-")
//                            .Replace("(", "")
//                            .Replace(")", "")
//                            .Replace(",", "")
//                            .Replace("'", "")
//                            .Replace("\"", "")
//                            .Trim('-');
        
//        // Limitar tamanho final
//        if (sanitized.Length > maxLength)
//        {
//            sanitized = sanitized.Substring(0, maxLength);
//            // Tentar cortar em um hífen
//            var lastDash = sanitized.LastIndexOf('-');
//            if (lastDash > maxLength / 2)
//            {
//                sanitized = sanitized.Substring(0, lastDash);
//            }
//        }
        
//        return string.IsNullOrWhiteSpace(sanitized) ? "query" : sanitized;
//    }

//    private void LogCommand(
//        DbCommand command,
//        CommandEventData eventData,
//        string commandType)
//    {
//        var commandId = GetCommandId(command);
//        var traceId = TraceContext.CurrentTraceId;
//        var jobName = ExtractJobName(traceId ?? "unknown");
        
//        // Preparar parâmetros SQL
//        var parameters = GetParametersSnapshot(command)
//            .Select(p => new
//            {
//                Name = p.ParameterName,
//                Value = p.Value?.ToString(),
//                DbType = p.DbType.ToString(),
//                Size = p.Size
//            })
//            .ToList();
        
//        var sqlWithParams = ReplaceParametersInCommand(command);
        
//        // Serializar parâmetros para JSON antes de passar ao LogContext
//        var parametersJson = JsonSerializer.Serialize(parameters);
        
//        // Adicionar propriedades específicas SQL ao log context
//        using (Serilog.Context.LogContext.PushProperty("LogType", "SQL"))
//        using (Serilog.Context.LogContext.PushProperty("TraceId", traceId))
//        using (Serilog.Context.LogContext.PushProperty("SqlCommandType", commandType))
//        using (Serilog.Context.LogContext.PushProperty("SqlQuery", command.CommandText))
//        using (Serilog.Context.LogContext.PushProperty("SqlQueryWithParams", sqlWithParams))
//        using (Serilog.Context.LogContext.PushProperty("SqlParameters", parametersJson))
//        using (Serilog.Context.LogContext.PushProperty("SqlCommandTimeout", command.CommandTimeout))
//        using (Serilog.Context.LogContext.PushProperty("SqlDirection", "Request"))
//        {
//            // Sem log manual - o framework vai capturar automaticamente
//        }
        
//        // Criar estrutura de pastas: Logs/YYYYMMDD/[NOME-JOB]/[trace-id]/
//        var dateFolder = DateTime.Now.ToString("yyyyMMdd");
//        var timePrefix = DateTime.Now.ToString("HH-mm-ss");
//        var queryPart = ExtractQueryPart(command.CommandText, commandType, 40);
        
//        var folderPath = Path.Combine(_logsBasePath, dateFolder, jobName, traceId);
//        Directory.CreateDirectory(folderPath);
        
//        var commandFileName = $"{timePrefix}-data-{queryPart}.log";
        
//        // Preparar dados do comando para salvar depois junto com o resultado
//        var commandReplaced = ReplaceParametersInCommand(command);
//        var commandData = new
//        {
//            CommandId = commandId,
//            TraceId = TraceContext.CurrentTraceId,
//            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff UTC"),
//            CommandType = commandType,
//            CommandText = command.CommandText,
//            Parameters = GetParametersSnapshot(command)
//                .Select(p => new
//                {
//                    Name = p.ParameterName,
//                    Value = p.Value?.ToString(),
//                    DbType = p.DbType.ToString(),
//                    Size = p.Size
//                })
//                .ToList(),
//            CommandTimeout = command.CommandTimeout
//        };
//        var commandJson = JsonSerializer.Serialize(commandData, new JsonSerializerOptions { WriteIndented = true });
        
//        // Armazenar dados do comando usando o hash do comando como chave
//        _commandData.TryAdd(command.GetHashCode(), (folderPath, commandFileName, commandJson, commandReplaced));
        
//        // Console log simplificado (se habilitado)
//        if (_enableConsoleLogs)
//        {
//            var queryPreview = command.CommandText.Length > 100 
//                ? command.CommandText.Substring(0, 100) + "..." 
//                : command.CommandText;
//            var logPath = Path.Combine(folderPath, commandFileName).Replace(Directory.GetCurrentDirectory(), "").TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
//            Console.WriteLine($"[DATA LOG]:[{traceId}]:REQUEST: [{commandType}-{queryPreview}]: {logPath}");
//        }
//    }

//    private async Task LogCommandAsync(
//        DbCommand command,
//        CommandEventData eventData,
//        string commandType)
//    {
//        await Task.Run(() => LogCommand(command, eventData, commandType));
//    }

//    private void LogResult(
//        DbCommand command,
//        CommandExecutedEventData eventData,
//        object? result,
//        bool success)
//    {
//        var commandId = GetCommandId(command);
//        var commandType = GetCommandType(command);
//        var traceId = TraceContext.CurrentTraceId;
//        var jobName = ExtractJobName(traceId ?? "unknown");
        
//        // Tentar recuperar dados do comando
//        string folderPath;
//        string fileName;
//        string commandJson;
//        string sqlRaw;
        
//        if (!_commandData.TryRemove(command.GetHashCode(), out var commandInfo))
//        {
//            // Fallback se não encontrou
//            var dateFolder = DateTime.Now.ToString("yyyyMMdd");
//            var timePrefix = DateTime.Now.ToString("HH-mm-ss");
//            var queryPart = ExtractQueryPart(command.CommandText, commandType, 40);
//            folderPath = Path.Combine(_logsBasePath, dateFolder, jobName, traceId);
//            Directory.CreateDirectory(folderPath);
//            fileName = $"{timePrefix}-data-{queryPart}.log";
//            commandJson = JsonSerializer.Serialize(new { Note = "Command data not available" }, new JsonSerializerOptions { WriteIndented = true });
//            sqlRaw = ReplaceParametersInCommand(command);
//        }
//        else
//        {
//            folderPath = commandInfo.FolderPath;
//            fileName = commandInfo.FileName;
//            commandJson = commandInfo.CommandJson;
//            sqlRaw = commandInfo.SqlRaw;
//        }
        
//        var duration = eventData.Duration.TotalMilliseconds;
        
//        // Preparar parâmetros SQL
//        var parameters = GetParametersSnapshot(command)
//            .Select(p => new
//            {
//                Name = p.ParameterName,
//                Value = p.Value?.ToString(),
//                DbType = p.DbType.ToString(),
//                Size = p.Size
//            })
//            .ToList();
        
//        var sqlWithParams = ReplaceParametersInCommand(command);
        
//        // Determinar resultado
//        int? rowsAffected = null;
//        if (result is int intResult)
//        {
//            rowsAffected = intResult;
//        }
//        else if (result is DbDataReader reader)
//        {
//            // Para DataReader, não podemos contar linhas aqui sem consumir o reader
//            rowsAffected = null;
//        }
        
//        // Serializar parâmetros para JSON antes de passar ao LogContext
//        var parametersJson = JsonSerializer.Serialize(parameters);
        
//        // Adicionar propriedades específicas SQL Response ao log context
//        // O Serilog vai capturar automaticamente através do LogContext quando o framework fizer log
//        using (Serilog.Context.LogContext.PushProperty("LogType", "SQL"))
//        using (Serilog.Context.LogContext.PushProperty("TraceId", traceId))
//        using (Serilog.Context.LogContext.PushProperty("SqlCommandType", commandType))
//        using (Serilog.Context.LogContext.PushProperty("SqlQuery", command.CommandText))
//        using (Serilog.Context.LogContext.PushProperty("SqlQueryWithParams", sqlWithParams))
//        using (Serilog.Context.LogContext.PushProperty("SqlParameters", parametersJson))
//        using (Serilog.Context.LogContext.PushProperty("SqlDuration", duration))
//        using (Serilog.Context.LogContext.PushProperty("SqlRowsAffected", rowsAffected))
//        using (Serilog.Context.LogContext.PushProperty("SqlSuccess", success))
//        using (Serilog.Context.LogContext.PushProperty("SqlDirection", "Response"))
//        {
//            // Sem log manual - o framework vai capturar automaticamente
//        }
        
//        // Console log simplificado (se habilitado)
//        if (_enableConsoleLogs)
//        {
//            var queryPreview = command.CommandText.Length > 100 
//                ? command.CommandText.Substring(0, 100) + "..." 
//                : command.CommandText;
//            var logPath = Path.Combine(folderPath, fileName).Replace(Directory.GetCurrentDirectory(), "").TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
//            Console.WriteLine($"[DATA LOG]:[{traceId}]:RESPONSE: [{commandType}-{queryPreview}]: {logPath}");
//        }
        
//        // Salvar resultado em arquivo agrupado (apenas para NonQuery e Scalar)
//        // Para SELECT queries (DbDataReader), o arquivo é escrito por CaptureReaderDataAsync
//        var resultToProcess = result; // Capturar para closure
        
//        // Pular escrita de arquivo para SELECT queries - CaptureReaderDataAsync já cuida disso
//        if (resultToProcess is DbDataReader)
//        {
//            return; // Não escrever arquivo aqui, CaptureReaderDataAsync já escreve
//        }
        
//        _ = Task.Run(async () =>
//        {
//            try
//            {
//                int? rowsAffected = null;
//                string? resultType = null;
//                object? resultData = null;
                
//                if (resultToProcess is int intResult)
//                {
//                    resultType = "NonQuery";
//                    rowsAffected = intResult;
//                    resultData = intResult;
//                }
//                else if (resultToProcess != null)
//                {
//                    resultType = "Scalar";
//                    var scalarValue = resultToProcess;
                    
//                    // Converter tipos especiais
//                    if (scalarValue is DateTime dt)
//                    {
//                        scalarValue = dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
//                    }
//                    else if (scalarValue is byte[] bytes)
//                    {
//                        scalarValue = Convert.ToBase64String(bytes);
//                    }
                    
//                    resultData = scalarValue;
//                }
                
//                // Resultado sem o campo Result (que vai no SQL RESULT para SELECT)
//                var finalResultData = new
//                {
//                    CommandId = commandId,
//                    TraceId = TraceContext.CurrentTraceId,
//                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff UTC"),
//                    Success = success,
//                    DurationMs = duration,
//                    ResultType = resultType,
//                    RowsAffected = rowsAffected
//                };
//                var resultJson = JsonSerializer.Serialize(finalResultData, new JsonSerializerOptions { WriteIndented = true });
                
//                // SQL RESULT vazio para NonQuery/Scalar (apenas SELECT tem resultados)
//                var sqlResultJson = resultData != null ? JsonSerializer.Serialize(new[] { resultData }, new JsonSerializerOptions { WriteIndented = false }) : "[]";
                
//                // Agrupar tudo em um único arquivo
//                await SaveGroupedLogFile(folderPath, fileName, sqlRaw, sqlResultJson, commandJson, resultJson);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogWarning(ex, "Error saving database result to file (non-critical)");
//            }
//        });
//    }

//    private async Task LogResultAsync(
//        DbCommand command,
//        CommandExecutedEventData eventData,
//        object? result,
//        bool success)
//    {
//        await Task.Run(() => LogResult(command, eventData, result, success));
//    }
    
//    private async Task SaveGroupedLogFile(string folderPath, string fileName, string sqlRaw, string sqlResultJson, string commandJson, string resultJson)
//    {
//        var filePath = Path.Combine(folderPath, fileName);
        
//        var content = new StringBuilder();
//        content.AppendLine("============= SQL ==========");
//        content.AppendLine(sqlRaw);
//        content.AppendLine();
//        content.AppendLine("=============== SQL RESULT");
//        // SQL RESULT: cada registro JSON em uma linha única, sem quebrar linha
//        // Se for um array JSON, cada elemento vai em uma linha
//        try
//        {
//            using var doc = JsonDocument.Parse(sqlResultJson);
//            if (doc.RootElement.ValueKind == JsonValueKind.Array)
//            {
//                foreach (var element in doc.RootElement.EnumerateArray())
//                {
//                    content.AppendLine(element.GetRawText());
//                }
//            }
//            else
//            {
//                content.AppendLine(sqlResultJson);
//            }
//        }
//        catch
//        {
//            // Se não for JSON válido, apenas adicionar como está
//            content.AppendLine(sqlResultJson);
//        }
//        content.AppendLine();
//        content.AppendLine("=============== COMMAND ORIGINAL");
//        content.AppendLine(commandJson);
//        content.AppendLine();
//        content.AppendLine("=============== COMMAND RESULT");
//        content.AppendLine(resultJson);
        
//        await WriteFileWithRetryAsync(filePath, content.ToString(), maxRetries: 5);
//    }
    
//    private async Task WriteFileWithRetryAsync(string filePath, string content, int maxRetries = 3)
//    {
//        for (int attempt = 1; attempt <= maxRetries; attempt++)
//        {
//            try
//            {
//                // Criar diretório se não existir
//                var directory = Path.GetDirectoryName(filePath);
//                if (!string.IsNullOrEmpty(directory))
//                {
//                    Directory.CreateDirectory(directory);
//                }
                
//                // Tentar escrever o arquivo
//                await File.WriteAllTextAsync(filePath, content);
//                return; // Sucesso
//            }
//            catch (IOException) when (attempt < maxRetries)
//            {
//                // Arquivo em uso - criar nome único com GUID (5 primeiros caracteres) antes da extensão
//                var directory = Path.GetDirectoryName(filePath);
//                var fileName = Path.GetFileNameWithoutExtension(filePath);
//                var extension = Path.GetExtension(filePath);
//                var guidPart = Guid.NewGuid().ToString("N").Substring(0, 5);
//                var uniqueFileName = $"{fileName}-{guidPart}{extension}";
//                filePath = Path.Combine(directory ?? "", uniqueFileName);
//                // Tentar novamente com o novo nome
//                var delay = TimeSpan.FromMilliseconds(50 * attempt);
//                await Task.Delay(delay);
//            }
//            catch (UnauthorizedAccessException) when (attempt < maxRetries)
//            {
//                // Sem permissão - criar nome único e tentar novamente
//                var directory = Path.GetDirectoryName(filePath);
//                var fileName = Path.GetFileNameWithoutExtension(filePath);
//                var extension = Path.GetExtension(filePath);
//                var guidPart = Guid.NewGuid().ToString("N").Substring(0, 5);
//                var uniqueFileName = $"{fileName}-{guidPart}{extension}";
//                filePath = Path.Combine(directory ?? "", uniqueFileName);
//                var delay = TimeSpan.FromMilliseconds(50 * attempt);
//                await Task.Delay(delay);
//            }
//        }
        
//        // Última tentativa com nome único garantido
//        try
//        {
//            var directory = Path.GetDirectoryName(filePath);
//            var fileName = Path.GetFileNameWithoutExtension(filePath);
//            var extension = Path.GetExtension(filePath);
//            var guidPart = Guid.NewGuid().ToString("N").Substring(0, 5);
//            var uniqueFileName = $"{fileName}-{guidPart}{extension}";
//            var uniquePath = Path.Combine(directory ?? "", uniqueFileName);
//            await File.WriteAllTextAsync(uniquePath, content);
//        }
//        catch (Exception ex)
//        {
//            // Se ainda falhar, apenas logar (não crítico)
//            _logger.LogWarning(ex, "Could not save file after all retries: {FilePath}", filePath);
//        }
//    }
//}

