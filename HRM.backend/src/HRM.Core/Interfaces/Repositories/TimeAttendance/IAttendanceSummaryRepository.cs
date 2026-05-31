using HRM.backend.src.HRM.Core.Entities.TimeAttendance;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance
{
    public interface IAttendanceSummaryRepository : IBaseRepository<AttendanceSummary>
    {
        Task<List<AttendanceSummary>> GetByPeriodAsync(byte month, short year, CancellationToken ct = default);
        Task<AttendanceSummary?> GetByEmployeePeriodAsync(int employeeId, byte month, short year, CancellationToken ct = default);
        Task<List<AttendanceDailySummary>> GetDailyByPeriodAsync(byte month, short year, CancellationToken ct = default);
        Task<AttendanceDailySummary?> GetDailyByIdAsync(int id, CancellationToken ct = default);
        Task<AttendanceDailySummary?> GetDailyByEmployeeDateAsync(int employeeId, DateTime workDate, CancellationToken ct = default);
        Task AddDailyAsync(AttendanceDailySummary summary, CancellationToken ct = default);
        Task AddAdjustmentLogAsync(AttendanceAdjustmentLog log, CancellationToken ct = default);
    }
}
