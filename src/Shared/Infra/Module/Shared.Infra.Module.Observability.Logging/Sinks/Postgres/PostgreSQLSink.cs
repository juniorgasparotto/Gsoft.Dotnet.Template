using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Npgsql;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Shared.Infra.Module.Observability.Logging.Sinks.Postgres
{
    public class PostgreSQLSink : PeriodicBatchingSink
    {
        private readonly string _connectionString;

        private readonly string _fullTableName;
        private readonly IDictionary<string, ColumnWriterBase> _columnOptions;
        private readonly IFormatProvider? _formatProvider;
        private readonly bool _useCopy;

        public const int DefaultBatchSizeLimit = 30;
        public const int DefaultQueueLimit = Int32.MaxValue;

        private bool _isTableCreated;
        private bool _isDatabaseEnsured;
        private readonly bool _needAutoCreateDatabase;


        public PostgreSQLSink(string connectionString,
            string tableName,
            TimeSpan period,
            IFormatProvider? formatProvider = null,
            IDictionary<string, ColumnWriterBase>? columnOptions = null,
            int batchSizeLimit = DefaultBatchSizeLimit,
            bool useCopy = true,
            string schemaName = "",
            bool needAutoCreateTable = false,
            bool needAutoCreateDatabase = false,
            bool respectCase = false)
#pragma warning disable CS0618 // PeriodicBatchingSink inheritance API obsoleta - PostgreSQLSink requer EmitBatch
            : base(batchSizeLimit, period)
#pragma warning restore CS0618
        {
            _connectionString = connectionString;
            _needAutoCreateDatabase = needAutoCreateDatabase;

            if (respectCase)
            {
                schemaName = QuoteIdentifier(schemaName);
                tableName = QuoteIdentifier(tableName);
            }
            _fullTableName = GetFullTableName(tableName, schemaName);

            _formatProvider = formatProvider;
            _useCopy = useCopy;

            _columnOptions = columnOptions ?? ColumnOptions.Default;
            if (respectCase)
            {
                _columnOptions = CreateQuotedColumnsDict(_columnOptions);
            }


            _isTableCreated = !needAutoCreateTable;
        }

        public PostgreSQLSink(string connectionString,
            string tableName,
            TimeSpan period,
            IFormatProvider? formatProvider = null,
            IDictionary<string, ColumnWriterBase>? columnOptions = null,
            int batchSizeLimit = DefaultBatchSizeLimit,
            int queueLimit = DefaultQueueLimit,
            bool useCopy = true,
            string schemaName = "",
            bool needAutoCreateTable = false,
            bool needAutoCreateDatabase = false,
            bool respectCase = false)
#pragma warning disable CS0618 // PeriodicBatchingSink inheritance API obsoleta - PostgreSQLSink requer EmitBatch
            : base(batchSizeLimit, period, queueLimit)
#pragma warning restore CS0618
        {
            _connectionString = connectionString;
            _needAutoCreateDatabase = needAutoCreateDatabase;

            if (respectCase)
            {
                schemaName = QuoteIdentifier(schemaName);
                tableName = QuoteIdentifier(tableName);
            }
            _fullTableName = GetFullTableName(tableName, schemaName);

            _formatProvider = formatProvider;
            _useCopy = useCopy;

            _columnOptions = columnOptions ?? ColumnOptions.Default;
            if (respectCase)
            {
                _columnOptions = CreateQuotedColumnsDict(_columnOptions);
            }


            _isTableCreated = !needAutoCreateTable;
        }

        private static string QuoteIdentifier(string identifier)
        {
            if (String.IsNullOrEmpty(identifier) || identifier.StartsWith("\""))
            {
                return identifier;
            }

            return $"\"{identifier}\"";
        }

        private IDictionary<string, ColumnWriterBase> CreateQuotedColumnsDict(IDictionary<string, ColumnWriterBase> originalColumnsDict)
        {
            var result = new Dictionary<string, ColumnWriterBase>(originalColumnsDict.Count);

            foreach (var kvp in originalColumnsDict)
            {
                result[QuoteIdentifier(kvp.Key)] = kvp.Value;
            }

            return result;
        }

        private string GetFullTableName(string tableName, string schemaName)
        {
            var schemaPrefix = String.Empty;
            if (!String.IsNullOrEmpty(schemaName))
            {
                schemaPrefix = schemaName + ".";
            }

            return schemaPrefix + tableName;
        }


        protected override void EmitBatch(IEnumerable<LogEvent> events)
        {
            if (_needAutoCreateDatabase && !_isDatabaseEnsured)
            {
                DatabaseCreator.EnsureDatabaseExists(_connectionString);
                _isDatabaseEnsured = true;
            }

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();

                if (!_isTableCreated)
                {
                    TableCreator.CreateTable(connection, _fullTableName, _columnOptions);
                    _isTableCreated = true;
                }

                if (_useCopy)
                {
                    ProcessEventsByCopyCommand(events, connection);
                }
                else
                {
                    ProcessEventsByInsertStatements(events, connection);
                }
            }
        }

        private void ProcessEventsByInsertStatements(IEnumerable<LogEvent> events, NpgsqlConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = GetInsertQuery();


                foreach (var logEvent in events)
                {
                    command.Parameters.Clear();
                    foreach (var columnOption in _columnOptions)
                    {
                        var value = columnOption.Value.GetValue(logEvent, _formatProvider);
                        var dbType = columnOption.Value.DbType;

                        // Debug logging para INSERT statements também
                        if (value is string str && str.Contains("\0"))
                        {
                            Console.WriteLine($"INSERT WARNING: Column '{columnOption.Key}' contains null byte in string: {str.Length} chars");
                            value = str.Replace("\0", "");
                        }

                        if (value == DBNull.Value)
                        {
                            command.Parameters.AddWithValue(ClearColumnNameForParameterName(columnOption.Key), DBNull.Value);
                        }
                        else
                        {
                            try
                            {
                                command.Parameters.AddWithValue(ClearColumnNameForParameterName(columnOption.Key), dbType, value);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"INSERT ERROR writing column '{columnOption.Key}' with DbType '{dbType}': {ex.Message}");
                                Console.WriteLine($"Value type: {value?.GetType()?.Name ?? "null"}");
                                Console.WriteLine($"Value: {(value is string s ? $"\"{s}\"" : value?.ToString() ?? "null")}");
                                throw;
                            }
                        }
                    }

                    command.ExecuteNonQuery();
                }
            }
        }

        private static string ClearColumnNameForParameterName(string? columnName)
        {
            return columnName?.Replace("\"", "") ?? "";
        }

        private void ProcessEventsByCopyCommand(IEnumerable<LogEvent> events, NpgsqlConnection connection)
        {
            try
            {
                using (var binaryCopyWriter = connection.BeginBinaryImport(GetCopyCommand()))
                {
                    WriteToStream(binaryCopyWriter, events);
                    binaryCopyWriter.Complete();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in ProcessEventsByCopyCommand: {ex.Message}");
                Console.WriteLine("Attempting fallback to INSERT statements...");

                // Fallback para INSERT se COPY falhar
                ProcessEventsByInsertStatements(events, connection);
            }
        }

        private string GetCopyCommand()
        {
            var columns = String.Join(", ", _columnOptions.Keys);

            return $"COPY {_fullTableName}({columns}) FROM STDIN BINARY;";

        }

        private string GetInsertQuery()
        {
            var columns = String.Join(", ", _columnOptions.Keys);

            var parameters = String.Join(", ", _columnOptions.Keys.Select(cn => ":" + ClearColumnNameForParameterName(cn)));

            return $@"INSERT INTO {_fullTableName} ({columns})
                                        VALUES ({parameters})";
        }

        private void WriteToStream(NpgsqlBinaryImporter writer, IEnumerable<LogEvent> entities)
        {
            foreach (var entity in entities)
            {
                writer.StartRow();

                foreach (var columnKey in _columnOptions.Keys)
                {
                    var value = _columnOptions[columnKey].GetValue(entity, _formatProvider);
                    var dbType = _columnOptions[columnKey].DbType;

                    // Sanitizar strings para remover bytes nulos (0x00)
                    if (value is string str && str.Contains("\0"))
                    {
                        value = str.Replace("\0", "");
                    }

                    if (value == DBNull.Value)
                    {
                        writer.WriteNull();
                    }
                    else
                    {
                        writer.Write(value, dbType);
                    }
                }
            }
        }
    }
}