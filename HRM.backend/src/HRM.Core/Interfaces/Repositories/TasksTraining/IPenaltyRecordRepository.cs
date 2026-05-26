using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining
{
    public interface IPenaltyRecordRepository : IBaseRepository<PenaltyRecord>
    {
        Task<List<PenaltyRecord>> GetByEmployeePeriodAsync(int employeeId, string period, CancellationToken ct = default);
        Task<List<PenaltyRecord>> GetByReviewAsync(int reviewId, CancellationToken ct = default);
        Task<bool> ExistsForReferenceAsync(PenaltySourceType sourceType, int referenceId, string ruleCode, CancellationToken ct = default);
    }
}
