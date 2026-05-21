using HRM.backend.src.HRM.Core.Models.History;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile
{
    public interface IHistoryTrackingRepository
    {
        Task<PagedResult<ConsolidatedHistoryRecord>> GetPagedConsolidatedHistoryAsync(
            int employeeId,
            HistoryFilterCriteria filter,
            CancellationToken ct = default);
    }
}
