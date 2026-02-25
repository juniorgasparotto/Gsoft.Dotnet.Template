using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Shared.Infra.Module.Observability.Logging.Sinks.Postgres
{
    public class ConfigurationReader
    {
        public const string ColumnsSectionName = "Columns";
        public const string ColumnOrderSectionName = "ColumnOrder";

        private readonly IConfiguration _configuration;

        public ConfigurationReader(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IDictionary<string, ColumnWriterBase> GetColumnOptions(string? configurationPath)
        {
            IConfiguration rootSection = _configuration;

            if (!string.IsNullOrEmpty(configurationPath))
            {
                rootSection = _configuration.GetSection(configurationPath);
            }
            
            return GetColumnOptionsFromSection(rootSection);
        }

        public IDictionary<string, ColumnWriterBase> GetColumnOptionsFromSection(IConfigurationSection argsSection)
        {
            return GetColumnOptionsFromSection((IConfiguration)argsSection);
        }

        private IDictionary<string, ColumnWriterBase> GetColumnOptionsFromSection(IConfiguration rootSection)
        {
            var assemblies = GetAssemblies();
            var columnWriterTypes = assemblies.SelectMany(a => a.ExportedTypes)
                .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(ColumnWriterBase)))
                .ToList();

            // Tentar ler de "columns" (minúsculo) primeiro (dentro dos Args)
            IConfigurationSection columnsSection = rootSection.GetSection("columns");
            if (!columnsSection.Exists())
            {
                // Fallback para "Columns" (maiúsculo) para compatibilidade
                columnsSection = rootSection.GetSection(ColumnsSectionName);
            }

            if (!columnsSection.Exists())
            {
                throw new InvalidOperationException($"'{ColumnsSectionName}' or 'columns' section not found in provided configuration");
            }

            // Ler todas as colunas primeiro
            var allColumns = new Dictionary<string, ColumnWriterBase>();
            foreach (IConfigurationSection columnSection in columnsSection.GetChildren())
            {
                string columnName = columnSection.Key;
                ColumnWriterBase columnWriter = GetColumnWriter(columnSection, columnWriterTypes);
                allColumns[columnName] = columnWriter;
            }

            // Verificar se existe a seção ColumnOrder (array com a ordem)
            // Tentar ler de "columnOrder" (camelCase) primeiro (dentro dos Args)
            IConfigurationSection orderSection = rootSection.GetSection("columnOrder");
            if (!orderSection.Exists())
            {
                // Fallback para "ColumnOrder" (PascalCase) para compatibilidade
                orderSection = rootSection.GetSection(ColumnOrderSectionName);
            }

            if (orderSection.Exists())
            {
                // Usar a ordem explícita do ColumnOrder
                var columnOptions = new OrderedDictionary<string, ColumnWriterBase>();
                var order = orderSection.Get<string[]>() ?? Array.Empty<string>();

                foreach (var columnName in order)
                {
                    if (allColumns.TryGetValue(columnName, out var writer))
                    {
                        columnOptions[columnName] = writer;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Column '{columnName}' specified in ColumnOrder not found in Columns section");
                    }
                }

                // Verificar se todas as colunas foram incluídas
                var missing = allColumns.Keys.Except(order).ToList();
                if (missing.Any())
                {
                    throw new InvalidOperationException($"Columns not included in ColumnOrder: {string.Join(", ", missing)}");
                }

                return columnOptions;
            }
            else
            {
                // Sem ordem explícita, retorna na ordem que foi lida
                return allColumns;
            }
        }

        private ColumnWriterBase GetColumnWriter(IConfigurationSection columnSection, List<Type> columnWriterTypes)
        {
            //Parameterless constructor
            if (columnSection.Value != null)
            {
                var sectionValue = columnSection.Value;
                Type? columnWriterType = columnWriterTypes.FirstOrDefault(t =>
                    t.Name == sectionValue &&
                    t.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                                    .Any(c => c.GetParameters().Length == 0 || c.GetParameters().All(p => p.HasDefaultValue)));

                if (columnWriterType != null)
                {
                    (ConstructorInfo ctor, var parameters) = GetParameterlessConstructor(columnWriterType);

                    return (ColumnWriterBase)ctor.Invoke(parameters);
                }
                else
                {
                    throw new InvalidOperationException($"Cannot create writer of type {sectionValue} for column {columnSection.Key}");
                }
            }
            else
            {
                IConfigurationSection nameSection = columnSection.GetSection("Name");
                string? typeName = nameSection.Value;
                if (string.IsNullOrEmpty(typeName))
                {
                    throw new InvalidOperationException($"The configuration value in {nameSection.Path} has no 'Name' element.");
                }

                IConfigurationSection argsSection = columnSection.GetSection("Args");
                (ConstructorInfo ctor, var parameters) = GetConstructor(typeName, argsSection, columnWriterTypes);

                return (ColumnWriterBase)ctor.Invoke(parameters);
            }

            
        }

        private (ConstructorInfo, object?[]) GetConstructor(string typeName, IConfigurationSection argsSection, List<Type> columnWriterTypes)
        {
            var parametersSectionsDict = argsSection.GetChildren().ToDictionary(c => c.Key);

            var candidateConstructors = columnWriterTypes.Where(t => t.Name == typeName)
                .SelectMany(t => t.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
                .Where(c => c.GetParameters().All(p => p.HasDefaultValue || (p.Name != null && parametersSectionsDict.ContainsKey(p.Name))));

            ConstructorInfo? ctor = candidateConstructors
                .OrderByDescending(c => c.GetParameters().Count(p => p.Name != null && parametersSectionsDict.ContainsKey(p.Name)))
                .FirstOrDefault();

            if (ctor == null)
            {
                throw new InvalidOperationException($"Cannot create writer of type {typeName}: no suitable constructors found");
            }

            var parameters = new List<object?>(ctor.GetParameters().Length);
            foreach (ParameterInfo parameter in ctor.GetParameters())
            {
                if (parameter.Name != null && parametersSectionsDict.ContainsKey(parameter.Name))
                {
                    parameters.Add(parametersSectionsDict[parameter.Name].Get(parameter.ParameterType));
                }
                else
                {
                    parameters.Add(parameter.DefaultValue);
                }
            }

            return (ctor, parameters.ToArray());
        }

        private (ConstructorInfo, object?[]) GetParameterlessConstructor(Type columnWriterType)
        {
            var parameterlessCtor = columnWriterType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(c => c.GetParameters().Length == 0);
            if (parameterlessCtor != null)
            {
                return (parameterlessCtor, Array.Empty<object?>());
            }

            var ctorWithDefaultParams = columnWriterType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Where(c => c.GetParameters().All(p => p.HasDefaultValue))
                .OrderBy(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (ctorWithDefaultParams != null)
            {
                return (ctorWithDefaultParams, ctorWithDefaultParams.GetParameters().Select(p => p.DefaultValue).ToArray());
            }

            throw new InvalidOperationException($"Cannot find constructor for type {columnWriterType.FullName}");
        }

        private List<Assembly> GetAssemblies()
        {
            var assemblies = new List<Assembly> { typeof(PostgreSQLSink).Assembly };

            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                assemblies.Add(entryAssembly);
            }

            return assemblies;
        }
    }
}