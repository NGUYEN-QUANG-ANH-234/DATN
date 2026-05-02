using HRM.backend.src.HRM.Core.Entities.TimeAttendance;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance
{
    public interface IShiftRepository : IBaseRepository<WorkShift>
    {
        Task AddOrUpdateShiftAsync(WorkShift shift);
        Task<IEnumerable<WorkShift>> FetchShiftDetailsAsync();
    }
}
