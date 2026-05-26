using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;
using Microsoft.EntityFrameworkCore;
using TaskStatus = HRM.backend.src.HRM.Core.Enums.TaskStatus;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TasksTraining
{
    public class TaskRepository : BaseRepository<WorkTask>, ITaskRepository
    {
        public TaskRepository(MyDbContext context) : base(context) { }

        public async Task<List<WorkTask>> GetByAssigneeAsync(int employeeId, CancellationToken ct = default)
        {
            return await BaseQuery()
                .Where(t => t.AssignedTo == employeeId)
                .OrderByDescending(t => t.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<WorkTask>> GetPendingReviewByDeptAsync(int deptId, CancellationToken ct = default)
        {
            return await BaseQuery()
                .Where(t => t.DeptId == deptId && t.Status == TaskStatus.PendingReview)
                .OrderBy(t => t.ReviewDeadline)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<WorkTask>> GetByTrainingAsync(int trainingId, CancellationToken ct = default)
        {
            return await BaseQuery()
                .Where(t => t.TrainingId == trainingId)
                .OrderBy(t => t.Deadline)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<WorkTask>> FetchSlaViolationsAsync(DateTime now, CancellationToken ct = default)
        {
            return await BaseQuery()
                .Where(t =>
                    ((t.Status == TaskStatus.Assigned || t.Status == TaskStatus.InProgress || t.Status == TaskStatus.ReworkRequired) &&
                     t.Deadline.HasValue &&
                     t.Deadline.Value < now) ||
                    (t.Status == TaskStatus.PendingReview &&
                     t.ReviewDeadline.HasValue &&
                     t.ReviewDeadline.Value < now))
                .ToListAsync(ct);
        }

        public async Task<List<WorkTask>> GetByStatusAsync(TaskStatus status, CancellationToken ct = default)
        {
            return await BaseQuery()
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        private IQueryable<WorkTask> BaseQuery()
        {
            return _dbSet
                .Include(t => t.Assignee)
                    .ThenInclude(e => e!.Department)
                .Include(t => t.Department)
                    .ThenInclude(d => d!.Manager)
                .Include(t => t.Progresses)
                .Include(t => t.Feedbacks)
                .Include(t => t.Training);
        }
    }
}
