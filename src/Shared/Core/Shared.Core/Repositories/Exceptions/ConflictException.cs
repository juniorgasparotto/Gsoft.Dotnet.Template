namespace Shared.Core.Repositories.Exceptions;

[Serializable]
public class ConflictException : Exception
{
    public string FieldName { get; }
    public string Value { get; }

    public ConflictException()
    {

    }

    public ConflictException(string message) : base(message)
    {

    }

    public ConflictException(string fieldName, string message) : base(message)
    {
        this.FieldName = fieldName;
    }
}