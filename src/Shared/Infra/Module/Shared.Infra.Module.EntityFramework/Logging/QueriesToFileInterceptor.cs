using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace Shared.Infra.Module.EntityFramework.Logging
{
    public class QueriesToFileInterceptor : DbCommandInterceptor
    {
        #region NonQuery

        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            //Log(command);
            return base.NonQueryExecuted(command, eventData, result);
        }

        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            //Log(command);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            Log(command);
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Log(command);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        #endregion

        #region Reader

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            //Log(command);
            return base.ReaderExecuted(command, eventData, result);
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            //Log(command);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Log(command);
            return base.ReaderExecuting(command, eventData, result);
        }


        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Log(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        #endregion

        #region Scalar

        public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
        {
            //Log(command);
            return base.ScalarExecuted(command, eventData, result);
        }

        public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
        {
            //Log(command);
            return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            Log(command);
            return base.ScalarExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            Log(command);
            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }

        #endregion

        public void Log(DbCommand dbCommand)
        {
            var query = dbCommand.CommandText;
            foreach (DbParameter parameter in dbCommand.Parameters)
            {
                query = Regex.Replace(query, $@"({Regex.Escape(parameter.ParameterName)})([^\\d]|$)", f =>
                {
                    var parameterNumber = new string(parameter.ParameterName.Where(char.IsDigit).ToArray());
                    var parameterNumberQuery = new string(f.Value.Where(char.IsDigit).ToArray());
                    if (parameterNumber == parameterNumberQuery)
                    {
                        string? value = System.Convert.ToString(parameter.Value, System.Globalization.CultureInfo.InvariantCulture);
                        switch (parameter.DbType)
                        {
                            case DbType.Int32:
                            case DbType.Byte:
                            case DbType.Currency:
                            case DbType.Boolean:
                            case DbType.Int64:
                            case DbType.UInt64:
                            case DbType.Decimal:
                            case DbType.Double:
                            case DbType.Int16:
                            case DbType.Single:
                            case DbType.SByte:
                            case DbType.UInt16:
                            case DbType.UInt32:
                                value = parameter.Value == System.DBNull.Value ? "NULL" : (value ?? "");
                                break;
                            default:
                                value = parameter.Value == System.DBNull.Value ? "NULL" : $"'{(value ?? "").Replace("'", "''")}'";
                                break;
                        }

                        return value + (f.Groups.Count > 2 ? f.Groups[2].Value : "");
                    }

                    return f.Value;
                });
            }

            Directory.CreateDirectory("Queries");
            string path = Path.Combine("Queries", "queries.txt");

            using StreamWriter sw = (File.Exists(path)) ? File.AppendText(path) : File.CreateText(path);
            sw.WriteLine(query + "\r\n\r\n");
        }
    }
}
