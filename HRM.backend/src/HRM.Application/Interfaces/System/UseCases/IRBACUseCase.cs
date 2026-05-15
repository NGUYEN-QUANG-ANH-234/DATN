using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.System;

namespace HRM.backend.src.HRM.Application.Interfaces.System.UseCases
{
    public interface IRbacUseCase
    {
        Task<IEnumerable<RoleWithPermissionsDto>> GetAllRolesAndPermissionsAsync(CancellationToken ct = default);
        Task<bool> UpdateRolePermissionsAsync(UpdateRolePermissionsDto dto, int adminId, CancellationToken ct = default);
        Task<IEnumerable<PermissionGroupDto>> GetAllAvailablePermissionsAsync(CancellationToken ct = default);
        Task<IEnumerable<RoleDto>> GetSystemRolesAsync(CancellationToken ct = default);
    }
}
