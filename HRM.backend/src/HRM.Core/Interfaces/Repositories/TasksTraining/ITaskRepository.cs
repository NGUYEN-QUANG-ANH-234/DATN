using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using TaskStatus = HRM.backend.src.HRM.Core.Enums.TaskStatus;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining
{
    public interface ITaskRepository : IBaseRepository<WorkTask>
    {
        Task<List<WorkTask>> GetByAssigneeAsync(int employeeId, CancellationToken ct = default);
        Task<List<WorkTask>> GetPendingReviewByDeptAsync(int deptId, CancellationToken ct = default);
        Task<List<WorkTask>> GetByTrainingAsync(int trainingId, CancellationToken ct = default);
        Task<List<WorkTask>> FetchSlaViolationsAsync(DateTime now, CancellationToken ct = default);
        Task<List<WorkTask>> GetByStatusAsync(TaskStatus status, CancellationToken ct = default);
    }
}
