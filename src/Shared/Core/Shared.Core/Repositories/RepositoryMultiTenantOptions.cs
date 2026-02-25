namespace Shared.Core.Repositories;

public class RepositoryMultiTenantAsGuestOptions : RepositoryOptions
{
    public Guid[] UserIds { get; }
    public MultiTenantMode Mode { get; }

    public RepositoryMultiTenantAsGuestOptions(IEnumerable<Guid> userIds)
    {
        UserIds = userIds.ToArray();
    }
}

public class RepositoryMultiTenantAsOwnerOptions : RepositoryOptions
{
    public Guid[] UserIds { get; }

    public RepositoryMultiTenantAsOwnerOptions(IEnumerable<Guid> userIds)
    {
        UserIds = userIds.ToArray();
    }
}

public class RepositoryMultiTenantAsGuestAndOwnerOptions : RepositoryOptions
{
    public Guid[] UserIds { get; }

    public RepositoryMultiTenantAsGuestAndOwnerOptions(IEnumerable<Guid> userIds)
    {
        UserIds = userIds.ToArray();
    }
}
