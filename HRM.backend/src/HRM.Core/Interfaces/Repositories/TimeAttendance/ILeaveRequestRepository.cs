using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance
{
    public interface ILeaveRequestRepository : IBaseRepository<LeaveRequest>
    {
        Task AddRequestAsync(LeaveRequest request);
        Task UpdateRequestStatusAsync(int id, LeaveRequestStatus status, DateTime? deadline = null);
        Task<IEnumerable<LeaveRequest>> FetchExpiredRequestsAsync();
    }
}
