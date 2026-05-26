using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining
{
    public interface IPerformanceReviewRepository : IBaseRepository<PerformanceReview>
    {
        Task<PerformanceReview?> GetByEmployeePeriodAsync(int employeeId, string period, CancellationToken ct = default);
        Task<List<PerformanceReview>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default);
        Task<List<PerformanceReview>> GetByDeptPeriodAsync(int deptId, string period, CancellationToken ct = default);
        Task<List<PerformanceReview>> GetPendingEvaluationAsync(int deptId, CancellationToken ct = default);
        Task<List<PerformanceReview>> FetchSlaViolationsAsync(DateTime now, CancellationToken ct = default);
        Task<List<PerformanceReview>> GetByStatusAsync(ReviewStatus status, CancellationToken ct = default);
        Task<PerformanceReview?> GetDetailAsync(int id, CancellationToken ct = default);
        Task<PerformanceReview?> GetDetailTrackedAsync(int id, CancellationToken ct = default);
    }
}
