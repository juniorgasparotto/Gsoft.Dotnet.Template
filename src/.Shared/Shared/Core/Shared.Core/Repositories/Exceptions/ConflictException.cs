namespace Shared.Core.Repositories.Exceptions;

[Serializable]
public class ConflictException : Exception
{
    public string FieldName { get; }
    public string Value { get; }

    public ConflictException()
    {
        FieldName = string.Empty;
        Value = string.Empty;
    }

    public ConflictException(string message) : base(message)
    {
        FieldName = string.Empty;
        Value = string.Empty;
    }

    public ConflictException(string fieldName, string message) : base(message)
    {
        FieldName = fieldName;
        Value = string.Empty;
    }

    public ConflictException(string fieldName, string value, string message) : base(message)
    {
        FieldName = fieldName;
        Value = value;
    }
}