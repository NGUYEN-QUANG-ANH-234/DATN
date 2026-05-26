using HRM.backend.src.HRM.Core.Entities.TasksTraining;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining
{
    public interface ITaskProgressRepository : IBaseRepository<TaskProgress>
    {
        Task<List<TaskProgress>> GetByTaskAsync(int taskId, CancellationToken ct = default);
        Task<TaskProgress?> GetLatestByTaskAsync(int taskId, CancellationToken ct = default);
    }
}
