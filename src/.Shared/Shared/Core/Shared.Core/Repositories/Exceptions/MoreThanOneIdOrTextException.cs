namespace Shared.Core.Repositories.Exceptions;

public class MoreThanOneIdOrTextException : Exception
{
    public string SearchText { get; }

    public MoreThanOneIdOrTextException(string searchText, string msg)
        : base(msg)
    {
        SearchText = searchText;
    }
}