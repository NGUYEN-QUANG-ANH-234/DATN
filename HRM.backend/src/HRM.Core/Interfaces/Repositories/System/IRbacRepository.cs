using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Core.Entities.System;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.System
{
    public interface IRbacRepository 
    {
        Task<IEnumerable<RoleWithPermissionsDto>> FetchRolesWithPermissionsAsync(CancellationToken ct = default);
        Task UpdateRolePermissionsAsync(int roleId, IEnumerable<string> permissionCodes, CancellationToken ct = default);
        Task<IEnumerable<string>> GetAllPermissionCodesAsync(CancellationToken ct = default);
        Task<IEnumerable<PermissionGroupDto>> GetGroupedPermissionsAsync(CancellationToken ct = default);
    }
}
