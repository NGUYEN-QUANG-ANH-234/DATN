using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.EmployeeProfile
{
    public class DependentRepository : BaseRepository<Dependent>, IDependentRepository
    {
        public DependentRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<List<Dependent>> GetByEmployeeIdAsync(int employeeId, bool includeInactive = false, CancellationToken ct = default)
        {
            var query = _dbSet.AsNoTracking().Where(x => x.EmployeeId == employeeId);
            if (!includeInactive)
                query = query.Where(x => x.IsActive);

            return await query
                .OrderBy(x => x.FullName)
                .ToListAsync(ct);
        }

        public async Task<Dependent?> GetByIdForEmployeeAsync(int id, int employeeId, CancellationToken ct = default)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.Id == id && x.EmployeeId == employeeId, ct);
        }
    }
}
