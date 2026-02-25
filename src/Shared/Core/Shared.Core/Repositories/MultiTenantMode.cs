namespace Shared.Core.Repositories;

public enum MultiTenantMode
{
    None = 0,
    AsOwner = 1,
    AsGuest = 2,
    AsGuestAndOwner = 3
}
