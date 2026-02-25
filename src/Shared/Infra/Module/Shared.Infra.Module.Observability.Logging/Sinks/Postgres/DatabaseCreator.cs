using System.Linq;
using Npgsql;

namespace Shared.Infra.Module.Observability.Logging.Sinks.Postgres
{
    /// <summary>
    /// Creates PostgreSQL database if it does not exist.
    /// </summary>
    public static class DatabaseCreator
    {
        /// <summary>
        /// Ensures the database specified in the connection string exists.
        /// Connects to the default "postgres" database to create the target database if needed.
        /// </summary>
        public static void EnsureDatabaseExists(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var databaseName = builder.Database;

            if (string.IsNullOrEmpty(databaseName))
            {
                return;
            }

            // Connect to "postgres" database to create the target database (cannot create DB while connected to it)
            builder.Database = "postgres";

            using var connection = new NpgsqlConnection(builder.ConnectionString);
            connection.Open();

            var exists = DatabaseExists(connection, databaseName);
            if (!exists)
            {
                CreateDatabase(connection, databaseName);
            }
        }

        private static bool DatabaseExists(NpgsqlConnection connection, string databaseName)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @dbname";
            cmd.Parameters.AddWithValue("dbname", databaseName);

            using var reader = cmd.ExecuteReader();
            return reader.HasRows;
        }

        private static void CreateDatabase(NpgsqlConnection connection, string databaseName)
        {
            // Quote identifier for names with special chars or mixed case
            var quotedName = QuoteDatabaseName(databaseName);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE {quotedName}";
            cmd.ExecuteNonQuery();
        }

        private static string QuoteDatabaseName(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
                return databaseName;

            // PostgreSQL: unquoted identifiers are folded to lowercase
            // Quote only if contains special chars, spaces, or mixed case
            var needsQuoting = databaseName.Any(c => !char.IsLetterOrDigit(c) && c != '_') ||
                              databaseName.Any(char.IsUpper);

            return needsQuoting ? $"\"{databaseName.Replace("\"", "\"\"")}\"" : databaseName;
        }
    }
}
