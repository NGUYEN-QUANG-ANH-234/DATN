using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining
{
    public interface ITrainingRepository : IBaseRepository<Training>
    {
        Task<List<Training>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default);
        Task<List<Training>> GetPendingEvaluationByManagerAsync(int managerId, CancellationToken ct = default);
        Task<List<Training>> FetchOverdueEvaluationsAsync(DateTime now, CancellationToken ct = default);
        Task<List<Training>> GetByStatusAsync(TrainingStatus status, CancellationToken ct = default);
    }
}
