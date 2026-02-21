using System;
using System.Linq;
using Shared.Core.Repositories;

namespace Shared.Infra.Module.EntityFramework.Extensions;

public static class IQueryableExtensions
{
    public static PagedResult PageResult(this System.Linq.IQueryable query, int page, int pageSize)
    {
        var rowCount = query.Cast<object>().Count();
        var pageCount = (int)Math.Ceiling((double)rowCount / pageSize);
        var results = query.Cast<object>().Skip((page - 1) * pageSize).Take(pageSize).ToArray();

        return new PagedResult
        {
            CurrentPage = page,
            PageCount = pageCount,
            PageSize = pageSize,
            RowCount = rowCount,
            Results = results
        };
    }

    public static PagedResult<T> PageResult<T>(this IQueryable<T> query, int page, int pageSize)
    {
        var rowCount = query.Count();
        var pageCount = (int)Math.Ceiling((double)rowCount / pageSize);
        var results = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<T>
        {
            CurrentPage = page,
            PageCount = pageCount,
            PageSize = pageSize,
            RowCount = rowCount,
            Results = results
        };
    }
}
