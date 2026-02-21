namespace Shared.Core.Repositories
{
    public interface IUnitOfWork
    {
        Task<IDisposable> BeginTransactionAsync();
        Task CommitAsync();
        Task Rollback();
        Task SaveAsync();

        IRepository<TRepo> FindRepository<TRepo>();
        IRepositoryBase<object> FindRepository(Type type);
    }
}
