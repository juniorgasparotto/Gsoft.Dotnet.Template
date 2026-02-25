namespace Shared.Core.Repositories;

public class RepositoryOptions
{
    private List<object>? queryContinuations;

    public string Where { get; set; } = null!;
    public string[] Args { get; set; } = null!;
    public string Includes { get; set; } = null!;
    public string Sort { get; set; } = null!;
    public string GroupBy { get; set; } = null!;
    public string GroupBySelector { get; set; } = null!;
    public string[] Fields { get; set; } = null!;
    public int Top { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    public string Aggregate { get; set; } = null!;
    public string AggregateMember { get; set; } = null!;
    public bool AsNoTracking { get; set; }

    public RepositoryOptions SetWhere(string where)
    {
        this.Where = where;
        return this;
    }

    public RepositoryOptions SetArgs(string[] args)
    {
        this.Args = args;
        return this;
    }

    public RepositoryOptions SetIncludes(string includes)
    {
        this.Includes = includes;
        return this;
    }

    public RepositoryOptions SetSort(string sort)
    {
        this.Sort = sort;
        return this;
    }

    public RepositoryOptions SetAsNoTracking(bool noTracking)
    {
        this.AsNoTracking = noTracking;
        return this;
    }

    public RepositoryOptions SetPage(int page)
    {
        this.Page = page;
        return this;
    }

    public RepositoryOptions SetPageSize(int pageSize)
    {
        this.PageSize = pageSize;
        return this;
    }

    public RepositoryOptions SetTop(int top)
    {
        this.Top = top;
        return this;
    }

    public RepositoryOptions ContinueQueriable<T>(Func<IQueryable<T>, IQueryable<T>> continueQuery)
    {
        (this.queryContinuations ??= new List<object>()).Add(continueQuery);
        return this;
    }

    public IQueryable<T> Queriable<T>(IQueryable<T> query)
    {
        if (queryContinuations != null)
        {
            foreach (var qc in queryContinuations)
                if (qc != null && qc is Func<IQueryable<T>, IQueryable<T>> func)
                    query = func(query);
        }

        return query;
    }

    public string[] GetSplited(string value)
    {
        return value.Split(',').Select(f => f.Trim()).ToArray();
    }
}
