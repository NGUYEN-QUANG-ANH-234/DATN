using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TimeAttendance
{
    public class WorkShiftRepository : BaseRepository<WorkShift>, IWorkShiftRepository
    {
        public WorkShiftRepository(MyDbContext context) : base(context) { }

        public async Task<WorkShift?> GetByNameAsync(string shiftName, CancellationToken ct = default)
        {
            return await _dbSet.FirstOrDefaultAsync(w => w.ShiftName == shiftName && w.IsActive, ct);
        }

        public async Task<WorkShift?> GetByDeptIdAsync(int deptId, CancellationToken ct = default)
        {
            return await _dbSet.FirstOrDefaultAsync(w => w.DeptId == deptId && w.IsActive, ct);
        }

        public async Task<List<WorkShift>> GetAllActiveWithDepartmentAsync(CancellationToken ct = default)
        {
            return await _dbSet.Include(w => w.Department).Where(w => w.IsActive).AsNoTracking().ToListAsync(ct);
        }
    }
}
