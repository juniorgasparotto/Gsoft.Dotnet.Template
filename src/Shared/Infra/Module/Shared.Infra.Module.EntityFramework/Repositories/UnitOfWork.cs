using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shared.Core.Entities;
using Shared.Core.Repositories;
using Shared.Core.Repositories.Exceptions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Shared.Infra.Module.EntityFramework.Repositories
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly DbContext _dataContext;
        private IDbContextTransaction? _transaction;

        public IRepository<User> Users { get; } = null!;

        public UnitOfWork(DbContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task SaveAsync()
        {
            await _dataContext.SaveChangesAsync();
        }

        public async Task<IDisposable> BeginTransactionAsync()
        {
            _transaction = await _dataContext.Database.BeginTransactionAsync();
            return _transaction;
        }

        public async Task CommitAsync()
        {
            try
            {
                await _dataContext.SaveChangesAsync();
                if (_transaction != null)
                    await _transaction.CommitAsync();
            }
            catch (DbUpdateException ex) when (ex.IsPKException(out string? pk, out string? msg) == true)
            {
                throw new ConflictException(pk ?? "", msg ?? "");
            }
        }

        public async Task Rollback()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
            }
        }

        public IRepository<TRepo> FindRepository<TRepo>()
        {
            foreach (var p in GetType().GetProperties())
            {
                if (p.PropertyType == typeof(IRepository<TRepo>) || p.PropertyType.GetInterfaces().Any(f => f == typeof(IRepository<TRepo>)))
                    return (IRepository<TRepo>?)p.GetValue(this) ?? default!;
            }

            return default!;
        }

        public IRepositoryBase<object> FindRepository(Type type)
        {
            foreach (var p in GetType().GetProperties())
            {
                var isRepoFromDirectType = p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(IRepository<>) && p.PropertyType.GetGenericTypeDefinition() == type;
                var isRepoFromIndirectType = p.PropertyType.GetInterfaces().Any(f => f.IsGenericType && f.GetGenericTypeDefinition() == typeof(IRepository<>) && f.GetGenericTypeDefinition() == type);

                if (isRepoFromDirectType || isRepoFromIndirectType)
                    return (IRepositoryBase<object>?)p.GetValue(this) ?? default!;
            }

            return default!;
        }

        public void Dispose()
        {
            _transaction?.Dispose();
        }
    }
}
