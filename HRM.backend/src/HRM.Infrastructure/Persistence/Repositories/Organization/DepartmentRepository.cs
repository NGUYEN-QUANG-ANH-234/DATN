using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Organization;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.Organization
{
    public class DepartmentRepository : BaseRepository<Department>, IDepartmentRepository
    {
        public DepartmentRepository(MyDbContext context) : base(context) { }

        public async Task<List<Department>> GetAllActiveAsync(CancellationToken ct = default)
        {
            // Trả về danh sách không tracking để tăng tốc độ dựng Sơ đồ cây (Chỉ đọc)
            return await _dbSet
                .Where(d => d.Status == DeptStatus.Active)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<bool> HasActiveSubDepartmentsAsync(int deptId, CancellationToken ct = default)
        {
            // Kiểm tra siêu tốc xem có phòng ban con nào còn hoạt động không
            return await _dbSet
                .AnyAsync(d => d.ParentDeptId == deptId && d.Status == DeptStatus.Active, ct);
        }

        public async Task<bool> CheckCodeExistsAsync(string deptCode, CancellationToken ct = default)
        {
            return await _dbSet.AnyAsync(d => d.DeptCode == deptCode, ct);
        }
    }
}
