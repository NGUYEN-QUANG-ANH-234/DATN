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
                .Where(l => l.EmployeeId == employeeId && l.WorkDate >= start && l.WorkDate < end)
                .OrderByDescending(l => l.CheckIn)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<AttendanceLog?> GetOpenLogAsync(int employeeId, DateTime now, int maxOpenHours, CancellationToken ct = default)
        {
            var minCheckIn = now.AddHours(-Math.Max(1, maxOpenHours));

            return await _dbSet
                .Include(l => l.WorkShift)
                .Where(l => l.EmployeeId == employeeId &&
                            l.CheckOut == null &&
                            l.CheckIn != null &&
                            l.CheckIn >= minCheckIn &&
                            l.CheckIn <= now)
                .OrderByDescending(l => l.CheckIn)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IEnumerable<AttendanceLog>> FetchLogsAsync(DateTime startDate, DateTime endDate)
        {
            // Lọc log quẹt thẻ theo ngày CheckIn
            return await _dbSet
                .Where(l => l.WorkDate >= startDate.Date && l.WorkDate <= endDate.Date)
                .ToListAsync();
        }

        public async Task<List<AttendanceLog>> FetchLogsByPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(l => l.Employee)
                .ThenInclude(e => e!.Department)
                .Include(l => l.WorkShift)
                .Where(l => l.EmployeeId.HasValue &&
                            l.WorkDate >= startDate.Date &&
                            l.WorkDate < endDate.Date)
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
                            l.WorkDate >= start &&
                            l.WorkDate < end)
                .ToListAsync();

            foreach (var log in existingLogs.Where(l => normalizedDates.Contains(l.WorkDate.Date)))
            {
                log.Status = status;
            }

            var existingDates = existingLogs
                .Select(l => l.WorkDate.Date)
                .ToHashSet();

            var logs = normalizedDates
                .Where(date => !existingDates.Contains(date))
                .Select(date => new AttendanceLog
            {
                EmployeeId = empId,
                WorkDate = date,
                CheckIn = date, // Dùng CheckIn làm mốc ngày nghỉ
                Status = status
            });

            await _dbSet.AddRangeAsync(logs);
        }
    }
}
