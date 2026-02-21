namespace Shared.Core.Entities.Interfaces;

public interface IMultiTenantShared : IMultiTenant, IEntityDateAudit
{
    Guid EntityId { get; set; }
    User User { get; set; }
    bool SendByOwner { get; set; }
    bool Accept { get; set; }
}