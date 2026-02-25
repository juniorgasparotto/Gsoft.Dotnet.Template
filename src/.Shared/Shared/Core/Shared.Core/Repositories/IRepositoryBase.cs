using System;
using System.Collections;
using System.Linq.Expressions;

namespace Shared.Core.Repositories
{
    public interface IRepositoryBase<out T>
    {
        string PropertyIdName { get; }
        string PropertyTextName { get; }

        #region All
        IQueryable GetAllAsDynamic(RepositoryOptions options);
        IQueryable<T> GetAll(RepositoryOptions options);
        IQueryable<T> GetAllByIdOrName(RepositoryOptions options, string idOrName, bool useLike);
        #endregion

        #region All Pagination
        System.Linq.Dynamic.Core.PagedResult GetAllPaginationAsDynamic(RepositoryOptions options);
        Task<IEnumerable> ToIEnumerableAsync(IQueryable query, CancellationToken cancellationToken = default);
        #endregion

        #region ById
        T GetByIdOrName(RepositoryOptions options, string idOrName, bool useLike);
        T GetById(RepositoryOptions options, Guid id);
        #endregion

        #region Writes
        void Add(object entity, bool acceptReferences = false);
        void Update(object entity);
        void Delete(object entity);
        void MapValues(object from, object to);
        #endregion

        C GetContext<C>() where C : class;
        string GetSelect(string[] fields);
    }

    public interface IRepository<T> : IRepositoryBase<T>
    {
        #region Unique entity
        Task<T> GetByIdOrNameAsync(RepositoryOptions options, string idOrName, bool useLike);
        Task<T> GetByIdAsync(RepositoryOptions options, Guid id);
        #endregion

        #region Multiple entities
        System.Linq.Dynamic.Core.PagedResult<T> GetAllPagination(RepositoryOptions options);
        System.Linq.Dynamic.Core.PagedResult<T> GetAllPaginationByIdOrName(RepositoryOptions options, string idOrName, bool useLike);
        #endregion

        #region Extensions
        void Detach(Func<T, bool> expression);
        IQueryable<T> Include<TProperty>(IQueryable<T> queriable, Expression<Func<T, TProperty>> navigationPropertyPath);
        Task<IEnumerable<T>> ToIEnumerableAsync(IQueryable<T> query, CancellationToken cancellationToken = default);
        #endregion
    }
}
