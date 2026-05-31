using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TimeAttendance
{
    public class AttendanceSummaryRepository : BaseRepository<AttendanceSummary>, IAttendanceSummaryRepository
    {
        public AttendanceSummaryRepository(MyDbContext context) : base(context) { }

        public async Task<List<AttendanceSummary>> GetByPeriodAsync(byte month, short year, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(x => x.Employee)
                .ThenInclude(e => e.Department)
                .Where(x => x.Month == month && x.Year == year)
                .OrderBy(x => x.Employee.FullName)
                .ToListAsync(ct);
        }

        public async Task<AttendanceSummary?> GetByEmployeePeriodAsync(int employeeId, byte month, short year, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(x => x.Employee)
                .ThenInclude(e => e.Department)
                .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.Month == month && x.Year == year, ct);
        }

        public async Task<List<AttendanceDailySummary>> GetDailyByPeriodAsync(byte month, short year, CancellationToken ct = default)
        {
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1);
            return await _context.AttendanceDailySummaries
                .Include(x => x.Employee)
                .ThenInclude(e => e.Department)
                .Where(x => x.WorkDate >= start && x.WorkDate < end)
                .OrderBy(x => x.WorkDate)
                .ThenBy(x => x.Employee.FullName)
                .ToListAsync(ct);
        }

        public async Task<AttendanceDailySummary?> GetDailyByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.AttendanceDailySummaries
                .Include(x => x.Employee)
                .ThenInclude(e => e.Department)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task<AttendanceDailySummary?> GetDailyByEmployeeDateAsync(int employeeId, DateTime workDate, CancellationToken ct = default)
        {
            var date = workDate.Date;
            return await _context.AttendanceDailySummaries
                .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.WorkDate == date, ct);
        }

        public async Task AddDailyAsync(AttendanceDailySummary summary, CancellationToken ct = default)
        {
            await _context.AttendanceDailySummaries.AddAsync(summary, ct);
        }

        public async Task AddAdjustmentLogAsync(AttendanceAdjustmentLog log, CancellationToken ct = default)
        {
            await _context.AttendanceAdjustmentLogs.AddAsync(log, ct);
        }
    }
}
