using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TimeAttendance
{
    public class WorkCalendarConfigRepository : BaseRepository<WorkCalendarConfig>, IWorkCalendarConfigRepository
    {
        public WorkCalendarConfigRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<WorkCalendarConfig?> GetByDeptPeriodAsync(int deptId, byte month, short year, CancellationToken ct = default)
        {
            return await _dbSet.FirstOrDefaultAsync(x =>
                x.DeptId == deptId &&
                x.Month == month &&
                x.Year == year, ct);
        }

        public async Task<List<WorkCalendarConfig>> GetByPeriodAsync(byte month, short year, CancellationToken ct = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(x => x.Month == month && x.Year == year)
                .ToListAsync(ct);
        }

        public async Task<List<WorkCalendarConfig>> GetAllWithDepartmentAsync(CancellationToken ct = default)
        {
            return await _dbSet
                .Include(x => x.Department)
                .AsNoTracking()
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToListAsync(ct);
        }
    }
}
