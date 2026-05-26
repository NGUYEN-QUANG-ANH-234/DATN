using HRM.backend.src.HRM.Core.Entities.TasksTraining;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining
{
    public interface ITaskFeedbackRepository : IBaseRepository<TaskFeedback>
    {
        Task<List<TaskFeedback>> GetByTaskAsync(int taskId, CancellationToken ct = default);
        Task<List<TaskFeedback>> GetByProgressAsync(int progressId, CancellationToken ct = default);
    }
}
