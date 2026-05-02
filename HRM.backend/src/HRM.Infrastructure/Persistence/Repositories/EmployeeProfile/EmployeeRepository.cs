using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.RequestHandover;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.EmployeeProfile
{
    public class EmployeeRepository : BaseRepository<Employee>, IEmployeeRepository
    {
        public EmployeeRepository(MyDbContext context) : base(context) { }

        public async Task UpdateProfileInfoAsync(Employee employee)
        {
            // Chỉ cập nhật trạng thái In-Memory, chờ UoW commit
            _dbSet.Update(employee);
            await Task.CompletedTask;
        }

        public async Task<(IEnumerable<EmploymentHistory> Items, int TotalCount)> FetchHistoryByEmployeeIdAsync(
            int employeeId, DateTime? fromDate, DateTime? toDate, int skip, int take)
        {
            // Giả định bạn có entity EmploymentHistory trong DbContext
            var query = _context.EmploymentHistories.Where(h => h.EmployeeId == employeeId);

            if (fromDate.HasValue) query = query.Where(h => h.EffectiveDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(h => h.EffectiveDate <= toDate.Value);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(h => h.EffectiveDate)
                                   .Skip(skip).Take(take)
                                   .ToListAsync();

            return (items, total);
        }
    }
}
