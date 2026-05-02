using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.RequestHandover;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile
{
    public interface IEmployeeRepository : IBaseRepository<Employee>
    {
        // --- 1. Mảng Profile ---
        Task UpdateProfileInfoAsync(Employee employee);

        // --- 2. Mảng History ---
        // Lưu ý: Không truyền DTO (HistoryFilterDto) xuống Repo. Tách thành các tham số nguyên thủy.
        Task<(IEnumerable<EmploymentHistory> Items, int TotalCount)> FetchHistoryByEmployeeIdAsync(
            int employeeId,
            DateTime? fromDate,
            DateTime? toDate,
            int skip,
            int take);
    }
}
