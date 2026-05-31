using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining
{
    public interface IPenaltyRecordRepository : IBaseRepository<PenaltyRecord>
    {
        Task<List<PenaltyRecord>> GetByEmployeePeriodAsync(int employeeId, string period, CancellationToken ct = default);
        Task<List<PenaltyRecord>> GetByReviewAsync(int reviewId, CancellationToken ct = default);
        Task<List<PenaltyRecord>> GetApprovedPerformanceByEmployeePeriodAsync(int employeeId, string period, CancellationToken ct = default);
        Task<List<PenaltyRecord>> GetPendingReviewAsync(PenaltyRecordStatus status, CancellationToken ct = default);
        Task<List<PenaltyRecord>> GetPersonnelHistoryByEmployeeAsync(int employeeId, CancellationToken ct = default);
        Task<bool> ExistsForReferenceAsync(PenaltySourceType sourceType, int referenceId, string ruleCode, CancellationToken ct = default);
        Task<bool> ExistsForEmployeePeriodRuleAsync(int employeeId, string period, PenaltySourceType sourceType, string ruleCode, CancellationToken ct = default);
    }
}
