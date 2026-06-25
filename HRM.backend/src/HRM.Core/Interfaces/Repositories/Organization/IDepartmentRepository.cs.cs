using HRM.backend.src.HRM.Core.Entities.Organization;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.Organization
{
    public interface IDepartmentRepository : IBaseRepository<Department>
    {
        Task<List<Department>> GetAllActiveAsync(CancellationToken ct = default);
        Task<bool> HasActiveSubDepartmentsAsync(int deptId, CancellationToken ct = default);
        Task<bool> HasAnySubDepartmentsAsync(int deptId, CancellationToken ct = default);
        Task<bool> CheckCodeExistsAsync(string deptCode, CancellationToken ct = default);

    }
}
