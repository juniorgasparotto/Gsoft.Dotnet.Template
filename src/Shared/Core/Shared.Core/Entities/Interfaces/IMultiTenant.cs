using System;

namespace Shared.Core.Entities.Interfaces
{
    public interface IMultiTenant
    {
        Guid UserId { get; set; }

        public bool IsOwner(Guid loggedUserId)
        {
            return UserId == loggedUserId;                
        }
    }
}
