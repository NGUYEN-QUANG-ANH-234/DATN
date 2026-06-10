using HRM.backend.src.HRM.Application.DTOs.Organization;

namespace HRM.backend.src.HRM.Application.Interfaces.System.UseCases
{
    public interface IOrgTreeUseCase
    {
        Task<List<DepartmentTreeDto>> GetOrganizationTreeAsync(CancellationToken ct = default);
        Task<bool> UpdateDepartmentNodeAsync(int deptId, UpdateDeptStructureDto dto, int actorId, CancellationToken ct = default);
        Task<bool> UpdateDepartmentAsync(int deptId, UpdateDepartmentDto dto, int actorId, CancellationToken ct = default);
        Task<bool> DeactivateDepartmentAsync(int deptId, int actorId, CancellationToken ct = default);
        Task<int> CreateDepartmentAsync(CreateDepartmentDto dto, int actorId, CancellationToken ct = default);
    }
}
