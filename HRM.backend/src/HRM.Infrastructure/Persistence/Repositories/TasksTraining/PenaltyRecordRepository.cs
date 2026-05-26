using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TasksTraining
{
    public class PenaltyRecordRepository : BaseRepository<PenaltyRecord>, IPenaltyRecordRepository
    {
        public PenaltyRecordRepository(MyDbContext context) : base(context) { }

        public async Task<List<PenaltyRecord>> GetByEmployeePeriodAsync(int employeeId, string period, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(r => r.EmployeeId == employeeId && r.Period == period)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<PenaltyRecord>> GetByReviewAsync(int reviewId, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(r => r.PerformanceReviewId == reviewId)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<bool> ExistsForReferenceAsync(PenaltySourceType sourceType, int referenceId, string ruleCode, CancellationToken ct = default)
        {
            return await _dbSet.AnyAsync(r =>
                r.SourceType == sourceType &&
                r.ReferenceId == referenceId &&
                r.RuleCode == ruleCode,
                ct);
        }
    }
}
