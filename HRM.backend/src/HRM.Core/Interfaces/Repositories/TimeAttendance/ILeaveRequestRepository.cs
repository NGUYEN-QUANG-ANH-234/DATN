using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance
{
    public interface ILeaveRequestRepository : IBaseRepository<LeaveRequest>
    {
        Task AddRequestAsync(LeaveRequest request);
        Task UpdateRequestStatusAsync(int id, LeaveRequestStatus status, DateTime? deadline = null);
        Task<LeaveRequest?> GetDetailAsync(int id, CancellationToken ct = default);
        Task<List<LeaveRequest>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default);
        Task<List<LeaveRequest>> GetPendingDeptAsync(int? deptId, CancellationToken ct = default);
        Task<List<LeaveRequest>> GetByStatusAsync(LeaveRequestStatus status, CancellationToken ct = default);
        Task<IEnumerable<LeaveRequest>> FetchExpiredRequestsAsync();
    }
}
