using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.RequestHandover;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.EmployeeProfile
{
    public class EmployeeRepository : BaseRepository<Employee>, IEmployeeRepository
    {
        public EmployeeRepository(MyDbContext context) : base(context) { }

        public async Task<int> CountActiveInDeptAsync(int deptId, CancellationToken ct = default)
        {
            // Đếm số lượng nhân sự chưa nghỉ việc thuộc phòng ban này
            return await _dbSet
                .CountAsync(e => e.DeptId == deptId && e.Status != EmployeeStatus.Terminated, ct);
        }

        public async Task<bool> CheckIdentityNumberExistsAsync(string identityNumber, int excludeEmployeeId, CancellationToken ct = default)
        {
            return await _dbSet.AnyAsync(e => e.IdentityNumber == identityNumber && e.Id != excludeEmployeeId, ct);
        }

        public async Task<Employee?> GetProfileByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        }

        public async Task<Employee?> GetByAccountIdAsync(int accountId, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(e => e.Account)
                .FirstOrDefaultAsync(e => e.AccountId == accountId, ct);
        }

        public async Task<Employee?> GetDocumentProfileByIdAsync(int id, CancellationToken ct = default)
        {
            return await BuildDocumentProfileQuery()
                .FirstOrDefaultAsync(e => e.Id == id, ct);
        }

        public async Task<Employee?> GetDocumentProfileByAccountIdAsync(int accountId, CancellationToken ct = default)
        {
            return await BuildDocumentProfileQuery()
                .FirstOrDefaultAsync(e => e.AccountId == accountId, ct);
        }

        private IQueryable<Employee> BuildDocumentProfileQuery()
        {
            return _dbSet
                .Include(e => e.Account)
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Include(e => e.JobLevel)
                .Include(e => e.Manager)
                .Include(e => e.Contracts)
                .AsNoTracking();
        }

        public async Task<List<Employee>> GetActiveByDeptWithDepartmentAsync(int deptId, int? excludeEmployeeId = null, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(e => e.Department)
                .Where(e => e.DeptId == deptId &&
                            e.Status != EmployeeStatus.Terminated &&
                            (!excludeEmployeeId.HasValue || e.Id != excludeEmployeeId.Value))
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<Employee>> GetActiveWithDepartmentAsync(CancellationToken ct = default)
        {
            return await _dbSet
                .Include(e => e.Department)
                .Where(e => e.Status != EmployeeStatus.Terminated)
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}
