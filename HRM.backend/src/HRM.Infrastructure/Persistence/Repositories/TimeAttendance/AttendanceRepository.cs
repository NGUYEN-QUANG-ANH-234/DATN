using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TimeAttendance
{
    public class AttendanceRepository : BaseRepository<AttendanceLog>, IAttendanceRepository
    {
        public AttendanceRepository(MyDbContext context) : base(context) { }

        public async Task InsertLogAsync(AttendanceLog log)
        {
            await _dbSet.AddAsync(log);
        }

        public async Task<AttendanceLog?> GetTodayLogAsync(int employeeId, DateTime day, CancellationToken ct = default)
        {
            var start = day.Date;
            var end = start.AddDays(1);

            return await _dbSet
                .Where(l => l.EmployeeId == employeeId && l.CheckIn >= start && l.CheckIn < end)
                .OrderByDescending(l => l.CheckIn)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IEnumerable<AttendanceLog>> FetchLogsAsync(DateTime startDate, DateTime endDate)
        {
            // Lọc log quẹt thẻ theo ngày CheckIn
            return await _dbSet
                .Where(l => l.CheckIn >= startDate && l.CheckIn <= endDate)
                .ToListAsync();
        }

        public async Task<List<AttendanceLog>> FetchLogsByPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(l => l.Employee)
                .ThenInclude(e => e!.Department)
                .Include(l => l.WorkShift)
                .Where(l => l.EmployeeId.HasValue &&
                            l.CheckIn >= startDate &&
                            l.CheckIn < endDate)
                .ToListAsync(ct);
        }

        public async Task BulkUpdateLogStatusesAsync(IEnumerable<AttendanceLog> logs)
        {
            _dbSet.UpdateRange(logs);
            await Task.CompletedTask;
        }

        public async Task SyncLeaveToAttendanceAsync(int empId, List<DateTime> dates, AttendanceStatus status)
        {
            var normalizedDates = dates.Select(d => d.Date).Distinct().ToList();
            var end = normalizedDates.Count == 0 ? DateTime.MinValue : normalizedDates.Max().AddDays(1);
            var start = normalizedDates.Count == 0 ? DateTime.MinValue : normalizedDates.Min();

            var existingLogs = await _dbSet
                .Where(l => l.EmployeeId == empId &&
                            l.CheckIn.HasValue &&
                            l.CheckIn >= start &&
                            l.CheckIn < end)
                .ToListAsync();

            foreach (var log in existingLogs.Where(l => normalizedDates.Contains(l.CheckIn!.Value.Date)))
            {
                log.Status = status;
            }

            var existingDates = existingLogs
                .Where(l => l.CheckIn.HasValue)
                .Select(l => l.CheckIn!.Value.Date)
                .ToHashSet();

            var logs = normalizedDates
                .Where(date => !existingDates.Contains(date))
                .Select(date => new AttendanceLog
            {
                EmployeeId = empId,
                CheckIn = date, // Dùng CheckIn làm mốc ngày nghỉ
                Status = status
            });

            await _dbSet.AddRangeAsync(logs);
        }
    }
}
