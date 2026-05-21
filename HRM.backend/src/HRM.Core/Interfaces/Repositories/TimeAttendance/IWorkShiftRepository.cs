using HRM.backend.src.HRM.Core.Entities.TimeAttendance;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance
{
    public interface IWorkShiftRepository : IBaseRepository<WorkShift>
    {
        Task<WorkShift?> GetByNameAsync(string shiftName, CancellationToken ct = default);
        Task<WorkShift?> GetByDeptIdAsync(int deptId, CancellationToken ct = default);
        Task<List<WorkShift>> GetAllActiveWithDepartmentAsync(CancellationToken ct = default);
    }
}
