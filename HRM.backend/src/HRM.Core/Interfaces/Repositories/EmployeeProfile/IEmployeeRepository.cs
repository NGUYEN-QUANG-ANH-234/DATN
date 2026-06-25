using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.WorkflowRequests;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile
{
    public interface IEmployeeRepository : IBaseRepository<Employee>
    {
        Task<int> CountActiveInDeptAsync(int deptId, CancellationToken ct = default);
        Task<int> CountInDeptAsync(int deptId, CancellationToken ct = default);
        Task<bool> CheckIdentityNumberExistsAsync(string identityNumber, int excludeEmployeeId, CancellationToken ct = default);
        Task<Employee?> GetProfileByIdAsync(int id, CancellationToken ct = default);
        Task<Employee?> GetByAccountIdAsync(int accountId, CancellationToken ct = default);
        Task<Employee?> GetDocumentProfileByIdAsync(int id, CancellationToken ct = default);
        Task<Employee?> GetDocumentProfileByAccountIdAsync(int accountId, CancellationToken ct = default);
        Task<List<Employee>> GetActiveByDeptWithDepartmentAsync(int deptId, int? excludeEmployeeId = null, CancellationToken ct = default);
        Task<List<Employee>> GetActiveWithDepartmentAsync(CancellationToken ct = default);
        Task<List<int>> GetManagedDepartmentIdsByAccountIdAsync(int accountId, CancellationToken ct = default);
    }
}
