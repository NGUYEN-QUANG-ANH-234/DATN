using HRM.backend.src.HRM.Core.Entities.TimeAttendance;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance
{
    public interface IAttendanceSummaryRepository : IBaseRepository<AttendanceSummary>
    {
        Task<List<AttendanceSummary>> GetByPeriodAsync(byte month, short year, CancellationToken ct = default);
        Task<AttendanceSummary?> GetByEmployeePeriodAsync(int employeeId, byte month, short year, CancellationToken ct = default);
    }
}
