using HRM.backend.src.HRM.Application.DTOs.Organization;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TimeAttendance
{
    public class LeaveBalanceRepository : BaseRepository<LeaveBalance>, ILeaveBalanceRepository
    {
        public LeaveBalanceRepository(MyDbContext context) : base(context) { }

        public async Task UpdateDeptAllocatedDaysAsync(int deptId, int leaveTypeId, short year, decimal totalDays, CancellationToken ct = default)
        {
            // Lấy danh sách ID của tất cả nhân viên thuộc phòng ban và chưa nghỉ việc
            var employeeIds = await _context.Employees
                .Where(e => e.DeptId == deptId && e.Status != Core.Enums.EmployeeStatus.Terminated)
                .Select(e => e.Id)
                .ToListAsync(ct);

            foreach (var empId in employeeIds)
            {
                // Tìm kiếm bản ghi leave_balance dựa trên Composite Key
                var balance = await _dbSet.FirstOrDefaultAsync(b => b.EmployeeId == empId && b.LeaveTypeId == leaveTypeId && b.Year == year, ct);

                if (balance != null)
                {
                    // Đã tồn tại: Tiến hành cập nhật allocated_days (TotalDays)
                    balance.TotalDays = totalDays;
                }
                else
                {
                    // Chưa tồn tại: Khởi tạo bản ghi phép đầu năm cho nhân sự mới
                    var newBalance = new LeaveBalance
                    {
                        EmployeeId = empId,
                        LeaveTypeId = leaveTypeId,
                        Year = year,
                        TotalDays = totalDays,
                        UsedDays = 0
                    };
                    await _dbSet.AddAsync(newBalance, ct);
                }
            }
        }

        public async Task<List<DeptLeaveConfigDto>> GetDeptLeaveConfigsAsync(CancellationToken ct = default)
        {
            // Nhóm dữ liệu LeaveBalance theo Phòng ban để lấy ra cấu hình chuẩn
            return await _context.LeaveBalances
                .Include(lb => lb.Employee)
                .Include(lb => lb.LeaveType)
                .Where(lb => lb.Employee != null && lb.Employee.DeptId != null)
                .GroupBy(lb => new {
                    DeptId = lb.Employee!.DeptId!.Value,
                    LeaveTypeName = lb.LeaveType!.TypeName,
                    lb.Year,
                    lb.TotalDays
                })
                .Select(g => new DeptLeaveConfigDto
                {
                    DeptId = g.Key.DeptId,
                    LeaveTypeName = g.Key.LeaveTypeName ?? "",
                    Year = g.Key.Year,
                    TotalDays = g.Key.TotalDays ?? 0
                })
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}
