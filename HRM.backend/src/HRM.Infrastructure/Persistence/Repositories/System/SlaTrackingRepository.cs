using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.System
{
    public class SlaTrackingRepository : BaseRepository<SlaTrackingTask>, ISlaTrackingRepository
    {
        public SlaTrackingRepository(MyDbContext context) : base(context) { }

        public async Task<SlaTrackingTask?> GetPendingTaskAsync(SlaModuleType module, int referenceId, CancellationToken ct = default)
        {
            return await _dbSet.FirstOrDefaultAsync(t =>
                t.ModuleType == module &&
                t.ReferenceId == referenceId &&
                t.Status == SlaTaskStatus.Pending, ct);
        }

        public async Task<List<SlaTrackingTask>> GetViolatedTasksAsync(DateTime currentTime, CancellationToken ct = default)
        {
            return await _dbSet.Where(t =>
                t.Status == SlaTaskStatus.Pending &&
                t.Deadline < currentTime).ToListAsync(ct);
        }
    }
}
