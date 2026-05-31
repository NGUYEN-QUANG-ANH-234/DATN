using HRM.backend.src.HRM.Core.Entities.TimeAttendance;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance
{
    public interface IWorkCalendarConfigRepository : IBaseRepository<WorkCalendarConfig>
    {
        Task<WorkCalendarConfig?> GetByDeptPeriodAsync(int deptId, byte month, short year, CancellationToken ct = default);
        Task<List<WorkCalendarConfig>> GetByPeriodAsync(byte month, short year, CancellationToken ct = default);
        Task<List<WorkCalendarConfig>> GetAllWithDepartmentAsync(CancellationToken ct = default);
    }
}
