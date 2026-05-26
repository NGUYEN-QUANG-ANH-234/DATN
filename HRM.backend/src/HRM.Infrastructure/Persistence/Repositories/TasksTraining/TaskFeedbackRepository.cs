using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TasksTraining
{
    public class TaskFeedbackRepository : BaseRepository<TaskFeedback>, ITaskFeedbackRepository
    {
        public TaskFeedbackRepository(MyDbContext context) : base(context) { }

        public async Task<List<TaskFeedback>> GetByTaskAsync(int taskId, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(f => f.Reviewer)
                .Where(f => f.TaskId == taskId)
                .OrderByDescending(f => f.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<TaskFeedback>> GetByProgressAsync(int progressId, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(f => f.Reviewer)
                .Where(f => f.ProgressId == progressId)
                .OrderByDescending(f => f.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}
