using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance
{
    public interface IAttendanceRepository : IBaseRepository<AttendanceLog>
    {
        Task InsertLogAsync(AttendanceLog log);
        Task<AttendanceLog?> GetTodayLogAsync(int employeeId, DateTime day, CancellationToken ct = default);
        Task<AttendanceLog?> GetOpenLogAsync(int employeeId, DateTime now, int maxOpenHours, CancellationToken ct = default);
        Task<IEnumerable<AttendanceLog>> FetchLogsAsync(DateTime startDate, DateTime endDate);
        Task<List<AttendanceLog>> FetchLogsByPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
        Task BulkUpdateLogStatusesAsync(IEnumerable<AttendanceLog> logs);
        Task SyncLeaveToAttendanceAsync(int empId, List<DateTime> dates, AttendanceStatus status);
    }
}
