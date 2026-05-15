using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.System
{
    public class RoleRepository : BaseRepository<Role>, IRoleRepository
    {
        public RoleRepository(MyDbContext context) : base(context) { }

        public async Task<IEnumerable<Role>> GetAllRolesAsync(CancellationToken ct = default)
        {
            // Trả về danh sách Role đang có trong Database
            return await _dbSet.AsNoTracking().ToListAsync(ct);
        }
    }
}
