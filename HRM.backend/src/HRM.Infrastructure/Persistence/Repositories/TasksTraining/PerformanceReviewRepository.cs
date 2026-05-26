using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TasksTraining
{
    public class PerformanceReviewRepository : BaseRepository<PerformanceReview>, IPerformanceReviewRepository
    {
        public PerformanceReviewRepository(MyDbContext context) : base(context) { }

        public async Task<PerformanceReview?> GetByEmployeePeriodAsync(int employeeId, string period, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.EmployeeId == employeeId && r.Period == period, ct);
        }

        public async Task<List<PerformanceReview>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(r => r.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(r => r.Department)
                .Include(r => r.Details)
                .Where(r => r.EmployeeId == employeeId)
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<PerformanceReview>> GetByDeptPeriodAsync(int deptId, string period, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(r => r.Employee)
                .Include(r => r.Details)
                .Where(r => r.DeptId == deptId && r.Period == period)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<PerformanceReview>> GetPendingEvaluationAsync(int deptId, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(r => r.Employee)
                .Include(r => r.Details)
                .Where(r => r.DeptId == deptId && r.Status == ReviewStatus.PendingEvaluation)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<PerformanceReview>> FetchSlaViolationsAsync(DateTime now, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(r => r.Employee)
                .Where(r => r.ReviewDeadline.HasValue &&
                            r.ReviewDeadline.Value < now &&
                            (r.Status == ReviewStatus.PendingEmployeeUpdate || r.Status == ReviewStatus.PendingEvaluation))
                .ToListAsync(ct);
        }

        public async Task<List<PerformanceReview>> GetByStatusAsync(ReviewStatus status, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(r => r.Employee)
                .Include(r => r.Details)
                .Where(r => r.Status == status)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<PerformanceReview?> GetDetailAsync(int id, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(r => r.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(r => r.Employee)
                    .ThenInclude(e => e!.Position)
                .Include(r => r.Department)
                .Include(r => r.Details)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id, ct);
        }

        public async Task<PerformanceReview?> GetDetailTrackedAsync(int id, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(r => r.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(r => r.Department)
                .Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.Id == id, ct);
        }
    }
}
