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

        public async Task<List<PenaltyRecord>> GetApprovedPerformanceByEmployeePeriodAsync(int employeeId, string period, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(r =>
                    r.EmployeeId == employeeId &&
                    r.Period == period &&
                    r.AffectsPerformance &&
                    (r.Status == PenaltyRecordStatus.Approved || r.Status == PenaltyRecordStatus.Applied))
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<PenaltyRecord>> GetPendingReviewAsync(PenaltyRecordStatus status, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(r => r.Status == status)
                .OrderBy(r => r.OccurredAt ?? r.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<PenaltyRecord>> GetPersonnelHistoryByEmployeeAsync(int employeeId, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(r => r.EmployeeId == employeeId && r.AffectsPersonnelDecision)
                .OrderByDescending(r => r.OccurredAt ?? r.CreatedAt)
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

        public async Task<bool> ExistsForEmployeePeriodRuleAsync(int employeeId, string period, PenaltySourceType sourceType, string ruleCode, CancellationToken ct = default)
        {
            return await _dbSet.AnyAsync(r =>
                r.EmployeeId == employeeId &&
                r.Period == period &&
                r.SourceType == sourceType &&
                r.RuleCode == ruleCode,
                ct);
        }
    }
}
