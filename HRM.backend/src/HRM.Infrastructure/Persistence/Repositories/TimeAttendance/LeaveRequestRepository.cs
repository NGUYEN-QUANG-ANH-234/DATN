using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TimeAttendance
{
    public class LeaveRequestRepository : BaseRepository<LeaveRequest>, ILeaveRequestRepository
    {
        public LeaveRequestRepository(MyDbContext context) : base(context) { }

        public async Task AddRequestAsync(LeaveRequest request) => await _dbSet.AddAsync(request);

        public async Task UpdateRequestStatusAsync(int id, LeaveRequestStatus status, DateTime? deadline = null)
        {
            var request = await _dbSet.FindAsync(id);
            if (request != null)
            {
                request.Status = status;
                if (deadline.HasValue) request.DeadlineAt = deadline.Value;
            }
        }

        public async Task<IEnumerable<LeaveRequest>> FetchExpiredRequestsAsync()
        {
            return await _dbSet
                .Include(r => r.Employee)
                .Include(r => r.LeaveType)
                .Where(r => (r.Status == LeaveRequestStatus.PendingDept ||
                             r.Status == LeaveRequestStatus.PendingDirector) &&
                            r.DeadlineAt < DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<LeaveRequest?> GetDetailAsync(int id, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(r => r.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(r => r.Employee)
                    .ThenInclude(e => e!.Account)
                .Include(r => r.LeaveType)
                .FirstOrDefaultAsync(r => r.Id == id, ct);
        }

        public async Task<List<LeaveRequest>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
                .Include(r => r.LeaveType)
                .Where(r => r.EmployeeId == employeeId)
                .OrderByDescending(r => r.StartDate)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<LeaveRequest>> GetPendingDeptAsync(int? deptId, CancellationToken ct = default)
        {
            var query = _dbSet
                .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
                .Include(r => r.LeaveType)
                .Where(r => r.Status == LeaveRequestStatus.PendingDept);

            if (deptId.HasValue)
                query = query.Where(r => r.Employee != null && r.Employee.DeptId == deptId.Value);

            return await query
                .OrderBy(r => r.DeadlineAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<LeaveRequest>> GetByStatusAsync(LeaveRequestStatus status, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
                .Include(r => r.LeaveType)
                .Where(r => r.Status == status)
                .OrderBy(r => r.DeadlineAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<LeaveRequest>> GetApprovedByPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            var start = startDate.Date;
            var end = endDate.Date;

            return await _dbSet
                .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
                .Include(r => r.LeaveType)
                .Where(r => r.EmployeeId.HasValue &&
                            r.StartDate.HasValue &&
                            r.EndDate.HasValue &&
                            r.StartDate.Value < end &&
                            r.EndDate.Value >= start &&
                            (r.Status == LeaveRequestStatus.Approved ||
                             r.Status == LeaveRequestStatus.Auto_Approved ||
                             r.Status == LeaveRequestStatus.AutoFinalApproved))
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<LeaveRequest>> GetApprovedForPayrollLockByPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            var start = startDate.Date;
            var end = endDate.Date;

            return await _dbSet
                .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
                .Include(r => r.LeaveType)
                .Where(r => r.EmployeeId.HasValue &&
                            r.StartDate.HasValue &&
                            r.EndDate.HasValue &&
                            r.StartDate.Value < end &&
                            r.EndDate.Value >= start &&
                            !r.IsPayrollLocked &&
                            (r.Status == LeaveRequestStatus.Approved ||
                             r.Status == LeaveRequestStatus.Auto_Approved ||
                             r.Status == LeaveRequestStatus.AutoFinalApproved))
                .ToListAsync(ct);
        }
    }
}
