using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.EmployeeProfile
{
    public class DependentUpdateRequestRepository : BaseRepository<DependentUpdateRequest>, IDependentUpdateRequestRepository
    {
        public DependentUpdateRequestRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<DependentUpdateRequest?> GetByIdForUpdateAsync(int id, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task<List<DependentUpdateRequest>> GetPendingByStatusesAsync(IEnumerable<RequestStatus> statuses, CancellationToken ct = default)
        {
            var statusList = statuses.ToList();
            return await _dbSet
                .Include(x => x.Employee)
                .Where(x => statusList.Contains(x.Status))
                .OrderBy(x => x.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<DependentUpdateRequest>> GetPendingForEmployeeAsync(int employeeId, int? dependentId, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(x =>
                    x.EmployeeId == employeeId &&
                    x.DependentId == dependentId &&
                    (x.Status == RequestStatus.Pending_HR || x.Status == RequestStatus.Pending_Director))
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}
