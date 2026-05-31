using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile
{
    public interface IDependentUpdateRequestRepository : IBaseRepository<DependentUpdateRequest>
    {
        Task<DependentUpdateRequest?> GetByIdForUpdateAsync(int id, CancellationToken ct = default);
        Task<List<DependentUpdateRequest>> GetPendingByStatusesAsync(IEnumerable<RequestStatus> statuses, CancellationToken ct = default);
        Task<List<DependentUpdateRequest>> GetPendingForEmployeeAsync(int employeeId, int? dependentId, CancellationToken ct = default);
    }
}
