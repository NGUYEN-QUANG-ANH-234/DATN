using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TimeAttendance
{
    public class OvertimeRequestRepository : BaseRepository<OvertimeRequest>, IOvertimeRequestRepository
    {
        public OvertimeRequestRepository(MyDbContext context) : base(context) { }

        public async Task<List<OvertimeRequest>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default)
        {
            return await BaseQuery()
                .Where(x => x.EmployeeId == employeeId)
                .OrderByDescending(x => x.WorkDate)
                .ThenByDescending(x => x.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<OvertimeRequest>> GetByStatusAsync(OvertimeRequestStatus status, CancellationToken ct = default)
        {
            return await BaseQuery()
                .Where(x => x.Status == status)
                .OrderBy(x => x.WorkDate)
                .ThenBy(x => x.StartTime)
                .ToListAsync(ct);
        }

        public async Task<List<OvertimeRequest>> GetPendingManagerByDeptAsync(int deptId, CancellationToken ct = default)
        {
            return await BaseQuery()
                .Where(x => x.Status == OvertimeRequestStatus.PendingManager &&
                            x.Employee.DeptId == deptId)
                .OrderBy(x => x.WorkDate)
                .ThenBy(x => x.StartTime)
                .ToListAsync(ct);
        }

        public async Task<List<OvertimeRequest>> GetApprovedAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
        {
            var query = BaseQuery().Where(x => x.Status == OvertimeRequestStatus.Approved);
            if (fromDate.HasValue)
                query = query.Where(x => x.WorkDate >= fromDate.Value.Date);
            if (toDate.HasValue)
                query = query.Where(x => x.WorkDate < toDate.Value.Date.AddDays(1));

            return await query
                .OrderByDescending(x => x.WorkDate)
                .ThenBy(x => x.Employee.FullName)
                .ToListAsync(ct);
        }

        public async Task<List<OvertimeRequest>> GetApprovedByPeriodAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
        {
            return await BaseQuery()
                .Where(x => x.Status == OvertimeRequestStatus.Approved &&
                            x.WorkDate >= fromDate.Date &&
                            x.WorkDate < toDate.Date)
                .ToListAsync(ct);
        }

        public async Task<bool> HasOverlappingRequestAsync(int employeeId, DateTime workDate, TimeSpan startTime, TimeSpan endTime, int? excludeId = null, CancellationToken ct = default)
        {
            return await _dbSet.AnyAsync(x =>
                x.EmployeeId == employeeId &&
                x.WorkDate == workDate.Date &&
                x.Status != OvertimeRequestStatus.Rejected &&
                x.Status != OvertimeRequestStatus.Cancelled &&
                (!excludeId.HasValue || x.Id != excludeId.Value) &&
                startTime < x.EndTime &&
                endTime > x.StartTime,
                ct);
        }

        public async Task<OvertimeRequest?> GetDetailAsync(int id, CancellationToken ct = default)
        {
            return await BaseQuery().FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        private IQueryable<OvertimeRequest> BaseQuery()
        {
            return _dbSet
                .Include(x => x.Employee)
                .ThenInclude(e => e.Department);
        }
    }
}
