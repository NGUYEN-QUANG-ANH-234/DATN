using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance
{
    public interface IAttendanceRepository : IBaseRepository<AttendanceLog>
    {
        Task InsertLogAsync(AttendanceLog log);
        Task<IEnumerable<AttendanceLog>> FetchLogsAsync(DateTime startDate, DateTime endDate);
        Task BulkUpdateLogStatusesAsync(IEnumerable<AttendanceLog> logs);
        Task SyncLeaveToAttendanceAsync(int empId, List<DateTime> dates, AttendanceStatus status);
    }
}
