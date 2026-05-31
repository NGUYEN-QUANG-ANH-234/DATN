using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance
{
    public interface IOvertimeRequestRepository : IBaseRepository<OvertimeRequest>
    {
        Task<List<OvertimeRequest>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default);
        Task<List<OvertimeRequest>> GetByStatusAsync(OvertimeRequestStatus status, CancellationToken ct = default);
        Task<List<OvertimeRequest>> GetPendingManagerByDeptAsync(int deptId, CancellationToken ct = default);
        Task<List<OvertimeRequest>> GetApprovedAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);
        Task<List<OvertimeRequest>> GetApprovedByPeriodAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
        Task<List<OvertimeRequest>> GetReconcileCandidatesAsync(int employeeId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default);
        Task<bool> HasOverlappingRequestAsync(int employeeId, DateTime startAt, DateTime endAt, int? excludeId = null, CancellationToken ct = default);
        Task<OvertimeRequest?> GetDetailAsync(int id, CancellationToken ct = default);
    }
}
