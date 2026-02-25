using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Shared.Infra.Module.Observability.Logging.Sinks.Postgres;

/// <summary>
/// Extensões para configurar o sink PostgreSQL customizado via JSON
/// Namespace Serilog é necessário para funcionar com ReadFrom.Configuration()
/// </summary>
public static class PostgreSQLExtensions
{
    /// <summary>
    /// Tempo padrão entre batches
    /// </summary>
    public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Adiciona sink PostgreSQL com colunas definidas na seção "columns" dentro dos Args
    /// </summary>
    public static LoggerConfiguration PostgreSQL(
        this LoggerSinkConfiguration sinkConfiguration,
        string connectionString,
        string tableName,
        IConfiguration configuration,
        string configurationPath = "",
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        TimeSpan? period = null,
        IFormatProvider? formatProvider = null,
        int batchSizeLimit = PostgreSQLSink.DefaultBatchSizeLimit,
        LoggingLevelSwitch? levelSwitch = null,
        bool useCopy = true,
        string schemaName = "",
        bool needAutoCreateTable = false,
        bool needAutoCreateDatabase = false,
        // Parâmetros opcionais para permitir que o Serilog passe columnOrder e columns dos Args
        // Esses valores são ignorados, pois lemos da configuração
        object? columnOrder = null,
        object? columns = null)
    {
        if (sinkConfiguration == null)
            throw new ArgumentNullException(nameof(sinkConfiguration));

        IDictionary<string, ColumnWriterBase> columnOptions;

        // Se configurationPath estiver vazio, tentar ler dos Args do sink (Serilog passa os Args como uma seção)
        if (string.IsNullOrEmpty(configurationPath))
        {
            // Tentar ler diretamente da configuração raiz (quando chamado via Serilog, os Args são passados como uma seção)
            // O Serilog passa os Args como uma IConfigurationSection através de reflection
            // Mas como não temos acesso direto a isso, vamos tentar ler da seção "Serilog:WriteTo" e encontrar nosso sink
            var writeToSection = configuration.GetSection("Serilog:WriteTo");
            IConfigurationSection? argsSection = null;

            // Procurar pelo sink PostgreSQL nos WriteTo
            foreach (var writeToChild in writeToSection.GetChildren())
            {
                var name = writeToChild.GetSection("Name").Value;
                if (name == "PostgreSQL")
                {
                    argsSection = writeToChild.GetSection("Args");
                    break;
                }
            }

            if (argsSection != null && argsSection.Exists())
            {
                // Ler columnOrder e columns de dentro dos Args
                columnOptions = new ConfigurationReader(configuration).GetColumnOptionsFromSection(argsSection);
            }
            else
            {
                // Fallback: tentar ler da raiz (comportamento antigo)
                columnOptions = new ConfigurationReader(configuration).GetColumnOptions("");
            }
        }
        else
        {
            // Comportamento antigo: ler de um caminho específico
            columnOptions = new ConfigurationReader(configuration).GetColumnOptions(configurationPath);
        }

        period ??= DefaultPeriod;

        return sinkConfiguration.Sink(
            new PostgreSQLSink(
                connectionString,
                tableName,
                period.Value,
                formatProvider,
                columnOptions,
                batchSizeLimit,
                useCopy,
                schemaName,
                needAutoCreateTable,
                needAutoCreateDatabase),
            restrictedToMinimumLevel,
            levelSwitch);
    }

    /// <summary>
    /// Adiciona sink PostgreSQL com colunas customizadas passadas diretamente
    /// </summary>
    public static LoggerConfiguration PostgreSQL(
        this LoggerSinkConfiguration sinkConfiguration,
        string connectionString,
        string tableName,
        IDictionary<string, ColumnWriterBase>? columnOptions = null,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        TimeSpan? period = null,
        IFormatProvider? formatProvider = null,
        int batchSizeLimit = PostgreSQLSink.DefaultBatchSizeLimit,
        LoggingLevelSwitch? levelSwitch = null,
        bool useCopy = true,
        string schemaName = "",
        bool needAutoCreateTable = false,
        bool needAutoCreateDatabase = false,
        bool respectCase = false)
    {
        if (sinkConfiguration == null)
            throw new ArgumentNullException(nameof(sinkConfiguration));

        period ??= DefaultPeriod;

        return sinkConfiguration.Sink(
            new PostgreSQLSink(
                connectionString,
                tableName,
                period.Value,
                formatProvider,
                columnOptions,
                batchSizeLimit,
                useCopy,
                schemaName,
                needAutoCreateTable,
                needAutoCreateDatabase,
                respectCase),
            restrictedToMinimumLevel,
            levelSwitch);
    }
}

