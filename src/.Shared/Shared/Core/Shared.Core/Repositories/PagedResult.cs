namespace Shared.Core.Repositories;

public class PagedResult
{
    public int CurrentPage { get; set; }
    public int PageCount { get; set; }
    public int PageSize { get; set; }
    public int RowCount { get; set; }
    public IEnumerable<object> Results { get; set; } = Array.Empty<object>();
}

public class PagedResult<T>
{
    public int CurrentPage { get; set; }
    public int PageCount { get; set; }
    public int PageSize { get; set; }
    public int RowCount { get; set; }
    public List<T> Results { get; set; } = new();
}
