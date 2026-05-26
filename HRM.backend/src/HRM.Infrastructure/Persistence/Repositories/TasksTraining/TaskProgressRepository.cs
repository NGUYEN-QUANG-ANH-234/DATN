using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TasksTraining
{
    public class TaskProgressRepository : BaseRepository<TaskProgress>, ITaskProgressRepository
    {
        public TaskProgressRepository(MyDbContext context) : base(context) { }

        public async Task<List<TaskProgress>> GetByTaskAsync(int taskId, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(p => p.TaskId == taskId)
                .OrderByDescending(p => p.SubmittedAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<TaskProgress?> GetLatestByTaskAsync(int taskId, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(p => p.TaskId == taskId)
                .OrderByDescending(p => p.SubmittedAt)
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);
        }
    }
}
