using HRM.backend.src.HRM.Application.DTOs.Organization;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance
{
    public interface ILeaveBalanceRepository : IBaseRepository<LeaveBalance>
    {
        Task<LeaveBalance?> GetBalanceAsync(int employeeId, int leaveTypeId, short year, CancellationToken ct = default);
        Task DeductAsync(int employeeId, int leaveTypeId, short year, decimal days, CancellationToken ct = default);
        Task UpdateDeptAllocatedDaysAsync(int deptId, int leaveTypeId, short year, decimal totalDays, CancellationToken ct = default);
        Task<List<DeptLeaveConfigDto>> GetDeptLeaveConfigsAsync(CancellationToken ct = default);
    }
}
