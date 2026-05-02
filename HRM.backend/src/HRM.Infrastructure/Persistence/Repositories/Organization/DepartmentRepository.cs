using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Organization;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.Organization
{
    public class DepartmentRepository : BaseRepository<Department>, IDepartmentRepository
    {
        public DepartmentRepository(MyDbContext context) : base(context) { }

        public async Task DissolveDepartmentAsync(int deptId)
        {
            var dept = await _dbSet.FindAsync(deptId);
            if (dept != null)
            {
                // Cập nhật trạng thái phòng ban thành Giải thể (Dissolved / Inactive)
                dept.Status = DeptStatus.Dissolved;
                // Hoặc dept.IsActive = false; tùy theo thiết kế Entity của bạn
            }
        }

        public async Task ProcessTerminationAsync(int employeeId)
        {
            // Truy cập chéo sang bảng Employees thông qua _context
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee != null)
            {
                employee.Status = EmployeeStatus.Terminated; // Cập nhật trạng thái nghỉ việc
                employee.DeptId = null; // Gỡ khỏi phòng ban
            }
        }

        public async Task ProcessTransferAsync(int employeeId, int newDeptId)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee != null)
            {
                employee.DeptId = newDeptId; // Chuyển sang phòng ban mới
            }
        }

        public async Task<Department?> GetDepartmentWithEmployeesAsync(int deptId)
        {
            // Hỗ trợ UseCase check xem phòng ban còn nhân sự không trước khi giải thể
            return await _dbSet
                .Include(d => d.Employees)
                .FirstOrDefaultAsync(d => d.Id == deptId);
        }
    }
}
