using HRM.backend.src.HRM.Core.Entities.System;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.System
{
    public interface IRoleRepository : IBaseRepository<Role>
    {
        Task<IEnumerable<Role>> GetAllRolesAsync(CancellationToken ct = default);
    }
}
