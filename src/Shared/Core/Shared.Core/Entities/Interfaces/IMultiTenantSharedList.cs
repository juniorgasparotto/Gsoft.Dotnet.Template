namespace Shared.Core.Entities.Interfaces;

public interface IMultiTenantSharedList : IMultiTenant, IEntityId
{
    IEnumerable<IMultiTenantShared> GetMultiTenantShareds();
    void SetMultiTenantShareds(IEnumerable<IMultiTenantShared> newUsers);
}

public interface IMultiTenantSharedList<TNxN> : IMultiTenantSharedList where TNxN : IMultiTenantShared
{
    List<TNxN> Users { get; set; }
}
