namespace Shared.Infra.Module.EntityFramework.Repositories;

using Microsoft.EntityFrameworkCore;
using Shared.Core.Entities.Interfaces;
using Shared.Core.Repositories;
using Shared.Core.Repositories.Exceptions;
using System.Collections;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

public partial class Repository<TContext, T> : IRepository<T> where TContext: DbContext where T : class
{
    private readonly string TYPE_NAME = typeof(T).Name;
    private readonly string PROPERTY_ID_NAME = nameof(IEntityId.Id);
    private readonly string PROPERTY_USERS_NAME = nameof(IMultiTenantSharedList<IMultiTenantShared>.Users);
    private readonly string PROPERTY_USER_ID_NAME = nameof(IMultiTenant.UserId);
    private readonly string PROPERTY_ACCEPT_NAME = nameof(IMultiTenantShared.Accept);
    private readonly string PROPERTY_CREATED = nameof(IEntityDateAudit.Created);
    private readonly bool IS_MULTITENANT_LIST = typeof(T).GetInterfaces().Any(f => f.IsGenericType && f.GetGenericTypeDefinition() == typeof(IMultiTenantSharedList<>));
    private readonly bool IS_ENTITY_ID = typeof(IEntityId).IsAssignableFrom(typeof(T));
    private readonly bool IS_MULTITENANT_SINGLE = typeof(IMultiTenant).IsAssignableFrom(typeof(T));
    private readonly bool IS_ENTITY_AUDIT = typeof(IEntityDateAudit).IsAssignableFrom(typeof(T));
    private readonly EntityInfoService _entityInfoService;
    private readonly EntityInfoService.Info<T>? _entityInfo;
    private readonly TContext _dataContext;

    public virtual string PropertyIdName { get; } = "Id";
    public virtual string PropertyTextName { get; } = "Name";

    public Repository(TContext dataContext, EntityInfoService entityInfoService)
    {
        _entityInfoService = entityInfoService;
        _entityInfo = _entityInfoService.GetEntityInfo<T>() as EntityInfoService.Info<T>;
        _dataContext = dataContext;
    }

    #region All

    public IQueryable GetAllAsDynamic(RepositoryOptions options)
    {
        //var a = (from t in _dataContext.Transactions
        //         group new { t.Name, CategoryName = t.Category.Name } by new { t.Name } into g
        //         select new
        //         {
        //             Name = g.Max(f => f.Name),
        //             Category = g.Max(f => f.CategoryName)
        //         }
        //         ).ToList();

        IQueryable<T> query;

        if (options?.AsNoTracking == true)
            query = _dataContext.Set<T>().AsNoTracking().AsQueryable();
        else
            query = _dataContext.Set<T>().AsQueryable();

        var queryNonGeneric = (IQueryable)query;

        if (options != null)
        {
            var opts = options;
            query = opts.Queriable(query);
            query = SetUser(query, opts);

            if (!string.IsNullOrWhiteSpace(opts.Includes))
                foreach (var i in opts.GetSplited(opts.Includes))
                    query = query.Include(i);

            queryNonGeneric = query;

            if (!string.IsNullOrWhiteSpace(opts.Where))
                queryNonGeneric = queryNonGeneric.Where(opts.Where, opts.Args ?? Array.Empty<string>());

            if (!string.IsNullOrWhiteSpace(opts.GroupBy))
            {
                opts.GroupBySelector ??= "it";
                queryNonGeneric = queryNonGeneric.GroupBy(opts.GroupBy, opts.GroupBySelector);
            }

            if (opts.Fields != null && opts.Fields.Length > 0)
                queryNonGeneric = queryNonGeneric.Select(GetSelect(opts.Fields));

            // Não posso setar um default no dynamic, pois pode haver groupby
            if (!string.IsNullOrWhiteSpace(opts.Sort))
                queryNonGeneric = queryNonGeneric.OrderBy(opts.Sort);

            // Não posso setar um default no dynamic, pois pode haver groupby
            //if (_entityInfo?.CompleteGlobalEntityQuery != null)
            //    queryNonGeneric = _entityInfo.CompleteGlobalEntityQuery(opts, query);

            // O take deve sempre vir no final, do contrário ele cria uma subquery e o WHERE e ORDER BY ficam fora da subquery
            if (opts.Top > 0)
                queryNonGeneric = queryNonGeneric.Take(opts.Top);
        }

        return queryNonGeneric;
    }

    public IQueryable<T> GetAll(RepositoryOptions options)
    {
        IQueryable<T> query;

        if (options?.AsNoTracking == true)
            query = _dataContext.Set<T>().AsNoTracking().AsQueryable();
        else
            query = _dataContext.Set<T>().AsQueryable();

        if (options != null)
        {
            query = options.Queriable<T>(query);
            query = SetUser(query, options!);

            if (!string.IsNullOrWhiteSpace(options.Includes))
                foreach (var i in options.GetSplited(options.Includes))
                    query = query.Include(i);

            if (!string.IsNullOrWhiteSpace(options.Where))
                query = query.Where(options.Where, options.Args);

            if (options.Fields != null && options.Fields.Length > 0)
            {
                string select = GetSelect(options.Fields);
                query = Select(query, select);
            }

            var sort = GetSort(options);
            if (!string.IsNullOrWhiteSpace(sort))
                query = query.OrderBy(sort);

            if (_entityInfo?.CompleteGlobalEntityQuery != null)
                query = _entityInfo.CompleteGlobalEntityQuery(options, query);

            if (options.Top > 0)
                query = (IQueryable<T>)query.Take(options.Top);
        }

        return query;
    }

    public IQueryable<T> GetAllByIdOrName(RepositoryOptions options, string idOrName, bool useLike)
    {
        options ??= new RepositoryOptions();
        options.ContinueQueriable<T>(query =>
        {
            string? propId = null;
            var method = useLike ? "StartsWith" : "Equals";

            if (IS_ENTITY_ID)
                propId = PropertyIdName;
            else if (_entityInfo?.PropertyIdName != null)
                propId = _entityInfo.PropertyIdName;

            if (propId != null)
                return query = query.Where($"{GetLeftFieldId(propId)}.{method}(@0) || {PropertyTextName}.{method}(@0)", idOrName);
            else
                return query = query.Where($"{PropertyTextName}.{method}(@0)", idOrName);
        }
        );

        var query = GetAll(options);
        return query;
    }

    #endregion

    public System.Linq.Dynamic.Core.PagedResult GetAllPaginationAsDynamic(RepositoryOptions options)
    {
        var query = GetAllAsDynamic(options);
        return query.PageResult(GetInitPage(options), options.PageSize);
    }

    #region ById

    public T? GetById(RepositoryOptions options, Guid id)
    {
        return GetByIdAsync(options, id).Result;
    }

    public T? GetByIdOrName(RepositoryOptions options, string idOrName, bool useLike)
    {
        return this.GetByIdOrNameAsync(options, idOrName, useLike).Result;
    }

    public async Task<T?> GetByIdAsync(RepositoryOptions options, Guid id)
    {
        var query = GetAll(options);
        query = SetId(id, query);
        return await query.FirstOrDefaultAsync();
    }

    public async Task<T?> GetByIdOrNameAsync(RepositoryOptions options, string idOrName, bool useLike)
    {
        var entities = await ToIEnumerableAsync(GetAllByIdOrName(options, idOrName, useLike));
        return GetUniqueByIdOrName(PropertyIdName, PropertyTextName, idOrName, entities);
    }

    #endregion

    #region All Pagination

    public System.Linq.Dynamic.Core.PagedResult<T> GetAllPagination(RepositoryOptions options)
    {
        var query = GetAll(options);
        return query.PageResult(GetInitPage(options), options.PageSize);
    }

    public System.Linq.Dynamic.Core.PagedResult<T> GetAllPaginationByIdOrName(RepositoryOptions options, string idOrName, bool useLike)
    {
        var query = GetAllByIdOrName(options, idOrName, useLike);
        return query.PageResult(GetInitPage(options), options.PageSize);
    }

    #endregion

    public IQueryable<T> Include<TProperty>(IQueryable<T> queriable, Expression<Func<T, TProperty>> navigationPropertyPath)
    {
        queriable = queriable.Include(navigationPropertyPath);
        return queriable;
    }

    #region Writes


    public void Add(object entity, bool acceptReferences = false)
    {
        if (entity is IEntityId entityId && entityId.Id == default)
            entityId.Id = Guid.NewGuid();

        if (!acceptReferences)
        {
            // Por padrão, desativa a inserção/updade/delete de entidades de todo o grafo,
            // o foco deve ser na entidade root apenas
            _dataContext.ChangeTracker.TrackGraph(entity, e =>
            {
                e.Entry.State = EntityState.Unchanged;
            });
        }

        if (entity is IEntityDateAudit cast)
        {
            cast.Created = cast.Created == DateTime.MinValue ? DateTime.Now : cast.Created;
            cast.Updated = cast.Updated == DateTime.MinValue ? DateTime.Now : cast.Updated;
        }

        _dataContext.Add(entity);
    }

    public void Update(object entity)
    {
        if (entity is IEntityDateAudit cast)
            cast.Updated = DateTime.Now;

        _dataContext.Update(entity);
    }

    public void Delete(object entity)
    {
        _dataContext.Remove(entity);
    }

    public void MapValues(object from, object to)
    {
        var entry = _dataContext.Entry(to);
        if (entry != null)
        {
            if (from is IEntityDateAudit entity && to is IEntityDateAudit toAudit)
            {
                entity.Created = toAudit.Created;
                entity.Updated = toAudit.Updated;
            }

            if (from is IEntityId entityId && to is IEntityId toEntityId)
                entityId.Id = toEntityId.Id;

            if (from is IMultiTenant multiTenant && to is IMultiTenant toMultiTenant)
                multiTenant.UserId = toMultiTenant.UserId;

            entry.CurrentValues.SetValues(from);
        }
    }

    public void Detach(Func<T, bool> expression)
    {
        var local = _dataContext.Set<T>().Local
            .Where(expression)
            .FirstOrDefault();

        if (local != null)
            _dataContext.Entry(local).State = EntityState.Detached;
    }

    #endregion

    #region IEnumerable Async

    public async Task<IEnumerable<T>> ToIEnumerableAsync(IQueryable<T> query, CancellationToken cancellationToken = default)
    {
        return await query.ToArrayAsync(cancellationToken);
    }

    public async Task<IEnumerable> ToIEnumerableAsync(IQueryable query, CancellationToken cancellationToken = default)
    {
        return await query.ToDynamicArrayAsync();
    }

    #endregion

    public C? GetContext<C>() where C : class
    {
        return _dataContext as C;
    }

    #region Set Users

    private IQueryable<T> SetUser(IQueryable<T> query, RepositoryOptions options)
    {
        if (options is RepositoryMultiTenantAsOwnerOptions asOwnerOptions)
            query = SetUserAsOwner(query, asOwnerOptions);
        else if (options is RepositoryMultiTenantAsGuestOptions asGuestOptions)
            query = SetUserAsGuest(query, asGuestOptions);
        else if (options is RepositoryMultiTenantAsGuestAndOwnerOptions asGuestAndOwnerOptions)
            query = SetUserAsGuestAndOwnerOptions(query, asGuestAndOwnerOptions);
        return query;
    }

    private IQueryable<T> SetUserAsOwner(IQueryable<T> query, RepositoryMultiTenantAsOwnerOptions options)
    {
        // Não precisa do IS_MULTITENANT_LIST pq todos que são lista são single tb
        if (IS_MULTITENANT_SINGLE && options.UserIds != null)
            return query.Where($"o => @0.Contains(o.{PROPERTY_USER_ID_NAME})", options.UserIds);

        throw new NotSupportedException($"The entity '{TYPE_NAME}' not support the MultiTenantMode='{nameof(MultiTenantMode.AsOwner)}'");
    }

    private IQueryable<T> SetUserAsGuest(IQueryable<T> query, RepositoryMultiTenantAsGuestOptions options)
    {
        if (IS_MULTITENANT_LIST && options.UserIds != null)
            return query.Where($"o => o.{PROPERTY_USERS_NAME}.Any(u => o.{PROPERTY_USER_ID_NAME} != u.{PROPERTY_USER_ID_NAME} && @0.Contains(u.{PROPERTY_USER_ID_NAME}) && u.{PROPERTY_ACCEPT_NAME})", options.UserIds);

        throw new NotSupportedException($"The entity '{TYPE_NAME}' not support the MultiTenantMode='{nameof(MultiTenantMode.AsGuest)}'");
    }

    private IQueryable<T> SetUserAsGuestAndOwnerOptions(IQueryable<T> query, RepositoryMultiTenantAsGuestAndOwnerOptions options)
    {
        if (options.UserIds == null)
            return query;
        if (IS_MULTITENANT_LIST)
            return query.Where($"o => o.{PROPERTY_USERS_NAME}.Any(u => @0.Contains(u.{PROPERTY_USER_ID_NAME}) && u.{PROPERTY_ACCEPT_NAME})", options.UserIds);
        else if (IS_MULTITENANT_SINGLE)
            return query.Where($"o => @0.Contains(o.{PROPERTY_USER_ID_NAME})", options.UserIds);

        throw new NotSupportedException($"The entity '{TYPE_NAME}' not support the MultiTenantMode='{nameof(MultiTenantMode.AsGuestAndOwner)}'");
    }

    #endregion

    #region Privates

    private string? GetSort(RepositoryOptions options)
    {
        // 1) Ordena pelo o que o usuário solicitou
        // 2) Ordena pela config global da entidade se existir.
        //    Se não tiver sort nesse global, nenhuma ordenação será utilizada.
        // 3) Usa o campo de data de criação
        var sort = options.Sort;
        if (IS_ENTITY_AUDIT && sort == null && _entityInfo?.CompleteGlobalEntityQuery == null)
            sort = $"{PROPERTY_CREATED} DESC";

        return sort;
    }

    private IQueryable<T> SetId(Guid id, IQueryable<T> query)
    {
        query = query.Where($"o => o.{PROPERTY_ID_NAME} == @0", id);
        return query;
    }

    private string GetLeftFieldId(string propertyIdName)
    {
        var idLeft = propertyIdName;
        var prop = typeof(T).GetProperty(propertyIdName);
        if (prop != null && prop.PropertyType != typeof(string))
            idLeft = $"{idLeft}.ToString()";
        return idLeft;
    }

    public string GetSelect(string[] fields)
    {
        return $"new {{ {string.Join(',', fields)} }}";
    }

    private IQueryable<T> Select(IQueryable source, string selector, params object[] values)
    {
        if (source == null) throw new ArgumentNullException("source");
        if (selector == null) throw new ArgumentNullException("selector");

        var lambda = DynamicExpressionParser.ParseLambda(source.ElementType, typeof(T), selector, values);
        return source.Provider.CreateQuery<T>(
            Expression.Call(
                typeof(Queryable), "Select",
                new Type[] { source.ElementType, typeof(T) },
                source.Expression, Expression.Quote(lambda)));
    }

    private T? GetUniqueByIdOrName(string propertyIdName, string propertyTextName, string idOrName, IEnumerable<T> entities)
    {
        if (entities.Count() > 1)
        {
            var msg = $"Duplicate id or name with value '{idOrName}': ";
            foreach (var i in entities)
            {
                var idProp = i.GetType().GetProperty(propertyIdName);
                var nameProp = i.GetType().GetProperty(propertyTextName);
                var id = idProp?.GetValue(i);
                var name = nameProp?.GetValue(i);
                msg += $"({name}: {id}) ";
            }

            throw new MoreThanOneIdOrTextException(idOrName, msg);
        }

        return entities.SingleOrDefault();
    }

    private int GetInitPage(RepositoryOptions options)
    {
        return options.Page != 0 ? options.Page : 1;
    }

    #endregion
}

