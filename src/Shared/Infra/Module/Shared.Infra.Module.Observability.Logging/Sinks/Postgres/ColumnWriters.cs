using System;
using System.Text;
using NpgsqlTypes;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Shared.Infra.Module.Observability.Logging.Sinks.Postgres
{
    public abstract class ColumnWriterBase
    {
        /// <summary>
        /// Column type
        /// </summary>
        public NpgsqlDbType DbType { get; }

        public int? ColumnLength { get; }
        

        protected ColumnWriterBase(NpgsqlDbType dbType, int? columnLength = null)
        {
            DbType = dbType;
            ColumnLength = columnLength;
        }

        /// <summary>
        /// Gets part of log event to write to the column
        /// </summary>
        /// <param name="logEvent"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public abstract object GetValue(LogEvent logEvent, IFormatProvider formatProvider = null);

    }

    /// <summary>
    /// Writes timestamp part
    /// </summary>
    public class TimestampColumnWriter : ColumnWriterBase
    {
        public TimestampColumnWriter(NpgsqlDbType dbType = NpgsqlDbType.Timestamp) : base(dbType)
        {
        }

        public override object GetValue(LogEvent logEvent, IFormatProvider formatProvider = null)
        {
            if (DbType == NpgsqlDbType.Timestamp)
            {
                return logEvent.Timestamp.DateTime;
            }

            return logEvent.Timestamp;
        }
    }

    /// <summary>
    /// Writes message part
    /// </summary>
    public class RenderedMessageColumnWriter : ColumnWriterBase
    {
        public RenderedMessageColumnWriter(NpgsqlDbType dbType = NpgsqlDbType.Text, int? columnLength = null) : base(dbType, columnLength)
        {
        }

        public override object GetValue(LogEvent logEvent, IFormatProvider formatProvider = null)
        {
            return logEvent.RenderMessage(formatProvider);
        }
    }

    /// <summary>
    /// Writes non rendered message
    /// </summary>
    public class MessageTemplateColumnWriter : ColumnWriterBase
    {
        public MessageTemplateColumnWriter(NpgsqlDbType dbType = NpgsqlDbType.Text, int? columnLength = null) : base(dbType, columnLength)
        {
        }

        public override object GetValue(LogEvent logEvent, IFormatProvider formatProvider = null)
        {
            return logEvent.MessageTemplate.Text;
        }
    }

    /// <summary>
    /// Writes log level
    /// </summary>
    public class LevelColumnWriter : ColumnWriterBase
    {
        private readonly bool _renderAsText;

        public LevelColumnWriter(bool renderAsText = false, NpgsqlDbType dbType = NpgsqlDbType.Integer, int? columnLength = null) : base(dbType, columnLength)
        {
            _renderAsText = renderAsText;
        }

        public override object GetValue(LogEvent logEvent, IFormatProvider formatProvider = null)
        {
            if (_renderAsText)
            {
                return logEvent.Level.ToString();
            }

            return (int)logEvent.Level;
        }
    }

    /// <summary>
    /// Writes exception (just it ToString())
    /// </summary>
    public class ExceptionColumnWriter : ColumnWriterBase
    {
        public ExceptionColumnWriter(NpgsqlDbType dbType = NpgsqlDbType.Text, int? columnLength = null) : base(dbType, columnLength)
        {
        }

        public override object GetValue(LogEvent logEvent, IFormatProvider formatProvider = null)
        {
            return logEvent.Exception == null ? (object)DBNull.Value : logEvent.Exception.ToString();
        }
    }

    /// <summary>
    /// Writes all event properties as json
    /// </summary>
    public class PropertiesColumnWriter : ColumnWriterBase
    {
        public PropertiesColumnWriter(NpgsqlDbType dbType = NpgsqlDbType.Jsonb, int? columnLength = null) : base(dbType, columnLength)
        {
        }

        public override object GetValue(LogEvent logEvent, IFormatProvider formatProvider = null)
        {
            return PropertiesToJson(logEvent);
        }

        private object PropertiesToJson(LogEvent logEvent)
        {
            if (logEvent.Properties.Count == 0)
                return "{}";

            var valuesFormatter = new JsonValueFormatter();

            var sb = new StringBuilder();

            sb.Append("{");

            using (var writer = new System.IO.StringWriter(sb))
            {
                foreach (var logEventProperty in logEvent.Properties)
                {
                    sb.Append($"\"{logEventProperty.Key}\":");

                    valuesFormatter.Format(logEventProperty.Value, writer);

                    sb.Append(", ");
                }
            }

            sb.Remove(sb.Length - 2, 2);
            sb.Append("}");

            return sb.ToString();
        }
    }

    /// <summary>
    /// Writes log event as json
    /// </summary>
    public class LogEventSerializedColumnWriter : ColumnWriterBase
    {
        public LogEventSerializedColumnWriter(NpgsqlDbType dbType = NpgsqlDbType.Jsonb, int? columnLength = null) : base(dbType, columnLength)
        {
        }

        public override object GetValue(LogEvent logEvent, IFormatProvider formatProvider = null)
        {
            return LogEventToJson(logEvent, formatProvider);
        }

        private object LogEventToJson(LogEvent logEvent, IFormatProvider formatProvider)
        {
            var jsonFormatter = new JsonFormatter(formatProvider: formatProvider);

            var sb = new StringBuilder();
            using (var writer = new System.IO.StringWriter(sb))
                jsonFormatter.Format(logEvent, writer);
            return sb.ToString();
        }
    }

    /// <summary>
    /// Write single event property
    /// </summary>
    public class SinglePropertyColumnWriter : ColumnWriterBase
    {
        public string Name { get; }
        public PropertyWriteMethod WriteMethod { get; }
        public string Format { get; }

        public SinglePropertyColumnWriter(string propertyName, PropertyWriteMethod writeMethod = PropertyWriteMethod.ToString, 
                                            NpgsqlDbType dbType = NpgsqlDbType.Text, string format = null, int? columnLength = null) : base(dbType, columnLength)
        {
            Name = propertyName;
            WriteMethod = writeMethod;
            Format = format;
        }

        public override object GetValue(LogEvent logEvent, IFormatProvider formatProvider = null)
        {
            if (!logEvent.Properties.ContainsKey(Name))
            {
                // Para JSONB, retornar null ao invés de DBNull para evitar 0x00
                if (DbType == NpgsqlDbType.Jsonb || DbType == NpgsqlDbType.Json)
                {
                    return DBNull.Value;
                }
                return DBNull.Value;
            }

            switch (WriteMethod)
            {
                case PropertyWriteMethod.Raw:
                    var rawValue = GetPropertyValue(logEvent.Properties[Name]);
                    // Evitar null byte em strings
                    if (rawValue is string str && string.IsNullOrEmpty(str))
                    {
                        return DbType == NpgsqlDbType.Jsonb || DbType == NpgsqlDbType.Json 
                            ? DBNull.Value 
                            : DBNull.Value;
                    }
                    return rawValue ?? DBNull.Value;
                    
                case PropertyWriteMethod.Json:
                    var valuesFormatter = new JsonValueFormatter();

                    var sb = new StringBuilder();

                    using (var writer = new System.IO.StringWriter(sb))
                    {
                        valuesFormatter.Format(logEvent.Properties[Name], writer);
                    }

                    var jsonResult = sb.ToString();
                    
                    // Garantir que não retornamos string vazia ou inválida para JSONB
                    if (string.IsNullOrWhiteSpace(jsonResult) || jsonResult == "null")
                    {
                        return DBNull.Value;
                    }
                    
                    // Remover caracteres nulos que podem causar erro 0x00
                    jsonResult = jsonResult.Replace("\0", "");
                    
                    return jsonResult;

                default:
                    if (logEvent.Properties[Name] is ScalarValue scalarValue)
                    {
                        var value = scalarValue.Value;
                        if (value == null)
                            return DBNull.Value;
                        // Evitar null byte em strings
                        if (value is string s)
                            return string.IsNullOrEmpty(s) ? DBNull.Value : s.Replace("\0", "");
                        return value;
                    }

                    var result = logEvent.Properties[Name].ToString(Format, formatProvider);
                    return string.IsNullOrEmpty(result) ? DBNull.Value : result.Replace("\0", "");
            }

        }

        private object GetPropertyValue(LogEventPropertyValue logEventProperty)
        {
            //TODO: Add support for arrays
            if (logEventProperty is ScalarValue scalarValue)
            {
                return scalarValue.Value;
            }

            return logEventProperty;
        }
    }

    public enum PropertyWriteMethod
    {
        Raw,
        ToString,
        Json
    }
}