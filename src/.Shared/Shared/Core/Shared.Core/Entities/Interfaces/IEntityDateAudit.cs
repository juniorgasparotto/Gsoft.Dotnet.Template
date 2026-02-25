using System;

namespace Shared.Core.Entities.Interfaces
{
    public interface IEntityDateAudit
    {
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
    }
}
