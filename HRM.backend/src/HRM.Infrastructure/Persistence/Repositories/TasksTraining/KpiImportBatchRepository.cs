using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TasksTraining
{
    public class KpiImportBatchRepository : BaseRepository<KpiImportBatch>, IKpiImportBatchRepository
    {
        public KpiImportBatchRepository(MyDbContext context) : base(context) { }

        public async Task<List<KpiImportBatch>> GetByDeptPeriodAsync(int deptId, string period, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(b => b.ImportedByAccount)
                .Where(b => b.DeptId == deptId && b.Period == period)
                .OrderByDescending(b => b.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}
