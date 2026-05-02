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

        public async Task<IEnumerable<AttendanceLog>> FetchLogsAsync(DateTime startDate, DateTime endDate)
        {
            // Lọc log quẹt thẻ theo ngày CheckIn
            return await _dbSet
                .Where(l => l.CheckIn >= startDate && l.CheckIn <= endDate)
                .ToListAsync();
        }

        public async Task BulkUpdateLogStatusesAsync(IEnumerable<AttendanceLog> logs)
        {
            _dbSet.UpdateRange(logs);
            await Task.CompletedTask;
        }

        public async Task SyncLeaveToAttendanceAsync(int empId, List<DateTime> dates, AttendanceStatus status)
        {
            var logs = dates.Select(date => new AttendanceLog
            {
                EmployeeId = empId,
                CheckIn = date, // Dùng CheckIn làm mốc ngày nghỉ
                Status = status
            });
            await _dbSet.AddRangeAsync(logs);
        }
    }
}
