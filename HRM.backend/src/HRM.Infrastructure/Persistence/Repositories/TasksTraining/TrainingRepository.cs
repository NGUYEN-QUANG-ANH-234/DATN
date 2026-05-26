using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TasksTraining
{
    public class TrainingRepository : BaseRepository<Training>, ITrainingRepository
    {
        public TrainingRepository(MyDbContext context) : base(context) { }

        public async Task<List<Training>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default)
        {
            return await BaseQuery()
                .Where(t => t.EmployeeId == employeeId)
                .OrderByDescending(t => t.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<Training>> GetPendingEvaluationByManagerAsync(int managerId, CancellationToken ct = default)
        {
            return await BaseQuery()
                .Where(t => t.ManagerId == managerId &&
                            (t.Status == TrainingStatus.PendingEvaluation || t.Status == TrainingStatus.InProgress))
                .OrderBy(t => t.EvaluationDeadline)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<Training>> FetchOverdueEvaluationsAsync(DateTime now, CancellationToken ct = default)
        {
            return await BaseQuery()
                .Where(t => t.EvaluationDeadline.HasValue &&
                            t.EvaluationDeadline.Value < now &&
                            (t.Status == TrainingStatus.PendingEvaluation || t.Status == TrainingStatus.InProgress))
                .ToListAsync(ct);
        }

        public async Task<List<Training>> GetByStatusAsync(TrainingStatus status, CancellationToken ct = default)
        {
            return await BaseQuery()
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        private IQueryable<Training> BaseQuery()
        {
            return _dbSet
                .Include(t => t.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(t => t.Employee)
                    .ThenInclude(e => e!.Position)
                .Include(t => t.Manager)
                .Include(t => t.Department)
                .Include(t => t.Tasks);
        }
    }
}
