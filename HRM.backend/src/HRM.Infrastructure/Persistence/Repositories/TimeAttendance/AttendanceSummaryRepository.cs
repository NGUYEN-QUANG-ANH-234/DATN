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
    }
}
